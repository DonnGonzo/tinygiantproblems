using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TinyGiantProblems.Editor
{
    public static class MossbackCollision
    {
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
            const string path = "Assets/_TinyGiantProblems/Art/Collision/MossbackCollision.obj";
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.globalScale = 1;
            importer.isReadable = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
            var mesh = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().Single();
            var frame = UnityEngine.Object.FindFirstObjectByType<CreatureFrame>();
            var body = frame.visual.Find("Mossback fitted visual").GetComponentInChildren<MeshFilter>();
            var old = body.transform.Find("Solid Mossback body");
            if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var go = new GameObject("Solid Mossback body");
            go.transform.SetParent(body.transform, false);
            var collider = go.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            collider.convex = false;
            collider.isTrigger = false;
            EditorSceneManager.MarkSceneDirty(frame.gameObject.scene);
        }
    }
}
