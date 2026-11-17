using System.Collections.Generic;
using UnityEngine;

namespace TinyGiantProblems
{
    public enum ItemId
    {
        None = 0,
        // Tools
        Brush = 1, Vacuum = 2, Sprayer = 3, RopeLauncher = 4, Grabber = 5, Scanner = 6, SnackLauncher = 7, Glider = 8, Claw = 9,
        // Upgrades (owned, never placed in a slot)
        BigSack = 30, TurboBristles = 31,
        // Debris and finds (stackable, sold at the salvage station)
        MossClump = 50, Burr = 51, Twig = 52, PestJar = 53, ShinyPebble = 54, GuildBadge = 55, LostDrone = 56,
    }

    public enum ItemKind { Tool, Upgrade, Debris }

    public sealed class ItemDef
    {
        public ItemId id;
        public ItemKind kind;
        public string name, glyph, blurb;
        public CareTool care = CareTool.Hands;
        public int price, sellValue, maxStack = 1;
        public bool starter;
        public Color color;
        // ItemLibrary key for the in-world model; empty uses a primitive stand-in.
        public string model;
        // World size, in metres, of a dropped or held copy.
        public float size = .5f;
        public bool IsTool => kind == ItemKind.Tool;
        public bool IsDebris => kind == ItemKind.Debris;
    }

    public static class ItemCatalog
    {
        static readonly Dictionary<ItemId, ItemDef> defs = new Dictionary<ItemId, ItemDef>();
        public static readonly List<ItemDef> ShopOrder = new List<ItemDef>();

        static ItemCatalog()
        {
            // Starter tools: everything needed to earn the first coins.
            Tool(ItemId.Brush, "Grooming Brush", "BR", "Scrubs grime and picks burrs out of the fur.", CareTool.Groomer, 0, true, new Color(.93f, .69f, .32f), "grooming-brush/grooming-brush");
            Tool(ItemId.Vacuum, "Pest Vacuum", "VC", "Sucks up Bramblemites and bottles them for sale.", CareTool.Vacuum, 0, true, new Color(.83f, .33f, .3f), "pest-vacuum/pest-vacuum");
            // Purchased tools: earned through work orders and salvage.
            Tool(ItemId.Sprayer, "Soothing Sprayer", "SP", "Cools hot spots. Mossback's favourite.", CareTool.Sprayer, 120, false, new Color(.33f, .78f, .68f), "soothing-sprayer/soothing-sprayer");
            Tool(ItemId.RopeLauncher, "Rope Launcher", "RL", "Clip a safety tether to a gold socket and reel in.", CareTool.Tether, 80, false, new Color(.95f, .82f, .45f), "rope-launcher/rope-launcher");
            Tool(ItemId.Grabber, "Multi-Grabber", "GR", "Hold USE to pull loose debris from 10 m away.", CareTool.Hands, 150, false, new Color(.55f, .62f, .86f), "multi-grabber-tool/multi-grabber-tool");
            Tool(ItemId.Scanner, "Portable Scanner", "SC", "Pings every mess and hidden find nearby.", CareTool.Hands, 180, false, new Color(.45f, .86f, .95f), "portable-scanner/portable-scanner");
            Tool(ItemId.SnackLauncher, "Snack Launcher", "SN", "Lob a treat: big comfort, delays the next sneeze.", CareTool.Hands, 250, false, new Color(.98f, .55f, .3f), "snack-launcher/snack-launcher");
            Tool(ItemId.Claw, "Climbing Claw", "CL", "Walk into Mossback's flanks to climb anywhere. Watch the claw stamina bar.", CareTool.Hands, 200, false, new Color(.86f, .66f, .25f), "climbing-claw/climbing-claw");
            Tool(ItemId.Glider, "Wind Glider", "GL", "Equip while falling to float down gently.", CareTool.Hands, 300, false, new Color(.72f, .9f, .55f), "glider/glider");
            Upgrade(ItemId.BigSack, "Big Debris Sack", "+S", "Debris stacks hold 32 instead of 16.", 100, new Color(.75f, .58f, .38f));
            Upgrade(ItemId.TurboBristles, "Turbo Bristles", "+T", "Brush and vacuum work 50% faster.", 220, new Color(.98f, .8f, .3f));
            Debris(ItemId.MossClump, "Moss Clump", "MC", "Matted moss. The salvage yard composts it.", 6, new Color(.45f, .62f, .3f), "plants/moss", .55f);
            Debris(ItemId.Burr, "Burr", "BU", "Spiky seed head pulled from the fur.", 9, new Color(.62f, .45f, .25f), "burr-cluster/burr-cluster", .45f);
            Debris(ItemId.Twig, "Shed Twig", "TW", "Mossback grows a little forest. Some falls off.", 7, new Color(.5f, .34f, .2f), "branch/branch", .8f);
            Debris(ItemId.PestJar, "Bottled Bramblemite", "BM", "A sealed canister of very grumpy mites.", 14, new Color(.86f, .3f, .32f), "spare-canister/spare-canister", .45f);
            Debris(ItemId.ShinyPebble, "Shiny Pebble", "PB", "Found under the grime. Collectors love these.", 30, new Color(.75f, .85f, .95f), "rocks/small-rock", .35f);
            Debris(ItemId.GuildBadge, "Lost Guild Badge", "GB", "Some technician will be very relieved.", 45, new Color(1f, .78f, .25f), "", .3f);
            Debris(ItemId.LostDrone, "Lost Survey Drone", "SD", "Guild property, last seen under the jaw.", 60, new Color(.6f, .7f, .78f), "survey-drone/survey-drone", .7f);
        }

        static void Tool(ItemId id, string name, string glyph, string blurb, CareTool care, int price, bool starter, Color color, string model)
        {
            var d = new ItemDef { id = id, kind = ItemKind.Tool, name = name, glyph = glyph, blurb = blurb, care = care, price = price, starter = starter, color = color, model = model, maxStack = 1, size = .6f };
            defs[id] = d; if (!starter) ShopOrder.Add(d);
        }
        static void Upgrade(ItemId id, string name, string glyph, string blurb, int price, Color color)
        {
            var d = new ItemDef { id = id, kind = ItemKind.Upgrade, name = name, glyph = glyph, blurb = blurb, price = price, color = color, maxStack = 1 };
            defs[id] = d; ShopOrder.Add(d);
        }
        static void Debris(ItemId id, string name, string glyph, string blurb, int value, Color color, string model, float size)
        {
            defs[id] = new ItemDef { id = id, kind = ItemKind.Debris, name = name, glyph = glyph, blurb = blurb, sellValue = value, color = color, model = model, size = size, maxStack = 16 };
        }

        public static ItemDef Get(ItemId id) => defs.TryGetValue(id, out var d) ? d : null;
        public static IEnumerable<ItemDef> All => defs.Values;
        public static ItemId ForCareTool(CareTool tool)
        {
            switch (tool)
            {
                case CareTool.Groomer: return ItemId.Brush;
                case CareTool.Vacuum: return ItemId.Vacuum;
                case CareTool.Sprayer: return ItemId.Sprayer;
                case CareTool.Tether: return ItemId.RopeLauncher;
                default: return ItemId.None;
            }
        }
    }
}
