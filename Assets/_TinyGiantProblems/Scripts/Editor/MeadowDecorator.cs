using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace TinyGiantProblems.Editor
{
    public static class MeadowDecorator
    {
        const string Folder="Assets/_TinyGiantProblems/Art/Meadow";
        // Conservative upper envelope measured from the supplied Mossback mesh.
        static float Height(float z)
        {
            // The central starting tile is level so idle crew remain stable while
            // Mossback moves; adjacent tiles ramp to the measured coat surface.
            float[] zs={-11.25f,-9.75f,-8.25f,-6.75f,-4.5f,0,2.25f,6.75f,9,11.25f};
            float[] ys={9.15f,10.9f,10.9f,11.5f,12.05f,12.55f,12.5f,12.8f,12.9f,12.7f};
            for(int i=1;i<zs.Length;i++) if(z<=zs[i]) return Mathf.Lerp(ys[i-1],ys[i],Mathf.InverseLerp(zs[i-1],zs[i],z));
            return ys[ys.Length-1];
        }
        static float Surface(float z)
        {
            float start=-11.25f+Mathf.Clamp(Mathf.Floor((z+11.25f)/1.5f),0,14)*1.5f;
            return Mathf.Lerp(Height(start),Height(start+1.5f),(z-start)/1.5f);
        }
        static Quaternion Slope(float z) => Quaternion.Euler(-Mathf.Atan2(Surface(z+.02f)-Surface(z-.02f),.04f)*Mathf.Rad2Deg,0,0);
        static void Clear(Transform t) { for(int i=t.childCount-1;i>=0;i--) UnityEngine.Object.DestroyImmediate(t.GetChild(i).gameObject); }
        static Transform Group(string name,Transform parent) { var old=parent.Find(name); if(old) UnityEngine.Object.DestroyImmediate(old.gameObject); var t=new GameObject(name).transform;t.SetParent(parent,false);return t; }
        static GameObject Put(string asset,Transform parent,Vector3 pos,float size,float yaw=0)
        {
            var p=AssetDatabase.LoadAssetAtPath<GameObject>(GiantTinyAssets.Root+"/"+asset+".prefab");
            if(!p) throw new InvalidOperationException("Missing model "+asset);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(p,parent);go.transform.localPosition=pos;go.transform.localScale=Vector3.one*size;go.transform.localRotation=Quaternion.Euler(0,yaw,0);return go;
        }
        static void Strip(Transform t) { foreach(var r in t.GetComponentsInChildren<Renderer>()) UnityEngine.Object.DestroyImmediate(r); }
        static Material Material(string name,Color color)
        {
            var path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m) { m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path); }
            m.color=color;m.SetFloat("_Smoothness",.15f);EditorUtility.SetDirty(m);return m;
        }
        [Serializable] sealed class DeckAlignment { public float plankFraction; }
        static float plankFraction;
        static void Tile(Transform parent,Vector3 center,float width,float length,float yaw=0,float pitch=0)
        {
            var go=Put("decking-and-rope-ladder-kit/plain-deck",parent,Vector3.zero,1);
            go.transform.SetParent(null,false);var b=GiantTinyAssets.BoundsOf(go);go.transform.SetParent(parent,false);
            go.transform.localScale=new Vector3(width/b.size.x,.4f/b.size.y,length/b.size.z);
            go.transform.localRotation=Quaternion.Euler(pitch,yaw,0);
            go.transform.localPosition=center-go.transform.localRotation*Vector3.up*(.4f*plankFraction);
        }
        static void Tube(Transform parent,string name,Vector3 a,Vector3 b,float radius,Material mat)
        {
            var dir=(b-a).normalized;var u=Vector3.Cross(dir,Mathf.Abs(dir.y)>.9f?Vector3.forward:Vector3.up).normalized;var v=Vector3.Cross(dir,u);
            var vertices=new Vector3[24];var uv=new Vector2[24];var indices=new int[72];
            for(int i=0;i<12;i++) { var d=(u*Mathf.Cos(i*Mathf.PI/6)+v*Mathf.Sin(i*Mathf.PI/6))*radius;vertices[i]=a+d;vertices[i+12]=b+d;uv[i]=new Vector2(i/12f,0);uv[i+12]=new Vector2(i/12f,1);int j=(i+1)%12;int k=i*6;indices[k]=i;indices[k+1]=j;indices[k+2]=i+12;indices[k+3]=j;indices[k+4]=j+12;indices[k+5]=i+12; }
            var mesh=new Mesh {name=name,vertices=vertices,uv=uv,triangles=indices};mesh.RecalculateNormals();mesh.RecalculateBounds();
            var path=Folder+"/"+name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved) { saved.Clear();saved.vertices=vertices;saved.uv=uv;saved.triangles=indices;saved.RecalculateNormals();saved.RecalculateBounds();EditorUtility.SetDirty(saved);UnityEngine.Object.DestroyImmediate(mesh);mesh=saved; } else AssetDatabase.CreateAsset(mesh,path);
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=mat;
        }
        [MenuItem("Tiny Giant Problems/Redecorate Meadow")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var g=UnityEngine.Object.FindFirstObjectByType<Expedition>();var frame=g.creature.transform;
            plankFraction=JsonUtility.FromJson<DeckAlignment>(File.ReadAllText(GiantTinyAssets.Root+"/decking-and-rope-ladder-kit/plain-deck-alignment.json")).plankFraction;
            if(plankFraction<=0 || plankFraction>1) throw new InvalidOperationException("Invalid deck surface measurement");
            var decor=Group("Guild expedition fittings",frame);
            var zones=frame.GetComponentsInChildren<ClimbableZone>().OrderBy(z=>Array.IndexOf(new[]{"Harness","Hindquarters","Spine","Shoulder","Head"},z.zoneName)).ToArray();
            for(int i=0;i<zones.Length;i++)
            {
                var zone=zones[i];float z=-9+i*4.5f;
                var deck=zone.GetComponentsInChildren<BoxCollider>().First(c=>c.name.EndsWith(" traversal"));
                float deckY=Surface(z);
                deck.transform.localPosition=new Vector3(0,deckY-.2f,z);deck.transform.localScale=new Vector3(2.6f,.4f,4.5f);Strip(deck.transform);deck.enabled=false;
                foreach(var t in zone.GetComponentsInChildren<Transform>().ToArray())
                    if(t && (t.name=="Imported deck" || t.name=="Prototype harness supports" || t.name=="Harness plank" || t.name=="Guild supply crate" || t.name=="Side access collision")) UnityEngine.Object.DestroyImmediate(t.gameObject);
                var row=Group("Fitted modular walkway",zone.transform);
                for(int j=0;j<3;j++)
                {
                    float tz=z+(j-1)*1.5f, ya=Height(tz-.75f),yb=Height(tz+.75f),pitch=-Mathf.Atan2(yb-ya,1.5f)*Mathf.Rad2Deg,len=Mathf.Sqrt(2.25f+(yb-ya)*(yb-ya));
                    var center=new Vector3(0,(ya+yb)*.5f,tz);Tile(row,center,2.6f,len*1.04f,0,pitch);
                    var solid=new GameObject("Aligned walkway collision");solid.transform.SetParent(row,false);solid.transform.localRotation=Quaternion.Euler(pitch,0,0);solid.transform.localPosition=center-solid.transform.localRotation*Vector3.up*.2f;solid.AddComponent<BoxCollider>().size=new Vector3(2.6f,.4f,len+.015f);
                }
                bool access=i==0 || i==2;
                foreach(var c in zone.GetComponentsInChildren<BoxCollider>().Where(c=>c.name=="Climbable flank").ToArray())
                {
                    if(!access) { UnityEngine.Object.DestroyImmediate(c.gameObject);continue; }
                    c.transform.localPosition=new Vector3(Mathf.Sign(c.transform.localPosition.x)*5,(deckY+5.9f)*.5f,z);c.transform.localScale=new Vector3(1,deckY-5.9f,1.2f);Strip(c.transform);
                }
                if(access) foreach(int side in new[]{-1,1})
                {
                    for(int j=0;j<3;j++) Tile(row,new Vector3(side*(1.9f+j*1.5f),deckY,z),1.2f,1.56f,90);
                    var accessGo=new GameObject("Side access collision");accessGo.transform.SetParent(zone.transform,false);accessGo.transform.localPosition=new Vector3(side*3.4f,deckY-.2f,z);var box=accessGo.AddComponent<BoxCollider>();box.size=new Vector3(4.5f,.4f,1.2f);
                    var ladder=Group("Rope ladder "+side,row);
                    float height=deckY+.12f-5.9f;int sections=Mathf.CeilToInt(height/1.1f);float step=height/sections;
                    for(int k=0;k<sections;k++) Put("decking-and-rope-ladder-kit/rope-ladder",ladder,new Vector3(side*5.57f,5.9f+k*step,z),step*1.04f,side*90);
                }
            }
            foreach(var t in frame.GetComponentsInChildren<Transform>().ToArray()) if(t && (t.name=="Guild trail" || t.name=="Anchor cap")) UnityEngine.Object.DestroyImmediate(t.gameObject);
            for(int i=0;i<g.player.anchors.Length;i++)
            {
                var a=g.player.anchors[i];Clear(a);Strip(a);a.localScale=Vector3.one;float az=-9+i*2.5f;a.localPosition=new Vector3(i%2==0?-1.12f:1.12f,Surface(az)+.01f,az);
                var winch=Put("rescue-winch/rescue-winch",a,Vector3.zero,.23f,i%2==0?90:-90);winch.transform.localRotation=Slope(az)*winch.transform.localRotation;
            }
            for(int i=0;i<g.jobs.Length;i++)
            {
                var job=g.jobs[i];if(job.ring) UnityEngine.Object.DestroyImmediate(job.ring.gameObject);job.ring=null;
                if(job.visual) UnityEngine.Object.DestroyImmediate(job.visual.gameObject);
                float jz=new[]{-4.5f,0,9f}[i];job.transform.localPosition=new Vector3(i==1?.73f:-.73f,Surface(jz)+.01f,jz);
                var target=Group("Care model",job.transform);job.visual=target;target.localRotation=Slope(jz);
                var item=Put(i==0?"bramblenmite/bramblenmite":i==1?"tangled-fur-clump/tangled-fur-clump":"plants/moss",target,Vector3.zero,i==0?.55f:.65f);
                if(i==2) { var m=Material("Irritated moss",new Color(.83f,.46f,.23f));m.mainTexture=item.GetComponentInChildren<Renderer>().sharedMaterial.mainTexture;foreach(var r in item.GetComponentsInChildren<Renderer>()) r.sharedMaterial=m; }
            }
            var oldSupply=frame.Find("Imported supplies");if(oldSupply) UnityEngine.Object.DestroyImmediate(oldSupply.gameObject);
            var oldBeacon=frame.Find("Imported extraction beacon");if(oldBeacon) UnityEngine.Object.DestroyImmediate(oldBeacon.gameObject);
            Put("chest/closed-chest",decor,new Vector3(.85f,Surface(-9.5f),-9.5f),.4f,180);
            Put("tool-case/tool-case",decor,new Vector3(.85f,Surface(-9),-9),.25f);
            Put("treatment-bottle/treatment-bottle",decor,new Vector3(.85f,Surface(-8.6f),-8.6f),.117f);
            Put("spare-canister/spare-canister",decor,new Vector3(1.06f,Surface(-9),-9),.2f);
            Strip(g.extraction);g.extraction.localScale=Vector3.one;g.extraction.localPosition=new Vector3(-.8f,Surface(-9.5f),-9.5f);
            Put("extraction-beacon/beacon-clean",decor,new Vector3(-.8f,Surface(-9.5f),-9.5f),.55f);
            foreach(Transform item in decor) {var p=item.localPosition;p.y=Surface(p.z)+.005f;item.localPosition=p;item.localRotation=Slope(p.z)*item.localRotation;}
            g.extraction.localPosition=new Vector3(-.8f,Surface(-9.5f),-9.5f);
            g.player.checkpoint.localPosition=new Vector3(0,Surface(-9)+.2f,-9);g.player.transform.position=g.player.checkpoint.position;
            FitTechnician(g.player);
            Landscape(GameObject.Find("Migratory meadow").transform);
            var sun=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l=>l.type==LightType.Directional);sun.intensity=1.35f;sun.color=new Color(1,.94f,.82f);
            RenderSettings.fogDensity=.0018f;RenderSettings.fogColor=new Color(.64f,.75f,.77f);RenderSettings.ambientLight=new Color(.62f,.68f,.7f);
            var skyPath=Folder+"/Meadow sky.mat";var sky=AssetDatabase.LoadAssetAtPath<Material>(skyPath);if(!sky) {sky=new Material(Shader.Find("Skybox/Procedural"));AssetDatabase.CreateAsset(sky,skyPath);}sky.SetColor("_SkyTint",new Color(.48f,.58f,.64f));sky.SetColor("_GroundColor",new Color(.52f,.62f,.57f));sky.SetFloat("_Exposure",1.1f);EditorUtility.SetDirty(sky);RenderSettings.skybox=sky;
            g.player.orbitCamera.clearFlags=CameraClearFlags.Skybox;g.player.orbitCamera.farClipPlane=800;
            MeadowPolish.Apply();
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);
        }
        static void FitTechnician(TechnicianMotor player)
        {
            foreach(var t in player.model.GetComponentsInChildren<Transform>().ToArray()) if(t && (t.name=="Round helmet" || t.name=="Backpack" || t.name=="Visor")) UnityEngine.Object.DestroyImmediate(t.gameObject);
            var gear=Group("Guild clothing",player.model);
            // Existing head is now supplied by a small face beneath the fitted hat.
            var head=GameObject.CreatePrimitive(PrimitiveType.Sphere);head.name="Technician face";head.transform.SetParent(gear,false);head.transform.localPosition=new Vector3(0,1.4f,0);head.transform.localScale=new Vector3(.43f,.42f,.4f);UnityEngine.Object.DestroyImmediate(head.GetComponent<Collider>());head.GetComponent<Renderer>().sharedMaterial=Material("Warm cloth and skin",new Color(.77f,.59f,.4f));
            Put("technician-cosmetic-kit/hat",gear,new Vector3(0,1.49f,0),.78f,180);
            Put("technician-cosmetic-kit/pouch",gear,new Vector3(0,.68f,-.38f),.55f,180);
            Put("technician-cosmetic-kit/scarf",gear,new Vector3(0,1.08f,.02f),.43f,180);
        }
        static Terrain TerrainAsset(Transform parent)
        {
            const int n=257;var path=Folder+"/Meadow terrain.asset";var data=AssetDatabase.LoadAssetAtPath<TerrainData>(path);
            if(!data) {data=new TerrainData();AssetDatabase.CreateAsset(data,path);}data.heightmapResolution=n;data.size=new Vector3(500,38,500);
            var heights=new float[n,n];for(int z=0;z<n;z++) for(int x=0;x<n;x++)
            {float wx=x*500f/(n-1)-250,wz=z*500f/(n-1)-250;float edge=Mathf.SmoothStep(0,1,Mathf.Clamp01((Mathf.Max(Mathf.Abs(wx)-35,Mathf.Abs(wz)-85))/110));heights[z,x]=edge*(.15f+.6f*Mathf.PerlinNoise(wx*.012f+18,wz*.012f+21));}data.SetHeights(0,0,heights);
            var layers=new TerrainLayer[2];for(int i=0;i<2;i++)
            {
                var texture=new Texture2D(128,128,TextureFormat.RGB24,true);for(int y=0;y<128;y++) for(int x=0;x<128;x++) {float noise=Mathf.PerlinNoise(x*.14f+i*9,y*.14f);texture.SetPixel(x,y,(i==0?new Color(.34f,.43f,.21f):new Color(.43f,.34f,.23f))*(.85f+noise*.3f));}texture.Apply();var texPath=Folder+"/Ground"+i+".png";File.WriteAllBytes(texPath,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(texPath);
                var layerPath=Folder+"/Ground"+i+".terrainlayer";layers[i]=AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);if(!layers[i]) {layers[i]=new TerrainLayer();AssetDatabase.CreateAsset(layers[i],layerPath);}layers[i].diffuseTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);layers[i].tileSize=new Vector2(9,9);layers[i].smoothness=0;
            }
            data.terrainLayers=layers;data.alphamapResolution=256;var alpha=new float[256,256,2];for(int z=0;z<256;z++) for(int x=0;x<256;x++) {float wx=x*500f/255-250,wz=z*500f/255-250;float curve=-41+Mathf.Sin(wz*.025f)*7;float trail=1-Mathf.SmoothStep(2,6,Mathf.Abs(wx-curve));alpha[z,x,1]=trail*.8f;alpha[z,x,0]=1-alpha[z,x,1];}data.SetAlphamaps(0,0,alpha);EditorUtility.SetDirty(data);
            var go=Terrain.CreateTerrainGameObject(data);go.name="Sculpted meadow";go.transform.SetParent(parent,false);go.transform.localPosition=new Vector3(-250,-5.5f,-250);var terrain=go.GetComponent<Terrain>();terrain.heightmapPixelError=8;terrain.drawInstanced=true;return terrain;
        }
        static void Landscape(Transform meadow)
        {
            Clear(meadow);var terrain=TerrainAsset(meadow);var root=Group("Forest clearing",meadow);var random=new System.Random(913);
            Func<float,float,float> height=(x,z)=>terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y;
            string[] trees={"broad-tree","tall-tree","bent-tree"};string[] rocks={"large-rock","medium-rock","small-rock"};
            for(int i=0;i<115;i++)
            {
                float angle=(float)random.NextDouble()*Mathf.PI*2,r=75+(float)random.NextDouble()*125;float x=Mathf.Cos(angle)*r,z=Mathf.Sin(angle)*r*1.13f;
                if(Mathf.Abs(x)<39 && Mathf.Abs(z)<94) continue;
                float size=10+(float)random.NextDouble()*12;Put("trees/"+trees[i%3],root,new Vector3(x,height(x,z)-.08f,z),size,(float)random.NextDouble()*360);
                if(i%3==0) {Put("plants/plant-group",root,new Vector3(x+3,height(x+3,z+2)-.03f,z+2),2.1f,i*37);Put("plants/moss",root,new Vector3(x-2,height(x-2,z)-.04f,z),2.4f);}
            }
            for(int i=0;i<35;i++)
            {float angle=i*2.39996f;float r=46+i%5*14;float x=Mathf.Cos(angle)*r,z=Mathf.Sin(angle)*r*1.5f;if(Mathf.Abs(x)<35 && Mathf.Abs(z)<80) continue;Put("rocks/"+rocks[i%3],root,new Vector3(x,height(x,z)-.12f,z),2.5f+i%4,i*53);Put("plants/moss",root,new Vector3(x+2,height(x+2,z)-.06f,z),1.6f);}
            Put("rocks/rock-arch",root,new Vector3(-48,height(-48,34)-.1f,34),14,90);
            var camp=Group("Guild base camp",meadow);float cx=-46,cz=-44;
            Put("boarding-dock/boarding-dock",camp,new Vector3(cx,height(cx,cz)-.05f,cz),7,90);
            var bench=Put("guild-workshop-furniture-kit/workbench",camp,new Vector3(cx-5,height(cx-5,cz+4),cz+4),2.4f,120);
            var benchTop=GiantTinyAssets.BoundsOf(bench).max.y;
            var equipment=Group("Workbench equipment",meadow);
            Put("portable-scanner/portable-scanner",equipment,new Vector3(cx-5.35f,benchTop,cz+4),.4f,120);
            Put("grooming-brush/grooming-brush",equipment,new Vector3(cx-4.65f,benchTop,cz+4),.4f,120);
            Put("treatment-bottle/treatment-bottle",equipment,new Vector3(cx-5,benchTop,cz+4.3f),.3f);
            Put("guild-workshop-furniture-kit/stool",camp,new Vector3(cx-3,height(cx-3,cz+3),cz+3),.7f);
            Put("guild-workshop-furniture-kit/shelf",camp,new Vector3(cx-7,height(cx-7,cz+5),cz+5),2.2f,120);
            Put("chest/open-chest",camp,new Vector3(cx+3,height(cx+3,cz-3),cz-3),1.3f,-30);
            Put("tool-case/tool-case",camp,new Vector3(cx+4.6f,height(cx+4.6f,cz-3),cz-3),.8f,20);
            Put("extraction-beacon/beacon-clean",camp,new Vector3(cx+3,height(cx+3,cz+3),cz+3),1.5f);
            Put("survey-drone/survey-drone",camp,new Vector3(cx-2,height(cx-2,cz-4),cz-4),1.1f,30);
            Put("baby-creature/baby-creature",camp,new Vector3(cx+5,height(cx+5,cz-7),cz-7),2.6f,35);
            Put("glider/glider",camp,new Vector3(cx+6,height(cx+6,cz-2),cz-2),2.5f,35);
            Put("decking-and-rope-ladder-kit/railed-deck",camp,new Vector3(cx-6,height(cx-6,cz-8),cz-8),3.2f,180);
        }
        public static string Validate()
        {
            var g=UnityEngine.Object.FindFirstObjectByType<Expedition>();Physics.SyncTransforms();
            var forbidden=new[]{"Cloud","Distant rolling hill","Tree trunk","Round canopy","Guild trail","Anchor cap","Care site ring","Harness ladder","Harness plank","Meadow floor"};
            var raw=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.enabled && forbidden.Contains(r.name)).ToArray();
            if(raw.Length>0) throw new InvalidOperationException("Placeholder visuals remain: "+string.Join(",",raw.Select(r=>r.name)));
            var terrain=UnityEngine.Object.FindFirstObjectByType<Terrain>();int grounded=0;
            foreach(var name in new[]{"Forest clearing","Guild base camp"})
                foreach(Transform t in GameObject.Find(name).transform)
                {float ground=terrain.SampleHeight(t.position)+terrain.transform.position.y;if(Mathf.Abs(GiantTinyAssets.BoundsOf(t.gameObject).min.y-ground)>.21f) throw new InvalidOperationException("Ground alignment: "+t.name);grounded++;}
            foreach(var anchor in g.player.anchors)
                if(!Physics.Raycast(anchor.position+Vector3.up,Vector3.down,out var hit,1.3f) || !hit.collider.GetComponentInParent<ClimbableZone>()) throw new InvalidOperationException("Anchor off walkway: "+anchor.name);
            return "PASS: no legacy map visuals; "+grounded+" scenery roots grounded; all eight anchors aligned with walkable surfaces.";
        }
        public static void CampPreview()
        {
            var go=new GameObject("Camp preview camera");var cam=go.AddComponent<Camera>();cam.clearFlags=CameraClearFlags.Skybox;cam.fieldOfView=48;cam.farClipPlane=800;go.transform.position=new Vector3(-32,7,-29);go.transform.LookAt(new Vector3(-48,-2,-43));
            var rt=new RenderTexture(1600,900,24);var previous=RenderTexture.active;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();Directory.CreateDirectory("Screenshots");File.WriteAllBytes("Screenshots/Redecorated-Camp.png",image.EncodeToPNG());RenderTexture.active=previous;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
