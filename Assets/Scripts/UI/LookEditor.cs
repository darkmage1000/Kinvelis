using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>
    /// The character creator's controls: rows of big buttons and colour swatches that edit an <see cref="Appearance"/>
    /// in place. Used by the main menu when a world is made and by the Gear tab's CHANGE LOOK; whoever hosts it shows
    /// the explorer and rebuilds the model in <c>changed</c>. Nothing here rebuilds the list, so the scroll position
    /// stays put while you tap through options.
    /// </summary>
    public static class LookEditor
    {
        static readonly Color On = new Color(0.25f, 0.55f, 0.45f);

        public static void Build(RectTransform list, Appearance a, Action changed)
        {
            var refresh = new List<Action>();
            void Changed() { foreach (var r in refresh) r(); changed?.Invoke(); }
            // how wide the list is decides how many buttons go on a row (the menu's panel is wider than the Gear tab's)
            Canvas.ForceUpdateCanvases();
            width = list.rect.width > 200f ? list.rect.width - 8f : 640f;

            Names(list, refresh, "Body", AvatarLook.BodyNames, 2, () => a.body, v => { a.body = v; Changed(); });
            Swatches(list, refresh, "Skin", AvatarLook.Skin, () => a.skin, v => { a.skin = v; Changed(); });
            Names(list, refresh, "Hair", AvatarLook.HairNames, 5, () => a.hair, v => { a.hair = v; Changed(); });
            Swatches(list, refresh, "Hair colour", AvatarLook.HairColors, () => a.hairColor, v => { a.hairColor = v; Changed(); });
            Swatches(list, refresh, "Eyes", AvatarLook.EyeColors, () => a.eyes, v => { a.eyes = v; Changed(); });
            Names(list, refresh, "Outfit", AvatarLook.OutfitNames, 4, () => a.outfit, v => { a.outfit = v; Changed(); });
            Swatches(list, refresh, "Outfit colour", AvatarLook.Cloth, () => AvatarLook.IndexA(a), v => { AvatarLook.SetA(a, v); Changed(); });
            Swatches(list, refresh, "Second colour", AvatarLook.Cloth, () => AvatarLook.IndexB(a), v => { AvatarLook.SetB(a, v); Changed(); });
            Names(list, refresh, "On your head", AvatarLook.HatNames, 3, () => a.hat, v => { a.hat = v; Changed(); });
            Swatches(list, refresh, "Hat, pack and trim colour", AvatarLook.Cloth, () => AvatarLook.IndexAccent(a), v => { AvatarLook.SetAccent(a, v); Changed(); });
            Names(list, refresh, "Backpack", new[] { "Wear it", "Leave it" }, 2, () => a.pack ? 0 : 1, v => { a.pack = v == 0; Changed(); });
            foreach (var r in refresh) r();
        }

        static float width;

        static void Title(RectTransform list, string text)
        {
            var h = UIFactory.Label(list, text, 26, UIFactory.Accent, TextAnchor.LowerLeft, FontStyle.Bold);
            UIFactory.Size(h, 40);
        }

        /// <summary>Named choices, a few to a row.</summary>
        static void Names(RectTransform list, List<Action> refresh, string title, string[] names, int perRow, Func<int> get, Action<int> set)
        {
            Title(list, title);
            while (perRow > 2 && (width - 8f * (perRow - 1)) / perRow < 150f) perRow--;   // never narrower than a thumb
            float w = Mathf.Min(300f, (width - 8f * (perRow - 1)) / perRow);
            Transform row = null;
            for (int i = 0; i < names.Length; i++)
            {
                if (i % perRow == 0) row = UIFactory.HRow(list, 76).transform;
                int idx = i;
                var b = UIFactory.Button(row, names[i], () => set(idx), UIFactory.ButtonCol, 26);
                UIFactory.Size(b, 76, w);
                var img = b.GetComponent<Image>();
                refresh.Add(() => img.color = get() == idx ? On : UIFactory.ButtonCol);
            }
        }

        /// <summary>Colour swatches, eight to a row; the chosen one is full size with a white edge.</summary>
        static void Swatches(RectTransform list, List<Action> refresh, string title, Color[] colors, Func<int> get, Action<int> set)
        {
            Title(list, title);
            int perRow = Mathf.Clamp(Mathf.FloorToInt((width + 10f) / 90f), 4, 8);
            Transform row = null;
            for (int i = 0; i < colors.Length; i++)
            {
                if (i % perRow == 0) row = UIFactory.HRow(list, 80, 10).transform;
                int idx = i;
                var b = UIFactory.Button(row, "", () => set(idx), colors[i], 20);
                UIFactory.Size(b, 80, 80);
                var edge = b.gameObject.AddComponent<Outline>();
                edge.effectColor = Color.white;
                edge.effectDistance = new Vector2(5, -5);
                var tr = b.transform;
                refresh.Add(() =>
                {
                    bool on = get() == idx;
                    edge.enabled = on;
                    tr.localScale = Vector3.one * (on ? 1f : 0.8f);
                });
            }
        }
    }
}
