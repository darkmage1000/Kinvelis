using System;
using System.IO;
using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Saves a picture where the player can find it and offers to share it. On Android that is the phone's own photo
    /// library (Pictures/Kinvelis, through MediaStore - no storage permission is needed on Android 10 and later) and
    /// the system share sheet; everywhere else, and whenever the Android calls fail, the file goes to the game's own
    /// folder (persistentDataPath/Photos). Nothing here is reachable from the editor's Android path, so the Android
    /// branch is only ever exercised on a device.
    /// </summary>
    public static class Gallery
    {
        /// <summary>What the last Save produced on Android: a content:// address the share sheet can read (null elsewhere).</summary>
        static string lastUri;
        public static string LastPath { get; private set; }

        /// <summary>Saves PNG bytes. Returns a short description of where it went (for a toast).</summary>
        public static string Save(byte[] png, string baseName)
        {
            string file = $"{baseName}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            lastUri = null;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var resolver = activity.Call<AndroidJavaObject>("getContentResolver"))
                using (var values = new AndroidJavaObject("android.content.ContentValues"))
                using (var media = new AndroidJavaClass("android.provider.MediaStore$Images$Media"))
                {
                    values.Call("put", "_display_name", file);
                    values.Call("put", "mime_type", "image/png");
                    values.Call("put", "relative_path", "Pictures/" + GameInfo.Title);
                    using (var collection = media.GetStatic<AndroidJavaObject>("EXTERNAL_CONTENT_URI"))
                    using (var uri = resolver.Call<AndroidJavaObject>("insert", collection, values))
                    {
                        if (uri != null)
                        {
                            using (var stream = resolver.Call<AndroidJavaObject>("openOutputStream", uri))
                            {
                                stream.Call("write", png);
                                stream.Call("flush");
                                stream.Call("close");
                            }
                            lastUri = uri.Call<string>("toString");
                            LastPath = "Pictures/" + GameInfo.Title + "/" + file;
                            return "Saved to your photos (" + GameInfo.Title + " album)";
                        }
                    }
                }
            }
            catch (Exception e) { Debug.LogWarning("[Gallery] photo library save failed, using the game folder: " + e.Message); }
#endif
            try
            {
                string dir = Path.Combine(Application.persistentDataPath, "Photos");
                Directory.CreateDirectory(dir);
                LastPath = Path.Combine(dir, file);
                File.WriteAllBytes(LastPath, png);
                return "Saved to " + LastPath;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Gallery] save failed: " + e.Message);
                return "Could not save the picture";
            }
        }

        public static bool CanShare => !string.IsNullOrEmpty(lastUri);

        /// <summary>Opens the system share sheet for the picture just saved (Android only).</summary>
        public static bool ShareLast(string text)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (string.IsNullOrEmpty(lastUri)) return false;
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var uriClass = new AndroidJavaClass("android.net.Uri"))
                using (var uri = uriClass.CallStatic<AndroidJavaObject>("parse", lastUri))
                using (var intentClass = new AndroidJavaClass("android.content.Intent"))
                using (var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.SEND"))
                {
                    intent.Call<AndroidJavaObject>("setType", "image/png");
                    intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.STREAM", uri);
                    if (!string.IsNullOrEmpty(text)) intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.TEXT", text);
                    intent.Call<AndroidJavaObject>("addFlags", 1);   // FLAG_GRANT_READ_URI_PERMISSION
                    using (var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, "Share"))
                        activity.Call("startActivity", chooser);
                }
                return true;
            }
            catch (Exception e) { Debug.LogWarning("[Gallery] share failed: " + e.Message); return false; }
#else
            return false;
#endif
        }

        /// <summary>The part of a screen capture inside a UI rectangle (overlay canvas: its corners are screen pixels).</summary>
        public static Texture2D Crop(Texture2D screen, RectTransform area)
        {
            var c = new Vector3[4];
            area.GetWorldCorners(c);
            int x = Mathf.Clamp(Mathf.RoundToInt(c[0].x), 0, screen.width - 1), y = Mathf.Clamp(Mathf.RoundToInt(c[0].y), 0, screen.height - 1);
            int w = Mathf.Clamp(Mathf.RoundToInt(c[2].x - c[0].x), 1, screen.width - x), h = Mathf.Clamp(Mathf.RoundToInt(c[2].y - c[0].y), 1, screen.height - y);
            var t = new Texture2D(w, h, TextureFormat.RGB24, false);
            t.SetPixels(screen.GetPixels(x, y, w, h));
            t.Apply();
            return t;
        }
    }
}
