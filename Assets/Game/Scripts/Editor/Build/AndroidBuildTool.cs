using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Android APK/AAB build tool. Ported from TBO's AutoBundleVersionTool.
    /// Use from the menu (Tools/Build/Android Build Tool) or from the command line
    /// via tools/build/build-android.bat (calls <see cref="BuildFromCommandLine"/>).
    /// Signing secrets are never stored in the project: the window keeps the
    /// passwords in memory only, the CLI reads them from environment variables.
    /// </summary>
    public class AndroidBuildTool : EditorWindow
    {
        public enum BuildKind { APK, AAB }

        const string PrefKeystore = "Game.Build.Keystore";
        const string PrefAlias = "Game.Build.Alias";
        const string PrefOutDir = "Game.Build.OutDir";
        const string PrefKind = "Game.Build.Kind";
        const string PrefDev = "Game.Build.Dev";
        const string DefaultOutDir = "Builds";

        string _versionName;
        int _versionCode;
        BuildKind _kind = BuildKind.APK;
        bool _development;
        bool _autoCode = true;
        bool _useKeystore;
        string _keystorePath;
        string _alias;
        string _storePass = "";
        string _aliasPass = "";
        string _outDir;

        [MenuItem("Tools/Build/Android Build Tool")]
        public static void ShowWindow() => GetWindow<AndroidBuildTool>("Android Build");

        void OnEnable()
        {
            _versionName = PlayerSettings.bundleVersion;
            _versionCode = PlayerSettings.Android.bundleVersionCode;
            _keystorePath = EditorPrefs.GetString(PrefKeystore, "");
            _alias = EditorPrefs.GetString(PrefAlias, "");
            _outDir = EditorPrefs.GetString(PrefOutDir, DefaultOutDir);
            _kind = (BuildKind)EditorPrefs.GetInt(PrefKind, 0);
            _development = EditorPrefs.GetBool(PrefDev, false);
            _useKeystore = !string.IsNullOrEmpty(_keystorePath);
        }

        void OnGUI()
        {
            GUILayout.Label("Version", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Package", PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android));
            _versionName = EditorGUILayout.TextField("Version Name", _versionName);
            using (new EditorGUI.DisabledScope(_autoCode))
                _versionCode = EditorGUILayout.IntField("Version Code", _versionCode);
            _autoCode = EditorGUILayout.Toggle("Auto code (yyMMddHH)", _autoCode);

            GUILayout.Space(8);
            GUILayout.Label("Output", EditorStyles.boldLabel);
            _kind = (BuildKind)EditorGUILayout.EnumPopup("Build Type", _kind);
            _development = EditorGUILayout.Toggle("Development build", _development);
            using (new EditorGUILayout.HorizontalScope())
            {
                _outDir = EditorGUILayout.TextField("Output folder", _outDir);
                if (GUILayout.Button("...", GUILayout.Width(28)))
                {
                    var picked = EditorUtility.OpenFolderPanel("Output folder", ProjectRoot(), "");
                    if (!string.IsNullOrEmpty(picked)) _outDir = picked;
                }
            }

            GUILayout.Space(8);
            GUILayout.Label("Signing", EditorStyles.boldLabel);
            _useKeystore = EditorGUILayout.Toggle("Use custom keystore", _useKeystore);
            using (new EditorGUI.DisabledScope(!_useKeystore))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    _keystorePath = EditorGUILayout.TextField("Keystore", _keystorePath);
                    if (GUILayout.Button("...", GUILayout.Width(28)))
                    {
                        var picked = EditorUtility.OpenFilePanel("Keystore", ProjectRoot(), "keystore,jks");
                        if (!string.IsNullOrEmpty(picked)) _keystorePath = picked;
                    }
                }
                _alias = EditorGUILayout.TextField("Alias", _alias);
                _storePass = EditorGUILayout.PasswordField("Keystore password", _storePass);
                _aliasPass = EditorGUILayout.PasswordField("Alias password", _aliasPass);
            }
            if (!_useKeystore)
                EditorGUILayout.HelpBox("No keystore: build is signed with the Unity debug key (fine for testing, not for Play Store).", MessageType.Info);
            else
                EditorGUILayout.HelpBox("Passwords stay in memory only and are never saved to the project.", MessageType.None);

            GUILayout.Space(12);
            using (new EditorGUI.DisabledScope(EditorApplication.isCompiling || EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Build " + _kind, GUILayout.Height(32)))
                    RunFromWindow();
            }
            if (GUILayout.Button("Open output folder"))
                EditorUtility.RevealInFinder(Path.GetFullPath(ResolveOutDir(_outDir)));
        }

        void RunFromWindow()
        {
            EditorPrefs.SetString(PrefKeystore, _useKeystore ? _keystorePath : "");
            EditorPrefs.SetString(PrefAlias, _alias);
            EditorPrefs.SetString(PrefOutDir, _outDir);
            EditorPrefs.SetInt(PrefKind, (int)_kind);
            EditorPrefs.SetBool(PrefDev, _development);

            var cfg = new BuildConfig
            {
                Kind = _kind,
                VersionName = _versionName,
                VersionCode = _autoCode ? AutoVersionCode() : _versionCode,
                Development = _development,
                OutDir = _outDir,
                KeystorePath = _useKeystore ? _keystorePath : null,
                Alias = _alias,
                StorePass = _storePass,
                AliasPass = _aliasPass,
            };

            if (!Execute(cfg, out var path, out var error))
            {
                EditorUtility.DisplayDialog("Build failed", error, "OK");
                return;
            }
            _versionCode = cfg.VersionCode;
            EditorUtility.DisplayDialog("Build succeeded", path, "OK");
            EditorUtility.RevealInFinder(path);
        }

        // ------------------------------------------------------------------
        // Command line:  Unity -batchmode -quit -executeMethod
        //   Game.EditorTools.AndroidBuildTool.BuildFromCommandLine
        //   -buildType apk|aab  [-versionName 1.0.1] [-versionCode N]
        //   [-outDir Builds] [-development]
        // Signing (optional) from env: KEYSTORE_PATH, KEYSTORE_PASS, KEY_ALIAS, KEY_PASS
        // ------------------------------------------------------------------
        public static void BuildFromCommandLine()
        {
            try
            {
                var args = Environment.GetCommandLineArgs();
                var kindStr = Arg(args, "-buildType") ?? "apk";
                var cfg = new BuildConfig
                {
                    Kind = kindStr.Equals("aab", StringComparison.OrdinalIgnoreCase) ? BuildKind.AAB : BuildKind.APK,
                    VersionName = Arg(args, "-versionName") ?? PlayerSettings.bundleVersion,
                    OutDir = Arg(args, "-outDir") ?? DefaultOutDir,
                    Development = args.Contains("-development"),
                    KeystorePath = NullIfEmpty(Environment.GetEnvironmentVariable("KEYSTORE_PATH")),
                    StorePass = Environment.GetEnvironmentVariable("KEYSTORE_PASS") ?? "",
                    Alias = Environment.GetEnvironmentVariable("KEY_ALIAS") ?? "",
                    AliasPass = Environment.GetEnvironmentVariable("KEY_PASS") ?? "",
                };
                var codeStr = Arg(args, "-versionCode");
                cfg.VersionCode = int.TryParse(codeStr, out var code) ? code : AutoVersionCode();

                if (Execute(cfg, out var path, out var error))
                {
                    Debug.Log("[AndroidBuild] SUCCESS: " + path);
                    EditorApplication.Exit(0);
                }
                else
                {
                    Debug.LogError("[AndroidBuild] FAILED: " + error);
                    EditorApplication.Exit(1);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        // ------------------------------------------------------------------

        class BuildConfig
        {
            public BuildKind Kind;
            public string VersionName;
            public int VersionCode;
            public bool Development;
            public string OutDir;
            public string KeystorePath, Alias, StorePass, AliasPass;
        }

        static bool Execute(BuildConfig cfg, out string outputPath, out string error)
        {
            outputPath = null;
            error = null;

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) { error = "No enabled scenes in Build Settings."; return false; }

            if (cfg.KeystorePath != null)
            {
                if (!File.Exists(cfg.KeystorePath)) { error = "Keystore not found: " + cfg.KeystorePath; return false; }
                if (string.IsNullOrEmpty(cfg.Alias) || string.IsNullOrEmpty(cfg.StorePass))
                { error = "Keystore alias/password missing."; return false; }
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            { error = "Could not switch build target to Android."; return false; }

            // Play Store needs 64-bit: IL2CPP + ARM64 (+ ARMv7 for older devices).
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;

            PlayerSettings.bundleVersion = cfg.VersionName;
            PlayerSettings.Android.bundleVersionCode = cfg.VersionCode;
            EditorUserBuildSettings.buildAppBundle = cfg.Kind == BuildKind.AAB;

            PlayerSettings.Android.useCustomKeystore = cfg.KeystorePath != null;
            if (cfg.KeystorePath != null)
            {
                PlayerSettings.Android.keystoreName = cfg.KeystorePath;
                PlayerSettings.Android.keystorePass = cfg.StorePass;
                PlayerSettings.Android.keyaliasName = cfg.Alias;
                PlayerSettings.Android.keyaliasPass = string.IsNullOrEmpty(cfg.AliasPass) ? cfg.StorePass : cfg.AliasPass;
            }

            var dir = ResolveOutDir(cfg.OutDir);
            Directory.CreateDirectory(dir);
            var ext = cfg.Kind == BuildKind.AAB ? "aab" : "apk";
            var file = $"{Safe(PlayerSettings.productName)}_{Safe(cfg.VersionName)}_{cfg.VersionCode}{(cfg.Development ? "_dev" : "")}.{ext}";
            outputPath = Path.GetFullPath(Path.Combine(dir, file));

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = cfg.Development ? BuildOptions.Development : BuildOptions.None,
            });

            // Do not leave secrets in ProjectSettings.asset.
            if (cfg.KeystorePath != null)
            {
                PlayerSettings.Android.keystorePass = "";
                PlayerSettings.Android.keyaliasPass = "";
                PlayerSettings.Android.useCustomKeystore = false;
            }

            if (report.summary.result != BuildResult.Succeeded)
            {
                error = $"Result: {report.summary.result}, errors: {report.summary.totalErrors}";
                return false;
            }
            return true;
        }

        static int AutoVersionCode() => int.Parse(DateTime.Now.ToString("yyMMddHH"));

        static string ProjectRoot() => Path.GetDirectoryName(Application.dataPath);

        static string ResolveOutDir(string dir) =>
            Path.IsPathRooted(dir) ? dir : Path.Combine(ProjectRoot(), string.IsNullOrEmpty(dir) ? DefaultOutDir : dir);

        static string Safe(string s)
        {
            if (string.IsNullOrEmpty(s)) return "Unknown";
            foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s.Trim().Replace(' ', '_');
        }

        static string NullIfEmpty(string s) => string.IsNullOrEmpty(s) ? null : s;

        static string Arg(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
