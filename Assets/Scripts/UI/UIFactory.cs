using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>Code-built uGUI helpers (no prefabs / manual editor setup). Rounded sprites are generated at runtime.</summary>
    public static class UIFactory
    {
        public static Font Font;
        public static Sprite Rounded, Circle, Ring, Soft;

        public static readonly Color PanelBg = new Color(0.08f, 0.1f, 0.14f, 0.88f);
        public static readonly Color PanelBg2 = new Color(0.14f, 0.17f, 0.23f, 0.95f);
        public static readonly Color Accent = new Color(0.45f, 0.9f, 0.6f);
        public static readonly Color ButtonCol = new Color(0.22f, 0.3f, 0.36f, 0.95f);
        public static readonly Color TextDim = new Color(0.75f, 0.8f, 0.85f);

        public static void Init()
        {
            Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Rounded = MakeRounded(64, 18);
            Circle = MakeCircle(128, false);
            Ring = MakeCircle(128, true);
            Soft = MakeSoft(64);
        }

        static Sprite MakeRounded(int s, int r)
        {
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, r, s - r), cy = Mathf.Clamp(y + 0.5f, r, s - r);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                byte a = (byte)(Mathf.Clamp01(r - d + 0.5f) * 255);
                px[y * s + x] = new Color32(255, 255, 255, a);
            }
            t.SetPixels32(px); t.Apply(false, true);
            return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
        }

        static Sprite MakeCircle(int s, bool ring)
        {
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[s * s];
            float R = s * 0.5f;
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(R, R));
                float a = Mathf.Clamp01(R - d);
                if (ring) a *= Mathf.Clamp01(d - (R - 6f));
                px[y * s + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            t.SetPixels32(px); t.Apply(false, true);
            return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
        }

        static Sprite MakeSoft(int s)
        {
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(s * 0.5f, s * 0.5f)) / (s * 0.5f);
                px[y * s + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(1f - d) * 255));
            }
            t.SetPixels32(px); t.Apply(false, true);
            return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
        }

        // ---------------------------------------------------------------- LAYOUT
        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt, float l = 0, float b = 0, float r = 0, float t = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        public static RectTransform Anchors(this RectTransform rt, Vector2 min, Vector2 max, Vector2 offMin = default, Vector2 offMax = default)
        {
            rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = offMin; rt.offsetMax = offMax;
            return rt;
        }

        // ---------------------------------------------------------------- WIDGETS
        public static Image Panel(Transform parent, string name, Color c, Sprite sprite = null)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite ?? Rounded;
            img.type = Image.Type.Sliced;
            img.color = c;
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color c, TextAnchor align = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            var rt = Rect(parent, "Text");
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = c;
            t.alignment = align;
            t.fontStyle = style;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var sh = rt.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.6f);
            sh.effectDistance = new Vector2(1.5f, -1.5f);
            return t;
        }

        public static Button Button(Transform parent, string label, Action onClick, Color? col = null, int fontSize = 30)
        {
            var img = Panel(parent, "Btn_" + label, col ?? ButtonCol);
            var b = img.gameObject.AddComponent<Button>();
            var cb = b.colors;
            cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
            cb.pressedColor = new Color(0.75f, 0.75f, 0.75f);
            b.colors = cb;
            b.targetGraphic = img;
            if (onClick != null) b.onClick.AddListener(() => { SynthAudio.Play(Sfx.UiClick); onClick(); });
            var t = Label(img.transform, label, fontSize, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.rectTransform.Stretch(8, 4, 8, 4);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 14;
            t.resizeTextMaxSize = fontSize;
            return b;
        }

        public static Image Bar(Transform parent, Color fill, out Image fillImg, Color? bg = null)
        {
            var back = Panel(parent, "Bar", bg ?? new Color(0, 0, 0, 0.55f));
            var f = Panel(back.transform, "Fill", fill);
            f.rectTransform.Stretch(3, 3, 3, 3);
            fillImg = f;
            return back;
        }

        /// <summary>Sets a bar fill (0..1) by scaling its right anchor (works with sliced rounded sprites).</summary>
        public static void SetFill(this Image fill, float v)
        {
            v = Mathf.Clamp01(v);
            var rt = fill.rectTransform;
            if (Mathf.Abs(rt.anchorMax.x - v) < 0.0005f) return;
            rt.anchorMax = new Vector2(v, 1f);
            fill.enabled = v > 0.001f;
        }

        /// <summary>Scrollable vertical list. Returns the content transform to add rows to.</summary>
        public static RectTransform ScrollList(Transform parent, out ScrollRect scroll, float spacing = 8f)
        {
            var view = Panel(parent, "Scroll", new Color(0, 0, 0, 0.18f));
            scroll = view.gameObject.AddComponent<ScrollRect>();
            view.gameObject.AddComponent<RectMask2D>();
            var content = Rect(view.transform, "Content");
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = new Vector2(8, 0); content.offsetMax = new Vector2(-8, 0);
            var vl = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = spacing;
            vl.padding = new RectOffset(0, 0, 8, 8);
            vl.childControlHeight = true; vl.childControlWidth = true;
            vl.childForceExpandHeight = false; vl.childForceExpandWidth = true;
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            scroll.viewport = (RectTransform)view.transform;
            return content;
        }

        public static LayoutElement Size(Component c, float height, float width = -1)
        {
            var le = c.gameObject.GetComponent<LayoutElement>() ?? c.gameObject.AddComponent<LayoutElement>();
            le.minHeight = height; le.preferredHeight = height;
            if (width > 0) { le.minWidth = width; le.preferredWidth = width; }
            return le;
        }

        public static HorizontalLayoutGroup HRow(Transform parent, float height, float spacing = 8f)
        {
            var rt = Rect(parent, "Row");
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childControlHeight = true; h.childControlWidth = true;
            h.childForceExpandHeight = true; h.childForceExpandWidth = false;
            h.childAlignment = TextAnchor.MiddleLeft;
            Size(rt, height);
            return h;
        }

        /// <summary>Colored rounded icon with a 2-letter glyph (items, kin portraits).</summary>
        public static Image Icon(Transform parent, Color c, string glyph, float size, bool circle = false)
        {
            var img = Panel(parent, "Icon", c, circle ? Circle : Rounded);
            if (circle) img.type = Image.Type.Simple;
            img.rectTransform.sizeDelta = new Vector2(size, size);
            var t = Label(img.transform, glyph, Mathf.RoundToInt(size * 0.38f), Luma(c) > 0.6f ? new Color(0.1f, 0.1f, 0.12f) : Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.rectTransform.Stretch();
            return img;
        }

        /// <summary>Item icon (rendered 3D sprite), falling back to the colored glyph tile.</summary>
        public static Image ItemIcon(Transform parent, ItemDef d, float size)
        {
            var sprite = d != null ? ItemIcons.Get(d.id) : null;
            if (sprite == null) return Icon(parent, d != null ? d.iconColor : Color.gray, d != null ? d.iconGlyph : "?", size);
            var rt = Rect(parent, "ItemIcon");
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            rt.sizeDelta = new Vector2(size, size);
            return img;
        }

        /// <summary>Single-line text input (opens the on-screen keyboard on mobile).</summary>
        public static InputField Input(Transform parent, string value, string placeholder, int fontSize = 28)
        {
            var bg = Panel(parent, "Input", new Color(0, 0, 0, 0.45f));
            var f = bg.gameObject.AddComponent<InputField>();
            var txt = Label(bg.transform, value, fontSize, Color.white, TextAnchor.MiddleLeft);
            txt.rectTransform.Stretch(16, 4, 16, 4);
            txt.supportRichText = false;
            txt.raycastTarget = false;
            var ph = Label(bg.transform, placeholder, fontSize, new Color(1, 1, 1, 0.35f), TextAnchor.MiddleLeft, FontStyle.Italic);
            ph.rectTransform.Stretch(16, 4, 16, 4);
            f.textComponent = txt;
            f.placeholder = ph;
            f.text = value;
            f.targetGraphic = bg;
            return f;
        }

        /// <summary>Labeled slider row with live value text. Returns the slider.</summary>
        public static Slider SliderRow(Transform parent, string label, float min, float max, float value, bool whole, Func<float, string> fmt, Action<float> onChange, float labelWidth = 360f)
        {
            var row = HRow(parent, 70, 14);
            var l = Label(row.transform, label, 26, Color.white);
            Size(l, 60, labelWidth);
            var bg = Panel(row.transform, "Slider", new Color(0, 0, 0, 0.4f));
            Size(bg, 46, 520);
            var s = bg.gameObject.AddComponent<Slider>();
            var fillArea = Rect(bg.transform, "FillArea"); fillArea.Stretch(10, 12, 10, 12);
            var fill = Panel(fillArea, "Fill", Accent); fill.rectTransform.Stretch();
            var handleArea = Rect(bg.transform, "HandleArea"); handleArea.Stretch(20, 0, 20, 0);
            var handle = Panel(handleArea, "Handle", Color.white, Circle);
            handle.type = Image.Type.Simple;
            handle.rectTransform.sizeDelta = new Vector2(46, 0); // height follows the handle area
            s.fillRect = fill.rectTransform; s.handleRect = handle.rectTransform; s.targetGraphic = handle;
            s.minValue = min; s.maxValue = max; s.wholeNumbers = whole;
            s.value = value;
            var vt = Label(row.transform, fmt(value), 26, Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            Size(vt, 60, 140);
            s.onValueChanged.AddListener(v => { vt.text = fmt(v); onChange(v); });
            return s;
        }

        public static float Luma(Color c) => c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;

        public static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);
        public static string Col(string s, Color c) => $"<color=#{Hex(c)}>{s}</color>";
    }
}
