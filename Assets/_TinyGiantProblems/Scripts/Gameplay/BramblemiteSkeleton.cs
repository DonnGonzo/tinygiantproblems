using UnityEngine;
namespace TinyGiantProblems
{
    [DefaultExecutionOrder(12)]
    public sealed class BramblemiteSkeleton : MonoBehaviour
    {
        public Transform[] joints;
        public Vector3[] restPositions;
        public SkinnedMeshRenderer body,physicsSkin;
        public MeshCollider solid;
        public Mesh collisionRest;
        Mesh posedCollision;
        CareJob job;
        float clock,nextBake;
        void OnEnable(){posedCollision=new Mesh {name="Animated bramblemite collision"};posedCollision.MarkDynamic();job=GetComponentInParent<CareJob>();}
        void OnDisable(){if(solid)solid.sharedMesh=collisionRest;if(posedCollision)Destroy(posedCollision);}
        void Update()
        {
            bool complete=job && job.state.Complete;
            solid.enabled=!complete;
            if(complete){ResetPose();return;}
            var game=Expedition.Instance;
            bool vacuuming=job && game && game.IsPlaying && !game.Paused && game.Nearest==job && game.player.Using && game.player.selected==CareTool.Vacuum;
            clock+=Time.deltaTime*(vacuuming?2.4f:1);
            Pose(clock,vacuuming?1.7f:1);
            if(Time.unscaledTime>=nextBake){physicsSkin.BakeMesh(posedCollision,true);solid.sharedMesh=null;solid.sharedMesh=posedCollision;nextBake=Time.unscaledTime+.05f;}
            Physics.SyncTransforms();
        }
        public void ResetPose(){for(int i=0;i<joints.Length;i++){joints[i].localPosition=restPositions[i];joints[i].localRotation=Quaternion.identity;}}
        public void Pose(float t,float energy=1)
        {
            ResetPose();
            joints[0].localPosition+=Vector3.up*(.004f*(1+Mathf.Sin(t*3)));
            joints[0].localRotation=Quaternion.Euler(0,Mathf.Sin(t*.7f)*2,Mathf.Sin(t*2)*1.5f);
            joints[1].localRotation=Quaternion.Euler(Mathf.Sin(t*1.4f)*2,Mathf.Sin(t*.8f)*4,0);
            for(int side=0;side<2;side++)
            {
                int n=2+side*2;float phase=t*2.2f+side*1.4f;
                joints[n].localRotation=Quaternion.Euler(Mathf.Sin(phase)*7*energy,0,Mathf.Sin(phase*.7f)*8*energy);
                joints[n+1].localRotation=Quaternion.Euler(Mathf.Sin(phase-1)*10*energy,0,Mathf.Sin(phase*.7f-.7f)*7*energy);
                for(int leg=0;leg<3;leg++)
                {
                    int k=6+side*6+leg*2;float tap=Mathf.Max(0,Mathf.Sin(t*4.3f+leg*Mathf.PI*.75f+side*Mathf.PI));
                    joints[k].localRotation=Quaternion.Euler(tap*5*energy,Mathf.Sin(t*4.3f+leg+side*3)*4*energy,(side==0?-1:1)*tap*5*energy);
                    joints[k+1].localRotation=Quaternion.Euler(-tap*8*energy,0,0);
                }
            }
        }
    }
}
