using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TinyGiantProblems
{
    // Wallet, owned tools, inventory and work orders. Lives on the Expedition
    // Authority object; progress persists between expeditions in PlayerPrefs.
    [DefaultExecutionOrder(5)]
    public sealed class GuildEconomy : MonoBehaviour
    {
        public static GuildEconomy Instance { get; private set; }
        public int Coins { get; private set; }
        public int RunEarnings { get; private set; }
        public int ContractPayout { get; private set; }
        public readonly HashSet<ItemId> Owned = new HashSet<ItemId>();
        public Inventory Inventory = new Inventory();
        public TaskBoard Board { get; private set; }
        public float CleanSpeed => Owns(ItemId.TurboBristles) ? 1.5f : 1f;
        public event Action<string, int> Toast;
        public event Action<WorkOrder> OrderCompleted;

        Expedition game;
        ExpeditionPhase lastPhase;
        bool[] jobDone = new bool[0];
        bool dirty;
        float saveTimer;
        const string SaveKey = "TGP.Save.v1";

        [Serializable] sealed class SaveData { public int coins; public int[] owned; public Inventory inventory; }

        void Awake()
        {
            Instance = this;
            game = GetComponent<Expedition>();
            if (!game) game = Expedition.Instance;
            Load();
            NewBoard();
            Inventory.Changed += () => dirty = true;
        }

        void Start()
        {
            if (game) { game.Began += OnBegin; lastPhase = game.Phase; }
        }

        void OnDestroy()
        {
            if (game) game.Began -= OnBegin;
            Save();
            if (Instance == this) Instance = null;
        }

        void OnApplicationQuit() => Save();

        void OnBegin()
        {
            RunEarnings = 0; ContractPayout = 0;
            NewBoard();
        }

        void NewBoard()
        {
            var titles = game && game.jobs != null ? game.jobs.Select(j => j.title).ToList() : new List<string>();
            Board = TaskBoard.MossbackContract(titles);
            Board.Completed = o =>
            {
                Toast?.Invoke("Work order complete · " + o.title, o.bounty);
                OrderCompleted?.Invoke(o);
            };
            jobDone = new bool[titles.Count];
        }

        void Update()
        {
            if (!game) return;
            if (game.jobs != null && jobDone.Length == game.jobs.Length)
                for (int i = 0; i < game.jobs.Length; i++)
                {
                    bool done = game.jobs[i].state.Complete;
                    if (done && !jobDone[i] && game.IsPlaying) Report(TaskKind.CareJob, 1, i);
                    jobDone[i] = done;
                }
            if (game.Phase == ExpeditionPhase.Debrief && lastPhase != ExpeditionPhase.Debrief)
            {
                ContractPayout = game.Reward;
                Earn(ContractPayout, "Contract payout · Guild pay");
                Save();
            }
            lastPhase = game.Phase;
            saveTimer -= Time.unscaledDeltaTime;
            if (dirty && saveTimer <= 0) { Save(); saveTimer = 2; }
        }

        public bool Owns(ItemId id) => Owned.Contains(id);

        public void Report(TaskKind kind, int amount = 1, int subtype = -1)
        {
            if (Board == null) return;
            int paid = Board.Report(kind, amount, subtype);
            if (paid > 0) { AddCoins(paid); ProceduralSfx.Play(ProceduralSfx.Coin, .9f); }
        }

        public void Earn(int amount, string reason)
        {
            if (amount <= 0) return;
            AddCoins(amount);
            Toast?.Invoke(reason, amount);
            ProceduralSfx.Play(ProceduralSfx.Coin, .9f);
        }

        void AddCoins(int amount) { Coins += amount; RunEarnings += amount; dirty = true; }

        public bool CanBuy(ItemDef d, out string reason)
        {
            reason = "";
            if (d == null || d.kind == ItemKind.Debris) { reason = "Not for sale"; return false; }
            if (Owns(d.id)) { reason = "Owned"; return false; }
            if (Coins < d.price) { reason = $"Need {d.price - Coins} more coins"; return false; }
            if (d.IsTool && Inventory.RoomFor(d.id) < 1) { reason = "Bag full: sell some salvage"; return false; }
            return true;
        }

        public bool TryBuy(ItemDef d)
        {
            if (!CanBuy(d, out var reason))
            {
                Toast?.Invoke(d != null ? d.name + ": " + reason : reason, 0);
                ProceduralSfx.Play(ProceduralSfx.Deny, .7f);
                return false;
            }
            Coins -= d.price;
            Owned.Add(d.id);
            if (d.IsTool) Inventory.Add(d.id, 1);
            ApplyUpgrades();
            Toast?.Invoke("Bought " + d.name, -d.price);
            ProceduralSfx.Play(ProceduralSfx.Buy, .9f);
            Save();
            return true;
        }

        public int SellAll(out int pieces)
        {
            var breakdown = new Dictionary<ItemId, int>();
            int value = Inventory.TakeAllDebris(out pieces, breakdown);
            if (pieces == 0) return 0;
            AddCoins(value);
            Toast?.Invoke($"Sold {pieces} salvage", value);
            ProceduralSfx.Play(ProceduralSfx.Sell, 1);
            if (breakdown.TryGetValue(ItemId.LostDrone, out var drones)) Report(TaskKind.RecoverDrone, drones);
            Report(TaskKind.SellDebris, pieces);
            Save();
            return value;
        }

        // Returns how many could not be stored.
        public int Collect(ItemId id, int count)
        {
            int left = Inventory.Add(id, count);
            if (left < count)
            {
                var d = ItemCatalog.Get(id);
                if (id == ItemId.ShinyPebble || id == ItemId.GuildBadge) Report(TaskKind.FindTrinket);
                if (id == ItemId.LostDrone) Toast?.Invoke("Found the lost survey drone! Sell it at the yard.", 0);
                ProceduralSfx.Play(ProceduralSfx.Pickup, .6f);
                if (d != null && (d.sellValue >= 30)) Toast?.Invoke("Found " + d.name, 0);
            }
            return left;
        }

        void ApplyUpgrades()
        {
            Inventory.debrisStackLimit = Owns(ItemId.BigSack) ? 32 : 16;
        }

        void EnsureStarterKit()
        {
            foreach (var d in ItemCatalog.All) if (d.starter) Owned.Add(d.id);
            ApplyUpgrades();
            Inventory.Sanitize();
            foreach (var id in Owned)
            {
                var d = ItemCatalog.Get(id);
                if (d != null && d.IsTool && !Inventory.Has(id)) Inventory.Add(id, 1);
            }
        }

        public void Save()
        {
            var data = new SaveData { coins = Coins, owned = Owned.Select(o => (int)o).ToArray(), inventory = Inventory };
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
            dirty = false;
        }

        void Load()
        {
            Owned.Clear(); Coins = 0; Inventory = new Inventory();
            var json = PlayerPrefs.GetString(SaveKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var data = JsonUtility.FromJson<SaveData>(json);
                    if (data != null)
                    {
                        Coins = Mathf.Max(0, data.coins);
                        if (data.owned != null) foreach (var o in data.owned) if (ItemCatalog.Get((ItemId)o) != null) Owned.Add((ItemId)o);
                        if (data.inventory != null) Inventory = data.inventory;
                    }
                }
                catch (Exception e) { Debug.LogWarning("Guild save could not be read, starting fresh: " + e.Message); }
            }
            EnsureStarterKit();
        }

        public void ResetProgress()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            var changed = Inventory.Changed;
            Load();
            Inventory.Changed = changed;
            Inventory.Changed?.Invoke();
            NewBoard();
            Toast?.Invoke("Guild progress reset", 0);
        }
    }
}
