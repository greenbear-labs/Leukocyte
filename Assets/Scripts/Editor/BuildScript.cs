using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Leukocyte.EditorTools
{
    /// <summary>Command-line and menu builds.
    /// Batch mode: Unity -batchmode -quit -projectPath . -executeMethod Leukocyte.EditorTools.BuildScript.BuildMac -buildPath ~/Desktop/Leukocyte.app</summary>
    public static class BuildScript
    {
        const string ScenePath = "Assets/Scenes/FemoralArtery.unity";

        [MenuItem("Leukocyte/Build macOS App")]
        public static void BuildMac() => Build(BuildTarget.StandaloneOSX, "Builds/Leukocyte.app");

        [MenuItem("Leukocyte/Build Windows Player")]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Builds/Windows/Leukocyte.exe");

        static void Build(BuildTarget target, string defaultPath)
        {
            if (!File.Exists(ScenePath)) LeukocyteSceneSetup.CreateScene();

            string path = ArgValue("-buildPath") ?? defaultPath;
            if (path.StartsWith("~"))
                path = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + path.Substring(1);

            PlayerSettings.productName = "Leukocyte";
            PlayerSettings.companyName = "greenbear-labs";
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = path,
                target = target,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"[Leukocyte] Build {summary.result}: {path} ({summary.totalSize / (1024 * 1024)} MB, {summary.totalErrors} errors)");
            if (Application.isBatchMode && summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        static string ArgValue(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
