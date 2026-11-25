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
            if(File.Exists(WorldCollisionBuilder.CollisionRoot+"/manifest.json")) WorldCollisionBuilder.ApplyLibrary();
            AssetDatabase.SaveAssets();
        }
        public static Bounds BoundsOf(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
            return bounds;
        }
    }
}
