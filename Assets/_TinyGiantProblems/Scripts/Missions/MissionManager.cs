using System;
using System.Collections.Generic;
using UnityEngine;

namespace TinyGiantProblems
{
    // Runs the active mission: tracks objectives from game events (Report),
    // side-task payouts, challenges, extraction gating, rewards and the saved
    // progression (completed missions, best times, earned challenges).
    [DefaultExecutionOrder(6)]
    public sealed class MissionManager : MonoBehaviour
    {
        public static MissionManager Instance { get; private set; }
        const string SaveKey = "TGP.Missions.v1";

        public MissionDef Selected { get; private set; }
        public MissionDef Active { get; private set; }
        public int[] progress = new int[0];
        public bool[] done = new bool[0];
        public bool[] challengeFailed = new bool[0];
        public bool MainComplete { get; private set; }
        public float RunTime => game ? game.Elapsed : 0;
        public MissionResult LastResult { get; private set; }

        public event Action<ObjectiveDef> ObjectiveCompleted;
        public event Action<MissionResult> MissionFinished;

        [Serializable] public sealed class Record { public string id; public bool done; public float bestTime; public int bestChallenges; public string[] challenges = new string[0]; }
        [Serializable] sealed class SaveData { public string selected; public List<Record> records = new List<Record>(); }
        public sealed class MissionResult { public MissionDef mission; public bool firstClear; public int coins; public List<string> challengesEarned = new List<string>(); public List<string> challengesMissed = new List<string>(); public float time; }

        SaveData save = new SaveData();
        Expedition game;
        int fallsAtStart, launchesAtStart, rescuesAtStart, stingsAtStart, glideCarry;
        float glideSeconds;

        void Awake()
        {
            Instance = this;
            game = GetComponent<Expedition>();
            Load();
            var catalog = MissionCatalog.Instance;
            Selected = catalog ? catalog.Get(save.selected) : null;
            if (!Selected || !IsUnlocked(Selected)) Selected = FirstPlayable();
        }

        void Start() { if (game) game.Began += OnBegin; }
        void OnDestroy() { if (game) game.Began -= OnBegin; if (Instance == this) Instance = null; }

        // ---- progression ----
        // Read-only lookups get a blank record; only Complete() adds one to the save.
        static readonly Record Blank = new Record();
        public Record RecordFor(MissionDef m) => save.records.Find(x => x.id == m.id) ?? Blank;
        Record EnsureRecord(MissionDef m) { var r = save.records.Find(x => x.id == m.id); if (r == null) { r = new Record { id = m.id }; save.records.Add(r); } return r; }
        public bool IsDone(MissionDef m) => m && save.records.Exists(r => r.id == m.id && r.done);
        public bool IsUnlocked(MissionDef m)
        {
            if (!m) return false;
            foreach (var req in m.requires) if (!save.records.Exists(r => r.id == req && r.done)) return false;
            return true;
        }
        public int CompletedCount { get { int n = 0; foreach (var r in save.records) if (r.done) n++; return n; } }
        MissionDef FirstPlayable()
        {
            var c = MissionCatalog.Instance; if (!c) return null;
            foreach (var m in c.missions) if (m && IsUnlocked(m) && !IsDone(m)) return m;
            foreach (var m in c.missions) if (m && IsUnlocked(m)) return m;
            return null;
        }
        public void Select(MissionDef m) { if (m && IsUnlocked(m)) { Selected = m; save.selected = m.id; Save(); } }

        // ---- run ----
        void OnBegin()
        {
            Active = Selected;
            int n = Active ? Active.objectives.Length : 0;
            progress = new int[n]; done = new bool[n];
            challengeFailed = new bool[Active ? Active.challenges.Length : 0];
            MainComplete = n == 0;
            var p = game.player; var fall = p.GetComponent<FallHurt>();
            fallsAtStart = fall ? fall.Falls : 0; launchesAtStart = p.launches; rescuesAtStart = p.recoveries; stingsAtStart = p.stings;
            glideSeconds = 0; glideCarry = 0;
            if (Active)
            {
                game.reactionScale = Active.ReactionScale;
                game.Caption = Active.title + ": " + Active.brief;
            }
        }

        public void Report(TaskKind kind, int amount = 1, int subtype = -1)
        {
            if (!Active || game == null || !game.IsPlaying) return;
            var economy = GuildEconomy.Instance;
            for (int i = 0; i < Active.objectives.Length; i++)
            {
                var o = Active.objectives[i];
                if (done[i] || o.kind != kind) continue;
                if (o.subtype >= 0 && o.subtype != subtype) continue;
                progress[i] = o.UsesBest ? Mathf.Max(progress[i], amount) : Mathf.Min(o.target, progress[i] + amount);
                if (progress[i] >= o.target)
                {
                    progress[i] = o.target; done[i] = true;
                    if (o.reward > 0 && economy) economy.Earn(o.reward, (o.optional ? "Side task · " : "Objective · ") + o.title);
                    else economy?.Notify("Objective complete · " + o.title);
                    ProceduralSfx.Play(ProceduralSfx.Coin, .9f);
                    ObjectiveCompleted?.Invoke(o);
                }
            }
            RefreshMain();
        }

        void RefreshMain()
        {
            bool all = true;
            for (int i = 0; i < Active.objectives.Length; i++) if (!Active.objectives[i].optional && !done[i]) all = false;
            if (all && !MainComplete)
            {
                MainComplete = true;
                game.Caption = "[Mission objectives done] Head to the extraction beacon and press " + GameInput.Display(GameInput.Interact) + " to finish.";
                ProceduralSfx.Play(ProceduralSfx.Sell, .8f);
            }
        }

        void Update()
        {
            if (!Active || !game || !game.IsPlaying) return;
            // Continuous objectives and challenge watchers.
            Report(TaskKind.Comfort, Mathf.FloorToInt(game.Comfort));
            var abilities = game.player.GetComponent<ToolAbilities>();
            if (abilities && abilities.Gliding) { glideSeconds += Time.deltaTime; int whole = Mathf.FloorToInt(glideSeconds); if (whole > glideCarry) { Report(TaskKind.Glide, whole - glideCarry); glideCarry = whole; } }
            for (int i = 0; i < Active.challenges.Length; i++) if (!challengeFailed[i] && Broken(Active.challenges[i], false)) { challengeFailed[i] = true; game.Caption = "[Challenge lost] " + Active.challenges[i].title; ProceduralSfx.Play(ProceduralSfx.Deny, .5f); }
        }

        bool Broken(ChallengeDef c, bool atFinish)
        {
            var p = game.player; var fall = p.GetComponent<FallHurt>();
            switch (c.kind)
            {
                case ChallengeKind.TimeLimit: return game.Elapsed > c.seconds;
                case ChallengeKind.NoFallHurt: return fall && fall.Falls > fallsAtStart;
                case ChallengeKind.NoLaunch: return p.launches > launchesAtStart;
                case ChallengeKind.NoRescue: return p.recoveries > rescuesAtStart;
                case ChallengeKind.NoSting: return p.stings > stingsAtStart;
                case ChallengeKind.AllTrinkets: return atFinish && TrinketsLeft() > 0;
            }
            return false;
        }

        static int TrinketsLeft()
        {
            int n = 0;
            foreach (var d in DebrisPickup.All) if (d && (d.item == ItemId.ShinyPebble || d.item == ItemId.GuildBadge)) n++;
            return n;
        }

        public string ChallengeStatus(int i)
        {
            var c = Active.challenges[i];
            if (challengeFailed[i]) return "<color=#FF7366>failed</color>";
            if (c.kind == ChallengeKind.TimeLimit) { float left = Mathf.Max(0, c.seconds - game.Elapsed); return $"{(int)left / 60}:{(int)left % 60:00} left"; }
            if (c.kind == ChallengeKind.AllTrinkets) return TrinketsLeft() + " left";
            return "on track";
        }

        // Called by Expedition.Finish when the technician extracts.
        public void Complete()
        {
            if (!Active) return;
            var r = EnsureRecord(Active);
            var result = new MissionResult { mission = Active, firstClear = !r.done, time = game.Elapsed };
            int coins = Active.rewardCoins;
            var earned = new List<string>(r.challenges);
            for (int i = 0; i < Active.challenges.Length; i++)
            {
                var c = Active.challenges[i];
                bool ok = !challengeFailed[i] && !Broken(c, true);
                if (ok) { result.challengesEarned.Add(c.title); if (!earned.Contains(c.title)) { earned.Add(c.title); coins += c.reward; } }
                else result.challengesMissed.Add(c.title);
            }
            if (!r.done) { coins += Active.rewardCoins / 2; if (Active.rewardItem != ItemId.None && GuildEconomy.Instance) GuildEconomy.Instance.Inventory.Add(Active.rewardItem, 1); }
            r.done = true; r.challenges = earned.ToArray(); r.bestChallenges = Mathf.Max(r.bestChallenges, result.challengesEarned.Count);
            r.bestTime = r.bestTime <= 0 ? game.Elapsed : Mathf.Min(r.bestTime, game.Elapsed);
            result.coins = coins;
            if (GuildEconomy.Instance) GuildEconomy.Instance.Earn(coins, "Mission complete · " + Active.title);
            LastResult = result;
            // Move the board on to the next mission that just unlocked.
            var next = FirstPlayable(); if (next) { Selected = next; save.selected = next.id; }
            Save();
            MissionFinished?.Invoke(result);
        }

        public int ObjectiveCount(bool optional) { int n = 0; if (Active) foreach (var o in Active.objectives) if (o.optional == optional) n++; return n; }

        void Load()
        {
            var json = PlayerPrefs.GetString(SaveKey, "");
            if (string.IsNullOrEmpty(json)) return;
            try { save = JsonUtility.FromJson<SaveData>(json) ?? new SaveData(); } catch { save = new SaveData(); }
            if (save.records == null) save.records = new List<Record>();
        }
        public void Save() { PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(save)); PlayerPrefs.Save(); }
        public void ResetProgress() { save = new SaveData(); PlayerPrefs.DeleteKey(SaveKey); Selected = FirstPlayable(); }
    }
}
