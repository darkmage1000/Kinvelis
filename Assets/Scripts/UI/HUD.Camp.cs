using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>
    /// Floating labels over the camp's working structures while you are near them: what step each one is on
    /// ("Planting", "Needs water", "Growing 0:14", "Smelting 0:09", "Crafting Stone Pick"), a bar for how far along it
    /// is, and how much finished output is waiting. Amber = hands-on, green = running by itself, red = stuck.
    /// </summary>
    public partial class HUD
    {
        class CampLabel
        {
            public RectTransform root;
            public Image back, fill;
            public Text text;
            public float refresh;
            public Building shown;
        }

        readonly List<CampLabel> campLabels = new List<CampLabel>();
        RectTransform campLabelRoot;
        const float CampLabelRange = 17f;
        const int CampLabelMax = 10;
        static readonly Color LabelWork = new Color(1f, 0.78f, 0.32f), LabelRun = new Color(0.5f, 0.9f, 0.58f), LabelStuck = new Color(1f, 0.45f, 0.4f);

        CampLabel CampLabelAt(int i)
        {
            if (campLabelRoot == null)
            {
                campLabelRoot = UIFactory.Rect(safe, "CampLabels");
                campLabelRoot.Stretch();
                campLabelRoot.SetAsFirstSibling();   // under every button
            }
            while (campLabels.Count <= i)
            {
                var l = new CampLabel();
                l.back = UIFactory.Panel(campLabelRoot, "CampLabel", new Color(0.05f, 0.06f, 0.1f, 0.72f));
                l.back.raycastTarget = false;
                l.root = l.back.rectTransform;
                l.root.anchorMin = l.root.anchorMax = new Vector2(0.5f, 0.5f);
                l.root.sizeDelta = new Vector2(270f, 54f);
                l.text = UIFactory.Label(l.root, "", 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                l.text.raycastTarget = false;
                l.text.rectTransform.Anchors(Vector2.zero, Vector2.one, new Vector2(8, 12), new Vector2(-8, -2));
                l.text.resizeTextForBestFit = true; l.text.resizeTextMinSize = 14; l.text.resizeTextMaxSize = 22;
                var track = UIFactory.Panel(l.root, "Track", new Color(1f, 1f, 1f, 0.14f));
                track.raycastTarget = false;
                track.rectTransform.Anchors(new Vector2(0, 0), new Vector2(1, 0), new Vector2(8, 5), new Vector2(-8, 11));
                l.fill = UIFactory.Panel(track.transform, "Fill", LabelWork);
                l.fill.raycastTarget = false;
                l.fill.rectTransform.anchorMin = Vector2.zero; l.fill.rectTransform.anchorMax = new Vector2(0f, 1f);
                l.fill.rectTransform.offsetMin = l.fill.rectTransform.offsetMax = Vector2.zero;
                campLabels.Add(l);
            }
            return campLabels[i];
        }

        void UpdateCampLabels(Camera cam)
        {
            var bm = BaseManager.I; var gm = GameManager.I;
            int used = 0;
            if (bm != null && bm.HasBase && cam != null && gm != null && gm.Player != null && !Caves.InCave)
            {
                Vector3 pp = gm.Player.Position;
                foreach (var b in bm.Buildings)
                {
                    if (used >= CampLabelMax) break;
                    if (b == null) continue;
                    bool station = b.IsStation && bm.HasOrderFor(b.Def.station);
                    bool pylon = b.Def.id == "volt_pylon";
                    bool larder = BaseManager.IsLarder(b);
                    if (!b.IsProducer && !station && !pylon && !larder) continue;
                    Vector3 off = b.transform.position - pp; off.y = 0f;
                    float dist = off.magnitude;
                    if (dist > CampLabelRange) continue;
                    Vector3 sp = cam.WorldToScreenPoint(b.transform.position + Vector3.up * (b.TopY + 0.35f));
                    if (sp.z < 0.5f) continue;
                    var l = CampLabelAt(used++);
                    if (!l.root.gameObject.activeSelf) l.root.gameObject.SetActive(true);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(safe, sp, canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null, out Vector2 local);
                    l.root.anchoredPosition = local;
                    float fade = Mathf.Clamp01((CampLabelRange - dist) / 4f);
                    l.root.localScale = Vector3.one * Mathf.Lerp(0.72f, 1f, Mathf.Clamp01(1f - dist / CampLabelRange));

                    l.refresh -= Time.unscaledDeltaTime;
                    if (l.refresh <= 0f || l.shown != b)
                    {
                        l.refresh = 0.2f;
                        l.shown = b;
                        string text; float fill; Color col;
                        if (pylon)
                        {
                            // the camp's battery gauge: green while a kin charges it, amber running down, red flat
                            fill = bm.Charge01;
                            text = b.Powered ? (fill >= 0.995f ? "Fully charged" : $"Charging  ·  {Mathf.RoundToInt(fill * 100f)}%")
                                : bm.Charge > 0f ? $"Stored charge  ·  {WorkStages.Clock(bm.Charge)} left" : bm.PowerAsleep ? "No charge  ·  kin asleep" : "No charge";
                            col = b.Powered ? LabelRun : bm.Charge > 0f ? LabelWork : LabelStuck;
                        }
                        else if (larder)
                        {
                            // the same gauge for stored cold
                            fill = bm.Cold01;
                            text = b.Powered ? (fill >= 0.995f ? "Full of cold" : $"Chilling  ·  {Mathf.RoundToInt(fill * 100f)}%")
                                : bm.Cold > 0f ? $"Stored cold  ·  {WorkStages.Clock(bm.Cold)} left" : bm.ColdAsleep ? "No cold  ·  kin asleep" : "No cold";
                            col = b.Powered ? LabelRun : bm.Cold > 0f ? LabelWork : LabelStuck;
                        }
                        else if (b.IsProducer) ProducerLabel(bm, b, out text, out fill, out col);
                        else StationLabel(bm, b, out text, out fill, out col);
                        l.text.text = text;
                        l.fill.color = col;
                        l.fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(fill), 1f);
                    }
                    var bc = l.back.color; bc.a = 0.72f * fade; l.back.color = bc;
                    var tc = l.text.color; tc.a = fade; l.text.color = tc;
                }
            }
            for (int i = used; i < campLabels.Count; i++)
                if (campLabels[i].root.gameObject.activeSelf) campLabels[i].root.gameObject.SetActive(false);
        }

        static void ProducerLabel(BaseManager bm, Building b, out string text, out float fill, out Color col)
        {
            string ready = b.BufferCount > 0 ? $"  ·  {b.BufferCount} ready" : "";
            string block = b.AtStart ? b.StartBlock() : null;
            if (block != null)
            {
                text = (b.BufferCount + b.BatchOutput > b.Def.bufferCap ? "Full" : $"Needs {ContentDB.ItemName(b.Def.inputItem)}") + ready;
                fill = 0f; col = LabelStuck;
                return;
            }
            fill = b.Stage01;
            if (b.Waiting) { text = b.StageText(false) + ready; col = LabelRun; return; }
            bool hands = bm.HandsAt(b) > 0 || bm.PlayerWorkTarget == b;
            // at night the kin who would do this may all be in bed: it waits for morning (or for you)
            bool abed = !hands && bm.NightNow && bm.AnyoneCan(b, b.Stage) && !bm.AnyoneCan(b, b.Stage, true);
            text = b.StageText(hands) + (abed ? "  ·  kin asleep" : "") + ready;
            col = abed ? new Color(0.62f, 0.68f, 0.9f) : hands || bm.AnyoneCan(b, b.Stage) ? LabelWork : LabelStuck;
        }

        static void StationLabel(BaseManager bm, Building b, out string text, out float fill, out Color col)
        {
            col = LabelWork;
            if (b.Def.station == CraftStation.Research)
            {
                text = Tech.Active != null ? "Researching " + Tech.Active.name : "";
                fill = Tech.Progress01;
                return;
            }
            text = ""; fill = 0f;
            foreach (var o in bm.Orders)
            {
                if (!ContentDB.Recipes.TryGetValue(o.recipeId, out var r) || r.station != b.Def.station) continue;
                text = $"{MenuUI.StationActivity(b.Def.station)} {ContentDB.ItemName(r.outputItem)}" + (o.remaining > 1 ? $"  x{o.remaining}" : "");
                fill = bm.OrderProgress01(o);
                return;
            }
        }
    }
}
