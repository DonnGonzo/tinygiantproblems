using System.Collections.Generic;
using UnityEngine;

namespace TinyGiantProblems
{
    // One piece of visible mess living on Mossback: grime, a burr or a crawling mite.
    public sealed class MessSpot : MonoBehaviour
    {
        public MessKind kind;
        public float progress;
        public ItemId hiddenFind;
        public float lastWorked = -10;
        public bool Cleared { get; private set; }
        public Transform visual;
        public Vector3 normal = Vector3.up;
        Vector3 restScale, homeLocal;
        float phase, popTime = -1, ping;
        GameObject beacon, glint;
        Transform frame;

        public CareTool Tool => kind == MessKind.Mite ? CareTool.Vacuum : CareTool.Groomer;
        public float CleanSeconds => kind == MessKind.Grime ? 1.6f : kind == MessKind.Burr ? 2.1f : 1.1f;
        public string Label => kind == MessKind.Grime ? "Grime patch" : kind == MessKind.Burr ? "Burr" : "Bramblemite";

        public void Init(MessKind k, Transform v, Transform creatureFrame, float seed)
        {
            kind = k; visual = v; frame = creatureFrame; phase = seed;
            restScale = v.localScale; homeLocal = transform.localPosition;
        }

        public void AddGlint()
        {
            glint = GameArt.Shape("Something shiny", PrimitiveType.Sphere, transform, new Vector3(.2f, .14f, .1f), Vector3.one * .09f, new Color(1, .95f, .7f), .9f, 6);
        }

        public void Ping(float seconds)
        {
            ping = seconds;
            if (!beacon)
            {
                var color = kind == MessKind.Mite ? new Color(1, .35f, .3f) : hiddenFind != ItemId.None ? new Color(1, .85f, .3f) : new Color(.45f, .95f, 1);
                beacon = GameArt.Shape("Scanner ping", PrimitiveType.Cylinder, transform, Vector3.up * 1.6f, new Vector3(.08f, 1.6f, .08f), color, 0, 4);
            }
        }

        public void Clear()
        {
            if (Cleared) return;
            Cleared = true; popTime = 0;
            if (beacon) Destroy(beacon);
            if (glint) Destroy(glint);
        }

        void Update()
        {
            float t = Time.time + phase;
            if (popTime >= 0)
            {
                popTime += Time.deltaTime;
                float k = popTime / .28f;
                if (visual) visual.localScale = restScale * Mathf.Max(0, (1 + k * 1.2f) * (1 - k));
                if (k >= 1) Destroy(gameObject);
                return;
            }
            bool worked = Time.time - lastWorked < .15f;
            float shrink = 1 - progress * .55f;
            float jiggle = worked ? 1 + Mathf.Sin(t * 42) * .08f : 1;
            float pingPulse = ping > 0 ? 1 + Mathf.Sin(t * 10) * .15f : 1;
            if (visual) visual.localScale = restScale * shrink * jiggle * pingPulse;
            if (kind == MessKind.Mite && !worked && frame)
            {
                // Mites wander in slow loops over the coat until vacuumed.
                float a = t * .7f;
                var offset = (frame.right * Mathf.Cos(a) + frame.forward * Mathf.Sin(a * 1.3f)) * .55f;
                var home = transform.parent ? transform.parent.TransformPoint(homeLocal) : homeLocal;
                transform.position = home + offset + normal * Mathf.Abs(Mathf.Sin(t * 9)) * .05f;
                var heading = -frame.right * Mathf.Sin(a) + frame.forward * Mathf.Cos(a * 1.3f) * 1.3f;
                if (heading.sqrMagnitude > .001f) transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(heading, normal), normal);
            }
            if (glint) glint.transform.localScale = Vector3.one * (.05f + Mathf.Max(0, Mathf.Sin(t * 3)) * .12f);
            if (ping > 0)
            {
                ping -= Time.deltaTime;
                if (ping <= 0 && beacon) Destroy(beacon);
            }
        }
    }

    // Spawns and tracks the mess that visibly covers Mossback, handles the brush
    // and vacuum verbs against it, and brightens the coat as it gets cleaner.
    [DefaultExecutionOrder(30)]
    public sealed class MessField : MonoBehaviour
    {
        public static MessField Instance { get; private set; }
        public int grimeCount = 14, burrCount = 10, miteCount = 8, hiddenFinds = 3;
        public float reach = 4.2f;
        public readonly List<MessSpot> spots = new List<MessSpot>();
        public MessSpot Current { get; private set; }
        public MessSpot Aimed { get; private set; }
        public int Total { get; private set; }
        public int ClearedCount { get; private set; }
        public float Cleanliness => Total == 0 ? 1 : ClearedCount / (float)Total;

        Expedition game;
        CreatureFrame frame;
        MossbackSkeleton skeleton;
        Transform root;
        readonly List<Renderer> coat = new List<Renderer>();
        readonly List<Color> coatBase = new List<Color>();
        MaterialPropertyBlock block;
        ParticleSystem dust, puff;
        float shownClean, soundTimer, hintTimer;
        int seed = 1207;
        static readonly Color DirtyTint = new Color(.6f, .53f, .42f);

        void Awake() { Instance = this; }
        void OnDestroy() { if (game) game.Began -= Respawn; if (Instance == this) Instance = null; }

        void Start()
        {
            game = Expedition.Instance;
            if (!game || !game.creature) { enabled = false; return; }
            frame = game.creature;
            skeleton = frame.GetComponentInChildren<MossbackSkeleton>();
            root = new GameObject("Mossback mess").transform;
            root.SetParent(frame.transform, false);
            dust = GameArt.Particles("Brush dust", null, new Color(.55f, .45f, .3f, .9f), .35f, 2.5f, .7f);
            var dm = dust.main; dm.gravityModifier = .6f;
            puff = GameArt.Particles("Mess pop", null, new Color(.9f, .95f, .75f, .9f), .5f, 4, .6f);
            FindCoat();
            game.Began += Respawn;
            Respawn();
        }

        void FindCoat()
        {
            coat.Clear(); coatBase.Clear();
            if (skeleton && skeleton.body) coat.Add(skeleton.body);
            else if (frame.visual)
                foreach (var r in frame.visual.GetComponentsInChildren<Renderer>())
                    if (r.bounds.size.magnitude > 25) coat.Add(r);
            foreach (var r in coat)
            {
                var m = r.sharedMaterial;
                coatBase.Add(m && m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white);
            }
            block = new MaterialPropertyBlock();
        }

        public void Respawn()
        {
            foreach (var s in spots) if (s) Destroy(s.gameObject);
            spots.Clear();
            ClearedCount = 0; Total = 0; shownClean = 0;
            var rng = new System.Random(seed++);
            Physics.SyncTransforms();
            Spawn(MessKind.Grime, grimeCount, rng);
            Spawn(MessKind.Burr, burrCount, rng);
            Spawn(MessKind.Mite, miteCount, rng);
            // Some grime hides a collectable find that only shows once scrubbed away.
            var grime = spots.FindAll(s => s.kind == MessKind.Grime);
            for (int i = 0; i < hiddenFinds && grime.Count > 0; i++)
            {
                var g = grime[rng.Next(grime.Count)]; grime.Remove(g);
                g.hiddenFind = i == 0 ? ItemId.GuildBadge : ItemId.ShinyPebble;
                g.AddGlint();
            }
            Total = spots.Count;
        }

        float Range(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);

        void Spawn(MessKind kind, int count, System.Random rng)
        {
            int placed = 0;
            for (int attempt = 0; attempt < count * 30 && placed < count; attempt++)
            {
                bool fur = rng.NextDouble() < .55;
                float side = rng.NextDouble() < .5 ? -1 : 1;
                float x = fur ? side * Range(rng, 5.15f, 5.9f) : Range(rng, -4.2f, 4.2f);
                float z = Range(rng, -8.6f, 17.8f);
                var local = new Vector3(x, 0, z);
                bool blocked = false;
                foreach (var job in game.jobs)
                {
                    if (!job) continue;
                    var j = frame.transform.InverseTransformPoint(job.transform.position);
                    if (Vector2.Distance(new Vector2(x, z), new Vector2(j.x, j.z)) < 1.3f) blocked = true;
                }
                foreach (var s in spots)
                {
                    var o = frame.transform.InverseTransformPoint(s.transform.position);
                    if (Vector2.Distance(new Vector2(o.x, o.z), new Vector2(x, z)) < .9f) blocked = true;
                }
                if (blocked) continue;
                if (!Surface(local, fur, out var hit)) continue;
                Create(kind, hit, rng);
                placed++;
            }
        }

        bool Surface(Vector3 local, bool fur, out RaycastHit best)
        {
            best = default;
            var origin = frame.transform.TransformPoint(new Vector3(local.x, 24, local.z));
            var hits = Physics.RaycastAll(origin, -frame.transform.up, 120, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                if (!h.collider.transform.IsChildOf(frame.transform)) continue;
                var n = h.collider.name;
                if (n.StartsWith("Climbable flank") || n.StartsWith("AnchorSocket") || n.StartsWith("Guild supply")) continue;
                bool deck = n.EndsWith(" traversal");
                if (fur && deck) continue;
                if (!fur && !deck) return false;
                if (Vector3.Dot(h.normal, frame.transform.up) < .35f) return false;
                best = h; return true;
            }
            return false;
        }

        Transform Anchor(RaycastHit hit)
        {
            if (hit.collider.name.EndsWith(" traversal")) return hit.collider.transform.parent ? hit.collider.transform.parent : frame.transform;
            if (skeleton && skeleton.joints != null && skeleton.joints.Length > 0)
            {
                Transform best = null; float d = float.MaxValue;
                foreach (var j in skeleton.joints) if (j) { var dd = (j.position - hit.point).sqrMagnitude; if (dd < d) { d = dd; best = j; } }
                if (best) return best;
            }
            return frame.transform;
        }

        void Create(MessKind kind, RaycastHit hit, System.Random rng)
        {
            var go = new GameObject(kind + " mess");
            var t = go.transform;
            t.SetParent(Anchor(hit), true);
            t.position = hit.point + hit.normal * .02f;
            t.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * Quaternion.Euler(0, Range(rng, 0, 360), 0);
            var ls = t.parent.lossyScale;
            t.localScale = new Vector3(1 / ls.x, 1 / ls.y, 1 / ls.z);
            var v = new GameObject("Visual").transform; v.SetParent(t, false);
            switch (kind)
            {
                case MessKind.Grime:
                    {
                        var mud = new Color(.27f, .2f, .12f);
                        float w = Range(rng, .9f, 1.3f);
                        GameArt.Shape("Mud", PrimitiveType.Sphere, v, Vector3.zero, new Vector3(w, .16f, w * .8f), mud, .7f);
                        GameArt.Shape("Mud", PrimitiveType.Sphere, v, new Vector3(w * .35f, 0, w * .2f), new Vector3(w * .6f, .2f, w * .55f), mud * 1.15f, .7f);
                        GameArt.Shape("Mud", PrimitiveType.Sphere, v, new Vector3(-w * .3f, 0, -w * .25f), new Vector3(w * .5f, .14f, w * .45f), mud * .85f, .7f);
                        for (int i = 0; i < 3; i++)
                            GameArt.Shape("Grit", PrimitiveType.Sphere, v, new Vector3(Range(rng, -.4f, .4f) * w, .08f, Range(rng, -.3f, .3f) * w), Vector3.one * .1f, new Color(.4f, .32f, .22f));
                        break;
                    }
                case MessKind.Burr:
                    {
                        var m = GameArt.Model("burr-cluster/burr-cluster", v, .85f);
                        if (!m) GameArt.ItemVisual(ItemCatalog.Get(ItemId.Burr), v, .7f);
                        break;
                    }
                default:
                    {
                        var m = GameArt.Model("bramblenmite/bramblenmite", v, .75f);
                        if (!m)
                        {
                            GameArt.Shape("Mite body", PrimitiveType.Sphere, v, Vector3.up * .25f, new Vector3(.5f, .35f, .6f), new Color(.75f, .23f, .23f), .5f);
                            for (int i = 0; i < 6; i++)
                                GameArt.Shape("Leg", PrimitiveType.Sphere, v, new Vector3((i % 2 == 0 ? -.3f : .3f), .1f, -.2f + (i / 2) * .2f), Vector3.one * .12f, new Color(.1f, .08f, .08f));
                        }
                        break;
                    }
            }
            var spot = go.AddComponent<MessSpot>();
            spot.normal = hit.normal;
            spot.Init(kind, v, frame.transform, Range(rng, 0, 50));
            spots.Add(spot);
        }

        void Update()
        {
            Current = null; Aimed = null;
            if (!game || !game.IsPlaying || game.Paused) return;
            var p = game.player;
            var economy = GuildEconomy.Instance;
            var held = economy ? economy.Inventory.SelectedDef : null;
            CareTool tool = held != null ? held.care : p.selected;
            var center = p.transform.position + Vector3.up * .9f;
            var look = p.orbitCamera ? Vector3.ProjectOnPlane(p.orbitCamera.transform.forward, Vector3.up).normalized : p.model.forward;
            float bestScore = float.MaxValue; MessSpot bestMatch = null;
            float aimedScore = float.MaxValue;
            foreach (var s in spots)
            {
                if (!s || s.Cleared) continue;
                var to = s.transform.position - center;
                float d = to.magnitude;
                if (d > reach) continue;
                float facing = Vector3.Dot(to.normalized, look);
                float score = d - facing * 1.2f;
                if (score < aimedScore) { aimedScore = score; Aimed = s; }
                if (s.Tool == tool && score < bestScore) { bestScore = score; bestMatch = s; }
            }
            if (bestMatch) Aimed = bestMatch;
            bool jobTakesPriority = game.Nearest && game.Nearest.tool == tool;
            if (p.Using && !p.braced && !p.climbing && !jobTakesPriority)
            {
                if (bestMatch)
                {
                    Current = bestMatch;
                    Work(bestMatch, economy ? economy.CleanSpeed : 1);
                }
                else if (Aimed && (hintTimer -= Time.deltaTime) <= 0)
                {
                    var need = ItemCatalog.Get(ItemCatalog.ForCareTool(Aimed.Tool));
                    game.Caption = $"{Aimed.Label}: use the {need.name}.";
                    hintTimer = 2;
                }
            }
        }

        void Work(MessSpot s, float speed)
        {
            s.progress += Time.deltaTime * speed / s.CleanSeconds;
            s.lastWorked = Time.time;
            if (s.kind != MessKind.Mite && Random.value < .5f)
                GameArt.Burst(dust, s.transform.position + s.normal * .25f, 1, s.normal * 1.5f, 1.2f);
            soundTimer -= Time.deltaTime;
            if (soundTimer <= 0)
            {
                ProceduralSfx.Play(s.kind == MessKind.Mite ? ProceduralSfx.Suck : ProceduralSfx.Scrub, .45f);
                soundTimer = s.kind == MessKind.Mite ? .22f : .17f;
            }
            if (s.progress >= 1) Finish(s);
        }

        void Finish(MessSpot s)
        {
            s.Clear();
            ClearedCount++;
            GameArt.Burst(puff, s.transform.position + s.normal * .3f, 14, s.normal * 2, 2.2f);
            ProceduralSfx.Play(ProceduralSfx.Pop, .7f);
            game.Comfort = Mathf.Min(100, game.Comfort + 1.2f);
            game.Irritation = Mathf.Max(0, game.Irritation - 1);
            var economy = GuildEconomy.Instance;
            if (economy) economy.Report(TaskKind.ClearMess, 1, (int)s.kind);
            var pos = s.transform.position + s.normal * .45f;
            var fling = s.normal * 3.2f + Vector3.up * 2.5f;
            switch (s.kind)
            {
                case MessKind.Grime:
                    DebrisPickup.Spawn(ItemId.MossClump, pos, fling + Random.insideUnitSphere * 1.5f);
                    if (Random.value < .4f) DebrisPickup.Spawn(ItemId.MossClump, pos, fling + Random.insideUnitSphere * 2f);
                    if (s.hiddenFind != ItemId.None)
                    {
                        DebrisPickup.Spawn(s.hiddenFind, pos, Vector3.up * 4 + Random.insideUnitSphere);
                        game.Caption = "Something glints where the grime was. Grab it!";
                    }
                    break;
                case MessKind.Burr:
                    DebrisPickup.Spawn(ItemId.Burr, pos, fling + Random.insideUnitSphere * 1.5f);
                    break;
                case MessKind.Mite:
                    if (!economy || economy.Collect(ItemId.PestJar, 1) > 0) DebrisPickup.Spawn(ItemId.PestJar, pos, fling);
                    else game.Caption = "Bramblemite bottled. Sell the jar at the salvage yard.";
                    break;
            }
            if (ClearedCount == Total) game.Caption = "Every patch of mess is gone. Mossback looks magnificent!";
        }

        public void PingAll(Vector3 from, float radius, float seconds)
        {
            foreach (var s in spots)
                if (s && !s.Cleared && (s.transform.position - from).sqrMagnitude < radius * radius) s.Ping(seconds);
        }

        void LateUpdate()
        {
            if (coat.Count == 0 || block == null) return;
            shownClean = Mathf.MoveTowards(shownClean, Cleanliness, Time.deltaTime * .25f);
            var tint = Color.Lerp(DirtyTint, Color.white, Mathf.Pow(shownClean, .8f));
            for (int i = 0; i < coat.Count; i++)
            {
                var r = coat[i]; if (!r) continue;
                r.GetPropertyBlock(block);
                block.SetColor("_BaseColor", coatBase[i] * tint);
                r.SetPropertyBlock(block);
            }
        }
    }
}
