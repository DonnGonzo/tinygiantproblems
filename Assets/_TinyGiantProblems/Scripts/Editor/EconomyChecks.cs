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

            var board = TaskBoard.MossbackContract(new[] { "A", "B", "C" });
            Check(board.Report(TaskKind.CareJob, 1, 1) == 40 && board.CareOrder(1).paid, "Care job pays its bounty once");
            Check(board.Report(TaskKind.CareJob, 1, 1) == 0, "Bounty cannot be paid twice");
            int paid = 0; for (int i = 0; i < 8; i++) paid += board.Report(TaskKind.ClearMess, 1, (int)MessKind.Grime);
            Check(paid == 35, "Grime order pays after eight patches");
            Check(board.Report(TaskKind.ClearMess, 1, (int)MessKind.Burr) == 0, "Mess kinds track separately");
            Check(board.Report(TaskKind.SellDebris, 12) == 50, "Selling twelve pieces completes the salvage order");

            int starterValue = 0, mandatory = 0;
            foreach (var o in TaskBoard.MossbackContract(new[] { "A", "B", "C" }).orders) if (!o.optional) mandatory += o.bounty;
            foreach (var d in ItemCatalog.All) if (d.starter) starterValue++;
            Check(starterValue == 2 && ItemCatalog.Get(ItemId.Sprayer).price <= mandatory - 60, "Sprayer is affordable from one run of work orders before the hot spot");
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
