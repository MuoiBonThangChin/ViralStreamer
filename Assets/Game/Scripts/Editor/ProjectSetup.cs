using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StreamStar.EditorTools
{
    /// <summary>
    /// One-shot project configuration. Runs from the menu or headless:
    /// Unity.exe -batchmode -quit -projectPath . -executeMethod StreamStar.EditorTools.ProjectSetup.Run
    /// </summary>
    public static class ProjectSetup
    {
        const string ScenePath = "Assets/Game/Scenes/Main.unity";
        const string HapticsDefine = "MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED";

        [MenuItem("Stream Star/Run Project Setup")]
        public static void Run()
        {
            ConfigurePlayer();
            CreateScene();
            AssetDatabase.SaveAssets();
            Debug.Log("[StreamStar] Project setup complete.");
        }

        [MenuItem("Stream Star/Wipe Save Data")]
        public static void WipeSave()
        {
            GameState.WipeSave();
            Debug.Log("[StreamStar] Save data wiped.");
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "StreamStar Studio";
            PlayerSettings.productName = "Stream Star Idle Tycoon";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.streamstar.idletycoon");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.streamstar.idletycoon");
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;

            // Feel's haptic feedbacks compile only with this symbol
            foreach (var target in new[] { NamedBuildTarget.Standalone, NamedBuildTarget.Android, NamedBuildTarget.iOS })
            {
                var defines = PlayerSettings.GetScriptingDefineSymbols(target).Split(';').Where(d => d.Length > 0).ToList();
                if (defines.Contains(HapticsDefine)) continue;
                defines.Add(HapticsDefine);
                PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", defines));
            }
        }

        static void CreateScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            if (!File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
                var camera = cameraGo.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Palette.Navy;
                camera.orthographic = true;
                cameraGo.AddComponent<AudioListener>();
                cameraGo.transform.position = new Vector3(0f, 0f, -10f);

                new GameObject("GameBootstrap").AddComponent<GameBootstrap>();

                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            AssetDatabase.Refresh();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }
    }
}
