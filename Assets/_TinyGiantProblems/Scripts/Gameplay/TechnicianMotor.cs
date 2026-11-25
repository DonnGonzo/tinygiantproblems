using UnityEngine;

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
        public string ZoneName => "Back";
        public Vector3 velocity;          // vertical speed + external impulses (launches, tether pull)
        public Vector3 planarVelocity;    // smoothed walking/running velocity
        public Vector3 MoveOverride;
        public bool automate;
        public bool UseOverride;
        public bool BraceOverride;
        // Automation: set true to press jump once (consumed on the next frame).
        public bool JumpOverride;
        bool BraceHeld => automate ? BraceOverride : GameInput.Brace.IsPressed() && !GameUI.CapturesInput;
        public float yaw = 20, pitch = 27, distance = 8;
        public bool cameraShake = true;
        Transform support, tether;
        Vector3 supportLocal, supportLast;
        float recoverHold, lastGrounded = -10, lastJumpPress = -10, cameraDistance = 8;
        Vector3 groundNormal = Vector3.up, focusSmoothed, focusVelocity;
        bool jumpHeld, focusInit;
        float autoJumpHeldUntil;
        MovementTuning Tune => MovementTuning.Current;
        public bool Using => stunned <= 0 && (automate ? UseOverride : GameInput.Use.IsPressed() && !GameUI.CapturesInput);
        // Paralysis from a Bramblemite sting or a hard fall: no movement, jumping or tool use.
        public float stunned;
        public void Stun(float seconds) { if (grace > 0) return; stunned = Mathf.Max(stunned, seconds); stings++; }
        // Run counters read by mission challenges.
        public int launches, stings;
        public bool UsePressedThisFrame => automate ? useOverrideEdge : GameInput.Use.WasPressedThisFrame() && !GameUI.CapturesInput;
        bool useOverrideEdge, lastUseOverride;
        public bool Moving => capsule && new Vector2(capsule.velocity.x, capsule.velocity.z).sqrMagnitude > .4f;
        public bool InteractPressed => GameInput.Interact.WasPressedThisFrame();
        public bool Active => Expedition.Instance && Expedition.Instance.IsPlaying && !Expedition.Instance.Paused;

        void Awake()
        {
            capsule = GetComponent<CharacterController>();
            // Small moving-platform deltas must not be discarded at high frame rates.
            capsule.minMoveDistance = 0;
            capsule.slopeLimit = Tune.maxSlope;
            distance = Tune.distance; cameraDistance = distance;
        }

        void Update()
        {
            useOverrideEdge = automate && UseOverride && !lastUseOverride; lastUseOverride = automate && UseOverride;
            if (!Active) { braced = false; return; }
            var t = Tune;
            var dt = Time.deltaTime;
            grace = Mathf.Max(0, grace - dt);
            if (swingT >= 0)
            {
                // Arc out and over the bulge, claw first.
                swingT = Mathf.Min(1, swingT + dt / .55f);
                var arc = Vector3.Lerp(swingFrom, swingTo, Mathf.SmoothStep(0, 1, swingT)) + Vector3.up * Mathf.Sin(swingT * Mathf.PI) * .8f;
                capsule.enabled = false; transform.position = arc; capsule.enabled = true;
                velocity = Vector3.zero; planarVelocity = Vector3.zero; climbing = true; clawing = true; SetSupport(null);
                if (swingT >= 1) swingT = -1;
                return;
            }
            if (support)
            {
                var next = support.TransformPoint(supportLocal);
                capsule.Move(next - supportLast);
            }
            // Tool selection lives in the hotbar (HotbarUI); it writes `selected`.
            if (selected == CareTool.Tether && UsePressedThisFrame) ToggleTether();
            bool uiBlocked = GameUI.CapturesInput && !automate;
            var input = automate ? new Vector2(MoveOverride.x, MoveOverride.z) : uiBlocked ? Vector2.zero : GameInput.Move.ReadValue<Vector2>();
            input = Vector2.ClampMagnitude(input, 1);
            stunned = Mathf.Max(0, stunned - dt);
            if (stunned > 0) input = Vector2.zero;
            if (tumbling) input *= .15f;
            if (automate && JumpOverride) { lastJumpPress = Time.time; autoJumpHeldUntil = Time.time + .3f; JumpOverride = false; }
            else if (!automate && !uiBlocked && GameInput.Jump.WasPressedThisFrame()) lastJumpPress = Time.time;
            jumpHeld = automate ? Time.time < autoJumpHeldUntil : GameInput.Jump.IsPressed();
            var turn = Quaternion.Euler(0, yaw, 0);
            var direction = turn * new Vector3(input.x, 0, input.y);

            grounded = Physics.SphereCast(transform.position + Vector3.up * .55f, .26f, Vector3.down, out var ground, .42f, ~0, QueryTriggerInteraction.Ignore);
            groundNormal = grounded ? ground.normal : Vector3.up;
            bool steep = grounded && Vector3.Angle(groundNormal, Vector3.up) > t.maxSlope;
            if (grounded && velocity.y <= 0)
            {
                velocity.y = -2;
                lastGrounded = Time.time;
                SetSupport(ground.collider.transform);
            }
            else SetSupport(null);
            if (tumbling && (grounded && Time.time - launchedAt > .35f || Time.time - launchedAt > 12)) tumbling = false;

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
                velocity = Vector3.zero; planarVelocity = Vector3.zero;
                var surfaceUp = Vector3.ProjectOnPlane(Vector3.up, clawWall.normal).normalized;
                var side = Vector3.Cross(clawWall.normal, surfaceUp).normalized;
                direction = surfaceUp * input.y * .6f + side * input.x * .6f - clawWall.normal * .25f;
                var face = -clawWall.normal; face.y = 0;
                if (face.sqrMagnitude > .01f) model.rotation = Quaternion.Slerp(model.rotation, Quaternion.LookRotation(face), dt * 10);
            }
            // Pull up over the lip when a claw climb runs out at the top.
            if (wasClimbing && !climbing && wasClawing && claw && !claw.Exhausted && input.y > 0) { capsule.Move(Vector3.up * .8f + wallDirection * .7f); velocity.y = 2; }

            braced = BraceHeld && grounded && !climbing && grip > 0;
            if (braced) grip = Mathf.Max(0, grip - dt * 4);
            else if (!climbing) grip = Mathf.Min(100, grip + dt * 12);

            // Jump: buffered presses and a little coyote time after leaving a ledge.
            bool canJump = stunned <= 0 && !braced && (climbing || Time.time - lastGrounded <= t.coyoteTime) && !steep;
            if (canJump && Time.time - lastJumpPress <= t.jumpBuffer)
            {
                velocity.y = Mathf.Sqrt(2 * t.gravity * t.jumpHeight);
                lastJumpPress = -10; lastGrounded = -10;
                climbing = false; SetSupport(null);
                Jumped?.Invoke();
            }
            if (!climbing)
            {
                float g = t.gravity;
                if (velocity.y < 0) g *= t.fallGravityMultiplier;
                else if (!jumpHeld && !tumbling) g *= t.lowJumpMultiplier;
                velocity.y = Mathf.Max(-t.maxFallSpeed, velocity.y - g * dt);
            }

            // Walking/running: smoothed toward the target with separate ramps
            // for speeding up, slowing down and steering in the air.
            Vector3 target;
            if (climbing) target = direction * t.walkSpeed;
            else
            {
                bool sprint = !automate && GameInput.Sprint.IsPressed();
                target = new Vector3(direction.x, 0, direction.z) * (sprint ? t.sprintSpeed : t.walkSpeed);
                if (braced) target = Vector3.zero;
            }
            if (climbing) planarVelocity = target;
            else
            {
                float rate = !grounded ? t.airAcceleration : target.sqrMagnitude >= planarVelocity.sqrMagnitude ? t.acceleration : t.deceleration;
                planarVelocity = Vector3.MoveTowards(planarVelocity, target, rate * dt);
            }
            var step = planarVelocity;
            if (grounded && !climbing && !steep && velocity.y <= 0)
            {
                // Follow the slope so walking downhill doesn't bounce.
                step = Vector3.ProjectOnPlane(planarVelocity, groundNormal).normalized * planarVelocity.magnitude;
            }
            if (steep && !climbing)
            {
                var downhill = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;
                step += downhill * t.slideSpeed;
            }

            if (tethered && tether)
            {
                var delta = tether.position - (transform.position + Vector3.up);
                if (delta.magnitude > 10) { velocity += delta.normalized * 28 * dt; velocity *= .98f; }
                if (!automate && GameInput.Reel.IsPressed())
                {
                    capsule.Move(delta.normalized * 9 * dt);
                    velocity = Vector3.zero;
                    if (delta.magnitude < 1.5f) { Warp(tether.position + Vector3.up); tethered = false; grace = 3; }
                }
            }
            capsule.Move((step + velocity) * dt);
            // Claw under a bulge: once the head has been pinned by the overhang for
            // a moment, reach around it and swing up to the face above.
            pinned = clawing && input.y > 0 && (capsule.collisionFlags & CollisionFlags.Above) != 0 ? pinned + dt : 0;
            if (pinned > .35f && claw && claw.FindSwingTarget(out var swingTarget))
            {
                swingFrom = transform.position; swingTo = swingTarget; swingT = 0; pinned = 0;
                ProceduralSfx.Play(ProceduralSfx.Whoosh, .5f);
            }
            // A thrown body keeps its momentum; impulses settle quickly on the ground.
            float drag = tumbling ? 1.5f : grounded ? 10 : 3;
            velocity.x = Mathf.MoveTowards(velocity.x, 0, drag * dt);
            velocity.z = Mathf.MoveTowards(velocity.z, 0, drag * dt);
            var facing = new Vector3(planarVelocity.x, 0, planarVelocity.z);
            if (facing.sqrMagnitude > .05f && !climbing && direction.sqrMagnitude > .01f)
                model.rotation = Quaternion.RotateTowards(model.rotation, Quaternion.LookRotation(facing), t.turnSpeed * dt);
            model.localScale = new Vector3(1, braced ? .65f : 1, 1);
            if (support) { supportLocal = support.InverseTransformPoint(transform.position); supportLast = transform.position; }
            if (transform.position.y < -6) Recover();
            recoverHold = !automate && GameInput.Recover.IsPressed() ? recoverHold + dt : 0;
            if (recoverHold > 1) { Recover(); recoverHold = 0; }
            rope.enabled = tethered && tether;
            if (rope.enabled) { rope.SetPosition(0, transform.position + Vector3.up); rope.SetPosition(1, tether.position); }
        }

        public event System.Action Jumped;

        void LateUpdate()
        {
            if (!orbitCamera) return;
            var t = Tune;
            if (Expedition.Instance && Expedition.Instance.Phase == ExpeditionPhase.Briefing)
            {
                var frame = Expedition.Instance.creature.transform;
                orbitCamera.transform.position = frame.TransformPoint(new Vector3(37,24,43));
                orbitCamera.transform.LookAt(frame.TransformPoint(new Vector3(0,6,5))-orbitCamera.transform.right*12);
                focusInit = false;
                return;
            }
            if (Active && !automate && !GameUI.CapturesInput)
            {
                var look = GameInput.Look.ReadValue<Vector2>();
                var sens = GameSettings.LookSensitivity;
                Vector2 d = GameInput.UsingGamepad ? look * t.gamepadSensitivity * sens * Time.deltaTime : look * t.mouseSensitivity * sens;
                bool invert = t.invertY ^ GameSettings.InvertY;
                yaw += d.x; pitch = Mathf.Clamp(pitch + (invert ? d.y : -d.y), t.minPitch, t.maxPitch);
                // Plain scroll belongs to the hotbar; hold the zoom modifier to zoom.
                float zoom = GameInput.Zoom.ReadValue<float>() * Time.deltaTime * 6;
                if (GameInput.ZoomModifier.IsPressed()) zoom += GameInput.HotbarScroll.ReadValue<float>() * .005f;
                distance = Mathf.Clamp(distance - zoom, t.minDistance, t.maxDistance);
            }
            // Smoothly follow the technician; snap after teleports.
            var focusTarget = transform.position + Vector3.up * t.focusHeight;
            if (!focusInit || (focusTarget - focusSmoothed).sqrMagnitude > 64) { focusSmoothed = focusTarget; focusVelocity = Vector3.zero; focusInit = true; }
            focusSmoothed = Vector3.SmoothDamp(focusSmoothed, focusTarget, ref focusVelocity, t.followDamping);
            var rotation = Quaternion.Euler(pitch, yaw, 0);
            var back = -(rotation * Vector3.forward);
            // Never through walls: pull in at once, ease back out.
            float allowed = distance;
            if (Physics.SphereCast(focusSmoothed, t.collisionRadius, back, out var hit, distance, ~0, QueryTriggerInteraction.Ignore) && !hit.collider.transform.IsChildOf(transform))
                allowed = Mathf.Max(.6f, hit.distance - .1f);
            cameraDistance = allowed < cameraDistance ? allowed : Mathf.MoveTowards(cameraDistance, allowed, (distance / Mathf.Max(.05f, t.collisionRecover)) * Time.deltaTime);
            orbitCamera.transform.position = focusSmoothed + back * cameraDistance;
            orbitCamera.transform.rotation = rotation;
            if (shakeTime > 0)
            {
                shakeTime -= Time.deltaTime;
                float k = shakeAmp * Mathf.Clamp01(shakeTime / shakeLength) * GameSettings.ScreenShake;
                float n = Time.time * 31;
                orbitCamera.transform.position += orbitCamera.transform.rotation * new Vector3(Mathf.PerlinNoise(n, 1) - .5f, Mathf.PerlinNoise(2, n) - .5f, 0) * k * 1.2f;
                orbitCamera.transform.rotation *= Quaternion.Euler((Mathf.PerlinNoise(n, 5) - .5f) * k * 4, 0, (Mathf.PerlinNoise(7, n) - .5f) * k * 5);
            }
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
            velocity = Vector3.ClampMagnitude(impulse, 28); planarVelocity = Vector3.zero; SetSupport(null); grace = 3;
            climbing = false; clawing = false;
            // Big throws send the body tumbling until it lands (TechnicianRig).
            if (impulse.magnitude > 9) { tumbling = true; launchedAt = Time.time; Shake(.5f, .35f); launches++; }
        }
        // Thrown by a sneeze/shake: limp tumble, almost no air control.
        public bool tumbling;
        public float launchedAt = -10;
        float shakeAmp, shakeTime, shakeLength = 1;
        public void Shake(float amplitude, float seconds) { shakeAmp = Mathf.Max(shakeAmp * (shakeTime / Mathf.Max(.01f, shakeLength)), amplitude); shakeTime = shakeLength = seconds; }
        public void Warp(Vector3 position) { capsule.enabled = false; transform.position = position; capsule.enabled = true; velocity = Vector3.zero; planarVelocity = Vector3.zero; SetSupport(null); var fall = GetComponent<FallHurt>(); if (fall) fall.ResetApex(); }
        public void Recover() { recoveries++; Warp(checkpoint.position); tethered = false; climbing = false; grace = 6; Expedition.Instance.Caption = "[Safety winch] Hauled back up onto Mossback. Your work is safe."; }
        public void ResetMotor() { recoveries = 0; grip = 100; grace = 6; tethered = false; braced = false; stunned = 0; selected = CareTool.Vacuum; Warp(checkpoint.position); }
    }
}
