using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>
    /// Title screen (MainMenu.unity): Continue, Worlds (3 save slots), New World (name, seed, presets,
    /// custom rule sliders), Settings. A little diorama of the three partner kin idles behind the UI.
    /// Hands off to Main.unity through GameSession.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        Canvas canvas;
        RectTransform panelRoot;
        GameObject modal;
        GameSettings settings;
        readonly List<CreatureRig> rigs = new List<CreatureRig>();
        Transform stage;

        // new-world draft
        WorldSettings draft = WorldSettings.FromPreset(WorldPreset.Normal);
        int draftSlot = -1;
        string draftSeedText = "";
        Appearance draftLook = new Appearance();
        CreatureRig lookRig;
        Transform lookStand;
        float lookYaw = 200f;

        void Awake()
        {
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            Time.timeScale = 1f;
            ContentDB.Init();
            settings = GameSettings.Load();
            if (settings.quality < 0) settings.quality = (int)QualityProfile.SuggestTier();
            QualityProfile.Apply((QualityTier)settings.quality);
            RenderAssets.Init();
            UIFactory.Init();
            SaveSystem.MigrateLegacy();

            BuildBackdrop();
            var audioGo = new GameObject("Audio");
            audioGo.AddComponent<SynthAudio>().Init();
            SynthAudio.MusicVolume = settings.musicVolume;
            SynthAudio.SfxVolume = settings.sfxVolume;

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();

            var cgo = new GameObject("MenuUI");
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.6f;
            cgo.AddComponent<GraphicRaycaster>();

            // title
            var title = UIFactory.Label(canvas.transform, GameInfo.Title.ToUpperInvariant(), 120, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            title.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(70, -40), new Vector2(1000, 150));
            var sub = UIFactory.Label(canvas.transform, "Rimewild Isle  ·  catch, bond, build", 34, new Color(1f, 0.95f, 0.8f), TextAnchor.UpperLeft, FontStyle.Bold);
            sub.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(76, -175), new Vector2(1000, 50));

            panelRoot = UIFactory.Rect(canvas.transform, "Panel");
            panelRoot.Anchors(new Vector2(0, 0), new Vector2(0.52f, 1), new Vector2(60, 50), new Vector2(0, -250));
            ShowHome();
        }

        // ============================================================ BACKDROP
        void BuildBackdrop()
        {
            var camGo = new GameObject("MenuCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.78f, 0.98f);
            cam.fieldOfView = 40f;
            camGo.transform.SetPositionAndRotation(new Vector3(-3.2f, 2.4f, -7.5f), Quaternion.Euler(12f, 14f, 0f));
            camGo.AddComponent<AudioListener>();

            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;
            sunGo.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            RenderSettings.sun = sun;
            RenderSettings.fog = false;
            var sh = new SphericalHarmonicsL2();
            sh.AddAmbientLight(new Color(0.5f, 0.56f, 0.62f));
            sh.AddDirectionalLight(Vector3.up, new Color(0.3f, 0.35f, 0.45f), 1f);
            RenderSettings.ambientProbe = sh;

            // grassy island stage
            stage = new GameObject("Stage").transform;
            stage.position = new Vector3(2.2f, 0f, 0.6f);
            var mb = new MeshBuilder();
            mb.Cylinder(Matrix4x4.TRS(new Vector3(0, -0.4f, 0), Quaternion.identity, new Vector3(9f, 0.8f, 9f)), new Color(0.55f, 0.42f, 0.3f), new Color(0.45f, 0.75f, 0.35f), 28, 1f, true);
            mb.Sphere(Matrix4x4.TRS(new Vector3(-3.2f, 1.6f, 2.6f), Quaternion.identity, new Vector3(2.4f, 2f, 2.4f)), new Color(0.4f, 0.75f, 0.35f), new Color(0.25f, 0.55f, 0.28f), 12, 8);
            mb.Cylinder(Matrix4x4.TRS(new Vector3(-3.2f, 0.4f, 2.6f), Quaternion.identity, new Vector3(0.35f, 1.4f, 0.35f)), new Color(0.45f, 0.3f, 0.2f), 8, 0.7f, false);
            mb.Rock(Matrix4x4.TRS(new Vector3(3.2f, 0.25f, 2.2f), Quaternion.identity, new Vector3(1.4f, 0.9f, 1.2f)), new Color(0.55f, 0.55f, 0.6f), new Color(0.7f, 0.7f, 0.74f), 7u, 7, 5);
            mb.Crystal(Matrix4x4.TRS(new Vector3(2.5f, 0f, 3.2f), Quaternion.Euler(0, 0, -12), new Vector3(0.5f, 1.6f, 0.5f)), new Color(0.4f, 0.85f, 1f), Color.white);
            for (int i = 0; i < 14; i++)
            {
                float a = i * 0.9f, r = 2.5f + (i % 4) * 1.2f;
                mb.Sphere(Matrix4x4.TRS(new Vector3(Mathf.Cos(a) * r, 0.05f, Mathf.Sin(a) * r), Quaternion.identity, new Vector3(0.25f, 0.12f, 0.25f)),
                    i % 3 == 0 ? new Color(1f, 0.6f, 0.8f) : (i % 3 == 1 ? new Color(1f, 0.92f, 0.45f) : Color.white), 6, 4);
            }
            var sgo = new GameObject("Island");
            sgo.transform.SetParent(stage, false);
            sgo.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("menu_island");
            var mr = sgo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.Terrain;

            // the three partners (plus a couple of wild friends)
            string[] ids = { "knuckub", "chiskit", "hopling", "sproutle", "voltbeak" };
            Vector3[] pos = { new Vector3(1.2f, 0, 0.2f), new Vector3(2.7f, 0, 0.8f), new Vector3(0.1f, 0, 1.3f), new Vector3(3.6f, 0, -0.6f), new Vector3(-1.3f, 0, 1.6f) };
            for (int i = 0; i < ids.Length; i++)
            {
                var sp = ContentDB.Sp(ids[i]);
                if (sp == null) continue;
                var d = new CreatureData { uid = "menu" + i, speciesId = sp.id, level = 1, abilities = sp.abilities };
                var go = new GameObject("Kin_" + sp.id);
                go.transform.SetParent(stage, false);
                go.transform.localPosition = pos[i];
                go.transform.localRotation = Quaternion.Euler(0, 200f - i * 12f, 0);
                rigs.Add(ModelBuilder.Build(d, go.transform, RenderAssets.Creature));
            }
        }

        float t;
        void Update()
        {
            t += Time.unscaledDeltaTime;
            if (stage != null) stage.rotation = Quaternion.Euler(0, Mathf.Sin(t * 0.12f) * 10f, 0);
            for (int i = 0; i < rigs.Count; i++)
            {
                rigs[i].Animate(Time.unscaledDeltaTime, 0f, false, false);
                if (Mathf.Repeat(t + i * 1.3f, 5f) < Time.unscaledDeltaTime) { rigs[i].Squash(); rigs[i].Lunge(); }
            }
            if (lookRig != null && lookStand != null)
            {
                lookYaw += Time.unscaledDeltaTime * 26f;
                lookStand.rotation = Quaternion.Euler(0f, lookYaw, 0f);
                lookRig.Animate(Time.unscaledDeltaTime, 0f, false, false);
            }
        }

        // ============================================================ CHARACTER CREATOR
        /// <summary>
        /// The step between CREATE WORLD and the loading page: the explorer stands on a little stone in front of the
        /// camera, turning slowly, while the panel on the left changes them (LookEditor).
        /// </summary>
        void ShowCreator()
        {
            var list = NewPanel("Your explorer");
            if (lookStand == null)
            {
                var cam = Camera.main.transform;
                var go = new GameObject("LookStand");
                // on the right of the view, close enough to fill it from boots to hat
                go.transform.position = cam.position + cam.forward * 4.3f + cam.right * 1.6f - cam.up * 1.0f;
                lookStand = go.transform;
                var mb = new MeshBuilder();
                mb.Cylinder(Matrix4x4.TRS(new Vector3(0, -0.09f, 0), Quaternion.identity, new Vector3(1.15f, 0.18f, 1.15f)), new Color(0.36f, 0.4f, 0.5f), new Color(0.52f, 0.56f, 0.66f), 20, 0.9f, true);
                var plinth = new GameObject("Plinth");
                plinth.transform.SetParent(lookStand, false);
                plinth.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("menu_plinth");
                plinth.AddComponent<MeshRenderer>().sharedMaterial = RenderAssets.Creature;
            }
            if (stage != null) stage.gameObject.SetActive(false);
            RebuildLookModel();

            var top = UIFactory.HRow(list, 90);
            UIFactory.Size(UIFactory.Button(top.transform, "SURPRISE ME", () => { draftLook = Appearance.Random(); ShowCreator(); }, new Color(0.5f, 0.35f, 0.7f), 28), 90, 320);
            UIFactory.Size(UIFactory.Button(top.transform, "CLASSIC", () => { draftLook = new Appearance(); ShowCreator(); }, UIFactory.ButtonCol, 28), 90, 240);
            UIFactory.Size(UIFactory.Label(top.transform, "Looks only: nothing here changes how you play, and you can change it later from the Gear tab.", 20, UIFactory.TextDim), 90, 420);

            LookEditor.Build(list, draftLook, RebuildLookModel);

            var go2 = UIFactory.HRow(list, 110, 16);
            UIFactory.Size(UIFactory.Button(go2.transform, "BACK", () => { CloseCreator(); ShowNewWorld(); }, new Color(0.3f, 0.3f, 0.34f), 32), 100, 220);
            UIFactory.Size(UIFactory.Button(go2.transform, "START", StartWorld, new Color(0.2f, 0.55f, 0.42f), 36), 100, 560);
        }

        string lookKey;
        void RebuildLookModel()
        {
            if (lookStand == null) return;
            if (lookRig != null && lookRig.Model != null) Destroy(lookRig.Model.gameObject);
            if (lookKey != null) ModelBuilder.DropCustom(lookKey);
            lookKey = draftLook.Key(false);
            lookRig = AvatarLook.BuildRig(draftLook, false, lookStand);
        }

        void CloseCreator()
        {
            if (lookStand != null) Destroy(lookStand.gameObject);
            lookStand = null; lookRig = null;
            if (stage != null) stage.gameObject.SetActive(true);
        }

        void StartWorld()
        {
            GameSession.Slot = draftSlot;
            GameSession.NewWorld = draft.Clone();
            GameSession.NewLook = draftLook.Clone();
            GameSession.FromMenu = true;
            LoadWorld();
        }

        // ============================================================ PANELS
        RectTransform NewPanel(string title)
        {
            CloseModal();
            for (int i = panelRoot.childCount - 1; i >= 0; i--) Destroy(panelRoot.GetChild(i).gameObject);
            var bg = UIFactory.Panel(panelRoot, "Panel", new Color(0.05f, 0.07f, 0.1f, 0.92f));
            bg.rectTransform.Stretch();
            if (!string.IsNullOrEmpty(title))
            {
                var h = UIFactory.Label(bg.transform, title, 44, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                h.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -14), new Vector2(700, 60));
            }
            var list = UIFactory.ScrollList(bg.transform, out _, 12f);
            ((RectTransform)list.parent).Anchors(Vector2.zero, Vector2.one, new Vector2(20, 20), new Vector2(-20, string.IsNullOrEmpty(title) ? -20 : -86));
            return list;
        }

        Button Big(Transform parent, string label, Action a, Color? c = null, float h = 96)
        {
            var b = UIFactory.Button(parent, label, a, c ?? new Color(0.2f, 0.32f, 0.4f, 0.95f), 36);
            UIFactory.Size(b, h);
            return b;
        }

        void ShowHome()
        {
            var list = NewPanel(null);
            int last = SaveSystem.LastSlot;
            if (last < 0 || !SaveSystem.SlotExists(last))
            {
                // fall back to the most recently saved world (e.g. a save migrated from an older version)
                long best = -1;
                for (int i = 0; i < GameSession.SlotCount; i++)
                {
                    var su = SaveSystem.Summary(i);
                    if (su.exists && su.savedAtUnix > best) { best = su.savedAtUnix; last = i; }
                }
            }
            var lastSum = last >= 0 ? SaveSystem.Summary(last) : null;
            if (lastSum != null && lastSum.exists)
                Big(list, $"CONTINUE  <size=24>{lastSum.worldName} · Lv {lastSum.level}</size>", () => Play(last), new Color(0.2f, 0.55f, 0.42f, 0.95f), 120).GetComponentInChildren<Text>().supportRichText = true;
            Big(list, "WORLDS", ShowWorlds);
            Big(list, "NEW WORLD", () => { draft = WorldSettings.FromPreset(WorldPreset.Normal); draftSlot = FirstEmptySlot(); draftSeedText = ""; ShowNewWorld(); });
            Big(list, "SETTINGS", ShowSettings);
            if (!Application.isEditor) Big(list, "QUIT", Application.Quit, new Color(0.45f, 0.22f, 0.22f, 0.95f));
            var v = UIFactory.Label(list, $"v{Application.version}", 20, new Color(1, 1, 1, 0.5f));
            UIFactory.Size(v, 30);
        }

        static int FirstEmptySlot()
        {
            for (int i = 0; i < GameSession.SlotCount; i++) if (!SaveSystem.SlotExists(i)) return i;
            return -1;
        }

        // ------------------------------------------------------------ worlds
        void ShowWorlds()
        {
            var list = NewPanel("Worlds");
            for (int i = 0; i < GameSession.SlotCount; i++)
            {
                int slot = i;
                var s = SaveSystem.Summary(i);
                var card = UIFactory.Panel(list, "Slot", new Color(1, 1, 1, s.exists ? 0.08f : 0.04f));
                UIFactory.Size(card, 170);
                if (s.exists)
                {
                    var tt = UIFactory.Label(card.transform,
                        $"<b>{s.worldName}</b>   <size=24>Slot {slot + 1}</size>\n" +
                        $"<size=24>Explorer Lv {s.level}  ·  {s.kin} kin{(s.partner != null ? "  ·  partner " + s.partner : "")}  ·  {FormatTime(s.playSeconds)} played</size>\n" +
                        $"<size=20>{s.world.Summary()}  ·  saved {FormatAgo(s.savedAtUnix)}</size>", 32, Color.white, TextAnchor.MiddleLeft);
                    tt.supportRichText = true;
                    tt.rectTransform.Stretch(20, 8, 330, 8);
                    var play = UIFactory.Button(card.transform, "PLAY", () => Play(slot), new Color(0.2f, 0.55f, 0.42f), 32);
                    ((RectTransform)play.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-150, 0), new Vector2(160, 110));
                    var del = UIFactory.Button(card.transform, "DELETE", () => Confirm($"Delete \"{s.worldName}\"?", "This permanently erases the world, your camp and all kin in it.",
                        () => { SaveSystem.DeleteSlot(slot); ShowWorlds(); }), new Color(0.45f, 0.22f, 0.22f), 22);
                    ((RectTransform)del.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-14, 0), new Vector2(124, 110));
                }
                else
                {
                    var tt = UIFactory.Label(card.transform, $"<b>Empty slot {slot + 1}</b>\n<size=22>Start a new world here.</size>", 30, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleLeft);
                    tt.supportRichText = true;
                    tt.rectTransform.Stretch(20, 8, 330, 8);
                    var nw = UIFactory.Button(card.transform, "NEW WORLD", () => { draft = WorldSettings.FromPreset(WorldPreset.Normal); draftSlot = slot; draftSeedText = ""; ShowNewWorld(); }, new Color(0.25f, 0.42f, 0.55f), 26);
                    ((RectTransform)nw.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-14, 0), new Vector2(260, 110));
                }
            }
            Big(list, "BACK", ShowHome, new Color(0.3f, 0.3f, 0.34f, 0.95f), 84);
        }

        static string FormatTime(double s)
        {
            int m = (int)(s / 60);
            return m >= 60 ? $"{m / 60}h {m % 60}m" : $"{m}m";
        }

        static string FormatAgo(long unix)
        {
            if (unix <= 0) return "-";
            var span = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(unix);
            if (span.TotalMinutes < 1) return "just now";
            if (span.TotalHours < 1) return $"{(int)span.TotalMinutes} min ago";
            if (span.TotalDays < 1) return $"{(int)span.TotalHours} h ago";
            return $"{(int)span.TotalDays} d ago";
        }

        // ------------------------------------------------------------ new world
        void ShowNewWorld()
        {
            var list = NewPanel("New World");
            if (draftSlot < 0)
            {
                UIFactory.Label(list, "All 3 slots are used - pick one to overwrite:", 26, new Color(1f, 0.8f, 0.6f));
                var row = UIFactory.HRow(list, 80, 12);
                for (int i = 0; i < GameSession.SlotCount; i++)
                {
                    int s = i;
                    UIFactory.Size(UIFactory.Button(row.transform, $"SLOT {i + 1}", () => { draftSlot = s; ShowNewWorld(); }), 76, 200);
                }
            }
            else
            {
                string warn = SaveSystem.SlotExists(draftSlot) ? UIFactory.Col("  (will overwrite!)", new Color(1f, 0.5f, 0.4f)) : "";
                var l = UIFactory.Label(list, $"Slot {draftSlot + 1}{warn}", 26, UIFactory.TextDim);
                l.supportRichText = true;
                UIFactory.Size(l, 36);
            }

            var nameRow = UIFactory.HRow(list, 80, 14);
            UIFactory.Size(UIFactory.Label(nameRow.transform, "World name", 26, Color.white), 70, 220);
            var nameIn = UIFactory.Input(nameRow.transform, draft.worldName, "Rimewild Isle");
            UIFactory.Size(nameIn, 70, 560);
            nameIn.onValueChanged.AddListener(v => draft.worldName = v);

            var seedRow = UIFactory.HRow(list, 80, 14);
            UIFactory.Size(UIFactory.Label(seedRow.transform, "Seed", 26, Color.white), 70, 220);
            var seedIn = UIFactory.Input(seedRow.transform, draftSeedText, "random (any number or word)");
            UIFactory.Size(seedIn, 70, 560);
            seedIn.onValueChanged.AddListener(v => draftSeedText = v);

            var presets = UIFactory.HRow(list, 84, 10);
            foreach (WorldPreset p in Enum.GetValues(typeof(WorldPreset)))
            {
                var pp = p;
                UIFactory.Size(UIFactory.Button(presets.transform, p.ToString().ToUpperInvariant(), () =>
                {
                    if (pp == WorldPreset.Custom) { draft.preset = WorldPreset.Custom; ShowNewWorld(); return; }
                    string nm = draft.worldName;
                    bool toys = draft.toyBox;
                    draft = WorldSettings.FromPreset(pp);
                    draft.worldName = nm;
                    draft.toyBox = toys;
                    ShowNewWorld();
                }, draft.preset == p ? new Color(0.25f, 0.55f, 0.45f) : UIFactory.ButtonCol, 26), 80, 190);
            }
            var desc = UIFactory.Label(list, PresetBlurb(draft.preset), 22, UIFactory.TextDim);
            UIFactory.Size(desc, 34);
            // the Toy Box goes with any preset: it is a switch of its own
            var toyRow = UIFactory.HRow(list, 84, 12);
            UIFactory.Size(UIFactory.Button(toyRow.transform, "TOY BOX: " + (draft.toyBox ? "ON" : "OFF"), () => { draft.toyBox = !draft.toyBox; ShowNewWorld(); },
                draft.toyBox ? new Color(0.5f, 0.35f, 0.7f) : UIFactory.ButtonCol, 26), 80, 300);
            UIFactory.Size(UIFactory.Label(toyRow.transform, "Lets whoever is playing change every rule from Settings while they play: time, weather, kin size, jump height and all the sliders below.", 20, UIFactory.TextDim), 80, 700);

            // rule sliders (editing any switches the preset to Custom)
            void Custom() { if (draft.preset != WorldPreset.Custom) draft.preset = WorldPreset.Custom; }
            Func<float, string> x = v => $"x{v:0.##}";
            Header(list, "Wildlife");
            UIFactory.SliderRow(list, "Wild spawn rate", 0.25f, 3f, draft.wildSpawnRate, false, x, v => { draft.wildSpawnRate = v; Custom(); });
            UIFactory.SliderRow(list, "Capture chance", 0.25f, 3f, draft.captureRate, false, x, v => { draft.captureRate = v; Custom(); });
            UIFactory.SliderRow(list, "Shimmer (rare colour) odds", 1f, 20f, draft.shimmerRate, false, x, v => { draft.shimmerRate = v; Custom(); });
            UIFactory.SliderRow(list, "Alpha respawn (minutes)", 5f, 120f, draft.alphaRespawnMinutes, true, v => $"{v:0} min", v => { draft.alphaRespawnMinutes = v; Custom(); });
            Header(list, "Progression");
            UIFactory.SliderRow(list, "XP gain", 0.25f, 5f, draft.expRate, false, x, v => { draft.expRate = v; Custom(); });
            UIFactory.SliderRow(list, "Gathering yield", 0.25f, 5f, draft.gatherRate, false, x, v => { draft.gatherRate = v; Custom(); });
            UIFactory.SliderRow(list, "Resource regrowth speed", 0.25f, 4f, draft.resourceRespawnRate, false, x, v => { draft.resourceRespawnRate = v; Custom(); });
            Header(list, "Combat & survival");
            UIFactory.SliderRow(list, "Damage taken", 0.25f, 3f, draft.wildDamage, false, x, v => { draft.wildDamage = v; Custom(); });
            UIFactory.SliderRow(list, "Damage dealt", 0.25f, 3f, draft.playerDamage, false, x, v => { draft.playerDamage = v; Custom(); });
            UIFactory.SliderRow(list, "Hunger drain", 0f, 3f, draft.hungerRate, false, x, v => { draft.hungerRate = v; Custom(); });
            UIFactory.SliderRow(list, "Stamina drain", 0f, 2f, draft.staminaDrain, false, x, v => { draft.staminaDrain = v; Custom(); });
            UIFactory.SliderRow(list, "Day length", 6f, 60f, draft.dayLengthMinutes, true, v => $"{v:0} min", v => { draft.dayLengthMinutes = v; Custom(); });
            Header(list, "Camp");
            UIFactory.SliderRow(list, "Camp workers (Hearth Lv1)", 1f, 20f, draft.campWorkersBase, true, v => $"{v:0}", v => { draft.campWorkersBase = Mathf.RoundToInt(v); Custom(); });
            UIFactory.SliderRow(list, "+ workers per Hearth upgrade", 0f, 10f, draft.campWorkersPerLevel, true, v => $"+{v:0}", v => { draft.campWorkersPerLevel = Mathf.RoundToInt(v); Custom(); });
            UIFactory.SliderRow(list, "Work speed", 0.25f, 4f, draft.workSpeed, false, x, v => { draft.workSpeed = v; Custom(); });
            UIFactory.SliderRow(list, "Camp raids", 0f, 3f, draft.raidFrequency, false, v => v < 0.05f ? "off" : $"x{v:0.##}", v => { draft.raidFrequency = v; Custom(); });
            Header(list, "Young players");
            var kid1 = UIFactory.HRow(list, 84, 12);
            void Kid(Transform row, string label, bool on, Action flip) =>
                UIFactory.Size(UIFactory.Button(row, label + ": " + (on ? "ON" : "OFF"), () => { flip(); Custom(); ShowNewWorld(); }, on ? new Color(0.25f, 0.55f, 0.45f) : UIFactory.ButtonCol, 24), 80, 340);
            Kid(kid1.transform, "Simple buttons", draft.simpleControls, () => draft.simpleControls = !draft.simpleControls);
            Kid(kid1.transform, "Can't faint", draft.cantFaint, () => draft.cantFaint = !draft.cantFaint);
            Kid(kid1.transform, "Safe travel", draft.safeTravel, () => draft.safeTravel = !draft.safeTravel);
            var kid2 = UIFactory.HRow(list, 84, 12);
            Kid(kid2.transform, "Friendly wild kin", draft.friendlyWild, () => draft.friendlyWild = !draft.friendlyWild);
            Kid(kid2.transform, "Wild kin at my level", draft.levelMatched, () => draft.levelMatched = !draft.levelMatched);
            Kid(kid2.transform, "Endless seeds & ammo", draft.endlessSeeds, () => draft.endlessSeeds = !draft.endlessSeeds);
            var kid3 = UIFactory.HRow(list, 84, 12);
            Kid(kid3.transform, "Free building", draft.freeBuild, () => draft.freeBuild = !draft.freeBuild);
            Kid(kid3.transform, "Ride any kin", draft.rideAnything, () => draft.rideAnything = !draft.rideAnything);
            Kid(kid3.transform, "Parade", draft.parade, () => draft.parade = !draft.parade);
            var kid4 = UIFactory.HRow(list, 84, 12);
            Kid(kid4.transform, "Kids' roster", draft.kidRoster, () => draft.kidRoster = !draft.kidRoster);
            UIFactory.Size(UIFactory.Label(kid4.transform, "Kids' roster leaves the three glamorous lady kin (Sirelle, Sekhara, Cindra) out of the wild.", 20, UIFactory.TextDim), 80, 700);
            Header(list, "Options");
            var tog = UIFactory.HRow(list, 84, 12);
            UIFactory.Size(UIFactory.Button(tog.transform, "Fast travel: " + (draft.fastTravel ? "ON" : "OFF"), () => { draft.fastTravel = !draft.fastTravel; Custom(); ShowNewWorld(); }), 80, 380);
            UIFactory.Size(UIFactory.Button(tog.transform, "Offline camp progress: " + (draft.offlineProgress ? "ON" : "OFF"), () => { draft.offlineProgress = !draft.offlineProgress; Custom(); ShowNewWorld(); }), 80, 480);

            var go = UIFactory.HRow(list, 110, 14);
            UIFactory.Size(UIFactory.Button(go.transform, "BACK", ShowHome, new Color(0.3f, 0.3f, 0.34f), 32), 100, 220);
            UIFactory.Size(UIFactory.Button(go.transform, "NEXT: YOUR EXPLORER", CreateWorld, new Color(0.2f, 0.55f, 0.42f), 36), 100, 560);
        }

        void Header(Transform list, string text)
        {
            var h = UIFactory.Label(list, text, 30, UIFactory.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Size(h, 50);
        }

        static string PresetBlurb(WorldPreset p)
        {
            switch (p)
            {
                case WorldPreset.Casual: return "Relaxed: easier captures, more loot & XP, gentle wildlife, bigger camps.";
                case WorldPreset.Hard: return "Survival: more & tougher wildlife, scarcer resources, hungrier, smaller camps.";
                case WorldPreset.Custom: return "Custom rules - tune every slider below.";
                case WorldPreset.Explorer: return "Little Explorer, for young children: can't faint, no hunger, fewer buttons, one-tap catching, seeds never run out.";
                default: return $"The intended {GameInfo.Title} experience.";
            }
        }

        void CreateWorld()
        {
            if (draftSlot < 0) { SynthAudio.Play(Sfx.Clunk); return; }
            if (string.IsNullOrWhiteSpace(draft.worldName)) draft.worldName = "Rimewild Isle";
            draft.seed = ParseSeed(draftSeedText);
            void Go() => ShowCreator();   // who are you? then StartWorld
            if (SaveSystem.SlotExists(draftSlot)) Confirm("Overwrite world?", $"Slot {draftSlot + 1} already has a world. Replace it with \"{draft.worldName}\"?", Go);
            else Go();
        }

        static int ParseSeed(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return UnityEngine.Random.Range(1, int.MaxValue);
            if (int.TryParse(text.Trim(), out int n) && n != 0) return n;
            unchecked
            {
                int h = 17;
                foreach (char c in text.Trim().ToLowerInvariant()) h = h * 31 + c;
                return h == 0 ? 7129 : Mathf.Abs(h);
            }
        }

        void Play(int slot)
        {
            GameSession.Slot = slot;
            GameSession.NewWorld = null;
            GameSession.NewLook = null;
            GameSession.FromMenu = true;
            LoadWorld();
        }

        void LoadWorld()
        {
            CloseModal();
            for (int i = panelRoot.childCount - 1; i >= 0; i--) Destroy(panelRoot.GetChild(i).gameObject);
            // the loading page: the same picture the app opens on (Assets/Resources/UI/loading_art), filling the screen
            Transform top = panelRoot.parent;
            var art = Resources.Load<Texture2D>("UI/loading_art");
            if (art != null)
            {
                var cover = UIFactory.Panel(top, "LoadingCover", new Color(0.05f, 0.06f, 0.19f, 1f));
                cover.rectTransform.Stretch();
                var pic = UIFactory.Rect(cover.transform, "Art").gameObject.AddComponent<RawImage>();
                pic.texture = art; pic.raycastTarget = false;
                pic.rectTransform.anchorMin = pic.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                var fit = pic.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;   // fill, cropping the spare sky at the sides
                fit.aspectRatio = art.width / (float)art.height;
                top = cover.transform;
            }
            var l = UIFactory.Label(top, "Loading world...", 44, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            l.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 52), new Vector2(900, 70));   // on the hill, under the kin's feet
            var shade = l.gameObject.AddComponent<Outline>();
            shade.effectColor = new Color(0.05f, 0.06f, 0.2f, 1f); shade.effectDistance = new Vector2(3, -3);
            SynthAudio.Play(Sfx.Summon);
            SceneManager.LoadScene(GameSession.WorldScene);
        }

        // ------------------------------------------------------------ settings
        void ShowSettings()
        {
            var list = NewPanel("Settings");
            var q = UIFactory.HRow(list, 84, 12);
            UIFactory.Size(UIFactory.Label(q.transform, "Graphics", 26, Color.white), 80, 220);
            foreach (QualityTier tier in Enum.GetValues(typeof(QualityTier)))
            {
                var tt = tier;
                UIFactory.Size(UIFactory.Button(q.transform, tier.ToString().ToUpperInvariant(), () => { settings.quality = (int)tt; settings.Save(); QualityProfile.Apply(tt); ShowSettings(); },
                    settings.quality == (int)tier ? new Color(0.25f, 0.55f, 0.45f) : UIFactory.ButtonCol, 26), 80, 200);
            }
            UIFactory.SliderRow(list, "Look sensitivity", 0.3f, 2.5f, settings.lookSensitivity, false, v => $"{v:0.0}", v => { settings.lookSensitivity = v; settings.Save(); });
            UIFactory.SliderRow(list, "Music", 0f, 1f, settings.musicVolume, false, v => $"{v * 100:0}%", v => { settings.musicVolume = v; SynthAudio.MusicVolume = v; settings.Save(); });
            UIFactory.SliderRow(list, "Sound FX", 0f, 1f, settings.sfxVolume, false, v => $"{v * 100:0}%", v => { settings.sfxVolume = v; SynthAudio.SfxVolume = v; settings.Save(); SynthAudio.Play(Sfx.UiClick); });
            var tg = UIFactory.HRow(list, 84, 12);
            UIFactory.Size(UIFactory.Button(tg.transform, "Invert Y: " + (settings.invertY ? "ON" : "OFF"), () => { settings.invertY = !settings.invertY; settings.Save(); ShowSettings(); }), 80, 300);
            UIFactory.Size(UIFactory.Button(tg.transform, "FPS counter: " + (settings.showFps ? "ON" : "OFF"), () => { settings.showFps = !settings.showFps; settings.Save(); ShowSettings(); }), 80, 340);
            Big(list, "BACK", ShowHome, new Color(0.3f, 0.3f, 0.34f, 0.95f), 84);
        }

        // ------------------------------------------------------------ modal
        void Confirm(string title, string body, Action yes)
        {
            CloseModal();
            var bg = UIFactory.Panel(canvas.transform, "Modal", new Color(0, 0, 0, 0.7f));
            bg.rectTransform.Stretch();
            var box = UIFactory.Panel(bg.transform, "Box", UIFactory.PanelBg2);
            box.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 420));
            var t = UIFactory.Label(box.transform, $"<b>{title}</b>\n\n<size=26>{body}</size>", 36, Color.white, TextAnchor.UpperCenter);
            t.supportRichText = true;
            t.rectTransform.Stretch(40, 130, 40, 30);
            var y = UIFactory.Button(box.transform, "YES", () => { CloseModal(); yes(); }, new Color(0.55f, 0.28f, 0.24f), 32);
            ((RectTransform)y.transform).Place(new Vector2(0.5f, 0), new Vector2(1, 0), new Vector2(-12, 24), new Vector2(260, 90));
            var n = UIFactory.Button(box.transform, "NO", CloseModal, UIFactory.ButtonCol, 32);
            ((RectTransform)n.transform).Place(new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(12, 24), new Vector2(260, 90));
            modal = bg.gameObject;
        }

        void CloseModal()
        {
            if (modal != null) Destroy(modal);
            modal = null;
        }
    }
}
