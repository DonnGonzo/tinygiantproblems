using UnityEngine;

namespace TinyGiantProblems
{
    [DefaultExecutionOrder(10)]
    public sealed class MossbackSkeleton : MonoBehaviour
    {
        public CreatureFrame creature;
        public Transform[] joints;
        public SkinnedMeshRenderer body, physicsSkin;
        public MeshCollider bodyCollider;
        public Mesh collisionRestMesh;
        public Vector3[] restPositions;
        public float animationSpeed=1;
        [Range(0,1)] public float motionAmount=1;
        Mesh collisionPose;
        float nextCollision;
        MossbackMotion motion;
        public float AnimationTime => creature.clock*animationSpeed;

        void OnEnable()
        {
            collisionPose=new Mesh {name="Mossback animated collision"};
            collisionPose.MarkDynamic();
            nextCollision=0;
            if(creature) motion=creature.GetComponent<MossbackMotion>();
        }
        void OnDisable()
        {
            if(bodyCollider) bodyCollider.sharedMesh=collisionRestMesh;
            if(collisionPose) Destroy(collisionPose);
        }
        void Update()
        {
            Pose(AnimationTime);
            if(Time.unscaledTime>=nextCollision)
            {
                RefreshCollision();
                nextCollision=Time.unscaledTime+(motion && motion.reactionWeight>.1f?0:.05f);
            }
            Physics.SyncTransforms();
        }
        public void RefreshCollision()
        {
            if(!collisionPose) collisionPose=new Mesh {name="Mossback animated collision"};
            // Include scale compensation for the creature's existing 3x parent.
            physicsSkin.BakeMesh(collisionPose,true);
            bodyCollider.sharedMesh=null;
            bodyCollider.sharedMesh=collisionPose;
        }
        public void Pose(float t)
        {
            if(joints==null || joints.Length<21) return;
            ResetPose();
            float a=motionAmount;
            float alert=creature.reactionPulse;
            if(!motion && creature) motion=creature.GetComponent<MossbackMotion>();
            // Slow attentive look, small nods and closed-mouth chewing.
            joints[1].localRotation=Quaternion.Euler(Mathf.Sin(t*.43f)*1.2f*a,Mathf.Sin(t*.26f)*2.4f*a,0);
            joints[2].localRotation=Quaternion.Euler(Mathf.Sin(t*.57f+.5f)*1.8f*a,Mathf.Sin(t*.31f+1)*3.5f*a,Mathf.Sin(t*.39f)*.7f*a);
            var jawAxis=new Vector3(.2f,0,-.98f);
            float chewing=(.5f+.5f*Mathf.Sin(t*2.5f))*(.4f+.6f*Mathf.Pow(.5f+.5f*Mathf.Sin(t*.23f),2));
            joints[3].localRotation=Quaternion.AngleAxis((1+chewing*3.5f)*a,jawAxis);
            joints[4].localRotation=Quaternion.Euler(0,0,(Mathf.Sin(t*.9f)*2+EarFlick(t,0)*12+alert*4)*a);
            joints[5].localRotation=Quaternion.Euler(0,0,-(Mathf.Sin(t*.85f+1)*2+EarFlick(t,4)*12+alert*4)*a);
            joints[6].localRotation=Quaternion.Euler(Mathf.Sin(t*.45f)*3*a,Mathf.Sin(t*.6f)*5*a,0);
            joints[7].localRotation=Quaternion.Euler(0,Mathf.Sin(t*.6f-.6f)*7*a,Mathf.Sin(t*.5f)*2*a);
            joints[8].localRotation=Quaternion.Euler(0,Mathf.Sin(t*.6f-1.1f)*9*a,0);
            if(motion)
            {
                joints[1].localRotation=Quaternion.AngleAxis(motion.neckYaw*a,Vector3.up)*Quaternion.AngleAxis(motion.neckNod*a,jawAxis);
                joints[2].localRotation=Quaternion.AngleAxis(motion.headYaw*a,Vector3.up)*Quaternion.AngleAxis(motion.headNod*a,jawAxis)*Quaternion.AngleAxis(motion.headRoll*a,new Vector3(.98f,0,.2f));
                joints[3].localRotation=Quaternion.AngleAxis(motion.jawOpen*a,jawAxis);
                joints[4].localRotation*=Quaternion.Euler(0,0,motion.earFlap*a);
                joints[5].localRotation*=Quaternion.Euler(0,0,-motion.earFlap*a);
                joints[6].localRotation*=Quaternion.Euler(0,motion.tailWhip*.25f*a,0);
                joints[7].localRotation*=Quaternion.Euler(0,motion.tailWhip*.55f*a,0);
                joints[8].localRotation*=Quaternion.Euler(0,motion.tailWhip*a,0);
            }
            for(int leg=0;leg<4;leg++) PoseLeg(leg,t,a);
        }
        public void ResetPose()
        {
            for(int i=0;i<joints.Length;i++) {joints[i].localPosition=restPositions[i];joints[i].localRotation=Quaternion.identity;}
        }
        static float EarFlick(float t,float offset)
        {
            float phase=Mathf.Repeat(t+offset,11);
            return phase<.65f ? Mathf.Sin(phase/.65f*Mathf.PI*2)*Mathf.Sin(phase/.65f*Mathf.PI) : 0;
        }
        void PoseLeg(int leg,float t,float amount)
        {
            int n=9+leg*3;var hip=joints[n];var knee=joints[n+1];var foot=joints[n+2];
            var hipPoint=transform.InverseTransformPoint(hip.position);
            var kneePoint=transform.InverseTransformPoint(knee.position);
            var footPoint=transform.InverseTransformPoint(foot.position);
            float upper=Vector3.Distance(hipPoint,kneePoint),lower=Vector3.Distance(kneePoint,footPoint);
            // One hoof at a time lifts and settles. No treadmill/sliding walk cycle.
            float phase=Mathf.Repeat(t-leg*4,16);
            float lift=phase<2.4f ? Mathf.Pow(Mathf.Sin(phase/2.4f*Mathf.PI),2) : 0;
            if(motion) lift*=1-Mathf.Clamp01(motion.reactionWeight);
            var world=transform.TransformPoint(footPoint);
            if(Application.isPlaying) world=creature.RestWorldPoint(creature.transform.InverseTransformPoint(world));
            var terrain=Terrain.activeTerrain;
            float ground=terrain ? terrain.SampleHeight(world)+terrain.transform.position.y : -5.5f;
            world.y=ground+.27f*creature.transform.lossyScale.y+lift*.24f*creature.transform.lossyScale.y*amount;
            var target=transform.InverseTransformPoint(world)+new Vector3(.38f,0,.92f)*(Mathf.Sin(phase/2.4f*Mathf.PI*2)*lift*.12f*amount);
            var delta=target-hipPoint;float distance=delta.magnitude;
            // Small extension accommodates the uneven stance in the supplied mesh.
            float extension=Mathf.Max(1,distance/(upper+lower-.02f));upper*=extension;lower*=extension;
            var direction=delta.normalized;
            var bend=Vector3.ProjectOnPlane(new Vector3(.38f,0,.92f),direction).normalized;
            float along=(upper*upper-lower*lower+distance*distance)/(2*Mathf.Max(.001f,distance));
            float height=Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
            var solvedKnee=hipPoint+direction*along+bend*height;
            hip.rotation=transform.rotation*Quaternion.FromToRotation(Vector3.down,solvedKnee-hipPoint);
            knee.position=transform.TransformPoint(solvedKnee);
            knee.rotation=transform.rotation*Quaternion.FromToRotation(Vector3.down,target-solvedKnee);
            foot.position=transform.TransformPoint(target);
            foot.rotation=Quaternion.identity;
        }
    }
}
