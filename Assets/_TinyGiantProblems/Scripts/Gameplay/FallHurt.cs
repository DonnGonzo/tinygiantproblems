using UnityEngine;
using UnityEngine.UI;

namespace TinyGiantProblems
{
    // Falling a long way never kills: the technician lands winded and is
    // paralysed for a few seconds (TechnicianMotor.stunned), with a caption,
    // a red screen pulse and the rig's slump-and-get-up motion.
    // The drop is measured from the apex of the airborne stretch. Gliding,
    // climbing, standing on the lift (it is ground), warps and the safety winch
    // reset the apex, so none of them can hurt.
    [DefaultExecutionOrder(21)]
    public sealed class FallHurt : MonoBehaviour
    {
        public TechnicianMotor motor;
        public float hurtHeight = 12, maxHeight = 30, minStun = 2, maxStun = 4;
        public float apex;
        public float HurtUntil { get; private set; }
        public float LastDrop { get; private set; }
        public int Falls { get; private set; }
        bool wasGrounded = true;
        Vector3 lastPos;
        int lastRecoveries;
        ToolAbilities abilities;
        Image flash;

        public static bool IsHurt(TechnicianMotor m) { var f = m ? m.GetComponent<FallHurt>() : null; return f && Time.time < f.HurtUntil && m.stunned > 0; }

        void Awake() { if (!motor) motor = GetComponent<TechnicianMotor>(); }
        void Start() { abilities = GetComponent<ToolAbilities>(); lastPos = transform.position; apex = transform.position.y; }

        public void ResetApex() => apex = transform.position.y;

        void LateUpdate()
        {
            if (!motor) return;
            var p = transform.position;
            bool warped = (p - lastPos).sqrMagnitude > 25 * 25 * Time.deltaTime * Time.deltaTime + 4;
            bool recovered = motor.recoveries != lastRecoveries;
            lastRecoveries = motor.recoveries; lastPos = p;
            bool safe = !motor.Active || warped || recovered || motor.climbing || ClimbingClaw.IsClimbing(motor)
                        || (abilities && abilities.Gliding) || (motor.tethered && motor.velocity.y > -6);
            if (safe || motor.grounded && wasGrounded) apex = p.y;
            else apex = Mathf.Max(apex, p.y);

            if (motor.grounded && !wasGrounded && !safe)
            {
                float drop = apex - p.y;
                LastDrop = drop;
                if (drop >= hurtHeight) Hurt(drop);
                apex = p.y;
            }
            wasGrounded = motor.grounded;
            Pulse();
        }

        public void Hurt(float drop)
        {
            float t = Mathf.Lerp(minStun, maxStun, Mathf.InverseLerp(hurtHeight, maxHeight, drop));
            motor.stunned = Mathf.Max(motor.stunned, t);
            motor.velocity = Vector3.zero;
            HurtUntil = Time.time + t;
            Falls++;
            if (Expedition.Instance) Expedition.Instance.Caption = $"[Oof!] A {drop:0} m fall. You're winded. Catch your breath ({t:0.0}s).";
            ProceduralSfx.Play(ProceduralSfx.Pop, 1);
            ProceduralSfx.Play(ProceduralSfx.Deny, .6f);
        }

        void Pulse()
        {
            float left = HurtUntil - Time.time;
            float a = left > 0 ? Mathf.Clamp01(left / .6f) * .35f + Mathf.Sin(Time.time * 6) * .05f : 0;
            if (!flash)
            {
                if (a <= 0) return;
                var root = UIKit.MakeCanvas("Fall hurt overlay", 30, out _);
                flash = UIKit.Panel(root, "Hurt pulse", new Vector2(.5f, .5f), Vector2.zero, new Vector2(4000, 4000), new Color(.8f, .1f, .08f, 0));
            }
            flash.color = new Color(.8f, .1f, .08f, Mathf.Max(0, a));
        }
    }
}
