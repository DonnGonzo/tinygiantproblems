using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TinyGiantProblems.Editor
{
    public static class GiantTinyAssets
    {
        public const string Root = "Assets/_TinyGiantProblems/Art/Imported/GiantTiny";
        [MenuItem("Tiny Giant Problems/Prepare GiantTiny Library")]
        public static void Prepare() => PrepareFolders(null);
        public static readonly string[] UpdatedFolders={"mossback","tangled-fur-clump","decking-and-rope-ladder-kit","spare-canister","rope-launcher","extraction-beacon","glider"};
        public static void PrepareUpdated() => PrepareFolders(UpdatedFolders);
        public static void PrepareDeckKit() => PrepareFolders(new[]{"decking-and-rope-ladder-kit"});
        static void PrepareFolders(string[] only)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
            AssetDatabase.Refresh();
            foreach (var file in Directory.GetFiles(Root, "*.obj", SearchOption.AllDirectories))
            {
                var path = file.Replace('\\', '/');
                var folder = Path.GetDirectoryName(path).Replace('\\', '/');
                if(only!=null && !only.Contains(Path.GetFileName(folder))) continue;
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.globalScale = 1;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.isReadable = false;
                importer.indexFormat = ModelImporterIndexFormat.UInt32;
                importer.SaveAndReimport();
                var materialPath = folder + "/Surface.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, materialPath); }
                material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/BaseColor.png");
                material.color = Color.white;
                material.SetFloat("_Smoothness", .18f);
                material.enableInstancing=true;
                if(File.Exists(folder+"/Normal.png"))
                {
                    foreach(var map in new[]{"Normal","MetallicSmoothness","Occlusion"})
                    {
                        var ti=(TextureImporter)AssetImporter.GetAtPath(folder+"/"+map+".png");
                        ti.textureType=map=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;
                        ti.sRGBTexture=false;ti.maxTextureSize=2048;ti.SaveAndReimport();
                    }
                    material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Normal.png"));material.EnableKeyword("_NORMALMAP");
                    material.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/MetallicSmoothness.png"));material.EnableKeyword("_METALLICSPECGLOSSMAP");
                    material.SetTexture("_OcclusionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Occlusion.png"));material.EnableKeyword("_OCCLUSIONMAP");
                    material.SetFloat("_Metallic",1);material.SetFloat("_Smoothness",1);material.SetFloat("_Cull",0);
                }
                EditorUtility.SetDirty(material);
                var root = new GameObject(Path.GetFileNameWithoutExtension(path));
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), root.transform);
                foreach (var r in model.GetComponentsInChildren<Renderer>()) r.sharedMaterial = material;
                var bounds = BoundsOf(model);
                // Uniform normalized size, centered footprint and a ground-level pivot.
                float scale = 1f / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                model.transform.localScale = Vector3.one * scale;
                model.transform.localPosition = -new Vector3(bounds.center.x, bounds.min.y, bounds.center.z) * scale;
                PrefabUtility.SaveAsPrefabAsset(root, Path.ChangeExtension(path, ".prefab"));
                UnityEngine.Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets();
        }
        public static Bounds BoundsOf(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
            return bounds;
        }
        static GameObject Place(string asset, Transform parent, Vector3 position, float size = 1)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/" + asset + ".prefab");
            if (!prefab) throw new InvalidOperationException("Missing prepared asset: " + asset);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.localPosition = position;
            go.transform.localScale = Vector3.one * size;
            return go;
        }
        static Transform Group(string name, Transform parent)
        {
            var old = parent.Find(name);
            if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }
        [MenuItem("Tiny Giant Problems/Apply GiantTiny Models")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
            var game = UnityEngine.Object.FindFirstObjectByType<Expedition>();
            if (!game) throw new InvalidOperationException("Open the playable scene first.");
            var frame = game.creature;
            for (int i = frame.visual.childCount-1; i>=0; i--) UnityEngine.Object.DestroyImmediate(frame.visual.GetChild(i).gameObject);
            // Face along the established route; fit its footprint without scaling the technician.
            var creature = Place("mossback/mossback", frame.visual, Vector3.zero);
            creature.transform.localRotation = Quaternion.Euler(0,90,0);
            // Measure in an unscaled temporary frame so authored dimensions are local metres.
            creature.transform.SetParent(null, false);
            var b = BoundsOf(creature);
            var fit = new Vector3(18/b.size.x,18/b.size.y,42/b.size.z);
            var wrapper = new GameObject("Mossback fitted visual").transform;
            wrapper.SetParent(frame.visual,false);
            creature.transform.SetParent(wrapper,false);
            creature.transform.localPosition = -new Vector3(b.center.x,b.min.y,b.center.z);
            wrapper.localScale = fit;
            wrapper.localPosition = new Vector3(0,-5.5f,0);
            MossbackCollision.Apply();
            MossbackRigBuilder.Apply();
            foreach (var zone in frame.GetComponentsInChildren<ClimbableZone>())
            {
                var deck = zone.GetComponentsInChildren<BoxCollider>().First(c=>c.name.EndsWith(" traversal"));
                var deckArt = Group("Imported deck",zone.transform);
                var art = Place("platforrms/upper-platform",deckArt,Vector3.zero);
                // Prefab bounds measured at unit scale away from the creature hierarchy.
                art.transform.SetParent(null,false); var ab=BoundsOf(art);
                art.transform.SetParent(deckArt,false);
                art.transform.localScale = new Vector3(deck.transform.localScale.x/ab.size.x,1/ab.size.y,deck.transform.localScale.z/ab.size.z);
                // Raised corner fittings sit above the planks: align plank surface, not mesh maximum.
                deckArt.localPosition=deck.transform.localPosition+Vector3.down*.42f;
                var originalDeckRenderer=deck.GetComponent<Renderer>();
                if(originalDeckRenderer) originalDeckRenderer.enabled=false;
                foreach(var r in zone.GetComponentsInChildren<Renderer>())
                    if(r.name=="Harness plank") r.enabled=false;
            }
            foreach(var job in game.jobs)
            {
                if(job.tool==CareTool.Sprayer) continue; // Hot spot remains a gameplay marker, not a burr job.
                if(job.visual) UnityEngine.Object.DestroyImmediate(job.visual.gameObject);
                var holder = Group("Imported care target",job.transform);
                holder.localPosition=Vector3.up*.08f;
                Place(job.tool==CareTool.Vacuum?"bramblenmite/bramblenmite":"tangled-fur-clump/tangled-fur-clump",holder,Vector3.zero,1.4f);
                job.visual=holder;
            }
            var supply=Group("Imported supplies",frame.transform);
            float humanScale=1/frame.transform.lossyScale.x;
            Place("chest/closed-chest",supply,new Vector3(-2.7f,12.1f,-13),1.2f*humanScale);
            Place("tool-case/tool-case",supply,new Vector3(-2.6f,12.1f,-11),.75f*humanScale);
            Place("treatment-bottle/treatment-bottle",supply,new Vector3(-3.4f,12.1f,-11),.35f*humanScale);
            Place("spare-canister/spare-canister",supply,new Vector3(-3.4f,12.1f,-12),.6f*humanScale);
            foreach(var r in frame.GetComponentsInChildren<Renderer>()) if(r.name=="Guild supply crate")
            {
                r.enabled=false;
                r.transform.localScale=new Vector3(1.2f,.95f,.8f)*humanScale;
                r.transform.localPosition=new Vector3(-2.7f,12.1f+.475f*humanScale,-13);
            }
            var beacon=Group("Imported extraction beacon",frame.transform);
            beacon.position=game.extraction.position;
            Place("extraction-beacon/beacon-clean",beacon,Vector3.up*.03f,.65f);
            var tools=Group("Imported held tools",game.player.model);
            tools.localPosition=new Vector3(.4f,.9f,.25f);
            var selection=tools.gameObject.AddComponent<TechnicianToolVisuals>();
            selection.motor=game.player;
            selection.vacuum=Place("pest-vacuum/pest-vacuum",tools,Vector3.zero,.6f);
            selection.brush=Place("grooming-brush/grooming-brush",tools,Vector3.zero,.4f);
            selection.sprayer=Place("soothing-sprayer/soothing-sprayer",tools,Vector3.zero,.45f);
            selection.ropeLauncher=Place("rope-launcher/rope-launcher",tools,Vector3.zero,.6f);
            selection.ropeLauncher.transform.localRotation=Quaternion.Euler(0,90,0);
            selection.ropeLauncher.SetActive(false);
            selection.brush.SetActive(false); selection.sprayer.SetActive(false);
            var meadow=GameObject.Find("Migratory meadow").transform;
            foreach(var r in meadow.GetComponentsInChildren<Renderer>())
                if(r.name=="Tree trunk" || r.name=="Round canopy") r.enabled=false;
            var scenery=Group("Imported scenery",meadow);
            var random=new System.Random(71);
            string[] treeNames={"broad-tree","tall-tree","bent-tree"};
            string[] rockNames={"large-rock","medium-rock","small-rock","rock-arch"};
            for(int i=0;i<44;i++)
            {
                float x=(float)(random.NextDouble()*270-135), z=(float)(random.NextDouble()*270-135);
                if(Mathf.Abs(x)<42 && Mathf.Abs(z)<85) x=Mathf.Sign(x)*50;
                var t=Place("trees/"+treeNames[i%3],scenery,new Vector3(x,-5.5f,z),8+(float)random.NextDouble()*10);
                t.transform.localRotation=Quaternion.Euler(0,(float)random.NextDouble()*360,0);
            }
            for(int i=0;i<16;i++)
            {
                float x=(i%2==0?-1:1)*(38+i*4),z=-65+i*9;
                Place("rocks/"+rockNames[i%4],scenery,new Vector3(x,-5.5f,z),i%4==3?10:3+i%3);
                Place("plants/plant-group",scenery,new Vector3(x+4,-5.5f,z+3),1.4f);
                Place("plants/moss",scenery,new Vector3(x-3,-5.5f,z-2),1.2f);
            }
            // Ground-side staging dock; no new boarding mechanic is implied.
            Place("boarding-dock/boarding-dock",scenery,new Vector3(-40,-5.5f,-45),8);
            MeadowDecorator.Apply();
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
            EditorSceneManager.SaveScene(game.gameObject.scene);
            Debug.Log("GiantTiny models applied. Kit prefabs are separated; existing gameplay colliders retained.");
        }
        public static void PreviewAll() => PreviewFolders(null);
        public static void PreviewUpdated() => PreviewFolders(UpdatedFolders);
        static void PreviewFolders(string[] only)
        {
            Directory.CreateDirectory("tmp/gianttiny/previews");
            foreach (var path in Directory.GetFiles(Root, "*.prefab", SearchOption.AllDirectories))
            {
                if(only!=null && !only.Contains(Path.GetFileName(Path.GetDirectoryName(path)))) continue;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path.Replace('\\','/')));
                go.transform.position = new Vector3(10000, 0, 0);
                foreach (var t in go.GetComponentsInChildren<Transform>()) t.gameObject.layer = 31;
                var cameraGo = new GameObject("Asset preview");
                var camera = cameraGo.AddComponent<Camera>();
                camera.cullingMask = 1 << 31;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.7f,.75f,.78f);
                camera.orthographic = true;
                camera.orthographicSize = .75f;
                camera.transform.position = go.transform.position + new Vector3(1.5f,1.1f,2);
                camera.transform.LookAt(go.transform.position + Vector3.up * .4f);
                var rt = new RenderTexture(512,512,24);
                var previous = RenderTexture.active;
                camera.targetTexture = rt;
                camera.Render();
                RenderTexture.active = rt;
                var image = new Texture2D(512,512,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,512,512),0,0); image.Apply();
                File.WriteAllBytes("tmp/gianttiny/previews/" + Path.GetFileNameWithoutExtension(path) + ".png", image.EncodeToPNG());
                RenderTexture.active = previous;
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(cameraGo);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
