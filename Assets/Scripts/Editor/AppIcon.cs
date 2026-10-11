using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

namespace Wildbond.EditorTools
{
    /// <summary>
    /// The app icon (user request 2026-10-10: "seeing unity logo is boring, pick a cool kin"): Kagekit, the ninja cat,
    /// against a full moon. Three images in Assets/Icons - icon_full (the plain square icon), and icon_background +
    /// icon_foreground for Android's adaptive icon (the launcher masks them to its own shape, so the kin sits inside
    /// the middle two thirds of the foreground). They were rendered in Play mode from the kin's own model
    /// (ModelBuilder.BuildStatic on a private layer, a black and a white shot for the alpha) - to change the kin,
    /// render new images over these three files and run Wildbond > Set App Icon. The build menu applies it every time.
    /// </summary>
    public static class AppIcon
    {
        const string Dir = "Assets/Icons/";

        [MenuItem("Wildbond/Set App Icon")]
        public static void Apply()
        {
            var full = Load("icon_full.png");
            var fg = Load("icon_foreground.png");
            var bg = Load("icon_background.png");
            if (full == null || fg == null || bg == null) { Debug.LogWarning("[AppIcon] icon images missing in " + Dir); return; }
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { full }, IconKind.Any);   // default icon for every platform
            var android = NamedBuildTarget.Android;
            Set(android, AndroidPlatformIconKind.Adaptive, bg, fg);
            Set(android, AndroidPlatformIconKind.Round, full);
            Set(android, AndroidPlatformIconKind.Legacy, full);
            ApplySplash();
            AssetDatabase.SaveAssets();
            Debug.Log("[AppIcon] app icon set");
        }

        /// <summary>
        /// The launch screen (user, 2026-10-10: "replace the made with unity with a loading page with a few kin on it"):
        /// Assets/Icons/splash.png is Android's static splash image, shown while the engine starts, scaled to fill.
        /// It is 20:9 with everything that matters in the middle 16:9, so no phone crops a kin or the title. The same
        /// picture without the "Loading..." line is Assets/Resources/UI/loading_art.png, which MainMenu.LoadWorld shows
        /// while a world loads. Both were composed by a script from a Play-mode render of six kin (see Previews/Loading).
        /// There is no public API for the splash texture, so it is set on the PlayerSettings asset directly.
        /// </summary>
        static void ApplySplash()
        {
            var splash = Load("splash.png", 4096);
            if (splash == null) { Debug.LogWarning("[AppIcon] splash image missing in " + Dir); return; }
            PlayerSettings.Android.splashScreenScale = AndroidSplashScreenScale.ScaleToFill;
            var so = new SerializedObject(Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings"));
            var prop = so.FindProperty("androidSplashScreen");
            if (prop == null) { Debug.LogWarning("[AppIcon] this Unity version has no androidSplashScreen setting"); return; }
            prop.objectReferenceValue = splash;
            so.ApplyModifiedPropertiesWithoutUndo();
            // the in-game copy must keep its own proportions (no power-of-two squeeze)
            var art = AssetImporter.GetAtPath("Assets/Resources/UI/loading_art.png") as TextureImporter;
            if (art != null && (art.npotScale != TextureImporterNPOTScale.None || art.mipmapEnabled || art.maxTextureSize != 4096))
            {
                art.npotScale = TextureImporterNPOTScale.None;
                art.mipmapEnabled = false;
                art.maxTextureSize = 4096;
                art.SaveAndReimport();
            }
        }

        static void Set(NamedBuildTarget target, PlatformIconKind kind, params Texture2D[] layers)
        {
            var icons = PlayerSettings.GetPlatformIcons(target, kind);
            foreach (var icon in icons) icon.SetTextures(layers);
            PlayerSettings.SetPlatformIcons(target, kind, icons);
        }

        /// <summary>Icons want sharp, uncompressed, non-power-of-two-safe textures with real transparency.</summary>
        static Texture2D Load(string file, int maxSize = 1024)
        {
            string path = Dir + file;
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null && (imp.mipmapEnabled || !imp.alphaIsTransparency || imp.textureCompression != TextureImporterCompression.Uncompressed || imp.npotScale != TextureImporterNPOTScale.None || imp.maxTextureSize != maxSize))
            {
                imp.textureType = TextureImporterType.Default;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.npotScale = TextureImporterNPOTScale.None;
                imp.maxTextureSize = maxSize;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
