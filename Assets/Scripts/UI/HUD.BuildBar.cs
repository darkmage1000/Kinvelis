using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>
    /// The build bar (user, 2026-10-28: "easy and functional building a toddler could use ... swap from placing to
    /// destroying in that ui for placing", then "tapping build should open placing, with an option at top for house or
    /// camp"): BUILD goes straight into placing, and while placing a strip along the bottom shows, as pictures, either
    /// REMOVE and every house piece you have the blueprint for (HOUSE) or every camp structure you can build at your level
    /// (CAMP). HOUSE / CAMP tabs sit on top of it. Tap a picture to switch to it at once; the one in hand is lit gold, ones
    /// you can't afford are dimmed.
    /// </summary>
    public partial class HUD
    {
        RectTransform buildBar, buildBarContent;
        ScrollRect buildBarScroll;
        Image houseTab, campTab;
        readonly List<(object def, Image bg, Image pic, Text name)> buildChips = new List<(object, Image, Image, Text)>();
        Image removeChip;
        int buildBarStamp = -1;
        bool buildBarWasOn, buildBarHouse;

        static readonly Color ChipCol = new Color(0.16f, 0.2f, 0.26f, 0.92f), ChipOn = new Color(0.85f, 0.65f, 0.2f, 0.95f),
            ChipRemove = new Color(0.55f, 0.24f, 0.22f, 0.92f), ChipRemoveOn = new Color(0.9f, 0.35f, 0.3f, 0.98f),
            TabOn = new Color(0.85f, 0.65f, 0.2f, 0.98f), TabOff = new Color(0.16f, 0.2f, 0.26f, 0.92f);

        void BuildBuildBar()
        {
            buildBar = UIFactory.Rect(placeCluster, "BuildBar");
            buildBar.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-60, 22), new Vector2(940, 150));
            var bg = UIFactory.Panel(buildBar, "Bg", new Color(0.05f, 0.07f, 0.1f, 0.55f), UIFactory.Rounded);
            bg.type = Image.Type.Sliced;
            bg.rectTransform.Stretch();
            buildBarScroll = bg.gameObject.AddComponent<ScrollRect>();
            bg.gameObject.AddComponent<RectMask2D>();
            buildBarContent = UIFactory.Rect(bg.transform, "Content");
            buildBarContent.anchorMin = new Vector2(0, 0); buildBarContent.anchorMax = new Vector2(0, 1); buildBarContent.pivot = new Vector2(0, 0.5f);
            buildBarContent.offsetMin = Vector2.zero; buildBarContent.offsetMax = Vector2.zero;
            var hl = buildBarContent.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 8; hl.padding = new RectOffset(10, 10, 8, 8);
            hl.childControlHeight = true; hl.childControlWidth = true; hl.childForceExpandHeight = true; hl.childForceExpandWidth = false;
            var fit = buildBarContent.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            buildBarScroll.content = buildBarContent;
            buildBarScroll.vertical = false;
            buildBarScroll.movementType = ScrollRect.MovementType.Clamped;
            buildBarScroll.viewport = bg.rectTransform;

            // HOUSE / CAMP tabs on top of the bar's left end
            houseTab = Tab("HOUSE", 0, () => BuildPlacement.I.OpenBuild(true));
            campTab = Tab("CAMP", 1, () => BuildPlacement.I.OpenBuild(false));
            buildBar.gameObject.SetActive(false);
        }

        Image Tab(string label, int i, System.Action onTap)
        {
            var b = UIFactory.Button(buildBar, label, onTap, TabOff, 24);
            var img = b.GetComponent<Image>();
            img.sprite = UIFactory.Rounded; img.type = Image.Type.Sliced;
            ((RectTransform)b.transform).Place(new Vector2(0, 1), new Vector2(0, 0), new Vector2(10 + i * 168, 6), new Vector2(160, 58));
            return img;
        }

        /// <summary>Fills the bar for the mode in hand (again whenever what you can build changes).</summary>
        void FillBuildBar(bool house)
        {
            foreach (Transform c in buildBarContent) Destroy(c.gameObject);
            buildChips.Clear();
            removeChip = null;
            if (house)
            {
                removeChip = Chip("REMOVE", null, ChipRemove, () => BuildPlacement.I.BeginRemove());
                foreach (var d in Housing.Pieces)
                {
                    if (Housing.Locked(d)) continue;
                    var dd = d;
                    var bg = Chip(d.name, ItemIcons.Get("hp_" + d.id), ChipCol, () => BuildPlacement.I.BeginPiece(dd));
                    buildChips.Add((d, bg, bg.transform.Find("Pic")?.GetComponent<Image>(), bg.transform.Find("Name")?.GetComponent<Text>()));
                }
            }
            else
                foreach (var d in MenuUI.CampBuildable())
                {
                    var dd = d;
                    var bg = Chip(d.displayName, ItemIcons.Get("bld_" + d.id), ChipCol, () => BuildPlacement.I.Begin(dd));
                    buildChips.Add((d, bg, bg.transform.Find("Pic")?.GetComponent<Image>(), bg.transform.Find("Name")?.GetComponent<Text>()));
                }
            buildBarWasOn = false;   // scroll to the one in hand again
        }

        Image Chip(string label, Sprite pic, Color col, System.Action onTap)
        {
            var b = UIFactory.Button(buildBarContent, "", onTap, col, 18);
            var img = b.GetComponent<Image>();
            img.sprite = UIFactory.Rounded; img.type = Image.Type.Sliced;
            UIFactory.Size(b, 130, 124);
            var oldLabel = b.GetComponentInChildren<Text>();
            if (oldLabel != null) Destroy(oldLabel.gameObject);
            if (pic != null)
            {
                var p = UIFactory.Panel(b.transform, "Pic", Color.white, pic);
                p.preserveAspect = true; p.raycastTarget = false;
                p.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(88, 80));
            }
            else
            {
                // REMOVE: a big cross instead of a picture
                var x = UIFactory.Label(b.transform, "X", 64, new Color(1f, 0.9f, 0.85f), TextAnchor.MiddleCenter, FontStyle.Bold);
                x.raycastTarget = false;
                x.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(110, 84));
            }
            var t = UIFactory.Label(b.transform, label, 17, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.name = "Name"; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 11; t.resizeTextMaxSize = 17;
            t.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 4), new Vector2(118, 40));
            return img;
        }

        void UpdateBuildBar()
        {
            var bp = BuildPlacement.I;
            bool on = bp.Active;
            if (buildBar.gameObject.activeSelf != on) buildBar.gameObject.SetActive(on);
            if (!on) { buildBarWasOn = false; return; }
            bool house = bp.Piece != null || bp.Removing;
            // refill on a mode switch, or when what you can build changes (a level, research, a blueprint, the camp founded)
            var st = GameManager.I.Player.Stats;
            int stamp = (house ? 1 : 0) + 2 * st.blueprints.Count + 1000 * st.level + 100000 * st.techDone.Count + (BaseManager.I.HasBase ? 50000000 : 0);
            if (stamp != buildBarStamp || house != buildBarHouse || buildChips.Count == 0) { buildBarStamp = stamp; buildBarHouse = house; FillBuildBar(house); }
            houseTab.color = house ? TabOn : TabOff;
            campTab.color = house ? TabOff : TabOn;
            houseTab.gameObject.SetActive(BaseManager.I.HasBase);   // no house before there is a camp
            bool free = WorldRules.Current.freeBuild;
            int picked = -1;
            for (int i = 0; i < buildChips.Count; i++)
            {
                var c = buildChips[i];
                bool cur = house ? !bp.Removing && bp.Piece == c.def : bp.Current == c.def;
                if (cur) picked = i;
                var cost = c.def is HousePieceDef hp ? hp.cost : ((BuildingDef)c.def).cost;
                bool can = free || ItemSources.Has(cost);
                c.bg.color = cur ? ChipOn : ChipCol;
                if (c.pic != null) c.pic.color = can ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                if (c.name != null) c.name.color = can ? Color.white : new Color(1f, 0.6f, 0.55f);
            }
            if (removeChip != null) removeChip.color = bp.Removing ? ChipRemoveOn : ChipRemove;
            // on opening, scroll so the one in hand is in view
            if (!buildBarWasOn)
            {
                buildBarWasOn = true;
                Canvas.ForceUpdateCanvases();
                float w = buildBarContent.rect.width, view = ((RectTransform)buildBarScroll.viewport).rect.width;
                float x = picked < 0 ? 0f : 10 + (picked + (removeChip != null ? 1 : 0)) * 132f;
                buildBarScroll.horizontalNormalizedPosition = w > view ? Mathf.Clamp01((x - view * 0.4f) / (w - view)) : 0f;
            }
        }
    }
}
