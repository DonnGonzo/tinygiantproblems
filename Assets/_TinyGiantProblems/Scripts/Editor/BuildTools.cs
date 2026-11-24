using System.IO;
using UnityEditor;
using UnityEngine;

namespace TinyGiantProblems.Editor
{
    public static class BuildTools
    {
        [MenuItem("Tiny Giant Problems/Build Windows Player")]
        public static void BuildWindows()
        {
            Directory.CreateDirectory("Builds/Windows");
            var report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, "Builds/Windows/TinyGiantProblems.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new System.Exception("Windows build failed: " + report.summary.result);
            Debug.Log("Windows build succeeded: " + report.summary.totalSize + " bytes");
        }
    }
}
