using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace StreamStar.EditorTools
{
    /// <summary>
    /// Headless play-mode check: boots the game, taps, buys, and fails on any logged error.
    /// Unity.exe -batchmode -projectPath . -executeMethod StreamStar.EditorTools.SmokeTest.Run
    /// </summary>
    [InitializeOnLoad]
    public static class SmokeTest
    {
        const string ActiveKey = "StreamStar.SmokeTest.Active";

        static int _frames;
        static int _step;
        static double _mark;
        static RenderTexture _target;
        static readonly List<string> Errors = new List<string>();

        static SmokeTest()
        {
            if (!SessionState.GetBool(ActiveKey, false)) return;
            Application.logMessageReceived += OnLog;
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        public static void Run()
        {
            SessionState.SetBool(ActiveKey, true);
            GameState.WipeSave();
            EditorSceneManager.OpenScene("Assets/Game/Scenes/Main.unity");
            Application.logMessageReceived += OnLog;
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.EnterPlaymode();
        }

        static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                Errors.Add(type + ": " + condition + "\n" + stackTrace);
        }

        static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredEditMode) return;
            GameState.WipeSave();
            SessionState.SetBool(ActiveKey, false);
            int count = SessionState.GetInt("StreamStar.SmokeTest.Errors", 0);
            Debug.Log("[SmokeTest] finished with " + count + " error(s)");
            EditorApplication.Exit(count > 0 ? 1 : 0);
        }

        static void Update()
        {
            if (!EditorApplication.isPlaying) return;
            _frames++;

            if (_step == 0 && _frames > 30)
            {
                _step = 1;
                // batchmode has no game view: point the canvas at a camera that renders into a texture
                var canvas = Object.FindFirstObjectByType<Canvas>();
                var camera = Camera.main;
                if (canvas != null && camera != null)
                {
                    _target = new RenderTexture(1080, 1920, 24);
                    camera.targetTexture = _target;
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                    canvas.planeDistance = 5f;
                }
                // some income so the HUD shows real numbers
                var tap = Object.FindFirstObjectByType<TapZone>();
                if (tap == null) Errors.Add("TapZone not found: UI was not built");
                else
                    for (int i = 0; i < 20; i++)
                        tap.OnTap(new Vector2(Screen.width * 0.5f, Screen.height * 0.55f));
                Click("Buy0_0"); // first gear row: starts passive income and the donation jar
                _mark = EditorApplication.timeSinceStartup;
            }
            else if (_step == 1 && _frames > 200 && EditorApplication.timeSinceStartup - _mark > 10.0) // long enough for the donation jar to show
            {
                _step = 2;
                Shot("main");
                var tap = Object.FindFirstObjectByType<TapZone>();
                if (tap != null)
                    for (int i = 0; i < 60; i++) // 40 taps fill the hype bar
                        tap.OnTap(new Vector2(Screen.width * 0.5f, Screen.height * 0.55f));
                Debug.Log("[SmokeTest] tapped");
                Click("NavGear");
            }
            else if (_step == 2 && _frames > 290)
            {
                _step = 3;
                Shot("sheet");
                int clicked = 0;
                foreach (var button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (button.name == "Ok") continue; // popup dismiss, exercised separately
                    button.onClick.Invoke();
                    clicked++;
                }
                Debug.Log("[SmokeTest] clicked " + clicked + " buttons");
            }
            else if (_step == 3 && _frames > 380)
            {
                _step = 4;
                Shot("all");
                foreach (var error in Errors) Debug.Log("[SmokeTest] ERROR " + error);
                SessionState.SetInt("StreamStar.SmokeTest.Errors", Errors.Count);
                EditorApplication.ExitPlaymode();
            }
        }

        static void Click(string buttonName)
        {
            foreach (var button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (button.name != buttonName) continue;
                button.onClick.Invoke();
                return;
            }
            Errors.Add("Button not found: " + buttonName);
        }

        /// <summary>Writes STREAMSTAR_SHOT with "-label" before the extension.</summary>
        static void Shot(string label)
        {
            string path = System.Environment.GetEnvironmentVariable("STREAMSTAR_SHOT") ?? "";
            if (path.Length == 0 || _target == null || Camera.main == null) return;
            path = System.IO.Path.ChangeExtension(path, null) + "-" + label + ".png";
            Camera.main.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = _target;
            var texture = new Texture2D(_target.width, _target.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, _target.width, _target.height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
            Debug.Log("[SmokeTest] screenshot " + path);
        }
    }
}
