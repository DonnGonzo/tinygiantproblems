using UnityEngine;
using UnityEngine.InputSystem;
namespace TinyGiantProblems
{
    public sealed class Expedition : MonoBehaviour
    {
        public static Expedition Instance { get; private set; }
        public TechnicianMotor player;
        public CareJob[] jobs;
        public Transform extraction;
        public CreatureFrame creature;
        public AudioSource music, effects;
        public AudioClip sneeze, shake, complete, spray;
        public ExpeditionPhase Phase = ExpeditionPhase.Briefing;
        public float Comfort = 38, Irritation = 30, Elapsed, WarningRemaining;
        public bool Paused;
        public string Caption = "Welcome to the Small Works Guild. Mossback could use a little help.";
        public string Reaction = "";
        public float nextReaction = 32;
        public int reactionCount;
        public CareJob Nearest { get; private set; }
        public bool IsPlaying => Phase == ExpeditionPhase.Working || Phase == ExpeditionPhase.Extraction;
        public int Completed { get { int n = 0; foreach (var j in jobs) if (j.state.Complete) n++; return n; } }
        public int Reward => CareRules.Reward(Completed, player.recoveries, Elapsed);
        float sprayCooldown;
        public System.Action<CareJob,float> CareRequested;
        public System.Action ExtractionRequested;
        public System.Action MissionBegan;
        // Multicast hooks for economy, mess and debris systems (MissionBegan is owned by CrewSession).
        public event System.Action Began;
        public event System.Action<string> ReactionImpact;
        public bool IsRemote;
        void Awake() { Instance = this; Application.runInBackground = true; }
        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame || Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
            { if (GameUI.CloseTopmost()) { } else if (IsPlaying) SetPause(!Paused); }
            if (!IsPlaying || Paused) return;
            var dt = Time.deltaTime;
            Elapsed += dt;
            Nearest = null; float closest = 3.2f;
            foreach (var job in jobs)
            {
                var d = Vector3.Distance(player.transform.position, job.transform.position);
                if (!job.state.Complete && d < closest) { closest = d; Nearest = job; }
            }
            if (Nearest && player.Using && !player.braced && !player.climbing)
            {
                if (CareRequested != null) CareRequested(Nearest, dt);
                else ApplyCare(Nearest, player.selected, closest, dt);
            }
            if (!IsRemote)
            {
                nextReaction -= dt;
                if (WarningRemaining > 0)
                {
                    WarningRemaining -= dt;
                    if (WarningRemaining <= 0) Impact();
                }
                else if (nextReaction <= 0) Warn();
                Irritation = Mathf.Clamp(Irritation + dt * .12f, 0, 100);
                if (CareRules.CanExtract(Completed, Comfort)) Phase = ExpeditionPhase.Extraction;
            }
            if (Phase == ExpeditionPhase.Extraction && Vector3.Distance(player.transform.position, extraction.position) < 3.5f && player.InteractPressed) Finish();
        }
        public void ApplyCare(CareJob job, CareTool tool, float distance, float dt)
        {
            if (!IsPlaying || Paused || job == null) return;
            if (tool != job.tool)
            {
                var needed = ItemCatalog.Get(ItemCatalog.ForCareTool(job.tool));
                var economy = GuildEconomy.Instance;
                Caption = needed == null ? "This job needs the " + job.tool + "."
                    : economy && !economy.Owns(needed.id) ? $"This job needs the {needed.name}. Buy it at the Guild Tool Locker for {needed.price} coins."
                    : $"This job needs the {needed.name}. Scroll or press its hotbar number.";
                return;
            }
            if (job.state.Apply(job.tool, tool, distance, false, dt))
            {
                Comfort = Mathf.Min(100, Comfort + 20); Irritation = Mathf.Max(0, Irritation - 15);
                effects.PlayOneShot(complete, .8f); Caption = "Care complete: " + job.title + ". Mossback feels better!";
            }
            else if (distance <= 3.2f)
            {
                sprayCooldown -= dt;
                if (sprayCooldown <= 0) { effects.PlayOneShot(spray, .2f); sprayCooldown = 1.8f; }
            }
        }
        public void Warn()
        {
            if(WarningRemaining>0) return;
            reactionCount++;
            Reaction = Random.value < .55f ? "FULL-BODY SHAKE" : "SNEEZE";
            WarningRemaining = 4;
            nextReaction = float.PositiveInfinity;
            creature.GetComponent<MossbackMotion>()?.Warn(Reaction);
            player.Shake(.18f, 4f); // a low rumble you can feel while there is still time to brace
            Caption = Reaction == "SNEEZE" ? "[A huge inhale] Hold F / B to brace, or clip your tether!" : "[Low rumble, rustling fur] Hold F / B to brace!";
        }
        public void Impact()
        {
            creature.GetComponent<MossbackMotion>()?.Impact(Reaction);
            effects.PlayOneShot(Reaction == "SNEEZE" ? sneeze : shake, .75f);
            nextReaction=Random.Range(26f,43f);
            creature.reactionPulse=1.2f;
            WarningRemaining = 0;
            // Only technicians actually standing on (or climbing) Mossback get thrown.
            bool onMossback = player.climbing || Physics.Raycast(player.transform.position + Vector3.up * .5f, Vector3.down, out var under, 2.5f, ~0, QueryTriggerInteraction.Ignore) && under.collider.transform.IsChildOf(creature.transform);
            bool secure = player.braced || player.tethered || !onMossback;
            if (secure) player.Shake(Reaction == "SNEEZE" ? .55f : .8f, .7f);
            else player.Launch(ReactionThrow(Reaction));
            Caption = !onMossback ? "[Distant rumble] Mossback "+(Reaction == "SNEEZE" ? "sneezes" : "shakes")+" out in the meadow." : secure ? "[Secure!] Nice bracing. Back to work." : Reaction == "SNEEZE" ? "[AAAH-CHOO!] Blasted off the back! Brace (F) when Mossback inhales." : "[Whoooosh!] Shaken loose! Brace (F) when the fur starts to rumble.";
            ReactionImpact?.Invoke(Reaction);
            Reaction = "";
            Irritation = Mathf.Max(0, Irritation - 10);
        }
        // How hard an unprepared technician is thrown. Away from Mossback's spine
        // and upward; a sneeze is strongest near the head, a shake everywhere.
        public Vector3 ReactionThrow(string reaction)
        {
            var frame = creature.transform;
            var local = frame.InverseTransformPoint(player.transform.position);
            var outward = frame.TransformDirection(new Vector3(local.x, 0, 0)); outward.y = 0;
            if (outward.sqrMagnitude < .01f) outward = frame.right * (Random.value < .5f ? -1 : 1);
            outward.Normalize();
            if (reaction == "SNEEZE")
            {
                // local z: head end is +z (about +11), tail end about -12.
                float nearHead = Mathf.InverseLerp(-10, 11, local.z);
                float power = Mathf.Lerp(13, 24, nearHead);
                var forward = frame.forward;
                return (outward * .55f + forward * .45f * nearHead).normalized * power + Vector3.up * Mathf.Lerp(9, 13, nearHead);
            }
            return outward * Random.Range(15f, 20f) + Vector3.up * Random.Range(10f, 13f) + frame.forward * Random.Range(-3f, 3f);
        }

        public void Begin(bool replicated = false)
        {
            if (IsRemote && !replicated) { Caption = "The host starts the next expedition."; return; }
            Phase = ExpeditionPhase.Working; Comfort = 38; Irritation = 30; Elapsed = 0; nextReaction = Random.Range(26f,39f);
            creature.reactionPulse=0;creature.GetComponent<MossbackMotion>()?.ResetMotion();
            creature.ResetFramePose();
            WarningRemaining = 0; reactionCount = 0; Reaction = "";
            foreach (var job in jobs) job.state.Progress = 0;
            player.ResetMotor(); SetPause(false);
            Caption = "Walk Mossback's back to each care site and use the matching tool. A Floating Platform gives you somewhere steady to stand.";
            MissionBegan?.Invoke();
            Began?.Invoke();
        }
        public void Finish(bool authoritative = false)
        {
            if (!CareRules.CanExtract(Completed, Comfort)) return;
            if (IsRemote && !authoritative) { ExtractionRequested?.Invoke(); return; }
            Phase = ExpeditionPhase.Debrief;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            PlayerPrefs.SetInt("GuildBest", Mathf.Max(PlayerPrefs.GetInt("GuildBest"), Reward)); PlayerPrefs.Save();
        }
        public void SetPause(bool value)
        {
            Paused = value;
            Time.timeScale = value && !IsRemote && CareRequested == null ? 0 : 1;
            if (!value && GameUI.CapturesInput) return;
            Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked; Cursor.visible = value;
        }
        void OnDestroy() { Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; if (Instance == this) Instance = null; }
    }
}
