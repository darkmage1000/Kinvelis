using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>
    /// Photo mode (Settings > PHOTO MODE): the HUD fades out but keeps working, so you still walk, look around and
    /// pinch to zoom exactly as usual - the screen is simply clear. Two small buttons stay: SNAP saves what you see to
    /// the phone's photos (Gallery) with nothing drawn over it, DONE brings the HUD back. The world keeps running, so
    /// a photo can catch a kin mid-leap.
    /// </summary>
    public static class PhotoMode
    {
        public static bool Active { get; private set; }
        static GameObject overlay;
        static Text note;

        public static void Enter()
        {
            if (Active || HUD.I == null) return;
            Active = true;
            HUD.I.SetFaded(true);

            overlay = new GameObject("PhotoOverlay");
            var c = overlay.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 500;
            var sc = overlay.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            sc.matchWidthOrHeight = 0.6f;
            overlay.AddComponent<GraphicRaycaster>();
            var snap = UIFactory.Button(overlay.transform, "SNAP", Snap, new Color(0.2f, 0.55f, 0.42f, 0.9f), 30);
            ((RectTransform)snap.transform).Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-110, -18), new Vector2(200, 78));
            var done = UIFactory.Button(overlay.transform, "DONE", Exit, new Color(0.3f, 0.3f, 0.36f, 0.9f), 30);
            ((RectTransform)done.transform).Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(110, -18), new Vector2(200, 78));
            note = UIFactory.Label(overlay.transform, "Move and look as usual, pinch to zoom. SNAP saves the picture.", 26, Color.white, TextAnchor.UpperCenter, FontStyle.Bold);
            note.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -104), new Vector2(1200, 44));
            note.raycastTarget = false;
            var shade = note.gameObject.AddComponent<Outline>();
            shade.effectColor = new Color(0, 0, 0, 0.8f); shade.effectDistance = new Vector2(2, -2);
        }

        public static void Exit()
        {
            if (!Active) return;
            Active = false;
            if (HUD.I != null) HUD.I.SetFaded(false);
            if (overlay != null) Object.Destroy(overlay);
            overlay = null; note = null;
        }

        static void Snap()
        {
            if (GameManager.I != null) GameManager.I.StartCoroutine(Capture());
        }

        static IEnumerator Capture()
        {
            if (overlay != null) overlay.SetActive(false);
            yield return null;                       // a frame without the buttons
            yield return new WaitForEndOfFrame();
            var shot = ScreenCapture.CaptureScreenshotAsTexture();
            byte[] png = shot.EncodeToPNG();
            Object.Destroy(shot);
            string where = Gallery.Save(png, "photo");
            if (overlay != null) overlay.SetActive(true);
            if (note != null) note.text = where + ".";
            SynthAudio.Play(Sfx.Shake, 1.5f, 0.5f);
        }
    }
}
