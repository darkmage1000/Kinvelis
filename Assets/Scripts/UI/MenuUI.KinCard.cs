using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>
    /// The kin card (Kin tab > KIN CARD): a picture of one particular kin - its own model with its own size, coat and
    /// markings, turning slowly - with its name, kind, level, quality and looks, laid out like a trading card. SAVE
    /// puts exactly what is on the card into the phone's photos (Gallery) and SHARE hands it to the share sheet.
    /// </summary>
    public partial class MenuUI
    {
        KinStage kinStage;
        RectTransform kinCardRect;
        GameObject kinCardButtons;

        void ShowKinCard(CreatureData d)
        {
            CloseModal();
            var sp = d.Species;
            if (kinStage == null) kinStage = new KinStage();
            kinStage.Show(d);

            var bg = UIFactory.Panel(panel.transform, "KinCard", new Color(0, 0, 0, 0.82f));
            bg.rectTransform.Stretch();
            modal = bg.gameObject;

            Color el = ElementInfo.Color(sp.element1);
            var card = UIFactory.Panel(bg.transform, "Card", Color.Lerp(new Color(0.07f, 0.08f, 0.2f), el, 0.22f));
            card.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-150, 0), new Vector2(720, 1000));
            card.rectTransform.localScale = Vector3.one * 0.86f;   // the whole card fits a phone held sideways
            var edge = card.gameObject.AddComponent<Outline>();
            edge.effectColor = new Color(1f, 0.86f, 0.45f); edge.effectDistance = new Vector2(5, -5);
            kinCardRect = card.rectTransform;

            var top = UIFactory.Label(card.transform, GameInfo.Title.ToUpperInvariant(), 26, new Color(1f, 0.9f, 0.6f), TextAnchor.MiddleLeft, FontStyle.Bold);
            top.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -14), new Vector2(400, 44));
            var num = UIFactory.Label(card.transform, $"#{sp.dexNumber:000}", 26, new Color(1f, 1f, 1f, 0.7f), TextAnchor.MiddleRight, FontStyle.Bold);
            num.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-28, -14), new Vector2(200, 44));

            var frame = UIFactory.Panel(card.transform, "Frame", new Color(0, 0, 0, 0.3f));
            frame.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -66), new Vector2(664, 500));
            var raw = UIFactory.Rect(frame.transform, "Picture").gameObject.AddComponent<RawImage>();
            raw.rectTransform.Stretch(4, 4, 4, 4);
            raw.texture = kinStage.Texture;
            raw.raycastTarget = false;

            string tags = d.VariantTags();
            var name = UIFactory.Label(card.transform, d.DisplayName, 56, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            name.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -578), new Vector2(660, 70));
            name.resizeTextForBestFit = true; name.resizeTextMinSize = 28; name.resizeTextMaxSize = 56;

            string els = UIFactory.Col(sp.element1.ToString(), el) + (sp.element2 != sp.element1 && sp.element2 != Element.Normal ? "  " + UIFactory.Col(sp.element2.ToString(), ElementInfo.Color(sp.element2)) : "");
            var lines = new List<string>
            {
                $"<b>{sp.displayName}</b>   {UIFactory.Col(sp.rarity.ToString(), RarityInfo.Color(sp.rarity))}   Lv {d.level}",
                els + (tags.Length > 0 ? "   " + UIFactory.Col(tags, new Color(1f, 0.85f, 0.4f)) : "") + (d.stage > 0 ? "   " + UIFactory.Col(CreatureData.StageName(d.stage), new Color(0.8f, 0.9f, 1f)) : ""),
                "Quality   " + Aptitude.Summary(d),
                sp.custom ? "Dreamed up by you" : "Looks   " + d.Look.Describe(sp),
            };
            var tr = new List<string>();
            foreach (var t in d.traits) { var td = ContentDB.Trait(t); if (td != null) tr.Add(td.displayName); }
            if (tr.Count > 0) lines.Add("Traits   " + string.Join(", ", tr));
            lines.Add($"Bond {d.BondLevel}/{CreatureData.MaxBond}" + (d.capturedAt > 0 ? "   ·   with you since " + System.DateTimeOffset.FromUnixTimeSeconds(d.capturedAt).LocalDateTime.ToString("d MMM yyyy") : ""));
            var body = UIFactory.Label(card.transform, string.Join("\n", lines), 27, new Color(0.93f, 0.95f, 1f), TextAnchor.UpperLeft);
            body.supportRichText = true;
            body.lineSpacing = 1.25f;
            body.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -652), new Vector2(660, 300));
            var foot = UIFactory.Label(card.transform, "Rimewild Isle", 22, new Color(1f, 1f, 1f, 0.55f), TextAnchor.MiddleRight, FontStyle.Italic);
            foot.rectTransform.Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-28, 12), new Vector2(400, 36));

            // buttons to the right of the card (never part of the saved picture)
            var side = UIFactory.Rect(bg.transform, "Buttons");
            side.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(410, 0), new Vector2(320, 420));
            var vl = side.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 16; vl.childControlHeight = true; vl.childControlWidth = true; vl.childForceExpandHeight = false;
            kinCardButtons = side.gameObject;
            UIFactory.Size(UIFactory.Button(side, "SAVE PICTURE", () => StartCoroutine(SaveKinCard(d, false)), new Color(0.2f, 0.55f, 0.42f), 30), 100);
            UIFactory.Size(UIFactory.Button(side, "SHARE", () => StartCoroutine(SaveKinCard(d, true)), new Color(0.3f, 0.42f, 0.7f), 30), 100);
            UIFactory.Size(UIFactory.Button(side, "CLOSE", CloseModal, new Color(0.45f, 0.28f, 0.28f), 30), 100);
        }

        IEnumerator SaveKinCard(CreatureData d, bool share)
        {
            if (kinCardRect == null) yield break;
            yield return new WaitForEndOfFrame();
            var shot = ScreenCapture.CaptureScreenshotAsTexture();
            var card = Gallery.Crop(shot, kinCardRect);
            Destroy(shot);
            string where = Gallery.Save(card.EncodeToPNG(), "kin_" + d.Species.id);
            Destroy(card);
            SynthAudio.Play(Sfx.CaptureSuccess, 1.2f, 0.6f);
            if (share && !Gallery.ShareLast($"{d.DisplayName}, my {d.Species.displayName} in {GameInfo.Title}")) GameEvents.Say(where + ". (Sharing works on a phone.)");
            else GameEvents.Good(where + ".");
        }

        void TickKinCard()
        {
            if (kinStage == null) return;
            if (IsOpen && modal != null && modal.name == "KinCard") kinStage.Render(Time.unscaledDeltaTime);
        }
    }

    /// <summary>One kin on a turntable, rendered to a texture (the kin card's picture). Its own layer-30 studio far below the world.</summary>
    public class KinStage
    {
        const int Layer = 30;
        static readonly Vector3 Origin = new Vector3(0f, -3200f, 0f);
        public RenderTexture Texture { get; private set; }
        readonly GameObject root;
        readonly Camera cam;
        CreatureRig rig;
        float yaw = 200f;

        public KinStage()
        {
            root = new GameObject("KinStage");
            root.transform.position = Origin;
            var camGo = new GameObject("KinStageCam");
            cam = camGo.AddComponent<Camera>();
            cam.enabled = false;
            cam.cullingMask = 1 << Layer;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.12f, 0.24f, 1f);
            cam.fieldOfView = 24f;
            cam.nearClipPlane = 0.1f; cam.farClipPlane = 80f;
            cam.allowHDR = false;
            var extra = cam.GetUniversalAdditionalCameraData();
            extra.renderPostProcessing = false;
            extra.renderShadows = false;
            Texture = new RenderTexture(664, 500, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4, name = "KinStageRT" };
            Texture.Create();
            cam.targetTexture = Texture;
        }

        public void Show(CreatureData d)
        {
            if (rig != null && rig.Model != null) Object.Destroy(rig.Model.gameObject);
            root.transform.rotation = Quaternion.identity;
            rig = ModelBuilder.Build(d, root.transform, RenderAssets.Creature);
            rig.Animate(0.016f, 0f, false, false);
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Layer;
            // frame the whole kin, whatever its size
            var b = new Bounds(Origin, Vector3.zero); bool any = false;
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            float radius = Mathf.Max(0.35f, b.extents.magnitude * 0.82f);
            float dist = radius / Mathf.Tan(12f * Mathf.Deg2Rad) * 0.86f;
            Vector3 focus = new Vector3(Origin.x, b.center.y, Origin.z);
            cam.transform.position = focus + new Vector3(0f, radius * 0.35f, dist);
            cam.transform.LookAt(focus);
        }

        public void Render(float dt)
        {
            if (rig == null) return;
            yaw += dt * 20f;
            root.transform.rotation = Quaternion.Euler(0f, Mathf.Sin(yaw * Mathf.Deg2Rad) * 38f, 0f);   // sways to show both sides, never its back (the camera is on its +z side)
            rig.Animate(dt, 0f, false, false);

            var sun = RenderSettings.sun;
            bool fog = RenderSettings.fog;
            var probe = RenderSettings.ambientProbe;
            Quaternion sr = Quaternion.identity; float si = 0f; Color sc = Color.white; LightShadows ss = LightShadows.None;
            if (sun != null) { sr = sun.transform.rotation; si = sun.intensity; sc = sun.color; ss = sun.shadows; sun.transform.rotation = Quaternion.Euler(40f, 160f, 0f); sun.intensity = 1.1f; sun.color = Color.white; sun.shadows = LightShadows.None; }
            RenderSettings.fog = false;
            var sh = new SphericalHarmonicsL2();
            sh.AddAmbientLight(new Color(0.5f, 0.52f, 0.58f));
            sh.AddDirectionalLight(Vector3.up, new Color(0.25f, 0.25f, 0.3f), 1f);
            RenderSettings.ambientProbe = sh;

            var req = new UniversalRenderPipeline.SingleCameraRequest { destination = Texture };
            if (RenderPipeline.SupportsRenderRequest(cam, req)) RenderPipeline.SubmitRenderRequest(cam, req);
            else cam.Render();

            RenderSettings.fog = fog;
            RenderSettings.ambientProbe = probe;
            if (sun != null) { sun.transform.rotation = sr; sun.intensity = si; sun.color = sc; sun.shadows = ss; }
        }
    }
}
