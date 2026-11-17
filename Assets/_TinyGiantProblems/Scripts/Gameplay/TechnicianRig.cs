using UnityEngine;

namespace TinyGiantProblems
{
    // Procedural skeletal animation for the imported guild technician.
    // The bones are built by Editor/TechnicianRigBuilder (code rig, skinned on
    // the T-pose-less turnaround mesh). Every motion is a smooth blend of
    // layered poses driven by TechnicianMotor state: idle breathing, walk, run,
    // brace crouch, wall/claw climbing, jump tuck, falling flail, sting jolt,
    // fall-hurt slump and the get-up that follows. Tools ride on `handAnchor`.
    //
    // A Mixamo-rigged replacement can drop in later: keep `handAnchor` on its
    // right hand and fill `bones` in the same order (see Bone), or replace this
    // component with an Animator and leave the claw/fall/tool systems as they are.
    [DefaultExecutionOrder(46)]
    public sealed class TechnicianRig : MonoBehaviour
    {
        public enum Bone { Hips, Spine, Chest, Head, HatTip, UpperArmL, ForeArmL, HandL, UpperArmR, ForeArmR, HandR, ThighL, ShinL, FootL, ThighR, ShinR, FootR, Count }
        public Transform[] bones;
        public Vector3[] restPositions;
        public Transform handAnchor, leftHandAnchor;
        public SkinnedMeshRenderer body;
        public TechnicianMotor motor;

        // Smoothed layer weights, public for tests and the playtest report.
        public float walk, run, climb, air, brace, hurt, jolt, holdTool, getUp;
        float phase, hatSwing, hatVel, landSquash, lastVy, idleClock;
        bool wasGrounded = true;
        float airTime;
        Vector3 lastPos;
        ToolAbilities abilities;
        TechnicianToolVisuals tools;

        void Start()
        {
            if (!motor) motor = GetComponentInParent<TechnicianMotor>();
            abilities = motor ? motor.GetComponent<ToolAbilities>() : null;
            lastPos = transform.position;
            if (transform.parent) baseScale = transform.parent.localScale.x;
        }
        float baseScale = 1;

        static float Damp(float current, float target, float speed, float dt) => Mathf.Lerp(current, target, 1 - Mathf.Exp(-speed * dt));

        void LateUpdate()
        {
            if (!motor || bones == null || bones.Length < (int)Bone.Count) return;
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            if (!tools) tools = motor.GetComponentInChildren<TechnicianToolVisuals>();

            var v = motor.capsule ? motor.capsule.velocity : Vector3.zero;
            float speed = new Vector2(v.x, v.z).magnitude;
            bool grounded = motor.grounded;
            bool climbing = motor.climbing || ClimbingClaw.IsClimbing(motor);
            bool gliding = abilities && abilities.Gliding;
            bool fallHurt = FallHurt.IsHurt(motor);
            bool stung = motor.stunned > 0 && !fallHurt;

            // Landing squash from the air time just ended.
            if (grounded && !wasGrounded && airTime > .3f) landSquash = Mathf.Clamp01(airTime * .7f);
            airTime = grounded || climbing ? 0 : airTime + dt;
            wasGrounded = grounded;
            landSquash = Damp(landSquash, 0, 6, dt);

            float loco = grounded && !motor.braced && !fallHurt ? Mathf.Clamp01(speed / 4.5f) : 0;
            float sprint = grounded && !motor.braced && !fallHurt ? Mathf.Clamp01((speed - 5.2f) / 2.4f) : 0;
            walk = Damp(walk, loco * (1 - sprint), 10, dt);
            run = Damp(run, sprint, 8, dt);
            climb = Damp(climb, climbing ? 1 : 0, 10, dt);
            air = Damp(air, !grounded && !climbing && airTime > .08f ? 1 : 0, 9, dt);
            brace = Damp(brace, motor.braced ? 1 : 0, 12, dt);
            hurt = Damp(hurt, fallHurt ? 1 : 0, fallHurt ? 14 : 3.5f, dt);
            jolt = Damp(jolt, stung ? 1 : 0, 16, dt);
            getUp = Mathf.Clamp01(hurt) * (fallHurt ? 0 : 1);
            var held = GuildEconomy.Instance ? GuildEconomy.Instance.Inventory.SelectedDef : null;
            holdTool = Damp(holdTool, held != null && held.IsTool && held.id != ItemId.Glider && held.id != ItemId.Claw ? 1 : 0, 8, dt);

            float cadence = climbing ? 5.5f * Mathf.Clamp01(Vector3.Distance(transform.position, lastPos) / dt / 1.2f + .15f)
                                     : 5.2f + speed * 1.15f;
            if (loco > .02f || climbing) phase += dt * cadence;
            idleClock += dt;
            lastPos = transform.position;

            // Reset to rest, then layer.
            for (int i = 0; i < (int)Bone.Count; i++) { bones[i].localPosition = restPositions[i]; bones[i].localRotation = Quaternion.identity; }
            var e = new Vector3[(int)Bone.Count];
            var hipOffset = Vector3.zero;

            float s = Mathf.Sin(phase), c = Mathf.Cos(phase);
            float stepUp = Mathf.Abs(Mathf.Sin(phase));

            // Idle: breathing and a slow weight shift.
            float idle = Mathf.Clamp01(1 - walk - run - climb - air - brace - hurt);
            e[(int)Bone.Chest].x += Mathf.Sin(idleClock * 1.7f) * 1.6f * idle;
            e[(int)Bone.Hips].z += Mathf.Sin(idleClock * .6f) * 1.5f * idle;
            e[(int)Bone.Head].y += Mathf.Sin(idleClock * .45f) * 6 * idle;
            e[(int)Bone.UpperArmL].z += -4 * idle; e[(int)Bone.UpperArmR].z += 4 * idle;

            // Walk: legs swing, knees fold on the passing step, opposite arm swing.
            if (walk > .001f)
            {
                float w = walk;
                e[(int)Bone.ThighL].x += -s * 30 * w; e[(int)Bone.ThighR].x += s * 30 * w;
                e[(int)Bone.ShinL].x += Mathf.Max(0, c) * 42 * w; e[(int)Bone.ShinR].x += Mathf.Max(0, -c) * 42 * w;
                e[(int)Bone.FootL].x += -Mathf.Max(0, -s) * 15 * w; e[(int)Bone.FootR].x += -Mathf.Max(0, s) * 15 * w;
                e[(int)Bone.UpperArmL].x += s * 26 * w; e[(int)Bone.UpperArmR].x += -s * 26 * w * (1 - holdTool * .8f);
                e[(int)Bone.ForeArmL].x += -12 * w; e[(int)Bone.ForeArmR].x += -12 * w;
                e[(int)Bone.Hips].z += s * 4 * w; e[(int)Bone.Hips].y += s * 6 * w;
                e[(int)Bone.Spine].x += 4 * w; e[(int)Bone.Chest].y += -s * 5 * w;
                hipOffset.y += (stepUp - .5f) * .022f * w;
            }
            // Run: longer stride, high knees, pumping bent arms, forward lean.
            if (run > .001f)
            {
                float r = run;
                e[(int)Bone.ThighL].x += -s * 48 * r; e[(int)Bone.ThighR].x += s * 48 * r;
                e[(int)Bone.ShinL].x += (Mathf.Max(0, c) * 85 + 10) * r; e[(int)Bone.ShinR].x += (Mathf.Max(0, -c) * 85 + 10) * r;
                e[(int)Bone.UpperArmL].x += s * 45 * r; e[(int)Bone.UpperArmR].x += -s * 45 * r * (1 - holdTool * .7f);
                e[(int)Bone.ForeArmL].x += -70 * r; e[(int)Bone.ForeArmR].x += -70 * r * (1 - holdTool * .5f);
                e[(int)Bone.UpperArmL].z += -8 * r; e[(int)Bone.UpperArmR].z += 8 * r;
                e[(int)Bone.Hips].y += s * 9 * r; e[(int)Bone.Spine].x += 12 * r; e[(int)Bone.Head].x += -6 * r;
                hipOffset.y += (stepUp - .35f) * .045f * r;
            }
            // Brace: low crouch, arms forward gripping the coat.
            if (brace > .001f)
            {
                float b = brace;
                hipOffset.y += -.09f * b;
                e[(int)Bone.ThighL].x += -62 * b; e[(int)Bone.ThighR].x += -62 * b;
                e[(int)Bone.ShinL].x += 95 * b; e[(int)Bone.ShinR].x += 95 * b;
                e[(int)Bone.FootL].x += -30 * b; e[(int)Bone.FootR].x += -30 * b;
                e[(int)Bone.Spine].x += 22 * b; e[(int)Bone.Chest].x += 8 * b; e[(int)Bone.Head].x += -18 * b;
                e[(int)Bone.UpperArmL].x += -55 * b; e[(int)Bone.UpperArmR].x += -55 * b;
                e[(int)Bone.ForeArmL].x += -35 * b; e[(int)Bone.ForeArmR].x += -35 * b;
            }
            // Climb: alternate reaches overhead with the opposite knee driving up.
            if (climb > .001f)
            {
                float k = climb;
                e[(int)Bone.UpperArmL].x += (-135 + s * 25) * k; e[(int)Bone.UpperArmR].x += (-135 - s * 25) * k;
                e[(int)Bone.UpperArmL].z += -12 * k; e[(int)Bone.UpperArmR].z += 12 * k;
                e[(int)Bone.ForeArmL].x += (-25 - Mathf.Max(0, s) * 35) * k; e[(int)Bone.ForeArmR].x += (-25 - Mathf.Max(0, -s) * 35) * k;
                e[(int)Bone.ThighL].x += (-45 - Mathf.Max(0, -s) * 35) * k; e[(int)Bone.ThighR].x += (-45 - Mathf.Max(0, s) * 35) * k;
                e[(int)Bone.ShinL].x += (70 + Mathf.Max(0, -s) * 25) * k; e[(int)Bone.ShinR].x += (70 + Mathf.Max(0, s) * 25) * k;
                e[(int)Bone.Spine].x += 10 * k; e[(int)Bone.Head].x += -25 * k; e[(int)Bone.Hips].y += s * 5 * k;
                hipOffset += new Vector3(0, s * .015f, .04f) * k;
            }
            // Air: tuck on the way up, flail when dropping fast, spread under the glider.
            if (air > .001f)
            {
                float a = air;
                float up = Mathf.Clamp01(motor.velocity.y / 5f), down = Mathf.Clamp01(-motor.velocity.y / 9f);
                float flail = down * (gliding ? 0 : 1);
                float t = idleClock * 11;
                e[(int)Bone.ThighL].x += (-55 * up - 15 + Mathf.Sin(t) * 25 * flail) * a; e[(int)Bone.ThighR].x += (-35 * up - 5 - Mathf.Sin(t) * 25 * flail) * a;
                e[(int)Bone.ShinL].x += (75 * up + 25) * a; e[(int)Bone.ShinR].x += (60 * up + 15) * a;
                e[(int)Bone.UpperArmL].x += (-110 * up - 40 * flail) * a; e[(int)Bone.UpperArmR].x += (-110 * up - 40 * flail) * a * (1 - holdTool * .5f);
                e[(int)Bone.UpperArmL].z += (-30 - 30 * flail + Mathf.Sin(t * 1.3f) * 20 * flail) * a; e[(int)Bone.UpperArmR].z += (30 + 30 * flail - Mathf.Sin(t * 1.3f) * 20 * flail) * a;
                e[(int)Bone.Spine].x += (-8 * flail + 6 * up) * a; e[(int)Bone.Head].x += (-15 * flail) * a;
                if (gliding) { e[(int)Bone.UpperArmL].x += -140 * a; e[(int)Bone.UpperArmR].x += -140 * a; e[(int)Bone.ThighL].x += 10 * a; e[(int)Bone.ThighR].x += 10 * a; }
            }
            // Holding a tool: right arm carries it forward at the hip.
            if (holdTool > .001f)
            {
                float h = holdTool * (1 - climb) * (1 - hurt);
                e[(int)Bone.UpperArmR].x += -28 * h; e[(int)Bone.ForeArmR].x += -55 * h; e[(int)Bone.UpperArmR].z += 6 * h;
                e[(int)Bone.HandR].x += 10 * h;
            }
            // Sting jolt: rigid arms out, shivering.
            if (jolt > .001f)
            {
                float j = jolt, z = Mathf.Sin(Time.time * 70);
                e[(int)Bone.UpperArmL].z += (-55 + z * 6) * j; e[(int)Bone.UpperArmR].z += (55 - z * 6) * j;
                e[(int)Bone.Spine].x += (-10 + z * 3) * j; e[(int)Bone.Head].z += z * 8 * j;
                e[(int)Bone.HatTip].z += z * 18 * j;
                hipOffset += new Vector3(z * .006f, 0, 0) * j;
            }
            // Fall hurt: slumped on the ground, dazed head circles. As the
            // stun ends the weight eases out, which reads as getting back up.
            if (hurt > .001f)
            {
                float h = hurt, daze = idleClock * 3.2f;
                hipOffset += new Vector3(0, -.2f, -.05f) * h;
                e[(int)Bone.ThighL].x += -85 * h; e[(int)Bone.ThighR].x += -78 * h;
                e[(int)Bone.ThighL].z += -10 * h; e[(int)Bone.ThighR].z += 12 * h;
                e[(int)Bone.ShinL].x += 25 * h; e[(int)Bone.ShinR].x += 35 * h;
                e[(int)Bone.Spine].x += -18 * h; e[(int)Bone.Chest].x += 12 * h;
                e[(int)Bone.Head].x += (20 + Mathf.Sin(daze) * 10) * h; e[(int)Bone.Head].z += Mathf.Cos(daze) * 14 * h;
                e[(int)Bone.UpperArmL].z += -40 * h; e[(int)Bone.UpperArmR].z += 40 * h;
                e[(int)Bone.UpperArmL].x += 20 * h; e[(int)Bone.UpperArmR].x += 20 * h;
                e[(int)Bone.HatTip].x += 30 * h;
                // Get-up: lean forward while the weight is leaving.
                if (!fallHurt) { e[(int)Bone.Spine].x += 40 * h * (1 - h) * 4; }
            }
            // Landing squash.
            if (landSquash > .001f)
            {
                hipOffset.y += -.07f * landSquash;
                e[(int)Bone.ThighL].x += -35 * landSquash; e[(int)Bone.ThighR].x += -35 * landSquash;
                e[(int)Bone.ShinL].x += 55 * landSquash; e[(int)Bone.ShinR].x += 55 * landSquash;
                e[(int)Bone.Spine].x += 10 * landSquash;
            }

            // Floppy hat: a spring that trails body motion.
            float accel = (motor.velocity.y - lastVy) / Mathf.Max(dt, 1 / 60f); lastVy = motor.velocity.y;
            float hatTarget = -speed * 2.5f + Mathf.Clamp(accel * .4f, -20, 20) + Mathf.Sin(phase * 2) * 6 * (walk + run);
            // Sub-stepped so a long frame (load, warp) cannot blow the spring up.
            float left = Mathf.Min(dt, .1f);
            while (left > 0) { float h = Mathf.Min(left, 1 / 120f); hatVel += ((hatTarget - hatSwing) * 60 - hatVel * 9) * h; hatSwing += hatVel * h; left -= h; }
            if (!float.IsFinite(hatSwing) || !float.IsFinite(hatVel)) { hatSwing = 0; hatVel = 0; }
            hatVel = Mathf.Clamp(hatVel, -400, 400);
            e[(int)Bone.HatTip].x += Mathf.Clamp(hatSwing, -35, 35);
            e[(int)Bone.HatTip].z += Mathf.Sin(idleClock * 1.3f) * 4;

            bones[(int)Bone.Hips].localPosition = restPositions[(int)Bone.Hips] + hipOffset;
            for (int i = 0; i < (int)Bone.Count; i++) if (e[i] != Vector3.zero) bones[i].localRotation = Quaternion.Euler(e[i]);

            // Tools follow the hand's position but only a little of its swing, so a
            // carried tool stays level and pointed where the technician faces.
            // Climbing and the claw let the hand lead fully.
            var facing = motor.model ? motor.model.rotation : transform.rotation;
            float follow = Mathf.Lerp(.22f, 1, Mathf.Max(climb, air * .5f));
            if (handAnchor) handAnchor.rotation = Quaternion.Slerp(facing, bones[(int)Bone.HandR].rotation, follow);
            if (leftHandAnchor) leftHandAnchor.rotation = Quaternion.Slerp(facing, bones[(int)Bone.HandL].rotation, follow);

            // The motor squashes its model while bracing; the rig crouches instead.
            var instance = transform.parent;
            if (motor.model && instance && instance.parent == motor.model)
                instance.localScale = new Vector3(baseScale, baseScale / Mathf.Max(.01f, motor.model.localScale.y), baseScale);
        }
    }
}
