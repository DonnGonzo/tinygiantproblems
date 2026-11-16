using System.Collections.Generic;
using UnityEngine;

namespace TinyGiantProblems
{
    // Procedural body animation for the technician: walk bob, stepping boots,
    // swinging gloves, climbing reach, jump stretch and landing squash.
    [DefaultExecutionOrder(45)]
    public sealed class TechnicianAnimator : MonoBehaviour
    {
        public TechnicianMotor motor;
        readonly List<(Transform t, Vector3 pos, Quaternion rot)> body = new List<(Transform, Vector3, Quaternion)>();
        readonly List<(Transform t, Vector3 pos, float side)> boots = new List<(Transform, Vector3, float)>();
        readonly List<(Transform t, Vector3 pos, float side)> gloves = new List<(Transform, Vector3, float)>();
        float phase, squash, airTime;
        bool wasGrounded = true;

        void Start()
        {
            if (!motor) motor = GetComponent<TechnicianMotor>();
            if (!motor || !motor.model) { enabled = false; return; }
            foreach (Transform child in motor.model)
            {
                if (child.name == "Boot") boots.Add((child, child.localPosition, Mathf.Sign(child.localPosition.x)));
                else if (child.name == "Glove") gloves.Add((child, child.localPosition, Mathf.Sign(child.localPosition.x)));
                else body.Add((child, child.localPosition, child.localRotation));
            }
        }

        void LateUpdate()
        {
            if (!motor || !motor.model) return;
            float dt = Time.deltaTime;
            var v = motor.capsule ? motor.capsule.velocity : Vector3.zero;
            float speed = new Vector2(v.x, v.z).magnitude;
            float walk = motor.grounded && !motor.braced ? Mathf.Clamp01(speed / 5f) : 0;
            phase += dt * (6 + speed * 1.6f) * (walk > .05f || motor.climbing ? 1 : 0);
            if (motor.grounded && !wasGrounded && airTime > .25f) squash = Mathf.Clamp01(airTime * .8f);
            airTime = motor.grounded ? 0 : airTime + dt;
            wasGrounded = motor.grounded;
            squash = Mathf.MoveTowards(squash, 0, dt * 4);
            float stretch = !motor.grounded && !motor.climbing ? Mathf.Clamp(motor.velocity.y * .015f, -.08f, .1f) : 0;
            float bob = Mathf.Abs(Mathf.Sin(phase)) * .07f * walk - squash * .12f;
            float lean = walk * 3;
            foreach (var b in body)
            {
                if (!b.t) continue;
                // Held tools follow the hand; everything else bobs as one torso.
                if (b.t.GetComponent<TechnicianToolVisuals>()) { b.t.localPosition = b.pos + Vector3.up * bob * .6f; continue; }
                b.t.localPosition = new Vector3(b.pos.x, b.pos.y * (1 + stretch - squash * .15f) + bob, b.pos.z);
                b.t.localRotation = Quaternion.Euler(lean, 0, Mathf.Sin(phase) * 2.5f * walk) * b.rot;
            }
            foreach (var b in boots)
            {
                if (!b.t) continue;
                float s = Mathf.Sin(phase + (b.side > 0 ? Mathf.PI : 0));
                b.t.localPosition = b.pos + new Vector3(0, Mathf.Max(0, s) * .12f * walk, s * .22f * walk);
            }
            foreach (var g in gloves)
            {
                if (!g.t) continue;
                float s = Mathf.Sin(phase + (g.side > 0 ? 0 : Mathf.PI));
                var pos = g.pos + new Vector3(0, bob, -s * .18f * walk);
                if (motor.climbing) pos = g.pos + new Vector3(0, .55f + Mathf.Max(0, s) * .25f, .25f);
                else if (motor.braced) pos = g.pos + new Vector3(0, -.25f, .15f);
                g.t.localPosition = pos;
            }
        }
    }
}
