using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TinyGiantProblems.Editor
{
    public static class MossbackModelInstaller
    {
        const string Folder = "Assets/_TinyGiantProblems/Art/Creatures/Mossback/";
        public const string PrefabPath = Folder + "Mossback.prefab";

        [MenuItem("Tiny Giant Problems/Install Imported Mossback")]
        public static void Install()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before installing Mossback.");
            var frame = UnityEngine.Object.FindFirstObjectByType<CreatureFrame>();
            if (!frame) throw new InvalidOperationException("Open the Mossback playable scene first.");
            var importer = (ModelImporter)AssetImporter.GetAtPath(Folder + "Mossback.obj");
            importer.globalScale = 1;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.isReadable = false;
            importer.SaveAndReimport();
            var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "Mossback.mat");
            if (!material)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, Folder + "Mossback.mat");
            }
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "Mossback_BaseColor.png");
            material.color = Color.white;
            material.SetFloat("_Smoothness", .0964f);
            material.SetFloat("_Metallic", 0);
            var root = new GameObject("Mossback imported model");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "Mossback.obj"));
            model.transform.SetParent(root.transform, false);
            foreach (var renderer in model.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = material;
            var bounds = model.GetComponentsInChildren<Renderer>()[0].bounds;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(renderer.bounds);
            // Fit the visual to the established first-playable footprint; the stable collision frame remains independent.
            var scale = new Vector3(18f / bounds.size.x, 18f / bounds.size.y, 60f / bounds.size.z);
            model.transform.localScale = scale;
            model.transform.localPosition = new Vector3(0, 3.5f, -5) - Vector3.Scale(bounds.center, scale);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            Apply(frame);
            EditorUtility.SetDirty(material);
            EditorSceneManager.MarkSceneDirty(frame.gameObject.scene);
            EditorSceneManager.SaveScene(frame.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Imported Mossback installed; original traversal, care jobs, anchors and checkpoint retained.");
        }

        public static void Apply(CreatureFrame frame)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (!prefab) return;
            var visual = frame.visual;
            for (int i = visual.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(visual.GetChild(i).gameObject);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, visual);
            model.transform.localPosition = Vector3.zero;
            SetCreatureSize(frame);
            // Keep the proven collision routes visible as guild decking, rather than opaque green body blocks.
            var wood = AssetDatabase.LoadAssetAtPath<Material>("Assets/_TinyGiantProblems/Art/Materials/HarnessWood.mat");
            foreach (var zone in frame.GetComponentsInChildren<ClimbableZone>())
            {
                foreach (var renderer in zone.GetComponentsInChildren<MeshRenderer>())
                {
                    if (renderer.name == "Climbable flank") renderer.enabled = false;
                    if (renderer.name.EndsWith(" traversal")) renderer.sharedMaterial = wood;
                }
                var old = zone.transform.Find("Prototype harness supports");
                if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
                var supports = new GameObject("Prototype harness supports").transform;
                supports.SetParent(zone.transform, false);
                var deck = zone.GetComponentsInChildren<BoxCollider>().First(c => c.name.EndsWith(" traversal"));
                float z = deck.transform.localPosition.z;
                foreach (int side in new[] { -1, 1 })
                {
                    foreach (float offset in new[] { -1.2f, 1.2f })
                        Beam(supports, wood, new Vector3(side * 5.56f, 8.3f, z + offset), new Vector3(.12f, 7.5f, .12f));
                    for (float y = 4.8f; y < 12; y += .55f)
                        Beam(supports, wood, new Vector3(side * 5.56f, y, z), new Vector3(.15f, .12f, 2.5f));
                }
            }
        }

        [MenuItem("Tiny Giant Problems/Apply Mossback Size")]
        public static void ApplySizeToScene()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before resizing Mossback.");
            var frame = UnityEngine.Object.FindFirstObjectByType<CreatureFrame>();
            if (!frame) throw new InvalidOperationException("Open the Mossback playable scene first.");
            SetCreatureSize(frame);
            EditorSceneManager.MarkSceneDirty(frame.gameObject.scene);
            EditorSceneManager.SaveScene(frame.gameObject.scene);
        }

        static void SetCreatureSize(CreatureFrame frame)
        {
            // Triple all three dimensions, including the attached traversal surfaces.
            // Original feet are at local Y=-5.5; raise the root so they stay on the meadow.
            frame.transform.localScale = Vector3.one * 3f;
            frame.transform.position = new Vector3(frame.transform.position.x, 11f, frame.transform.position.z);
            var game = UnityEngine.Object.FindFirstObjectByType<Expedition>();
            if (game && game.player && game.player.checkpoint)
            {
                // Keep the human-sized technician and camera on the enlarged starting deck.
                var player = game.player;
                player.checkpoint.localPosition = new Vector3(-1, 12.3f, -12);
                player.transform.position = player.checkpoint.position;
            }
        }

        static void Beam(Transform parent, Material material, Vector3 position, Vector3 scale)
        {
            var beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam.name = "Harness ladder";
            beam.transform.SetParent(parent, false);
            beam.transform.localPosition = position;
            beam.transform.localScale = scale;
            beam.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(beam.GetComponent<Collider>());
        }

        public static string Preview()
        {
            var frame = UnityEngine.Object.FindFirstObjectByType<CreatureFrame>();
            var go = new GameObject("Temporary Mossback preview camera");
            var camera = go.AddComponent<Camera>();
            camera.transform.position = frame.transform.TransformPoint(new Vector3(36, 25, 43));
            camera.transform.LookAt(frame.transform.TransformPoint(new Vector3(0, 4, 3)));
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.60f,.76f,.75f);
            camera.farClipPlane = 500;
            var target = new RenderTexture(1200, 900, 24);
            camera.targetTexture = target;
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(1200, 900, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1200, 900), 0, 0);
            image.Apply();
            Directory.CreateDirectory("Screenshots");
            File.WriteAllBytes("Screenshots/Mossback-Imported.png", image.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(go);
            return Path.GetFullPath("Screenshots/Mossback-Imported.png");
        }
    }
}
