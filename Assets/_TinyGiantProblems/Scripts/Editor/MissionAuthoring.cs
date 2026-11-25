using System.IO;
using UnityEditor;
using UnityEngine;

namespace TinyGiantProblems.Editor
{
    // Writes the starting mission set to Resources/Missions as ScriptableObjects.
    // After that the assets are the source of truth: tweak them in the
    // Inspector. Re-running this menu item overwrites them with these defaults.
    public static class MissionAuthoring
    {
        const string Folder = "Assets/_TinyGiantProblems/Resources/Missions";

        static ObjectiveDef O(string title, TaskKind kind, int target = 1, int subtype = -1, int reward = 0) => new ObjectiveDef { title = title, kind = kind, target = target, subtype = subtype, reward = reward };
        static ObjectiveDef Side(string title, TaskKind kind, int target, int reward, int subtype = -1) => new ObjectiveDef { title = title, kind = kind, target = target, subtype = subtype, optional = true, reward = reward };
        static ChallengeDef C(string title, ChallengeKind kind, int reward, float seconds = 0) => new ChallengeDef { title = title, kind = kind, reward = reward, seconds = seconds };

        [MenuItem("Tiny Giant Problems/Missions/Rebuild default mission assets")]
        public static void Rebuild()
        {
            Directory.CreateDirectory(Folder);
            int grime = (int)MessKind.Grime, burr = (int)MessKind.Burr, mite = (int)MessKind.Mite;
            var list = new[]
            {
                Make("01-very-bad-itch", "A Very Bad Itch", 1, new string[0], 150, ItemId.None,
                    "Mossback is miserable. Finish the three care jobs on his back and get his comfort up before you extract.",
                    new[] { O("Collect the Bramblemites", TaskKind.CareJob, 1, 0, 40), O("Untangle the fur", TaskKind.CareJob, 1, 1, 40), O("Soothe the hot spot (Sprayer)", TaskKind.CareJob, 1, 2), O("Comfort at 70%", TaskKind.Comfort, 70),
                            Side("Scrub grime patches", TaskKind.ClearMess, 8, 35, grime), Side("Pick burrs from the fur", TaskKind.ClearMess, 6, 35, burr) },
                    new[] { C("Done in under 7 minutes", ChallengeKind.TimeLimit, 60, 420), C("Never winded by a fall", ChallengeKind.NoFallHurt, 40) }),
                Make("02-salvage-run", "Salvage Run", 1, new[] { "01-very-bad-itch" }, 140, ItemId.None,
                    "Mossback sheds moss, burrs and twigs everywhere. Gather salvage off the meadow and sell it inside the Guild House.",
                    new[] { O("Pick up salvage", TaskKind.CollectDebris, 15), O("Sell salvage at the Guild House", TaskKind.SellDebris, 12), O("Clear any mess", TaskKind.ClearMess, 6),
                            Side("Recover the lost survey drone", TaskKind.RecoverDrone, 1, 60), Side("Find a hidden trinket", TaskKind.FindTrinket, 1, 40) },
                    new[] { C("Never thrown off by a sneeze or shake", ChallengeKind.NoLaunch, 50), C("Done in under 6 minutes", ChallengeKind.TimeLimit, 50, 360) }),
                Make("03-mite-patrol", "Mite Patrol", 2, new[] { "02-salvage-run" }, 200, ItemId.Grabber,
                    "Bramblemites are breeding in his coat. Chase them down with the vacuum (they run!) and bottle them for the yard.",
                    new[] { O("Vacuum Bramblemites", TaskKind.ClearMess, 7, mite), O("Sell bottled mites", TaskKind.SellDebris, 5), O("Comfort at 60%", TaskKind.Comfort, 60),
                            Side("Scan the coat for stragglers", TaskKind.Scan, 1, 30), Side("Scrub grime patches", TaskKind.ClearMess, 6, 30, grime) },
                    new[] { C("Never stung (brace when they rear up!)", ChallengeKind.NoSting, 70), C("No safety-winch rescues", ChallengeKind.NoRescue, 40) }),
                Make("04-high-ground", "High Ground", 2, new[] { "03-mite-patrol" }, 220, ItemId.StaminaSpring,
                    "Set up a work base: buy a Floating Platform, put it on Mossback's back, kit it out, and prove you can climb his flank with the claw.",
                    new[] { O("Place a Floating Platform on the back", TaskKind.PlacePlatform, 1), O("Place a platform kit", TaskKind.PlaceKit, 1), O("Claw-climb 15 m up a flank", TaskKind.ClawClimb, 15),
                            Side("Clear mess around the base", TaskKind.ClearMess, 8, 40), Side("Glide for 5 seconds", TaskKind.Glide, 5, 30) },
                    new[] { C("Done in under 9 minutes", ChallengeKind.TimeLimit, 60, 540), C("Never winded by a fall", ChallengeKind.NoFallHurt, 50) }),
                Make("05-weather-the-storm", "Weather the Storm", 3, new[] { "04-high-ground" }, 260, ItemId.None,
                    "Mossback has hay fever. Sneezes and shakes come fast today. Hold on through them and keep working.",
                    new[] { O("Brace through sneezes and shakes on his back", TaskKind.BraceReaction, 4), O("Clear any mess", TaskKind.ClearMess, 12), O("Comfort at 65%", TaskKind.Comfort, 65),
                            Side("Feed Mossback a snack", TaskKind.CalmGiant, 1, 40), Side("Sell salvage", TaskKind.SellDebris, 10, 40) },
                    new[] { C("Never thrown off", ChallengeKind.NoLaunch, 80), C("Never winded by a fall", ChallengeKind.NoFallHurt, 50) }),
                Make("06-treasure-coat", "Treasure in the Coat", 3, new[] { "04-high-ground" }, 240, ItemId.None,
                    "Grime hides lost guild treasures. Scan, scrub and dig them out, then cash them in.",
                    new[] { O("Find hidden trinkets", TaskKind.FindTrinket, 2), O("Scrub grime patches", TaskKind.ClearMess, 10, grime), O("Sell salvage", TaskKind.SellDebris, 15),
                            Side("Scan for finds", TaskKind.Scan, 2, 30), Side("Recover the lost survey drone", TaskKind.RecoverDrone, 1, 60) },
                    new[] { C("Find every hidden trinket", ChallengeKind.AllTrinkets, 90), C("Done in under 8 minutes", ChallengeKind.TimeLimit, 60, 480) }),
                Make("07-guild-inspection", "Guild Inspection", 4, new[] { "05-weather-the-storm", "06-treasure-coat" }, 450, ItemId.None,
                    "The Guild Master is visiting. Mossback must be spotless and happy, everything at once, while he fidgets.",
                    new[] { O("Collect the Bramblemites", TaskKind.CareJob, 1, 0), O("Untangle the fur", TaskKind.CareJob, 1, 1), O("Soothe the hot spot", TaskKind.CareJob, 1, 2),
                            O("Clear any mess", TaskKind.ClearMess, 20), O("Sell salvage", TaskKind.SellDebris, 20), O("Comfort at 85%", TaskKind.Comfort, 85),
                            Side("Brace through reactions", TaskKind.BraceReaction, 3, 50), Side("Feed Mossback a snack", TaskKind.CalmGiant, 1, 40) },
                    new[] { C("Done in under 12 minutes", ChallengeKind.TimeLimit, 120, 720), C("Never thrown, stung or winded", ChallengeKind.NoLaunch, 100), C("No safety-winch rescues", ChallengeKind.NoRescue, 60) }),
            };
            var catalogPath = Folder + "/MissionCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<MissionCatalog>(catalogPath);
            if (!catalog) { catalog = ScriptableObject.CreateInstance<MissionCatalog>(); AssetDatabase.CreateAsset(catalog, catalogPath); }
            catalog.missions = list;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("Mission assets written: " + list.Length);
        }

        static MissionDef Make(string id, string title, int tier, string[] requires, int coins, ItemId item, string brief, ObjectiveDef[] objectives, ChallengeDef[] challenges)
        {
            string path = Folder + "/" + id + ".asset";
            var m = AssetDatabase.LoadAssetAtPath<MissionDef>(path);
            if (!m) { m = ScriptableObject.CreateInstance<MissionDef>(); AssetDatabase.CreateAsset(m, path); }
            m.id = id; m.title = title; m.tier = tier; m.requires = requires; m.rewardCoins = coins; m.rewardItem = item;
            m.brief = brief; m.objectives = objectives; m.challenges = challenges;
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
