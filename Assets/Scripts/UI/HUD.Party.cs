using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>HUD widgets for equipment and kin: tool/weapon hotbar, KIN quick-swap bar, companion stance.</summary>
    public partial class HUD
    {
        // ---- hotbar ----
        readonly Image[] hotSlotBg = new Image[PlayerStats.HotbarSize];
        readonly Image[] hotSlotIcon = new Image[PlayerStats.HotbarSize];
        readonly Text[] hotSlotGlyph = new Text[PlayerStats.HotbarSize];
        readonly Text[] hotSlotName = new Text[PlayerStats.HotbarSize];
        Text autoToolLabel;
        Image autoToolBg;
        RectTransform hotbarRoot;

        // ---- kin button + quick panel ----
        Image kinBtnImg;
        Text kinBtnLabel;
        GameObject kinPanel;
        RectTransform kinSlotRow;
        readonly List<KinSlotView> kinSlots = new List<KinSlotView>();
        readonly Image[] stanceSegBg = new Image[3];
        float kinPanelIdle;

        // ---- run toggle ----
        Image runBg;
        Text runLabel;
        GameObject homeBtn;

        void BuildRunButton()
        {
            runBg = UIFactory.Panel(safe, "Btn_Run", new Color(0.1f, 0.12f, 0.16f, 0.6f), UIFactory.Circle);
            runBg.type = Image.Type.Simple;
            runBg.rectTransform.Place(Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(430, 120), new Vector2(116, 116));
            var tb = runBg.gameObject.AddComponent<TouchButton>();
            tb.button = Btn.SprintToggle; tb.image = runBg;
            tb.normal = new Color(0.1f, 0.12f, 0.16f, 0.6f); tb.pressed = new Color(0.3f, 0.7f, 0.45f, 0.85f);
            runLabel = UIFactory.Label(runBg.transform, "RUN", 24, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            runLabel.rectTransform.Stretch(6, 6, 6, 6);
            // simple buttons: you always run, so RUN's place is taken by HOME (back to camp from anywhere)
            var home = UIFactory.Panel(safe, "Btn_Home", new Color(0.25f, 0.55f, 0.4f, 0.85f), UIFactory.Circle);
            home.type = Image.Type.Simple;
            home.rectTransform.Place(Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(430, 120), new Vector2(124, 124));
            home.gameObject.AddComponent<Button>().onClick.AddListener(() => GameManager.I.GoHome());
            var hl = UIFactory.Label(home.transform, "HOME", 26, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            hl.rectTransform.Stretch(6, 6, 6, 6);
            homeBtn = home.gameObject;
            homeBtn.SetActive(false);
        }

        void UpdateRunButton(PlayerController pl)
        {
            var tb = runBg.GetComponent<TouchButton>();
            if (pl.Exhausted)
            {
                runLabel.text = "TIRED";
                tb.normal = new Color(0.45f, 0.2f, 0.2f, 0.7f);
            }
            else if (pl.SprintToggled)
            {
                runLabel.text = pl.IsMounted ? "GALLOP" : "RUNNING";
                tb.normal = new Color(0.25f, 0.62f, 0.4f, 0.9f);
            }
            else
            {
                runLabel.text = "RUN";
                tb.normal = new Color(0.1f, 0.12f, 0.16f, 0.6f);
            }
            if (!Input_IsDown(tb)) runBg.color = tb.normal;
        }

        static bool Input_IsDown(TouchButton tb) => tb.transform.localScale.x < 0.99f;

        // ---- ride button ----
        Image rideBg;
        Text rideLabel;

        // ---- stance pill on companion frame ----
        Text stancePillText;
        Image stancePillBg;

        class KinSlotView
        {
            public GameObject go;
            public Image bg, ring, hp;
            public Text glyph, info;
        }

        // ============================================================ HOTBAR
        void BuildHotbar()
        {
            const float slot = 104f, gap = 10f;
            float width = PlayerStats.HotbarSize * (slot + gap) + 120f;
            hotbarRoot = UIFactory.Rect(safe, "Hotbar").Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-40f, 18f), new Vector2(width, slot + 26f));
            for (int i = 0; i < PlayerStats.HotbarSize; i++)
            {
                int idx = i;
                var bg = UIFactory.Panel(hotbarRoot, "Hot" + i, new Color(0.08f, 0.1f, 0.14f, 0.72f));
                bg.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(i * (slot + gap), 0), new Vector2(slot, slot));
                var btn = bg.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() => OnHotbar(idx));
                var icon = UIFactory.Panel(bg.transform, "Icon", Color.white);
                icon.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(76, 76));
                icon.raycastTarget = false;
                var glyph = UIFactory.Label(icon.transform, "", 26, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                glyph.rectTransform.Stretch();
                var nm = UIFactory.Label(bg.transform, "", 15, Color.white, TextAnchor.LowerCenter);
                nm.rectTransform.Stretch(2, 3, 2, 0);
                var key = UIFactory.Label(bg.transform, (i + 1).ToString(), 16, new Color(1, 1, 1, 0.45f), TextAnchor.UpperLeft);
                key.rectTransform.Stretch(8, 0, 0, 4);
                hotSlotBg[i] = bg; hotSlotIcon[i] = icon; hotSlotGlyph[i] = glyph; hotSlotName[i] = nm;
            }
            // Auto-tool toggle chip
            autoToolBg = UIFactory.Panel(hotbarRoot, "AutoTool", new Color(0.2f, 0.5f, 0.42f, 0.85f));
            autoToolBg.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(PlayerStats.HotbarSize * (slot + gap), 0), new Vector2(110, slot));
            var ab = autoToolBg.gameObject.AddComponent<Button>();
            ab.onClick.AddListener(ToggleAutoTool);
            autoToolLabel = UIFactory.Label(autoToolBg.transform, "", 19, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            autoToolLabel.rectTransform.Stretch(4, 4, 4, 4);
        }

        void OnHotbar(int i)
        {
            var s = GameManager.I.Player.Stats;
            string id = s.hotbar[i];
            if (string.IsNullOrEmpty(id)) { GameEvents.Say("Empty slot - assign tools/weapons from MENU > Bag."); MenuUI.I.Open(MenuTab.Bag); return; }
            GameManager.I.Player.ToggleHold(id);
        }

        public void ToggleAutoTool()
        {
            var s = GameManager.I.Player.Stats;
            s.autoTool = !s.autoTool;
            GameEvents.Say(s.autoTool ? "Auto-tool ON: gathering uses your best tool automatically" : "Auto-tool OFF: only the tool in your hand is used",
                s.autoTool ? new Color(0.6f, 1f, 0.7f) : new Color(1f, 0.85f, 0.5f));
            SynthAudio.Play(Sfx.UiClick);
        }

        void UpdateHotbar(PlayerController pl)
        {
            var s = pl.Stats;
            string shown = pl.DisplayedItem;
            for (int i = 0; i < PlayerStats.HotbarSize; i++)
            {
                string id = s.hotbar[i];
                var d = string.IsNullOrEmpty(id) ? null : ContentDB.Item(id);
                bool held = d != null && s.held == id;
                bool auto = d != null && !held && shown == id;
                hotSlotBg[i].color = held ? new Color(0.25f, 0.6f, 0.48f, 0.95f) : (auto ? new Color(0.55f, 0.45f, 0.2f, 0.9f) : new Color(0.08f, 0.1f, 0.14f, 0.72f));
                hotSlotIcon[i].enabled = d != null;
                if (d != null)
                {
                    var spr = ItemIcons.Get(d.id);
                    hotSlotIcon[i].sprite = spr != null ? spr : UIFactory.Rounded;
                    hotSlotIcon[i].type = spr != null ? Image.Type.Simple : Image.Type.Sliced;
                    hotSlotIcon[i].preserveAspect = spr != null;
                    hotSlotIcon[i].color = spr != null ? Color.white : d.iconColor;
                    hotSlotGlyph[i].text = spr != null ? "" : d.iconGlyph;
                    hotSlotGlyph[i].color = UIFactory.Luma(d.iconColor) > 0.6f ? new Color(0.1f, 0.1f, 0.12f) : Color.white;
                    hotSlotName[i].text = held ? "IN HAND" : (auto ? "AUTO" : ShortName(d));
                }
                else { hotSlotGlyph[i].text = ""; hotSlotName[i].text = "+"; }
            }
            autoToolLabel.text = s.autoTool ? "AUTO\nTOOL\nON" : "AUTO\nTOOL\nOFF";
            autoToolBg.color = s.autoTool ? new Color(0.2f, 0.5f, 0.42f, 0.85f) : new Color(0.3f, 0.3f, 0.32f, 0.85f);
        }

        static string ShortName(ItemDef d)
        {
            string n = d.displayName;
            int sp = n.LastIndexOf(' ');
            return sp > 0 ? n.Substring(sp + 1) : n;
        }

        // ============================================================ KIN BUTTON + QUICK PANEL
        void BuildKinButton()
        {
            kinBtnImg = UIFactory.Panel(actionCluster, "Btn_KinPanel", new Color(0.3f, 0.5f, 0.85f, 0.85f), UIFactory.Circle);
            kinBtnImg.type = Image.Type.Simple;
            kinBtnImg.rectTransform.Place(new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(-560, 110), new Vector2(116, 116));
            var b = kinBtnImg.gameObject.AddComponent<Button>();
            b.onClick.AddListener(ToggleKinPanel);
            kinBtnLabel = UIFactory.Label(kinBtnImg.transform, "KIN", 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            kinBtnLabel.rectTransform.Stretch(6, 6, 6, 6);
            kinBtnLabel.supportRichText = true;
        }

        void BuildRideButton()
        {
            rideBg = UIFactory.Panel(actionCluster, "Btn_Ride", new Color(0.55f, 0.4f, 0.2f, 0.85f), UIFactory.Circle);
            rideBg.type = Image.Type.Simple;
            rideBg.rectTransform.Place(new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(-700, 220), new Vector2(104, 104));
            rideBg.gameObject.AddComponent<Button>().onClick.AddListener(() => GameManager.I.Player.ToggleRide());
            rideLabel = UIFactory.Label(rideBg.transform, "RIDE", 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            rideLabel.rectTransform.Stretch(6, 6, 6, 6);
        }

        void UpdateRideButton(PlayerController pl)
        {
            bool show = pl.IsMounted || pl.CompanionRideable;
            if (rideBg.gameObject.activeSelf != show) rideBg.gameObject.SetActive(show);
            if (!show) return;
            bool can = pl.IsMounted || pl.CanRide(out _);
            rideLabel.text = pl.IsMounted ? "DIS-\nMOUNT" : pl.RideLabel;
            rideBg.color = pl.IsMounted ? new Color(0.7f, 0.35f, 0.25f, 0.9f) : (can ? new Color(0.55f, 0.4f, 0.2f, 0.9f) : new Color(0.35f, 0.33f, 0.3f, 0.7f));
        }

        void BuildKinPanel()
        {
            var p = UIFactory.Panel(safe, "KinPanel", new Color(0.06f, 0.08f, 0.12f, 0.92f));
            p.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-40f, 175f), new Vector2(1000, 300));
            kinPanel = p.gameObject;

            var title = UIFactory.Label(p.transform, "PARTY  -  tap to swap your companion", 22, UIFactory.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold);
            title.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -6), new Vector2(620, 36));

            var all = UIFactory.Button(p.transform, "ALL KIN", () => { ToggleKinPanel(); MenuUI.I.Open(MenuTab.Kin); }, new Color(0.25f, 0.35f, 0.45f), 20);
            ((RectTransform)all.transform).Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-14, -8), new Vector2(150, 44));
            var close = UIFactory.Button(p.transform, "X", ToggleKinPanel, new Color(0.5f, 0.25f, 0.25f), 22);
            ((RectTransform)close.transform).Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-172, -8), new Vector2(56, 44));

            kinSlotRow = UIFactory.Rect(p.transform, "Slots").Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -52), new Vector2(964, 150));
            for (int i = 0; i < KinRoster.PartySize; i++)
            {
                int idx = i;
                var v = new KinSlotView();
                var bg = UIFactory.Panel(kinSlotRow, "Kin" + i, new Color(1, 1, 1, 0.07f));
                bg.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(i * 192f, 0), new Vector2(180, 150));
                bg.gameObject.AddComponent<Button>().onClick.AddListener(() => { GameManager.I.Party.SummonSlot(idx); kinPanelIdle = 0f; });
                v.ring = UIFactory.Panel(bg.transform, "Ring", new Color(1f, 0.85f, 0.35f), UIFactory.Ring);
                v.ring.type = Image.Type.Simple; v.ring.raycastTarget = false;
                v.ring.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(86, 86));
                v.bg = UIFactory.Panel(bg.transform, "Portrait", Color.gray, UIFactory.Circle);
                v.bg.type = Image.Type.Simple; v.bg.raycastTarget = false;
                v.bg.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(74, 74));
                v.glyph = UIFactory.Label(v.bg.transform, "", 28, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                v.glyph.rectTransform.Stretch();
                v.info = UIFactory.Label(bg.transform, "", 18, Color.white, TextAnchor.UpperCenter, FontStyle.Bold);
                v.info.supportRichText = true;
                v.info.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(176, 46));
                UIFactory.Bar(bg.transform, new Color(0.4f, 0.9f, 0.55f), out v.hp).rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(150, 12));
                v.go = bg.gameObject;
                kinSlots.Add(v);
            }

            // stance segmented control
            var seg = UIFactory.Rect(p.transform, "Stance").Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(18, 12), new Vector2(964, 70));
            var lbl = UIFactory.Label(seg, "BEHAVIOR", 20, UIFactory.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold);
            lbl.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(140, 60));
            for (int i = 0; i < 3; i++)
            {
                var st = (CompanionStance)i;
                var b = UIFactory.Button(seg, KinRoster.StanceName(st), () => { GameManager.I.Party.Stance = st; GameEvents.Say($"Companion: {KinRoster.StanceName(st)} - {KinRoster.StanceHint(st)}", KinRoster.StanceColor(st)); kinPanelIdle = 0f; }, UIFactory.ButtonCol, 22);
                ((RectTransform)b.transform).Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(150 + i * 272, 0), new Vector2(260, 62));
                stanceSegBg[i] = (Image)b.targetGraphic;
            }
            kinPanel.SetActive(false);
        }

        public void ToggleKinPanel()
        {
            bool on = !kinPanel.activeSelf;
            kinPanel.SetActive(on);
            kinPanelIdle = 0f;
            SynthAudio.Play(on ? Sfx.UiOpen : Sfx.UiClick, 1f, 0.6f);
        }

        void UpdateKinWidgets(GameManager gm)
        {
            var party = gm.Party;
            var comp = party.Companion;

            // KIN button: tinted by the element of whoever is out. It used to show that kin's initials ("Kn" for a
            // Knuckub), which read as a typo for "Kin" (user, 2026-10-10) - now it always says what it does.
            if (comp != null && comp.IsAlive)
            {
                kinBtnImg.color = ElementInfo.Color(comp.Data.Element1) * new Color(0.85f, 0.85f, 0.85f, 0.95f);
                kinBtnLabel.text = "KIN\n<size=15>SWAP</size>";
            }
            else
            {
                kinBtnImg.color = new Color(0.3f, 0.5f, 0.85f, 0.85f);
                kinBtnLabel.text = party.CountIn(KinLocation.Party) > 0 ? "KIN\n<size=15>" + party.CountIn(KinLocation.Party) + "/5</size>" : "KIN";
            }

            // stance pill
            var stance = party.Stance;
            stancePillText.text = KinRoster.StanceName(stance);
            stancePillBg.color = KinRoster.StanceColor(stance) * new Color(0.75f, 0.75f, 0.75f, 0.95f);

            if (!kinPanel.activeSelf) return;
            kinPanelIdle += Time.unscaledDeltaTime;
            if (kinPanelIdle > 8f) { kinPanel.SetActive(false); return; }

            var list = party.PartyList();
            for (int i = 0; i < kinSlots.Count; i++)
            {
                var v = kinSlots[i];
                if (i >= list.Count)
                {
                    v.bg.color = new Color(1, 1, 1, 0.12f);
                    v.glyph.text = "";
                    v.ring.enabled = false;
                    v.info.text = "<color=#888888>empty</color>";
                    v.hp.SetFill(0f);
                    continue;
                }
                var d = list[i];
                bool isOut = comp != null && comp.Data == d;
                float hp01 = isOut ? comp.Hp / comp.MaxHp : (d.Fainted ? 0f : d.CurrentHp / d.MaxHp);
                v.bg.color = d.Fainted && !isOut ? new Color(0.35f, 0.35f, 0.38f) : ElementInfo.Color(d.Element1);
                v.glyph.text = Initials(d);
                v.ring.enabled = isOut;
                string state = isOut ? "<color=#ffd75a>OUT</color>" : (d.Fainted ? $"<color=#ff7a6a>RESTING {Mathf.CeilToInt(party.ReviveIn(d))}s</color>" : $"Lv {d.level}");
                v.info.text = $"{Trim(d.DisplayName, 12)}\n<size=15>{state}</size>";
                v.hp.SetFill(hp01);
            }
            for (int i = 0; i < 3; i++)
                stanceSegBg[i].color = (int)stance == i ? KinRoster.StanceColor((CompanionStance)i) * new Color(0.7f, 0.7f, 0.7f, 1f) : UIFactory.ButtonCol;
        }

        void BuildStancePill(Transform compFramePanel)
        {
            stancePillBg = UIFactory.Panel(compFramePanel, "Stance", new Color(0.8f, 0.65f, 0.25f));
            stancePillBg.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(128, 60));
            stancePillBg.gameObject.AddComponent<Button>().onClick.AddListener(() => GameManager.I.Party.CycleStance());
            stancePillText = UIFactory.Label(stancePillBg.transform, "", 17, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            stancePillText.rectTransform.Stretch(4, 4, 4, 4);
        }

        static string Initials(CreatureData d) => d.Species.displayName.Substring(0, 2);
        static string Trim(string s, int n) => s.Length <= n ? s : s.Substring(0, n - 1) + ".";

        /// <summary>Hide/show the whole HUD (starter selection, cutscenes).</summary>
        public void SetVisible(bool on) => safe.gameObject.SetActive(on);
    }
}
