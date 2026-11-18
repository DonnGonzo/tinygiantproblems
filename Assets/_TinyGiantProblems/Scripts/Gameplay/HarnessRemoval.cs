using UnityEngine;

namespace TinyGiantProblems
{
    // The fixed wooden harness walkway on Mossback's back is gone: technicians
    // now walk on the creature itself and bring their own Floating Platform.
    // Runs at load, before the stations are built. Everything that used to sit
    // on the walkway (checkpoint, tether winches, care jobs, extraction point,
    // ) is set down onto the back.
    public static class HarnessRemoval
    {
        public static bool Applied { get; private set; }

        public static void Apply(Expedition game)
        {
            if (Applied || !game || !game.creature) return;
            var frame = game.creature.transform;

            foreach (var zone in frame.GetComponentsInChildren<ClimbableZone>(true))
                foreach (Transform part in zone.transform)
                {
                    if (part.GetComponent<CareJob>()) continue;
                    foreach (var r in part.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
                    foreach (var c in part.GetComponentsInChildren<Collider>(true)) c.enabled = false;
                }
            foreach (var name in new[] { "Finished harness rigging", "Guild expedition fittings" })
            {
                var t = frame.Find(name);
                if (t) t.gameObject.SetActive(false);
            }
            Physics.SyncTransforms();

            // The old stops hung over the steep hindquarters; stand them on the
            // level ridge just behind the spine instead (creature-local x/z).
            Stand(frame, game.player.checkpoint, new Vector2(-.8f, -2f), .1f);
            foreach (var a in game.player.anchors) Drop(frame, a, 0);
            foreach (var job in game.jobs) Drop(frame, job.transform, .05f);
            if (game.extraction) Stand(frame, game.extraction, new Vector2(.2f, -2.6f), .05f);
            Applied = true;
        }

        // Highest creature surface under a world point (ignores props and players).
        public static bool Surface(Transform frame, Vector3 world, out RaycastHit hit)
        {
            var origin = new Vector3(world.x, world.y + 30, world.z);
            hit = default; bool found = false;
            foreach (var h in Physics.RaycastAll(origin, Vector3.down, 80, ~0, QueryTriggerInteraction.Ignore))
            {
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
        static void Stand(Transform frame, Transform t, Vector2 local, float lift)
        {
            if (!t) return;
            if (FindStand(frame, local, out var p)) t.position = p + Vector3.up * lift;
            else Drop(frame, t, lift);
        }

        static void Drop(Transform frame, Transform t, float lift)
        {
            if (t && Surface(frame, t.position, out var hit)) t.position = hit.point + Vector3.up * lift;
        }
    }
}
