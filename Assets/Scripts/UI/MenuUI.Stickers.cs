using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>
    /// The sticker book (MenuTab.Stickers, shown instead of the Dex while a world uses simple buttons): one big picture
    /// per kind of kin. Caught = in colour with its name; seen but not caught = a grey shape. No stats, nothing to
    /// read - it is the Dex for somebody who picks kin because they look cool.
    /// </summary>
    public partial class MenuUI
    {
        void BuildStickers()
        {
            SaveScroll();
            var stats = GameManager.I.Player.Stats;
            var list = TrackedList(content);
            var caught = new List<SpeciesDef>();
            var seen = new List<SpeciesDef>();
            foreach (var sp in ContentDB.SpeciesList)
            {
                if (sp.bossOnly) continue;
                var e = stats.DexFor(sp.id, false);
                if (stats.registeredSpecies.Contains(sp.id) || (e != null && e.caught > 0)) caught.Add(sp);
                else if (e != null && e.seen > 0) seen.Add(sp);
            }
            caught.Sort((a, b) => a.dexNumber.CompareTo(b.dexNumber));
            seen.Sort((a, b) => a.dexNumber.CompareTo(b.dexNumber));

            Header(list, $"My Stickers  <size=30>{UIFactory.Col(caught.Count.ToString(), Quests.Gold)}</size>", 40);
            Para(list, "Every kind of kin you catch is a sticker. Grey ones you have seen but not caught yet.", 22);
            StickerRows(list, caught, true);
            if (seen.Count > 0)
            {
                Header(list, "Seen", 30);
                StickerRows(list, seen, false);
            }
            RestoreScroll();
        }

        void StickerRows(RectTransform list, List<SpeciesDef> kinds, bool have)
        {
            const int perRow = 6;
            HorizontalLayoutGroup row = null;
            for (int i = 0; i < kinds.Count; i++)
            {
                if (i % perRow == 0) { row = UIFactory.HRow(list, 214, 12); row.childForceExpandHeight = false; row.childForceExpandWidth = false; }
                var sp = kinds[i];
                Color el = ElementInfo.Color(sp.element1);
                var cell = UIFactory.Panel(row.transform, "Sticker", have ? Color.Lerp(el, new Color(0.12f, 0.14f, 0.18f), 0.62f) : new Color(0.14f, 0.16f, 0.2f));
                UIFactory.Size(cell, 206, 200);
                var pic = UIFactory.Rect(cell.transform, "Pic").gameObject.AddComponent<Image>();
                pic.sprite = ItemIcons.Get("kin_" + sp.id);
                pic.preserveAspect = true; pic.raycastTarget = false;
                pic.color = have ? Color.white : new Color(0.05f, 0.06f, 0.08f, 0.85f);
                pic.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -8), new Vector2(150, 150));
                var name = UIFactory.Label(cell.transform, have ? sp.displayName : "?", 24, have ? Color.white : UIFactory.TextDim, TextAnchor.MiddleCenter, FontStyle.Bold);
                name.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 6), new Vector2(190, 38));
                name.resizeTextForBestFit = true; name.resizeTextMinSize = 14; name.resizeTextMaxSize = 24;
            }
        }
    }
}
