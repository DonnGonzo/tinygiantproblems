using UnityEngine;
using UnityEngine.UI;

namespace TinyGiantProblems
{
    // The Climbing Claw: while it is the selected hotbar item, walking into any
    // steep part of Mossback (flanks, legs, tail, the coat) digs the claw in and
    // the technician climbs. Its own stamina drains while hanging and refills on
    // the ground; at zero the claw slips and you fall until you land again.
    // TechnicianMotor asks TryGrip() each frame before its own climbing check.
    [DefaultExecutionOrder(19)]
    public sealed class ClimbingClaw : MonoBehaviour
    {
        public TechnicianMotor motor;
        public float stamina = 100, maxStamina = 100;
        public float drainMoving = 8f, drainHanging = 4f, refill = 30, reach = 1.15f;
        public bool Active { get; private set; }
        public bool Exhausted { get; private set; }
        public bool Equipped => GuildEconomy.Instance && GuildEconomy.Instance.Inventory.SelectedDef?.id == ItemId.Claw;
        public Vector3 SurfaceNormal { get; private set; }
        public float LastSlip { get; private set; } = -10;

        Image fill, frame; RectTransform bar; CanvasGroup group;
        float shown;

        public static ClimbingClaw For(TechnicianMotor m) => m ? m.GetComponent<ClimbingClaw>() : null;
        public static bool IsClimbing(TechnicianMotor m) { var c = For(m); return c && c.Active; }

        void Awake() { if (!motor) motor = GetComponent<TechnicianMotor>(); }

        // Called by TechnicianMotor. Returns true and the surface hit when the
        // claw holds this frame.
        public bool TryGrip(Vector3 pushDirection, Vector2 input, float dt, out RaycastHit hit)
        {
            hit = default;
            bool was = Active;
            Active = false;
            if (!Equipped || Exhausted || stamina <= 0 || motor.stunned > 0 || motor.tumbling && Time.time - motor.launchedAt < .8f) return false;
            var game = Expedition.Instance;
            if (!game || !game.creature) return false;
            // Stay on while already climbing; otherwise the player must push into the surface.
            bool pushing = input.sqrMagnitude > .04f || was;
            if (!pushing) return false;
            var dir = was && SurfaceNormal != Vector3.zero ? -SurfaceNormal : pushDirection;
            if (!Probe(dir, out hit) && !(was && Probe(pushDirection, out hit))) return false;
            if (!hit.collider.transform.IsChildOf(game.creature.transform)) return false;
            if (hit.normal.y > .62f) return false; // Walkable: just walk.
            if (hit.collider.GetComponentInParent<ClimbableZone>() && !was && motor.grounded && input.y <= 0) return false;
            // Under a bulge (belly, jaw, tail root) the head meets the overhang
            // first: climb along its underside so the technician works outward
            // and around it instead of pressing into it.
            if (input.y > 0 && Physics.SphereCast(transform.position + Vector3.up * 1.2f, .25f, Vector3.up, out var over, .9f, ~0, QueryTriggerInteraction.Ignore)
                && over.normal.y < -.15f && over.collider.transform.IsChildOf(game.creature.transform))
                hit = over;
            // In the crevice where a leg meets the belly the probe can catch an
            // inner face; never hold a surface that faces back into the body.
            var outward = Outward(game);
            var flat = new Vector3(hit.normal.x, 0, hit.normal.z);
            if (flat.sqrMagnitude > .04f && Vector3.Dot(flat.normalized, outward) < -.25f) { if (!was) return false; hit.normal = Vector3.Lerp(hit.normal, outward, .8f).normalized; }
            SurfaceNormal = hit.normal;
            Active = true;
            bool moving = input.sqrMagnitude > .04f;
            stamina = Mathf.Max(0, stamina - (moving ? drainMoving : drainHanging) * dt);
            if (stamina <= 0) { Slip(); return false; }
            return true;
        }

        // Finds the next climbable face above an overhang the technician is
        // pinned under: search outward and upward, then aim back at the body.
        public bool FindSwingTarget(out Vector3 target)
        {
            target = default;
            var game = Expedition.Instance; if (!game) return false;
            var outward = Outward(game);
            for (int k = 0; k < 5; k++)
            {
                var origin = transform.position + outward * (4 + k * 1.5f) + Vector3.up * (2.5f + k * 1.2f);
                if (!Physics.Raycast(origin, -outward, out var h, 12 + k * 2, ~0, QueryTriggerInteraction.Ignore)) continue;
                if (!h.collider.transform.IsChildOf(game.creature.transform)) continue;
                var flat = new Vector3(h.normal.x, 0, h.normal.z);
                if (h.normal.y < -.2f || flat.sqrMagnitude < .04f || Vector3.Dot(flat.normalized, outward) < .2f) continue;
                if (h.point.y < transform.position.y + 1.5f) continue;
                target = h.point + h.normal * .5f - Vector3.up * .8f;
                return true;
            }
            return false;
        }

        // Horizontal direction from Mossback's spine line out to the technician.
        public Vector3 Outward(Expedition game)
        {
            var frame = game.creature.transform;
            var local = frame.InverseTransformPoint(transform.position);
            var o = frame.TransformDirection(new Vector3(local.x, 0, 0)); o.y = 0;
            return o.sqrMagnitude > .01f ? o.normalized : -frame.right;
        }

        bool Probe(Vector3 dir, out RaycastHit hit)
        {
            dir.y = Mathf.Clamp(dir.y, -.5f, .5f);
            var origin = transform.position + Vector3.up * .9f;
            if (Physics.SphereCast(origin, .25f, dir.normalized, out hit, reach, ~0, QueryTriggerInteraction.Ignore)) return true;
            return Physics.Raycast(transform.position + Vector3.up * .35f, dir.normalized, out hit, reach + .2f, ~0, QueryTriggerInteraction.Ignore);
        }

        void Slip()
        {
            Active = false; Exhausted = true; LastSlip = Time.time;
            if (Expedition.Instance) Expedition.Instance.Caption = "[Claw slipping!] Out of stamina. You lose your grip. Rest on solid ground to recover.";
            ProceduralSfx.Play(ProceduralSfx.Deny, .7f);
        }

        void Update()
        {
            if (!motor) return;
            float dt = Time.deltaTime;
            if (!Active && motor.grounded)
            {
                stamina = Mathf.Min(maxStamina, stamina + refill * dt);
                if (stamina > maxStamina * .35f) Exhausted = false;
            }
            UpdateHud(dt);
        }

        void UpdateHud(float dt)
        {
            bool want = Equipped || stamina < maxStamina - .5f;
            shown = Mathf.MoveTowards(shown, want && motor.Active ? 1 : 0, dt * 4);
            if (!bar)
            {
                if (shown <= 0) return;
                var root = UIKit.MakeCanvas("Claw stamina HUD", 11, out _);
                // Beside the hotbar, clear of the caption and prompt lines.
                frame = UIKit.Panel(root, "Claw stamina", new Vector2(.5f, 0), new Vector2(540, 66), new Vector2(240, 22), UIKit.Ink);
                bar = frame.rectTransform;
                fill = UIKit.Panel(bar, "Fill", new Vector2(0, .5f), new Vector2(3, 0), new Vector2(234, 16), UIKit.Good);
                fill.rectTransform.pivot = new Vector2(0, .5f);
                var t = UIKit.Label(bar, "CLAW", 14, UIKit.Cream, new Vector2(.5f, .5f), Vector2.zero, new Vector2(240, 22), TMPro.TextAlignmentOptions.Center);
                t.fontStyle = TMPro.FontStyles.Bold;
                group = bar.gameObject.AddComponent<CanvasGroup>();
            }
            group.alpha = shown;
            float k = stamina / maxStamina;
            fill.rectTransform.sizeDelta = new Vector2(234 * k, 16);
            var warn = Exhausted ? UIKit.Bad : Color.Lerp(UIKit.Bad, k > .5f ? UIKit.Good : UIKit.Gold, Mathf.Clamp01(k * 3));
            if (Active && k < .25f) warn = Color.Lerp(warn, Color.white, Mathf.PingPong(Time.time * 4, 1) * .5f);
            fill.color = warn;
        }
    }
}
