using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wildbond
{
    public enum MenuTab { Bag, Gear, Craft, Kin, Dex, Camp, Build, Skills, Map, Settings, Journey, Trader, Station, KinShop, Requests, Tamer, Warden, Outpost, Hunt, Explore, Quests, Stickers, Box, Dev }

    /// <summary>Full-screen tabbed menu (pauses the world). Content is rebuilt from data on open/change.</summary>
    public partial class MenuUI : MonoBehaviour
    {
        public static MenuUI I { get; private set; }

        public bool IsOpen => panel != null && panel.activeSelf;
        public MenuTab Tab { get; private set; }

        GameObject panel;
        RectTransform content;
        readonly Dictionary<MenuTab, Image> tabButtons = new Dictionary<MenuTab, Image>();
        bool dirty;
        GameObject modal;

        // selections
        int selectedSlot = -1;
        string selectedKin;

        public void Build(Canvas canvas)
        {
            I = this;
            var bg = UIFactory.Panel(canvas.transform, "Menu", new Color(0.04f, 0.06f, 0.09f, 0.94f));
            bg.rectTransform.Stretch();
            panel = bg.gameObject;

            var tabs = UIFactory.Rect(bg.transform, "Tabs");
            tabs.Anchors(new Vector2(0, 0), new Vector2(0, 1), new Vector2(20, 20), new Vector2(270, -20));
            var vl = tabs.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 10; vl.childControlHeight = true; vl.childControlWidth = true; vl.childForceExpandHeight = true;
            foreach (MenuTab t in Enum.GetValues(typeof(MenuTab)))
            {
                var tab = t;
                var b = UIFactory.Button(tabs, TabName(t), () => Open(tab), UIFactory.ButtonCol, 30);
                tabButtons[t] = (Image)b.targetGraphic;
            }
            tabButtons[MenuTab.Trader].gameObject.SetActive(false);   // opened by talking to the trader, not from the sidebar
            tabButtons[MenuTab.Warden].gameObject.SetActive(false);   // a Warden's shrine
            tabButtons[MenuTab.Outpost].gameObject.SetActive(false);  // an outpost tent (or Camp > Outposts)
            tabButtons[MenuTab.Hunt].gameObject.SetActive(false);     // Maro the Seeker (or Journey > NOTEBOOK)
            tabButtons[MenuTab.Explore].gameObject.SetActive(false);  // Journey > LOG, or READ at a tablet
            tabButtons[MenuTab.Requests].gameObject.SetActive(false); // the board at a settlement's gate
            tabButtons[MenuTab.Tamer].gameObject.SetActive(false);    // a rival tamer's challenge
            tabButtons[MenuTab.KinShop].gameObject.SetActive(false);  // opened by talking to a settlement's Kinkeeper
            tabButtons[MenuTab.Station].gameObject.SetActive(false);  // opened at a station (COOK / CRAFT) or from the Craft tab
            tabButtons[MenuTab.Box].gameObject.SetActive(false);      // opened at a chest or a feed box (OPEN), or from Camp > Storage
            tabButtons[MenuTab.Stickers].transform.SetSiblingIndex(tabButtons[MenuTab.Kin].transform.GetSiblingIndex() + 1);   // sticker book sits under KIN
            tabButtons[MenuTab.Quests].transform.SetAsFirstSibling(); // the quest log leads the sidebar (new tabs go at the end of the enum)
            var close = UIFactory.Button(tabs, "CLOSE", Close, new Color(0.55f, 0.25f, 0.25f, 0.95f), 30);

            var area = UIFactory.Panel(bg.transform, "Content", UIFactory.PanelBg2);
            area.rectTransform.Anchors(Vector2.zero, Vector2.one, new Vector2(290, 20), new Vector2(-20, -20));
            content = area.rectTransform;
            panel.SetActive(false);

            GameEvents.InventoryChanged += MarkDirty;
            GameEvents.RosterChanged += MarkDirty;
            GameEvents.BaseChanged += MarkDirty;
        }

        void OnDestroy()
        {
            GameEvents.InventoryChanged -= MarkDirty;
            gearPreview?.Dispose();
            GameEvents.RosterChanged -= MarkDirty;
            GameEvents.BaseChanged -= MarkDirty;
        }

        static string TabName(MenuTab t)
        {
            switch (t)
            {
                case MenuTab.Kin: return "KIN";
                case MenuTab.Camp: return "CAMP";
                default: return t.ToString().ToUpperInvariant();
            }
        }

        void MarkDirty() { if (IsOpen) dirty = true; }

        public void Toggle()
        {
            if (IsOpen) { Close(); return; }
            // simple buttons: the menu opens on the stars, never on a tab that is hidden
            Open(WorldRules.Current.simpleControls && System.Array.IndexOf(KidHidden, Tab) >= 0 ? MenuTab.Quests : Tab);
        }

        /// <summary>Tabs a Little Explorer never needs (simple buttons): what is left is Stars, Kin, Stickers, Build, Map, Settings.</summary>
        static readonly MenuTab[] KidHidden = { MenuTab.Bag, MenuTab.Gear, MenuTab.Craft, MenuTab.Dex, MenuTab.Camp, MenuTab.Skills, MenuTab.Journey };

        void ApplyKidTabs()
        {
            bool simple = WorldRules.Current.simpleControls;
            foreach (var t in KidHidden) tabButtons[t].gameObject.SetActive(!simple);
            tabButtons[MenuTab.Stickers].gameObject.SetActive(simple);
            tabButtons[MenuTab.Dev].gameObject.SetActive(Dev.On);   // the tester's tab, only while dev mode is on
            var qt = tabButtons[MenuTab.Quests].GetComponentInChildren<Text>();
            if (qt != null) qt.text = simple ? "STARS" : "QUESTS";
        }

        public void Open(MenuTab t)
        {
            if (t != MenuTab.Map && t != MenuTab.Dex) MapView.HabitatFocus = null;
            if (BuildPlacement.I != null && BuildPlacement.I.Active) BuildPlacement.I.Cancel();
            Tab = t;
            panel.SetActive(true);
            GameManager.I.SetPaused(true);
            SynthAudio.Play(Sfx.UiOpen);
            Rebuild();
        }

        public void Close()
        {
            CloseModal();
            panel.SetActive(false);
            GameManager.I.SetPaused(false);
        }

        void LateUpdate()
        {
            if (!IsOpen) return;
            if (InputHub.Down(Btn.Cancel) || InputHub.Down(Btn.Menu)) { if (modal != null) CloseModal(); else Close(); return; }
            if (dirty) { dirty = false; Rebuild(); }
            if (Tab == MenuTab.Camp || Tab == MenuTab.Craft || Tab == MenuTab.Station || Tab == MenuTab.Outpost) LiveRefresh();
            TickGearPreview();
            TickKinCard();
        }

        float liveTimer;
        void LiveRefresh()
        {
            liveTimer -= Time.unscaledDeltaTime;
            if (liveTimer > 0f) return;
            liveTimer = 1f;
            dirty = true;
        }

        void Rebuild()
        {
            ApplyKidTabs();
            foreach (var kv in tabButtons) kv.Value.color = kv.Key == Tab ? new Color(0.25f, 0.55f, 0.45f, 1f) : UIFactory.ButtonCol;
            for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);
            switch (Tab)
            {
                case MenuTab.Bag: BuildBag(); break;
                case MenuTab.Gear: BuildGear(); break;
                case MenuTab.Dex: BuildDex(); break;
                case MenuTab.Trader: BuildTrader(); break;
                case MenuTab.Station: BuildStation(); break;
                case MenuTab.KinShop: BuildKinShop(); break;
                case MenuTab.Requests: BuildRequests(); break;
                case MenuTab.Tamer: BuildTamer(); break;
                case MenuTab.Journey: BuildJourney(); break;
                case MenuTab.Warden: BuildWarden(); break;
                case MenuTab.Outpost: BuildOutpost(); break;
                case MenuTab.Hunt: BuildHunt(); break;
                case MenuTab.Explore: BuildExplore(); break;
                case MenuTab.Quests: BuildQuests(); break;
                case MenuTab.Stickers: BuildStickers(); break;
                case MenuTab.Box: BuildBox(); break;
                case MenuTab.Dev: BuildDev(); break;
                case MenuTab.Craft: BuildCraft(); break;
                case MenuTab.Kin: BuildKin(); break;
                case MenuTab.Camp: BuildCamp(); break;
                case MenuTab.Build: BuildBuild(); break;
                case MenuTab.Skills: BuildSkills(); break;
                case MenuTab.Map: BuildMap(); break;
                case MenuTab.Settings: BuildSettings(); break;
            }
        }

        // ============================================================ HELPERS
        Text Header(Transform parent, string text, int size = 40)
        {
            var t = UIFactory.Label(parent, text, size, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            t.supportRichText = true;
            UIFactory.Size(t, size + 16);
            return t;
        }

        Text Para(Transform parent, string text, int size = 24, Color? c = null)
        {
            var t = UIFactory.Label(parent, text, size, c ?? UIFactory.TextDim, TextAnchor.UpperLeft);
            t.supportRichText = true;
            var fit = t.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return t;
        }

        RectTransform LeftRight(out RectTransform left, out RectTransform right, float split = 0.55f)
        {
            left = UIFactory.Rect(content, "Left");
            left.Anchors(new Vector2(0, 0), new Vector2(split, 1), new Vector2(16, 16), new Vector2(-8, -16));
            right = UIFactory.Rect(content, "Right");
            right.Anchors(new Vector2(split, 0), new Vector2(1, 1), new Vector2(8, 16), new Vector2(-16, -16));
            return content;
        }

        static string Ingredients(Ingredient[] list)
        {
            var parts = new List<string>();
            foreach (var ing in list)
            {
                int have = ItemSources.Count(ing.itemId);
                Color c = have >= ing.count ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.55f, 0.5f);
                parts.Add(UIFactory.Col($"{ContentDB.ItemName(ing.itemId)} {have}/{ing.count}", c));
            }
            return string.Join("   ", parts);
        }

        Slider MakeSlider(Transform parent, string label, float value, Action<float> onChange)
        {
            var row = UIFactory.HRow(parent, 70, 16);
            var l = UIFactory.Label(row.transform, label, 26, Color.white);
            UIFactory.Size(l, 60, 300);
            var bg = UIFactory.Panel(row.transform, "Slider", new Color(0, 0, 0, 0.4f));
            UIFactory.Size(bg, 50, 600);
            var s = bg.gameObject.AddComponent<Slider>();
            var fillArea = UIFactory.Rect(bg.transform, "FillArea"); fillArea.Stretch(10, 12, 10, 12);
            var fill = UIFactory.Panel(fillArea, "Fill", UIFactory.Accent);
            fill.rectTransform.Stretch();
            var handleArea = UIFactory.Rect(bg.transform, "HandleArea"); handleArea.Stretch(20, 0, 20, 0);
            var handle = UIFactory.Panel(handleArea, "Handle", Color.white, UIFactory.Circle);
            handle.type = Image.Type.Simple;
            handle.rectTransform.sizeDelta = new Vector2(46, 0); // height follows the handle area
            s.fillRect = fill.rectTransform;
            s.handleRect = handle.rectTransform;
            s.targetGraphic = handle;
            s.minValue = 0f; s.maxValue = 1f;
            s.value = value;
            s.onValueChanged.AddListener(v => onChange(v));
            return s;
        }

        void ShowModal(string title, Action<RectTransform> fill)
        {
            CloseModal();
            var bg = UIFactory.Panel(panel.transform, "Modal", new Color(0, 0, 0, 0.7f));
            bg.rectTransform.Stretch();
            var box = UIFactory.Panel(bg.transform, "Box", UIFactory.PanelBg2);
            box.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100, 800));
            var t = UIFactory.Label(box.transform, title, 36, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            t.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -12), new Vector2(900, 60));
            var x = UIFactory.Button(box.transform, "X", CloseModal, new Color(0.55f, 0.25f, 0.25f), 30);
            ((RectTransform)x.transform).Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-12, -12), new Vector2(70, 60));
            var list = UIFactory.ScrollList(box.transform, out _);
            ((RectTransform)list.parent).Anchors(Vector2.zero, Vector2.one, new Vector2(20, 20), new Vector2(-20, -90));
            fill(list);
            modal = bg.gameObject;
        }

        void CloseModal()
        {
            if (modal != null) Destroy(modal);
            modal = null;
        }

        void Confirm(string title, string body, Action yes)
        {
            ShowModal(title, list =>
            {
                Para(list, body, 28, Color.white);
                var row = UIFactory.HRow(list, 90, 20);
                UIFactory.Size(UIFactory.Button(row.transform, "YES", () => { CloseModal(); yes(); }, new Color(0.6f, 0.3f, 0.25f)), 90, 300);
                UIFactory.Size(UIFactory.Button(row.transform, "NO", CloseModal), 90, 300);
            });
        }
    }
}
