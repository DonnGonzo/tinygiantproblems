using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TinyGiantProblems.Editor
{
    // Editor side of the November content drop: prepares the decimated
    // character, climbing claw and mountain (Tools/import_new_models.py),
    // registers them in the ItemLibrary and builds the technician's code rig.
    // Re-run "Tiny Giant Problems/November content/Rebuild all" after
    // re-exporting any of the GLBs.
    public static class NovemberContent
    {
        public static readonly string[] Folders = { "technician", "climbing-claw", "mountain", "floating-platform" };

        [MenuItem("Tiny Giant Problems/November content/Rebuild all")]
        public static void RebuildAll()
        {
            PrepareModels();
            TechnicianRigBuilder.Apply();
            RegisterLibrary();
            Debug.Log("November content rebuilt: technician rig, climbing claw, mountain.");
        }

        public static void PrepareModels()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
            AssetDatabase.Refresh();
            foreach (var slug in Folders)
            {
                string folder = GiantTinyAssets.Root + "/" + slug, path = folder + "/" + slug + ".obj";
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                if (!importer) throw new InvalidOperationException("Run Tools/import_new_models.py first: " + path);
                importer.globalScale = 1;
                // The technician's OBJ groups (usemtl) only become separate
                // submeshes when material import is on; its materials are replaced below.
                bool regions = slug == "technician" || File.Exists(folder + "/" + slug + ".palette.json");
                importer.materialImportMode = regions ? ModelImporterMaterialImportMode.ImportStandard : ModelImporterMaterialImportMode.None;
                // The guild house meshes are cut at runtime (doorway, inner floors), which needs CPU-readable data.
                importer.isReadable = slug == "technician";
                importer.indexFormat = ModelImporterIndexFormat.UInt32;
                // Decimation leaves the source normals stale; smooth fresh ones.
                importer.importNormals = ModelImporterNormals.Calculate;
                importer.normalSmoothingAngle = 80;
                importer.importTangents = ModelImporterTangents.CalculateMikk;
                importer.SaveAndReimport();
                var material = Surface(folder);
                var regionMaterials = slug == "technician" ? TechnicianMaterials(folder) : PaletteMaterials(folder, slug);
                var root = new GameObject(slug);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), root.transform);
                foreach (var r in model.GetComponentsInChildren<Renderer>())
                    if (regionMaterials != null) r.sharedMaterials = regionMaterials; else r.sharedMaterial = material;
                var b = GiantTinyAssets.BoundsOf(model);
                float scale = 1f / Mathf.Max(b.size.x, b.size.y, b.size.z);
                model.transform.localScale = Vector3.one * scale;
                model.transform.localPosition = -new Vector3(b.center.x, b.min.y, b.center.z) * scale;
                if (slug == "mountain" || slug == "floating-platform")
                {
                    var filter = model.GetComponentInChildren<MeshFilter>();
                    var collider = filter.gameObject.AddComponent<MeshCollider>();
                    collider.sharedMesh = WorldCollisionBuilder.CollisionMesh(slug + "/" + slug);
                    root.AddComponent<PropCollisionPolicy>().surfaces = new Collider[] { collider };
                }
                PrefabUtility.SaveAsPrefabAsset(root, folder + "/" + slug + ".prefab");
                Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets();
        }

        // One soft, flat-coloured material per technician colour region
        // (Tools/import_new_models.py REGIONS order). The Guild Member editor
        // tints these per region at runtime.
        [System.Serializable] sealed class Palette { public string[] names; public float[] flat; }

        // Flat-colour materials from Tools/import_new_models.py's palette file
        // (one per OBJ colour group), or null when the model keeps its texture.
        static Material[] PaletteMaterials(string folder, string slug)
        {
            string path = folder + "/" + slug + ".palette.json";
            if (!File.Exists(path)) return null;
            var json = File.ReadAllText(path);
            var names = JsonUtility.FromJson<Palette>(json).names;
            // colours are a list of [r,g,b] lists, which JsonUtility cannot read: parse by hand.
            var nums = System.Text.RegularExpressions.Regex.Matches(json.Substring(json.IndexOf("colors")), @"-?\d+(\.\d+)?(e-?\d+)?").Cast<System.Text.RegularExpressions.Match>().Select(m => float.Parse(m.Value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            var mats = new Material[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                string mp = folder + "/" + slug + " " + names[i] + ".mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(mp);
                if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, mp); }
                var c = new Color(nums[i * 3], nums[i * 3 + 1], nums[i * 3 + 2]);
                m.SetColor("_BaseColor", c); m.SetTexture("_BaseMap", null);
                m.SetFloat("_Metallic", 0); m.SetFloat("_Smoothness", .15f); m.SetFloat("_Cull", 0);
                m.enableInstancing = true;
                EditorUtility.SetDirty(m);
                mats[i] = m;
            }
            return mats;
        }

        static Material[] TechnicianMaterials(string folder)
        {
            var mats = new Material[TechnicianLook.RegionNames.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                string path = folder + "/Technician " + TechnicianLook.RegionNames[i] + ".mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
                m.SetColor("_BaseColor", TechnicianLook.Defaults[i]);
                m.SetTexture("_BaseMap", null);
                m.SetFloat("_Metallic", 0);
                m.SetFloat("_Smoothness", i == TechnicianLook.Goggles ? .75f : i == TechnicianLook.Accents ? .45f : .2f);
                m.SetFloat("_Cull", 0);
                m.enableInstancing = true;
                EditorUtility.SetDirty(m);
                mats[i] = m;
            }
            return mats;
        }

        static Material Surface(string folder)
        {
            string materialPath = folder + "/Surface.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, materialPath); }
            var baseImporter = (TextureImporter)AssetImporter.GetAtPath(folder + "/BaseColor.png");
            if (baseImporter.maxTextureSize != 2048) { baseImporter.maxTextureSize = 2048; baseImporter.SaveAndReimport(); }
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/BaseColor.png");
            material.color = Color.white;
            material.enableInstancing = true;
            // The generated maps have tiny UV islands whose gutters hold full
            // metal/gloss, which read as white cracks; keep albedo + occlusion
            // and a soft, non-metallic surface instead.
            var occlusion = (TextureImporter)AssetImporter.GetAtPath(folder + "/Occlusion.png");
            occlusion.sRGBTexture = false; occlusion.maxTextureSize = 2048; occlusion.SaveAndReimport();
            material.SetTexture("_BumpMap", null); material.DisableKeyword("_NORMALMAP");
            material.SetTexture("_MetallicGlossMap", null); material.DisableKeyword("_METALLICSPECGLOSSMAP");
            material.SetTexture("_OcclusionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/Occlusion.png")); material.EnableKeyword("_OCCLUSIONMAP");
            material.SetFloat("_Metallic", 0); material.SetFloat("_Smoothness", .22f); material.SetFloat("_Cull", 0);
            EditorUtility.SetDirty(material);
            return material;
        }

        public static void RegisterLibrary()
        {
            var lib = Resources.Load<ItemLibrary>("ItemLibrary");
            var list = lib.entries.ToList();
            foreach (var key in new[] { "climbing-claw/climbing-claw", "mountain/mountain", "floating-platform/floating-platform", "extraction-beacon/beacon-clean", "guild-workshop-furniture-kit/stool", "technician-cosmetic-kit/hat", "technician-cosmetic-kit/scarf", TechnicianRigBuilder.Key })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GiantTinyAssets.Root + "/" + key + ".prefab");
                if (!prefab) throw new InvalidOperationException("Missing prefab " + key);
                list.RemoveAll(e => e.key == key);
                list.Add(new ItemLibrary.Entry { key = key, prefab = prefab });
            }
            lib.entries = list.ToArray();
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
        }


        // Play-mode review shot from a camera placed in the technician's model
        // frame (x right, y up, z forward), HUD included. Screenshots/<name>.png
        public static string Shot(string name, Vector3 eyeLocal, Vector3 lookLocal, int width = 1200, int height = 900, float fov = 40)
        {
            var g = Expedition.Instance; var frame = g.player.model;
            var cam = new GameObject("Review camera").AddComponent<Camera>();
            cam.fieldOfView = fov; cam.nearClipPlane = .03f; cam.farClipPlane = 900;
            cam.transform.position = frame.TransformPoint(eyeLocal);
            cam.transform.LookAt(frame.TransformPoint(lookLocal));
            return Render(cam, name, width, height);
        }

        public static string WorldShot(string name, Vector3 eye, Vector3 look, int width = 1600, int height = 900, float fov = 50)
        {
            var cam = new GameObject("Review camera").AddComponent<Camera>();
            cam.fieldOfView = fov; cam.nearClipPlane = .1f; cam.farClipPlane = 1500;
            cam.transform.position = eye; cam.transform.LookAt(look);
            return Render(cam, name, width, height);
        }

        static string Render(Camera cam, string name, int width, int height)
        {
            var main = Camera.main;
            if (main) { cam.clearFlags = main.clearFlags; cam.backgroundColor = main.backgroundColor; var src = main.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); if (src) { var dst = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); if (!dst) dst = cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); dst.renderPostProcessing = src.renderPostProcessing; } }
            var rt = new RenderTexture(width, height, 24);
            cam.targetTexture = rt; cam.Render();
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0); tex.Apply();
            Directory.CreateDirectory("Screenshots");
            File.WriteAllBytes("Screenshots/" + name + ".png", tex.EncodeToPNG());
            RenderTexture.active = prev; cam.targetTexture = null;
            Object.DestroyImmediate(tex); rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(cam.gameObject);
            return "Screenshots/" + name + ".png";
        }

        // Puts every tool in the bag (play-mode review only).
        public static void GiveAllTools()
        {
            var e = GuildEconomy.Instance;
            foreach (var d in ItemCatalog.All) if (d.IsTool && !e.Inventory.Has(d.id)) e.Inventory.Add(d.id, 1);
        }
        public static void Select(ItemId id)
        {
            var inv = GuildEconomy.Instance.Inventory;
            for (int i = 0; i < Inventory.Size; i++)
                if (inv.slots[i].id == id) { if (i >= Inventory.HotbarSize) { inv.MoveOrSwap(i, 8); i = 8; } inv.Select(i); return; }
        }


        public const string IconFolder = "Assets/_TinyGiantProblems/Resources/ItemIcons";

        // Renders every item with a model to a 256 px transparent PNG for the UI.
        [MenuItem("Tiny Giant Problems/Bake item icons")]
        public static string BakeIcons()
        {
            Directory.CreateDirectory(IconFolder);
            int n = 0;
            foreach (var def in ItemCatalog.All)
            {
                if (!ItemIcons.HasPicture(def)) continue;
                var tex = ItemIcons.Render(def, 256);
                if (!tex) continue;
                string path = IconFolder + "/" + def.id + ".png";
                var png = tex.EncodeToPNG();
                // Unity can briefly hold an imported texture open; retry the write.
                for (int attempt = 0; ; attempt++)
                {
                    try { File.WriteAllBytes(path, png); break; }
                    catch (IOException) when (attempt < 5) { System.Threading.Thread.Sleep(250); }
                }
                Object.DestroyImmediate(tex);
                n++;
            }
            AssetDatabase.Refresh();
            foreach (var file in Directory.GetFiles(IconFolder, "*.png"))
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(file.Replace(Path.DirectorySeparatorChar, '/'));
                ti.textureType = TextureImporterType.Default; ti.alphaIsTransparency = true; ti.mipmapEnabled = false;
                ti.wrapMode = TextureWrapMode.Clamp; ti.maxTextureSize = 256; ti.SaveAndReimport();
            }
            return n + " icons baked to " + IconFolder;
        }

        // Turntable previews of a prefab to Screenshots/<name>-<angle>.png.
        public static string Preview(string prefabPath, string name, float height = 1, params float[] angles)
        {
            if (angles.Length == 0) angles = new[] { 0f, 90f, 180f, 270f };
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = new Vector3(5000, 0, 0);
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
            var light = new GameObject("Preview light").AddComponent<Light>();
            light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(40, 30, 0); light.cullingMask = 1 << 31; light.intensity = 1.4f;
            var cam = new GameObject("Preview camera").AddComponent<Camera>();
            cam.cullingMask = 1 << 31; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.62f, .7f, .74f);
            cam.orthographic = true; cam.orthographicSize = height * .6f; cam.nearClipPlane = .01f; cam.farClipPlane = 50;
            Directory.CreateDirectory("Screenshots");
            var rt = new RenderTexture(700, 700, 24);
            var tex = new Texture2D(700, 700, TextureFormat.RGB24, false);
            var center = go.transform.position + Vector3.up * height * .5f;
            foreach (var a in angles)
            {
                cam.transform.position = center + Quaternion.Euler(0, a, 0) * Vector3.forward * 5;
                cam.transform.LookAt(center);
                cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, 700, 700), 0, 0); tex.Apply();
                File.WriteAllBytes($"Screenshots/{name}-{a:0}.png", tex.EncodeToPNG());
            }
            RenderTexture.active = null; cam.targetTexture = null;
            Object.DestroyImmediate(tex); rt.Release(); Object.DestroyImmediate(rt);
            Object.DestroyImmediate(cam.gameObject); Object.DestroyImmediate(light.gameObject); Object.DestroyImmediate(go);
            return "Screenshots/" + name + "-*.png";
        }
    }
}
