using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>
    /// Zoomable / pannable world map (Map tab). Drag to pan, pinch or wheel to zoom (1x-16x),
    /// +/- and "center on me" buttons. The whole-island texture is the base layer; a sharp detail
    /// tile for the visible area is re-rendered on a worker thread after you stop moving the view.
    /// Markers (you, camp, landmarks, waystones) stay pinned and waystones are tappable to fast travel.
    /// </summary>
    public class MapView : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler, IScrollHandler
    {
        const float MinZoom = 1f, MaxZoom = 16f;
        const int DetailSize = 512;

        // persistent view between openings
        static float savedZoom = 1f;
        static Vector2 savedCenter = new Vector2(0.5f, 0.5f);
        static bool hasSaved;

        RectTransform viewport, markerLayer;
        RawImage baseImg, detailImg;
        Texture2D detailTex;
        Rect detailUv;                 // UV region the detail texture currently covers
        float zoom = 1f;
        Vector2 center = new Vector2(0.5f, 0.5f);   // view center in map UV
        float dirtyTimer = -1f;
        bool needRender;
        Task renderTask;
        Color32[] renderPixels;
        Rect renderUv;
        Text zoomLabel;
        Action<string, string> onTravel;
        // selection banner
        GameObject selBar;
        Text selText;
        string selId, selName;
        Vector2 flyTarget; float flyZoom; bool flying;

        readonly Dictionary<int, Vector2> pointers = new Dictionary<int, Vector2>();
        float pinchStartDist, pinchStartZoom;

        class Marker { public RectTransform rt; public Vector3 world; public Text label; public bool rotateWithPlayer; }

        // ============================================================ HABITAT OVERLAY (Kin Dex)
        /// <summary>When set, the map dims everything outside this species' habitat and shows its sightings.</summary>
        public static SpeciesDef HabitatFocus;
        const int HabRes = 128;
        static byte[] habBiome;        // BiomeId per cell (255 = ocean)
        static bool[] habWater;        // cell is water or next to it
        static Task habTask;
        static readonly Dictionary<string, Texture2D> habCache = new Dictionary<string, Texture2D>();
        RawImage habitatImg;

        /// <summary>Starts the (threaded, one-off) biome/water grid used for habitat overlays.</summary>
        public static void PrepareHabitat()
        {
            if (habBiome != null || habTask != null) return;
            habTask = Task.Run(() =>
            {
                var b = new byte[HabRes * HabRes];
                var w = new bool[HabRes * HabRes];
                float cell = WorldGen.HalfWorld * 2f / HabRes;
                for (int y = 0; y < HabRes; y++)
                for (int x = 0; x < HabRes; x++)
                {
                    float wx = -WorldGen.HalfWorld + (x + 0.5f) * cell, wz = -WorldGen.HalfWorld + (y + 0.5f) * cell;
                    float h = WorldGen.Height(wx, wz);
                    int i = y * HabRes + x;
                    b[i] = h < -8f ? (byte)255 : (byte)WorldGen.Biome(wx, wz);
                    w[i] = h < 0f;
                }
                // "near water" = within ~1 cell of water
                var near = new bool[HabRes * HabRes];
                for (int y = 0; y < HabRes; y++)
                for (int x = 0; x < HabRes; x++)
                {
                    bool any = false;
                    for (int dy = -1; dy <= 1 && !any; dy++)
                    for (int dx = -1; dx <= 1 && !any; dx++)
                    {
                        int xx = x + dx, yy = y + dy;
                        if (xx >= 0 && yy >= 0 && xx < HabRes && yy < HabRes && w[yy * HabRes + xx]) any = true;
                    }
                    near[y * HabRes + x] = any;
                }
                habWater = near;
                habBiome = b;
            });
        }

        static Texture2D HabitatTexture(SpeciesDef sp)
        {
            if (habBiome == null) return null;
            if (habCache.TryGetValue(sp.id, out var tex) && tex != null) return tex;
            var px = new Color32[HabRes * HabRes];
            Color el = ElementInfo.Color(sp.element1);
            Color32 inside = new Color(el.r, el.g, el.b, 0.4f), outside = new Color(0.02f, 0.03f, 0.06f, 0.72f), clear = new Color(0, 0, 0, 0);
            for (int i = 0; i < px.Length; i++)
            {
                byte bi = habBiome[i];
                if (bi == 255) { px[i] = clear; continue; }
                bool ok = sp.LivesIn((BiomeId)bi) && (!sp.nearWater || habWater[i]) && !sp.starter;
                px[i] = ok ? inside : outside;
            }
            tex = new Texture2D(HabRes, HabRes, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "habitat_" + sp.id };
            tex.SetPixels32(px);
            tex.Apply(false, true);
            habCache[sp.id] = tex;
            return tex;
        }
        readonly List<Marker> markers = new List<Marker>();

        public static MapView Create(RectTransform parent, Vector2 size, Action<string, string> travel)
        {
            var frame = UIFactory.Panel(parent, "MapView", new Color(0.05f, 0.12f, 0.2f, 1f));
            frame.rectTransform.sizeDelta = size;
            frame.gameObject.AddComponent<RectMask2D>();
            var mv = frame.gameObject.AddComponent<MapView>();
            mv.onTravel = travel;
            mv.Build(frame.rectTransform);
            return mv;
        }

        void Build(RectTransform vp)
        {
            viewport = vp;
            baseImg = UIFactory.Rect(vp, "Base").Stretch().gameObject.AddComponent<RawImage>();
            baseImg.texture = MapTexture.Texture;
            baseImg.raycastTarget = false;
            detailImg = UIFactory.Rect(vp, "Detail").gameObject.AddComponent<RawImage>();
            detailImg.raycastTarget = false;
            detailImg.enabled = false;
            var drt = detailImg.rectTransform;
            drt.anchorMin = drt.anchorMax = new Vector2(0, 0);
            drt.pivot = new Vector2(0, 0);
            if (HabitatFocus != null)
            {
                PrepareHabitat();
                habitatImg = UIFactory.Rect(vp, "Habitat").Stretch().gameObject.AddComponent<RawImage>();
                habitatImg.raycastTarget = false;
                habitatImg.enabled = false;
            }
            markerLayer = UIFactory.Rect(vp, "Markers").Stretch();

            // controls (bottom-right of the map)
            var ctl = UIFactory.Rect(vp, "Controls").Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-12, 12), new Vector2(96, 330));
            var v = ctl.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 10; v.childControlHeight = true; v.childControlWidth = true; v.childForceExpandHeight = true;
            UIFactory.Button(ctl, "+", () => ZoomBy(2f, new Vector2(0.5f, 0.5f)), new Color(0.15f, 0.2f, 0.28f, 0.92f), 44);
            UIFactory.Button(ctl, "-", () => ZoomBy(0.5f, new Vector2(0.5f, 0.5f)), new Color(0.15f, 0.2f, 0.28f, 0.92f), 44);
            UIFactory.Button(ctl, "ME", CenterOnPlayer, new Color(0.2f, 0.42f, 0.36f, 0.92f), 22);
            UIFactory.Button(ctl, "ALL", () => { zoom = 1f; center = new Vector2(0.5f, 0.5f); MarkDirty(); }, new Color(0.15f, 0.2f, 0.28f, 0.92f), 20);
            zoomLabel = UIFactory.Label(vp, "", 20, new Color(1, 1, 1, 0.8f), TextAnchor.UpperLeft, FontStyle.Bold);
            zoomLabel.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -10), new Vector2(400, 30));

            // selection banner (bottom-left)
            var bar = UIFactory.Panel(vp, "Selection", new Color(0.05f, 0.08f, 0.12f, 0.92f));
            bar.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(12, 12), new Vector2(560, 84));
            selText = UIFactory.Label(bar.transform, "", 24, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            selText.rectTransform.Stretch(16, 4, 190, 4);
            var go = UIFactory.Button(bar.transform, "TRAVEL", () => { if (selId != null) onTravel?.Invoke(selId, selName); }, new Color(0.2f, 0.55f, 0.6f), 26);
            ((RectTransform)go.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(170, 64));
            selBar = bar.gameObject;
            selBar.SetActive(false);

            if (HabitatFocus != null)
            {
                var hb = UIFactory.Panel(vp, "HabitatBar", new Color(0.05f, 0.08f, 0.12f, 0.92f));
                hb.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -44), new Vector2(620, 64));
                var ht = UIFactory.Label(hb.transform, $"Habitat: {HabitatFocus.displayName}  <size=18>(colored = where it lives, dots = sightings)</size>", 22, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                ht.supportRichText = true;
                ht.rectTransform.Stretch(14, 2, 130, 2);
                var cb = UIFactory.Button(hb.transform, "CLEAR", () => { HabitatFocus = null; MenuUI.I.Open(MenuTab.Map); }, new Color(0.4f, 0.3f, 0.3f), 20);
                ((RectTransform)cb.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(110, 50));
            }

            BuildMarkers();
            if (hasSaved) { zoom = savedZoom; center = savedCenter; }
            else { zoom = 3f; center = MapTexture.WorldToUV(GameManager.I.Player.Position); }
            ClampView();
            MarkDirty();
        }

        // ============================================================ MARKERS
        void BuildMarkers()
        {
            foreach (var l in Landmarks.List) AddMarker(l.pos, l.color, l.name, 30, null, l.icon);
            // named sub-regions you have walked into
            foreach (var sr in SubRegions.All)
                if (sr.strength > 0f && SubRegions.Found(sr)) AddMarker(sr.Pos, Color.Lerp(sr.tint, Color.white, 0.6f), sr.name, 10, null);
            // finds you have come across, and relics a lookout showed you
            foreach (var poi in Pois.All)
                if (poi.kind != PoiKind.Lookout && Pois.Found(poi)) AddMarker(poi.pos, Pois.Done(poi) ? new Color(0.55f, 0.55f, 0.5f) : new Color(1f, 0.9f, 0.55f), poi.name, 28, null, Pois.Icon(poi.kind));
            var st = GameManager.I.Player.Stats;
            if (Relics.I != null && Relics.I.Ready && st.relicHints.Count > 0)
                foreach (var rl in Relics.I.All)
                    if (st.relicHints.Contains(rl.id) && !st.relicsFound.Contains(rl.id)) AddMarker(rl.pos, Color.white, "Relic glint", 24, null, MapIcon.Relic);
            var ws = Waystones.I;
            foreach (var w in ws.All)
            {
                if (!ws.IsFound(w)) continue;
                bool act = ws.IsActive(w);
                string wid = w.id, wname = w.name;
                AddMarker(w.pos, act ? new Color(0.4f, 0.95f, 1f) : new Color(0.6f, 0.65f, 0.7f),
                    act ? w.name.Replace(" Waystone", "") : "?", act ? 32 : 26, act ? (Action)(() => Select(wid, wname, w.pos)) : null, MapIcon.Waystone);
            }
            if (Trader.I != null && Trader.I.Present) AddMarker(Trader.I.Position, new Color(1f, 0.8f, 0.3f), Trader.Name + " (trader)", 32, null, MapIcon.Trader);
            if (Caves.I != null)
                foreach (var cv in Caves.I.Entrances)
                    if (Caves.I.IsFound(cv)) AddMarker(cv.pos, new Color(0.8f, 0.6f, 1f), $"{cv.name} (Lv {cv.minLv}-{cv.maxLv})", 30, null, cv.Icon);
            if (BaseManager.I.HasBase)
                AddMarker(BaseManager.I.CorePosition, new Color(0.4f, 1f, 0.6f), "Camp", 36, () => Select(Waystones.CampId, "your camp", BaseManager.I.CorePosition), MapIcon.Camp);
            if (HabitatFocus != null)
            {
                var e = GameManager.I.Player.Stats.DexFor(HabitatFocus.id, false);
                Color el = ElementInfo.Color(HabitatFocus.element1);
                if (e != null) foreach (var s in e.sightings) AddMarker(new Vector3(s.x, 0, s.y), el, "", 16, null);
                foreach (var a in Landmarks.AlphaSites)
                    if (a.species == HabitatFocus.id) AddMarker(a.pos, new Color(1f, 0.55f, 0.2f), "Alpha " + HabitatFocus.displayName, 32, null, MapIcon.Lair);
            }
            var questAt = Quests.Marker;
            if (questAt.HasValue) AddMarker(questAt.Value, Quests.Gold, "Quest: " + Quests.Tracked.title, 40, null, MapIcon.Quest);
            var me = AddMarker(GameManager.I.Player.Position, Color.white, "", 34, null);
            me.rotateWithPlayer = true;
            var arrow = UIFactory.Label(me.rt, "^", 34, new Color(0.1f, 0.1f, 0.12f), TextAnchor.MiddleCenter, FontStyle.Bold);
            arrow.rectTransform.Stretch();
        }

        readonly List<int> labelOrder = new List<int>();
        readonly List<Rect> labelRects = new List<Rect>();

        /// <summary>
        /// Names never sit on top of each other (menu pass 2): bigger markers (camp, villages, Wardens) keep their names
        /// first, then the rest in the order they were added; a name that would overlap one already shown is hidden
        /// until you zoom in far enough for it to fit. The zoom readout's corner is kept clear too.
        /// </summary>
        void Declutter(Rect r)
        {
            labelOrder.Clear();
            for (int i = 0; i < markers.Count; i++)
            {
                var m = markers[i];
                if (m.label != null && m.label.enabled && m.rt.gameObject.activeSelf) labelOrder.Add(i);
            }
            labelOrder.Sort((a, b) =>
            {
                int c = markers[b].rt.sizeDelta.x.CompareTo(markers[a].rt.sizeDelta.x);
                return c != 0 ? c : a.CompareTo(b);
            });
            labelRects.Clear();
            labelRects.Add(new Rect(0f, r.height - 44f, 300f, 44f));
            foreach (int i in labelOrder)
            {
                var m = markers[i];
                Vector2 p = m.rt.anchoredPosition;
                float half = m.rt.sizeDelta.x * 0.5f;
                var box = new Rect(p.x + half + 4f, p.y - 12f, Mathf.Min(320f, m.label.preferredWidth), 24f);
                bool hit = false;
                foreach (var t in labelRects) if (t.Overlaps(box)) { hit = true; break; }
                if (hit) { m.label.enabled = false; continue; }
                labelRects.Add(box);
            }
        }

        Marker AddMarker(Vector3 world, Color c, string label, float size, Action onTap, MapIcon icon = MapIcon.Dot)
        {
            var img = UIFactory.Panel(markerLayer, "M", c, icon == MapIcon.Dot ? UIFactory.Circle : MapIcons.Get(icon));
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            img.raycastTarget = onTap != null;
            if (onTap != null)
            {
                rt.sizeDelta = new Vector2(size + 14, size + 14);   // generous touch target
                img.gameObject.AddComponent<Button>().onClick.AddListener(() => onTap());
            }
            Text t = null;
            if (!string.IsNullOrEmpty(label))
            {
                t = UIFactory.Label(rt, label, 21, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                t.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(0, 0.5f), new Vector2(6, 0), new Vector2(320, 30));
            }
            var m = new Marker { rt = rt, world = world, label = t };
            markers.Add(m);
            return m;
        }

        // ============================================================ INPUT
        public void OnPointerDown(PointerEventData e)
        {
            flying = false;
            pointers[e.pointerId] = e.position;
            if (pointers.Count == 2) { pinchStartDist = PinchDist(); pinchStartZoom = zoom; }
        }

        public void OnPointerUp(PointerEventData e)
        {
            pointers.Remove(e.pointerId);
            MarkDirty();
        }

        public void OnDrag(PointerEventData e)
        {
            pointers[e.pointerId] = e.position;
            if (pointers.Count >= 2)
            {
                float d = PinchDist();
                if (pinchStartDist > 10f)
                {
                    float target = Mathf.Clamp(pinchStartZoom * d / pinchStartDist, MinZoom, MaxZoom);
                    ZoomTo(target, ScreenToViewport01(PinchMid()));
                }
                return;
            }
            // pan: convert screen delta to UV delta
            float px = viewport.rect.width * viewport.lossyScale.x;
            Vector2 duv = e.delta / Mathf.Max(1f, px) / zoom;
            center -= duv;
            ClampView();
            MarkDirty();
        }

        public void OnScroll(PointerEventData e)
        {
            ZoomBy(e.scrollDelta.y > 0 ? 1.25f : 0.8f, ScreenToViewport01(e.position));
        }

        float PinchDist()
        {
            var it = pointers.Values.GetEnumerator();
            it.MoveNext(); var a = it.Current; it.MoveNext(); var b = it.Current;
            return Vector2.Distance(a, b);
        }

        Vector2 PinchMid()
        {
            Vector2 s = Vector2.zero; int n = 0;
            foreach (var p in pointers.Values) { s += p; if (++n == 2) break; }
            return s / Mathf.Max(1, n);
        }

        Vector2 ScreenToViewport01(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, screen, null, out var local);
            var r = viewport.rect;
            return new Vector2((local.x - r.xMin) / r.width, (local.y - r.yMin) / r.height);
        }

        void ZoomBy(float factor, Vector2 pivot01) => ZoomTo(Mathf.Clamp(zoom * factor, MinZoom, MaxZoom), pivot01);

        /// <summary>Zoom keeping the map point under pivot (viewport 0..1) fixed.</summary>
        void ZoomTo(float z, Vector2 pivot01)
        {
            Vector2 before = ViewToUv(pivot01);
            zoom = z;
            Vector2 after = ViewToUv(pivot01);
            center += before - after;
            ClampView();
            MarkDirty();
        }

        void CenterOnPlayer()
        {
            center = MapTexture.WorldToUV(GameManager.I.Player.Position);
            if (zoom < 4f) zoom = 4f;
            ClampView();
            MarkDirty();
        }

        /// <summary>Fly the map to a destination and offer a TRAVEL button for it.</summary>
        public void Select(string id, string name, Vector3 world)
        {
            selId = id; selName = name;
            selText.text = $"{name}\n<size=18>Tap TRAVEL to go there</size>";
            selText.supportRichText = true;
            selBar.SetActive(true);
            flyTarget = MapTexture.WorldToUV(world);
            flyZoom = Mathf.Max(zoom, 6f);
            flying = true;
            SynthAudio.Play(Sfx.UiClick, 1.1f);
        }

        Vector2 ViewToUv(Vector2 v01) => center + (v01 - new Vector2(0.5f, 0.5f)) / zoom;

        void ClampView()
        {
            float half = 0.5f / zoom;
            center.x = Mathf.Clamp(center.x, half, 1f - half);
            center.y = Mathf.Clamp(center.y, half, 1f - half);
            savedZoom = zoom; savedCenter = center; hasSaved = true;
        }

        void MarkDirty() { dirtyTimer = 0.25f; needRender = true; }

        // ============================================================ UPDATE
        void Update()
        {
            if (flying)
            {
                float k = 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime);
                zoom = Mathf.Lerp(zoom, flyZoom, k);
                center = Vector2.Lerp(center, flyTarget, k);
                ClampView();
                MarkDirty();
                if (Mathf.Abs(zoom - flyZoom) < 0.02f && (center - flyTarget).sqrMagnitude < 1e-7f) flying = false;
            }
            float span = 1f / zoom;
            var view = new Rect(center.x - span * 0.5f, center.y - span * 0.5f, span, span);
            baseImg.uvRect = view;
            if (baseImg.texture != MapTexture.Texture) baseImg.texture = MapTexture.Texture;
            if (habitatImg != null && HabitatFocus != null)
            {
                if (!habitatImg.enabled) { var ht = HabitatTexture(HabitatFocus); if (ht != null) { habitatImg.texture = ht; habitatImg.enabled = true; } }
                habitatImg.uvRect = view;
            }

            // detail tile placement (in viewport pixels)
            var r = viewport.rect;
            if (detailImg.enabled)
            {
                var drt = detailImg.rectTransform;
                drt.anchoredPosition = new Vector2((detailUv.x - view.x) / view.width * r.width, (detailUv.y - view.y) / view.height * r.height);
                drt.sizeDelta = new Vector2(detailUv.width / view.width * r.width, detailUv.height / view.height * r.height);
            }

            // markers
            var pl = GameManager.I.Player;
            foreach (var m in markers)
            {
                Vector3 w = m.rotateWithPlayer ? pl.Position : m.world;
                Vector2 uv = MapTexture.WorldToUV(w);
                Vector2 v = new Vector2((uv.x - view.x) / view.width, (uv.y - view.y) / view.height);
                bool inside = v.x > -0.02f && v.x < 1.02f && v.y > -0.02f && v.y < 1.02f;
                if (m.rt.gameObject.activeSelf != inside) m.rt.gameObject.SetActive(inside);
                if (!inside) continue;
                m.rt.anchoredPosition = new Vector2(v.x * r.width, v.y * r.height);
                if (m.rotateWithPlayer) m.rt.localRotation = Quaternion.Euler(0, 0, -pl.transform.eulerAngles.y);
                if (m.label != null) m.label.enabled = zoom >= 2f || m.rt.sizeDelta.x >= 26f;
            }
            Declutter(r);

            float meters = WorldGen.HalfWorld * 2f / zoom;
            zoomLabel.text = $"{zoom:0.#}x  ·  view {meters:0} m";

            // schedule a sharp detail render for the visible region once the view settles
            if (dirtyTimer > 0f) dirtyTimer -= Time.unscaledDeltaTime;
            if (needRender && dirtyTimer <= 0f && renderTask == null && pointers.Count == 0)
            {
                needRender = false;
                if (zoom > 1.5f) StartRender(view);
            }
            if (renderPixels != null)
            {
                if (detailTex == null)
                    detailTex = new Texture2D(DetailSize, DetailSize, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "mapdetail" };
                detailTex.SetPixels32(renderPixels);
                detailTex.Apply(false, false);
                renderPixels = null;
                renderTask = null;
                detailImg.texture = detailTex;
                detailUv = renderUv;
                detailImg.enabled = true;
            }
            if (zoom <= 1.5f && detailImg.enabled) detailImg.enabled = false;
        }

        void StartRender(Rect view)
        {
            // render a little beyond the view so small pans stay sharp
            float pad = view.width * 0.15f;
            var uv = new Rect(view.x - pad, view.y - pad, view.width + pad * 2f, view.height + pad * 2f);
            renderUv = uv;
            float world = WorldGen.HalfWorld * 2f;
            renderTask = Task.Run(() =>
            {
                var px = new Color32[DetailSize * DetailSize];
                float cell = uv.width * world / DetailSize;
                for (int y = 0; y < DetailSize; y++)
                for (int x = 0; x < DetailSize; x++)
                {
                    float wx = -WorldGen.HalfWorld + (uv.x + (x + 0.5f) / DetailSize * uv.width) * world;
                    float wz = -WorldGen.HalfWorld + (uv.y + (y + 0.5f) / DetailSize * uv.height) * world;
                    px[y * DetailSize + x] = MapTexture.Shade(wx, wz, cell);
                }
                renderPixels = px;
            });
        }

        void OnDestroy()
        {
            if (detailTex != null) Destroy(detailTex);
        }
    }
}
