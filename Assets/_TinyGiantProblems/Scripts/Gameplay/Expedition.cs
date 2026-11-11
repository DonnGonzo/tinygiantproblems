using UnityEngine;
using UnityEngine.InputSystem;
namespace TinyGiantProblems
{
    public sealed class Expedition : MonoBehaviour
    {
        public static Expedition Instance { get; private set; }
        public TechnicianMotor player;
        public CareJob[] jobs;
        public Transform extraction;
        public CreatureFrame creature;
        public AudioSource music, effects;
        public AudioClip sneeze, shake, complete, spray;
        public ExpeditionPhase Phase = ExpeditionPhase.Briefing;
        public float Comfort = 38, Irritation = 30, Elapsed, WarningRemaining;
        public bool Paused;
        public string Caption = "Welcome to the Small Works Guild. Mossback could use a little help.";
        public string Reaction = "";
        public float nextReaction = 32;
        public int reactionCount;
        public CareJob Nearest { get; private set; }
        public bool IsPlaying => Phase == ExpeditionPhase.Working || Phase == ExpeditionPhase.Extraction;
        public int Completed { get { int n = 0; foreach (var j in jobs) if (j.state.Complete) n++; return n; } }
        public int Reward => CareRules.Reward(Completed, player.recoveries, Elapsed);
        float sprayCooldown;
        public System.Action<CareJob,float> CareRequested;
        public System.Action ExtractionRequested;
        public System.Action MissionBegan;
        public bool IsRemote;
        void Awake() { Instance = this; Application.runInBackground = true; }
        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame || Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
            { if (IsPlaying) SetPause(!Paused); }
            if (!IsPlaying || Paused) return;
            var dt = Time.deltaTime;
            Elapsed += dt;
            Nearest = null; float closest = 3.2f;
            foreach (var job in jobs)
            {
                var d = Vector3.Distance(player.transform.position, job.transform.position);
                if (!job.state.Complete && d < closest) { closest = d; Nearest = job; }
            }
            if (Nearest && player.Using && !player.braced && !player.climbing)
            {
                if (CareRequested != null) CareRequested(Nearest, dt);
                else ApplyCare(Nearest, player.selected, closest, dt);
            }
            if (!IsRemote)
            {
                nextReaction -= dt;
                if (WarningRemaining > 0)
                {
                    WarningRemaining -= dt;
                    if (WarningRemaining <= 0) Impact();
                }
                else if (nextReaction <= 0) Warn();
                Irritation = Mathf.Clamp(Irritation + dt * .12f, 0, 100);
                if (CareRules.CanExtract(Completed, Comfort)) Phase = ExpeditionPhase.Extraction;
            }
            if (Phase == ExpeditionPhase.Extraction && Vector3.Distance(player.transform.position, extraction.position) < 3.5f && player.InteractPressed) Finish();
        }
        public void ApplyCare(CareJob job, CareTool tool, float distance, float dt)
        {
            if (!IsPlaying || Paused || job == null) return;
            if (tool != job.tool) { Caption = "This job needs the " + job.tool + ". Select its tool slot."; return; }
            if (job.state.Apply(job.tool, tool, distance, false, dt))
            {
                Comfort = Mathf.Min(100, Comfort + 20); Irritation = Mathf.Max(0, Irritation - 15);
                effects.PlayOneShot(complete, .8f); Caption = "Care complete: " + job.title + ". Mossback feels better!";
            }
            else if (distance <= 3.2f)
            {
                sprayCooldown -= dt;
                if (sprayCooldown <= 0) { effects.PlayOneShot(spray, .2f); sprayCooldown = 1.8f; }
            }
        }
        public void Warn()
        {
            Reaction = reactionCount++ % 2 == 0 ? "FULL-BODY SHAKE" : "SNEEZE";
            WarningRemaining = 4;
            nextReaction = 30;
            Caption = Reaction == "SNEEZE" ? "[A huge inhale] Hold F / B to brace, or clip your tether!" : "[Low rumble, rustling fur] Hold F / B to brace!";
            effects.PlayOneShot(Reaction == "SNEEZE" ? sneeze : shake, .75f);
        }
        public void Impact()
        {
            WarningRemaining = 0;
            player.Launch(new Vector3(Reaction == "SNEEZE" ? 4 : -4, 10, 5));
            Caption = player.braced || player.tethered ? "[Secure!] Nice bracing. Back to work." : "[Whoooosh!] Clip a tether or let the safety winch catch you.";
            Reaction = "";
            Irritation = Mathf.Max(0, Irritation - 10);
        }
        public void Begin(bool replicated = false)
        {
            if (IsRemote && !replicated) { Caption = "The host starts the next expedition."; return; }
            Phase = ExpeditionPhase.Working; Comfort = 38; Irritation = 30; Elapsed = 0; nextReaction = 32;
            WarningRemaining = 0; reactionCount = 0; Reaction = "";
            foreach (var job in jobs) job.state.Progress = 0;
            player.ResetMotor(); SetPause(false);
            Caption = "Follow the gold trail. Hold the correct tool on each care site.";
            MissionBegan?.Invoke();
        }
        public void Finish(bool authoritative = false)
        {
            if (!CareRules.CanExtract(Completed, Comfort)) return;
            if (IsRemote && !authoritative) { ExtractionRequested?.Invoke(); return; }
            Phase = ExpeditionPhase.Debrief;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            PlayerPrefs.SetInt("GuildBest", Mathf.Max(PlayerPrefs.GetInt("GuildBest"), Reward)); PlayerPrefs.Save();
        }
        public void SetPause(bool value)
        {
            Paused = value;
            Time.timeScale = value && !IsRemote && CareRequested == null ? 0 : 1;
            Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked; Cursor.visible = value;
        }
        void OnDestroy() { Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; if (Instance == this) Instance = null; }
    }
}
