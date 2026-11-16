using System.Collections.Generic;
using UnityEngine;

namespace TinyGiantProblems
{
    // Shows the held item from the hotbar and animates it procedurally:
    // equip pop, walk sway, and a distinct use motion + effect per tool.
    // Appearance only: care rules and tool verbs stay in their own systems.
    [DefaultExecutionOrder(50)]
    public sealed class TechnicianToolVisuals : MonoBehaviour
    {
        public TechnicianMotor motor;
        public GameObject vacuum, brush, sprayer, ropeLauncher;
        public GameObject grabber, scanner, snackLauncher, glider;
        readonly Dictionary<ItemId, GameObject> tools = new Dictionary<ItemId, GameObject>();
        readonly Dictionary<GameObject, (Vector3 pos, Quaternion rot, Vector3 scale)> rest = new Dictionary<GameObject, (Vector3, Quaternion, Vector3)>();
        GameObject current, heldDebris;
        ItemId currentId = (ItemId)(-1);
        float equipT = 1, useBlend, cycle, kick;
        ParticleSystem mist, suction, sparks;
        ToolAbilities abilities;
        bool wasUsing;

        void Start()
        {
            if (!motor) motor = GetComponentInParent<TechnicianMotor>();
            abilities = motor ? motor.GetComponent<ToolAbilities>() : null;
            Register(ItemId.Vacuum, ref vacuum, Vector3.zero, .6f);
            Register(ItemId.Brush, ref brush, Vector3.zero, .4f);
            Register(ItemId.Sprayer, ref sprayer, Vector3.zero, .45f);
            Register(ItemId.RopeLauncher, ref ropeLauncher, new Vector3(0, 90, 0), .6f);
            Register(ItemId.Grabber, ref grabber, Vector3.zero, .7f);
            Register(ItemId.Scanner, ref scanner, Vector3.zero, .35f);
            Register(ItemId.SnackLauncher, ref snackLauncher, Vector3.zero, .6f);
            Register(ItemId.Glider, ref glider, Vector3.zero, 1.3f);
            foreach (var t in tools.Values) t.SetActive(false);
            mist = GameArt.Particles("Spray mist", null, new Color(.55f, .95f, .85f, .7f), .28f, 7, .55f);
            suction = GameArt.Particles("Suction swirl", null, new Color(.85f, .8f, .7f, .8f), .14f, 0, .35f);
            sparks = GameArt.Particles("Tool sparkle", null, new Color(1, .9f, .5f, 1), .12f, 2, .4f);
        }

        void Register(ItemId id, ref GameObject go, Vector3 euler, float size)
        {
            if (!go)
            {
                var def = ItemCatalog.Get(id);
                go = GameArt.Model(def.model, transform, size);
                if (!go) go = Placeholder(def, size);
                go.transform.localRotation = Quaternion.Euler(euler);
            }
            tools[id] = go;
            rest[go] = (go.transform.localPosition, go.transform.localRotation, go.transform.localScale);
        }

        GameObject Placeholder(ItemDef def, float size)
        {
            var root = new GameObject(def.name);
            root.transform.SetParent(transform, false);
            float s = size / Mathf.Max(.0001f, transform.lossyScale.x);
            GameArt.Shape("Grip", PrimitiveType.Cylinder, root.transform, Vector3.up * s * .2f, new Vector3(s * .15f, s * .2f, s * .15f), new Color(.2f, .15f, .1f));
            GameArt.Shape("Head", PrimitiveType.Cube, root.transform, new Vector3(0, s * .45f, s * .15f), new Vector3(s * .35f, s * .25f, s * .5f), def.color);
            return root;
        }

        ItemDef HeldDef()
        {
            var e = GuildEconomy.Instance;
            if (e) return e.Inventory.SelectedDef;
            return ItemCatalog.Get(ItemCatalog.ForCareTool(motor.selected));
        }

        // Kept for compatibility with older callers and tests.
        public void Show(CareTool tool) => Equip(ItemCatalog.Get(ItemCatalog.ForCareTool(tool)));

        void Equip(ItemDef def)
        {
            var id = def != null ? def.id : ItemId.None;
            if (id == currentId) return;
            currentId = id;
            if (current) { Restore(current); current.SetActive(false); }
            if (heldDebris) { Destroy(heldDebris); heldDebris = null; }
            current = null;
            if (def != null && def.IsTool && tools.TryGetValue(id, out var go)) { current = go; go.SetActive(true); }
            else if (def != null && def.IsDebris)
            {
                heldDebris = GameArt.ItemVisual(def, transform, def.size * .8f);
                heldDebris.transform.localPosition = new Vector3(0, .05f, .1f) / Mathf.Max(.0001f, transform.lossyScale.x);
                current = heldDebris;
                rest[heldDebris] = (heldDebris.transform.localPosition, heldDebris.transform.localRotation, heldDebris.transform.localScale);
            }
            equipT = 0;
        }

        void Restore(GameObject go)
        {
            if (rest.TryGetValue(go, out var r)) { go.transform.localPosition = r.pos; go.transform.localRotation = r.rot; go.transform.localScale = r.scale; }
        }

        static float EaseOutBack(float t) { const float c1 = 1.70158f, c3 = c1 + 1; t = Mathf.Clamp01(t); return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2); }

        void LateUpdate()
        {
            if (!motor) return;
            var def = HeldDef();
            Equip(def);
            if (!current || !rest.TryGetValue(current, out var r)) return;
            float dt = Time.deltaTime;
            bool using_ = motor.Using && motor.Active && !motor.braced && !motor.climbing;
            if (using_ && !wasUsing) kick = 1;
            wasUsing = using_;
            useBlend = Mathf.MoveTowards(useBlend, using_ ? 1 : 0, dt * 8);
            kick = Mathf.MoveTowards(kick, 0, dt * 4);
            equipT = Mathf.Min(1, equipT + dt / .25f);
            cycle += dt;
            float inv = 1 / Mathf.Max(.0001f, transform.lossyScale.x);
            var pos = r.pos; var rot = r.rot;
            // Walk sway and breathing idle
            float speed = motor.capsule ? new Vector2(motor.capsule.velocity.x, motor.capsule.velocity.z).magnitude : 0;
            float walk = Mathf.Clamp01(speed / 5);
            pos += new Vector3(Mathf.Sin(cycle * 9) * .03f * walk, Mathf.Abs(Mathf.Cos(cycle * 9)) * .04f * walk + Mathf.Sin(cycle * 1.8f) * .008f, 0) * inv;
            rot *= Quaternion.Euler(Mathf.Sin(cycle * 9) * 4 * walk, 0, Mathf.Sin(cycle * 4.5f) * 3 * walk);
            var target = Target();
            var nozzle = current.transform.position + motor.model.forward * .35f + Vector3.up * .1f;
            switch (currentId)
            {
                case ItemId.Brush:
                    // Scrub: quick forward-back strokes with a wrist roll.
                    pos += new Vector3(0, -.05f * useBlend, Mathf.Sin(cycle * 22) * .14f * useBlend + .12f * useBlend) * inv;
                    rot *= Quaternion.Euler(55 * useBlend + Mathf.Sin(cycle * 22) * 12 * useBlend, 0, Mathf.Cos(cycle * 22) * 10 * useBlend);
                    break;
                case ItemId.Vacuum:
                    // Suction: aim down at the target and buzz.
                    pos += (Random.insideUnitSphere * .012f * useBlend + new Vector3(0, -.04f, .1f) * useBlend) * inv;
                    rot *= Quaternion.Euler(30 * useBlend, 0, 0);
                    if (useBlend > .5f && target.HasValue && Random.value < .7f)
                    {
                        var from = target.Value + Random.insideUnitSphere * .35f;
                        GameArt.Burst(suction, from, 1, (nozzle - from) / .3f, .2f);
                    }
                    break;
                case ItemId.Sprayer:
                    // Pump squeezes with a recoil every quarter second.
                    float pump = Mathf.Max(0, Mathf.Sin(cycle * 25)) * useBlend;
                    pos += new Vector3(0, .03f * useBlend, -pump * .05f + .1f * useBlend) * inv;
                    rot *= Quaternion.Euler(15 * useBlend - pump * 6, 0, 0);
                    if (useBlend > .5f)
                    {
                        var dir = target.HasValue ? (target.Value - nozzle).normalized : motor.model.forward;
                        GameArt.Burst(mist, nozzle, 2, dir * 6 + Vector3.up * .5f, 1.2f);
                    }
                    break;
                case ItemId.RopeLauncher:
                    // Recoil on fire, then settle.
                    pos += new Vector3(0, kick * .05f, -kick * .18f) * inv;
                    rot *= Quaternion.Euler(-kick * 35, 0, 0);
                    break;
                case ItemId.Grabber:
                    bool grabbing = abilities && abilities.Grabbing;
                    float claw = grabbing ? Mathf.Abs(Mathf.Sin(cycle * 14)) : 0;
                    pos += new Vector3(0, .04f * useBlend, .2f * useBlend) * inv;
                    rot *= Quaternion.Euler(-10 * useBlend, 0, claw * 10);
                    current.transform.localScale = r.scale * (1 + claw * .08f);
                    break;
                case ItemId.Scanner:
                    float since = abilities ? Time.time - abilities.LastUse : 10;
                    float raise = Mathf.Clamp01(1 - since / 1.2f);
                    pos += new Vector3(-.15f * raise, .45f * raise, .1f * raise) * inv;
                    rot *= Quaternion.Euler(-20 * raise, cycle * 720 * raise, 0);
                    if (raise > .5f && Random.value < .5f) GameArt.Burst(sparks, current.transform.position, 1, Random.onUnitSphere * 3, .5f);
                    break;
                case ItemId.SnackLauncher:
                    float lob = abilities ? Mathf.Clamp01(1 - (Time.time - abilities.LastUse) / .6f) : 0;
                    pos += new Vector3(0, lob * .1f, -lob * .15f) * inv;
                    rot *= Quaternion.Euler(-lob * 40, 0, 0);
                    break;
                case ItemId.Glider:
                    bool gliding = abilities && abilities.Gliding;
                    float g = gliding ? 1 : 0;
                    pos += new Vector3(-.4f * g, 1.0f * g, -.2f * g) * inv;
                    rot *= Quaternion.Euler(-80 * g, 0, Mathf.Sin(cycle * 3) * 6 * g);
                    break;
                default:
                    // Held salvage bobs in the palm.
                    pos += new Vector3(0, Mathf.Sin(cycle * 3) * .02f, 0) * inv;
                    rot *= Quaternion.Euler(0, cycle * 40, 0);
                    break;
            }
            current.transform.localPosition = pos;
            current.transform.localRotation = rot;
            if (currentId != ItemId.Grabber) current.transform.localScale = r.scale * EaseOutBack(equipT);
            else current.transform.localScale *= EaseOutBack(equipT);
        }

        Vector3? Target()
        {
            var g = Expedition.Instance;
            if (g && g.Nearest) return g.Nearest.transform.position + Vector3.up * .5f;
            var m = MessField.Instance;
            if (m && m.Current) return m.Current.transform.position;
            if (m && m.Aimed) return m.Aimed.transform.position;
            return null;
        }
    }
}
