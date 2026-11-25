using UnityEngine;

namespace TinyGiantProblems
{
    // Feedback for the technician's actions: jump and landing sounds with
    // dust, footsteps, landing shake, hit-stop on big impacts (thrown by a
    // sneeze, stung, hard landing), and chimes/confetti for objectives and
    // finished missions. Lives on the player; installed by the bootstrap.
    [DefaultExecutionOrder(20)]
    public sealed class GameFeel : MonoBehaviour
    {
        TechnicianMotor motor;
        CharacterController body;
        ParticleSystem dust, confetti;
        bool wasGrounded = true;
        float fallSpeed, stepClock;
        int launches, stings;
        MissionManager missions;

        void Start()
        {
            motor = GetComponent<TechnicianMotor>();
            body = GetComponent<CharacterController>();
            motor.Jumped += OnJump;
            dust = GameArt.Particles("Footstep dust", null, new Color(.78f, .72f, .6f, .8f), .35f, 1.6f, .55f);
            var main = dust.main; main.gravityModifier = -.05f;
            confetti = GameArt.Particles("Confetti", null, Color.white, .16f, 7, 2.2f);
            var cm = confetti.main; cm.gravityModifier = .6f;
            cm.startColor = new ParticleSystem.MinMaxGradient(new Color(1, .74f, .3f), new Color(.45f, .8f, .9f)) { mode = ParticleSystemGradientMode.TwoColors };
            launches = motor.launches; stings = motor.stings;
            missions = MissionManager.Instance;
            if (missions) { missions.ObjectiveCompleted += OnObjective; missions.MissionFinished += OnMissionFinished; }
        }

        void OnDestroy()
        {
            if (motor) motor.Jumped -= OnJump;
            if (missions) { missions.ObjectiveCompleted -= OnObjective; missions.MissionFinished -= OnMissionFinished; }
        }

        void OnJump()
        {
            ProceduralSfx.Play(ProceduralSfx.Jump, .45f);
            GameArt.Burst(dust, transform.position + Vector3.up * .1f, 6, Vector3.up * .6f, .8f);
        }

        void OnObjective(ObjectiveDef o) => ProceduralSfx.Play(ProceduralSfx.Objective, .7f);

        void OnMissionFinished(MissionManager.MissionResult r)
        {
            ProceduralSfx.Play(ProceduralSfx.Fanfare, .8f);
            GameArt.Burst(confetti, transform.position + Vector3.up * 2.2f, 70, Vector3.up * 6, 4);
        }

        void Update()
        {
            if (!motor || !motor.Active) { wasGrounded = motor && motor.grounded; fallSpeed = 0; return; }
            bool grounded = motor.grounded;
            if (!grounded) fallSpeed = Mathf.Max(fallSpeed, -motor.velocity.y);
            else if (!wasGrounded) Land(fallSpeed);
            if (grounded) fallSpeed = 0;
            wasGrounded = grounded;

            // Footsteps: cadence follows speed.
            var v = body ? body.velocity : Vector3.zero; v.y = 0;
            float speed = v.magnitude;
            if (grounded && speed > 1.2f && !motor.climbing)
            {
                stepClock += Time.deltaTime * (1.4f + speed * .32f);
                if (stepClock >= 1)
                {
                    stepClock = 0;
                    ProceduralSfx.Play(ProceduralSfx.Step, .18f);
                    if (speed > 5) GameArt.Burst(dust, transform.position + Vector3.up * .05f, 2, Vector3.up * .3f, .5f);
                }
            }
            else stepClock = .6f;

            // Thrown by a sneeze or shake, stung by a mite: a beat of hit-stop.
            if (motor.launches != launches) { launches = motor.launches; HitStop.Do(.07f); ProceduralSfx.Play(ProceduralSfx.Whoosh, .6f); }
            if (motor.stings != stings) { stings = motor.stings; HitStop.Do(.05f); motor.Shake(.25f, .2f); }
        }

        void Land(float speed)
        {
            if (speed < 4) return;
            float k = Mathf.InverseLerp(4, 22, speed);
            ProceduralSfx.Play(speed > 14 ? ProceduralSfx.Thud : ProceduralSfx.Land, .3f + .5f * k);
            GameArt.Burst(dust, transform.position + Vector3.up * .05f, Mathf.RoundToInt(6 + 18 * k), Vector3.up * (.5f + 1.5f * k), 1 + 2 * k);
            if (speed > 9) motor.Shake(.12f + .3f * k, .18f + .15f * k);
            if (speed > 18) HitStop.Do(.05f);
        }
    }

    // A few frames of slow time on a big impact. Skipped while paused or in
    // co-op (time scale would desync the crew).
    public static class HitStop
    {
        internal static float until;
        static HitStopRunner runner;
        public static void Do(float seconds, float scale = .06f)
        {
            var g = Expedition.Instance;
            if (!g || g.Paused || Time.timeScale <= 0) return;
            var net = Unity.Netcode.NetworkManager.Singleton;
            if (net && net.IsListening) return;
            if (!runner) runner = new GameObject("Hit stop").AddComponent<HitStopRunner>();
            until = Mathf.Max(until, Time.unscaledTime + seconds);
            Time.timeScale = scale;
        }
    }

    public sealed class HitStopRunner : MonoBehaviour
    {
        void Update()
        {
            var g = Expedition.Instance;
            if (Time.timeScale > 0 && Time.timeScale < 1 && Time.unscaledTime >= HitStop.until && !(g && g.Paused)) Time.timeScale = 1;
        }
    }
}
