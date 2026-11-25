using UnityEngine;

namespace TinyGiantProblems
{
    // Queries against Mossback's own body surface (the back the technician
    // walks on), ignoring props, placed objects and care jobs.
    public static class MossbackSurface
    {
        // Highest creature surface under a world point (ignores props and players).
        static readonly RaycastHit[] hits = new RaycastHit[32];
        public static bool Surface(Transform frame, Vector3 world, out RaycastHit hit)
        {
            var origin = new Vector3(world.x, world.y + 30, world.z);
            hit = default; bool found = false;
            int n = Physics.RaycastNonAlloc(origin, Vector3.down, hits, 80, ~0, QueryTriggerInteraction.Ignore);   // mites call this every frame
            for (int i = 0; i < n; i++)
            {
                var h = hits[i];
                if (!h.collider.transform.IsChildOf(frame) || h.collider.GetComponentInParent<PropCollisionPolicy>()) continue;
                if (h.collider.GetComponentInParent<PlacedObject>() || h.collider.GetComponentInParent<CareJob>()) continue;
                if (!found || h.point.y > hit.point.y) { hit = h; found = true; }
            }
            return found;
        }

        // Nearest level, high spot of the back to a creature-local x/z point.
        public static bool FindStand(Transform frame, Vector2 local, out Vector3 world)
        {
            world = default; float best = float.MaxValue; bool found = false;
            for (float dx = -2.4f; dx <= 2.4f; dx += .4f)
                for (float dz = -2.4f; dz <= 2.4f; dz += .4f)
                {
                    var p = frame.TransformPoint(new Vector3(local.x + dx, 20, local.y + dz));
                    if (!Surface(frame, p, out var hit) || hit.normal.y < .93f) continue;
                    // Level enough to stand: the ground half a metre around is at the same height.
                    bool level = true;
                    foreach (var o in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
                        if (!Surface(frame, hit.point + o * .6f, out var n) || Mathf.Abs(n.point.y - hit.point.y) > .35f) level = false;
                    if (!level) continue;
                    float score = new Vector2(dx, dz).sqrMagnitude;
                    if (score < best) { best = score; world = hit.point + Vector3.up * .02f; found = true; }
                }
            return found;
        }
        public static void Stand(Transform frame, Transform t, Vector2 local, float lift)
        {
            if (!t) return;
            if (FindStand(frame, local, out var p)) t.position = p + Vector3.up * lift;
            else Drop(frame, t, lift);
        }

        public static void Drop(Transform frame, Transform t, float lift)
        {
            if (t && Surface(frame, t.position, out var hit)) t.position = hit.point + Vector3.up * lift;
        }
    }
}
