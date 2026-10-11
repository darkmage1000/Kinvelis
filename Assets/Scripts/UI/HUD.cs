using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>
    /// Mobile HUD built in code: vitals, minimap, target frame with live catch %, toasts, damage numbers,
    /// floating joystick, look pad, and a thumb-reachable action cluster.
    /// </summary>
    public partial class HUD : MonoBehaviour
    {
        public static HUD I { get; private set; }

        RectTransform root, safe;
        Image hpFill, staFill, hungerFill, xpFill;
        Text levelText, hpText;
        RawImage minimap;
        RectTransform minimapArrow, markerLayer;
        Text locationText;
        // target
        RectTransform targetFrame;
        Text targetName, targetInfo, targetCatch;
        Image targetHp;
        // companion
        RectTransform compFrame;
        Text compName;
        Image compHp, compCd;
        // buttons
        RectTransform actionCluster, placeCluster, deathPanel;
        Text interactLabel, throwLabel, placeReason, sneakLabel, attackLabel;
        GameObject interactBtn, kinSkillBtn, hookBtn;
        // lock-on: the LOCK button and the previous / next arrows either side of the target frame
        Text lockLabel;
        GameObject lockPrev, lockNext;
        Text jumpLabel;
        Text fpsText;
        // toasts
        readonly List<(Text t, float life)> toasts = new List<(Text, float)>();
        RectTransform toastRoot;
        readonly List<(Text t, float life)> feed = new List<(Text, float)>();
        RectTransform feedRoot;
        // damage numbers
        struct Dmg { public Text t; public Vector3 world; public float life, scale; }
        readonly List<Dmg> dmg = new List<Dmg>();
        readonly Stack<Text> dmgPool = new Stack<Text>();
        RectTransform dmgRoot;
        readonly List<RectTransform> markers = new List<RectTransform>();
        readonly List<Vector3> fishDots = new List<Vector3>();
        readonly List<Image> markerImages = new List<Image>();
        public bool ShowFps;
        float fpsAcc; int fpsFrames;

        Canvas canvas;

        /// <summary>Photo mode: the HUD turns invisible but every control still answers.</summary>
        public void SetFaded(bool faded)
        {
            if (safe == null) return;
            var g = safe.GetComponent<CanvasGroup>();
            if (g == null) g = safe.gameObject.AddComponent<CanvasGroup>();   // (not ??: a missing built-in component is a fake null in the editor)
            g.alpha = faded ? 0f : 1f;
            // the quest goal box is GameManager's, a sibling of the safe area
            var goal = root != null ? root.Find("Goal") : null;
            if (goal != null)
            {
                var gg = goal.GetComponent<CanvasGroup>();
                if (gg == null) gg = goal.gameObject.AddComponent<CanvasGroup>();
                gg.alpha = faded ? 0f : 1f;
            }
        }

        public void Build(Canvas c)
        {
            I = this;
            canvas = c;
            root = (RectTransform)c.transform;
            safe = UIFactory.Rect(root, "SafeArea");
            safe.Stretch();
            ApplySafeArea();

            // look pad covers everything; UI on top swallows its own touches
            var look = UIFactory.Panel(safe, "LookPad", new Color(0, 0, 0, 0));
            look.rectTransform.Stretch();
            look.gameObject.AddComponent<LookPad>();

            BuildJoystick();
            BuildVitals();
            BuildMinimap();
            BuildTarget();
            BuildBoss();
            BuildCompanion();
            BuildActions();
            BuildHotbar();
            BuildRunButton();
            BuildRideButton();
            BuildKinPanel();
            BuildPlacementUI();
            BuildDeath();

            dmgRoot = UIFactory.Rect(safe, "DamageNumbers"); dmgRoot.Stretch();
            toastRoot = UIFactory.Rect(safe, "Toasts").Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -230), new Vector2(1000, 300));
            feedRoot = UIFactory.Rect(safe, "Feed").Place(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30, 150), new Vector2(420, 300));
            fpsText = UIFactory.Label(safe, "", 24, Color.white);
            fpsText.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 8), new Vector2(400, 30));
            fpsText.alignment = TextAnchor.LowerCenter;

            GameEvents.Toast += Toast;
        }

        void OnDestroy() { GameEvents.Toast -= Toast; }

        void ApplySafeArea()
        {
            var sa = Screen.safeArea;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            safe.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
            safe.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
            safe.offsetMin = safe.offsetMax = Vector2.zero;
        }

        // ============================================================ BUILD
        void BuildJoystick()
        {
            var zone = UIFactory.Panel(safe, "JoystickZone", new Color(0, 0, 0, 0));
            zone.rectTransform.Anchors(new Vector2(0, 0), new Vector2(0.3f, 0.62f));
            var js = zone.gameObject.AddComponent<VirtualJoystick>();
            var ring = UIFactory.Panel(zone.transform, "Ring", new Color(1, 1, 1, 0.22f), UIFactory.Ring);
            ring.type = Image.Type.Simple;
            ring.raycastTarget = false;
            ring.rectTransform.Place(Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(230, 220), new Vector2(240, 240));
            var knob = UIFactory.Panel(zone.transform, "Knob", new Color(1, 1, 1, 0.45f), UIFactory.Circle);
            knob.type = Image.Type.Simple;
            knob.raycastTarget = false;
            knob.rectTransform.Place(Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(230, 220), new Vector2(110, 110));
            js.baseRing = ring.rectTransform;
            js.knob = knob.rectTransform;
            js.radius = 105f;
        }

        Text vitalsTip;
        string vitalsTipLast;
        void UpdateVitalsTip(PlayerController pl)
        {
            var parts = new List<string>();
            if (pl.Status.Active) parts.Add(UIFactory.Col($"{pl.Status.type.ToString().ToUpperInvariant()} {Mathf.CeilToInt(pl.Status.time)}s", StatusState.Color(pl.Status.type)));
            if (pl.ColdExposure > 0.01f) parts.Add(UIFactory.Col(pl.ColdExposure > 0.6f ? "FREEZING" : "COLD", new Color(0.6f, 0.9f, 1f)));
            if (pl.HeatExposure > 0.01f) parts.Add(UIFactory.Col(pl.HeatExposure > 0.6f ? "SCORCHING" : "HOT", new Color(1f, 0.6f, 0.3f)));
            if (pl.Stats.MealActive)
            {
                int left = Mathf.CeilToInt(pl.Stats.MealSecondsLeft);
                parts.Add(UIFactory.Col($"{ContentDB.ItemName(pl.Stats.mealItem)} {left / 60}:{left % 60:00}", new Color(1f, 0.85f, 0.45f)));
            }
            string t = parts.Count > 0 ? string.Join("   ", parts) : "HP   ·   STAMINA   ·   FOOD";
            if (t != vitalsTipLast) { vitalsTipLast = t; vitalsTip.text = t; }
        }

        void BuildVitals()
        {
            var p = UIFactory.Panel(safe, "Vitals", UIFactory.PanelBg);
            p.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -20), new Vector2(470, 176));
            var lvl = UIFactory.Panel(p.transform, "Lvl", new Color(0.3f, 0.75f, 0.5f), UIFactory.Circle);
            lvl.type = Image.Type.Simple;
            lvl.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -14), new Vector2(84, 84));
            levelText = UIFactory.Label(lvl.transform, "1", 36, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            levelText.rectTransform.Stretch();

            UIFactory.Bar(p.transform, new Color(0.95f, 0.35f, 0.4f), out hpFill).rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(110, -16), new Vector2(344, 34));
            hpText = UIFactory.Label(hpFill.transform.parent, "", 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            hpText.rectTransform.Stretch();
            UIFactory.Bar(p.transform, new Color(0.35f, 0.85f, 0.45f), out staFill).rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(110, -58), new Vector2(344, 20));
            UIFactory.Bar(p.transform, new Color(1f, 0.7f, 0.3f), out hungerFill).rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(110, -86), new Vector2(344, 16));
            UIFactory.Bar(p.transform, new Color(0.5f, 0.75f, 1f), out xpFill).rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -110), new Vector2(440, 10));
            var tip = vitalsTip = UIFactory.Label(p.transform, "HP   ·   STAMINA   ·   FOOD", 18, UIFactory.TextDim);
            tip.supportRichText = true;
            tip.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -126), new Vector2(440, 40));
        }

        void BuildMinimap()
        {
            var frame = UIFactory.Panel(safe, "MinimapFrame", UIFactory.PanelBg);
            frame.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -20), new Vector2(270, 330));
            var mask = UIFactory.Panel(frame.transform, "Mask", Color.white, UIFactory.Circle);
            mask.type = Image.Type.Simple;
            mask.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(250, 250));
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var mmGo = UIFactory.Rect(mask.transform, "Minimap");
            mmGo.Stretch();
            minimap = mmGo.gameObject.AddComponent<RawImage>();
            minimap.texture = MapTexture.Texture;
            minimap.raycastTarget = false;
            markerLayer = UIFactory.Rect(mask.transform, "Markers");
            markerLayer.Stretch();
            var arrow = UIFactory.Label(mask.transform, "▲", 34, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            arrow.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 40));
            minimapArrow = arrow.rectTransform;
            var btn = mask.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(() => MenuUI.I.Open(MenuTab.Map));
            locationText = UIFactory.Label(frame.transform, "", 20, Color.white, TextAnchor.MiddleCenter);
            locationText.supportRichText = true;
            locationText.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(260, 64));

            var menu = UIFactory.Button(safe, "MENU", () => MenuUI.I.Toggle(), new Color(0.2f, 0.45f, 0.4f, 0.95f), 28);
            ((RectTransform)menu.transform).Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-310, -20), new Vector2(150, 76));
            var build = UIFactory.Button(safe, "BUILD", () => MenuUI.I.Open(MenuTab.Build), new Color(0.45f, 0.35f, 0.2f, 0.95f), 28);
            ((RectTransform)build.transform).Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-310, -106), new Vector2(150, 76));
            campBtn = UIFactory.Button(safe, "CAMP", () => MenuUI.I.OpenCamp(CampTab.Overview), new Color(0.3f, 0.42f, 0.55f, 0.95f), 28);
            ((RectTransform)campBtn.transform).Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-310, -192), new Vector2(150, 76));
            campBtn.gameObject.SetActive(false);
        }

        Button campBtn;
        float campCheck;
        void UpdateCampButton(PlayerController pl)
        {
            campCheck -= Time.unscaledDeltaTime;
            if (campCheck > 0f) return;
            campCheck = 0.5f;
            var bm = BaseManager.I;
            bool show = bm != null && bm.HasBase && bm.IsInsideBase(pl.Position, 0f);
            if (campBtn.gameObject.activeSelf != show) campBtn.gameObject.SetActive(show);
            if (!show) return;
            bool severe = bm.Alerts().Exists(x => x.severe);
            campBtn.image.color = severe ? new Color(0.7f, 0.3f, 0.25f, 0.95f) : new Color(0.3f, 0.42f, 0.55f, 0.95f);
            campBtn.GetComponentInChildren<Text>().text = severe ? "CAMP (!)" : "CAMP";
        }

        RectTransform bossFrame;
        Text bossName;
        Image bossHp;

        void BuildBoss()
        {
            var f = UIFactory.Panel(safe, "Boss", new Color(0.05f, 0.04f, 0.08f, 0.82f));
            bossFrame = f.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -178), new Vector2(760, 74));
            bossName = UIFactory.Label(f.transform, "", 24, Color.white, TextAnchor.UpperCenter, FontStyle.Bold);
            bossName.supportRichText = true;
            bossName.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(740, 32));
            UIFactory.Bar(f.transform, new Color(0.9f, 0.3f, 0.5f), out bossHp).rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -42), new Vector2(730, 22));
            bossFrame.gameObject.SetActive(false);
        }

        /// <summary>Warden health bar under the target frame; a null label hides it.</summary>
        public void SetBoss(string label, float frac, Color color)
        {
            bool on = label != null;
            if (bossFrame.gameObject.activeSelf != on) bossFrame.gameObject.SetActive(on);
            if (!on) return;
            if (bossName.text != label) bossName.text = label;
            bossHp.color = color;
            bossHp.SetFill(Mathf.Clamp01(frac));
        }

        void BuildTarget()
        {
            var f = UIFactory.Panel(safe, "Target", UIFactory.PanelBg);
            targetFrame = f.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(600, 150));
            targetName = UIFactory.Label(f.transform, "", 30, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            targetName.supportRichText = true;
            targetName.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -10), new Vector2(565, 40));
            UIFactory.Bar(f.transform, new Color(0.95f, 0.4f, 0.35f), out targetHp).rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -54), new Vector2(565, 22));
            targetInfo = UIFactory.Label(f.transform, "", 20, UIFactory.TextDim, TextAnchor.UpperLeft);
            targetInfo.supportRichText = true;
            targetInfo.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -82), new Vector2(565, 30));
            targetCatch = UIFactory.Label(f.transform, "", 22, new Color(0.6f, 1f, 0.6f), TextAnchor.UpperLeft, FontStyle.Bold);
            targetCatch.supportRichText = true;
            targetCatch.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -112), new Vector2(565, 30));
            // while locked on: step to the kin on the left / right
            var prev = UIFactory.Button(f.transform, "<", () => GameManager.I.Player.CycleLock(-1), new Color(0.8f, 0.58f, 0.2f, 0.9f), 44);
            ((RectTransform)prev.transform).Place(new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(92, 124));
            var next = UIFactory.Button(f.transform, ">", () => GameManager.I.Player.CycleLock(1), new Color(0.8f, 0.58f, 0.2f, 0.9f), 44);
            ((RectTransform)next.transform).Place(new Vector2(1, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(92, 124));
            lockPrev = prev.gameObject; lockNext = next.gameObject;
            lockPrev.SetActive(false); lockNext.SetActive(false);
            targetFrame.gameObject.SetActive(false);
        }

        void BuildCompanion()
        {
            var f = UIFactory.Panel(safe, "Companion", UIFactory.PanelBg);
            compFrame = f.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -210), new Vector2(470, 86));
            compName = UIFactory.Label(f.transform, "", 22, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            compName.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -8), new Vector2(340, 30));
            UIFactory.Bar(f.transform, new Color(0.4f, 0.9f, 0.55f), out compHp).rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -42), new Vector2(330, 16));
            UIFactory.Bar(f.transform, new Color(0.6f, 0.75f, 1f), out compCd).rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -64), new Vector2(310, 10));
            compName.rectTransform.sizeDelta = new Vector2(310, 30);
            compHp.transform.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(310, 16);
            BuildStancePill(f.transform);
            compFrame.gameObject.SetActive(false);
        }

        bool simpleShown;

        Button RoundButton(Transform parent, string label, Btn b, Vector2 pos, float size, Color col, out Text text, int font = 26)
        {
            var img = UIFactory.Panel(parent, "Btn_" + b, col, UIFactory.Circle);
            img.type = Image.Type.Simple;
            img.rectTransform.Place(new Vector2(1, 0), new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
            var tb = img.gameObject.AddComponent<TouchButton>();
            tb.button = b; tb.image = img; tb.normal = col; tb.pressed = new Color(col.r * 1.4f, col.g * 1.4f, col.b * 1.4f, Mathf.Min(1f, col.a + 0.25f));
            text = UIFactory.Label(img.transform, label, font, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            text.rectTransform.Stretch(6, 6, 6, 6);
            text.resizeTextForBestFit = true; text.resizeTextMinSize = 12; text.resizeTextMaxSize = font;
            return null;
        }

        void BuildActions()
        {
            actionCluster = UIFactory.Rect(safe, "Actions");
            actionCluster.Stretch();
            var dark = new Color(0.1f, 0.12f, 0.16f, 0.6f);
            RoundButton(actionCluster, "ATTACK", Btn.Attack, new Vector2(-200, 200), 200, new Color(0.85f, 0.35f, 0.3f, 0.75f), out attackLabel, 30);
            RoundButton(actionCluster, "SEED", Btn.Throw, new Vector2(-400, 150), 130, new Color(0.35f, 0.75f, 0.35f, 0.75f), out throwLabel, 22);
            RoundButton(actionCluster, "JUMP", Btn.Jump, new Vector2(-110, 420), 120, dark, out jumpLabel, 22);
            RoundButton(actionCluster, "HOOK", Btn.Grapple, new Vector2(-700, 350), 96, new Color(0.35f, 0.6f, 0.3f, 0.8f), out _, 20);
            hookBtn = actionCluster.Find("Btn_Grapple").gameObject;
            RoundButton(actionCluster, "LOCK", Btn.Lock, new Vector2(-545, 250), 104, new Color(0.8f, 0.58f, 0.2f, 0.75f), out lockLabel, 20);
            RoundButton(actionCluster, "ROLL", Btn.Dodge, new Vector2(-250, 400), 110, dark, out _, 22);
            BuildKinButton();
            BuildSkillBar();
            kinSkillBtn = skillBar.gameObject;
            RoundButton(actionCluster, "SNEAK", Btn.Sneak, new Vector2(-395, 300), 96, dark, out sneakLabel, 18);
            RoundButton(actionCluster, "USE", Btn.Interact, new Vector2(-200, 380), 0, new Color(0.9f, 0.75f, 0.3f, 0.85f), out interactLabel, 22);
            interactBtn = actionCluster.Find("Btn_Interact").gameObject;
            ((RectTransform)interactBtn.transform).Place(new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(-420, 450), new Vector2(230, 84));
            var ib = interactBtn.GetComponent<Image>(); ib.sprite = UIFactory.Rounded; ib.type = Image.Type.Sliced;
        }

        void BuildPlacementUI()
        {
            placeCluster = UIFactory.Rect(safe, "Placement");
            placeCluster.Stretch();
            RoundButton(placeCluster, "PLACE", Btn.Confirm, new Vector2(-200, 200), 190, new Color(0.3f, 0.75f, 0.4f, 0.85f), out _, 30);
            RoundButton(placeCluster, "ROTATE", Btn.Rotate, new Vector2(-400, 170), 130, new Color(0.3f, 0.45f, 0.8f, 0.85f), out _, 22);
            RoundButton(placeCluster, "CANCEL", Btn.Cancel, new Vector2(-150, 400), 120, new Color(0.7f, 0.3f, 0.3f, 0.85f), out _, 20);
            placeReason = UIFactory.Label(placeCluster, "", 30, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            placeReason.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 140), new Vector2(900, 50));
            placeCluster.gameObject.SetActive(false);
        }

        void BuildDeath()
        {
            var p = UIFactory.Panel(safe, "Death", new Color(0.05f, 0.02f, 0.05f, 0.7f));
            p.rectTransform.Stretch(-200, -200, -200, -200);
            var t = UIFactory.Label(p.transform, "You collapsed...\nYour kin carry you back to safety.", 44, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.rectTransform.Stretch();
            deathPanel = p.rectTransform;
            deathPanel.gameObject.SetActive(false);
        }

        public void ShowDeath(bool on) => deathPanel.gameObject.SetActive(on);

        // ============================================================ UPDATE
        public void Tick(float dt)
        {
            var gm = GameManager.I;
            var pl = gm.Player;
            var s = pl.Stats;
            hpFill.SetFill(s.hp / s.MaxHp);
            hpText.text = $"{Mathf.CeilToInt(s.hp)} / {Mathf.CeilToInt(s.MaxHp)}";
            staFill.SetFill(s.stamina / s.MaxStamina);
            hungerFill.SetFill(s.hunger / 100f);
            hungerFill.color = s.hunger < 20f ? new Color(1f, 0.3f, 0.2f) : new Color(1f, 0.7f, 0.3f);
            UpdateVitalsTip(pl);
            xpFill.SetFill(s.xp / (float)PlayerStats.XpToNext(s.level));
            levelText.text = s.level.ToString();

            UpdateMinimap(pl);
            UpdateTarget(pl);
            UpdateCompanion(gm);
            UpdateActions(gm, pl);
            UpdateHotbar(pl);
            UpdateKinWidgets(gm);
            UpdateRideButton(pl);
            UpdateCampButton(pl);
            UpdateRunButton(pl);
            UpdateToasts(dt);
            UpdateDamageNumbers(dt, gm.CameraRig.Cam);
            UpdateAimLabel(pl, gm.CameraRig.Cam);

            if (ShowFps)
            {
                fpsAcc += Time.unscaledDeltaTime; fpsFrames++;
                if (fpsAcc > 0.5f)
                {
                    fpsText.text = $"{fpsFrames / fpsAcc:0} fps · chunks {ChunkManager.I.LoadedChunks} · kin {CreatureRegistry.All.Count} · {QualityProfile.Current.tier}";
                    fpsAcc = 0; fpsFrames = 0;
                }
            }
            else if (fpsText.text.Length > 0) fpsText.text = "";
        }

        void UpdateMinimap(PlayerController pl)
        {
            if (Caves.InCave && Caves.I.MapTexture != null)
            {
                // underground: the cave's own layout (boss chamber in red)
                if (minimap.texture != Caves.I.MapTexture) minimap.texture = Caves.I.MapTexture;
                Vector2 cuv = Caves.ZoneUV(pl.Position);
                float cw = 120f / Caves.ZoneSize;
                minimap.uvRect = new Rect(cuv.x - cw * 0.5f, cuv.y - cw * 0.5f, cw, cw);
                minimapArrow.localRotation = Quaternion.Euler(0, 0, -pl.transform.eulerAngles.y);
                locationText.text = $"{Caves.I.CurrentName}\nUnderground";
                foreach (var m in markers) if (m.gameObject.activeSelf) m.gameObject.SetActive(false);
                return;
            }
            if (minimap.texture != MapTexture.Texture) minimap.texture = MapTexture.Texture;
            const float range = 360f; // meters shown across
            Vector2 uv = MapTexture.WorldToUV(pl.Position);
            float w = range / (WorldGen.HalfWorld * 2f);
            minimap.uvRect = new Rect(uv.x - w * 0.5f, uv.y - w * 0.5f, w, w);
            minimapArrow.localRotation = Quaternion.Euler(0, 0, -pl.transform.eulerAngles.y);
            var env = EnvironmentSystem.I;
            var sub = SubRegions.CurrentRegion;
            string place = env == null ? "" : sub != null ? $"<color=#{ColorUtility.ToHtmlStringRGB(Color.Lerp(sub.tint, Color.white, 0.55f))}>{sub.name}</color>" : BiomeInfo.Name(env.CurrentBiome);
            locationText.text = env != null ? $"{place}\n{env.ClockString()} · {EnvironmentSystem.WeatherName(env.Weather)}" : "";
            string nightLine = Nightfall.HudLine;
            if (nightLine != null) locationText.text += $"\n<color=#A9C8FF>{nightLine}</color>";
            string tide = Tides.Label(pl.Position);
            if (tide != null) locationText.text += $"\n<color=#8FE3FF>{tide}</color>";
            var we = WorldEvents.I;
            if (we != null && we.Active != WorldEventType.None)
                locationText.text += $"\n<color=#FFD866>{WorldEvents.Title(we.Active)} {Mathf.FloorToInt(we.TimeLeft / 60f)}:{Mathf.FloorToInt(we.TimeLeft % 60f):00}</color>";

            // markers: each kind of place has its own picture (MapIcons). Later ones draw on top.
            int mi = 0;
            float px = 250f / range;
            var mst = pl.Stats;
            foreach (var poi in Pois.All)
                if (poi.kind != PoiKind.Lookout && mst.poisFound.Contains(poi.id))
                    PlaceMarker(ref mi, poi.pos - pl.Position, px, Pois.Done(poi) ? new Color(0.6f, 0.6f, 0.56f) : new Color(1f, 0.9f, 0.55f), Pois.Icon(poi.kind));
            if (Relics.I != null && Relics.I.Ready && mst.relicHints.Count > 0)
                foreach (var rl in Relics.I.All)
                    if (mst.relicHints.Contains(rl.id) && !mst.relicsFound.Contains(rl.id)) PlaceMarker(ref mi, rl.pos - pl.Position, px, Color.white, MapIcon.Relic);
            foreach (var l in Landmarks.List) PlaceMarker(ref mi, l.pos - pl.Position, px, l.color, l.icon);
            if (Fishing.I != null && pl.Stats.Bag.Count(Fishing.Rod) > 0)
            {
                // with a rod in the bag, nearby fishing spots show as small blue dots
                fishDots.Clear();
                Fishing.I.Nearby(fishDots);
                foreach (var fp in fishDots)
                {
                    Vector3 fd = fp - pl.Position;
                    PlaceMarker(ref mi, fd, px, new Color(0.55f, 0.9f, 1f), MapIcon.Fish, false, 0f);
                }
            }
            if (Nests.I != null && Nests.I.Marker.HasValue) PlaceMarker(ref mi, Nests.I.Marker.Value - pl.Position, px, new Color(1f, 0.85f, 0.92f), MapIcon.Nest);
            if (Waystones.I != null)
                foreach (var ws in Waystones.I.All)
                {
                    if (!Waystones.I.IsFound(ws)) continue;
                    PlaceMarker(ref mi, ws.pos - pl.Position, px, Waystones.I.IsActive(ws) ? new Color(0.4f, 0.95f, 1f) : new Color(0.6f, 0.65f, 0.7f), MapIcon.Waystone);
                }
            if (Caves.I != null)
                foreach (var cv in Caves.I.Entrances)
                    if (Caves.I.IsFound(cv)) PlaceMarker(ref mi, cv.pos - pl.Position, px, new Color(0.8f, 0.6f, 1f), cv.Icon);
            if (Trader.I != null && Trader.I.Present) PlaceMarker(ref mi, Trader.I.Position - pl.Position, px, new Color(1f, 0.8f, 0.3f), MapIcon.Trader, true);
            if (WorldEvents.I != null) foreach (var cp in WorldEvents.I.CraterPositions()) PlaceMarker(ref mi, cp - pl.Position, px, new Color(0.65f, 0.85f, 1f), MapIcon.Crater, true);
            if (BaseManager.I.HasBase) PlaceMarker(ref mi, BaseManager.I.CorePosition - pl.Position, px, new Color(0.4f, 1f, 0.6f), MapIcon.Camp, true, float.MaxValue);
            // where the tracked quest wants you: always on the rim, however far
            var questAt = Quests.Marker;
            if (questAt.HasValue) PlaceMarker(ref mi, questAt.Value - pl.Position, px, Quests.Gold, MapIcon.Quest, true, float.MaxValue);
            for (int i = mi; i < markers.Count; i++) markers[i].gameObject.SetActive(false);
        }

        /// <summary>How far away a place still shows as a small faded picture on the minimap's rim (your camp always does).</summary>
        const float RimRange = 650f;

        /// <summary>
        /// A place on the minimap. Inside the disc it is drawn full size; outside, a small faded copy sits on the rim
        /// in its direction while it is within rimRange metres, and beyond that it is left off so the rim stays readable.
        /// </summary>
        void PlaceMarker(ref int i, Vector3 delta, float pxPerM, Color c, MapIcon icon, bool big = false, float rimRange = RimRange)
        {
            Vector2 p = new Vector2(delta.x, delta.z) * pxPerM;
            bool rim = p.sqrMagnitude > 112f * 112f;
            if (rim)
            {
                if (delta.x * delta.x + delta.z * delta.z > rimRange * rimRange) return;
                p = p.normalized * 114f;
            }
            if (i >= markers.Count)
            {
                var img = UIFactory.Panel(markerLayer, "Marker", c, MapIcons.Get(icon));
                img.type = Image.Type.Simple;
                img.preserveAspect = true;
                img.raycastTarget = false;
                img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                markers.Add(img.rectTransform);
                markerImages.Add(img);
            }
            var rt = markers[i];
            var im = markerImages[i++];
            rt.gameObject.SetActive(true);
            rt.anchoredPosition = p;
            float size = rim ? (big ? 20f : 16f) : (big ? 34f : 27f);
            rt.sizeDelta = new Vector2(size, size);
            im.sprite = MapIcons.Get(icon);
            c.a = rim ? 0.7f : 1f;
            im.color = c;
        }

        void UpdateTarget(PlayerController pl)
        {
            var t = pl.CombatTarget;
            bool on = t != null && t.IsAlive;
            bool locked = on && pl.LockTarget == t;
            if (lockLabel != null) lockLabel.text = pl.LockTarget != null ? "UN\nLOCK" : "LOCK";
            if (targetFrame.gameObject.activeSelf != on) targetFrame.gameObject.SetActive(on);
            if (!on) return;
            if (lockPrev.activeSelf != locked) { lockPrev.SetActive(locked); lockNext.SetActive(locked); }
            var d = t.Data;
            var sp = t.Sp;
            string el = UIFactory.Col(d.Element1.ToString(), ElementInfo.Color(d.Element1));
            if (d.Element2 != Element.Normal) el += " / " + UIFactory.Col(d.Element2.ToString(), ElementInfo.Color(d.Element2));
            string name = d.RegionalName;
            if (d.alpha) name = UIFactory.Col("ALPHA ", new Color(1f, 0.75f, 0.3f)) + name;
            if (d.shimmer) name = UIFactory.Col("* ", new Color(1f, 0.95f, 0.5f)) + name;
            if (locked) name = UIFactory.Col("LOCKED  ", new Color(1f, 0.78f, 0.25f)) + name;
            bool owned = pl.Stats.registeredSpecies.Contains(sp.id);
            bool danger = t.Mode == CreatureMode.Wild && DangerSkull.IsDanger(d);
            string lvText = danger ? UIFactory.Col($"Lv {d.level}  DANGER", new Color(1f, 0.3f, 0.25f)) : $"Lv {d.level}";
            targetName.text = $"{name}  <size=24>{lvText}</size>{(owned ? "" : UIFactory.Col("  NEW", new Color(0.5f, 1f, 1f)))}";
            targetHp.SetFill(t.Hp / t.MaxHp);
            string status = t.Status.Active ? "  ·  " + UIFactory.Col(t.Status.type.ToString(), StatusState.Color(t.Status.type)) : "";
            string rar = UIFactory.Col(sp.rarity.ToString(), RarityInfo.Color(sp.rarity));
            targetInfo.text = $"{el}  ·  {rar}  ·  {sp.temperament}{status}";
            var seed = GameManager.I.Capture.SelectedSeed(pl.Stats);
            if (seed != null)
            {
                float c = CaptureSystem.Chance(t, seed, pl.Stats, t.Unaware);
                Color cc = c > 0.6f ? new Color(0.5f, 1f, 0.5f) : (c > 0.3f ? new Color(1f, 0.9f, 0.4f) : new Color(1f, 0.5f, 0.4f));
                targetCatch.text = $"Catch: {UIFactory.Col($"{c * 100f:0}%", cc)}  <size=18>({seed.displayName}{(t.Unaware ? ", unaware bonus" : "")})</size>";
            }
            else targetCatch.text = UIFactory.Col("No seeds - craft Tether Seeds", new Color(1f, 0.6f, 0.5f));
        }

        void UpdateCompanion(GameManager gm)
        {
            var c = gm.Party.Companion;
            bool on = c != null && c.IsAlive;
            if (compFrame.gameObject.activeSelf != on) compFrame.gameObject.SetActive(on);
            kinSkillBtn.SetActive(on && !WorldRules.Current.simpleControls);   // simple buttons: kin pick their own skills
            if (!on) return;
            // in camp the name line says what it has gone off to do
            compName.supportRichText = true;
            compName.text = c.CampHelpStatus != null ? $"{c.Data.DisplayName}  <size=17>{UIFactory.Col(c.CampHelpStatus, new Color(0.7f, 1f, 0.8f))}</size>" : $"{c.Data.DisplayName}  Lv {c.Data.level}";
            compHp.SetFill(c.Hp / c.MaxHp);
            compCd.SetFill(Mathf.Max(c.AbilityCooldown01(0), c.AbilityCooldown01(1)));
            UpdateSkillBar(c);
        }

        // ============================================================ KIN SKILL BAR
        class SkillChip { public Image bg, cd; public Text name, timer; public string shownId; }
        RectTransform skillBar;
        readonly SkillChip[] chips = new SkillChip[CreatureData.MaxSkills];

        /// <summary>One button per kin skill (up to 4): name, element color, cooldown fill + countdown; locked slots show their unlock level.</summary>
        void BuildSkillBar()
        {
            skillBar = UIFactory.Rect(actionCluster, "SkillBar");
            skillBar.Stretch();
            for (int i = 0; i < chips.Length; i++)
            {
                int slot = i;
                var bg = UIFactory.Panel(skillBar, "Skill" + i, new Color(0.3f, 0.3f, 0.35f, 0.8f));
                bg.rectTransform.Place(new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(-175 - i * 152, 560), new Vector2(142, 84));
                bg.gameObject.AddComponent<Button>().onClick.AddListener(() => GameManager.I.UseKinSkill(slot));
                var cd = UIFactory.Panel(bg.transform, "Cooldown", new Color(0f, 0f, 0f, 0.62f));
                cd.sprite = null;
                cd.type = Image.Type.Filled;
                cd.fillMethod = Image.FillMethod.Horizontal;
                cd.fillOrigin = (int)Image.OriginHorizontal.Right;
                cd.rectTransform.Stretch();
                cd.raycastTarget = false;
                var name = UIFactory.Label(bg.transform, "", 19, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                name.rectTransform.Stretch(6, 30, 6, 4);
                name.resizeTextForBestFit = true; name.resizeTextMinSize = 12; name.resizeTextMaxSize = 19;
                name.raycastTarget = false;
                var timer = UIFactory.Label(bg.transform, "", 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                timer.rectTransform.Stretch(6, 4, 6, 44);
                timer.raycastTarget = false;
                chips[i] = new SkillChip { bg = bg, cd = cd, name = name, timer = timer };
            }
        }

        void UpdateSkillBar(CreatureActor c)
        {
            for (int i = 0; i < chips.Length; i++)
            {
                var ch = chips[i];
                var a = c.Skill(i);
                if (a == null)
                {
                    // locked slot
                    if (ch.shownId != "locked")
                    {
                        ch.shownId = "locked";
                        ch.bg.color = new Color(0.18f, 0.19f, 0.22f, 0.55f);
                        ch.name.text = "LOCKED";
                        ch.name.color = new Color(1f, 1f, 1f, 0.45f);
                    }
                    ch.timer.text = $"Lv {CreatureData.SlotUnlockLevel(i)}";
                    ch.timer.color = new Color(1f, 1f, 1f, 0.45f);
                    ch.cd.fillAmount = 0f;
                    continue;
                }
                if (ch.shownId != a.id)
                {
                    ch.shownId = a.id;
                    var col = ElementInfo.Color(a.element);
                    ch.bg.color = new Color(col.r * 0.75f, col.g * 0.75f, col.b * 0.75f, 0.88f);
                    ch.name.text = a.displayName.ToUpperInvariant();
                    ch.name.color = UIFactory.Luma(col) > 0.75f ? new Color(0.1f, 0.1f, 0.12f) : Color.white;
                }
                float left = c.SkillCooldownLeft(i);
                ch.cd.fillAmount = a.cooldown > 0f ? Mathf.Clamp01(left / a.cooldown) : 0f;
                ch.timer.text = left > 0.05f ? left.ToString(left < 10f ? "0.0" : "0") : "READY";
                ch.timer.color = left > 0.05f ? new Color(1f, 0.85f, 0.5f) : new Color(0.7f, 1f, 0.7f);
            }
        }

        void UpdateActions(GameManager gm, PlayerController pl)
        {
            bool placing = BuildPlacement.I.Active;
            if (placeCluster.gameObject.activeSelf != placing) placeCluster.gameObject.SetActive(placing);
            if (actionCluster.gameObject.activeSelf == placing) actionCluster.gameObject.SetActive(!placing);
            if (placing)
            {
                var bp = BuildPlacement.I;
                placeReason.text = bp.Valid ? UIFactory.Col($"Place {bp.Current.displayName}", new Color(0.6f, 1f, 0.6f)) : UIFactory.Col(bp.Reason ?? "", new Color(1f, 0.55f, 0.45f));
                placeReason.supportRichText = true;
                return;
            }
            var seed = gm.Capture.SelectedSeed(pl.Stats);
            throwLabel.text = pl.Aiming ? "PUT\nAWAY" : (seed != null ? $"SEED\n{pl.Stats.Bag.Count(seed.id)}" : "SEED\n0");
            sneakLabel.text = pl.IsCrouching ? "STAND" : "SNEAK";
            // simple buttons (Little Explorer worlds, or the Toy Box switch): no sneak, lock or roll
            bool simple = WorldRules.Current.simpleControls;
            if (simple != simpleShown)
            {
                simpleShown = simple;
                foreach (var n in new[] { "Btn_Sneak", "Btn_Lock", "Btn_Dodge" })
                {
                    var tr = actionCluster.Find(n);
                    if (tr != null) tr.gameObject.SetActive(!simple);
                }
                if (hotbarRoot != null) hotbarRoot.gameObject.SetActive(!simple);   // tools pick themselves
                runBg.gameObject.SetActive(!simple);
                homeBtn.SetActive(simple);
            }
            // a ranged weapon shows how many shots are left under the word
            int ammo = pl.AmmoLeft;
            attackLabel.supportRichText = true;
            if (pl.Aiming) attackLabel.text = "THROW";
            else if (ammo == 0 && pl.ContextAction == "ATTACK") attackLabel.text = $"{pl.ContextAction}\n<size=17><color=#FF9A8A>no {ContentDB.ItemName(pl.AmmoId).ToLowerInvariant()}s</color></size>";
            else if (ammo > 0 && ammo < 999 && pl.ContextAction == "SHOOT") attackLabel.text = $"SHOOT\n<size=20>{ammo}</size>";
            else attackLabel.text = pl.ContextAction;
            jumpLabel.text = pl.Gliding ? "DROP" : pl.CanGlideNow ? "GLIDE" : "JUMP";
            if (hookBtn.activeSelf != pl.HasGrapple) hookBtn.SetActive(pl.HasGrapple);
            string prompt = gm.InteractPrompt;
            bool showI = !string.IsNullOrEmpty(prompt);
            if (interactBtn.activeSelf != showI) interactBtn.SetActive(showI);
            if (showI) interactLabel.text = prompt;
        }

        // ============================================================ TOASTS / FEED
        void Toast(string msg, Color c)
        {
            var t = UIFactory.Label(toastRoot, msg, 30, c, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.rectTransform.anchorMin = new Vector2(0, 1); t.rectTransform.anchorMax = new Vector2(1, 1);
            t.rectTransform.pivot = new Vector2(0.5f, 1);
            t.rectTransform.sizeDelta = new Vector2(0, 44);
            toasts.Insert(0, (t, 3.2f));
            while (toasts.Count > 4) { Destroy(toasts[toasts.Count - 1].t.gameObject); toasts.RemoveAt(toasts.Count - 1); }
        }

        public void Feed(string msg, Color c)
        {
            var t = UIFactory.Label(feedRoot, msg, 26, c, TextAnchor.MiddleLeft, FontStyle.Bold);
            t.rectTransform.anchorMin = new Vector2(0, 0); t.rectTransform.anchorMax = new Vector2(1, 0);
            t.rectTransform.pivot = new Vector2(0, 0);
            t.rectTransform.sizeDelta = new Vector2(0, 36);
            feed.Insert(0, (t, 2.5f));
            while (feed.Count > 6) { Destroy(feed[feed.Count - 1].t.gameObject); feed.RemoveAt(feed.Count - 1); }
        }

        void UpdateToasts(float dt)
        {
            for (int i = toasts.Count - 1; i >= 0; i--)
            {
                var (t, life) = toasts[i];
                life -= Time.unscaledDeltaTime;
                if (life <= 0f) { Destroy(t.gameObject); toasts.RemoveAt(i); continue; }
                toasts[i] = (t, life);
                t.rectTransform.anchoredPosition = Vector2.Lerp(t.rectTransform.anchoredPosition, new Vector2(0, -i * 46f), Time.unscaledDeltaTime * 12f);
                var c = t.color; c.a = Mathf.Clamp01(life / 0.5f); t.color = c;
            }
            for (int i = feed.Count - 1; i >= 0; i--)
            {
                var (t, life) = feed[i];
                life -= Time.unscaledDeltaTime;
                if (life <= 0f) { Destroy(t.gameObject); feed.RemoveAt(i); continue; }
                feed[i] = (t, life);
                t.rectTransform.anchoredPosition = Vector2.Lerp(t.rectTransform.anchoredPosition, new Vector2(0, i * 38f), Time.unscaledDeltaTime * 12f);
                var c = t.color; c.a = Mathf.Clamp01(life / 0.4f); t.color = c;
            }
        }

        // ============================================================ SEED AIM (hold SEED)
        Text aimLabel;
        void UpdateAimLabel(PlayerController pl, Camera cam)
        {
            if (!pl.Aiming)
            {
                if (aimLabel != null && aimLabel.gameObject.activeSelf) aimLabel.gameObject.SetActive(false);
                return;
            }
            if (aimLabel == null)
            {
                aimLabel = UIFactory.Label(dmgRoot, "", 54, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                aimLabel.supportRichText = true;
                aimLabel.rectTransform.sizeDelta = new Vector2(520, 150);
                aimLabel.rectTransform.anchorMin = aimLabel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                var sh = aimLabel.gameObject.AddComponent<Shadow>();
                sh.effectDistance = new Vector2(3, -3);
                sh.effectColor = new Color(0, 0, 0, 0.8f);
            }
            if (!aimLabel.gameObject.activeSelf) aimLabel.gameObject.SetActive(true);
            var seed = GameManager.I.Capture.SelectedSeed(pl.Stats);
            string seedLine = seed != null ? $"<size=22>{seed.displayName} x{pl.Stats.Bag.Count(seed.id)}</size>" : "<size=22>No seeds!</size>";
            var t = pl.AimTarget;
            Vector2 local = new Vector2(0f, 160f);
            if (t != null)
            {
                Vector3 sp = cam.WorldToScreenPoint(t.Position + Vector3.up * (t.Radius * 2f + t.Rig.HoverHeight + 1.2f));
                if (sp.z > 0f) RectTransformUtility.ScreenPointToLocalPointInRectangle(safe, sp, null, out local);
                if (pl.AimInRange)
                {
                    int cap = CaptureSystem.SeedLevelCap(seed);
                    string tooHigh = seed != null && t.Level > cap ? "\n" + UIFactory.Col($"<size=22>Lv too high for {seed.displayName} (up to Lv {cap}) - use a better seed</size>", new Color(1f, 0.55f, 0.4f)) : "";
                    aimLabel.text = UIFactory.Col($"{pl.AimChance * 100f:0}%", PlayerController.ChanceColor(pl.AimChance)) + $"\n<size=24>{t.Sp.displayName} Lv{t.Level}{(t.Unaware ? "  · sneak bonus" : "")}</size>\n" + seedLine + tooHigh;
                }
                else
                    aimLabel.text = UIFactory.Col("Too far", new Color(1f, 0.6f, 0.4f)) + $"\n<size=24>{t.Sp.displayName} - get closer</size>\n" + seedLine;
            }
            else aimLabel.text = UIFactory.Col("Aim at a kin", new Color(0.85f, 0.9f, 1f)) + "\n" + seedLine;
            aimLabel.rectTransform.anchoredPosition = local;
        }

        // ============================================================ DAMAGE NUMBERS
        public static void DamageNumber(Vector3 world, string text, Color c, float scale = 1f)
        {
            if (I == null) return;
            var t = I.dmgPool.Count > 0 ? I.dmgPool.Pop() : UIFactory.Label(I.dmgRoot, "", 40, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.gameObject.SetActive(true);
            t.text = text;
            t.color = c;
            t.rectTransform.sizeDelta = new Vector2(400, 60);
            t.rectTransform.anchorMin = t.rectTransform.anchorMax = Vector2.zero;
            I.dmg.Add(new Dmg { t = t, world = world + Random.insideUnitSphere * 0.3f, life = 0.9f, scale = scale });
        }

        void UpdateDamageNumbers(float dt, Camera cam)
        {
            for (int i = dmg.Count - 1; i >= 0; i--)
            {
                var d = dmg[i];
                d.life -= Time.unscaledDeltaTime;
                d.world += Vector3.up * Time.unscaledDeltaTime * 1.2f;
                if (d.life <= 0f) { d.t.gameObject.SetActive(false); dmgPool.Push(d.t); dmg.RemoveAt(i); continue; }
                dmg[i] = d;
                Vector3 sp = cam.WorldToScreenPoint(d.world);
                if (sp.z < 0f) { d.t.enabled = false; continue; }
                d.t.enabled = true;
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(safe, sp, null, out local);
                d.t.rectTransform.anchorMin = d.t.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                d.t.rectTransform.anchoredPosition = local;
                float pop = d.life > 0.75f ? 1f + (d.life - 0.75f) * 3f : 1f;
                d.t.transform.localScale = Vector3.one * d.scale * pop;
                var c = d.t.color; c.a = Mathf.Clamp01(d.life / 0.3f); d.t.color = c;
            }
        }
    }
}
