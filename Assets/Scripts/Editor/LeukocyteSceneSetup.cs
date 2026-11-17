using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Leukocyte.EditorTools
{
    /// <summary>Creates the greybox scene the first time the project opens, and from the Leukocyte menu.</summary>
    [InitializeOnLoad]
    static class LeukocyteSceneSetup
    {
        const string ScenePath = "Assets/Scenes/FemoralArtery.unity";
        const string SessionKey = "Leukocyte.SceneSetupChecked";

        static LeukocyteSceneSetup()
        {
            EditorApplication.delayCall += AutoCreate;
        }

        static void AutoCreate()
        {
            if (SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            LeukocyteAssets.EnsureMaterials();
            if (File.Exists(ScenePath)) return;
            CreateScene();
        }

        [MenuItem("Leukocyte/Create Femoral Artery Scene")]
        public static void CreateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            LeukocyteAssets.EnsureMaterials();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            LeukocyteAssets.ApplySceneRenderSettings();
            new GameObject("GameBootstrap").AddComponent<GameBootstrap>();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);

            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == ScenePath))
            {
                scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }

            Debug.Log("[Leukocyte] Created " + ScenePath + ". Press Play to deploy.");
        }
    }
}
