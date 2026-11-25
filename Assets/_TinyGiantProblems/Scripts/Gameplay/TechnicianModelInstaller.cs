using UnityEngine;

namespace TinyGiantProblems
{
    // Swaps the primitive technician (jacket/boots/gloves + clothing kit) for
    // the imported, code-rigged guild technician at load, and moves the held
    // tool rig onto its right hand so tools follow every animation.
    public static class TechnicianModelInstaller
    {
        public const string Key = "technician/technician-rigged";
        public const float Height = 1.6f;

        public static TechnicianRig Install(TechnicianMotor player)
        {
            if (!player || !player.model) return null;
            var existing = player.model.GetComponentInChildren<TechnicianRig>(true);
            if (existing) return existing;
            var go = GameArt.Model(Key, player.model, Height);
            if (!go) return null;
            go.name = "Guild technician";
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            var rig = go.GetComponentInChildren<TechnicianRig>();
            rig.motor = player;
            foreach (var r in go.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

            var tools = player.model.GetComponentInChildren<TechnicianToolVisuals>(true);
            foreach (Transform child in player.model)
            {
                if (child == go.transform || tools && child == tools.transform) continue;
                foreach (var r in child.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            }
            if (tools && rig.handAnchor)
            {
                // Anchor is unscaled in world space so tool sizes stay in metres.
                rig.handAnchor.localScale = Vector3.one / Mathf.Max(.0001f, rig.handAnchor.parent.lossyScale.x);
                tools.transform.SetParent(rig.handAnchor, false);
                tools.transform.localPosition = Vector3.zero;
                tools.transform.localRotation = Quaternion.identity;
                tools.transform.localScale = Vector3.one;
            }
            return rig;
        }
    }
}
