using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TinyGiantProblems
{
    // Anything the technician can press INTERACT (E / X) on.
    public abstract class Station : MonoBehaviour
    {
        public static readonly List<Station> All = new List<Station>();
        public float radius = 3.2f;
        public abstract string Prompt { get; }
        public abstract void Interact();
        public virtual bool Available => true;
        protected virtual void OnEnable() => All.Add(this);
        protected virtual void OnDisable() => All.Remove(this);

        public static Station Nearest(Vector3 position)
        {
            Station best = null; float bestD = float.MaxValue;
            foreach (var s in All)
            {
                if (!s || !s.Available) continue;
                float d = Vector3.Distance(position, s.transform.position);
                if (d < s.radius && d < bestD) { bestD = d; best = s; }
            }
            return best;
        }
    }

    public sealed class SellStation : Station
    {
        ParticleSystem coins;
        Transform lid;
        float bounce;
        void Start()
        {
            coins = GameArt.Particles("Coin burst", transform, new Color(1, .8f, .3f), .25f, 6, 1.1f);
            var m = coins.main; m.gravityModifier = 1.4f;
        }
        public override string Prompt
        {
            get
            {
                var e = GuildEconomy.Instance; if (!e) return "";
                int pieces = 0, value = 0;
                foreach (var s in e.Inventory.slots)
                {
                    if (s.IsEmpty) continue; var d = ItemCatalog.Get(s.id);
                    if (d != null && d.IsDebris) { pieces += s.count; value += s.count * d.sellValue; }
                }
                return pieces == 0 ? "SALVAGE YARD · bring moss, burrs, twigs, mites and finds to sell"
                    : $"<color=#FFBC4A>E</color>  Sell {pieces} salvage for {UIKit.Money(value)} coins";
            }
        }
        public override void Interact()
        {
            var e = GuildEconomy.Instance; if (!e) return;
            int value = e.SellAll(out int pieces);
            if (pieces == 0) { ProceduralSfx.Play(ProceduralSfx.Deny, .5f); return; }
            GameArt.Burst(coins, transform.position + Vector3.up * 1.6f, Mathf.Clamp(pieces * 3, 10, 60), Vector3.up * 5, 3);
            bounce = 1;
            if (Expedition.Instance) Expedition.Instance.Caption = $"The yard keeper weighs your haul: {pieces} pieces, {value} coins.";
        }
        public void SetLid(Transform t) => lid = t;
        void Update()
        {
            bounce = Mathf.MoveTowards(bounce, 0, Time.deltaTime * 2.5f);
            if (lid) lid.localScale = Vector3.one * (1 + Mathf.Sin(bounce * Mathf.PI * 3) * .08f * bounce);
        }
    }

    public sealed class ToolLocker : Station
    {
        public override string Prompt => "<color=#FFBC4A>E</color>  Open the Guild Tool Locker";
        public override void Interact() { var shop = GuildShopUI.Instance; if (shop) shop.Open(); }
    }

    // Cable gondola between the harness deck and the meadow salvage yard.
    // Free winch post in the salvage yard: hauls the technician back up to the
    // start point on Mossback's back (the Guild Lift and its top stop are gone).
    // Getting down is up to you: jump, glide, climb down with the claw.
    public sealed class WinchPost : Station
    {
        public static bool Hauling { get; private set; }
        LineRenderer line;
        public override bool Available => !Hauling;
        public override string Prompt => "<color=#FFBC4A>E</color>  Winch up to Mossback's back (free)";

        public override void Interact()
        {
            var g = Expedition.Instance;
            if (!g || Hauling) return;
            StartCoroutine(Haul(g.player));
        }

        IEnumerator Haul(TechnicianMotor p)
        {
            Hauling = true;
            bool wasAuto = p.automate;
            p.automate = true; p.MoveOverride = Vector3.zero; p.UseOverride = false; p.BraceOverride = false; p.tethered = false;
            ProceduralSfx.Play(ProceduralSfx.Whoosh, .6f);
            var from = p.transform.position; var to = p.checkpoint.position + Vector3.up * .1f;
            if (!line) { line = new GameObject("Winch line").AddComponent<LineRenderer>(); line.startWidth = line.endWidth = .05f; line.sharedMaterial = GameArt.Colored(new Color(.3f, .22f, .14f)); }
            line.enabled = true; line.positionCount = 2;
            float t = 0, duration = 2.6f;
            while (t < 1)
            {
                t += Time.deltaTime / duration;
                float k = Mathf.SmoothStep(0, 1, t);
                var pos = Vector3.Lerp(from, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 6;
                p.Warp(pos);
                line.SetPosition(0, transform.position + Vector3.up * 2.4f); line.SetPosition(1, pos + Vector3.up * 1.4f);
                yield return null;
            }
            p.Warp(to); p.grace = 2; p.automate = wasAuto; line.enabled = false;
            Hauling = false;
            if (Expedition.Instance) Expedition.Instance.Caption = "Winched back up. Mind the itchy bits.";
        }
    }

    // Builds the stations at runtime so they work in the existing scene without
    // hand-editing it. Positions are relative to Mossback's creature frame.
    public static class WorldStations
    {
        public static Vector3 yardWorld = new Vector3(-42f, 0, -8f);

        public static void Build(Expedition game)
        {
            var frame = game.creature.transform;
            var root = new GameObject("Guild stations").transform;

            // Salvage yard on the meadow. (No tool locker or lift on Mossback's back.)
            var ground = GroundAt(yardWorld, frame);
            var yard = new GameObject("Salvage Yard").transform; yard.SetParent(root, false);
            yard.position = ground;
            yard.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(frame.position - ground, Vector3.up).normalized, Vector3.up);
            var toward = Vector3.ProjectOnPlane(frame.position - ground, Vector3.up).normalized;

            // The Guild House holds the locker, sell counter and wardrobe; the free
            // winch post stands just outside its front door.
            var houseGround = GroundAt(ground - toward * 10, frame);
            if (GuildHouse.Build(root, houseGround, toward))
            {
                var side = Vector3.Cross(Vector3.up, toward);
                var postSpot = GroundAt(GuildHouse.DoorOutside.position + toward * 3.5f + side * 5.5f, frame);
                BuildWinchPost(root, postSpot, toward);
                return;
            }

            var sell = new GameObject("Sell Station").AddComponent<SellStation>();
            sell.transform.SetParent(yard, false);
            sell.transform.localPosition = new Vector3(5, 0, 2);
            sell.radius = 3.6f;
            var bench = GameArt.Model("guild-workshop-furniture-kit/workbench", sell.transform, 2.6f);
            if (!bench) GameArt.Shape("Counter", PrimitiveType.Cube, sell.transform, Vector3.up * .5f, new Vector3(2.4f, 1, 1), new Color(.36f, .19f, .11f));
            var bin = GameArt.Model("chest/open-chest", sell.transform, 1.5f);
            if (bin) { bin.transform.localPosition = new Vector3(1.9f, 0, .3f); sell.SetLid(bin.transform); }
            GameArt.Shape("Yard ring", PrimitiveType.Cylinder, sell.transform, Vector3.up * .03f, new Vector3(5.5f, .02f, 5.5f), new Color(1, .73f, .29f), 0, 1.2f);
            Sign(sell.transform, "SALVAGE YARD · SELL HERE", new Vector3(0, 3.3f, 0), UIKit.Gold);
            var shelf = GameArt.Model("guild-workshop-furniture-kit/shelf", yard, 2.4f);
            if (shelf) shelf.transform.localPosition = new Vector3(8.5f, 0, 3.5f);

            var yardLocker = new GameObject("Yard Tool Locker").AddComponent<ToolLocker>();
            yardLocker.transform.SetParent(yard, false);
            yardLocker.transform.localPosition = new Vector3(-4.5f, 0, 2);
            yardLocker.radius = 3.2f;
            var chest = GameArt.Model("tool-case/tool-case", yardLocker.transform, 1.3f);
            if (!chest) GameArt.Shape("Locker", PrimitiveType.Cube, yardLocker.transform, Vector3.up * .6f, new Vector3(1.2f, 1.2f, .8f), new Color(.98f, .63f, .16f));
            Sign(yardLocker.transform, "TOOL LOCKER", new Vector3(0, 2.3f, 0), UIKit.Gold);

            BuildWinchPost(yard, yard.position, toward);
        }

        public static void MakeSign(Transform parent, string text, Vector3 offset, Color color, float scale = 1) { Sign(parent, text, offset, color); if (scale != 1) parent.GetChild(parent.childCount - 1).localScale *= scale; }

        // Winch post: the free way back up.
        static void BuildWinchPost(Transform parent, Vector3 at, Vector3 toward)
        {
            var post = new GameObject("Winch Post").AddComponent<WinchPost>();
            post.transform.SetParent(parent, false);
            post.transform.position = at;
            post.transform.rotation = Quaternion.LookRotation(toward);
            post.radius = 3f;
            var winch = GameArt.Model("rescue-winch/rescue-winch", post.transform, 1.6f);
            if (!winch) GameArt.Shape("Winch", PrimitiveType.Cylinder, post.transform, Vector3.up * .6f, new Vector3(.8f, .6f, .8f), new Color(.3f, .55f, .55f));
            GameArt.Shape("Winch pad", PrimitiveType.Cylinder, post.transform, Vector3.up * .05f, new Vector3(3, .1f, 3), new Color(.36f, .19f, .11f));
            Sign(post.transform, "WINCH UP · MOSSBACK", new Vector3(0, 2.8f, 0), UIKit.Cream);
        }

        static Vector3 GroundAt(Vector3 world, Transform creature)
        {
            var hits = Physics.RaycastAll(new Vector3(world.x, 200, world.z), Vector3.down, 400, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits) if (!h.collider.transform.IsChildOf(creature)) return h.point;
            return new Vector3(world.x, -5.5f, world.z);
        }

        static void Sign(Transform parent, string text, Vector3 offset, Color color)
        {
            var go = new GameObject("Sign", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = offset;
            var t = go.AddComponent<TextMeshPro>();
            t.font = UIKit.Font; t.text = text; t.fontSize = 6; t.color = color; t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.outlineWidth = .25f; t.outlineColor = new Color32(12, 26, 30, 255);
            ((RectTransform)go.transform).sizeDelta = new Vector2(20, 3);
            go.AddComponent<Billboard>();
        }
    }

    public sealed class Billboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main; if (!cam) return;
            var d = transform.position - cam.transform.position; d.y = 0;
            if (d.sqrMagnitude > .01f) transform.rotation = Quaternion.LookRotation(d);
        }
    }
}
