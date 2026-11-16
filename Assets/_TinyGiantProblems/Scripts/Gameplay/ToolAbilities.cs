using UnityEngine;

namespace TinyGiantProblems
{
    // Verbs for the purchasable assist tools. Care tools (brush, vacuum, sprayer,
    // rope launcher) keep working through Expedition and MessField.
    [DefaultExecutionOrder(25)]
    public sealed class ToolAbilities : MonoBehaviour
    {
        public TechnicianMotor motor;
        public float grabRange = 10, scanRadius = 45, snackCooldown = 22, scanCooldown = 10;
        public float SnackReady { get; private set; }
        public float ScanReady { get; private set; }
        public bool Gliding { get; private set; }
        public bool Grabbing { get; private set; }
        public float LastUse { get; private set; } = -10;
        LineRenderer beam;

        void Awake() { if (!motor) motor = GetComponent<TechnicianMotor>(); }

        void Start()
        {
            var go = new GameObject("Grabber beam");
            go.transform.SetParent(transform, false);
            beam = go.AddComponent<LineRenderer>();
            beam.positionCount = 2; beam.startWidth = .05f; beam.endWidth = .02f;
            beam.sharedMaterial = GameArt.Colored(new Color(.55f, .62f, .95f), 0, 3);
            beam.enabled = false;
        }

        void Update()
        {
            Gliding = false; Grabbing = false;
            if (beam) beam.enabled = false;
            var game = Expedition.Instance; var economy = GuildEconomy.Instance;
            if (!game || !economy || !motor.Active) return;
            SnackReady = Mathf.Max(0, SnackReady - Time.deltaTime);
            ScanReady = Mathf.Max(0, ScanReady - Time.deltaTime);
            var held = economy.Inventory.SelectedDef;
            if (held == null) return;
            switch (held.id)
            {
                case ItemId.Grabber: if (motor.Using) Grab(economy); break;
                case ItemId.Scanner: if (motor.UsePressedThisFrame) Scan(game); break;
                case ItemId.SnackLauncher: if (motor.UsePressedThisFrame) Snack(game, economy); break;
                case ItemId.Glider: Glide(); break;
            }
        }

        void Grab(GuildEconomy economy)
        {
            Grabbing = true; LastUse = Time.time;
            var eye = transform.position + Vector3.up * 1.1f;
            var look = motor.orbitCamera ? motor.orbitCamera.transform.forward : motor.model.forward;
            DebrisPickup best = null; float bestScore = float.MaxValue;
            foreach (var d in DebrisPickup.All)
            {
                if (!d || d.Collecting) continue;
                var to = d.transform.position - eye;
                float dist = to.magnitude;
                if (dist > grabRange) continue;
                float facing = Vector3.Dot(to / Mathf.Max(.01f, dist), look);
                if (facing < .55f) continue;
                if (economy.Inventory.RoomFor(d.item) < 1) continue;
                float score = dist * (1.6f - facing);
                if (score < bestScore) { bestScore = score; best = d; }
            }
            if (!best) return;
            best.Attract(transform, 10);
            beam.enabled = true;
            beam.SetPosition(0, eye + look * .6f);
            beam.SetPosition(1, best.transform.position);
        }

        void Scan(Expedition game)
        {
            if (ScanReady > 0) { game.Caption = $"Scanner recharging · {ScanReady:0}s"; return; }
            ScanReady = scanCooldown; LastUse = Time.time;
            var field = MessField.Instance;
            int pinged = 0;
            if (field)
            {
                field.PingAll(transform.position, scanRadius, 9);
                foreach (var s in field.spots) if (s && !s.Cleared && (s.transform.position - transform.position).sqrMagnitude < scanRadius * scanRadius) pinged++;
            }
            int finds = 0;
            foreach (var d in DebrisPickup.All) if (d && ItemCatalog.Get(d.item).sellValue >= 30) finds++;
            game.Caption = $"[Scanner ping] {pinged} messes nearby · {finds} valuable finds loose on the map.";
            ProceduralSfx.Play(ProceduralSfx.Whoosh, .6f);
        }

        void Snack(Expedition game, GuildEconomy economy)
        {
            if (SnackReady > 0) { game.Caption = $"Snack launcher reloading · {SnackReady:0}s"; return; }
            SnackReady = snackCooldown; LastUse = Time.time;
            var frame = game.creature.transform;
            var start = transform.position + Vector3.up * 1.4f;
            var target = frame.TransformPoint(new Vector3(0, 8.4f, 24));
            var go = GameArt.Shape("Mossberry snack", PrimitiveType.Sphere, null, start, Vector3.one * .45f, new Color(.95f, .45f, .55f), .6f, 1.5f);
            go.AddComponent<SnackProjectile>().Launch(start, target, () =>
            {
                game.Comfort = Mathf.Min(100, game.Comfort + 12);
                game.Irritation = Mathf.Max(0, game.Irritation - 20);
                if (game.WarningRemaining <= 0) game.nextReaction += 12;
                game.Caption = "[Happy rumble] Mossback munches the snack. Next reaction delayed.";
                ProceduralSfx.Play(ProceduralSfx.Munch, .8f);
                economy.Report(TaskKind.CalmGiant);
            });
            ProceduralSfx.Play(ProceduralSfx.Whoosh, .5f);
        }

        void Glide()
        {
            if (motor.grounded || motor.climbing || motor.velocity.y > -2f) return;
            Gliding = true;
            motor.velocity.y = Mathf.Max(motor.velocity.y, -3.2f);
            var forward = motor.model.forward;
            motor.velocity.x = Mathf.MoveTowards(motor.velocity.x, forward.x * 6, Time.deltaTime * 4);
            motor.velocity.z = Mathf.MoveTowards(motor.velocity.z, forward.z * 6, Time.deltaTime * 4);
        }
    }

    public sealed class SnackProjectile : MonoBehaviour
    {
        Vector3 a, b; float t, duration = 1.4f; System.Action hit;
        public void Launch(Vector3 from, Vector3 to, System.Action onHit) { a = from; b = to; hit = onHit; duration = Mathf.Clamp(Vector3.Distance(a, b) / 28f, .8f, 2.2f); }
        void Update()
        {
            t += Time.deltaTime / duration;
            var p = Vector3.Lerp(a, b, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * Vector3.Distance(a, b) * .35f;
            transform.position = p;
            transform.Rotate(400 * Time.deltaTime, 250 * Time.deltaTime, 0);
            if (t >= 1) { hit?.Invoke(); Destroy(gameObject); }
        }
    }
}
