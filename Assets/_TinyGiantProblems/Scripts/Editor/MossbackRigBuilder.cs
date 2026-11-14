using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TinyGiantProblems.Editor
{
    public static class MossbackRigBuilder
    {
        const string Folder="Assets/_TinyGiantProblems/Art/MossbackRig";
        static readonly Vector3[] Feet={new Vector3(1.12f,-4.54f,15.49f),new Vector3(4.94f,-4.20f,4.81f),new Vector3(-5.81f,-5.23f,-.89f),new Vector3(-.49f,-4.66f,-8.52f)};
        static float S(float a,float b,float x)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,x));
        static float Ellipsoid(Vector3 p,Vector3 center,Vector3 size)=>Vector3.Scale(p-center,new Vector3(1/size.x,1/size.y,1/size.z)).magnitude;
        static float EyeLock(Vector3 p)
        {
            // Eyes are part of the supplied mesh, not separate animated objects.
            // Keep each eye and its lids rigid to the skull, with a broad cheek blend.
            var size=new Vector3(1.5f,1,1.35f);
            float distance=Mathf.Min(Ellipsoid(p,new Vector3(6.68f,8.11f,11.4f),size),Ellipsoid(p,new Vector3(5.85f,8.21f,16.9f),size));
            return 1-S(1,2.5f,distance);
        }
        static BoneWeight Weight(Vector3 p)
        {
            var weights=new float[21];weights[0]=1;
            // The entire deck-bearing upper back remains rooted to the frame.
            float underDeck=(1-S(1.35f,6.5f,Mathf.Abs(p.x)))*(1-S(11.25f,16,p.z));
            float head=(1-S(.55f,1.45f,Ellipsoid(p,new Vector3(4.7f,6.5f,14.8f),new Vector3(8,7,10))))*S(1,4,p.y)*(1-S(9.5f,11.8f,p.y)*underDeck);
            float jaw=head*S(5.3f,7.2f,p.x)*(1-S(4.3f,5.6f,p.y))*S(2.9f,3.7f,p.y)*S(9,11,p.z)*(1-S(18,20,p.z));
            float leftEar=(1-S(.35f,1.3f,Ellipsoid(p,new Vector3(3.5f,9.6f,19),new Vector3(2.6f,2.8f,3.8f))))*S(6.5f,9.5f,p.y);
            float rightEar=(1-S(.35f,1.3f,Ellipsoid(p,new Vector3(6,9,9.8f),new Vector3(3.2f,2.8f,3.5f))))*S(6,9,p.y);
            head=Mathf.Max(head,Mathf.Max(leftEar,rightEar));
            weights[0]=1-head;weights[1]=head*.18f;weights[2]=head*.82f;
            float detailed=jaw+leftEar+rightEar;
            if(detailed>head && detailed>0) {float k=head/detailed;jaw*=k;leftEar*=k;rightEar*=k;}
            float remaining=head-jaw-leftEar-rightEar;
            weights[1]=remaining*.18f;weights[2]=remaining*.82f;weights[3]=jaw;weights[4]=leftEar;weights[5]=rightEar;
            float tail=(1-S(-15,-11,p.z))*(1-S(5,8,p.y));
            // Tail tuft rises above its stalk, so retain its terminal bone weight.
            tail=Mathf.Max(tail,1-S(-18,-16,p.z));
            if(tail>0) {for(int i=0;i<6;i++) weights[i]*=1-tail;float tip=1-S(-19,-16,p.z);float mid=(1-S(-17,-13,p.z))*(1-tip);weights[6]=tail*(1-mid-tip);weights[7]=tail*mid;weights[8]=tail*tip;}
            if(p.y<2.5f && p.z>-13)
            {
                int leg=0;float best=float.MaxValue;
                for(int i=0;i<4;i++) {var d=p-Feet[i];d.y=0;if(d.sqrMagnitude<best) {best=d.sqrMagnitude;leg=i;}}
                float influence=(1-S(-1.4f,2.5f,p.y))*(1-S(2.6f,4.3f,Mathf.Sqrt(best)));
                for(int i=0;i<9;i++) weights[i]*=1-influence;
                float lower=1-S(-3.1f,-.9f,p.y);float foot=1-S(Feet[leg].y-.1f,Feet[leg].y+2,p.y);
                int n=9+leg*3;weights[n]=influence*(1-lower);weights[n+1]=influence*lower*(1-foot);weights[n+2]=influence*lower*foot;
            }
            float eyeLock=EyeLock(p);
            for(int i=0;i<weights.Length;i++)weights[i]*=1-eyeLock;
            weights[2]+=eyeLock;
            // Four strongest influences, explicitly normalized.
            int[] ids={0,0,0,0};float[] w={0,0,0,0};
            for(int i=0;i<weights.Length;i++) for(int j=0;j<4;j++) if(weights[i]>w[j])
            {for(int k=3;k>j;k--) {w[k]=w[k-1];ids[k]=ids[k-1];} w[j]=weights[i];ids[j]=i;break;}
            float sum=w.Sum();
            return new BoneWeight {boneIndex0=ids[0],boneIndex1=ids[1],boneIndex2=ids[2],boneIndex3=ids[3],weight0=w[0]/sum,weight1=w[1]/sum,weight2=w[2]/sum,weight3=w[3]/sum};
        }
        static Mesh Skin(Mesh source,Matrix4x4 matrix,Transform[] bones,Transform root,string name)
        {
            var mesh=Object.Instantiate(source);mesh.name=name;
            var points=source.vertices;var normals=source.normals;var normalMatrix=matrix.inverse.transpose;
            var weights=new BoneWeight[points.Length];
            for(int i=0;i<points.Length;i++) {points[i]=matrix.MultiplyPoint3x4(points[i]);weights[i]=Weight(points[i]);if(normals.Length>i) normals[i]=normalMatrix.MultiplyVector(normals[i]).normalized;}
            mesh.vertices=points;if(normals.Length>0) mesh.normals=normals;
            mesh.boneWeights=weights;mesh.bindposes=bones.Select(b=>b.worldToLocalMatrix*root.localToWorldMatrix).ToArray();
            if(mesh.uv.Length==points.Length) mesh.RecalculateTangents();mesh.RecalculateBounds();
            string path=Folder+"/"+name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved) {EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);mesh=saved;} else AssetDatabase.CreateAsset(mesh,path);
            return mesh;
        }
        [MenuItem("Tiny Giant Problems/Rig Mossback")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first");
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var frame=Object.FindFirstObjectByType<CreatureFrame>();
            var source=frame.visual.Find("Mossback fitted visual").GetComponentInChildren<MeshFilter>();
            var old=frame.visual.Find("Mossback skeleton");if(old) Object.DestroyImmediate(old.gameObject);
            var root=new GameObject("Mossback skeleton").transform;root.SetParent(frame.visual,false);
            var rig=root.gameObject.AddComponent<MossbackSkeleton>();rig.creature=frame;
            var bones=new Transform[21];var positions=new Vector3[21];
            Action<int,string,int,Vector3> joint=(i,name,parent,p)=> {var b=new GameObject(name).transform;b.SetParent(parent<0?root:bones[parent],false);b.position=root.TransformPoint(p);bones[i]=b;positions[i]=b.localPosition;};
            joint(0,"Spine",-1,new Vector3(0,4,2));
            joint(1,"Neck",0,new Vector3(1.5f,6,10));joint(2,"Head",1,new Vector3(4,6.5f,13.5f));
            joint(3,"Jaw",2,new Vector3(5,5.3f,14));joint(4,"Ear L",2,new Vector3(3.3f,9,17.4f));joint(5,"Ear R",2,new Vector3(5.4f,8.5f,11));
            joint(6,"Tail base",0,new Vector3(-3,1,-12));joint(7,"Tail middle",6,new Vector3(-4.3f,3.4f,-17));joint(8,"Tail tip",7,new Vector3(-5,5,-19));
            for(int i=0;i<4;i++) {int n=9+i*3;var p=Feet[i];string label=new[]{"Fore L","Fore R","Hind L","Hind R"}[i];joint(n,label+" hip",0,new Vector3(p.x,1,p.z));joint(n+1,label+" knee",n,new Vector3(p.x,-1.8f,p.z));joint(n+2,label+" foot",n+1,p);}
            rig.joints=bones;rig.restPositions=positions;
            var matrix=root.worldToLocalMatrix*source.transform.localToWorldMatrix;
            var body=new GameObject("Skinned Mossback").AddComponent<SkinnedMeshRenderer>();body.transform.SetParent(root,false);
            body.sharedMesh=Skin(source.sharedMesh,matrix,bones,root,"Mossback skinned body");body.sharedMaterials=source.GetComponent<MeshRenderer>().sharedMaterials;
            body.bones=bones;body.rootBone=bones[0];body.quality=SkinQuality.Bone4;body.updateWhenOffscreen=true;body.localBounds=new Bounds(new Vector3(0,4,0),new Vector3(24,24,50));rig.body=body;
            source.GetComponent<MeshRenderer>().enabled=false;
            var originalCollider=source.GetComponentInChildren<MeshCollider>();originalCollider.enabled=false;
            var physics=new GameObject("Skinned collision source").AddComponent<SkinnedMeshRenderer>();physics.transform.SetParent(root,false);
            physics.sharedMesh=Skin(originalCollider.sharedMesh,matrix,bones,root,"Mossback skinned collision");physics.bones=bones;physics.rootBone=bones[0];physics.enabled=false;physics.updateWhenOffscreen=true;rig.physicsSkin=physics;
            rig.collisionRestMesh=physics.sharedMesh;
            var solid=new GameObject("Animated Mossback collision");solid.transform.SetParent(root,false);rig.bodyCollider=solid.AddComponent<MeshCollider>();rig.bodyCollider.sharedMesh=physics.sharedMesh;
            MossbackAnimatorBuilder.Apply();
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(frame.gameObject.scene);EditorSceneManager.SaveOpenScenes();
        }
        public static string Validate()
        {
            var rig=Object.FindFirstObjectByType<MossbackSkeleton>();var mesh=rig.body.sharedMesh;var weights=mesh.boneWeights;
            if(rig.joints.Length!=21 || mesh.bindposes.Length!=21 || weights.Length!=mesh.vertexCount) throw new Exception("Incomplete rig");
            foreach(var w in weights) if(Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)>.0001f) throw new Exception("Unnormalized skin");
            if(!rig.bodyCollider.enabled || rig.bodyCollider.isTrigger) throw new Exception("Missing solid animated collision");
            return "PASS: 21 joints; "+weights.Length+" normalized vertex weights; solid animated collision";
        }
        public static string ValidateMotion()
        {
            var rig=Object.FindFirstObjectByType<MossbackSkeleton>();var source=rig.body.sharedMesh;
            var rest=source.vertices;var weights=source.boneWeights;var triangles=source.triangles;
            var moved=new float[21];float rootDrift=0,maxStretch=1,eyeDrift=0;int eyeVertices=0;string stretchLocation="";
            var baked=new Mesh();
            try
            {
                foreach(var sample in new[]{("LookAround",0f),("LookAround",4f),("LookAround",8f),("LookAround",13.2f),("SneezeWindup",3.99f),("Sneeze",.14f),("WetDogShake",.48f),("WetDogShake",.64f)})
                {
                    string state=sample.Item1;float t=sample.Item2;
                    MossbackAnimatorBuilder.Sample(state,t);
                    rig.Pose(t);rig.body.BakeMesh(baked,true);var posed=baked.vertices;
                    var headLocal=rig.joints[2].worldToLocalMatrix*rig.body.transform.localToWorldMatrix;
                    for(int i=0;i<posed.Length;i++)
                    {
                        if(!float.IsFinite(posed[i].x) || !float.IsFinite(posed[i].y) || !float.IsFinite(posed[i].z)) throw new Exception("Nonfinite deformation");
                        float delta=Vector3.Distance(rest[i],posed[i]);var w=weights[i];
                        if(w.weight0>.7f) moved[w.boneIndex0]=Mathf.Max(moved[w.boneIndex0],delta);
                        if(w.boneIndex0==0 && w.weight0>.99999f) rootDrift=Mathf.Max(rootDrift,delta);
                        if(EyeLock(rest[i])>.99999f)
                        {
                            eyeVertices++;
                            eyeDrift=Mathf.Max(eyeDrift,Vector3.Distance(headLocal.MultiplyPoint3x4(posed[i]),source.bindposes[2].MultiplyPoint3x4(rest[i])));
                        }
                    }
                    for(int i=0;i<triangles.Length;i+=27)
                    {
                        int a=triangles[i],b=triangles[i+1];float before=Vector3.Distance(rest[a],rest[b]);
                        if(before>.02f) {float ratio=Vector3.Distance(posed[a],posed[b])/before;if(ratio>maxStretch){maxStretch=ratio;stretchLocation=state+" @ "+t+" vertex "+rest[a]+" bone "+weights[a].boneIndex0;}}
                    }
                }
                foreach(int i in new[]{2,3,11,14,17,20}) if(moved[i]<.02f) throw new Exception("No skin motion on "+rig.joints[i].name);
                if(rootDrift>.002f) throw new Exception("Deck-bearing skin drift: "+rootDrift);
                if(eyeVertices==0 || eyeDrift>.002f) throw new Exception("Eye movement relative to skull: "+eyeDrift);
                if(maxStretch>2) throw new Exception("Excessive sampled skin stretch: "+maxStretch+" "+stretchLocation);
                return "PASS: head, jaw and all four feet deform; rooted back drift="+rootDrift+"; maximum sampled edge stretch="+maxStretch+"; eye drift relative to skull="+eyeDrift;
            }
            finally {MossbackAnimatorBuilder.Sample("LookAround",0);rig.ResetPose();Object.DestroyImmediate(baked);}
        }
        public static void Preview(float time,string suffix,int width=1400,int height=1000,Matrix4x4? cameraFrame=null)
        {
            var rig=Object.FindFirstObjectByType<MossbackSkeleton>();rig.Pose(time);
            // Bake explicitly so edit-mode captures use this pose without waiting for a player tick.
            var baked=new Mesh();rig.body.BakeMesh(baked,true);
            var posed=new GameObject("Preview posed skin",typeof(MeshFilter),typeof(MeshRenderer));posed.transform.SetParent(rig.transform,false);
            posed.GetComponent<MeshFilter>().sharedMesh=baked;posed.GetComponent<MeshRenderer>().sharedMaterials=rig.body.sharedMaterials;
            bool visible=rig.body.enabled;rig.body.enabled=false;
            var f=rig.creature.transform;var go=new GameObject("Rig preview camera");var c=go.AddComponent<Camera>();c.clearFlags=CameraClearFlags.Skybox;c.farClipPlane=800;c.fieldOfView=42;
            var cameraMatrix=cameraFrame ?? f.localToWorldMatrix;
            go.transform.position=cameraMatrix.MultiplyPoint3x4(new Vector3(38,12,40));go.transform.LookAt(cameraMatrix.MultiplyPoint3x4(new Vector3(0,3,2)));
            var rt=new RenderTexture(width,height,24);var previous=RenderTexture.active;c.targetTexture=rt;c.Render();RenderTexture.active=rt;
            var tex=new Texture2D(width,height,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,width,height),0,0);tex.Apply();var path="Screenshots/Mossback-Rig-"+suffix+".png";Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,tex.EncodeToPNG());
            RenderTexture.active=previous;c.targetTexture=null;Object.DestroyImmediate(tex);Object.DestroyImmediate(rt);Object.DestroyImmediate(go);
            rig.body.enabled=visible;Object.DestroyImmediate(posed);Object.DestroyImmediate(baked);
            if(!EditorApplication.isPlaying) rig.ResetPose();
        }
    }
}
