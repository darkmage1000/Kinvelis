using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>
    /// The SEED button (user playtest 2026-10-15: "swapping seeds should be simpler than entering inv ... the button for
    /// going into throw seed mode, if held should show all seed options and i should drag into the one i want").
    /// A tap is the old SEED press (ready / put away, or a one-tap throw with simple buttons). Hold it (`HoldTime`) and the
    /// seeds you carry fan out above it, each with its picture and count; slide your thumb onto one and let go to pick
    /// it (`PlayerStats.selectedSeed`), let go anywhere else to keep the one you had. Legend seeds only show while their
    /// legend is out, as with `CaptureSystem.SelectedSeed`. Replaces the TouchButton HUD.BuildActions puts on SEED.
    /// </summary>
    public class SeedWheel : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        const float HoldTime = 0.28f, Radius = 250f, Bubble = 118f, PickDist = 90f;
        public Image image;
        public Color normal, pressed;

        int pointer = -99;
        float downAt;
        bool open;
        Vector2 local;
        RectTransform root;
        readonly List<(string id, RectTransform rt, Image bg)> options = new List<(string, RectTransform, Image)>();
        int hover = -1;

        public void OnPointerDown(PointerEventData e)
        {
            if (pointer != -99) return;
            pointer = e.pointerId;
            downAt = Time.unscaledTime;
            open = false; hover = -1;
            if (image != null) image.color = pressed;
            transform.localScale = Vector3.one * 0.92f;
            Track(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != pointer) return;
            Track(e);
            if (!open) return;
            int best = -1; float bd = PickDist;
            for (int i = 0; i < options.Count; i++)
            {
                float d = (options[i].rt.anchoredPosition - local).magnitude;
                if (d < bd) { bd = d; best = i; }
            }
            if (best != hover)
            {
                hover = best;
                for (int i = 0; i < options.Count; i++)
                {
                    options[i].bg.color = i == hover ? new Color(0.55f, 0.95f, 0.5f, 0.95f) : new Color(0.12f, 0.2f, 0.14f, 0.88f);
                    options[i].rt.localScale = Vector3.one * (i == hover ? 1.15f : 1f);
                }
                if (hover >= 0) SynthAudio.Play(Sfx.UiClick, 1.5f, 0.3f);
            }
        }

        void Track(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, e.position, e.pressEventCamera, out local);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId != pointer) return;
            pointer = -99;
            if (image != null) image.color = normal;
            transform.localScale = Vector3.one;
            if (open)
            {
                var stats = GameManager.I != null ? GameManager.I.Player.Stats : null;
                if (hover >= 0 && stats != null)
                {
                    string id = options[hover].id;
                    if (stats.selectedSeed != id)
                    {
                        stats.selectedSeed = id;
                        GameEvents.Say("Seed: " + ContentDB.ItemName(id), new Color(0.7f, 1f, 0.7f));
                        GameEvents.RaiseInventoryChanged();
                    }
                    SynthAudio.Play(Sfx.Pickup, 1.2f, 0.5f);
                }
                Close();
                return;
            }
            // a tap: the old SEED press
            InputHub.Press(Btn.Throw);
            InputHub.Release(Btn.Throw);
        }

        void Update()
        {
            if (pointer == -99 || open || Time.unscaledTime - downAt < HoldTime) return;
            Open();
        }

        void Open()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            var stats = gm.Player.Stats;
            var kinds = new List<ItemDef>();
            foreach (var s in stats.Bag.slots)
            {
                if (s.Empty) continue;
                var it = ContentDB.Item(s.id);
                if (it == null || it.category != ItemCategory.CaptureSeed || kinds.Contains(it)) continue;
                if (CaptureSystem.IsLegendSeed(it) && !(LegendHunts.LegendOut || Wardens.BossOut(it.legendFor))) continue;
                kinds.Add(it);
            }
            open = true;
            if (kinds.Count == 0) { GameEvents.Warn("No capture seeds! Craft Tether Seeds (Menu > Craft)."); return; }
            kinds.Sort((a, b) => Seeds.Rank(a).CompareTo(Seeds.Rank(b)));
            if (root == null)
            {
                root = UIFactory.Rect(transform, "SeedWheel");
                root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
                root.anchoredPosition = Vector2.zero; root.sizeDelta = Vector2.zero;
            }
            root.gameObject.SetActive(true);
            foreach (var o in options) Destroy(o.rt.gameObject);
            options.Clear();
            string cur = gm.Capture.SelectedSeed(stats)?.id;
            // fanned out above and to the left of the button (it sits at the bottom right)
            int n = kinds.Count;
            for (int i = 0; i < n; i++)
            {
                float t = n == 1 ? 0.5f : i / (float)(n - 1);
                float ang = Mathf.Lerp(95f, 185f, t) * Mathf.Deg2Rad;
                float r = Radius + (n > 5 ? (i % 2) * 90f : 0f);
                var bg = UIFactory.Panel(root, "Seed_" + kinds[i].id, new Color(0.12f, 0.2f, 0.14f, 0.88f), UIFactory.Circle);
                bg.type = Image.Type.Simple;
                bg.raycastTarget = false;
                bg.rectTransform.anchorMin = bg.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                bg.rectTransform.sizeDelta = new Vector2(Bubble, Bubble);
                bg.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
                var icon = UIFactory.Panel(bg.transform, "Icon", Color.white, ItemIcons.Get(kinds[i].id));
                icon.type = Image.Type.Simple;
                icon.raycastTarget = false;
                icon.rectTransform.Stretch(14, 22, 14, 6);
                icon.preserveAspect = true;
                var count = UIFactory.Label(bg.transform, $"{stats.Bag.Count(kinds[i].id)}", 20, Color.white, TextAnchor.LowerCenter, FontStyle.Bold);
                count.raycastTarget = false;
                count.rectTransform.Stretch(4, 2, 4, 4);
                var name = UIFactory.Label(bg.transform, kinds[i].displayName.Replace(" Seed", ""), 17, kinds[i].id == cur ? new Color(1f, 0.9f, 0.5f) : Color.white, TextAnchor.UpperCenter, FontStyle.Bold);
                name.raycastTarget = false;
                name.rectTransform.anchorMin = new Vector2(0, 1); name.rectTransform.anchorMax = new Vector2(1, 1);
                name.rectTransform.pivot = new Vector2(0.5f, 0f);
                name.rectTransform.anchoredPosition = new Vector2(0, 4); name.rectTransform.sizeDelta = new Vector2(70, 26);
                options.Add((kinds[i].id, bg.rectTransform, bg));
            }
            SynthAudio.Play(Sfx.UiOpen, 1.2f, 0.5f);
        }

        void Close()
        {
            open = false; hover = -1;
            foreach (var o in options) Destroy(o.rt.gameObject);
            options.Clear();
            if (root != null) root.gameObject.SetActive(false);
        }

        void OnDisable()
        {
            if (open) Close();
            pointer = -99;
            transform.localScale = Vector3.one;
            if (image != null) image.color = normal;
        }
    }
}
