using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>
    /// The house build bar (user, 2026-10-28: "easy and functional building a toddler could use ... swap from placing to
    /// destroying in that ui for placing, not have to go into menu building house then pick it"): while you place a house
    /// piece or use REMOVE, a strip along the bottom shows REMOVE and every house piece you can build, as pictures. Tap one
    /// to switch to it at once; the one in hand is lit gold, ones you can't afford are dimmed.
    /// </summary>
    public partial class HUD
    {
        RectTransform buildBar, buildBarContent;
        ScrollRect buildBarScroll;
        readonly List<(HousePieceDef def, Image bg, Image pic, Text name)> buildChips = new List<(HousePieceDef, Image, Image, Text)>();
        Image removeChip;
        int buildBarStamp = -1;
        bool buildBarWasOn;

        static readonly Color ChipCol = new Color(0.16f, 0.2f, 0.26f, 0.92f), ChipOn = new Color(0.85f, 0.65f, 0.2f, 0.95f),
            ChipRemove = new Color(0.55f, 0.24f, 0.22f, 0.92f), ChipRemoveOn = new Color(0.9f, 0.35f, 0.3f, 0.98f);

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
            buildBar.gameObject.SetActive(false);
        }

        /// <summary>Fills the bar with REMOVE and the pieces you have the blueprint for (again whenever the house changes).</summary>
        void FillBuildBar()
        {
            foreach (Transform c in buildBarContent) Destroy(c.gameObject);
            buildChips.Clear();
            removeChip = Chip("REMOVE", null, ChipRemove, () => BuildPlacement.I.BeginRemove());
            foreach (var d in Housing.Pieces)
            {
                if (Housing.Locked(d)) continue;
                var dd = d;
                var bg = Chip(d.name, ItemIcons.Get("hp_" + d.id), ChipCol, () => BuildPlacement.I.BeginPiece(dd));
                buildChips.Add((d, bg, bg.transform.Find("Pic")?.GetComponent<Image>(), bg.transform.Find("Name")?.GetComponent<Text>()));
            }
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
            bool on = bp.Active && (bp.Piece != null || bp.Removing);
            if (buildBar.gameObject.activeSelf != on) buildBar.gameObject.SetActive(on);
            if (!on) { buildBarWasOn = false; return; }
            // blueprints you find while building show up the next time it is filled
            int stamp = GameManager.I.Player.Stats.blueprints.Count;
            if (stamp != buildBarStamp || buildChips.Count == 0) { buildBarStamp = stamp; FillBuildBar(); }
            bool free = WorldRules.Current.freeBuild;
            int picked = -1;
            for (int i = 0; i < buildChips.Count; i++)
            {
                var c = buildChips[i];
                bool cur = !bp.Removing && bp.Piece == c.def;
                if (cur) picked = i;
                bool can = free || ItemSources.Has(c.def.cost);
                c.bg.color = cur ? ChipOn : ChipCol;
                if (c.pic != null) c.pic.color = can ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                if (c.name != null) c.name.color = can ? Color.white : new Color(1f, 0.6f, 0.55f);
            }
            removeChip.color = bp.Removing ? ChipRemoveOn : ChipRemove;
            // on opening, scroll so the piece in hand is in view
            if (!buildBarWasOn)
            {
                buildBarWasOn = true;
                Canvas.ForceUpdateCanvases();
                float w = buildBarContent.rect.width, view = ((RectTransform)buildBarScroll.viewport).rect.width;
                float x = picked < 0 ? 0f : 10 + (picked + 1) * 132f;
                buildBarScroll.horizontalNormalizedPosition = w > view ? Mathf.Clamp01((x - view * 0.4f) / (w - view)) : 0f;
            }
        }
    }
}
