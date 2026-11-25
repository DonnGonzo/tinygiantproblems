using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TinyGiantProblems.Editor
{
    // Fast logic checks for inventory, bounties and catalog data. Run from the
    // menu or headless: -executeMethod TinyGiantProblems.Editor.EconomyChecks.Run
    public static class EconomyChecks
    {
        static readonly List<string> results = new List<string>();
        static void Check(bool pass, string name) { results.Add((pass ? "PASS " : "FAIL ") + name); if (!pass) Debug.LogError("Economy check failed: " + name); }

        [MenuItem("Tiny Giant Problems/Run Economy Checks")]
        public static void Run()
        {
            results.Clear();
            var inv = new Inventory();
            Check(inv.Add(ItemId.Brush, 1) == 0 && inv.slots[0].id == ItemId.Brush, "Tool goes to first hotbar slot");
            Check(inv.Add(ItemId.Brush, 1) == 0 && inv.Count(ItemId.Brush) == 2, "Tools never stack");
            inv.Clear();
            Check(inv.Add(ItemId.MossClump, 20) == 0 && inv.slots[0].count == 16 && inv.slots[1].count == 4, "Debris stacks to 16 then spills");
            inv.debrisStackLimit = 32;
            Check(inv.Add(ItemId.MossClump, 20) == 0 && inv.slots[0].count == 32 && inv.slots[1].count == 8, "Big sack raises stack size");
            inv.MoveOrSwap(1, 5);
            Check(inv.slots[1].IsEmpty && inv.slots[5].count == 8, "Drag moves a stack to an empty slot");
            inv.Add(ItemId.Burr, 3);
            int burrSlot = System.Array.FindIndex(inv.slots, s => s.id == ItemId.Burr);
            inv.MoveOrSwap(burrSlot, 0);
            Check(inv.slots[0].id == ItemId.Burr && inv.slots[burrSlot].id == ItemId.MossClump, "Drag swaps different items");
            inv.QuickMove(0);
            Check(inv.slots[0].IsEmpty && inv.Count(ItemId.Burr) == 3 && System.Array.FindIndex(inv.slots, s => s.id == ItemId.Burr) >= Inventory.HotbarSize, "Shift-click moves hotbar stack into backpack");
            int value = inv.TakeAllDebris(out int pieces);
            Check(pieces == 43 && value == 40 * 6 + 3 * 9 && inv.Count(ItemId.MossClump) == 0, "Selling empties all debris and totals value");
            inv.Clear();
            for (int i = 0; i < Inventory.Size; i++) inv.Add(ItemId.Brush, 1);
            Check(inv.RoomFor(ItemId.Burr) == 0 && inv.Add(ItemId.Burr, 2) == 2, "Full bag rejects pickups");
            inv.Select(3); inv.Scroll(1); Check(inv.selected == 4, "Scroll advances selection");
            inv.Select(0); inv.Scroll(-1); Check(inv.selected == 8, "Scroll wraps around the hotbar");
            var json = JsonUtility.ToJson(inv); var copy = JsonUtility.FromJson<Inventory>(json); copy.Sanitize();
            Check(copy.Count(ItemId.Brush) == Inventory.Size && copy.selected == 8, "Inventory survives a save round trip");

            var catalog = MissionCatalog.Instance;
            Check(catalog && catalog.missions.Length >= 6, "Mission catalog has at least six missions");
            if (catalog)
            {
                var ids = new HashSet<string>(); bool idsOk = true, reqOk = true, mainOk = true, tierOk = true; int lastTier = 1;
                bool timed = false, noDamage = false, collect = false;
                foreach (var m in catalog.missions)
                {
                    if (!m || string.IsNullOrEmpty(m.id) || !ids.Add(m.id)) { idsOk = false; continue; }
                    foreach (var r in m.requires) if (!ids.Contains(r)) reqOk = false;   // only earlier missions
                    bool hasMain = false; foreach (var o in m.objectives) if (!o.optional) hasMain = true;
                    if (!hasMain) mainOk = false;
                    if (m.tier < lastTier) tierOk = false; lastTier = m.tier;
                    foreach (var c in m.challenges) { if (c.kind == ChallengeKind.TimeLimit) timed = true; if (c.kind == ChallengeKind.NoFallHurt || c.kind == ChallengeKind.NoSting) noDamage = true; if (c.kind == ChallengeKind.AllTrinkets) collect = true; }
                }
                Check(idsOk, "Mission ids are unique"); Check(reqOk, "Missions only require earlier missions"); Check(mainOk, "Every mission has a main objective");
                Check(tierOk && catalog.missions[catalog.missions.Length - 1].tier > catalog.missions[0].tier, "Difficulty tier ramps up through the catalog");
                Check(timed && noDamage && collect, "Timed, no-damage and collectible challenges exist");
                Check(catalog.missions[0].requires.Length == 0, "First mission is unlocked from the start");
                int early = 0; foreach (var o in catalog.missions[0].objectives) early += o.reward;
                Check(ItemCatalog.Get(ItemId.Sprayer).price <= early, "First mission pays enough mid-run to buy the Sprayer for the hot spot");
                var t = catalog.missions[catalog.missions.Length - 1];
                Check(t.ReactionScale < catalog.missions[0].ReactionScale && t.MessScale > catalog.missions[0].MessScale, "Higher tiers mean faster reactions and more mess");
            }
            int starterValue = 0;
            foreach (var d in ItemCatalog.All) if (d.starter) starterValue++;
            Check(starterValue == 2, "Two starter tools");
            foreach (var d in ItemCatalog.All)
                if (!string.IsNullOrEmpty(d.model)) Check(ItemLibrary.Instance && ItemLibrary.Instance.Prefab(d.model), "Item library has a model for " + d.name);
            Check(ItemLibrary.Instance && ItemLibrary.Instance.baseMaterial && ItemLibrary.Instance.particleMaterial, "Item library materials are assigned");
            int placeables = 0, kits = 0;
            foreach (var d in ItemCatalog.All) if (d.placeable) { placeables++; if (d.needsPlatform) kits++; }
            Check(placeables == 5 && kits == 4 && !ItemCatalog.Get(ItemId.Platform).needsPlatform, "Platform goes on Mossback, four kits go on a platform");
            Check(ItemCatalog.ShopOrder.Contains(ItemCatalog.Get(ItemId.Platform)) && ItemCatalog.Get(ItemId.Platform).price > ItemCatalog.Get(ItemId.Glider).price, "Floating Platform is sold and priced above the glider");
            var crateInv = new Inventory(); crateInv.Add(ItemId.Burr, 4);
            int full = crateInv.TakeAllDebris(out int crated);
            Check(crated == 4 && Mathf.RoundToInt(full * SupplyCrate.Rate) == 27, "Supply Crate pays 75% of the yard price");
            var controls = Resources.Load<UnityEngine.InputSystem.InputActionAsset>("Input/TinyGiantControls");
            bool allBound = controls;
            if (controls) foreach (var a in controls.FindActionMap("Player").actions) { bool kb = false, pad = false; foreach (var b in a.bindings) { if (b.path.StartsWith("<Keyboard>") || b.path.StartsWith("<Mouse>")) kb = true; if (b.path.StartsWith("<Gamepad>")) pad = true; } if (!kb && !a.name.StartsWith("Hotbar") && a.name != "Zoom") allBound = false; if (!pad && !a.name.StartsWith("Slot") && a.name != "HotbarScroll" && a.name != "ZoomModifier" && a.name != "Recover") allBound = false; }
            Check(allBound, "Every control has keyboard/mouse and gamepad bindings in the rebindable action asset");
            var tune = Resources.Load<MovementTuning>("MovementTuning");
            Check(tune && tune.acceleration > 0 && tune.coyoteTime > 0 && tune.jumpBuffer > 0, "Movement tuning asset exists with acceleration, coyote time and jump buffer");
            Check(ItemLibrary.Instance && ItemLibrary.Instance.Prefab(GuildHouse.ExteriorKey) && ItemLibrary.Instance.Prefab(GuildHouse.InteriorKey), "Guild House exterior and interior models are in the item library");
            Check(System.Type.GetType("TinyGiantProblems.GuildLift, Assembly-CSharp") == null && System.Type.GetType("TinyGiantProblems.WinchPost, Assembly-CSharp") != null, "Guild Lift is gone; the yard winch post replaces it");
            var look = (Color[])TechnicianLook.Defaults.Clone(); look[TechnicianLook.Trousers] = new Color(.1f, .2f, .6f);
            var lookBack = TechnicianLook.FromJson(TechnicianLook.ToJson(look));
            Check(lookBack[TechnicianLook.Trousers] == look[TechnicianLook.Trousers] && lookBack.Length == TechnicianLook.RegionNames.Length, "Guild member colours survive a save round trip");
            Check(TechnicianLook.FromJson("not json")[TechnicianLook.Hat] == TechnicianLook.Defaults[TechnicianLook.Hat], "Broken colour save falls back to defaults");
            foreach (var d in ItemCatalog.All) if (ItemIcons.HasPicture(d)) Check(Resources.Load<Texture2D>("ItemIcons/" + d.id), "Baked icon for " + d.name);

            Directory.CreateDirectory("TestResults");
            File.WriteAllLines("TestResults/economy-checks.txt", results);
            bool failed = results.Exists(r => r.StartsWith("FAIL"));
            Debug.Log("Economy checks " + (failed ? "FAILED" : "PASSED") + "\n" + string.Join("\n", results));
            if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
        }
    }
}
