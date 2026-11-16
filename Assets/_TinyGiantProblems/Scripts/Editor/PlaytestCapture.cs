using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TinyGiantProblems.Editor
{
    // Enters Play mode in the Mossback scene, drives PlaytestDriver and writes
    // screenshots + report.txt to TestResults/playtest. Works from the menu or
    // headless (without -nographics):
    //   Unity -batchmode -projectPath . -executeMethod TinyGiantProblems.Editor.PlaytestCapture.Run
    [InitializeOnLoad]
    public static class PlaytestCapture
    {
        const string Flag = "TGP.PlaytestCapture";
        const string Scene = "Assets/_TinyGiantProblems/Scenes/Mossback_FirstPlayable.unity";

        static PlaytestCapture()
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        [MenuItem("Tiny Giant Problems/Playtest Capture (screenshots + report)")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) { Start(); return; }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(Scene);
            SessionState.SetBool(Flag, true);
            EditorApplication.isPlaying = true;
        }

        static void OnPlayMode(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Flag, false)) Start();
            if (change == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Flag, false))
            {
                SessionState.EraseBool(Flag);
                if (Application.isBatchMode) EditorApplication.Exit(System.IO.File.ReadAllText("TestResults/playtest/report.txt").Contains("FAIL") ? 1 : 0);
            }
        }

        static void Start()
        {
            var driver = new GameObject("Playtest driver").AddComponent<PlaytestDriver>();
            driver.Finished = () => { if (SessionState.GetBool(Flag, false)) EditorApplication.isPlaying = false; };
            driver.Run();
        }
    }
}
