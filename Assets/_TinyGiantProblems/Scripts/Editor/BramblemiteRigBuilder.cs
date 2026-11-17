using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace TinyGiantProblems.Editor
{
    public static class BramblemiteRigBuilder
    {
        const string Folder="Assets/_TinyGiantProblems/Art/BramblemiteRig";
        const string Relative="bramblenmite/bramblenmite";
        static float S(float a,float b,float x)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,x));
        static BoneWeight Weight(Vector3 p)
        {
            float head=S(.2f,.38f,p.z)*(1-S(.34f,.43f,p.y));
            float antenna=S(.34f,.43f,p.y)*S(.31f,.40f,p.z)*(1-S(.07f,.14f,Mathf.Abs(Mathf.Abs(p.x)-.19f)));
            if(antenna>.001f)
            {
                int n=p.x<0?2:4;float tip=S(.44f,.56f,p.y);
                return new BoneWeight {boneIndex0=n,boneIndex1=n+1,boneIndex2=1,boneIndex3=0,weight0=antenna*(1-tip),weight1=antenna*tip,weight2=(1-antenna)*head,weight3=(1-antenna)*(1-head)};
            }
            float leg=(1-S(.14f,.25f,p.y))*S(.24f,.34f,Mathf.Abs(p.x));
            if(leg>.001f)
            {
                int side=p.x<0?0:1;float[] zs={.39f,.04f,-.22f};int index=Enumerable.Range(0,3).OrderBy(i=>Mathf.Abs(p.z-zs[i])).First();int n=6+side*6+index*2;float knee=1-S(.07f,.17f,p.y);
                return new BoneWeight {boneIndex0=n,boneIndex1=n+1,boneIndex2=0,weight0=leg*(1-knee),weight1=leg*knee,weight2=1-leg};
            }
            return new BoneWeight {boneIndex0=1,boneIndex1=0,weight0=head,weight1=1-head};
        }
        static Mesh Skin(Mesh source,Matrix4x4 matrix,Transform root,Transform[] bones,string name)
        {
            var mesh=Object.Instantiate(source);mesh.name=name;var points=mesh.vertices;var normals=mesh.normals;var weights=new BoneWeight[points.Length];var normalMatrix=matrix.inverse.transpose;
            for(int i=0;i<points.Length;i++){points[i]=matrix.MultiplyPoint3x4(points[i]);weights[i]=Weight(points[i]);if(i<normals.Length)normals[i]=normalMatrix.MultiplyVector(normals[i]).normalized;}
            mesh.vertices=points;if(normals.Length>0)mesh.normals=normals;mesh.boneWeights=weights;mesh.bindposes=bones.Select(b=>b.worldToLocalMatrix*root.localToWorldMatrix).ToArray();mesh.RecalculateBounds();if(mesh.uv.Length==points.Length)mesh.RecalculateTangents();
            string path=Folder+"/"+name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved){EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);mesh=saved;}else AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        public static void Apply()
        {
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();string path=GiantTinyAssets.Root+"/"+Relative+".prefab";
            var prefab=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var source=prefab.GetComponentInChildren<MeshFilter>();var previous=prefab.transform.Find("Bramblemite skeleton");if(previous)Object.DestroyImmediate(previous.gameObject);
                var root=new GameObject("Bramblemite skeleton").transform;root.SetParent(prefab.transform,false);var rig=root.gameObject.AddComponent<BramblemiteSkeleton>();
                var bones=new Transform[18];var positions=new Vector3[18];
                Action<int,string,int,Vector3> add=(i,name,parent,p)=>{var go=new GameObject(name).transform;go.SetParent(parent<0?root:bones[parent],false);go.position=root.TransformPoint(p);bones[i]=go;positions[i]=go.localPosition;};
                add(0,"Shell and thorax",-1,new Vector3(0,.3f,-.05f));add(1,"Head",0,new Vector3(0,.27f,.29f));
                for(int side=0;side<2;side++)
                {
                    float sign=side==0?-1:1;int a=2+side*2;add(a,"Antenna base "+side,1,new Vector3(sign*.16f,.38f,.38f));add(a+1,"Antenna tip "+side,a,new Vector3(sign*.2f,.51f,.43f));
                    for(int leg=0;leg<3;leg++){float z=new[]{.39f,.04f,-.22f}[leg];int n=6+side*6+leg*2;add(n,"Leg "+side+"-"+leg+" hip",0,new Vector3(sign*.28f,.21f,z));add(n+1,"Leg "+side+"-"+leg+" knee",n,new Vector3(sign*.38f,.12f,z+.015f));}
                }
                rig.joints=bones;rig.restPositions=positions;
                var matrix=root.worldToLocalMatrix*source.transform.localToWorldMatrix;
                var body=new GameObject("Animated bramblemite").AddComponent<SkinnedMeshRenderer>();body.transform.SetParent(root,false);body.sharedMesh=Skin(source.sharedMesh,matrix,root,bones,"Bramblemite skin");body.sharedMaterials=source.GetComponent<MeshRenderer>().sharedMaterials;body.bones=bones;body.rootBone=bones[0];body.quality=SkinQuality.Bone4;body.updateWhenOffscreen=true;body.localBounds=new Bounds(new Vector3(0,.4f,0),new Vector3(1.3f,1.1f,1.3f));rig.body=body;
                source.GetComponent<MeshRenderer>().enabled=false;var oldCollider=source.GetComponent<MeshCollider>();if(oldCollider)oldCollider.enabled=false;
                var physics=new GameObject("Bramblemite collision skin").AddComponent<SkinnedMeshRenderer>();physics.transform.SetParent(root,false);physics.sharedMesh=Skin(WorldCollisionBuilder.CollisionMesh(Relative),matrix,root,bones,"Bramblemite physics skin");physics.bones=bones;physics.rootBone=bones[0];physics.enabled=false;rig.physicsSkin=physics;rig.collisionRest=physics.sharedMesh;
                var solid=new GameObject("Solid bramblemite").AddComponent<MeshCollider>();solid.transform.SetParent(root,false);solid.sharedMesh=physics.sharedMesh;rig.solid=solid;
                var policy=prefab.GetComponent<PropCollisionPolicy>();if(!policy)policy=prefab.AddComponent<PropCollisionPolicy>();policy.surfaces=new Collider[]{solid};
                PrefabUtility.SaveAsPrefabAsset(prefab,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
            AssetDatabase.SaveAssets();
        }
        public static string ValidateMotion()
        {
            var rig=Object.FindFirstObjectByType<BramblemiteSkeleton>();if(!rig)throw new Exception("No live bramblemite rig");
            var rest=rig.body.sharedMesh.vertices;var weights=rig.body.sharedMesh.boneWeights;var moved=new float[18];var bake=new Mesh();
            try
            {
                foreach(float t in new[]{0f,.3f,.8f,1.4f,2.1f}){rig.Pose(t);rig.body.BakeMesh(bake,true);var posed=bake.vertices;
                    for(int i=0;i<rest.Length;i++){var w=weights[i];if(Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)>.0001f)throw new Exception("Invalid weights");float delta=Vector3.Distance(rest[i],posed[i]);if(!float.IsFinite(delta)||delta>.2f)throw new Exception("Unbounded deformation");if(w.weight0>.1f)moved[w.boneIndex0]=Mathf.Max(moved[w.boneIndex0],delta);if(w.weight1>.1f)moved[w.boneIndex1]=Mathf.Max(moved[w.boneIndex1],delta);if(w.weight2>.1f)moved[w.boneIndex2]=Mathf.Max(moved[w.boneIndex2],delta);if(w.weight3>.1f)moved[w.boneIndex3]=Mathf.Max(moved[w.boneIndex3],delta);}}
                for(int i=0;i<18;i++)if(moved[i]<.0001f)throw new Exception("Joint has no skin motion: "+i);
                return "PASS: 18 weighted joints; head, antennae and six legs deform within bounds.";
            }
            finally{rig.ResetPose();Object.DestroyImmediate(bake);}
        }
        public static void Preview(float time,string name)
        {
            var rig=Object.FindFirstObjectByType<BramblemiteSkeleton>();rig.Pose(time);var mesh=new Mesh();rig.body.BakeMesh(mesh,true);var model=new GameObject("Preview bramblemite",typeof(MeshFilter),typeof(MeshRenderer));model.transform.SetParent(rig.transform,false);model.GetComponent<MeshFilter>().sharedMesh=mesh;model.GetComponent<MeshRenderer>().sharedMaterials=rig.body.sharedMaterials;rig.body.enabled=false;
            var cameraGo=new GameObject("Bramblemite preview camera");var c=cameraGo.AddComponent<Camera>();c.clearFlags=CameraClearFlags.Skybox;c.nearClipPlane=.02f;c.farClipPlane=800;c.fieldOfView=40;
            c.transform.position=rig.transform.TransformPoint(new Vector3(1.3f,1.05f,1.6f));c.transform.LookAt(rig.transform.TransformPoint(new Vector3(0,.33f,0)));
            var rt=new RenderTexture(900,700,24);var old=RenderTexture.active;c.targetTexture=rt;c.Render();RenderTexture.active=rt;var texture=new Texture2D(900,700,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,900,700),0,0);texture.Apply();string path="Screenshots/Bramblemite-"+name+".png";Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,texture.EncodeToPNG());
            RenderTexture.active=old;c.targetTexture=null;Object.DestroyImmediate(texture);Object.DestroyImmediate(rt);Object.DestroyImmediate(cameraGo);Object.DestroyImmediate(model);Object.DestroyImmediate(mesh);rig.body.enabled=true;rig.ResetPose();
        }
    }
}
