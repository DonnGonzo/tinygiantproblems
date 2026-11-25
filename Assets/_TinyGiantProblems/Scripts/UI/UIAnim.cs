using UnityEngine;

namespace TinyGiantProblems
{
    // Small, allocation-free UI motion. Panels with UIAnim fade and scale in
    // whenever they are shown; Punch() gives any element a quick bounce
    // (objective done, warning tick). Runs on unscaled time so it works in
    // the pause menu and during hit-stop.
    public sealed class UIAnim : MonoBehaviour
    {
        public float duration = .18f, fromScale = .94f;
        CanvasGroup group; float t;

        public static UIAnim Add(GameObject go, float fromScale = .94f)
        {
            if (!go.TryGetComponent<UIAnim>(out var a)) a = go.AddComponent<UIAnim>();
            a.fromScale = fromScale;
            return a;
        }

        void OnEnable()
        {
            if (!group && !TryGetComponent(out group)) group = gameObject.AddComponent<CanvasGroup>();
            t = 0; Apply();
        }

        void Update()
        {
            if (t >= 1) return;
            t = Mathf.Min(1, t + Time.unscaledDeltaTime / duration);
            Apply();
        }

        void Apply()
        {
            float k = 1 - (1 - t) * (1 - t) * (1 - t);   // ease out cubic
            group.alpha = k;
            transform.localScale = Vector3.one * Mathf.LerpUnclamped(fromScale, 1, k);
        }

        public static void Punch(RectTransform rt, float amount = .08f)
        {
            if (!rt) return;
            if (!rt.TryGetComponent<UIPunch>(out var p)) p = rt.gameObject.AddComponent<UIPunch>();
            p.Kick(amount);
        }
    }

    public sealed class UIPunch : MonoBehaviour
    {
        float amount, t = 1;
        public void Kick(float a) { amount = a; t = 0; enabled = true; }
        void Update()
        {
            t = Mathf.Min(1, t + Time.unscaledDeltaTime / .28f);
            float s = 1 + amount * Mathf.Sin(t * Mathf.PI) * (1 - t * .5f);
            transform.localScale = new Vector3(s, s, 1);
            if (t >= 1) { transform.localScale = Vector3.one; enabled = false; }
        }
    }
}
