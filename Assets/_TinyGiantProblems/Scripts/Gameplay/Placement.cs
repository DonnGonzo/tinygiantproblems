using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TinyGiantProblems
{
    // Places hotbar placeables into the world. The Floating Platform snaps onto
    // Mossback's back (parented to the creature frame, so it rides every step and
    // shake); platform kits snap onto a placed platform's deck. A translucent ghost
    // shows where it will go (green = valid, red = not). USE places, G picks up.
    // Placements persist in PlayerPrefs alongside the guild save.
    [DefaultExecutionOrder(30)]
    public sealed class PlacementSystem : MonoBehaviour
    {
        public const string SaveKey = "TGP.Placements.v1";
        public static PlacementSystem Instance { get; private set; }
        public TechnicianMotor motor;
        public float reach = 9;
        public readonly List<PlacedObject> placed = new List<PlacedObject>();
        public string Hint { get; private set; } = "";

        GameObject ghost; ItemId ghostId = ItemId.None;
        Material ghostOk, ghostBad;
        bool valid; Transform targetParent; Vector3 targetPos; Quaternion targetRot;
        bool loaded;

        [Serializable] sealed class Entry { public int item, parent; public Vector3 pos; public Quaternion rot; }
        [Serializable] sealed class Data { public List<Entry> entries = new List<Entry>(); }

        public static void ClearSaved()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            if (Instance) { foreach (var p in Instance.placed.ToArray()) if (p) Destroy(p.gameObject); Instance.placed.Clear(); }
        }

        void Awake() { Instance = this; if (!motor) motor = GetComponent<TechnicianMotor>(); }

        void Start()
        {
            ghostOk = Ghost(new Color(.4f, 1f, .55f, .38f));
            ghostBad = Ghost(new Color(1f, .35f, .3f, .38f));
        }

        static Material Ghost(Color c)
        {
            var m = new Material(GameArt.Colored(c, 0, .4f)) { name = "Placement ghost" };
            m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            m.color = c;
            return m;
        }

        public static float WorldSize(ItemId id)
        {
            switch (id)
            {
                case ItemId.Platform: return 6.5f;
                case ItemId.SupplyCrate: return 1.1f;
                case ItemId.StaminaSpring: return .8f;
                case ItemId.RespawnBeacon: return 1.5f;
                case ItemId.ToolRack: return .9f;
                default: return 1;
            }
        }

        void Update()
        {
            if (!loaded && Expedition.Instance && Expedition.Instance.creature) { Load(); loaded = true; }
            var economy = GuildEconomy.Instance;
            var def = economy ? economy.Inventory.SelectedDef : null;
            bool active = motor && motor.Active && !motor.tumbling && !GameUI.CapturesInput && def != null && def.placeable;
            if (!active) { ShowGhost(ItemId.None); Hint = ""; }
            else
            {
                Aim(def);
                ShowGhost(def.id);
                if (ghost)
                {
                    ghost.transform.SetPositionAndRotation(targetPos, targetRot);
                    foreach (var r in ghost.GetComponentsInChildren<Renderer>()) r.sharedMaterial = valid ? ghostOk : ghostBad;
                }
                if (valid && motor.UsePressedThisFrame) PlaceSelected(def);
            }
            if (motor && motor.Active && !GameUI.CapturesInput && GameInput.PickUp.WasPressedThisFrame()) PickUpNearest();
        }

        // ---- aiming ----
        void Aim(ItemDef def)
        {
            valid = false; targetParent = null;
            var game = Expedition.Instance; var frame = game.creature.transform;
            var cam = motor.orbitCamera ? motor.orbitCamera.transform : null;
            var origin = cam ? cam.position : transform.position + Vector3.up * 1.6f;
            var dir = cam ? cam.forward : motor.model.forward;
            float reachFromCam = reach + (cam ? Vector3.Distance(cam.position, transform.position) : 0);
            var hits = Physics.RaycastAll(origin, dir, reachFromCam, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            RaycastHit hit = default; bool found = false;
            foreach (var h in hits)
            {
                if (h.collider.transform.IsChildOf(transform) || ghost && h.collider.transform.IsChildOf(ghost.transform)) continue;
                hit = h; found = true; break;
            }
            // Fallback: the spot just ahead of the technician's feet.
            if (!found)
            {
                var ahead = transform.position + motor.model.forward * (def.needsPlatform ? 1.6f : 4.5f) + Vector3.up * 3;
                found = Physics.Raycast(ahead, Vector3.down, out hit, 10, ~0, QueryTriggerInteraction.Ignore) && !hit.collider.transform.IsChildOf(transform);
            }
            targetPos = found ? hit.point : transform.position + motor.model.forward * 3;
            var yaw = Quaternion.Euler(0, motor.model.eulerAngles.y, 0);
            targetRot = yaw;
            if (!found) { Hint = "Aim at Mossback's back to place it"; return; }
            float fromPlayer = Vector3.Distance(transform.position, hit.point);

            if (def.needsPlatform)
            {
                var platform = hit.collider.GetComponentInParent<PlacedPlatform>();
                if (!platform) { Hint = $"{def.name}: aim at your Floating Platform's deck"; return; }
                targetParent = platform.transform;
                targetRot = Quaternion.LookRotation(Vector3.ProjectOnPlane(yaw * Vector3.forward, platform.transform.up), platform.transform.up);
                bool flat = hit.normal.y > .8f;
                bool clear = true;
                foreach (var p in placed) if (p && p != platform && (p.transform.position - hit.point).sqrMagnitude < 1.1f * 1.1f) clear = false;
                valid = flat && clear && fromPlayer < 6;
                Hint = valid ? $"USE to place the {def.name} · G picks things back up" : !flat ? "Too steep: aim at the flat deck" : !clear ? "Something is already there" : "Move closer";
                return;
            }
            // Floating Platform: on the creature's back, never on props or other platforms.
            bool onCreature = hit.collider.transform.IsChildOf(frame) && !hit.collider.GetComponentInParent<PlacedObject>() && !hit.collider.GetComponentInParent<PropCollisionPolicy>() && !hit.collider.GetComponentInParent<CareJob>();
            var up = Vector3.Slerp(Vector3.up, hit.normal, .35f);
            targetRot = Quaternion.LookRotation(Vector3.ProjectOnPlane(yaw * Vector3.forward, up), up);
            targetParent = frame;
            bool gentle = hit.normal.y > .7f;
            bool apart = true;
            foreach (var p in placed) if (p is PlacedPlatform && (p.transform.position - hit.point).sqrMagnitude < 7 * 7) apart = false;
            valid = onCreature && gentle && apart && fromPlayer < reach;
            targetPos = hit.point + up * (PlacedPlatform.Hover - PlacedPlatform.DeckAbovePivot(WorldSize(ItemId.Platform)));
            Hint = valid ? "USE to set the Floating Platform down · G picks it back up"
                : !onCreature ? "Floating Platform: aim at Mossback's back" : !gentle ? "Too steep here: find a flatter part of the back"
                : !apart ? "Too close to another platform" : "Move closer";
        }

        void ShowGhost(ItemId id)
        {
            if (id == ghostId) return;
            if (ghost) Destroy(ghost);
            ghost = null; ghostId = id;
            if (id == ItemId.None) return;
            var def = ItemCatalog.Get(id);
            ghost = GameArt.Model(def.model, null, WorldSize(id));
            if (!ghost) ghost = GameArt.ItemVisual(def, null, WorldSize(id));
            ghost.name = "Placement ghost";
            foreach (var r in ghost.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // ---- place / pick up ----
        void PlaceSelected(ItemDef def)
        {
            var e = GuildEconomy.Instance;
            var taken = e.Inventory.Take(e.Inventory.selected, 1);
            if (taken.IsEmpty) return;
            var obj = Spawn(def.id, targetParent, targetPos, targetRot);
            if (!obj) { e.Inventory.Add(def.id, 1); return; }
            ProceduralSfx.Play(ProceduralSfx.Pop, .8f);
            if (def.id == ItemId.Platform) e.Report(TaskKind.PlacePlatform); else e.Report(TaskKind.PlaceKit, 1, (int)def.id);
            if (Expedition.Instance) Expedition.Instance.Caption = def.id == ItemId.Platform ? "Floating Platform set down. Stand on it, then build platform kits on its deck." : def.name + " placed.";
            Save();
        }

        public PlacedObject Spawn(ItemId id, Transform parent, Vector3 worldPos, Quaternion worldRot)
        {
            var def = ItemCatalog.Get(id);
            var lib = ItemLibrary.Instance;
            var prefab = lib ? lib.Prefab(def.model) : null;
            GameObject go;
            if (prefab) go = Instantiate(prefab);
            else go = GameArt.ItemVisual(def, null, 1);
            go.name = def.name;
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(worldPos, worldRot);
            float parentScale = parent ? Mathf.Max(.0001f, parent.lossyScale.x) : 1;
            go.transform.localScale = Vector3.one * WorldSize(id) / parentScale;
            PlacedObject p;
            switch (id)
            {
                case ItemId.Platform: p = go.AddComponent<PlacedPlatform>(); break;
                case ItemId.SupplyCrate: p = go.AddComponent<SupplyCrate>(); break;
                case ItemId.StaminaSpring: p = go.AddComponent<StaminaSpring>(); break;
                case ItemId.RespawnBeacon: p = go.AddComponent<RespawnBeacon>(); break;
                case ItemId.ToolRack: p = go.AddComponent<ToolRack>(); break;
                default: p = go.AddComponent<PlacedObject>(); break;
            }
            p.item = id;
            if (p is PlacedPlatform platform) platform.Settle();
            placed.Add(p);
            return p;
        }

        void PickUpNearest()
        {
            PlacedObject best = null; float bestD = 3.2f;
            foreach (var p in placed)
            {
                if (!p) continue;
                float d = Vector3.Distance(transform.position, p.InteractPoint);
                if (p is PlacedPlatform) d += .8f; // kits on the deck win ties
                if (d < bestD) { bestD = d; best = p; }
            }
            if (best) PickUp(best);
        }

        public bool PickUp(PlacedObject p)
        {
            var e = GuildEconomy.Instance;
            var items = new List<PlacedObject> { p };
            if (p is PlacedPlatform) foreach (var q in placed) if (q && q != p && q.transform.IsChildOf(p.transform)) items.Add(q);
            int room = 0; foreach (var q in items) room += e.Inventory.RoomFor(q.item) > 0 ? 1 : 0;
            if (room < items.Count)
            {
                if (Expedition.Instance) Expedition.Instance.Caption = "No room in the bag to pick that up. Sell or drop something first.";
                ProceduralSfx.Play(ProceduralSfx.Deny, .7f);
                return false;
            }
            foreach (var q in items) { e.Inventory.Add(q.item, 1); placed.Remove(q); q.OnPickedUp(); }
            Destroy(p.gameObject);
            ProceduralSfx.Play(ProceduralSfx.Pickup, .8f);
            if (Expedition.Instance) Expedition.Instance.Caption = items.Count > 1 ? $"Packed up the platform and {items.Count - 1} kit(s)." : ItemCatalog.Get(p.item).name + " packed back into your bag.";
            Save();
            return true;
        }

        // ---- persistence ----
        public void Save()
        {
            var data = new Data();
            var platforms = new List<PlacedObject>();
            foreach (var p in placed) if (p is PlacedPlatform) platforms.Add(p);
            foreach (var p in placed)
            {
                if (!p) continue;
                int parent = -1;
                for (int i = 0; i < platforms.Count; i++) if (p != platforms[i] && p.transform.IsChildOf(platforms[i].transform)) parent = i;
                var basis = parent < 0 ? Expedition.Instance.creature.transform : platforms[parent].transform;
                data.entries.Add(new Entry { item = (int)p.item, parent = parent < 0 ? -1 : parent, pos = basis.InverseTransformPoint(p.transform.position), rot = Quaternion.Inverse(basis.rotation) * p.transform.rotation });
            }
            // Platforms first so kits can find their parent on load.
            data.entries.Sort((a, b) => (a.parent >= 0 ? 1 : 0).CompareTo(b.parent >= 0 ? 1 : 0));
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        void Load()
        {
            var json = PlayerPrefs.GetString(SaveKey, "");
            if (string.IsNullOrEmpty(json)) return;
            Data data;
            try { data = JsonUtility.FromJson<Data>(json); } catch { return; }
            var frame = Expedition.Instance.creature.transform;
            var platforms = new List<PlacedObject>();
            foreach (var en in data.entries)
            {
                Transform basis = frame;
                if (en.parent >= 0) { if (en.parent >= platforms.Count) continue; basis = platforms[en.parent].transform; }
                var p = Spawn((ItemId)en.item, basis, basis.TransformPoint(en.pos), basis.rotation * en.rot);
                if (p is PlacedPlatform) platforms.Add(p);
            }
        }
    }

    // Base for anything placed from the hotbar. E uses it (kits), G packs it up.
    public class PlacedObject : Station
    {
        public ItemId item;
        public virtual Vector3 InteractPoint => transform.position;
        public override string Prompt => $"{ItemCatalog.Get(item).name}  ·  <color=#FFBC4A>G</color>  pick up";
        public override void Interact() { }
        public virtual void OnPickedUp() { }
        protected virtual void Awake() { radius = 2.6f; }
    }

    public sealed class PlacedPlatform : PlacedObject
    {
        // Deck height above the back, in metres: low enough to hop on.
        public const float Hover = 1.0f;
        Vector3 deckLocal;
        public override Vector3 InteractPoint => transform.TransformPoint(deckLocal);
        public override string Prompt => "Floating Platform  ·  <color=#FFBC4A>G</color>  pack it up (and its kits)";
        protected override void Awake() { radius = 3.4f; }

        // The model's pivot is under its hanging crystals: measure once how far
        // the deck sits above it (per metre of size) so ghost and placement put
        // the deck, not the crystals, Hover metres above the back.
        static float deckPerSize = -1;
        public static float DeckAbovePivot(float size)
        {
            if (deckPerSize < 0)
            {
                deckPerSize = .55f;
                var lib = ItemLibrary.Instance; var prefab = lib ? lib.Prefab(ItemCatalog.Get(ItemId.Platform).model) : null;
                if (prefab)
                {
                    var probe = Instantiate(prefab, new Vector3(0, -3000, 0), Quaternion.identity);
                    Physics.SyncTransforms();
                    var center = probe.GetComponentInChildren<Renderer>().bounds.center;
                    var hits = Physics.RaycastAll(center + Vector3.up * 5, Vector3.down, 10, ~0, QueryTriggerInteraction.Ignore);
                    Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                    foreach (var h in hits) if (h.collider.transform.IsChildOf(probe.transform)) { deckPerSize = h.point.y - probe.transform.position.y; break; }
                    DestroyImmediate(probe);
                }
            }
            return deckPerSize * size;
        }

        public void Settle()
        {
            float size = transform.lossyScale.x;
            deckLocal = transform.InverseTransformPoint(transform.position + transform.up * DeckAbovePivot(size));
        }
    }

    public sealed class SupplyCrate : PlacedObject
    {
        public const float Rate = .75f;
        public override string Prompt
        {
            get
            {
                var e = GuildEconomy.Instance; int pieces = 0, value = 0;
                if (e) foreach (var s in e.Inventory.slots) { if (s.IsEmpty) continue; var d = ItemCatalog.Get(s.id); if (d != null && d.IsDebris) { pieces += s.count; value += s.count * d.sellValue; } }
                return pieces == 0 ? "Supply Crate · salvage dropped here sells at 75%  ·  <color=#FFBC4A>G</color>  pick up"
                    : $"<color=#FFBC4A>E</color>  Ship {pieces} salvage for {UIKit.Money(Mathf.RoundToInt(value * Rate))}  ·  <color=#FFBC4A>G</color>  pick up";
            }
        }
        public override void Interact()
        {
            var e = GuildEconomy.Instance; if (!e) return;
            int value = e.SellAll(out int pieces, Rate);
            if (pieces == 0) { ProceduralSfx.Play(ProceduralSfx.Deny, .5f); return; }
            if (Expedition.Instance) Expedition.Instance.Caption = $"Crate packed and flagged for the guild runner: {pieces} pieces, {value} coins.";
        }
    }

    public sealed class StaminaSpring : PlacedObject
    {
        public const float Range = 3f;
        ParticleSystem mist;
        public override string Prompt => "Stamina Spring · stand close to refill claw stamina and grip  ·  <color=#FFBC4A>G</color>  pick up";
        void Start() { mist = GameArt.Particles("Spring mist", transform, new Color(.55f, 1f, .85f, .7f), .2f, 1.5f, 1.2f); }
        void Update()
        {
            var g = Expedition.Instance; if (!g) return;
            var p = g.player;
            if (UnityEngine.Random.value < .2f) GameArt.Burst(mist, transform.position + Vector3.up * .6f, 1, Vector3.up * 1.2f, .4f);
            if (Vector3.Distance(p.transform.position, transform.position) > Range) return;
            var claw = ClimbingClaw.For(p);
            if (claw) claw.stamina = Mathf.Min(claw.maxStamina, claw.stamina + 60 * Time.deltaTime);
            p.grip = Mathf.Min(100, p.grip + 50 * Time.deltaTime);
        }
    }

    public sealed class RespawnBeacon : PlacedObject
    {
        Transform point, previous;
        public override string Prompt => "Respawn Beacon · the safety winch brings you back here  ·  <color=#FFBC4A>G</color>  pick up";
        void Start()
        {
            var g = Expedition.Instance; if (!g) return;
            point = new GameObject("Beacon respawn point").transform;
            point.SetParent(transform, false);
            point.position = transform.position + transform.forward * 1.2f + Vector3.up * .2f;
            previous = g.player.checkpoint;
            g.player.checkpoint = point;
        }
        public override void OnPickedUp()
        {
            var g = Expedition.Instance;
            if (g && g.player.checkpoint == point && previous) g.player.checkpoint = previous;
        }
    }

    public sealed class ToolRack : PlacedObject
    {
        public override string Prompt => "<color=#FFBC4A>E</color>  Open the Guild Tool Locker  ·  <color=#FFBC4A>G</color>  pick up";
        public override void Interact() { var shop = GuildShopUI.Instance; if (shop) shop.Open(); }
    }
}
