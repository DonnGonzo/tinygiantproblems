using UnityEngine;
namespace TinyGiantProblems
{
    public sealed class CreatureFrame : MonoBehaviour
    {
        public Transform visual;
        public float clock;
        public float reactionPulse;
        Vector3 origin, restScale;
        Quaternion restRotation;
        MossbackMotion motion;
        public Vector3 RestWorldPoint(Vector3 point) => origin+restRotation*Vector3.Scale(point,restScale);
        public void ResetFramePose()
        {
            clock=0;reactionPulse=0;transform.localScale=restScale;transform.SetPositionAndRotation(origin,restRotation);Physics.SyncTransforms();
        }
        [Header("Living idle motion")]
        [Min(2)] public float breathSeconds = 7.5f;
        [Range(0,.02f)] public float breathAmount = .008f;
        [Range(0,1)] public float weightShiftDegrees = .35f;
        // Entire creature frame animates together: coat, collision, harness and sockets.
        // MossbackSkeleton adds local head, jaw, ear, tail and leg articulation.
        void Awake()
        {
            origin = transform.position;
            restScale = transform.localScale;
            restRotation = transform.rotation;
            motion=GetComponent<MossbackMotion>();
        }
        void Update()
        {
            clock += Time.deltaTime;
            reactionPulse=Mathf.Max(0,reactionPulse-Time.deltaTime);
            float warning=Expedition.Instance ? Expedition.Instance.WarningRemaining : 0;
            float breath=Mathf.Sin(clock*Mathf.PI*2/Mathf.Max(2,breathSeconds));
            float warningBlend=warning>0 ? Mathf.Clamp01((4-warning)/2) : 0;
            float shake=motion?0:Mathf.Sin(clock*18)*reactionPulse*1.3f;
            float roll=Mathf.Sin(clock*.48f)*weightShiftDegrees+Mathf.Sin(clock*9)*warningBlend*.18f+shake;
            float pitch=Mathf.Sin(clock*.31f)*.12f-warningBlend*.22f+shake*.3f;
            var scale=Vector3.Scale(restScale,new Vector3(1+breath*breathAmount*.35f,1+breath*breathAmount,1+breath*breathAmount*.2f));
            var rotation=restRotation*Quaternion.Euler(pitch,Mathf.Sin(clock*.19f)*.45f,roll);
            if(motion) rotation*=Quaternion.AngleAxis(motion.bodyRoll,new Vector3(.38f,0,.92f));
            // Pivot breathing and weight shifts at ground level, not at the torso centre.
            var footPivot=new Vector3(0,-5.5f,0);
            var groundPivot=origin+restRotation*Vector3.Scale(footPivot,restScale);
            transform.localScale=scale;
            transform.SetPositionAndRotation(groundPivot-rotation*Vector3.Scale(footPivot,scale),rotation);
            if (visual) visual.localRotation = Quaternion.identity;
            // The technician runs after this Update. Its casts and controller
            // movement must see this frame's deck pose, including between physics ticks.
            Physics.SyncTransforms();
        }
    }
}
