using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TinyGiantProblems.Editor
{
    // Repeatable finishing pass. Geometry follows existing traversal surfaces.
    public static class MeadowPolish
    {
        const string Folder="Assets/_TinyGiantProblems/Art/Meadow";
        sealed class Tubes
        {
            readonly List<Vector3> vertices=new List<Vector3>();
            readonly List<int> triangles=new List<int>();
            public void Segment(Vector3 a,Vector3 b,float radius)
            {
                var direction=(b-a).normalized;
                var u=Vector3.Cross(direction,Mathf.Abs(direction.y)>.9f?Vector3.forward:Vector3.up).normalized;
                var v=Vector3.Cross(direction,u);int start=vertices.Count;
                for(int end=0;end<2;end++) for(int i=0;i<10;i++)
                    vertices.Add((end==0?a:b)+radius*(u*Mathf.Cos(i*Mathf.PI/5)+v*Mathf.Sin(i*Mathf.PI/5)));
                for(int i=0;i<10;i++) {int j=(i+1)%10;triangles.AddRange(new[]{start+i,start+j,start+i+10,start+j,start+j+10,start+i+10});}
                for(int i=1;i<9;i++) triangles.AddRange(new[]{start,start+i+1,start+i,start+10,start+10+i,start+11+i});
            }
            public void Rope(Vector3 a,Vector3 b,float sag,float radius=.018f)
            {
                var last=a;for(int i=1;i<=12;i++) {float t=i/12f;var p=Vector3.Lerp(a,b,t)-Vector3.up*(4*t*(1-t)*sag);Segment(last,p,radius);last=p;}
            }
            public void Save(string name,Transform parent,Material material)
            {
                var path=Folder+"/"+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(!mesh) {mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);} else mesh.Clear();
                mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
                var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
                go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;
            }
        }
        static Transform Group(string name,Transform parent)
        {
            var old=parent.Find(name);if(old) Object.DestroyImmediate(old.gameObject);
            var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;
        }
        static Material Mat(string name,Color color)
        {
            string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m) {m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.color=color;m.SetFloat("_Smoothness",.22f);EditorUtility.SetDirty(m);return m;
        }
        static float Surface(Transform frame,float z)
        {
            var deck=frame.GetComponentsInChildren<BoxCollider>().Where(c=>c.name=="Aligned walkway collision")
                .OrderBy(c=>Mathf.Abs(frame.InverseTransformPoint(c.transform.position).z-z)).First();
            var plane=new Plane(deck.transform.up,deck.transform.TransformPoint(Vector3.up*deck.size.y*.5f));
            var ray=new Ray(frame.TransformPoint(new Vector3(0,30,z)),-frame.up);plane.Raycast(ray,out var distance);
            return frame.InverseTransformPoint(ray.GetPoint(distance)).y;
        }
        public static void Apply()
        {
            if(EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play mode first.");
            var g=Object.FindFirstObjectByType<Expedition>();var frame=g.creature.transform;
            var root=Group("Finished harness rigging",frame);
            var wood=new Tubes();var rope=new Tubes();var brass=new Tubes();
            // Rails leave the four ladder branches and both ends open.
            foreach(int side in new[]{-1,1})
            {
                for(int i=0;i<=15;i++)
                {
                    float z=-11.25f+i*1.5f;var p=new Vector3(side*1.23f,Surface(frame,z),z);
                    wood.Segment(p-Vector3.up*.12f,p+Vector3.up*.38f,.035f);
                    brass.Segment(p+Vector3.up*.34f,p+Vector3.up*.40f,.047f);
                    rope.Segment(p+Vector3.up*.05f,p+Vector3.up*.10f,.043f);
                    if(i==15) continue;
                    float next=z+1.5f;
                    if((z < -8.4f && next > -9.6f) || (z < .6f && next > -.6f)) continue;
                    var q=new Vector3(side*1.23f,Surface(frame,next),next);
                    rope.Rope(p+Vector3.up*.34f,q+Vector3.up*.34f,.055f);
                    rope.Rope(p+Vector3.up*.18f,q+Vector3.up*.18f,.035f,.013f);
                }
                foreach(float z in new[]{-9f,0f})
                {
                    float y=Surface(frame,z);
                    foreach(float edge in new[]{-.52f,.52f})
                    {
                        // Broad triangle under each projecting access bridge.
                        wood.Segment(new Vector3(side*1.2f,y-.18f,z+edge),new Vector3(side*5.55f,y-.18f,z+edge),.075f);
                        wood.Segment(new Vector3(side*2.1f,y-1.35f,z+edge),new Vector3(side*5.48f,y-.18f,z+edge),.065f);
                        wood.Segment(new Vector3(side*2.1f,y-1.35f,z+edge),new Vector3(side*2.1f,y-.18f,z+edge),.075f);
                        for(int i=0;i<4;i++)
                        {
                            var p=new Vector3(side*(1.3f+i*1.4f),y,z+edge);
                            wood.Segment(p-Vector3.up*.1f,p+Vector3.up*.38f,.035f);
                            brass.Segment(p+Vector3.up*.34f,p+Vector3.up*.40f,.047f);
                            if(i<3) rope.Rope(p+Vector3.up*.34f,p+new Vector3(side*1.4f,.34f,0),.05f);
                        }
                    }
                    // Continuous ropes hide segment joins and tie the ladder into the landing.
                    var ladder=frame.GetComponentsInChildren<Transform>().First(t=>t.name=="Rope ladder "+side && Mathf.Abs(frame.InverseTransformPoint(t.GetComponentInChildren<Renderer>().bounds.center).z-z)<1);
                    var bounds=GiantTinyAssets.BoundsOf(ladder.gameObject);
                    float halfWidth=bounds.size.z/frame.lossyScale.z*.47f;
                    foreach(int edge in new[]{-1,1})
                    {
                        var top=new Vector3(side*5.57f,y+.1f,z+edge*halfWidth);
                        rope.Segment(new Vector3(side*5.57f,5.9f,z+edge*halfWidth),top,.018f);
                        rope.Rope(top,new Vector3(side*5.48f,y+.35f,z+edge*.52f),.015f);
                    }
                }
            }
            wood.Save("Harness timber supports",root,Mat("Harness warm timber",new Color(.26f,.12f,.055f)));
            rope.Save("Harness tied ropes",root,Mat("Harness natural rope",new Color(.66f,.47f,.24f)));
            brass.Save("Harness brass collars",root,Mat("Harness aged brass",new Color(.55f,.36f,.12f)));
            CampGround();
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveOpenScenes();
        }
        static void CampGround()
        {
            var terrain=Object.FindFirstObjectByType<Terrain>();var data=terrain.terrainData;
            int w=data.alphamapWidth,h=data.alphamapHeight;var alpha=data.GetAlphamaps(0,0,w,h);
            for(int z=0;z<h;z++) for(int x=0;x<w;x++)
            {
                float wx=terrain.transform.position.x+x*data.size.x/(w-1),wz=terrain.transform.position.z+z*data.size.z/(h-1);
                float oval=Mathf.Sqrt(Mathf.Pow((wx+46)/12,2)+Mathf.Pow((wz+44)/11,2));
                float clearing=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,1.1f,oval+(Mathf.PerlinNoise(wx*.3f,wz*.3f)-.5f)*.12f));
                float trailX=-41+Mathf.Sin(wz*.025f)*7;
                float trail=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(2,6,Mathf.Abs(wx-trailX)));
                float weight=Mathf.Max(trail*.8f,clearing*.9f);alpha[z,x,1]=weight;alpha[z,x,0]=1-weight;
            }
            data.SetAlphamaps(0,0,alpha);data.SetBaseMapDirty();terrain.drawInstanced=true;terrain.Flush();EditorUtility.SetDirty(data);
            var meadow=terrain.transform.parent;var edge=Group("Camp planted border",meadow);
            // Deliberate planting islands leave the east approach and equipment clear.
            for(int i=0;i<14;i++)
            {
                float angle=(105+(i/3)*34+(i%3)*5)*Mathf.Deg2Rad;float radius=11.5f+(i%3)*.7f;float x=-46+Mathf.Cos(angle)*radius,z=-44+Mathf.Sin(angle)*radius;
                string asset="plants/plant-group";
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(GiantTinyAssets.Root+"/"+asset+".prefab");
                var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,edge);go.transform.position=new Vector3(x,terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y-.03f,z);
                go.transform.localScale=Vector3.one*(i%3==0?1.3f:1.8f);go.transform.localRotation=Quaternion.Euler(0,i*73,0);
            }
        }
        public static void Preview()
        {
            var f=Object.FindFirstObjectByType<CreatureFrame>().transform;
            var go=new GameObject("Polish preview camera");var c=go.AddComponent<Camera>();c.clearFlags=CameraClearFlags.Skybox;c.farClipPlane=800;c.fieldOfView=55;
            go.transform.position=f.TransformPoint(new Vector3(9,16,-15));go.transform.LookAt(f.TransformPoint(new Vector3(0,11.3f,-5)));
            var rt=new RenderTexture(1600,900,24);var previous=RenderTexture.active;c.targetTexture=rt;c.Render();RenderTexture.active=rt;
            var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();Directory.CreateDirectory("Screenshots");File.WriteAllBytes("Screenshots/Polished-Harness.png",tex.EncodeToPNG());
            RenderTexture.active=previous;c.targetTexture=null;Object.DestroyImmediate(tex);Object.DestroyImmediate(rt);Object.DestroyImmediate(go);
            MeadowDecorator.CampPreview();
        }
    }
}
