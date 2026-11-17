using UnityEngine;

namespace TinyGiantProblems
{
    // A ring of imported mountains around the migratory meadow, built at load
    // like WorldStations so the authored scene is untouched. The peaks overlap
    // so there is no visual gap, and an invisible collider wall just inside
    // their feet keeps the technician (and gliders) inside the map.
    public static class MapBoundary
    {
        public const string Key = "mountain/mountain";
        public const float WallRadius = 212, WallHeight = 140;
        public static Transform Root { get; private set; }

        public static void Build(Expedition game)
        {
            if (Root) return;
            var terrain = Terrain.activeTerrain;
            var center = game && game.creature ? new Vector3(game.creature.transform.position.x, 0, game.creature.transform.position.z) : Vector3.zero;
            Root = new GameObject("Mountain ring (map limits)").transform;
            var random = new System.Random(1611);
            const int peaks = 24;
            for (int i = 0; i < peaks; i++)
            {
                float angle = (i + (float)random.NextDouble() * .35f) / peaks * Mathf.PI * 2;
                float radius = 236 + (float)random.NextDouble() * 22;
                float size = 105 + (float)random.NextDouble() * 55;
                var pos = center + new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * radius;
                pos.y = Ground(terrain, pos) - size * .035f;
                var m = Spawn(size);
                if (!m) { Object.Destroy(Root.gameObject); Root = null; return; }
                m.name = "Mountain " + i;
                m.transform.position = pos;
                m.transform.rotation = Quaternion.Euler(0, (float)random.NextDouble() * 360, 0);
                // Taller, narrower variants break up the silhouette.
                float stretch = .85f + (float)random.NextDouble() * .5f;
                m.transform.localScale = new Vector3(size, size * stretch, size);
                foreach (var r in m.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            // Second, closer row of foothills fills the valleys between peaks.
            for (int i = 0; i < peaks; i++)
            {
                float angle = (i + .5f + (float)random.NextDouble() * .2f) / peaks * Mathf.PI * 2;
                float size = 60 + (float)random.NextDouble() * 30;
                var pos = center + new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * (222 + (float)random.NextDouble() * 8);
                pos.y = Ground(terrain, pos) - size * .04f;
                var m = Spawn(size);
                m.name = "Foothill " + i;
                m.transform.position = pos;
                m.transform.rotation = Quaternion.Euler(0, (float)random.NextDouble() * 360, 0);
                m.transform.localScale = new Vector3(size, size * .7f, size);
                foreach (var r in m.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            HideBuriedScenery(center);
            // Invisible boundary: overlapping wall segments on a circle.
            var wall = new GameObject("Map limit wall").transform;
            wall.SetParent(Root, false);
            const int segments = 48;
            float width = 2 * Mathf.PI * WallRadius / segments * 1.15f;
            for (int i = 0; i < segments; i++)
            {
                float a = (i + .5f) / segments * Mathf.PI * 2;
                var seg = new GameObject("Limit " + i).AddComponent<BoxCollider>();
                seg.transform.SetParent(wall, false);
                var dir = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                seg.transform.position = center + dir * (WallRadius + 2) + Vector3.up * (WallHeight * .5f - 10);
                seg.transform.rotation = Quaternion.LookRotation(dir);
                seg.size = new Vector3(width, WallHeight, 4);
            }
        }

        // Outer-forest props whose base ends up inside a mountain slope poke
        // through it; hide those (only props far out, never anything gameplay).
        static void HideBuriedScenery(Vector3 center)
        {
            Physics.SyncTransforms();
            var meadow = GameObject.Find("Migratory meadow");
            if (!meadow) return;
            foreach (var group in meadow.GetComponentsInChildren<Transform>())
            {
                if (group.parent == null || group.parent.parent != meadow.transform) continue; // props: grandchildren of the meadow
                var p = group.position; var flat = new Vector3(p.x - center.x, 0, p.z - center.z);
                if (flat.magnitude < 140) continue;
                if (Physics.Raycast(new Vector3(p.x, 400, p.z), Vector3.down, out var hit, 600, ~0, QueryTriggerInteraction.Ignore)
                    && hit.collider.transform.IsChildOf(Root) && hit.point.y > p.y + 1.5f)
                    group.gameObject.SetActive(false);
            }
        }

        // Unlike GameArt.Model this keeps the prefab's collision mesh, so the
        // mountain slopes are solid and you cannot walk into their bases.
        static GameObject Spawn(float size)
        {
            var lib = ItemLibrary.Instance; var prefab = lib ? lib.Prefab(Key) : null;
            if (!prefab) return null;
            var m = Object.Instantiate(prefab, Root, false);
            m.transform.localScale = Vector3.one * size;
            return m;
        }

        static float Ground(Terrain terrain, Vector3 p) => terrain ? terrain.SampleHeight(p) + terrain.transform.position.y : -5.5f;
    }
}
