using UnityEngine;
namespace TinyGiantProblems
{
    public sealed class CreatureFrame : MonoBehaviour
    {
        public Transform visual;
        public float clock;
        Vector3 origin;
        void Awake() { origin = transform.position; }
        void Update()
        {
            clock += Time.deltaTime;
            transform.SetPositionAndRotation(origin + new Vector3(Mathf.Sin(clock * .075f) * 6, Mathf.Sin(clock * .7f) * .12f, 0),
                Quaternion.Euler(0, Mathf.Sin(clock * .09f) * 6, 0));
            if (visual) visual.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(clock * .7f) * .45f);
        }
    }
}
