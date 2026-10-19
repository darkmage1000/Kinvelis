using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>
    /// Bootstraps the whole game from code (the scene only needs this one component) and drives
    /// every system in a fixed order each frame. Also owns cross-system game rules
    /// (XP, loot, capture registration, death/respawn, interaction, goals, autosave).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }
        public static double WorldTime;
        public static bool SkipAutosaveOnce;

        [Header("World")]
        [SerializeField] int worldSeed = 7129;
        [SerializeField] float autosaveInterval = 90f;

        [Header("Progression")]
        [SerializeField] int xpPerCapture = 20;
        [SerializeField] int xpPerDefeat = 10;
        [Tooltip("Share of defeat XP the benched (not summoned) party kin receive.")]
        [SerializeField, Range(0, 1)] float benchDefeatShare = 0.5f;
        [Tooltip("Share of the player's capture XP every party kin receives.")]
        [SerializeField, Range(0, 1)] float captureShare = 0.5f;
        [Tooltip("XP a camp worker earns per 'workXpInterval' seconds of actual work.")]
        [SerializeField] int workXpAmount = 3;
        [SerializeField] float workXpInterval = 12f;
        public int WorkXpAmount => workXpAmount;
        public float WorkXpInterval => workXpInterval;

        public PlayerController Player { get; private set; }
        public CameraRig CameraRig { get; private set; }
        public KinRoster Party { get; private set; }
        public CaptureSystem Capture { get; private set; }
        public GameSettings Settings { get; private set; }
        public bool Started { get; private set; }
        public bool Paused { get; private set; }
        public bool ShowCampRing = true;
        public float AutosaveInterval => autosaveInterval;
        public string InteractPrompt { get; private set; }
        /// <summary>The camp structure the player is standing at (MANAGE on the HUD opens it), or null.</summary>
        public Building ManageTarget { get; private set; }
        public readonly HashSet<string> GoalsDone = new HashSet<string>();
        public long CreatedAtUnix { get; private set; }
        public double PlaySeconds { get; private set; }

        ChunkManager chunks;
        EnvironmentSystem env;
        CreatureSpawner spawner;
        ProjectileSystem projectiles;
        BaseManager baseMgr;
        BuildPlacement placement;
        Waystones waystones;
        HUD hud;
        MenuUI menu;
        float autosaveTimer;
        float hitStopTimer;
        float deathTimer = -1f;
        Building interactBuilding;
        Waystones.Stone interactStone;
        bool mapReady;
        Text goalText;
        Image goalIcon;
        Text goalArrow;
        string currentGoal;

        // ============================================================ BOOT
        void Awake()
        {
            I = this;
            WorldTime = 0;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Application.runInBackground = true; // keeps the editor simulating when unfocused (ignored on mobile)

            ContentDB.Init();
            Settings = GameSettings.Load();
            if (Settings.quality < 0) Settings.quality = (int)QualityProfile.SuggestTier();
            QualityProfile.Apply((QualityTier)Settings.quality);

            RenderAssets.Init();
            UIFactory.Init();

            // which world? (main menu hands over a slot and optionally new-world settings)
            SaveSystem.MigrateLegacy();
            ContentDB.ClearCustom();   // the last world's made-up kin
            SaveData save = null;
            if (GameSession.NewWorld != null)
            {
                SaveSystem.DeleteSlot(GameSession.Slot);
                WorldRules.Current = GameSession.NewWorld.Clone();
                GameSession.NewWorld = null;
            }
            else
            {
                save = SaveSystem.Load();
                if (save != null && save.player != null && save.player.customKin != null)
                    foreach (var ck in save.player.customKin) ContentDB.RegisterCustom(ContentSeed.BuildCustom(ck));   // before anything asks a kin for its species
                WorldRules.Current = save != null && save.world != null ? save.world : new WorldSettings { seed = save != null && save.seed != 0 ? save.seed : worldSeed };
            }
            if (WorldRules.Current.seed == 0) WorldRules.Current.seed = worldSeed;
            SaveSystem.LastSlot = GameSession.Slot;
            WorldGen.Init(save != null && save.seed != 0 ? save.seed : WorldRules.Current.seed);
            Landmarks.Build(Sys("Landmarks").transform);
            Settlements.Prepare();   // sites first, so chunks know where not to grow trees
            Wardens.Prepare();
            LegendHunts.Prepare();
            Landmarks.BuildOuterLairs();
            Trails.Prepare();        // roads between the settlements: the ground colour and the map both read them
            MapTexture.Begin();

            // camera + light
            var camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 900f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.6f, 0.75f, 0.9f);
            cam.fieldOfView = 58f;
            camGo.AddComponent<AudioListener>();
            CameraRig = camGo.AddComponent<CameraRig>();

            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            RenderSettings.sun = sun;
            ItemIcons.RenderAll(sun);

            // event system for touch UI
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();

            // systems
            Fx.Init(Sys("FX").transform);
            chunks = Sys("World").AddComponent<ChunkManager>(); chunks.Init(RenderAssets.Terrain, RenderAssets.Props);
            env = Sys("Environment").AddComponent<EnvironmentSystem>(); env.Init(sun);
            spawner = Sys("Creatures").AddComponent<CreatureSpawner>(); spawner.Init();
            projectiles = Sys("Projectiles").AddComponent<ProjectileSystem>(); projectiles.Init();
            Capture = Sys("Capture").AddComponent<CaptureSystem>(); Capture.Init();
            baseMgr = Sys("Camp").AddComponent<BaseManager>(); baseMgr.Init();
            placement = Sys("Placement").AddComponent<BuildPlacement>(); placement.Init();
            housing = Sys("House").AddComponent<Housing>(); housing.Init();
            Sys("Audio").AddComponent<SynthAudio>().Init();
            Party = new KinRoster();

            // player
            var playerGo = new GameObject("Player");
            Player = playerGo.AddComponent<PlayerController>();
            Player.Init(save != null ? save.player : NewPlayerStats(), CameraRig);
            Player.Stats.EnsureValid();

            // UI
            var canvasGo = new GameObject("UI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.6f;
            canvasGo.AddComponent<GraphicRaycaster>();
            hud = canvasGo.AddComponent<HUD>(); hud.Build(canvas);
            menu = canvasGo.AddComponent<MenuUI>(); menu.Build(canvas);
            BuildGoalLabel(canvas);
            waystones = Sys("Waystones").AddComponent<Waystones>();
            waystones.Init(canvas.transform);
            relics = Sys("Relics").AddComponent<Relics>();
            relics.Init();
            nightfall = Sys("Nightfall").AddComponent<Nightfall>();
            nightfall.Init();
            hazards = Sys("Hazards").AddComponent<Hazards>();
            hazards.Init();
            caves = Sys("Caves").AddComponent<Caves>();
            caves.Init(canvas.transform);
            pois = Sys("Pois").AddComponent<Pois>();
            pois.Init();   // last of the sites: it keeps clear of everything placed before it
            fishing = Sys("Fishing").AddComponent<Fishing>();
            fishing.Init();
            nests = Sys("Nests").AddComponent<Nests>();
            nests.Init();
            chests = Sys("Chests").AddComponent<Chests>();
            chests.Init();
            ambient = Sys("AmbientLife").AddComponent<AmbientLife>();
            ambient.Init();
            trader = Sys("Trader").AddComponent<Trader>();
            trader.Init();
            settlements = Sys("Settlements").AddComponent<Settlements>();
            settlements.Init();
            trails = Sys("Trails").AddComponent<Trails>();
            trails.Init();
            tamers = Sys("Tamers").AddComponent<Tamers>();
            tamers.Init();
            wardens = Sys("Wardens").AddComponent<Wardens>();
            wardens.Init();
            outposts = Sys("Outposts").AddComponent<Outposts>();
            outposts.Init();
            hunts = Sys("LegendHunts").AddComponent<LegendHunts>();
            hunts.Init();
            worldEvents = Sys("WorldEvents").AddComponent<WorldEvents>();
            worldEvents.Init();

            if (save != null) ApplySave(save);
            else StartFresh();
            SyncDexFromRoster();
            env.SetDayLength(WorldRules.Current.dayLengthMinutes * 60f);
            ApplySettings();
            Started = true;

            // first launch (or a save from before partners existed): pick a starter kin
            if (!Player.Stats.starterChosen) canvasGo.AddComponent<StarterSelect>().Begin(canvas);
        }

        public void SetGoalVisible(bool on)
        {
            if (goalText != null) goalText.transform.parent.gameObject.SetActive(on && !string.IsNullOrEmpty(currentGoal));
        }

        GameObject Sys(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            return go;
        }

        PlayerStats NewPlayerStats()
        {
            var s = new PlayerStats();
            if (GameSession.NewLook != null) { s.look = GameSession.NewLook.Clone(); GameSession.NewLook = null; }
            s.EnsureValid();
            s.Bag.Add("tether_seed", 3);
            s.Bag.Add("berry", 6);
            return s;
        }

        void StartFresh()
        {
            CreatedAtUnix = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var spawn = WorldGen.FindSpawnPoint();
            Player.Teleport(spawn, 20f);
            CameraRig.Init(CameraRig.Cam != null ? CameraRig.Cam : Camera.main, spawn, 20f);
            env.timeOfDay = 0.3f;
            GameEvents.Say("Welcome to Rimewild Isle!", new Color(0.7f, 1f, 0.8f));
        }

        void ApplySave(SaveData s)
        {
            WorldTime = s.worldTime;
            CreatedAtUnix = s.createdAtUnix;
            PlaySeconds = s.playSeconds;
            env.timeOfDay = s.timeOfDay;
            env.SetWeather((WeatherType)s.weather, s.weatherRemaining);
            foreach (var n in s.depleted) chunks.Depleted[n.key] = n.t;
            foreach (var a in s.alphaCooldowns) spawner.AlphaCooldowns[a.id] = a.t;
            foreach (var g in s.goalsDone) GoalsDone.Add(g);
            foreach (var k in s.kin) if (k.Species != null) Party.All.Add(k);

            // camp
            if (s.hasBase)
            {
                baseMgr.CoreLevel = Mathf.Max(1, s.coreLevel);
                baseMgr.Charge = Mathf.Max(0f, s.campCharge);
                baseMgr.Cold = Mathf.Max(0f, s.campCold);
                // core first so others validate
                foreach (var b in s.buildings)
                {
                    var def = ContentDB.Building(b.defId);
                    if (def == null || !def.isCore) continue;
                    RestoreBuilding(def, b);
                }
                foreach (var b in s.buildings)
                {
                    var def = ContentDB.Building(b.defId);
                    if (def == null || def.isCore) continue;
                    RestoreBuilding(def, b);
                }
                baseMgr.Storage.CopyFrom(s.storage);
                baseMgr.LayoutStorage(s.storageLayout);   // each structure's box is where the save left it
                baseMgr.Orders.AddRange(s.orders);
                if (s.standing != null) baseMgr.Standing.AddRange(s.standing);
            }
            housing.Load(s.house);
            baseMgr.LoadSites(s.sites);

            var pos = new Vector3(s.px, s.py, s.pz);
            Player.Teleport(pos, s.yaw);
            CameraRig.Init(Camera.main, pos, s.camYaw);

            // offline progress for the camp (real time since last save, capped)
            long away = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() - s.savedAtUnix;
            if (away > 30) baseMgr.SimulateOffline(away);

            var comp = Party.Find(s.companionUid);
            if (comp != null && comp.location == KinLocation.Party && !comp.Fainted) pendingSummon = comp;
            GameEvents.Say("Welcome back!", new Color(0.7f, 1f, 0.8f));
        }

        CreatureData pendingSummon;

        void RestoreBuilding(BuildingDef def, BuildingSave b)
        {
            var bld = baseMgr.Place(def, new Vector3(b.x, b.y, b.z), b.rotY, b.uid);
            bld.Stage = Mathf.Clamp(b.stage, 0, bld.Stages.Length - 1);
            bld.Progress = Mathf.Min(b.progress, bld.StageNeed * 0.999f);   // (a save from before stages: its old progress, kept within the first step)
            bld.BufferItem = b.bufferItem;
            bld.BufferCount = b.bufferCount;
            bld.Crop = b.crop;
            bld.Level = bld.Upgradable ? Mathf.Clamp(b.level, 1, Building.MaxLevel) : 1;   // 0 in saves from before levels
            bld.Filter = (BoxFilter)Mathf.Clamp(b.filter, 0, BaseManager.FilterNames.Length - 1);
            if (bld.Feed != null)
            {
                bld.FeedAuto = !b.feedByHand;
                if (b.feed != null) for (int i = 0; i < b.feed.Count && i < bld.Feed.slots.Count; i++) { bld.Feed.slots[i].id = b.feed[i].id ?? ""; bld.Feed.slots[i].count = b.feed[i].count; }
            }
        }

        /// <summary>Save and go back to the main menu (new worlds are created there).</summary>
        public void QuitToMenu()
        {
            SaveSystem.QuitToMenu();
        }

        public void ApplySettings()
        {
            var s = Settings;
            if ((int)QualityProfile.Current.tier != s.quality) QualityProfile.Apply((QualityTier)s.quality);
            InputHub.LookSensitivity = s.lookSensitivity;
            InputHub.InvertY = s.invertY;
            SynthAudio.MusicVolume = s.musicVolume;
            SynthAudio.SfxVolume = s.sfxVolume;
            if (hud != null) hud.ShowFps = s.showFps;
            var cam = CameraRig.Cam != null ? CameraRig.Cam : Camera.main;
            if (cam != null) cam.farClipPlane = QualityProfile.Current.fogEnd * 1.6f + 100f;
            s.Save();
        }

        public void SetPaused(bool p)
        {
            Paused = p;
            Time.timeScale = p ? 0f : 1f;
            if (p) Player?.CancelAim();
            InputHub.ClearAll();
        }

        public void HitStop(float seconds) => hitStopTimer = Mathf.Max(hitStopTimer, seconds);

        // ============================================================ FRAME
        Relics relics;
        Caves caves;
        Nightfall nightfall;
        Pois pois;
        Fishing fishing;
        Nests nests;
        Chests chests;
        AmbientLife ambient;
        Hazards hazards;
        Trader trader;
        Settlements settlements;
        Tamers tamers;
        Trails trails;
        Wardens wardens;
        Outposts outposts;
        LegendHunts hunts;
        WorldEvents worldEvents;
        System.Action caveAction;

        // ============================================================ KIN DEX
        /// <summary>A wild kin came into view range for the first time this spawn.</summary>
        public void OnKinSeen(CreatureActor a)
        {
            var e = Player.Stats.DexFor(a.Sp.id, true);
            bool first = e.seen == 0;
            e.seen++;
            var p = new Vector2(a.Position.x, a.Position.z);
            bool close = false;
            foreach (var s in e.sightings) if ((s - p).sqrMagnitude < 70f * 70f) { close = true; break; }
            if (!close) { e.sightings.Add(p); if (e.sightings.Count > 12) e.sightings.RemoveAt(0); }
            if (first) GameEvents.Say($"New kin spotted: #{a.Sp.dexNumber:000} {a.Sp.displayName} (Dex)", new Color(0.6f, 0.95f, 1f));
        }

        public static void DexCaught(PlayerStats s, CreatureData d, bool count)
        {
            var e = s.DexFor(d.speciesId, true);
            if (count) e.caught++;
            if (e.seen == 0) e.seen = 1;
            d.EnsureAptitude();
            int best = d.apt[d.spec];
            if (best > e.bestScore) { e.bestScore = best; e.bestPath = d.spec; }
        }

        /// <summary>Saves from before the Dex: seed caught/seen and best quality from registered species and owned kin.</summary>
        void SyncDexFromRoster()
        {
            var s = Player.Stats;
            foreach (var id in s.registeredSpecies)
            {
                var e = s.DexFor(id, true);
                if (e.seen == 0) e.seen = 1;
                if (e.caught == 0) e.caught = 1;
            }
            foreach (var d in Party.All) if (d.Species != null) DexCaught(s, d, false);
        }

        void Update()
        {
            InputHub.BeginFrame();
            float dt = Time.deltaTime;

            if (!mapReady) mapReady = MapTexture.Poll();

            // hit-stop (unscaled)
            if (!Paused)
            {
                if (hitStopTimer > 0f) { hitStopTimer -= Time.unscaledDeltaTime; Time.timeScale = 0.05f; }
                else if (Time.timeScale != 1f) Time.timeScale = 1f;
            }

            if (!Paused)
            {
                WorldTime += dt;
                HandleGlobalInput();

                if (deathTimer >= 0f)
                {
                    deathTimer -= Time.unscaledDeltaTime;
                    if (deathTimer < 0f) Respawn();
                }

                Player.Tick(dt);
                Capture.Tick(dt);
                projectiles.Tick(dt);
                Decoy.TickAll(dt);
                Parade.Tick(dt);
                spawner.Tick(dt, Player.Position, CameraRig.Cam);
                baseMgr.Tick(dt, Player.Position);
                placement.Tick(dt);
                waystones.Tick(Player.Position, dt);
                relics.Tick(dt, Player);
                SubRegions.Tick(dt, Player);
                nightfall.Tick(dt, Player);
                hazards.Tick(dt, Player);
                pois.Tick(dt, Player);
                fishing.Tick(dt, Player);
                nests.Tick(dt, Player);
                chests.Tick(dt, Player);
                ambient.Tick(dt, Player);
                caves.Tick(dt, Player);
                TickProgression(dt);
                trader.Tick(dt, Player);
                settlements.Tick(dt, Player);
                tamers.Tick(dt, Player);
                trails.Tick(dt, Player.Position);
                wardens.Tick(dt, Player);
                outposts.Tick(dt, Player);
                hunts.Tick(dt, Player);
                worldEvents.Tick(dt, Player);
                Party.TickRest(dt);
                UpdateInteract();
                UpdateGoals();

                if (pendingSummon != null && chunks.Ready) { Party.Summon(pendingSummon); pendingSummon = null; }

                PlaySeconds += Time.unscaledDeltaTime;
                autosaveTimer += Time.unscaledDeltaTime;
                if (autosaveTimer >= autosaveInterval) { autosaveTimer = 0f; SaveSystem.Save(); }
            }

            // world streaming & presentation keep running while paused so the menu backdrop is correct
            CameraRig.Tick(Player.Position, Mathf.Max(dt, Paused ? 0f : 0.0001f));
            chunks.Tick(Player.Position, CameraRig.Cam);
            env.Tick(Player.Position, CameraRig.Cam, dt);
            Landmarks.Cull(Player.Position, QualityProfile.Current.fogEnd * 2.2f);
            hud.Tick(dt);

            InputHub.EndFrame();
        }

        void HandleGlobalInput()
        {
            if (InputHub.Down(Btn.Menu)) { menu.Open(MenuTab.Bag); return; }
            if (InputHub.Down(Btn.Map)) { menu.Open(MenuTab.Map); return; }
            if (InputHub.Down(Btn.Build) && !placement.Active) { menu.Open(MenuTab.Build); return; }
            if (InputHub.Down(Btn.Summon)) Party.ToggleCompanion();
            if (InputHub.Down(Btn.Stance)) Party.CycleStance();
            if (InputHub.Down(Btn.KinPanel)) hud.ToggleKinPanel();
            for (int i = 0; i < PlayerStats.HotbarSize; i++)
                if (InputHub.Down(Btn.Hot1 + i) && !string.IsNullOrEmpty(Player.Stats.hotbar[i])) UseHotbar(i);
            if (InputHub.Down(Btn.CycleSeed)) Capture.CycleSeed(Player.Stats);
            if (InputHub.Down(Btn.KinSkill))
            {
                var c = Party.Companion;
                var t = Player.CombatTarget;
                if (c == null) GameEvents.Warn("Summon a kin first (KIN button).");
                else if (t == null) GameEvents.Warn("No target in range.");
                else if (!c.CommandAttack(t)) GameEvents.Say($"{c.Data.DisplayName}'s skills are recharging.");
                else Quests.Note("skill");
            }
            if (InputHub.Down(Btn.Interact))
            {
                if (caveAction != null) caveAction();
                else if (interactStone != null) waystones.Attune(interactStone);
                else if (interactBuilding != null) Interact(interactBuilding);
            }
        }

        /// <summary>Skill bar button: companion uses that skill on your target (or the nearest wild kin close by).</summary>
        public void UseKinSkill(int slot)
        {
            var c = Party.Companion;
            if (c == null || !c.IsAlive) { GameEvents.Warn("Summon a kin first (KIN button)."); return; }
            var t = (ICombatant)Player.CombatTarget ?? CreatureRegistry.FindNearestWild(Player.Position, 18f);
            // riding it: it uses the skill from under you (you steer, it doesn't run in)
            string why = Player.IsMounted && Player.Mount == c ? c.MountedSkill(slot, t) : c.CommandSkill(slot, t);
            if (why != null) GameEvents.Say(why);
            else Quests.Note("skill");
        }

        // ============================================================ BOND / BOUNTIES / EXPEDITIONS
        float bondTimer, progressTimer;
        bool bountiesReady;

        // ============================================================ PROVISION POUCH (auto-feed)
        public const string PouchId = "provision_pouch";
        const float AutoEatBelow = 35f, AutoTreatBelow = 0.3f;
        float autoFeedTimer, autoTreatCd;

        void TickAutoFeed(float dt)
        {
            autoTreatCd -= dt;
            autoFeedTimer -= dt;
            if (autoFeedTimer > 0f) return;
            autoFeedTimer = 1f;
            var s = Player.Stats;
            if (!Player.IsAlive || s.Bag.Count(PouchId) <= 0) return;
            if (s.autoFeed)
            {
                // you: the biggest food that still fits the gap, so nothing is wasted
                if (s.hunger <= AutoEatBelow)
                {
                    ItemDef best = null;
                    float gap = 100f - s.hunger;
                    var seen = new HashSet<string>();
                    foreach (var st in s.Bag.slots)
                    {
                        if (st.Empty || !seen.Add(st.id)) continue;
                        var f = ContentDB.Item(st.id);
                        if (f == null || f.category != ItemCategory.Food || f.foodValue <= 0f || f.foodValue > gap) continue;
                        if (f.mealMods != null && f.mealMods.Length > 0) continue;   // meals are for when you choose
                        if (best == null || f.foodValue > best.foodValue) best = f;
                    }
                    if (best != null) { UseItem(best.id); hud.Feed($"Pouch: ate {best.displayName}", new Color(0.7f, 1f, 0.75f)); }
                }
                // camp kin: hand-fed from storage, then your bag, while you're in camp (no Feed Box needed)
                if (baseMgr.HasBase && baseMgr.IsInsideBase(Player.Position, 4f))
                {
                    foreach (var k in Party.In(KinLocation.Base))
                    {
                        if (k.hunger >= 30f) continue;
                        if (baseMgr.HasFeedBox && baseMgr.HasFood) continue;   // it walks to the Feed Box itself
                        if (baseMgr.EatFood(k)) continue;
                        string food = s.Bag.Count("berry") > 0 ? "berry" : (s.Bag.Count("berry_tart") > 0 ? "berry_tart" : null);
                        if (food == null) break;
                        s.Bag.Remove(food, 1);
                        k.hunger = Mathf.Min(100f, k.hunger + BaseManager.KinFood(ContentDB.Item(food)));
                        hud.Feed($"Pouch: fed {k.DisplayName}", new Color(1f, 0.7f, 0.85f));
                        GameEvents.RaiseInventoryChanged();
                    }
                }
            }
            // companion: a Kin Treat when it is badly hurt
            var c = Party.Companion;
            if (s.autoTreat && autoTreatCd <= 0f && c != null && c.IsAlive && c.Data.CurrentHp < c.Data.MaxHp * AutoTreatBelow && s.Bag.Count("kin_treat") > 0)
            {
                autoTreatCd = 20f;
                UseItem("kin_treat");
                hud.Feed($"Pouch: Kin Treat for {c.Data.DisplayName}", new Color(1f, 0.85f, 0.45f));
            }
        }

        int conditionMask = -1;
        float conditionTimer;

        /// <summary>Meal bonuses run out; and when rain, a heatwave, snow, dawn or dusk begins, say which local kin it brings out.</summary>
        void TickMealAndConditions(float dt)
        {
            var s = Player.Stats;
            if (!string.IsNullOrEmpty(s.mealItem) && !s.MealActive)
            {
                GameEvents.Say($"Your {ContentDB.ItemName(s.mealItem)} bonus has worn off.");
                s.ClearMeal();
            }
            conditionTimer -= dt;
            if (conditionTimer > 0f || env == null) return;
            conditionTimer = 2f;
            int mask = 0;
            for (int c = 1; c <= (int)SpawnCondition.Dusk; c++) if (env.ConditionMet((SpawnCondition)c)) mask |= 1 << c;
            int started = conditionMask < 0 ? 0 : mask & ~conditionMask;
            conditionMask = mask;
            if (started == 0 || Caves.InCave) return;
            var biome = WorldGen.Biome(Player.Position.x, Player.Position.z);
            for (int c = 1; c <= (int)SpawnCondition.Dusk; c++)
            {
                if ((started & (1 << c)) == 0) continue;
                var names = new List<string>();
                foreach (var sp in ContentDB.SpeciesList)
                    if (sp.spawnCondition == (SpawnCondition)c && sp.LivesIn(biome)) names.Add(s.DexFor(sp.id, false) != null ? sp.displayName : "???");
                if (names.Count > 0)
                    GameEvents.Say($"Some kin only come out {EnvironmentSystem.ConditionText((SpawnCondition)c)}: {string.Join(", ", names)}.", new Color(0.6f, 0.95f, 1f));
            }
        }

        void TickProgression(float dt)
        {
            TickAutoFeed(dt);
            TickMealAndConditions(dt);
            if (!bountiesReady) { bountiesReady = true; Bounties.Ensure(); }
            // summoned partners bond over time
            var c = Party.Companion;
            if (c != null && c.IsAlive)
            {
                bondTimer += dt;
                if (bondTimer >= 90f) { bondTimer = 0f; AddBond(c.Data, 1f); }
            }
            progressTimer -= dt;
            if (progressTimer <= 0f) { progressTimer = 2f; Expeditions.Tick(); }
        }

        public void AddBond(CreatureData d, float amount)
        {
            if (d == null || amount <= 0f) return;
            int before = d.BondLevel;
            d.bond = Mathf.Min(CreatureData.BondThresholds[CreatureData.MaxBond] + 50f, d.bond + amount);
            if (d.BondLevel > before)
            {
                GameEvents.Good($"Bond with {d.DisplayName} reached Lv {d.BondLevel}! {CreatureData.BondPerks(d.BondLevel)}");
                SynthAudio.Play(Sfx.LevelUp, 1.3f, 0.7f);
            }
        }

        /// <summary>PET your summoned partner when standing right next to it (lowest-priority prompt).</summary>
        // ============================================================ SLEEP (a bed in your house)
        Housing housing;
        /// <summary>A bed can be slept in from this time of day (evening) until dawn.</summary>
        public const float SleepFrom = 0.68f, WakeAt = 0.25f;
        public const float RestedXp = 1.1f, RestedMinutes = 10f;
        public bool Sleeping { get; private set; }

        void TrySleep(Housing.Piece bed)
        {
            var env = EnvironmentSystem.I;
            if (env == null || Sleeping) return;
            if (!(env.IsNight || env.timeOfDay >= SleepFrom)) { GameEvents.Say("Not sleepy yet - you can sleep here from evening until dawn."); return; }
            if (!housing.Indoors(bed.pos)) { GameEvents.Warn("You need a roof (or a floor) over the bed to sleep in it."); return; }
            if (Wardens.I != null && Wardens.I.Active != null) { GameEvents.Warn("Not while a Warden is awake!"); return; }
            foreach (var a in CreatureRegistry.All)
                if (a != null && a.IsAlive && a.Mode == CreatureMode.Wild && a.Aggro && (a.Position - Player.Position).sqrMagnitude < 30f * 30f) { GameEvents.Warn("You can't sleep with wild kin after you."); return; }
            StartCoroutine(SleepRoutine(bed));
        }

        System.Collections.IEnumerator SleepRoutine(Housing.Piece bed)
        {
            Sleeping = true;
            var env = EnvironmentSystem.I;
            if (Player.IsMounted) Player.Dismount(true);
            Party.Recall(false);
            InputHub.ClearAll();
            SynthAudio.Play(Sfx.Recall, 0.7f, 0.6f);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.9f) { hud.SetFade(t); yield return null; }
            hud.SetFade(1f);
            // the night passes: the clock jumps to dawn and the camp catches up as if you had been away that long
            float dayLen = Mathf.Max(60f, env.DayLength);
            float skip = Mathf.Repeat(WakeAt - env.timeOfDay, 1f) * dayLen;
            env.timeOfDay = WakeAt;
            WorldTime += skip;
            baseMgr.SimulateOffline(skip, slept: true);
            var s = Player.Stats;
            s.hp = s.MaxHp; s.stamina = s.MaxStamina;
            s.hunger = Mathf.Max(5f, s.hunger - 15f);
            s.restedUntil = WorldTime + RestedMinutes * 60.0;
            Player.Status = default;
            yield return new WaitForSecondsRealtime(0.6f);
            for (float t = 1f; t > 0f; t -= Time.unscaledDeltaTime / 1.2f) { hud.SetFade(t); yield return null; }
            hud.SetFade(0f);
            GameEvents.Good($"Good morning! You slept through the night. Well rested: +10% XP for {RestedMinutes:0} minutes.");
            SynthAudio.Play(Sfx.LevelUp, 1.3f, 0.6f);
            Quests.Note("sleep");
            Sleeping = false;
            SaveSystem.Save();
        }

        void TryPetPrompt()
        {
            var c = Party.Companion;
            if (c == null || !c.IsAlive || c.Mounted || Player.Speed > 0.4f) return;
            if ((c.Position - Player.Position).sqrMagnitude > 2.4f * 2.4f) return;
            InteractPrompt = "PET";
            caveAction = () => PetCompanion(c);
        }

        void PetCompanion(CreatureActor c)
        {
            var d = c.Data;
            c.Rig.Squash();
            c.Rig.Lunge();
            Fx.Sparkle(c.Position + Vector3.up * (c.Radius * 2f), new Color(1f, 0.55f, 0.75f), 18, 0.8f);
            Fx.Burst(c.Position + Vector3.up * (c.Radius * 1.6f), new Color(1f, 0.45f, 0.65f), 10, 2.2f, 0.26f, 0.9f);
            Fx.Ring(c.Position, new Color(1f, 0.6f, 0.8f), c.Radius + 1f, 16);
            Quests.Note("pet");
            SynthAudio.Play(Sfx.Pluck, 1.4f, 0.6f);
            if (Time.time - d.lastPet < 60f) { GameEvents.Say($"{d.DisplayName} nuzzles you happily."); return; }
            d.lastPet = Time.time;
            AddBond(d, 3f);
            GameEvents.Say($"{d.DisplayName} loves that! (Bond {d.bond:0}/{NextBond(d)})", new Color(1f, 0.7f, 0.85f));
        }

        public static string NextBond(CreatureData d) => d.BondLevel >= CreatureData.MaxBond ? "MAX" : CreatureData.BondThresholds[d.BondLevel + 1].ToString("0");

        /// <summary>Feed a kin a treat from the bag or camp storage (Kin tab).</summary>
        public void FeedKin(CreatureData d)
        {
            if (Time.time - d.lastFeed < 45f) { GameEvents.Say($"{d.DisplayName} is still full."); return; }
            string food = ItemSources.Count("berry_tart") > 0 ? "berry_tart" : (ItemSources.Count("berry") > 0 ? "berry" : null);
            if (food == null) { GameEvents.Warn("No Sunberries or Berry Tarts to share."); return; }
            ItemSources.Consume(new[] { new Ingredient(food, 1) });
            d.lastFeed = Time.time;
            AddBond(d, food == "berry_tart" ? 10f : 4f);
            GameEvents.Say($"{d.DisplayName} enjoyed the {ContentDB.ItemName(food)}! (Bond {d.bond:0}/{NextBond(d)})", new Color(1f, 0.7f, 0.85f));
            SynthAudio.Play(Sfx.Pickup, 1.2f);
            GameEvents.RaiseInventoryChanged();
        }

        // ============================================================ INTERACTION
        void UpdateInteract()
        {
            interactBuilding = null;
            interactStone = null;
            InteractPrompt = null;
            caveAction = null;
            ManageTarget = null;
            if (placement.Active || !Player.IsAlive) return;
            if (baseMgr.HasBase && !Caves.InCave) ManageTarget = baseMgr.NearestInteractable(Player.Position, 2.2f);   // the HUD's MANAGE button
            // caves: ENTER at a mouth, LEAVE at a portal, OPEN chests
            var cavePrompt = caves.Prompt(Player.Position, out caveAction);
            if (cavePrompt != null) { InteractPrompt = cavePrompt; return; }
            if (Caves.InCave) { TryPetPrompt(); return; }
            if (fishing.Active) { InteractPrompt = fishing.Prompt(Player, out var reel); caveAction = reel; return; }   // a line is out: nothing else takes the button
            var nestPrompt = nests.Prompt(Player.Position, out var nestAction);
            if (nestPrompt != null) { InteractPrompt = nestPrompt; caveAction = nestAction; return; }
            var poiPrompt = pois.Prompt(Player.Position, out var poiAction);
            if (poiPrompt != null) { InteractPrompt = poiPrompt; caveAction = poiAction; return; }
            var chestPrompt = chests.Prompt(Player.Position, out var chestAction);
            if (chestPrompt != null) { InteractPrompt = chestPrompt; caveAction = chestAction; return; }
            if (trader.InReach(Player.Position)) { InteractPrompt = "TRADE"; caveAction = () => menu.Open(MenuTab.Trader); return; }
            var kinkeeper = settlements.NearKeeper(Player.Position);
            if (kinkeeper != null) { InteractPrompt = "KIN SHOP"; caveAction = () => menu.OpenKinShop(kinkeeper); return; }
            var board = settlements.NearBoard(Player.Position);
            if (board != null) { InteractPrompt = "REQUESTS"; caveAction = () => menu.OpenRequests(board); return; }
            var rival = tamers.InReach(Player.Position);
            if (rival != null && tamers.Active == null) { InteractPrompt = "CHALLENGE"; caveAction = () => menu.OpenTamer(rival); return; }
            var huntPrompt = hunts.Prompt(Player.Position, out var huntAction);
            if (huntPrompt != null) { InteractPrompt = huntPrompt; caveAction = huntAction; return; }
            var fishPrompt = fishing.Prompt(Player, out var fishAction);
            if (fishPrompt != null) { InteractPrompt = fishPrompt; caveAction = fishAction; return; }
            var post = outposts.InReach(Player.Position);
            if (post != null) { InteractPrompt = "OUTPOST"; caveAction = () => menu.OpenOutpost(post); return; }
            var shrine = wardens.InReach(Player.Position);
            if (shrine != null && wardens.Active == null) { InteractPrompt = "WARDEN"; caveAction = () => menu.OpenWarden(shrine); return; }
            var villager = settlements.NearVillager(Player.Position, out var village);
            if (villager != null) { InteractPrompt = "TALK"; caveAction = () => settlements.Talk(villager, village); return; }
            var stone = waystones.NearestInRange(Player.Position);
            if (stone != null)
            {
                interactStone = stone;
                InteractPrompt = waystones.IsActive(stone) ? "TRAVEL" : "ATTUNE";
                return;
            }
            // a construction site: BUILD to help put it up (BaseManager.Construction)
            var site = baseMgr.HasBase ? baseMgr.SiteNear(Player.Position, 1.8f) : null;
            if (site != null)
            {
                InteractPrompt = (baseMgr.PlayerBuildSite == site ? "STOP" : "BUILD") + $"\n{Mathf.RoundToInt(site.Progress01 * 100f)}%";
                caveAction = () => baseMgr.TogglePlayerBuild(site);
                return;
            }
            // your house: a bed to sleep in, a wardrobe to change your look (Housing)
            var bed = housing.BedNear(Player.Position, 1.4f);
            if (bed != null)
            {
                var env0 = EnvironmentSystem.I;
                bool late = env0 != null && (env0.IsNight || env0.timeOfDay >= SleepFrom);
                InteractPrompt = late ? "SLEEP" : "BED";
                caveAction = () => TrySleep(bed);
                return;
            }
            var wardrobe = housing.WardrobeNear(Player.Position, 1.4f);
            if (wardrobe != null) { InteractPrompt = "CHANGE\nLOOK"; caveAction = () => menu.OpenLook(); return; }
            // a camp kin that wants something from you (BaseManager.Upgrades): answered before the structure behind it
            var wanting = baseMgr.HasBase ? baseMgr.WantNear(Player.Position, 2.4f) : null;
            if (wanting != null) { InteractPrompt = BaseManager.WantPrompt(wanting.Data.campWant); caveAction = () => baseMgr.AnswerWant(wanting); return; }
            var b = baseMgr.NearestInteractable(Player.Position, 2.2f);
            if (b == null) { TryPetPrompt(); return; }
            interactBuilding = b;
            if (b.BufferCount > 0) InteractPrompt = $"COLLECT {b.BufferCount}";
            else if (baseMgr.PlayerWorkTarget == b) InteractPrompt = $"STOP\n{Mathf.RoundToInt(b.Progress01 * 100f)}%";
            else if (b.IsProducer && b.Waiting) InteractPrompt = $"{b.Cur.doing.ToUpperInvariant()}\n{WorkStages.Clock(b.SecondsLeft)}";
            else if (b.IsProducer) InteractPrompt = Crops.IsPlot(b) ? (b.Stage == 0 ? "PLANT" : b.Stage == WorkStages.CropWater ? "WATER" : "HARVEST") : "WORK";
            else if (b.Def.isCore) InteractPrompt = "CAMP";
            else if (b.Def.storageSlots > 0) InteractPrompt = "OPEN";
            else if (b.Def.feeds) InteractPrompt = "FEED BOX";
            else if (b.Def.id == "hatchery") InteractPrompt = KinGrowth.FirstReady() != null ? "HATCH!" : "HATCHERY";
            else if (b.Def.id == HealingSpring) InteractPrompt = Party.AnyPartyKinHurt() ? "HEAL\nPARTY" : "SPRING";
            else if (b.IsStation) InteractPrompt = MenuUI.StationVerb(b.Def.station);
            else InteractPrompt = "INFO";
        }

        void Interact(Building b)
        {
            if (b.BufferCount > 0) { CollectBuffer(b); return; }
            if (b.IsProducer) { baseMgr.TogglePlayerWork(b); return; }
            if (b.IsStation) { menu.OpenStation(b.Def.station); return; }
            if (b.Def.feeds || (b.BoxLen > 0 && !b.Def.isCore)) { menu.OpenBox(b); return; }   // a chest or a feed box: its own contents
            if (b.Def.id == "hatchery") { menu.OpenHatchery(); return; }   // a ready egg hatches right here
            if (b.Def.id == HealingSpring) { HealAtSpring(b); return; }
            menu.Open(MenuTab.Camp);
        }

        public const string HealingSpring = "healing_spring";

        /// <summary>HEAL PARTY at a Healing Spring: the whole party back to full, as often as you like - the trip home is the cost.</summary>
        void HealAtSpring(Building b)
        {
            Vector3 at = b.transform.position + Vector3.up * 0.6f;
            if (!Party.AnyPartyKinHurt() && (Party.Companion == null || !Party.Companion.Status.Active))
            {
                GameEvents.Say("Your party is already in full health.", new Color(0.7f, 0.95f, 1f));
                Fx.Sparkle(at, new Color(0.6f, 1f, 0.95f), 6, 0.6f);
                return;
            }
            int n = Party.HealPartyFull();
            Fx.Ring(b.transform.position + Vector3.up * 0.1f, new Color(0.55f, 1f, 0.9f), 3.2f, 30);
            Fx.Burst(at, new Color(0.6f, 1f, 0.95f), 26, 4.5f, 0.24f, 0.8f, true, -2f);
            var c = Party.Companion;
            if (c != null && c.IsAlive)
            {
                Fx.Sparkle(c.Position + Vector3.up * c.Radius, new Color(0.7f, 1f, 0.8f), 16, c.Radius + 0.4f);
                Fx.Ring(c.Position + Vector3.up * 0.1f, new Color(0.55f, 1f, 0.75f), c.Radius + 1.2f, 20);
                c.Rig.Squash();
            }
            SynthAudio.Play(Sfx.Summon, 1.25f, 0.8f);
            GameEvents.Good(n == 1 ? "The spring heals 1 kin in your party." : $"The spring heals {n} kin in your party - all back to full.");
        }

        public void CollectBuffer(Building b)
        {
            if (b.BufferCount <= 0) return;
            string item = b.BufferItem;
            int n = b.TakeFromBuffer(b.BufferCount, out _);
            int left = Player.Stats.Bag.Add(item, n);
            if (left > 0) { b.ReturnToBuffer(item, left); GameEvents.Warn("Bag full."); }
            if (n - left > 0)
            {
                hud.Feed($"+{n - left} {ContentDB.ItemName(item)}", Color.white);
                SynthAudio.Play(Sfx.Pickup);
            }
            GameEvents.RaiseInventoryChanged();
        }

        public void TakeAllFromStorage()
        {
            var st = baseMgr.Storage;
            foreach (var s in st.slots)
            {
                if (s.Empty) continue;
                int left = Player.Stats.Bag.Add(s.id, s.count);
                s.count = left;
                if (left <= 0) s.id = "";
            }
            GameEvents.RaiseInventoryChanged();
        }

        public void DepositResources()
        {
            var bag = Player.Stats.Bag;
            foreach (var s in bag.slots)
            {
                if (s.Empty) continue;
                var d = ContentDB.Item(s.id);
                if (d == null || (d.category != ItemCategory.Resource && d.category != ItemCategory.Food)) continue;
                int left = baseMgr.Storage.Add(s.id, s.count);
                s.count = left;
                if (left <= 0) s.id = "";
            }
            GameEvents.RaiseInventoryChanged();
        }

        // ============================================================ ITEMS / XP
        public void GiveItem(string id, int count, Vector3 at)
        {
            if (count <= 0) return;
            int left = Player.Stats.Bag.Add(id, count);
            int got = count - left;
            if (got > 0) ExplorerLog.OnItem(Player.Stats, id);
            if (got > 0 && Player.Stats.AutoHotbar(id))
            {
                // first tool/weapon of its kind goes to the hotbar; weapons go straight into your hand if it's empty
                var def = ContentDB.Item(id);
                if (def.category == ItemCategory.Weapon && string.IsNullOrEmpty(Player.Stats.held)) Player.Stats.Hold(id);
                GameEvents.Say($"{def.displayName} added to your hotbar", new Color(0.7f, 1f, 0.8f));
            }
            if (got > 0)
            {
                var d = ContentDB.Item(id);
                hud.Feed($"+{got} {ContentDB.ItemName(id)}", d != null ? Color.Lerp(d.iconColor, Color.white, 0.4f) : Color.white);
                SynthAudio.Play(Sfx.Pickup, 1f + Random.Range(-0.05f, 0.1f), 0.5f);
            }
            if (left > 0) GameEvents.Warn("Bag full! Build storage or drop items.");
            GameEvents.RaiseInventoryChanged();
        }

        /// <summary>A hotbar slot: a tool or weapon goes in (or out of) your hand; food or a heal is used at once.</summary>
        public void UseHotbar(int slot)
        {
            var s = Player.Stats;
            if (slot < 0 || slot >= s.hotbar.Length || string.IsNullOrEmpty(s.hotbar[slot])) return;
            string id = s.hotbar[slot];
            var d = ContentDB.Item(id);
            if (PlayerStats.IsUsable(d))
            {
                if (s.Bag.Count(id) <= 0) { GameEvents.Warn($"No {d.displayName} left."); return; }
                UseItem(id);
            }
            else Player.ToggleHold(id);
        }

        public void UseItem(string id)
        {
            var d = ContentDB.Item(id);
            var s = Player.Stats;
            if (d == null || s.Bag.Count(id) <= 0) return;
            if (id == Outposts.KitItem) { outposts.TryFound(Player); return; }
            // don't waste food / medicine that would do nothing
            if (d.bonusInventorySlots <= 0)
            {
                // "full" means the bar looks full: hunger ticks down constantly, so a real gap is required before food is worth eating
                bool hungry = d.foodValue > 0f && 100f - s.hunger >= Mathf.Min(d.foodValue * 0.5f, 10f);
                bool hurt = d.healValue > 0f && s.MaxHp - s.hp >= Mathf.Min(d.healValue, s.MaxHp * 0.05f);
                bool kinHurt = d.kinHealValue > 0f && Party.AnyPartyKinHurt();
                // a meal is also worth eating for its bonus, unless that same bonus is already running
                bool wantMeal = d.mealMods != null && d.mealMods.Length > 0 && (s.mealItem != d.id || s.MealSecondsLeft < 60f);
                if (!hungry && !hurt && !kinHurt && !wantMeal)
                {
                    GameEvents.Say(d.kinHealValue > 0f ? "Your party kin are already healthy." :
                                   d.foodValue > 0f ? "You're full - no need to eat that right now." : "You're already at full health.");
                    return;
                }
            }
            bool used = false;
            if (d.foodValue > 0f) { s.hunger = Mathf.Min(100f, s.hunger + d.foodValue); used = true; }
            if (d.healValue > 0f) { Player.Heal(d.healValue); used = true; }
            if (d.mealMods != null && d.mealMods.Length > 0)
            {
                s.SetMeal(d);
                if (s.PoisonWarded && Player.Status.Active && Player.Status.type == StatusType.Poison) Player.Status = default(StatusState);   // the draught clears it
                var lines = new List<string>();
                foreach (var m in d.mealMods) lines.Add(GearInfo.Format(m));
                GameEvents.Good($"{d.displayName}: {string.Join(", ", lines)} for {d.mealMinutes:0} min.");
                used = true;
            }
            if (d.kinHealValue > 0f) { Party.HealParty(d.kinHealValue, true); used = true; GameEvents.Good("Party kin restored!"); }
            if (d.bonusInventorySlots > 0) { s.bonusSlots += d.bonusInventorySlots; s.Bag.Resize(s.BagSize); used = true; GameEvents.Good($"Bag expanded to {s.BagSize} slots!"); }
            if (!used) return;
            s.Bag.Remove(id, 1);
            Fx.Sparkle(Player.Position + Vector3.up, new Color(0.6f, 1f, 0.7f), 8);
            SynthAudio.Play(Sfx.Pickup, 0.8f);
            GameEvents.RaiseInventoryChanged();
        }

        public void AddPlayerXp(int amount)
        {
            amount = Mathf.Max(1, Mathf.RoundToInt(amount * WorldRules.Current.expRate * (Player.Stats.Rested ? RestedXp : 1f)));
            int lv = Player.Stats.AddXp(amount);
            if (lv > 0)
            {
                GameEvents.Good($"LEVEL UP! Explorer Lv {Player.Stats.level} - skill point earned (Menu > Skills)");
                Fx.Sparkle(Player.Position + Vector3.up, new Color(1f, 0.95f, 0.5f), 30, 1f);
                Fx.Ring(Player.Position, new Color(1f, 0.9f, 0.4f), 3f);
                SynthAudio.Play(Sfx.LevelUp);
                GameEvents.RaisePlayerLeveledUp(Player.Stats.level);
            }
        }

        /// <summary>Grants XP to one kin (level-up toast unless quiet). Also used by camp workers.</summary>
        public void GrantKinXp(CreatureData d, int amount, bool quiet = false)
        {
            if (d == null || amount <= 0) return;
            int slotsBefore = d.SkillSlots;
            int lv = d.AddXp(Mathf.Max(1, Mathf.RoundToInt(amount * WorldRules.Current.expRate * Player.Stats.KinXpMul * Nightfall.KinXpMul)));
            if (lv > 0)
            {
                if (d.SkillSlots > slotsBefore)
                {
                    var known = d.KnownSkills;
                    var learned = ContentDB.Ability(known[known.Length - 1]);
                    if (learned != null) GameEvents.Good($"{d.DisplayName} learned {learned.displayName}! ({known.Length}/{CreatureData.MaxSkills} skills)");
                    SynthAudio.Play(Sfx.LevelUp, 1.25f, 0.8f);
                }
                if (!quiet || d.location != KinLocation.Base) GameEvents.Good($"{d.DisplayName} grew to Lv {d.level}!");
                else GameEvents.Say($"{d.DisplayName} grew to Lv {d.level} at camp.", new Color(0.7f, 1f, 0.8f));
                var worker = BaseManager.I != null ? BaseManager.I.ActorFor(d) : null;
                if (worker != null) worker.RecomputeStats();
                if (Party.Companion != null && Party.Companion.Data == d)
                {
                    Party.Companion.RecomputeStats();
                    Fx.Sparkle(Party.Companion.Position + Vector3.up, new Color(0.6f, 1f, 1f), 16);
                }
            }
        }

        /// <summary>Party XP split: the summoned kin gets activeShare, everyone else in the party benchShare (fainted kin excluded).</summary>
        void ShareKinXp(int amount, float activeShare, float benchShare, string excludeUid)
        {
            var active = Party.Companion != null ? Party.Companion.Data : null;
            foreach (var k in Party.PartyList())
            {
                if (k.uid == excludeUid) continue;
                bool isActive = k == active;
                if (!isActive && k.Fainted) continue;
                GrantKinXp(k, Mathf.Max(1, Mathf.RoundToInt(amount * (isActive ? activeShare : benchShare))));
            }
        }

        public void OnWildDefeated(CreatureActor a, ICombatant killer)
        {
            var d = a.Data;
            int xp = xpPerDefeat + d.level * 3 + (d.alpha ? 80 : 0);
            AddPlayerXp(xp);
            ShareKinXp(xp, 1f, benchDefeatShare, null);
            if (a.Duelist) return;   // a tamer's kin: good practice, but no Dex entry, bounty credit or loot
            LegendHunts.OnDefeat(a);
            float dexDrops = DexRewards.DropMul(Player.Stats, a.Sp.id) * Nightfall.DropMul;   // read before this defeat counts
            Player.Stats.DexFor(a.Sp.id, true).defeated++;
            Bounties.OnDefeat(d);
            Requests.OnDefeat(d);
            if (Party.Companion != null && Party.Companion.IsAlive) AddBond(Party.Companion.Data, 2f);
            int lootMul = Player.Stats.HasEffect(UniqueEffect.Magpie) && Random.value < 0.25f ? 2 : 1;
            foreach (var drop in a.Sp.drops)
                if (Random.value <= drop.chance * (d.alpha ? 2f : 1f))
                    GiveItem(drop.itemId, Mathf.RoundToInt(Random.Range(drop.min, drop.max + 1) * (d.alpha ? 3 : 1) * lootMul * dexDrops), a.Position);
            if (lootMul > 1) HUD.DamageNumber(a.Position + Vector3.up * 2f, "MAGPIE x2", new Color(1f, 0.85f, 0.4f), 0.7f);
            if (d.alpha)
            {
                DropAlphaTrophy(a);
                if (Random.value < 0.4f) GiveItem(Chests.KeyItem, 1, a.Position);   // alphas guard keys to Gilded chests
                spawner.OnAlphaGone(a);
                GameEvents.Good($"Alpha {a.Sp.displayName} defeated!");
            }
            MarkGoal("defeat");
        }

        /// <summary>Alphas drop a unique accessory: guaranteed the first time per species, 25% afterwards.
        /// Prefers trophies the player doesn't own yet.</summary>
        void DropAlphaTrophy(CreatureActor a)
        {
            var s = Player.Stats;
            bool first = !s.alphaTrophies.Contains(a.Sp.id);
            if (!first && Random.value > 0.25f) return;
            var pool = ContentSeed.AlphaTrophyPool(a.Data.Element1);
            var fresh = new System.Collections.Generic.List<string>();
            foreach (var id in pool) if (ContentDB.Item(id) != null && !s.Owns(id)) fresh.Add(id);
            string pick = fresh.Count > 0 ? fresh[Random.Range(0, fresh.Count)] : pool[Random.Range(0, pool.Length)];
            if (ContentDB.Item(pick) == null) return;
            if (first) s.alphaTrophies.Add(a.Sp.id);
            GiveItem(pick, 1, a.Position + Vector3.up);
            GameEvents.Good($"Alpha trophy: {ContentDB.ItemName(pick)}! Equip it from MENU > Gear.");
            SynthAudio.Play(Sfx.LevelUp, 1.2f);
        }

        /// <summary>A Hatchery egg hatched: same bookkeeping as a capture, without the capture XP.</summary>
        /// <summary>A kin bought from a settlement's Kinkeeper joins the roster (and the Dex) like any other.</summary>
        /// <summary>The kin maker's BRING IT TO LIFE: saves the design, makes its species and hands over one of them.</summary>
        public CreatureData CreateCustomKin(CustomKin design)
        {
            if (design == null) return null;
            var k = design.Clone();
            k.id = "custom_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
            Player.Stats.customKin.Add(k);
            var sp = ContentSeed.BuildCustom(k);
            ContentDB.RegisterCustom(sp);
            if (RenderSettings.sun != null) ItemIcons.RenderAll(RenderSettings.sun);   // its portrait
            int best = 1;
            foreach (var loc in new[] { KinLocation.Party, KinLocation.Base, KinLocation.Box }) foreach (var o in Party.In(loc)) best = Mathf.Max(best, o.level);
            var d = CreatureFactory.RollWild(sp, Mathf.Max(1, best - 2), BiomeId.Meadow, false);
            d.shimmer = false; d.alpha = false; d.regional = false; d.mutation = Mutation.None;
            d.look = new KinLook { rolled = true };   // exactly as it was drawn
            d.capturedAt = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Confetti(Player.Position + Vector3.up * 1.2f);
            OnKinBought(d, "dream");
            return d;
        }

        public void OnKinBought(CreatureData d, string seller)
        {
            var s = Player.Stats;
            if (!s.registeredSpecies.Contains(d.speciesId)) s.registeredSpecies.Add(d.speciesId);
            DexCaught(s, d, true);
            Party.AddCaptured(d);
            GameEvents.Good($"{d.RegionalName} is yours! {(d.location == KinLocation.Party ? "It joined your party." : "It's in the Hearth Box.")}");
            GameEvents.Say($"Quality: {Aptitude.Summary(d, false)}", Aptitude.GradeColor(d.apt[d.spec]));
        }

        public void OnKinHatched(CreatureData d, bool quiet = false)
        {
            var s = Player.Stats;
            bool isNew = !s.registeredSpecies.Contains(d.speciesId);
            if (isNew) s.registeredSpecies.Add(d.speciesId);
            DexCaught(s, d, true);
            Party.AddCaptured(d);
            AddPlayerXp(20);
            Quests.Note("hatch");
            if (quiet) return;   // the hatching show (MenuUI.Hatch) says all of this itself
            GameEvents.Good($"{d.RegionalName} hatched! {(d.location == KinLocation.Party ? "It joined your party." : "It's in the Hearth Box.")}");
            GameEvents.Say($"Quality: {Aptitude.Summary(d, false)}", Aptitude.GradeColor(d.apt[d.spec]));
            string tags = d.VariantTags();
            if (tags.Length > 0) GameEvents.Say(tags, new Color(1f, 0.85f, 0.4f));
            SynthAudio.Play(Sfx.CaptureSuccess, 1.2f);
        }

        public void OnKinCaptured(CreatureData d)
        {
            var s = Player.Stats;
            bool isNew = !s.registeredSpecies.Contains(d.speciesId);
            if (isNew) s.registeredSpecies.Add(d.speciesId);
            DexCaught(s, d, true);
            Bounties.OnCapture(d);
            Requests.OnCapture(d);
            Party.AddCaptured(d);
            LegendHunts.OnCapture(d);
            int xp = (xpPerCapture + d.level * 4) * (isNew ? 2 : 1) + (d.alpha ? 120 : 0);
            AddPlayerXp(xp);
            ShareKinXp(xp, captureShare, captureShare, d.uid);
            string where = d.location == KinLocation.Party ? "joined your party" : "was sent to the Hearth Box";
            GameEvents.Good($"Captured {d.RegionalName} Lv {d.level}! It {where}.");
            string tags = d.VariantTags();
            if (tags.Length > 0) GameEvents.Say(tags, new Color(1f, 0.85f, 0.4f));
            if (isNew) GameEvents.Say($"New kin registered: #{d.Species.dexNumber:000} {d.Species.displayName}", new Color(0.5f, 1f, 1f));
            if (isNew && WorldRules.Current.simpleControls)
            {
                // sticker book worlds: every new kind is an event
                Confetti(Player.Position + Vector3.up * 1.4f);
                GameEvents.Good($"NEW STICKER!  {d.Species.displayName}");
            }
            GameEvents.Say($"Quality: {Aptitude.Summary(d, false)}", Aptitude.GradeColor(d.apt[d.spec]));
            MarkGoal("capture");
            GameEvents.RaiseKinCaptured(d);
        }

        // ============================================================ DEATH
        public void OnPlayerDied()
        {
            deathTimer = 3f;
            hud.ShowDeath(true);
            Party.Recall(false);
            Capture.Cancel();
            SynthAudio.Play(Sfx.CaptureFail, 0.6f);
        }

        void Respawn()
        {
            hud.ShowDeath(false);
            if (caves.CheckpointRespawn(out var checkpoint))
            {
                // a deep cave's lower level: back up by the rope
                projectiles.ClearAll();
                Player.Revive(checkpoint);
                CameraRig.SnapTo(checkpoint);
                GameEvents.Say("You come round at the checkpoint, by the rope.", new Color(0.6f, 1f, 0.75f));
                return;
            }
            caves.Abort();   // fainting underground drags you back out
            tamers.Abort();
            wardens.Abort();
            hunts.Abort();
            Vector3 at = baseMgr.HasBase ? baseMgr.CorePosition + new Vector3(3f, 0, 3f) : WorldGen.StartExit;
            spawner.DespawnAllWild();
            projectiles.ClearAll();
            Player.Revive(at);
            CameraRig.SnapTo(at);
            GameEvents.Say("You wake up by the fire...", new Color(1f, 0.8f, 0.6f));
        }

        // ============================================================ QUEST LINE ON THE HUD
        // The goal panel shows the tracked quest (Core/Quests.cs) and its next objective; tap it to open the QUESTS tab.
        // GoalsDone / MarkGoal are the old onboarding flags: 'capture' and 'summon' are still read by the first quests.
        void BuildGoalLabel(Canvas c)
        {
            var p = UIFactory.Panel(c.transform, "Goal", new Color(0.08f, 0.1f, 0.14f, 0.7f));
            p.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -310), new Vector2(470, 90));
            p.raycastTarget = true;
            var tap = p.gameObject.AddComponent<Button>();
            tap.transition = Selectable.Transition.None;
            tap.onClick.AddListener(() => { if (MenuUI.I != null && !MenuUI.I.IsOpen) MenuUI.I.Open(MenuTab.Quests); });
            p.transform.SetSiblingIndex(1); // above the HUD, below the menu
            goalText = UIFactory.Label(p.transform, "", 23, new Color(1f, 0.92f, 0.6f), TextAnchor.MiddleLeft);
            goalText.supportRichText = true;
            goalText.raycastTarget = false;
            goalText.rectTransform.Stretch(14, 6, 14, 6);
            // for players who can't read yet: a picture of what the goal is about, and an arrow that points at it
            goalIcon = UIFactory.Rect(p.transform, "Pic").gameObject.AddComponent<Image>();
            goalIcon.preserveAspect = true; goalIcon.raycastTarget = false;
            goalIcon.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(80, 80));
            goalIcon.gameObject.SetActive(false);
            goalArrow = UIFactory.Label(p.transform, "^", 64, Quests.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            goalArrow.raycastTarget = false;
            goalArrow.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-42, 0), new Vector2(70, 70));
            goalArrow.gameObject.SetActive(false);
        }

        /// <summary>A burst of coloured paper: stars earned, new stickers.</summary>
        public void Confetti(Vector3 at)
        {
            Color[] cols = { new Color(1f, 0.35f, 0.4f), new Color(1f, 0.85f, 0.3f), new Color(0.4f, 0.9f, 0.5f), new Color(0.4f, 0.7f, 1f), new Color(0.85f, 0.5f, 1f) };
            foreach (var c in cols) Fx.Burst(at, c, 9, 6f, 0.22f, 1.1f);
            Fx.Sparkle(at, Color.white, 14, 1.2f);
        }

        /// <summary>HOME button (simple buttons): straight back to camp - or to the Dawnfield gate before there is one.</summary>
        public void GoHome()
        {
            if (Caves.InCave) { GameEvents.Say("Find the way out of the cave first."); return; }
            if (Wardens.I != null && Wardens.I.Active != null) { GameEvents.Say("Not while a Warden is awake!"); return; }
            if (Player.IsMounted) Player.Dismount(true);
            Vector3 at = baseMgr.HasBase ? baseMgr.CorePosition + new Vector3(3f, 0, 3f) : WorldGen.StartExit;
            at.y = Ground.Height(at.x, at.z);
            Fx.Sparkle(Player.Position + Vector3.up, new Color(0.6f, 1f, 0.8f), 16, 0.8f);
            Player.Teleport(at, Player.transform.eulerAngles.y);
            CameraRig.SnapTo(at);
            Fx.Sparkle(at + Vector3.up, new Color(0.6f, 1f, 0.8f), 16, 0.8f);
            SynthAudio.Play(Sfx.Summon, 1.1f, 0.6f);
            GameEvents.Good(baseMgr.HasBase ? "Home!" : "Back at the Dawnfield gate. Build a Hearthstone and HOME will bring you to it.");
        }

        public void MarkGoal(string id) => GoalsDone.Add(id);

        /// <summary>Level-matched worlds: the highest level a newly spawned wild kin may be (your strongest kin + 1).</summary>
        public int MatchedLevelCap { get { int m = 1; foreach (var d in Party.All) m = Mathf.Max(m, d.level); return m + 1; } }

        float goalTimer;
        void UpdateGoals()
        {
            // the arrow turns with the camera every frame; everything else is looked at twice a second
            if (goalArrow != null && goalArrow.gameObject.activeSelf)
            {
                var mk = Quests.Marker;
                var camT = Camera.main != null ? Camera.main.transform : null;
                if (mk.HasValue && camT != null)
                {
                    Vector3 to = mk.Value - Player.Position; to.y = 0f;
                    Vector3 fwd = camT.forward; fwd.y = 0f;
                    if (to.sqrMagnitude > 0.01f && fwd.sqrMagnitude > 0.01f)
                        goalArrow.rectTransform.localEulerAngles = new Vector3(0, 0, -Vector3.SignedAngle(fwd, to, Vector3.up));
                }
            }
            goalTimer -= Time.deltaTime;
            if (goalTimer > 0f) return;
            goalTimer = 0.5f;
            Quests.Tick();   // notes where you are, pays out finished quests, opens the next
            KinGrowth.TickReady();
            if (WorldRules.Current.endlessSeeds && Player.Stats.Bag.Count("tether_seed") < 5) Player.Stats.Bag.Add("tether_seed", 5);   // endless-seed worlds
            string next = Quests.HudLine();
            if (next != currentGoal)
            {
                currentGoal = next;
                goalText.text = next ?? "";
                goalText.transform.parent.gameObject.SetActive(next != null);
            }
            var tracked = Quests.Tracked;
            Sprite pic = tracked != null ? Quests.IconFor(tracked) : null;
            bool arrow = Quests.Marker.HasValue;
            if (goalIcon.gameObject.activeSelf != (pic != null)) goalIcon.gameObject.SetActive(pic != null);
            if (pic != null) goalIcon.sprite = pic;
            if (goalArrow.gameObject.activeSelf != arrow) goalArrow.gameObject.SetActive(arrow);
            goalText.rectTransform.Stretch(pic != null ? 98 : 14, 6, arrow ? 80 : 14, 6);
        }

        // ============================================================ LIFECYCLE
        void OnApplicationPause(bool paused)
        {
            if (paused && Started) SaveSystem.Save();
        }

        void OnApplicationQuit()
        {
            if (Started) SaveSystem.Save();
        }

        void OnDestroy()
        {
            if (Started && !SkipAutosaveOnce) SaveSystem.Save();
            SkipAutosaveOnce = false;
            CreatureRegistry.All.Clear();
            WorldCollision.Obstacles.Clear();
            Time.timeScale = 1f;
            if (I == this) I = null;
        }
    }
}
