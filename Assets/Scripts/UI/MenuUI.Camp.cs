using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wildbond
{
    public enum CampTab { Overview, Structures, Workers, Storage, Orders, Bounties, Expeditions, Hatchery, Outposts }

    /// <summary>Camp menu: Overview dashboard, Structure cards, Workers, Storage grid, Craft orders.</summary>
    public partial class MenuUI
    {
        CampTab campTab = CampTab.Overview;
        string storageSel;
        int storageFilter; // 0 all, 1 resources, 2 food, 3 gear & other
        bool campScrollReset;

        static readonly Color Good = new Color(0.35f, 0.85f, 0.45f), Idle = new Color(1f, 0.8f, 0.3f), Bad = new Color(1f, 0.4f, 0.35f), Info = new Color(0.6f, 0.68f, 0.78f);
        static Color HealthColor(BuildingHealth h) => h == BuildingHealth.Good ? Good : h == BuildingHealth.Idle ? Idle : h == BuildingHealth.Blocked ? Bad : Info;

        public void OpenCamp(CampTab t) { campTab = t; campScrollReset = true; manageUid = null; Open(MenuTab.Camp); }

        /// <summary>MANAGE at a structure: the Structures page showing just that one, with everything you can do there.</summary>
        string manageUid;
        public void OpenManage(Building b) { campTab = CampTab.Structures; campScrollReset = true; manageUid = b != null ? b.Uid : null; Open(MenuTab.Camp); }

        /// <summary>A greyed "go there" button for something that is done at a structure.</summary>
        void WalkBtn(Transform parent, string label, int size = 20) => UIFactory.Button(parent, label, null, new Color(0.3f, 0.3f, 0.32f), size);

        void BuildCamp()
        {
            SaveScroll();
            if (campScrollReset) { savedScroll = 1f; campScrollReset = false; }
            var bm = BaseManager.I;
            if (!bm.HasBase)
            {
                var l0 = TrackedList(content);
                Header(l0, "No camp yet");
                Para(l0, "1. Gather Stone, Wood and Fiber.\n2. Tap BUILD and place a Hearthstone anywhere flat.\n3. Build a Workbench, Lumber Mill, Quarry, Berry Plot...\n4. Move captured kin to the camp (Kin tab) and they'll work automatically.\n5. A Storage Chest + Hauling kin carry production into storage; a Feed Box keeps workers fed.", 26, Color.white);
                return;
            }

            // sub-tab bar
            var bar = UIFactory.Rect(content, "CampTabs");
            bar.Anchors(new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -88), new Vector2(-16, -12));
            var h = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 10; h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            var alerts = bm.Alerts();
            foreach (CampTab t in Enum.GetValues(typeof(CampTab)))
            {
                var tt = t;
                string label = t.ToString().ToUpperInvariant();
                if (t == CampTab.Overview && alerts.Exists(a => a.severe)) label += " (!)";
                if (t == CampTab.Orders && bm.Orders.Count > 0) label += $" ({bm.Orders.Count})";
                if (t == CampTab.Bounties) { int ready = 0; foreach (var bo in GameManager.I.Player.Stats.bounties) if (Bounties.Ready(bo)) ready++; if (ready > 0) label += $" ({ready}!)"; }
                if (t == CampTab.Outposts && Outposts.All.Count > 0) label += $" ({Outposts.All.Count})";
                if (t == CampTab.Hatchery) { int eggs = 0; foreach (var eg in GameManager.I.Player.Stats.eggs) if (KinGrowth.EggReady(eg)) eggs++; if (eggs > 0) label += $" ({eggs}!)"; }
                if (t == CampTab.Expeditions) { int back = 0; foreach (var ex in GameManager.I.Player.Stats.expeditions) if (Expeditions.Done(ex)) back++; if (back > 0) label += $" ({back}!)"; }
                UIFactory.Button(bar, label, () => { campTab = tt; campScrollReset = true; manageUid = null; Rebuild(); },
                    campTab == t ? new Color(0.25f, 0.55f, 0.45f) : UIFactory.ButtonCol, 20);
            }

            var body = UIFactory.Rect(content, "CampBody");
            body.Anchors(Vector2.zero, Vector2.one, new Vector2(0, 0), new Vector2(0, -96));
            switch (campTab)
            {
                case CampTab.Overview: CampOverview(body, alerts); break;
                case CampTab.Structures: CampStructures(body); break;
                case CampTab.Workers: CampWorkers(body); break;
                case CampTab.Storage: CampStorage(body); break;
                case CampTab.Orders: CampOrders(body); break;
                case CampTab.Bounties: CampBounties(body); break;
                case CampTab.Expeditions: CampExpeditions(body); break;
                case CampTab.Hatchery: CampHatchery(body); break;
                case CampTab.Outposts: CampOutposts(body); break;
            }
            RestoreScroll();
        }

        Image Card(Transform parent, float height, Color? c = null)
        {
            var card = UIFactory.Panel(parent, "Card", c ?? new Color(1, 1, 1, 0.06f));
            if (height > 0) UIFactory.Size(card, height);
            return card;
        }

        // ============================================================ OVERVIEW
        void CampOverview(RectTransform body, List<BaseManager.Alert> alerts)
        {
            var bm = BaseManager.I;
            var party = GameManager.I.Party;
            var list = TrackedList(body);
            int workers = party.CountIn(KinLocation.Base);

            // hearth card
            var hc = Card(list, 150);
            var hIcon = UIFactory.Rect(hc.transform, "Hearth").gameObject.AddComponent<Image>();
            hIcon.sprite = ItemIcons.Get("bld_hearthstone"); hIcon.preserveAspect = true; hIcon.raycastTarget = false;
            if (hIcon.sprite == null) hIcon.color = new Color(1, 1, 1, 0.1f);
            hIcon.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(120, 120));
            var ht = UIFactory.Label(hc.transform, $"<b>Hearthstone Lv {bm.CoreLevel}</b>\n<size=22>Camp radius {bm.Radius:0} m   ·   Workers {workers}/{bm.WorkerCap}   ·   Beds {bm.BedCount}</size>", 32, Color.white);
            ht.supportRichText = true;
            ht.rectTransform.Stretch(150, 10, 470, 10);
            if (bm.CoreLevel < BaseManager.MaxCoreLevel)
            {
                var cost = BaseManager.UpgradeCost(bm.CoreLevel + 1);
                var up = bm.AtCore ? UIFactory.Button(hc.transform, "UPGRADE", () => { bm.TryUpgradeCore(); Rebuild(); }, ItemSources.Has(cost) ? new Color(0.5f, 0.4f, 0.2f) : new Color(0.3f, 0.3f, 0.32f), 26)
                                   : UIFactory.Button(hc.transform, "AT THE\nHEARTHSTONE", null, new Color(0.3f, 0.3f, 0.32f), 18);
                ((RectTransform)up.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-14, 18), new Vector2(200, 70));
                var ct = UIFactory.Label(hc.transform, Ingredients(cost), 18, Color.white, TextAnchor.UpperRight);
                ct.supportRichText = true;
                ct.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(1, 1), new Vector2(-14, -22), new Vector2(440, 50));
            }
            else
            {
                var mx = UIFactory.Label(hc.transform, "MAX LEVEL", 26, UIFactory.Accent, TextAnchor.MiddleRight, FontStyle.Bold);
                mx.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-20, 0), new Vector2(300, 60));
            }
            if (bm.AtCore) UIFactory.Size(UIFactory.Button(list, "AUTO-ASSIGN ALL WORKERS", () => { bm.AutoAssignAll(); Rebuild(); }, new Color(0.25f, 0.5f, 0.42f), 26), 72);
            else Para(list, UIFactory.Col($"This is a view of your camp. Its work is done at each structure: walk up to one and use its button, or MANAGE for its workers and crop. The Hearthstone ({bm.WayTo(bm.Core)}) holds the upgrade, AUTO-ASSIGN and the job list.", Idle), 21, Color.white);

            // alerts
            if (alerts.Count > 0)
            {
                Header(list, "Alerts", 28);
                foreach (var a in alerts)
                {
                    var ac = Card(list, 58, a.severe ? new Color(0.5f, 0.15f, 0.12f, 0.55f) : new Color(0.5f, 0.4f, 0.1f, 0.45f));
                    var at = UIFactory.Label(ac.transform, (a.severe ? "!  " : "·  ") + a.text, 23, Color.white);
                    at.rectTransform.Stretch(16, 4, 16, 4);
                }
            }
            else
            {
                var ok = Card(list, 58, new Color(0.15f, 0.45f, 0.25f, 0.45f));
                var ot = UIFactory.Label(ok.transform, "All good - your camp is running smoothly.", 23, Color.white);
                ot.rectTransform.Stretch(16, 4, 16, 4);
            }

            // food
            Header(list, "Food", 28);
            var fc = Card(list, 96);
            float mins = bm.FoodMinutes;
            string runway = float.IsPositiveInfinity(mins) ? "no one is eating" : (mins > 600 ? "10+ hours" : $"~{mins:0} min");
            float spoil = bm.SpoilPerMinute;
            string going = spoil >= 0.05f ? $"  ·  {UIFactory.Col($"about {spoil:0.#} going off a minute", spoil > 3f ? Idle : Info)}" : "";
            var ft = UIFactory.Label(fc.transform, $"Sunberries {bm.Storage.Count("berry")}  ·  Berry Tarts {bm.Storage.Count("berry_tart")}  ·  lasts {runway}{going}", 24, Color.white);
            ft.supportRichText = true;
            ft.rectTransform.Stretch(16, 44, 16, 6);
            var fb = UIFactory.Bar(fc.transform, mins < 10 ? Bad : (mins < 30 ? Idle : Good), out var ff);
            fb.rectTransform.Anchors(new Vector2(0, 0), new Vector2(1, 0), new Vector2(16, 12), new Vector2(-16, 36));
            ff.SetFill(float.IsPositiveInfinity(mins) ? 1f : Mathf.Clamp01(mins / 60f));

            // production
            Header(list, "Production", 28);
            var prod = bm.ProductionPerMinute();
            if (prod.Count == 0) Para(list, "Nothing is being produced right now.", 22);
            else
            {
                var grid = UIFactory.Rect(list, "Prod");
                var g = grid.gameObject.AddComponent<GridLayoutGroup>();
                g.cellSize = new Vector2(250, 90); g.spacing = new Vector2(10, 10);
                UIFactory.Size(grid, Mathf.Ceil(prod.Count / 5f) * 100f);
                foreach (var kv in prod)
                {
                    var cell = UIFactory.Panel(grid, "P", new Color(1, 1, 1, 0.06f));
                    var ic = UIFactory.ItemIcon(cell.transform, ContentDB.Item(kv.Key), 70);
                    ic.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(70, 70));
                    var t = UIFactory.Label(cell.transform, $"<b>+{kv.Value:0.#}</b>/min\n<size=18>{ContentDB.ItemName(kv.Key)}</size>", 24, Good);
                    t.supportRichText = true;
                    t.rectTransform.Stretch(84, 4, 6, 4);
                }
            }

            // boosts
            Header(list, "Boosts", 28);
            var rules = WorldRules.Current;
            var chips = new List<string>();
            chips.Add(bm.DynamoBoost > 0 ? UIFactory.Col($"Dynamo +{bm.DynamoBoost * 100:0}%", Good) : UIFactory.Col(bm.HasBuilding("dynamo") ? "Dynamo: no power" : "No dynamo", Info));
            chips.Add(bm.RestedMul > 1f ? UIFactory.Col("Rested +15%", Good) : UIFactory.Col("Not rested (every kin needs a bed that fits)", Info));
            if (bm.WellBedded > 0) chips.Add(UIFactory.Col($"Grand Nest +10%: {bm.WellBedded} kin", Good));
            chips.Add(bm.LarderActive ? UIFactory.Col(bm.LarderStaffed ? "Frost Larder: -40% hunger" : $"Frost Larder: -40% hunger · cold for {WorkStages.Clock(bm.Cold)}", Good) : UIFactory.Col("No larder", Info));
            chips.Add(bm.CampPowered ? UIFactory.Col(bm.PylonStaffed ? $"Camp powered · charge {bm.ChargeText}" : $"On stored charge · {WorkStages.Clock(bm.Charge)} left", Good) : UIFactory.Col("No power", Info));
            if (Mathf.Abs(rules.workSpeed - 1f) > 0.01f) chips.Add(UIFactory.Col($"World work speed x{rules.workSpeed:0.##}", Idle));
            var pst = GameManager.I.Player.Stats;
            if (pst.KinHungerMul < 0.999f) chips.Add(UIFactory.Col($"Hearthmoss: -{(1f - pst.KinHungerMul) * 100f:0}% worker hunger", Good));
            if (pst.CraftSpeedMul > 1.001f) chips.Add(UIFactory.Col($"Craft Speed x{pst.CraftSpeedMul:0.00}", Good));
            Para(list, string.Join("     ", chips), 22, Color.white);
        }

        // ============================================================ STRUCTURES
        void CampStructures(RectTransform body)
        {
            var bm = BaseManager.I;
            var list = TrackedList(body);
            DestroyImmediate(list.GetComponent<VerticalLayoutGroup>());
            var grid = list.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(500, 300);
            grid.spacing = new Vector2(14, 14);
            grid.padding = new RectOffset(8, 8, 8, 8);

            var shownList = bm.Buildings;
            if (manageUid != null)
            {
                var mb = bm.FindBuilding(manageUid);
                if (mb == null) manageUid = null;
                else
                {
                    shownList = new List<Building> { mb };
                    var all = UIFactory.Panel(list, "All", new Color(1, 1, 1, 0.03f));
                    all.gameObject.AddComponent<Button>().onClick.AddListener(() => { manageUid = null; Rebuild(); });
                    var allT = UIFactory.Label(all.transform, "SHOW EVERY STRUCTURE", 26, new Color(1, 1, 1, 0.6f), TextAnchor.MiddleCenter, FontStyle.Bold);
                    allT.rectTransform.Stretch();
                }
            }
            foreach (var b in shownList)
            {
                var bb = b;
                bool here = bm.PlayerAt(b);
                var health = bm.Status(b, out string status);
                var card = UIFactory.Panel(list, "Bld", new Color(1, 1, 1, 0.07f));
                // icon + light
                var icon = UIFactory.Rect(card.transform, "Icon").Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -10), new Vector2(120, 120));
                var ii = icon.gameObject.AddComponent<Image>();
                ii.sprite = ItemIcons.Get("bld_" + b.Def.id); ii.preserveAspect = true; ii.raycastTarget = false;
                if (ii.sprite == null) ii.color = new Color(1, 1, 1, 0.1f);
                var light = UIFactory.Panel(card.transform, "Light", HealthColor(health), UIFactory.Circle);
                light.type = Image.Type.Simple;
                light.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-14, -14), new Vector2(26, 26));
                var name = UIFactory.Label(card.transform, $"<b>{b.Def.displayName}</b>{(b.Upgradable ? $"  <size=20>{UIFactory.Col($"Lv {b.Level}", b.Level > 1 ? new Color(1f, 0.82f, 0.35f) : UIFactory.TextDim)}</size>" : "")}\n<size=19>{status}</size>", 27, Color.white, TextAnchor.UpperLeft);
                name.supportRichText = true;
                name.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(140, -12), new Vector2(320, 110));
                name.color = Color.white;

                // output buffer bar
                if (b.IsProducer)
                {
                    var buf = UIFactory.Bar(card.transform, b.BufferCount >= b.BufferCap ? Bad : UIFactory.Accent, out var bf);
                    buf.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(140, -126), new Vector2(340, 18));
                    bf.SetFill(b.BufferCount / (float)b.BufferCap);
                    var bl = UIFactory.Label(card.transform, $"{ContentDB.ItemName(b.OutputItem)} {b.BufferCount}/{b.BufferCap}", 18, UIFactory.TextDim);
                    bl.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(140, -148), new Vector2(340, 24));
                }

                // worker slots
                if (b.WorkerSlots > 0)
                {
                    var slots = UIFactory.Rect(card.transform, "Slots").Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(12, 82), new Vector2(476, 80));
                    var workers = new List<CreatureData>(bm.WorkersAt(b));
                    for (int i = 0; i < b.WorkerSlots; i++)
                    {
                        var s = UIFactory.Panel(slots, "Slot", new Color(0, 0, 0, 0.3f));
                        s.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(i * 88, 0), new Vector2(78, 78));
                        s.gameObject.AddComponent<Button>().onClick.AddListener(() => { if (bm.PlayerAt(bb)) AssignPicker(bb); else GameEvents.Say($"Walk up to the {bb.Def.displayName} ({bm.WayTo(bb)}) to choose who works there."); });
                        if (i < workers.Count)
                        {
                            var p = Portrait(s.transform, workers[i], 64);
                            p.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64, 64));
                        }
                        else
                        {
                            var plus = UIFactory.Label(s.transform, "+", 40, new Color(1, 1, 1, 0.5f), TextAnchor.MiddleCenter, FontStyle.Bold);
                            plus.rectTransform.Stretch();
                        }
                    }
                    var job = UIFactory.Label(card.transform, $"{b.Def.workType}{(b.Def.secondaryWorkType != WorkType.None ? " / " + b.Def.secondaryWorkType : "")}", 18, UIFactory.TextDim, TextAnchor.MiddleRight);
                    job.rectTransform.Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-14, 130), new Vector2(260, 26));
                }

                // actions
                var act = UIFactory.Rect(card.transform, "Actions").Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(12, 12), new Vector2(476, 60));
                var ah = act.gameObject.AddComponent<HorizontalLayoutGroup>();
                ah.spacing = 8; ah.childControlWidth = true; ah.childControlHeight = true; ah.childForceExpandWidth = true;
                if (!here)
                {
                    // far away: look, don't touch
                    if (b.IsStation) UIFactory.Button(act, "VIEW", () => OpenStation(bb.Def.station), new Color(0.3f, 0.36f, 0.45f), 20);
                    WalkBtn(act, "Walk there: " + bm.WayTo(b), 18);
                    continue;
                }
                if (b.BufferCount > 0) UIFactory.Button(act, $"COLLECT {b.BufferCount}", () => { GameManager.I.CollectBuffer(bb); Rebuild(); }, new Color(0.25f, 0.5f, 0.42f), 22);
                if (b.IsStation) UIFactory.Button(act, StationVerb(b.Def.station), () => OpenStation(bb.Def.station), new Color(0.3f, 0.4f, 0.55f), 22);
                if (Crops.IsPlot(b)) UIFactory.Button(act, "CROP", () => CropPicker(bb), new Color(0.3f, 0.5f, 0.3f), 22);
                if (b.Def.feeds || (b.BoxLen > 0 && !b.Def.isCore)) UIFactory.Button(act, b.Def.feeds ? "FEED BOX" : "OPEN", () => OpenBox(bb), new Color(0.3f, 0.36f, 0.5f), 22);
                if (KinGrowth.IsHatchery(b.Def)) UIFactory.Button(act, "HATCHERY", () => OpenHatchery(), new Color(0.5f, 0.38f, 0.25f), 22);
                if (b.Upgradable && b.Level < Building.MaxLevel)
                {
                    var upCost = BaseManager.StructureUpgradeCost(b);
                    UIFactory.Button(act, "UPGRADE", () => Confirm($"Upgrade the {bb.Def.displayName}?", BaseManager.UpgradeText(bb) + "\n\nCost: " + Ingredients(BaseManager.StructureUpgradeCost(bb)),
                        () => { bm.TryUpgradeStructure(bb); Rebuild(); }), ItemSources.Has(upCost) ? new Color(0.55f, 0.45f, 0.2f) : new Color(0.35f, 0.33f, 0.3f), 20);
                }
                if (!b.Def.isCore) UIFactory.Button(act, "DEMOLISH", () => Confirm("Demolish?", $"Demolish {bb.Def.displayName}? Half the materials go to storage.", () => { bm.Demolish(bb); Rebuild(); }), new Color(0.42f, 0.24f, 0.24f), 20);
            }
            // add-building card
            if (manageUid != null) return;   // MANAGE shows just the one structure
            var add = UIFactory.Panel(list, "Add", new Color(1, 1, 1, 0.03f));
            add.gameObject.AddComponent<Button>().onClick.AddListener(() => Open(MenuTab.Build));
            var at = UIFactory.Label(add.transform, "+ BUILD NEW", 32, new Color(1, 1, 1, 0.6f), TextAnchor.MiddleCenter, FontStyle.Bold);
            at.rectTransform.Stretch();
        }

        /// <summary>Crop Plot menu: what to grow.</summary>
        void CropPicker(Building b)
        {
            ShowModal($"Crop Plot - growing {ContentDB.ItemName(b.OutputItem)}", list =>
            {
                Para(list, $"Planting a different crop costs {Crops.PlantCost} of it; the plot grows it from then on. Crops cook into meals at the Campfire.", 22);
                foreach (var c in Crops.All)
                {
                    var cc = c;
                    var d = ContentDB.Item(c.id);
                    if (d == null) continue;
                    bool cur = b.OutputItem == c.id;
                    int have = ItemSources.Count(c.id);
                    bool can = !cur && have >= Crops.PlantCost;
                    var row = UIFactory.HRow(list, 96, 12);
                    var ic = UIFactory.ItemIcon(row.transform, d, 76); UIFactory.Size(ic, 76, 76);
                    var t = UIFactory.Label(row.transform, $"<b>{d.displayName}</b>  <size=20>x{c.count} per harvest · {Crops.PerMinute(c):0.0}/min per work point</size>\n<size=19>{(have > 0 ? $"You have {have}" : "Wild: " + c.where)}</size>", 25, Color.white);
                    t.supportRichText = true; UIFactory.Size(t, 88, 640);
                    UIFactory.Size(UIFactory.Button(row.transform, cur ? "GROWING" : can ? $"PLANT ({Crops.PlantCost})" : $"Need {Crops.PlantCost}",
                        can ? () => { string err = Crops.Plant(b, cc.id); if (err != null) GameEvents.Warn(err); CloseModal(); Rebuild(); } : (System.Action)null,
                        cur ? new Color(0.25f, 0.5f, 0.32f) : can ? new Color(0.3f, 0.45f, 0.55f) : new Color(0.3f, 0.3f, 0.32f), 22), 80, 200);
                }
            });
        }

        /// <summary>
        /// Where kin with a job live: the nearest land that has any, then the land with the best of them
        /// ("Sunmeadow Vale: Acornip **, Vinelash ** + 3 more"). Kin you have seen are named with their job level as
        /// stars; the rest are only counted, so the Dex is not spoiled.
        /// </summary>
        static string JobHint(WorkType job)
        {
            var stats = GameManager.I.Player.Stats;
            BiomeId first = BiomeId.Meadow, best = BiomeId.Meadow;
            bool any = false; int bestLevel = 0, bestCount = 0;
            foreach (var land in ExplorerLog.Lands)
            {
                int top = 0, n = 0;
                foreach (var sp in ContentDB.SpeciesList)
                {
                    if (sp.starter || sp.bossOnly || sp.spawnWeight <= 0f || !sp.LivesIn(land)) continue;
                    int lv = sp.WorkLevel(job);
                    if (lv <= 0) continue;
                    n++; if (lv > top) top = lv;
                }
                if (n == 0) continue;
                if (!any) { any = true; first = land; }
                if (top > bestLevel || (top == bestLevel && n > bestCount)) { bestLevel = top; bestCount = n; best = land; }
            }
            if (!any) return $"No wild kin do {job} work.";
            string Line(BiomeId land, string lead)
            {
                var known = new List<SpeciesDef>();
                int unknown = 0;
                foreach (var sp in ContentDB.SpeciesList)
                {
                    if (sp.starter || sp.bossOnly || sp.spawnWeight <= 0f || !sp.LivesIn(land) || sp.WorkLevel(job) <= 0) continue;
                    var e = stats.DexFor(sp.id, false);
                    if (e != null && e.seen > 0) known.Add(sp); else unknown++;
                }
                known.Sort((x, y) => y.WorkLevel(job).CompareTo(x.WorkLevel(job)));
                var sb = new System.Text.StringBuilder(lead);
                sb.Append(UIFactory.Col(BiomeInfo.Name(land), Color.Lerp(BiomeInfo.Tint(land), Color.white, 0.4f))).Append(": ");
                for (int i = 0; i < known.Count && i < 5; i++)
                    sb.Append(i > 0 ? ", " : "").Append(known[i].displayName).Append(' ').Append(UIFactory.Col(new string('*', known[i].WorkLevel(job)), new Color(1f, 0.85f, 0.3f)));
                int rest = unknown + Mathf.Max(0, known.Count - 5);
                if (rest > 0) sb.Append(known.Count > 0 ? $"  + {rest} more" : $"{rest} kind{(rest == 1 ? "" : "s")} you haven't met");
                return sb.ToString();
            }
            string text = "<b>" + job + " kin</b>" + '\n' + Line(first, "Nearest - ");
            if (best != first) text += '\n' + Line(best, "Best (up to " + new string('*', bestLevel) + ") - ");
            return text;
        }

        // ============================================================ WORKERS
        void CampWorkers(RectTransform body)
        {
            var bm = BaseManager.I;
            var party = GameManager.I.Party;
            var list = TrackedList(body);
            // the kin that is out with you pitches in while you are in camp (CreatureActor.CampHelp)
            var stats = GameManager.I.Player.Stats;
            var crow = UIFactory.HRow(list, 64, 12);
            UIFactory.Size(UIFactory.Button(crow.transform, stats.companionRests ? "COMPANION HELPS IN CAMP: OFF" : "COMPANION HELPS IN CAMP: ON",
                () => { stats.companionRests = !stats.companionRests; Rebuild(); }, stats.companionRests ? new Color(0.3f, 0.3f, 0.34f) : new Color(0.25f, 0.5f, 0.42f), 20), 58, 420);
            var comp = GameManager.I.Party.Companion;
            string cline = stats.companionRests ? "Your companion stays at your side in camp."
                : comp == null ? "While you are in camp, the kin out with you does the camp jobs it knows, then follows you again when you leave."
                : comp.CampHelpStatus != null ? $"{comp.Data.DisplayName}: {comp.CampHelpStatus}"
                : $"{comp.Data.DisplayName} has nothing it can do here right now, so it follows you.";
            var cl = UIFactory.Label(crow.transform, cline, 20, UIFactory.TextDim);
            UIFactory.Size(cl, 58, 900);
            bool atCore = bm.AtCore;
            if (!atCore) Para(list, UIFactory.Col($"Assign jobs at the Hearthstone ({bm.WayTo(bm.Core)}), or at each structure with MANAGE.", Idle), 21, Color.white);
            if (atCore) UIFactory.Size(UIFactory.Button(UIFactory.HRow(list, 70, 8).transform, "AUTO-ASSIGN", () => { bm.AutoAssignAll(); Rebuild(); }, new Color(0.25f, 0.5f, 0.42f), 22), 64, 220);
            KinFilterBar(list, party.In(KinLocation.Base), Rebuild);
            int shown = 0;
            foreach (var d in KinFiltered(party.In(KinLocation.Base)))
            {
                shown++;
                var dd = d;
                // the kin as a card: who it is, its jobs (the filtered one in gold), grade, traits, what it is doing
                var btns = KinCard(list, d, kinJob, WorkType.None, 300f);
                if (atCore) UIFactory.Button(btns, "JOB", () => JobPicker(dd), new Color(0.25f, 0.42f, 0.55f), 22);
                else UIFactory.Button(btns, "JOB", null, new Color(0.3f, 0.3f, 0.32f), 22);
                UIFactory.Button(btns, "TO BOX", () => { party.Move(dd, KinLocation.Box); Rebuild(); }, new Color(0.35f, 0.32f, 0.4f), 20);
            }
            if (shown == 0) Para(list, party.CountIn(KinLocation.Base) == 0 ? "No workers yet. In the Kin tab, send kin TO CAMP (stand inside your camp)." : "No workers match the filter.", 24, Color.white);
        }

        /// <summary>Pick a job (building) for one worker.</summary>
        void JobPicker(CreatureData d)
        {
            var bm = BaseManager.I;
            ShowModal($"Job for {d.DisplayName}", list =>
            {
                UIFactory.Size(UIFactory.Button(list, "Unassign (idle)", () => { bm.Assign(d, null); CloseModal(); Rebuild(); }, new Color(0.4f, 0.3f, 0.3f), 24), 70);
                var options = new List<Building>();
                foreach (var b in bm.Buildings) if (b.WorkerSlots > 0 && b.Suitability(d) > 0) options.Add(b);
                options.Sort((x, y) => x.Suitability(d) != y.Suitability(d) ? y.Suitability(d).CompareTo(x.Suitability(d)) : string.CompareOrdinal(x.Def.displayName, y.Def.displayName));
                foreach (var b in options)
                {
                    var bb = b;
                    int n = new List<CreatureData>(bm.WorkersAt(b)).Count;
                    bool here = d.assignedBuilding == b.Uid;
                    int lvl = Mathf.Max(d.WorkLevel(b.Def.workType), d.WorkLevel(b.Def.secondaryWorkType));
                    var row = UIFactory.HRow(list, 90, 12);
                    var ic = UIFactory.Rect(row.transform, "I").gameObject.AddComponent<Image>();
                    ic.sprite = ItemIcons.Get("bld_" + b.Def.id); ic.preserveAspect = true; UIFactory.Size(ic, 80, 80);
                    var t = UIFactory.Label(row.transform, $"<b>{b.Def.displayName}</b>  {UIFactory.Col(new string('*', lvl), new Color(1f, 0.85f, 0.3f))}\n<size=19>{b.Def.workType} · {n}/{b.WorkerSlots} workers{(here ? " · current" : "")}</size>", 26, Color.white);
                    t.supportRichText = true; UIFactory.Size(t, 84, 560);
                    bool full = n >= b.WorkerSlots && !here;
                    UIFactory.Size(UIFactory.Button(row.transform, here ? "HERE" : (full ? "FULL" : "ASSIGN"), here || full ? (Action)null : () => { if (bm.Assign(d, bb)) { CloseModal(); Rebuild(); } },
                        here || full ? new Color(0.3f, 0.3f, 0.32f) : new Color(0.25f, 0.5f, 0.42f), 22), 76, 180);
                }
                if (options.Count == 0) Para(list, "No structure in your camp uses this kin's skills. Build one from the BUILD menu.", 24, Color.white);
            });
        }

        // ============================================================ STORAGE
        void CampStorage(RectTransform body)
        {
            var bm = BaseManager.I;
            var gm = GameManager.I;
            bool inCamp = bm.StorageHere() != null;   // camp-wide taking and storing: at any box or the Hearthstone
            var left = UIFactory.Rect(body, "Left");
            var right = UIFactory.Rect(body, "Right");
            left.Anchors(new Vector2(0, 0), new Vector2(0.66f, 1), new Vector2(16, 16), new Vector2(-8, -8));
            right.Anchors(new Vector2(0.66f, 0), new Vector2(1, 1), new Vector2(8, 16), new Vector2(-16, -8));

            var head = UIFactory.Rect(left, "Head").Place(new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(1100, 70));
            var hh = head.gameObject.AddComponent<HorizontalLayoutGroup>();
            hh.spacing = 8; hh.childControlWidth = true; hh.childControlHeight = true; hh.childForceExpandWidth = false;
            string[] filters = { "ALL", "RESOURCES", "FOOD", "OTHER" };
            for (int i = 0; i < filters.Length; i++)
            {
                int fi = i;
                UIFactory.Size(UIFactory.Button(head, filters[i], () => { storageFilter = fi; Rebuild(); }, storageFilter == i ? new Color(0.25f, 0.55f, 0.45f) : UIFactory.ButtonCol, 20), 60, 170);
            }
            var cap = UIFactory.Bar(left, UsedSlots(bm.Storage) >= bm.Storage.Capacity ? Bad : UIFactory.Accent, out var cf);
            cap.rectTransform.Anchors(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -104), new Vector2(0, -80));
            cf.SetFill(bm.Storage.Capacity > 0 ? UsedSlots(bm.Storage) / (float)bm.Storage.Capacity : 0f);
            var capL = UIFactory.Label(left, $"{UsedSlots(bm.Storage)}/{bm.Storage.Capacity} slots", 18, Color.white, TextAnchor.MiddleCenter);
            capL.rectTransform.Anchors(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -104), new Vector2(0, -80));

            var gridHost = UIFactory.Rect(left, "GridHost");
            gridHost.Anchors(Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -114));
            var list = TrackedList(gridHost);
            DestroyImmediate(list.GetComponent<VerticalLayoutGroup>());
            var grid = list.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(128, 128); grid.spacing = new Vector2(10, 10); grid.padding = new RectOffset(6, 6, 6, 6);

            // aggregate stacks
            var totals = new Dictionary<string, int>();
            foreach (var s in bm.Storage.slots) if (!s.Empty) { totals.TryGetValue(s.id, out int c); totals[s.id] = c + s.count; }
            var ids = new List<string>(totals.Keys);
            ids.Sort((a, b) =>
            {
                var da = ContentDB.Item(a); var db = ContentDB.Item(b);
                int ca = da != null ? (int)da.category : 99, cb = db != null ? (int)db.category : 99;
                return ca != cb ? ca.CompareTo(cb) : string.CompareOrdinal(a, b);
            });
            foreach (var id in ids)
            {
                var d = ContentDB.Item(id);
                int cat = d == null ? 3 : (d.category == ItemCategory.Resource ? 1 : (d.category == ItemCategory.Food ? 2 : 3));
                if (storageFilter != 0 && storageFilter != cat) continue;
                var iid = id;
                var cell = UIFactory.Panel(list, "S", id == storageSel ? new Color(0.3f, 0.6f, 0.5f, 1f) : new Color(1, 1, 1, 0.08f));
                cell.gameObject.AddComponent<Button>().onClick.AddListener(() => { storageSel = iid; SynthAudio.Play(Sfx.UiClick); Rebuild(); });
                var ic = UIFactory.ItemIcon(cell.transform, d, 90);
                ic.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -8), new Vector2(90, 90));
                ic.raycastTarget = false;
                var nm = UIFactory.Label(cell.transform, ContentDB.ItemName(id), 15, new Color(1f, 1f, 1f, 0.85f), TextAnchor.UpperCenter, FontStyle.Bold);
                nm.rectTransform.Anchors(new Vector2(0, 1), new Vector2(1, 1), new Vector2(4, -26), new Vector2(-4, -2));
                nm.horizontalOverflow = HorizontalWrapMode.Wrap; nm.resizeTextForBestFit = true; nm.resizeTextMinSize = 10; nm.resizeTextMaxSize = 15; nm.raycastTarget = false;
                var cnt = UIFactory.Label(cell.transform, "x" + totals[id], 24, Color.white, TextAnchor.LowerRight, FontStyle.Bold);
                cnt.rectTransform.Stretch(4, 2, 8, 2);
                cnt.raycastTarget = false;
            }

            // right: selection + bulk actions
            // a scrolling column: with many boxes the take / store buttons used to run off the bottom (user, 2026-10-15)
            var col = SideList(right, "storage", 12f);
            if (!inCamp) Para(col, UIFactory.Col("Walk up to a chest or the Hearthstone to take or store things. Each box can also be opened at the box itself.", Idle), 22);
            if (storageSel != null && totals.ContainsKey(storageSel))
            {
                var d = ContentDB.Item(storageSel);
                var srow = UIFactory.HRow(col, 110, 10);
                UIFactory.Size(UIFactory.ItemIcon(srow.transform, d, 100), 100, 100);
                var st = UIFactory.Label(srow.transform, $"<b>{ContentDB.ItemName(storageSel)}</b>\n<size=22>x{totals[storageSel]} in camp</size>", 28, Color.white);
                st.supportRichText = true; UIFactory.Size(st, 100, 300);
                if (inCamp)
                {
                    string sid = storageSel;
                    var tr = UIFactory.HRow(col, 72, 8);
                    UIFactory.Size(UIFactory.Button(tr.transform, "TAKE 1", () => TakeFromStorage(sid, 1), new Color(0.25f, 0.5f, 0.42f), 22), 66, 130);
                    UIFactory.Size(UIFactory.Button(tr.transform, "TAKE 10", () => TakeFromStorage(sid, 10), new Color(0.25f, 0.5f, 0.42f), 22), 66, 130);
                    UIFactory.Size(UIFactory.Button(tr.transform, "TAKE ALL", () => TakeFromStorage(sid, int.MaxValue), new Color(0.25f, 0.5f, 0.42f), 22), 66, 130);
                }
            }
            else Para(col, "Tap an item to take it.", 22);
            if (inCamp)
            {
                var br = UIFactory.HRow(col, 72, 8);
                UIFactory.Size(UIFactory.Button(br.transform, "TAKE EVERYTHING", () => { gm.TakeAllFromStorage(); Rebuild(); }, new Color(0.3f, 0.4f, 0.55f), 20), 66, 200);
                UIFactory.Size(UIFactory.Button(br.transform, "STORE MATERIALS", () => { gm.DepositResources(); Rebuild(); }, new Color(0.3f, 0.4f, 0.55f), 20), 66, 200);
            }
            // each box has contents of its own: open one to fill or empty that box
            Header(col, "Boxes", 28);
            if (inCamp) ActionBtn(col, "SORT EVERYTHING", () => { bm.SortStorage(); GameEvents.Good("Storage sorted: everything is in the box that holds its kind."); Rebuild(); }, new Color(0.25f, 0.5f, 0.42f));
            foreach (var box in bm.Boxes())
            {
                var bx = box;
                var bf = BaseManager.FilterOf(bx);
                ActionBtn(col, $"{bm.BoxName(bx)}   {bm.BoxUsed(bx)}/{bm.BoxCapacity(bx)}{(bf != BoxFilter.Any ? "   · " + BaseManager.FilterShort[(int)bf] : "")}", () => OpenBox(bx),
                    BaseManager.IsLarder(bx) ? new Color(0.25f, 0.45f, 0.6f) : new Color(0.3f, 0.36f, 0.5f));
            }
            foreach (var fb in bm.FeedBoxes())
            {
                var bx = fb;
                ActionBtn(col, $"{bm.BoxName(bx)}   {bm.FeedCount(bx)} food", () => OpenBox(bx), new Color(0.5f, 0.36f, 0.3f));
            }
        }

        void TakeFromStorage(string id, int amount)
        {
            var bm = BaseManager.I;
            var bag = GameManager.I.Player.Stats.Bag;
            int want = Mathf.Min(amount, bm.Storage.Count(id));
            int fits = Mathf.Min(want, bag.SpaceFor(id));
            if (fits <= 0) { GameEvents.Warn("Bag is full."); return; }
            bm.Storage.Remove(id, fits);
            bag.Add(id, fits);
            if (fits < want) GameEvents.Warn("Bag is full - took what fits.");
            SynthAudio.Play(Sfx.Pickup);
            GameEvents.RaiseInventoryChanged();
        }

        // ============================================================ ORDERS
        void CampOrders(RectTransform body)
        {
            var bm = BaseManager.I;
            var list = TrackedList(body);
            var srow = UIFactory.HRow(list, 76, 12);
            foreach (var st in RecipeStations())
            {
                var s2 = st;
                UIFactory.Size(UIFactory.Button(srow.transform, bm.AtStation(st) ? $"+ {StationName(st).ToUpper()}" : StationName(st).ToUpper(), () => OpenStation(s2), new Color(0.3f, 0.4f, 0.55f), 24), 70, 360);
            }
            if (bm.Orders.Count == 0) { Para(list, "No orders. Each station (Workbench, Campfire) has its own menu - queue there, and Crafting / Kindling kin work the orders (or you, standing at the station).", 24, Color.white); return; }
            for (int i = 0; i < bm.Orders.Count; i++)
            {
                int idx = i;
                var o = bm.Orders[i];
                if (!ContentDB.Recipes.TryGetValue(o.recipeId, out var r)) continue;
                var card = Card(list, 120);
                var ic = UIFactory.ItemIcon(card.transform, ContentDB.Item(r.outputItem), 90);
                ic.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(90, 90));
                var t = UIFactory.Label(card.transform, $"<b>{ContentDB.ItemName(r.outputItem)} x{r.outputCount}</b>  <size=20>{o.remaining} left · {Stations.BenchName(r.station, Stations.Tier(r))}{(bm.OrderProgress01(o) > 0f ? " · in progress" : "")}</size>", 26, Color.white, TextAnchor.UpperLeft);
                t.supportRichText = true;
                t.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(120, -14), new Vector2(700, 40));
                var pb = UIFactory.Bar(card.transform, UIFactory.Accent, out var pf);
                pb.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(120, 22), new Vector2(620, 24));
                pf.SetFill(bm.OrderProgress01(o));
                var btns = UIFactory.Rect(card.transform, "B").Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(360, 90));
                var bh = btns.gameObject.AddComponent<HorizontalLayoutGroup>();
                bh.spacing = 8; bh.childControlWidth = true; bh.childControlHeight = true; bh.childForceExpandWidth = true;
                var oo = o;
                if (!bm.AtStation(r.station, Stations.Tier(r))) { WalkBtn(btns, $"At the {Stations.BenchName(r.station, Stations.Tier(r))}", 18); continue; }
                UIFactory.Button(btns, "UP", idx > 0 ? () => { MoveOrder(idx, -1); } : (Action)null, idx > 0 ? UIFactory.ButtonCol : new Color(0.25f, 0.25f, 0.27f), 22);
                UIFactory.Button(btns, "DOWN", idx < bm.Orders.Count - 1 ? () => { MoveOrder(idx, 1); } : (Action)null, idx < bm.Orders.Count - 1 ? UIFactory.ButtonCol : new Color(0.25f, 0.25f, 0.27f), 22);
                UIFactory.Button(btns, "CANCEL", () => CancelOrder(oo), new Color(0.45f, 0.25f, 0.25f), 20);
            }
        }

        void MoveOrder(int idx, int dir)
        {
            var list = BaseManager.I.Orders;
            int j = idx + dir;
            if (j < 0 || j >= list.Count) return;
            (list[idx], list[j]) = (list[j], list[idx]);
            Rebuild();
        }
    }
}
