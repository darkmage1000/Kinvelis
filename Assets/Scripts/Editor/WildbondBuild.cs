using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Wildbond.EditorTools
{
    /// <summary>Android APK build (IL2CPP / ARM64). Output: Builds/Wildbond.apk, then install with adb.</summary>
    public static class WildbondBuild
    {
        public const string ApkPath = "Builds/Wildbond.apk";
        public static string LastResult = "none";

        [MenuItem("Wildbond/Build Android APK")]
        public static void BuildApk()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            var t = NamedBuildTarget.Android;
            PlayerSettings.SetScriptingBackend(t, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.SetApplicationIdentifier(t, "com.wildbond.game");
            PlayerSettings.productName = GameInfo.Title;   // the name under the icon (the package id and file names stay as they are)
            AppIcon.Apply();   // Kagekit on the home screen, not the Unity logo
            // no "Made with Unity" screen at launch (optional on every licence since Unity 6)
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.companyName = "Wildbond Studio";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.runInBackground = true;
            PlayerSettings.bundleVersion = "0.1." + System.DateTime.Now.ToString("MMddHHmm");
            PlayerSettings.Android.bundleVersionCode = (int)(System.DateTime.UtcNow.Ticks / System.TimeSpan.TicksPerMinute % 2000000000);
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
            EditorUserBuildSettings.buildAppBundle = false;

            Directory.CreateDirectory("Builds");
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Main.unity" },
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };
            LastResult = "building";
            var report = BuildPipeline.BuildPlayer(opts);
            var s = report.summary;
            LastResult = $"{s.result} errors={s.totalErrors} size={s.totalSize / (1024 * 1024)}MB time={s.totalTime.TotalMinutes:0.0}min";
            File.WriteAllText("Builds/last_build.txt", LastResult);
            Debug.Log("[Wildbond] Android build: " + LastResult);
        }
    }
}
