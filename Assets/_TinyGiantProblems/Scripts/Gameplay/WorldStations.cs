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
    public sealed class GuildLift : Station
    {
        public Transform top, bottom;
        public bool atTop;
        public GuildLift partner;
        public static bool Riding { get; private set; }
        LineRenderer cable;
        Transform car;
        public override bool Available => !Riding;
        public override string Prompt => atTop ? "<color=#FFBC4A>E</color>  Ride the Guild Lift down to the salvage yard" : "<color=#FFBC4A>E</color>  Ride the Guild Lift up to Mossback's harness";

        public void Setup(Transform topPoint, Transform bottomPoint, bool isTop, LineRenderer sharedCable, Transform gondola)
        { top = topPoint; bottom = bottomPoint; atTop = isTop; cable = sharedCable; car = gondola; }

        public override void Interact()
        {
            var g = Expedition.Instance;
            if (!g || Riding) return;
            StartCoroutine(Ride(g.player, atTop));
        }

        IEnumerator Ride(TechnicianMotor p, bool down)
        {
            Riding = true;
            p.automate = true; p.MoveOverride = Vector3.zero; p.UseOverride = false; p.BraceOverride = false;
            p.tethered = false;
            ProceduralSfx.Play(ProceduralSfx.Whoosh, .5f);
            float duration = 4.2f, t = 0;
            while (t < 1)
            {
                t += Time.deltaTime / duration;
                float k = Mathf.SmoothStep(0, 1, t);
                var pos = PathPoint(down ? k : 1 - k);
                p.Warp(pos);
                if (car) car.position = pos + Vector3.up * .05f;
                yield return null;
            }
            p.Warp((down ? bottom : top).position + Vector3.up * .1f);
            p.grace = 2;
            p.automate = false;
            Riding = false;
            if (Expedition.Instance) Expedition.Instance.Caption = down ? "Salvage yard. Sell what you carry, then ride back up." : "Back on the harness. Mind the itchy bits.";
        }

        // Curved route that swings wide of Mossback's flank.
        public Vector3 PathPoint(float k)
        {
            var a = top.position; var b = bottom.position;
            var g = Expedition.Instance;
            var center = g && g.creature ? g.creature.transform.position : a;
            var outward = Vector3.ProjectOnPlane(b - center, Vector3.up).normalized;
            var control = (a + b) * .5f + outward * 16 + Vector3.up * 6;
            float u = 1 - k;
            return u * u * a + 2 * u * k * control + k * k * b;
        }

        void LateUpdate()
        {
            if (!atTop || !cable || !top || !bottom) return;
            if (cable.positionCount != 20) cable.positionCount = 20;
            for (int i = 0; i < 20; i++) cable.SetPosition(i, PathPoint(i / 19f) + Vector3.up * 2.2f);
            if (car && !Riding) car.position = top.position + Vector3.up * .05f;
        }
    }

    // Builds the stations at runtime so they work in the existing scene without
    // hand-editing it. Positions are relative to Mossback's creature frame.
    public static class WorldStations
    {
        public static Vector3 liftTopLocal = new Vector3(-3.1f, 12.12f, -15f);
        public static Vector3 lockerLocal = new Vector3(-2.7f, 12.12f, -13f);
        public static Vector3 yardWorld = new Vector3(-42f, 0, -8f);

        public static void Build(Expedition game)
        {
            var frame = game.creature.transform;
            var root = new GameObject("Guild stations").transform;

            // Tool locker on the harness, at the existing supply chest.
            var locker = new GameObject("Guild Tool Locker").AddComponent<ToolLocker>();
            locker.transform.SetParent(frame, false);
            locker.transform.localPosition = lockerLocal;
            locker.transform.localScale = Vector3.one / frame.lossyScale.x;
            locker.radius = 3.4f;
            Sign(locker.transform, "TOOL LOCKER", new Vector3(0, 2.4f, 0), UIKit.Gold);

            // Salvage yard on the meadow, reached by the lift.
            var ground = GroundAt(yardWorld, frame);
            var yard = new GameObject("Salvage Yard").transform; yard.SetParent(root, false);
            yard.position = ground;
            yard.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(frame.position - ground, Vector3.up).normalized, Vector3.up);

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

            // The lift: a top platform on the harness and a bottom pad in the yard.
            var topPoint = new GameObject("Lift top").transform; topPoint.SetParent(frame, false); topPoint.localPosition = liftTopLocal;
            var bottomPoint = new GameObject("Lift bottom").transform; bottomPoint.SetParent(yard, false); bottomPoint.localPosition = Vector3.zero;
            var cableGo = new GameObject("Lift cable"); cableGo.transform.SetParent(root, false);
            var cable = cableGo.AddComponent<LineRenderer>();
            cable.positionCount = 2; cable.startWidth = cable.endWidth = .07f;
            cable.sharedMaterial = GameArt.Colored(new Color(.3f, .22f, .14f));
            var car = new GameObject("Lift gondola").transform; car.SetParent(root, false);
            GameArt.Shape("Gondola floor", PrimitiveType.Cylinder, car, Vector3.zero, new Vector3(1.6f, .06f, 1.6f), new Color(.36f, .19f, .11f));
            GameArt.Shape("Gondola rope", PrimitiveType.Cylinder, car, Vector3.up * 1.1f, new Vector3(.05f, 1.1f, .05f), new Color(.3f, .22f, .14f));
            GameArt.Shape("Gondola hook", PrimitiveType.Sphere, car, Vector3.up * 2.2f, Vector3.one * .3f, new Color(1, .73f, .29f), .8f);

            var liftTop = new GameObject("Guild Lift (top)").AddComponent<GuildLift>();
            liftTop.transform.SetParent(topPoint, false);
            liftTop.transform.localScale = Vector3.one / frame.lossyScale.x;
            liftTop.radius = 2.6f;
            var winch = GameArt.Model("rescue-winch/rescue-winch", liftTop.transform, 1.6f);
            if (winch) winch.transform.localPosition = new Vector3(-1.4f, 0, 0);
            Sign(liftTop.transform, "LIFT DOWN · SALVAGE YARD", new Vector3(0, 2.6f, 0), UIKit.Cream);

            var liftBottom = new GameObject("Guild Lift (bottom)").AddComponent<GuildLift>();
            liftBottom.transform.SetParent(bottomPoint, false);
            liftBottom.radius = 2.8f;
            GameArt.Shape("Lift pad", PrimitiveType.Cylinder, liftBottom.transform, Vector3.up * .05f, new Vector3(3, .1f, 3), new Color(.36f, .19f, .11f));
            Sign(liftBottom.transform, "LIFT UP · MOSSBACK", new Vector3(0, 2.8f, 0), UIKit.Cream);

            liftTop.Setup(topPoint, bottomPoint, true, cable, car);
            liftBottom.Setup(topPoint, bottomPoint, false, null, null);
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
