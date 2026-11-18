using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TinyGiantProblems
{
    // Development only: drives a scripted run through the new loop and saves
    // screenshots plus a pass/fail report to TestResults/playtest. Started by
    // Tiny Giant Problems/Playtest Capture; never present in normal play.
    public sealed class PlaytestDriver : MonoBehaviour
    {
        public string Status = "Not started";
        public System.Action Finished;
        readonly List<string> checks = new List<string>();
        int errors;
        const string Folder = "TestResults/playtest";

        void Check(bool pass, string name) { checks.Add((pass ? "PASS " : "FAIL ") + name); if (!pass) Debug.LogWarning("Playtest check failed: " + name); }
        void OnLog(string message, string stack, LogType type) { if (type == LogType.Exception || type == LogType.Error) { errors++; checks.Add("ERROR " + message.Split('\n')[0]); } }

        public void Run() { StartCoroutine(Drive()); }

        IEnumerator Drive()
        {
            Status = "Running";
            Application.logMessageReceived += OnLog;
            Directory.CreateDirectory(Folder);
            var savedProgress = PlayerPrefs.GetString("TGP.Save.v1", null);
            yield return new WaitForSeconds(1.5f);
            var g = Expedition.Instance; var p = g.player;
            var econ = GuildEconomy.Instance; var mess = MessField.Instance;
            Check(econ && mess && DebrisSystem.Instance && HotbarUI.Instance && GuildShopUI.Instance, "Bootstrap installed every system");
            econ.ResetProgress();
            Capture("01-briefing");
            g.Begin(); g.nextReaction = 9999;
            p.automate = true;
            yield return new WaitForSeconds(1.2f);
            Check(mess.Total >= 25, $"Mess spawned on Mossback ({mess.Total} pieces)");
            Check(econ.Inventory.Has(ItemId.Brush) && econ.Inventory.Has(ItemId.Vacuum) && !econ.Owns(ItemId.Sprayer), "Starter kit is brush + vacuum; sprayer must be bought");
            Capture("02-harness-start");
            OverviewShot("03-mossback-back-overview");

            // Scrub a grime patch with the brush.
            var grime = Closest(mess, MessKind.Grime, p.transform.position);
            Check(grime, "A grime patch exists");
            if (grime)
            {
                Select(econ, ItemId.Brush);
                yield return StandAt(p, grime.transform.position);
                Capture("04-grime-closeup");
                p.UseOverride = true;
                yield return new WaitForSeconds(1.0f);
                Capture("05-brush-scrubbing");
                yield return new WaitForSeconds(1.5f);
                p.UseOverride = false;
                Check(grime == null || grime.Cleared, "Brushing clears grime");
                Check(mess.ClearedCount >= 1, "Cleared count advances");
                yield return new WaitForSeconds(1.5f);
                Check(econ.Inventory.Count(ItemId.MossClump) >= 1, "Moss clump debris collected by walking over it");
            }

            // Vacuum a mite.
            var mite = Closest(mess, MessKind.Mite, p.transform.position);
            if (mite)
            {
                Select(econ, ItemId.Vacuum);
                yield return StandAt(p, mite.transform.position);
                p.UseOverride = true;
                yield return new WaitForSeconds(.7f);
                Capture("06-vacuum-suction");
                yield return new WaitForSeconds(1.2f);
                p.UseOverride = false;
                Check(econ.Inventory.Count(ItemId.PestJar) >= 1, "Vacuumed mite becomes a bottled Bramblemite");
            }

            // Backpack and shop.
            HotbarUI.Instance.Open();
            yield return null;
            Capture("07-backpack");
            HotbarUI.Instance.Close();
            econ.Earn(1500, "Playtest grant");
            GuildShopUI.Instance.Open();
            yield return null; yield return null;
            Capture("08-tool-locker");
            foreach (var d in ItemCatalog.ShopOrder) econ.TryBuy(d);
            yield return null;
            Capture("09-tool-locker-bought");
            GuildShopUI.Instance.Close();
            Check(econ.Owns(ItemId.Sprayer) && econ.Inventory.Has(ItemId.Glider) && econ.Owns(ItemId.BigSack), "Shop purchases land in the bag and upgrades apply");
            Check(econ.Inventory.debrisStackLimit == 32, "Big sack upgrade raises stack size");

            // Each tool's use animation.
            foreach (var id in new[] { ItemId.Brush, ItemId.Vacuum, ItemId.Sprayer, ItemId.Grabber, ItemId.Scanner, ItemId.SnackLauncher, ItemId.RopeLauncher })
            {
                Select(econ, id);
                yield return new WaitForSeconds(.35f);
                p.UseOverride = true;
                yield return new WaitForSeconds(.4f);
                ToolShot("10-tool-" + id);
                p.UseOverride = false;
                yield return new WaitForSeconds(.2f);
            }

            // A shake knocks debris loose.
            int loose = DebrisPickup.All.Count;
            p.Warp(p.checkpoint.position);
            g.Warn();
            yield return new WaitForSeconds(4.3f);
            Check(DebrisPickup.All.Count > loose, "Shake sheds debris");
            yield return new WaitForSeconds(1.5f);
            WideShot("11-debris-on-meadow");

            // Glide down to the yard (no lift any more), sell, then winch back up.
            Select(econ, ItemId.Glider);
            WinchPost post = null;
            foreach (var s in Station.All) if (s is WinchPost w) post = w;
            Check(post, "Winch post exists in the yard");
            if (post)
            {
                p.Warp(post.transform.position + Vector3.up * 14 + Vector3.back * 6);
                yield return new WaitForSeconds(4f);
                Check(p.transform.position.y < g.creature.transform.position.y - 5, "Glider brings the technician down to the meadow");
                Capture("14-salvage-yard");
                // The sell counter is inside the Guild House: walk in through the open door.
                Check(GuildHouse.DoorOutside, "Guild House stands in the meadow");
                if (GuildHouse.DoorOutside)
                {
                    p.Warp(GuildHouse.DoorOutside.position + Vector3.up * .2f);
                    p.yaw = Quaternion.LookRotation(-GuildHouse.Exterior.forward).eulerAngles.y;
                    p.MoveOverride = new Vector3(0, 0, 1);
                    yield return new WaitForSeconds(2.2f);
                    p.MoveOverride = Vector3.zero;
                }
                Check(GuildHouse.PlayerInside, "Walking through the open door takes you inside the Guild House");
                SellStation sell = null;
                foreach (var s in Station.All) if (s is SellStation ss) sell = ss;
                if (sell)
                {
                    econ.Inventory.Add(ItemId.Twig, 5);
                    p.Warp(sell.transform.position + (p.transform.position - sell.transform.position).normalized * 2f);
                    yield return new WaitForSeconds(.4f);
                    Capture("15-sell-prompt");
                    int coins = econ.Coins;
                    sell.Interact();
                    yield return new WaitForSeconds(.5f);
                    Check(econ.Coins > coins && econ.Inventory.Count(ItemId.Twig) == 0, "Selling converts salvage to coins");
                    Capture("16-sold");
                }
                p.yaw = Quaternion.LookRotation(GuildHouse.Exterior.forward).eulerAngles.y;
                p.MoveOverride = new Vector3(0, 0, 1);
                yield return new WaitForSeconds(3f);
                p.MoveOverride = Vector3.zero;
                Check(!GuildHouse.PlayerInside, "Walking back out through the door returns you to the meadow");
                p.Warp(post.transform.position + Vector3.forward * 1.5f);
                yield return new WaitForSeconds(.3f);
                post.Interact();
                yield return new WaitForSeconds(3.2f);
                Check(p.transform.position.y > g.creature.transform.position.y + 20, "Winch post hauls the technician back onto Mossback");
            }

            p.automate = false; p.UseOverride = false;
            Check(errors == 0, $"No errors or exceptions logged ({errors})");
            if (savedProgress != null) PlayerPrefs.SetString("TGP.Save.v1", savedProgress); else PlayerPrefs.DeleteKey("TGP.Save.v1");
            PlayerPrefs.Save();
            Application.logMessageReceived -= OnLog;
            File.WriteAllLines(Folder + "/report.txt", checks);
            Status = checks.Exists(c => !c.StartsWith("PASS")) ? "FAILED" : "PASSED";
            Debug.Log("Playtest " + Status + "\n" + string.Join("\n", checks));
            Finished?.Invoke();
        }

        static MessSpot Closest(MessField f, MessKind kind, Vector3 from)
        {
            MessSpot best = null; float d = float.MaxValue;
            foreach (var s in f.spots) if (s && !s.Cleared && s.kind == kind) { var dd = (s.transform.position - from).sqrMagnitude; if (dd < d) { d = dd; best = s; } }
            return best;
        }

        static void Select(GuildEconomy e, ItemId id)
        {
            for (int i = 0; i < Inventory.Size; i++)
                if (e.Inventory.slots[i].id == id)
                {
                    if (i >= Inventory.HotbarSize) { e.Inventory.MoveOrSwap(i, 8); i = 8; }
                    e.Inventory.Select(i); return;
                }
        }

        IEnumerator StandAt(TechnicianMotor p, Vector3 target)
        {
            var frame = Expedition.Instance.creature.transform;
            var local = frame.InverseTransformPoint(target);
            // Stand on the deck beside the mess, facing it.
            var standLocal = new Vector3(Mathf.Clamp(local.x, -4.3f, 4.3f), 12.2f, local.z);
            var stand = frame.TransformPoint(standLocal);
            var dir = target - stand; dir.y = 0;
            if (dir.sqrMagnitude < .5f) { stand -= frame.forward * 1.6f; dir = target - stand; dir.y = 0; }
            else stand = target - dir.normalized * Mathf.Min(1.6f, dir.magnitude);
            stand.y = frame.TransformPoint(standLocal).y;
            // No harness deck any more: stand on Mossback's back itself.
            if (HarnessRemoval.Applied && HarnessRemoval.Surface(frame, stand, out var ground)) stand = ground.point;
            p.Warp(stand + Vector3.up * .3f);
            p.yaw = Quaternion.LookRotation(dir.sqrMagnitude > .01f ? dir : frame.forward).eulerAngles.y;
            p.pitch = 30;
            p.model.rotation = Quaternion.LookRotation(dir.sqrMagnitude > .01f ? dir.normalized : frame.forward);
            yield return new WaitForSeconds(.6f);
        }

        // ---- capture helpers: overlay canvases are rendered through the camera ----
        void Capture(string name) => Render(name, null, null);
        void OverviewShot(string name)
        {
            var f = Expedition.Instance.creature.transform;
            Render(name, f.TransformPoint(new Vector3(0, 26, -22)), f.TransformPoint(new Vector3(0, 11, 4)));
        }
        void WideShot(string name)
        {
            var f = Expedition.Instance.creature.transform;
            Render(name, f.TransformPoint(new Vector3(-26, 8, -22)), f.TransformPoint(new Vector3(-8, -3, 2)));
        }
        void ToolShot(string name)
        {
            var p = Expedition.Instance.player;
            var fwd = p.model.forward;
            Render(name, p.transform.position + fwd * 2.4f + p.model.right * 1.2f + Vector3.up * 1.6f, p.transform.position + Vector3.up * 1f + fwd * .4f);
        }

        void Render(string name, Vector3? eye, Vector3? lookAt)
        {
            var cam = Camera.main; if (!cam) return;
            var oldPos = cam.transform.position; var oldRot = cam.transform.rotation;
            if (eye.HasValue) { cam.transform.position = eye.Value; cam.transform.LookAt(lookAt.Value); }
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var modes = new List<(Canvas, RenderMode, Camera, float)>();
            foreach (var c in canvases)
            {
                if (!c.isRootCanvas || c.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                modes.Add((c, c.renderMode, c.worldCamera, c.planeDistance));
                c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = cam; c.planeDistance = cam.nearClipPlane + .05f + c.sortingOrder * .001f;
            }
            Canvas.ForceUpdateCanvases();
            var rt = new RenderTexture(1600, 900, 24);
            var prev = RenderTexture.active; var prevTarget = cam.targetTexture;
            cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
            File.WriteAllBytes($"{Folder}/{name}.png", tex.EncodeToPNG());
            RenderTexture.active = prev; cam.targetTexture = prevTarget;
            Destroy(tex); rt.Release(); Destroy(rt);
            foreach (var m in modes) { m.Item1.renderMode = m.Item2; m.Item1.worldCamera = m.Item3; m.Item1.planeDistance = m.Item4; }
            cam.transform.SetPositionAndRotation(oldPos, oldRot);
        }
    }
}
