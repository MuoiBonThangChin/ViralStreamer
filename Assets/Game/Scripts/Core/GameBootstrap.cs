using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StreamStar
{
    /// <summary>
    /// Entry point. Builds camera, canvas, Feel managers and the UI entirely from code, so the scene file
    /// only has to exist. Add this component to a scene object, or let the auto-boot below create it.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        static bool _booted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _booted = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBoot()
        {
            if (_booted) return;
            if (FindFirstObjectByType<GameBootstrap>() != null) return; // the scene's own bootstrap will run
            new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
        }

        void Awake()
        {
            if (_booted)
            {
                Destroy(gameObject);
                return;
            }
            _booted = true;
            Boot();
        }

        void Boot()
        {
            Application.targetFrameRate = 60;
            Input.multiTouchEnabled = true;

            EnsureCamera();
            EnsureEventSystem();

            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f; // layout is authored against a 1080 wide column
            canvasGo.AddComponent<GraphicRaycaster>();

            var state = GameState.Load();
            double offlineCoins = state.ApplyOfflineEarnings(out double offlineSeconds);

            FeelKit.Create();
            var ui = MainUI.Create(canvas, state);
            FeelKit.I.StartMusic();

            if (offlineCoins > 0) ui.ShowOffline(offlineCoins, offlineSeconds);
        }

        static void EnsureCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                camera = go.AddComponent<Camera>();
            }
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Palette.Navy;
            camera.orthographic = true;
            if (FindFirstObjectByType<AudioListener>() == null) camera.gameObject.AddComponent<AudioListener>();
        }

        static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            var module = go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            module.AssignDefaultActions();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
