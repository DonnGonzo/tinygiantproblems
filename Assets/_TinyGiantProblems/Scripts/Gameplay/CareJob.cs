using UnityEngine;
namespace TinyGiantProblems
{
    public sealed class CareJob : MonoBehaviour
    {
        public string title;
        public string zone;
        public CareTool tool;
        public CareProgress state = new CareProgress();
        public Transform visual;
        public Renderer ring;
        Vector3 initialScale;
        void Start() { if (visual) initialScale = visual.localScale; }
        void Update()
        {
            if (visual)
            {
                visual.localScale = initialScale * (state.Complete ? .15f : 1f - state.Progress * .55f);
                visual.localRotation = Quaternion.Euler(0, Time.time * 20, Mathf.Sin(Time.time * 3) * 8);
            }
            if (ring) ring.material.color = state.Complete ? new Color(.35f, .85f, .6f) : new Color(1, .72f, .25f);
        }
    }
}
