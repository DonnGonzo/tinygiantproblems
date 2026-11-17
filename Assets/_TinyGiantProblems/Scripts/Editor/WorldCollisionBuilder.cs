using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace TinyGiantProblems.Editor
{
    public static class WorldCollisionBuilder
    {
        public const string CollisionRoot="Assets/_TinyGiantProblems/Art/Collision/Imported";
        public static Mesh CollisionMesh(string relative)
        {
            string path=CollisionRoot+"/"+relative+".obj";
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            if(!importer)throw new Exception("Generate prop collision first: "+path);
            if(!importer.isReadable || importer.globalScale!=1){importer.isReadable=true;importer.globalScale=1;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();}
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().Single();
        }
        public static void ApplyLibrary()
        {
            if(EditorApplication.isPlaying)throw new Exception("Exit Play mode first");
            AssetDatabase.Refresh();
            foreach(var raw in Directory.GetFiles(GiantTinyAssets.Root,"*.prefab",SearchOption.AllDirectories))
            {
                string path=raw.Replace('\\','/');string relative=path.Substring(GiantTinyAssets.Root.Length+1).Replace(".prefab","");
                if(relative.StartsWith("mossback/"))continue;
                var mesh=CollisionMesh(relative);var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var source=root.GetComponentInChildren<MeshFilter>();
                    var collider=source.GetComponent<MeshCollider>();if(!collider)collider=source.gameObject.AddComponent<MeshCollider>();
                    collider.sharedMesh=mesh;collider.convex=false;collider.isTrigger=false;collider.enabled=true;
                    var policy=root.GetComponent<PropCollisionPolicy>();if(!policy)policy=root.AddComponent<PropCollisionPolicy>();
                    policy.surfaces=new Collider[]{collider};policy.traversalArt=relative.StartsWith("decking-and-rope-ladder-kit/") || relative.StartsWith("platforrms/");
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            BramblemiteRigBuilder.Apply();
            ApplyScene();
        }
        public static void ApplyScene()
        {
            if(!Object.FindFirstObjectByType<Expedition>())return;
            foreach(var policy in Object.FindObjectsByType<PropCollisionPolicy>(FindObjectsInactive.Include,FindObjectsSortMode.None))policy.Apply();
            var rigging=GameObject.Find("Finished harness rigging");
            if(rigging)foreach(var mesh in rigging.GetComponentsInChildren<MeshFilter>())
            {var c=mesh.GetComponent<MeshCollider>();if(!c)c=mesh.gameObject.AddComponent<MeshCollider>();c.sharedMesh=mesh.sharedMesh;c.convex=false;c.isTrigger=false;}
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();
        }
        public static string Validate()
        {
            int solid=0,excluded=0;
            foreach(var p in Object.FindObjectsByType<PropCollisionPolicy>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                bool exemption=p.GetComponentInParent<TechnicianMotor>() || p.GetComponentInParent<TechnicianToolVisuals>() || p.traversalArt && p.GetComponentInParent<ClimbableZone>();
                if(exemption){if(p.surfaces.Any(c=>c && c.enabled))throw new Exception("Self/duplicate collision: "+p.name);excluded++;continue;}
                if(p.surfaces==null || p.surfaces.Length==0 || p.surfaces.Any(c=>!c || !c.enabled || c.isTrigger))throw new Exception("Non-solid world prop: "+p.name);
                solid++;
            }
            if(solid<250)throw new Exception("Incomplete scenery collision coverage");
            return "PASS: "+solid+" solid world props; "+excluded+" held/worn or authored-traversal instances safely excluded.";
        }
    }
}
