using UnityEngine;
using UnityEngine.InputSystem;

namespace TinyGiantProblems
{
    [DefaultExecutionOrder(20)]
    public sealed class TechnicianMotor : MonoBehaviour
    {
        public CharacterController capsule;
        public Transform model;
        public Camera orbitCamera;
        public Transform checkpoint;
        public Transform[] anchors;
        public LineRenderer rope;
        public float grip = 100;
        public float grace;
        public bool braced, climbing, grounded, tethered;
        // True while the Climbing Claw (not a harness rung) is holding on.
        public bool clawing;
        float pinned, swingT = -1;
        Vector3 swingFrom, swingTo;
        public bool Swinging => swingT >= 0;
        public int recoveries;
        public CareTool selected;
        public string ZoneName { get; private set; } = "Guild harness";
        public Vector3 velocity;
        public Vector3 MoveOverride;
        public bool automate;
        public bool UseOverride;
        public bool BraceOverride;
        bool BraceHeld => automate ? BraceOverride : brace.IsPressed();
        public float yaw = 20, pitch = 27, distance = 8;
        public bool cameraShake = true;
        Transform support, tether;
        Vector3 supportLocal, supportLast;
        float recoverHold;
        InputAction move, look, jump, sprint, brace, use, interact, reel;
        public bool Using => stunned <= 0 && (automate ? UseOverride : use != null && use.IsPressed() && !GameUI.CapturesInput);
        // Paralysis from a Bramblemite sting: no movement, jumping or tool use.
        public float stunned;
        public void Stun(float seconds) { if (grace <= 0) stunned = Mathf.Max(stunned, seconds); }
        public bool UsePressedThisFrame => automate ? useOverrideEdge : use != null && use.WasPressedThisFrame() && !GameUI.CapturesInput;
        bool useOverrideEdge, lastUseOverride;
        public bool Moving => capsule && new Vector2(capsule.velocity.x, capsule.velocity.z).sqrMagnitude > .4f;
        public bool InteractPressed => interact != null && interact.WasPressedThisFrame();
        public bool Active => Expedition.Instance && Expedition.Instance.IsPlaying && !Expedition.Instance.Paused;

        void Awake()
        {
            capsule = GetComponent<CharacterController>();
            // Small moving-platform deltas must not be discarded at high frame rates.
            capsule.minMoveDistance = 0;
            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddBinding("<Gamepad>/leftStick");
            look = new InputAction("Look", InputActionType.Value); look.AddBinding("<Mouse>/delta");
            jump = Button("Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            sprint = Button("Sprint", "<Keyboard>/leftShift", "<Gamepad>/leftStickPress");
            brace = Button("Brace", "<Keyboard>/f", "<Gamepad>/buttonEast");
            use = Button("Use", "<Mouse>/leftButton", "<Gamepad>/rightTrigger");
            interact = Button("Interact", "<Keyboard>/e", "<Gamepad>/buttonWest");
            reel = Button("Reel", "<Keyboard>/r", "<Gamepad>/leftTrigger");
            foreach (var a in new[] {move, look, jump, sprint, brace, use, interact, reel}) a.Enable();
        }
        static InputAction Button(string name, string kb, string pad)
        { var a = new InputAction(name, InputActionType.Button); a.AddBinding(kb); a.AddBinding(pad); return a; }
        void OnDestroy() { foreach (var a in new[] {move, look, jump, sprint, brace, use, interact, reel}) a?.Dispose(); }

        void Update()
        {
            useOverrideEdge = automate && UseOverride && !lastUseOverride; lastUseOverride = automate && UseOverride;
            if (!Active) { braced = false; return; }
            var dt = Time.deltaTime;
            grace = Mathf.Max(0, grace - dt);
            if (swingT >= 0)
            {
                // Arc out and over the bulge, claw first.
                swingT = Mathf.Min(1, swingT + dt / .55f);
                var arc = Vector3.Lerp(swingFrom, swingTo, Mathf.SmoothStep(0, 1, swingT)) + Vector3.up * Mathf.Sin(swingT * Mathf.PI) * .8f;
                capsule.enabled = false; transform.position = arc; capsule.enabled = true;
                velocity = Vector3.zero; climbing = true; clawing = true; SetSupport(null);
                if (swingT >= 1) swingT = -1;
                return;
            }
            if (support)
            {
                var next = support.TransformPoint(supportLocal);
                capsule.Move(next - supportLast);
            }
            var kb = Keyboard.current;
            // Tool selection lives in the hotbar (HotbarUI); it writes `selected`.
            if (selected == CareTool.Tether && UsePressedThisFrame) ToggleTether();
            var input = automate ? new Vector2(MoveOverride.x, MoveOverride.z) : move.ReadValue<Vector2>();
            stunned = Mathf.Max(0, stunned - dt);
            if (stunned > 0) input = Vector2.zero;
            var turn = Quaternion.Euler(0, yaw, 0);
            var direction = turn * new Vector3(input.x, 0, input.y);
            grounded = Physics.SphereCast(transform.position + Vector3.up * .55f, .26f, Vector3.down, out var ground, .42f, ~0, QueryTriggerInteraction.Ignore);
            if (grounded && velocity.y <= 0)
            {
                velocity.y = -2;
                SetSupport(ground.collider.transform);
                var zone = ground.collider.GetComponentInParent<ClimbableZone>();
                if (zone) ZoneName = zone.zoneName;
            }
            else SetSupport(null);
            bool wasClimbing = climbing;
            bool wasClawing = clawing;
            climbing = false; clawing = false;
            var wallDirection = direction.sqrMagnitude > .01f ? direction.normalized : model.forward;
            var claw = ClimbingClaw.For(this);
            if (claw && claw.TryGrip(wallDirection, input, dt, out var clawWall))
            {
                // Climbing Claw: scale any steep part of Mossback. Up follows the
                // surface, a light push keeps the claw dug in.
                climbing = true; clawing = true;
                SetSupport(clawWall.collider.transform);
                velocity = Vector3.zero;
                var surfaceUp = Vector3.ProjectOnPlane(Vector3.up, clawWall.normal).normalized;
                var side = Vector3.Cross(clawWall.normal, surfaceUp).normalized;
                direction = surfaceUp * input.y * .6f + side * input.x * .6f - clawWall.normal * .25f;
                var face = -clawWall.normal; face.y = 0;
                if (face.sqrMagnitude > .01f) model.rotation = Quaternion.Slerp(model.rotation, Quaternion.LookRotation(face), dt * 10);
            }
            else if (BraceHeld && grip > 0 && Physics.Raycast(transform.position + Vector3.up, wallDirection, out var wall, 1.2f) && wall.collider.GetComponentInParent<ClimbableZone>())
            {
                climbing = true;
                SetSupport(wall.collider.transform);
                velocity = Vector3.zero;
                direction = Vector3.up * input.y * .65f + Vector3.Cross(Vector3.up, wall.normal) * input.x * .65f;
                grip = Mathf.Max(0, grip - dt * 7);
            }
            // Pull up over the lip when a climb runs out at the top.
            if (wasClimbing && !climbing && (BraceHeld || wasClawing && claw && !claw.Exhausted) && input.y > 0) { capsule.Move(Vector3.up * .8f + wallDirection * .7f); velocity.y = 2; }
            braced = BraceHeld && grounded && !climbing && grip > 0;
            if (braced) grip = Mathf.Max(0, grip - dt * 4);
            else if (!climbing) grip = Mathf.Min(100, grip + dt * 12);
            if (stunned <= 0 && jump.WasPressedThisFrame() && (grounded || climbing)) { velocity.y = 7; climbing = false; SetSupport(null); }
            if (!climbing) velocity.y = Mathf.Max(-28, velocity.y - 20 * dt);
            var planar = direction * (sprint.IsPressed() ? 8 : 5);
            if (braced) planar = Vector3.zero;
            if (tethered && tether)
            {
                var delta = tether.position - (transform.position + Vector3.up);
                if (delta.magnitude > 10) { velocity += delta.normalized * 28 * dt; velocity *= .98f; }
                if (reel.IsPressed())
                {
                    capsule.Move(delta.normalized * 9 * dt);
                    velocity = Vector3.zero;
                    if (delta.magnitude < 1.5f) { Warp(tether.position + Vector3.up); tethered = false; grace = 3; }
                }
            }
            capsule.Move((planar + velocity) * dt);
            // Claw under a bulge: once the head has been pinned by the overhang for
            // a moment, reach around it and swing up to the face above.
            pinned = clawing && input.y > 0 && (capsule.collisionFlags & CollisionFlags.Above) != 0 ? pinned + dt : 0;
            if (pinned > .35f && claw && claw.FindSwingTarget(out var swingTarget))
            {
                swingFrom = transform.position; swingTo = swingTarget; swingT = 0; pinned = 0;
                ProceduralSfx.Play(ProceduralSfx.Whoosh, .5f);
            }
            velocity.x = Mathf.MoveTowards(velocity.x, 0, 8 * dt);
            velocity.z = Mathf.MoveTowards(velocity.z, 0, 8 * dt);
            if (planar.sqrMagnitude > .05f && !climbing)
                model.rotation = Quaternion.Slerp(model.rotation, Quaternion.LookRotation(new Vector3(planar.x, 0, planar.z)), dt * 12);
            model.localScale = new Vector3(1, braced ? .65f : 1, 1);
            if (support) { supportLocal = support.InverseTransformPoint(transform.position); supportLast = transform.position; }
            if (transform.position.y < -6) Recover();
            recoverHold = kb != null && kb.backspaceKey.isPressed ? recoverHold + dt : 0;
            if (recoverHold > 1) { Recover(); recoverHold = 0; }
            rope.enabled = tethered && tether;
            if (rope.enabled) { rope.SetPosition(0, transform.position + Vector3.up); rope.SetPosition(1, tether.position); }
        }
        void LateUpdate()
        {
            if (!orbitCamera) return;
            if (Expedition.Instance && Expedition.Instance.Phase == ExpeditionPhase.Briefing)
            {
                var frame = Expedition.Instance.creature.transform;
                orbitCamera.transform.position = frame.TransformPoint(new Vector3(37,24,43));
                orbitCamera.transform.LookAt(frame.TransformPoint(new Vector3(0,6,5)));
                orbitCamera.transform.LookAt(frame.TransformPoint(new Vector3(0,6,5))-orbitCamera.transform.right*12);
                return;
            }
            if (Active && !automate && !GameUI.CapturesInput)
            {
                var d = look.ReadValue<Vector2>() * .12f;
                if (Gamepad.current != null) d += Gamepad.current.rightStick.ReadValue() * Time.deltaTime * 110;
                yaw += d.x; pitch = Mathf.Clamp(pitch - d.y, -12, 65);
                // Plain scroll belongs to the hotbar; hold Ctrl (or use - / =) to zoom.
                bool zoomHeld = Keyboard.current != null && Keyboard.current.ctrlKey.isPressed;
                if (Mouse.current != null && zoomHeld) distance = Mathf.Clamp(distance - Mouse.current.scroll.ReadValue().y * .005f, 4, 15);
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.minusKey.isPressed) distance = Mathf.Clamp(distance + Time.deltaTime * 6, 4, 15);
                    if (Keyboard.current.equalsKey.isPressed) distance = Mathf.Clamp(distance - Time.deltaTime * 6, 4, 15);
                }
            }
            Vector3 focus = transform.position + Vector3.up * 1.3f;
            var rotation = Quaternion.Euler(pitch, yaw, 0);
            var desired = focus - rotation * Vector3.forward * distance;
            var ray = desired - focus;
            if (Physics.SphereCast(focus, .22f, ray.normalized, out var hit, ray.magnitude, ~0, QueryTriggerInteraction.Ignore))
                desired = focus + ray.normalized * Mathf.Max(.6f, hit.distance - .15f);
            orbitCamera.transform.position = desired;
            orbitCamera.transform.rotation = rotation;
        }
        void SetSupport(Transform next)
        {
            if (support == next) return;
            support = next;
            if (support) { supportLocal = support.InverseTransformPoint(transform.position); supportLast = transform.position; }
        }
        public void ToggleTether()
        {
            if (tethered) { tethered = false; return; }
            float best = 12;
            foreach (var a in anchors) { var d = Vector3.Distance(a.position, transform.position); if (d < best) { best = d; tether = a; } }
            tethered = tether && best < 12;
            Expedition.Instance.Caption = tethered ? "[Rope clipped] Hold R / LT to reel to the anchor." : "Move within 12m of a tether winch.";
        }
        public void Launch(Vector3 impulse)
        {
            if (!CareRules.LaunchAllowed(braced, tethered, grace)) return;
            velocity = Vector3.ClampMagnitude(impulse, 15); SetSupport(null); grace = 3;
        }
        public void Warp(Vector3 position) { capsule.enabled = false; transform.position = position; capsule.enabled = true; velocity = Vector3.zero; SetSupport(null); var fall = GetComponent<FallHurt>(); if (fall) fall.ResetApex(); }
        public void Recover() { recoveries++; Warp(checkpoint.position); tethered = false; climbing = false; grace = 6; Expedition.Instance.Caption = "[Safety winch] Back on the harness. Your work is safe."; }
        public void ResetMotor() { recoveries = 0; grip = 100; grace = 6; tethered = false; braced = false; stunned = 0; selected = CareTool.Vacuum; Warp(checkpoint.position); }
    }
}
