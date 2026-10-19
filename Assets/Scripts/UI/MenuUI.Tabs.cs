using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wildbond
{
    public partial class MenuUI
    {
        ScrollRect mainScroll;
        MenuTab scrollTab;
        float savedScroll = 1f;

        RectTransform TrackedList(Transform parent)
        {
            var list = UIFactory.ScrollList(parent, out var sr);
            ((RectTransform)list.parent).Stretch();
            if (mainScroll == null || scrollTab != Tab) savedScroll = 1f;
            mainScroll = sr;
            scrollTab = Tab;
            return list;
        }

        void RestoreScroll()
        {
            Canvas.ForceUpdateCanvases();
            if (mainScroll != null) mainScroll.verticalNormalizedPosition = savedScroll;
            foreach (var kv in sideActive)
                if (kv.Value != null && sideSaved.TryGetValue(kv.Key, out float v)) kv.Value.verticalNormalizedPosition = v;
        }

        void SaveScroll()
        {
            if (mainScroll != null && scrollTab == Tab) savedScroll = mainScroll.verticalNormalizedPosition;
            foreach (var kv in sideActive) if (kv.Value != null) sideSaved[kv.Key] = kv.Value.verticalNormalizedPosition;
            sideActive.Clear();
        }

        // ---- a second scrolling column (a detail or action panel beside the main list) that keeps its place when the
        // screen is rebuilt after a tap, instead of jumping back to the top (user, 2026-10-15)
        readonly Dictionary<string, float> sideSaved = new Dictionary<string, float>();
        readonly List<KeyValuePair<string, ScrollRect>> sideActive = new List<KeyValuePair<string, ScrollRect>>();
        RectTransform SideList(Transform parent, string key, float spacing = 8f)
        {
            var list = UIFactory.ScrollList(parent, out var sr, spacing);
            ((RectTransform)list.parent).Stretch();
            sideActive.Add(new KeyValuePair<string, ScrollRect>(Tab + ":" + key, sr));
            return list;
        }

        // ============================================================ BAG
        void BuildBag()
        {
            SaveScroll();
            var stats = GameManager.I.Player.Stats;
            LeftRight(out var left, out var right, 0.58f);
            var head = Header(left, $"Bag  <size=26>{UsedSlots(stats.Bag)}/{stats.Bag.Capacity} slots  ·  {UIFactory.Col($"Glimmer {stats.glimmer}", new Color(1f, 0.85f, 0.35f))}</size>");
            head.rectTransform.Anchors(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -60), Vector2.zero);

            var gridHost = UIFactory.Rect(left, "GridHost");
            gridHost.Anchors(Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -70));
            var list = TrackedList(gridHost);
            DestroyImmediate(list.GetComponent<VerticalLayoutGroup>());
            var grid = list.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(128, 128);
            grid.spacing = new Vector2(12, 12);
            grid.padding = new RectOffset(8, 8, 8, 8);

            for (int i = 0; i < stats.Bag.slots.Count; i++)
            {
                int idx = i;
                var s = stats.Bag.slots[i];
                var cell = UIFactory.Panel(list, "Slot", i == selectedSlot ? new Color(0.3f, 0.6f, 0.5f, 1f) : new Color(1, 1, 1, 0.08f));
                var b = cell.gameObject.AddComponent<Button>();
                b.onClick.AddListener(() => { selectedSlot = idx; SynthAudio.Play(Sfx.UiClick); Rebuild(); });
                if (s.Empty) continue;
                var def = ContentDB.Item(s.id);
                var icon = UIFactory.ItemIcon(cell.transform, def, 92);
                icon.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -8), new Vector2(92, 92));
                icon.raycastTarget = false;
                // its name on the slot: many ores, crystals and bars look alike (menu pass 2026-10-15)
                var nm = UIFactory.Label(cell.transform, ContentDB.ItemName(s.id), 15, new Color(1f, 1f, 1f, 0.85f), TextAnchor.UpperCenter, FontStyle.Bold);
                nm.rectTransform.Anchors(new Vector2(0, 1), new Vector2(1, 1), new Vector2(4, -26), new Vector2(-4, -2));
                nm.horizontalOverflow = HorizontalWrapMode.Wrap;
                nm.resizeTextForBestFit = true; nm.resizeTextMinSize = 10; nm.resizeTextMaxSize = 15;
                nm.raycastTarget = false;
                var cnt = UIFactory.Label(cell.transform, s.count > 1 ? "x" + s.count : "", 24, Color.white, TextAnchor.LowerRight, FontStyle.Bold);
                cnt.rectTransform.Stretch(4, 2, 8, 2);
                string tag = s.id == stats.held ? "E" : (stats.HotbarIndex(s.id) >= 0 ? (stats.HotbarIndex(s.id) + 1).ToString() : "");
                if (tag.Length > 0) { var eq = UIFactory.Label(cell.transform, tag, 22, UIFactory.Accent, TextAnchor.UpperLeft, FontStyle.Bold); eq.rectTransform.Stretch(8, 4, 4, 4); }
            }
            RestoreScroll();

            // detail
            var dv = right.gameObject.AddComponent<VerticalLayoutGroup>();
            dv.spacing = 14; dv.childControlHeight = true; dv.childControlWidth = true; dv.childForceExpandHeight = false;
            if (selectedSlot < 0 || selectedSlot >= stats.Bag.slots.Count || stats.Bag.slots[selectedSlot].Empty)
            {
                Header(right, "Select an item");
                Para(right, $"Food {Mathf.RoundToInt(stats.hunger)}/100   HP {Mathf.RoundToInt(stats.hp)}/{Mathf.RoundToInt(stats.MaxHp)}\n\n" +
                    "Tools in your bag are used automatically when you gather. Equip a weapon to hit harder.\n\n" +
                    (ItemSources.UsingStorage ? UIFactory.Col("You're in camp: crafting and building also draw from camp storage.", UIFactory.Accent) : ""));
                return;
            }
            var st = stats.Bag.slots[selectedSlot];
            var d = ContentDB.Item(st.id);
            var big = UIFactory.ItemIcon(right, d, 170);
            UIFactory.Size(big, 170);
            Header(right, $"{d.displayName}  <size=26>x{st.count}</size>");
            Para(right, $"{UIFactory.Col(d.category.ToString(), UIFactory.Accent)}\n{d.description}");
            string extra = "";
            if (d.foodValue > 0) extra += $"Food +{d.foodValue}  ";
            if (d.healValue > 0) extra += $"Heal +{d.healValue}  ";
            if (d.mealMods != null && d.mealMods.Length > 0)
            {
                var buffs = new List<string>();
                foreach (var m in d.mealMods) buffs.Add(GearInfo.Format(m));
                extra += $"\nMeal bonus: {string.Join(", ", buffs)} for {d.mealMinutes:0} min  ";
            }
            if (d.toolPower > 0) extra += $"{d.toolKind} tier {d.toolPower}  ";
            if (d.weaponDamage > 0) extra += $"Attack power {d.weaponDamage}  {HandStyles.Describe(HandStyles.Of(d))}  ";
            if (d.category == ItemCategory.CaptureSeed)
            {
                int cap = CaptureSystem.SeedLevelCap(d);
                extra += $"Works on kin {(cap >= 99 ? "of any level" : $"up to Lv {cap}")}  ·  catch x{CaptureSystem.SeedBonus(d):0.00}";
            }
            if (extra.Length > 0) Para(right, extra, 24, Color.white);

            var gm = GameManager.I;
            switch (d.category)
            {
                case ItemCategory.Food:
                    ActionBtn(right, "EAT", () => gm.UseItem(d.id));
                    HotbarRow(right, stats, d);
                    break;
                case ItemCategory.Consumable:
                    ActionBtn(right, "USE", () => gm.UseItem(d.id));
                    HotbarRow(right, stats, d);
                    break;
                case ItemCategory.Upgrade:
                    ActionBtn(right, "USE", () => gm.UseItem(d.id)); break;
                case ItemCategory.Weapon:
                case ItemCategory.Tool:
                {
                    var pl = gm.Player;
                    ActionBtn(right, stats.held == d.id ? "PUT AWAY (bare hands)" : "HOLD IN HAND", () => pl.ToggleHold(d.id));
                    HotbarRow(right, stats, d);
                    if (d.category == ItemCategory.Tool) Para(right, stats.autoTool ? "Auto-tool is ON: gathering picks your best tool automatically." : "Auto-tool is OFF: hold this tool to use it.", 20);
                    break;
                }
                case ItemCategory.KeyItem:
                    if (d.id == GameManager.PouchId)
                    {
                        Color on = new Color(0.25f, 0.5f, 0.32f), off = new Color(0.35f, 0.35f, 0.4f);
                        ActionBtn(right, stats.autoFeed ? "AUTO-FEED ME + CAMP KIN: ON" : "AUTO-FEED ME + CAMP KIN: OFF", () => { stats.autoFeed = !stats.autoFeed; Rebuild(); }, stats.autoFeed ? on : off);
                        ActionBtn(right, stats.autoTreat ? "AUTO KIN TREAT: ON" : "AUTO KIN TREAT: OFF", () => { stats.autoTreat = !stats.autoTreat; Rebuild(); }, stats.autoTreat ? on : off);
                        Para(right, "You eat at 35% food. Camp kin are fed while you stand in camp. The companion gets a treat under 30% HP.", 20);
                    }
                    break;
                case ItemCategory.CaptureSeed:
                    ActionBtn(right, stats.selectedSeed == d.id ? "SELECTED" : "SELECT SEED", () => { stats.selectedSeed = d.id; GameEvents.RaiseInventoryChanged(); }); break;
                case ItemCategory.Gear:
                {
                    var lines = new List<string>();
                    lines.Add(UIFactory.Col($"{d.rarity} {d.equipSlot}", RarityInfo.Color(d.rarity)));
                    if (d.mods != null) foreach (var m in d.mods) lines.Add(UIFactory.Col(GearInfo.Format(m), UIFactory.Accent));
                    if (d.unique != UniqueEffect.None) lines.Add(UIFactory.Col($"{GearInfo.EffectName(d.unique)}: {GearInfo.EffectDesc(d.unique)}", new Color(1f, 0.8f, 0.4f)));
                    Para(right, string.Join("\n", lines), 24, Color.white);
                    int gs = stats.SlotFor(d);
                    ActionBtn(right, "EQUIP", () => { stats.Equip(d.id); SynthAudio.Play(Sfx.Craft, 1.3f, 0.6f); selectedSlot = -1; GameEvents.RaiseInventoryChanged(); });
                    ActionBtn(right, "COMPARE IN GEAR TAB", () => { selectedGearSlot = gs; Open(MenuTab.Gear); }, new Color(0.3f, 0.4f, 0.55f));
                    break;
                }
            }
            if (BaseManager.I.HasBase && BaseManager.I.StorageHere() != null)   // standing at a chest or the Hearthstone
                ActionBtn(right, "STORE IN CAMP", () =>
                {
                    int left2 = BaseManager.I.Storage.Add(st.id, st.count);
                    stats.Bag.Remove(st.id, st.count - left2);
                    if (left2 > 0) GameEvents.Warn("Camp storage is full.");
                    GameEvents.RaiseInventoryChanged();
                });
            ActionBtn(right, "DROP 1", () => { stats.Bag.Remove(d.id, 1); GameEvents.RaiseInventoryChanged(); }, new Color(0.45f, 0.25f, 0.25f));
        }

        static int UsedSlots(Inventory inv)
        {
            int n = 0;
            foreach (var s in inv.slots) if (!s.Empty) n++;
            return n;
        }

        void ActionBtn(Transform parent, string label, System.Action a, Color? c = null)
        {
            var b = UIFactory.Button(parent, label, a, c ?? new Color(0.25f, 0.5f, 0.42f), 30);
            UIFactory.Size(b, 84);
        }

        /// <summary>Put an item on hotbar slot 1-6 (a slot already in use is marked *), or take it off.</summary>
        void HotbarRow(Transform right, PlayerStats stats, ItemDef d)
        {
            int hi = stats.HotbarIndex(d.id);
            if (hi >= 0) { ActionBtn(right, $"REMOVE FROM HOTBAR ({hi + 1})", () => { stats.hotbar[hi] = null; GameEvents.RaiseInventoryChanged(); }, new Color(0.35f, 0.35f, 0.4f)); return; }
            var row = UIFactory.HRow(right, 84, 8);
            var lbl = UIFactory.Label(row.transform, "HOTBAR:", 24, Color.white); UIFactory.Size(lbl, 80, 120);
            for (int k = 0; k < PlayerStats.HotbarSize; k++)
            {
                int slot = k;
                string cur = stats.hotbar[k];
                UIFactory.Size(UIFactory.Button(row.transform, string.IsNullOrEmpty(cur) ? $"{k + 1}" : $"{k + 1}*", () => { stats.SetHotbar(slot, d.id); GameEvents.RaiseInventoryChanged(); }, new Color(0.25f, 0.42f, 0.5f), 26), 80, 66);
            }
        }

        // ============================================================ CRAFT
        void BuildCraft()
        {
            SaveScroll();
            var bm = BaseManager.I;
            var list = TrackedList(content);
            Header(list, "Crafting");
            Para(list, "Hand recipes craft instantly, anywhere. Everything else is made at its own station in your camp - each one has its own menu.");

            // every station has its own screen; these open them from anywhere (view-only away from camp)
            var stations = RecipeStations();
            if (stations.Count > 0)
            {
                var srow = UIFactory.HRow(list, 92, 12);
                foreach (var st in stations)
                {
                    var s2 = st;
                    int n = ContentDB.RecipeList.FindAll(x => x.station == s2).Count;
                    int queued = 0;
                    foreach (var o in bm.Orders) if (ContentDB.Recipes.TryGetValue(o.recipeId, out var qr) && qr.station == s2) queued += o.remaining;
                    string label = $"{StationName(st).ToUpper()}\n{StationActivity(st)} · {n} recipes{(queued > 0 ? $" · {queued} queued" : "")}";
                    UIFactory.Size(UIFactory.Button(srow.transform, label, () => OpenStation(s2), bm.HasStation(st) ? new Color(0.3f, 0.4f, 0.55f) : new Color(0.3f, 0.3f, 0.34f), 22), 84, 420);
                }
            }

            Header(list, "By hand", 30);
            RecipeRows(list, CraftStation.Hand, 900);
            RestoreScroll();
        }

        static int CraftGroup(RecipeDef r)
        {
            var d = ContentDB.Item(r.outputItem);
            if (!PlayerStats.IsGear(d)) return 0;
            return GearSlots.IsArmor(d.equipSlot) ? 1 : 2;
        }

        void Craft(RecipeDef r)
        {
            var gm = GameManager.I;
            if (!ItemSources.Has(r.inputs)) return;
            if (r.station == CraftStation.Hand)
            {
                if (gm.Player.Stats.Bag.SpaceFor(r.outputItem) < r.outputCount) { GameEvents.Warn("Bag is full."); return; }
                ItemSources.Consume(r.inputs);
                gm.GiveItem(r.outputItem, r.outputCount, gm.Player.Position + Vector3.up);
                SynthAudio.Play(Sfx.Craft);
                gm.AddPlayerXp(2);
            }
            else
            {
                ItemSources.Consume(r.inputs);
                var bm = BaseManager.I;
                var existing = bm.Orders.Find(o => o.recipeId == r.id);
                if (existing != null) existing.remaining++;
                else bm.Orders.Add(new CraftOrder { recipeId = r.id, remaining = 1 });
                GameEvents.Good($"Queued {ContentDB.ItemName(r.outputItem)} at the {r.station}.");
                SynthAudio.Play(Sfx.Craft, 0.8f);
            }
            GameEvents.RaiseInventoryChanged();
        }

        void CancelOrder(CraftOrder o)
        {
            var r = ContentDB.Recipes[o.recipeId];
            foreach (var ing in r.inputs) BaseManager.I.Storage.Add(ing.itemId, ing.count * o.remaining);
            BaseManager.I.Orders.Remove(o);
            bool kept = o.auto && BaseManager.I.KeepOf(o.recipeId) > 0;
            if (kept) BaseManager.I.SetKeep(o.recipeId, 0);   // or it would be queued again in two seconds
            GameEvents.Say(kept ? $"Stopped keeping {ContentDB.ItemName(r.outputItem)} in stock - ingredients returned to camp storage." : "Order cancelled - ingredients returned to camp storage.");
            GameEvents.RaiseInventoryChanged();
        }

        // ============================================================ KIN
        void BuildKin()
        {
            SaveScroll();
            var party = GameManager.I.Party;
            var stats = GameManager.I.Player.Stats;
            LeftRight(out var left, out var right, 0.46f);
            var list = TrackedList(left);
            Header(list, $"Kin  <size=24>Registered {stats.registeredSpecies.Count}/{ContentDB.SpeciesList.Count}</size>");
            UIFactory.Size(UIFactory.Button(list, $"DREAM UP A KIN  ({stats.customKin.Count}/{CustomKinLimit})", ShowKinMaker, new Color(0.5f, 0.35f, 0.7f), 26), 74);
            foreach (var loc in new[] { KinLocation.Party, KinLocation.Base, KinLocation.Box, KinLocation.Expedition, KinLocation.Outpost })
            {
                int cnt = party.CountIn(loc);
                if ((loc == KinLocation.Expedition || loc == KinLocation.Outpost) && cnt == 0) continue;
                string cap = loc == KinLocation.Party ? $"{cnt}/{KinRoster.PartySize}" : (loc == KinLocation.Base ? $"{cnt}/{BaseManager.I.WorkerCap}" : cnt.ToString());
                Header(list, $"{(loc == KinLocation.Outpost ? "At Outposts" : loc == KinLocation.Expedition ? "On Expedition" : loc == KinLocation.Box ? "Hearth Box" : loc == KinLocation.Base ? "Camp Workers" : "Party")}  <size=22>{cap}</size>", 28);
                foreach (var d in party.In(loc))
                {
                    var dd = d;
                    var row = UIFactory.Panel(list, "KinRow", d.uid == selectedKin ? new Color(0.3f, 0.55f, 0.48f, 1f) : new Color(1, 1, 1, 0.06f));
                    UIFactory.Size(row, 92);
                    var b = row.gameObject.AddComponent<Button>();
                    b.onClick.AddListener(() => { selectedKin = dd.uid; SynthAudio.Play(Sfx.UiClick); Rebuild(); });
                    var ic = Portrait(row.transform, d, 76);
                    ic.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(76, 76));
                    string tags = (d.alpha ? " [A]" : "") + (d.shimmer ? " [*]" : "") + (party.Companion != null && party.Companion.Data == d ? UIFactory.Col(" OUT", UIFactory.Accent) : "");
                    string els = UIFactory.Col(d.Element1.ToString(), ElementInfo.Color(d.Element1)) + (d.Element2 != Element.Normal && d.Element2 != d.Element1 ? "/" + UIFactory.Col(d.Element2.ToString(), ElementInfo.Color(d.Element2)) : "");
                    var jobAt = d.location == KinLocation.Base && BaseManager.I != null ? BaseManager.I.FindBuilding(d.assignedBuilding) : null;
                    string extra = d.location == KinLocation.Base ? "  ·  " + (jobAt != null ? jobAt.Def.displayName : UIFactory.Col("no job", new Color(1f, 0.8f, 0.3f))) : "";
                    var t = UIFactory.Label(row.transform, $"<b>{d.DisplayName}</b>{tags}\n<size=20>Lv {d.level}  ·  {els}  ·  {UIFactory.Col(Aptitude.SpecLabel(d), Aptitude.GradeColor(d.Apt((KinPath)d.spec)))}  ·  {(d.Fainted ? UIFactory.Col("Fainted", new Color(1f, 0.5f, 0.4f)) : $"HP {Mathf.CeilToInt(d.CurrentHp)}/{Mathf.CeilToInt(d.MaxHp)}")}{extra}</size>", 26, Color.white);
                    t.supportRichText = true;
                    t.rectTransform.Stretch(96, 4, 8, 4);
                }
            }
            if (party.All.Count == 0) Para(list, "No kin yet. Weaken a wild kin, then throw a Tether Seed (SEED button).");
            RestoreScroll();

            // detail
            var d0 = party.Find(selectedKin);
            if (d0 == null && party.All.Count > 0) { d0 = party.All[0]; selectedKin = d0.uid; }   // never an empty page
            var rl = SideList(right, "kin:" + selectedKin);
            if (d0 == null) { Header(rl, "Select a kin"); return; }
            KinDetail(rl, d0);
            RestoreScroll();
        }

        /// <summary>Shimmer kin: its three colours as swatch buttons. With a Shimmer Prism in the bag, tapping one switches the coat.</summary>
        void ShimmerSection(RectTransform p, CreatureData d)
        {
            var sp = d.Species;
            bool prism = Shimmer.HasPrism;
            Para(p, prism ? "Shimmer colour - tap one to switch (Shimmer Prism):" : "Shimmer colours:", 20, Color.white);
            var row = UIFactory.HRow(p, 66, 10);
            for (int v = 0; v < Shimmer.Variants; v++)
            {
                int vv = v;
                var pal = Shimmer.PaletteFor(sp, v);
                bool cur = v == d.shimmerVariant;
                Color c = Color.Lerp(pal.primary, Color.black, 0.4f); c.a = 1f;
                string label = cur ? $"[ {Shimmer.NameFor(sp, v)} ]" : Shimmer.NameFor(sp, v);
                UIFactory.Size(UIFactory.Button(row.transform, label, prism && !cur ? () => { Shimmer.Recolor(d, vv); Rebuild(); } : (System.Action)null, c, 22), 60, 250);
            }
            if (!prism) Para(p, "A Shimmer Prism lets you switch between these. Workbench: 100 Prism Shards, mined from Prism Veins on the far, high slopes of the Rimefang Peaks.", 18);
        }

        void KinDetail(RectTransform p, CreatureData d)
        {
            var sp = d.Species;
            var party = GameManager.I.Party;
            string el = UIFactory.Col(d.Element1.ToString(), ElementInfo.Color(d.Element1)) + (d.Element2 != Element.Normal ? " / " + UIFactory.Col(d.Element2.ToString(), ElementInfo.Color(d.Element2)) : "");
            // ---- who it is, at a glance: its picture, name, level, elements, rarity, the main numbers (2026-10-15 menu pass)
            var top = UIFactory.Panel(p, "KinTop", new Color(1, 1, 1, 0.05f));
            UIFactory.Size(top, 236);
            var pic = Portrait(top.transform, d, 210);
            pic.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(210, 210));
            string tags = d.VariantTags();
            var headT = UIFactory.Label(top.transform,
                $"<size=36><b>{d.DisplayName}</b></size>  <size=26>Lv {d.level}</size>\n"
                + $"<size=20>#{sp.dexNumber:000} {(d.nickname != null ? d.RegionalName + "  ·  " : "")}{el}  ·  {(sp.starter ? UIFactory.Col("Partner", new Color(1f, 0.85f, 0.4f)) : UIFactory.Col(sp.rarity.ToString(), RarityInfo.Color(sp.rarity)))}  ·  {sp.temperament}</size>\n"
                + (tags.Length > 0 ? $"<size=19>{UIFactory.Col(tags, new Color(1f, 0.85f, 0.4f))}</size>\n" : "")
                + $"<size=22>HP {Mathf.CeilToInt(d.CurrentHp)}/{Mathf.CeilToInt(d.MaxHp)}   ATK {d.Atk:0}   DEF {d.Def:0}   SPD x{d.SpeedMul:0.00}</size>\n"
                + $"<size=19>{Aptitude.Summary(d)}</size>", 26, Color.white, TextAnchor.UpperLeft);
            headT.supportRichText = true;
            headT.horizontalOverflow = HorizontalWrapMode.Wrap;
            headT.rectTransform.Stretch(236, 10, 12, 30);
            var xpb = UIFactory.Bar(top.transform, new Color(0.5f, 0.75f, 1f), out var xpf);
            xpb.rectTransform.Anchors(new Vector2(0, 0), new Vector2(1, 0), new Vector2(236, 12), new Vector2(-12, 26));
            xpf.SetFill(d.xp / (float)CreatureData.XpToNext(d.level));
            KinMoveButtons(p, d);
            if (d.shimmer) ShimmerSection(p, d);
            Header(p, "About", 26);
            Para(p, $"<i>{sp.lore}</i>\nPersonality: {sp.personality}", 22);
            Para(p, "Looks: " + d.Look.Describe(sp) + "  <size=18>(its own; eggs take after their parents)</size>", 21, Color.white);
            Para(p, "Out with you: " + UIFactory.Col(KinAid.Describe(d.Element1, d.Element2), new Color(0.65f, 0.95f, 0.8f)) + "  <size=18>(travel help from its element)</size>", 21, Color.white);
            Header(p, "Quality and traits", 26);
            Para(p, "Quality: " + Aptitude.Summary(d) + $"\n<size=19>Combat x{d.CombatAptMul:0.00} stats  ·  Work x{d.WorkAptMul:0.00} speed  ·  Travel x{d.TravelAptMul:0.00} speed, x{d.MountStaminaMul:0.00} riding stamina</size>", 22, Color.white);

            var traits = new List<string>();
            foreach (var t in d.traits) { var td = ContentDB.Trait(t); if (td != null) traits.Add(UIFactory.Col(td.displayName, td.positive ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.6f, 0.5f)) + $" <size=18>({td.description})</size>"); }
            Para(p, "Traits: " + (traits.Count > 0 ? string.Join("\n  ", traits) : "none"), 22, Color.white);

            var abil = new List<string>();
            var learnset = d.Learnset;
            for (int si = 0; si < learnset.Length; si++)
            {
                var ad = ContentDB.Ability(learnset[si]);
                if (ad == null) continue;
                bool known = si < d.SkillSlots;
                bool rare = sp.rareAbilities != null && System.Array.IndexOf(sp.rareAbilities, learnset[si]) >= 0;
                string line = (known ? UIFactory.Col(ad.displayName, ElementInfo.Color(ad.element)) : UIFactory.Col(ad.displayName, new Color(0.55f, 0.57f, 0.62f)))
                    + $" <size=18>{ad.kind} · pow {ad.power} · {ad.cooldown:0.#}s{(ad.status != StatusType.None ? $" · {ad.status} {ad.statusChance * 100:0}%" : "")}{(SkillEffects.Describe(ad).Length > 0 ? " · " + SkillEffects.Describe(ad) : "")}{(rare ? " · RARE COMBO" : "")}</size>";
                if (!known) line += UIFactory.Col($"  (learns at Lv {CreatureData.SlotUnlockLevel(si)})", new Color(1f, 0.8f, 0.4f));
                abil.Add(line);
            }
            Header(p, $"Skills  <size=20>{d.SkillSlots}/{CreatureData.MaxSkills} known</size>", 26);
            Para(p, "  " + string.Join("\n  ", abil), 22, Color.white);
            Header(p, "Camp work", 26);

            var work = new List<string>();
            foreach (var w in sp.work) work.Add(d.JobOn(w.type) ? $"{w.type} {new string('*', d.JobLevel(w.type))}" : UIFactory.Col($"{w.type} (off)", new Color(0.6f, 0.6f, 0.65f)));
            Para(p, $"Camp work: {string.Join(",  ", work)}{(d.shimmer && sp.work.Length > 0 ? UIFactory.Col("  (+1 each: Shimmer)", new Color(1f, 0.85f, 0.4f)) : "")}\n{(d.Nocturnal ? UIFactory.Col("Up all night: keeps working at camp after dark.", new Color(0.75f, 0.8f, 1f)) : $"Sleeps at camp from dusk to dawn, in a {BaseManager.BedNeeded(d)} or bigger." + (d.location == KinLocation.Base && BaseManager.I != null && BaseManager.I.HasBase ? " " + BaseManager.I.SleepsIn(d) + "." : ""))}\n{sp.overworldRole}", 22, UIFactory.Accent);
            if (sp.work.Length > 0)
            {
                // job switches: what this kin may do at camp (and when it helps you gather)
                var jr = UIFactory.HRow(p, 64, 8);
                foreach (var w in sp.work)
                {
                    var wt = w.type;
                    bool on = d.JobOn(wt);
                    UIFactory.Size(UIFactory.Button(jr.transform, $"{wt}\n{(on ? "ON" : "OFF")}", () => { d.SetJob(wt, !d.JobOn(wt)); SynthAudio.Play(Sfx.UiClick); Rebuild(); },
                        on ? new Color(0.25f, 0.5f, 0.42f) : new Color(0.32f, 0.32f, 0.36f), 19), 60, 150);
                }
            }
            if (d.location == KinLocation.Base)
            {
                var bm = BaseManager.I;
                var b = bm.FindBuilding(d.assignedBuilding);
                var a = bm.ActorFor(d);
                Para(p, $"Assigned: {(b != null ? b.Def.displayName : "nothing")}   ·   Hunger {d.hunger:0}%   ·   {(a != null ? a.WorkStatus : "Working (remote)")}", 22, Color.white);
            }

            // bond
            Header(p, "Bond and name", 26);
            int bl = d.BondLevel;
            float lo = CreatureData.BondThresholds[bl], hi = bl >= CreatureData.MaxBond ? lo : CreatureData.BondThresholds[bl + 1];
            Para(p, $"{UIFactory.Col($"Bond Lv {bl}/{CreatureData.MaxBond}", new Color(1f, 0.6f, 0.78f))}  <size=19>({d.bond:0}/{GameManager.NextBond(d)})  {CreatureData.BondPerks(bl)}</size>", 22, Color.white);
            var brow = UIFactory.HRow(p, 70, 12);
            var bbar = UIFactory.Bar(brow.transform, new Color(1f, 0.55f, 0.75f), out var bfill); UIFactory.Size(bbar, 22, 420);
            bfill.SetFill(bl >= CreatureData.MaxBond ? 1f : (d.bond - lo) / Mathf.Max(1f, hi - lo));
            UIFactory.Size(UIFactory.Button(brow.transform, "FEED A TREAT", () => { GameManager.I.FeedKin(d); Rebuild(); }, new Color(0.6f, 0.35f, 0.5f), 20), 64, 240);
            // a name of your own for it (empty = back to its kind's name)
            var nrow = UIFactory.HRow(p, 70, 12);
            UIFactory.Size(UIFactory.Label(nrow.transform, "Name", 24, Color.white), 64, 110);
            var nameIn = UIFactory.Input(nrow.transform, d.nickname ?? "", d.RegionalName, 26);
            UIFactory.Size(nameIn, 64, 360);
            nameIn.characterLimit = 14;
            nameIn.onEndEdit.AddListener(v => { d.nickname = string.IsNullOrWhiteSpace(v) ? null : v.Trim(); GameEvents.RaiseRosterChanged(); });
            if (d.location == KinLocation.Expedition) { Para(p, UIFactory.Col("Away on an expedition (Camp > Expeditions).", Gold), 24); return; }
            if (d.location == KinLocation.Outpost) { Para(p, UIFactory.Col("Stationed at an outpost (Camp > Outposts to recall it).", Gold), 24); return; }

            // evolution
            Header(p, "Evolution", 26);
            if (KinGrowth.CanEvolveStage(d))
            {
                var nextForm = d.Species.FormAt(d.stage + 1);
                string nextName = nextForm != null ? nextForm.name : CreatureData.StageName(d.stage + 1);
                string why = KinGrowth.WhyNot(d);
                int g = KinGrowth.EvolveGlimmer(d);
                Para(p, $"{UIFactory.Col($"Evolve to {nextName}", new Color(0.75f, 0.6f, 1f))} at Lv {KinGrowth.EvolveLevel(d)}: {Ingredients(KinGrowth.EvolveCost(d))}{(g > 0 ? $"   {UIFactory.Col($"{g} Glimmer", Gold)}" : "")}\n<size=19>{(nextForm != null ? EvoChangeText(nextForm) + "  " : "")}+15% HP/ATK/DEF, {(nextForm != null ? "bigger" : "bigger glowing form")}, +10% work speed</size>", 22, Color.white);
                var dd = d;
                UIFactory.Size(UIFactory.Button(p, why == null ? $"EVOLVE INTO {nextName.ToUpperInvariant()}" : why,
                    why == null ? () => Confirm("Evolve?", $"Evolve {dd.DisplayName} into its {nextName} form? Materials are used up.", () => EvolveWithShow(dd, false)) : (System.Action)null,
                    why == null ? new Color(0.5f, 0.35f, 0.75f) : new Color(0.3f, 0.3f, 0.32f), 22), 70);
            }
            else Para(p, UIFactory.Col("Fully evolved (Exalted).", new Color(0.75f, 0.6f, 1f)), 22);

            var row2 = UIFactory.HRow(p, 84, 12);
            UIFactory.Size(UIFactory.Button(row2.transform, "RELEASE", () => Confirm("Release kin?", $"Release {d.DisplayName} back into the wild? This can't be undone.", () => { party.Release(d); selectedKin = null; Rebuild(); }), new Color(0.5f, 0.25f, 0.25f)), 80, 250);
        }

        /// <summary>SUMMON / RECALL and TO PARTY / CAMP / BOX, just under the kin's picture (they were at the very bottom).</summary>
        void KinMoveButtons(RectTransform p, CreatureData d)
        {
            var party = GameManager.I.Party;
            if (d.location == KinLocation.Expedition || d.location == KinLocation.Outpost) return;
            bool inCamp = BaseManager.I.HasBase && BaseManager.I.IsInsideBase(GameManager.I.Player.Position, 0f);
            var row1 = UIFactory.HRow(p, 76, 10);
            if (d.location == KinLocation.Party)
            {
                bool isOut = party.Companion != null && party.Companion.Data == d;
                UIFactory.Size(UIFactory.Button(row1.transform, isOut ? "RECALL" : "SUMMON", () => { if (isOut) party.Recall(); else party.Summon(d); Rebuild(); }, new Color(0.3f, 0.45f, 0.8f), 22), 72, 190);
            }
            if (d.location != KinLocation.Party) UIFactory.Size(UIFactory.Button(row1.transform, "TO PARTY", () => MoveKin(d, KinLocation.Party, inCamp), UIFactory.ButtonCol, 22), 72, 190);
            if (d.location != KinLocation.Base) UIFactory.Size(UIFactory.Button(row1.transform, "TO CAMP", () => MoveKin(d, KinLocation.Base, inCamp), UIFactory.ButtonCol, 22), 72, 190);
            if (d.location != KinLocation.Box) UIFactory.Size(UIFactory.Button(row1.transform, "TO BOX", () => MoveKin(d, KinLocation.Box, inCamp), UIFactory.ButtonCol, 22), 72, 190);
            UIFactory.Size(UIFactory.Button(row1.transform, "KIN CARD", () => ShowKinCard(d), new Color(0.3f, 0.42f, 0.7f), 22), 72, 190);
        }

        void MoveKin(CreatureData d, KinLocation to, bool inCamp)
        {
            if (!inCamp && (to == KinLocation.Base || to == KinLocation.Box || d.location != KinLocation.Party))
            {
                GameEvents.Warn("Stand inside your camp to move kin between party, camp and box.");
                return;
            }
            GameManager.I.Party.Move(d, to);
            Rebuild();
        }

        // ============================================================ BUILD
        void BuildBuild()
        {
            SaveScroll();
            var bm = BaseManager.I;
            var list = TrackedList(content);
            var lvl = GameManager.I.Player.Stats.level;
            if (bm.HasBase)
            {
                // camp structures, or your house piece by piece (Housing): a switch fixed at the top, the camp's category tabs under it
                var sw = FixedBar(list, 70, "BuildSwitch");
                var swRow = sw.gameObject.AddComponent<HorizontalLayoutGroup>();
                swRow.spacing = 10; swRow.childControlHeight = true; swRow.childControlWidth = true; swRow.childForceExpandHeight = true; swRow.childForceExpandWidth = true;
                UIFactory.Button(sw, "CAMP STRUCTURES", () => { buildHouse = false; mainScroll = null; Rebuild(); }, !buildHouse ? new Color(0.62f, 0.48f, 0.22f) : new Color(0.3f, 0.3f, 0.34f), 26);
                UIFactory.Button(sw, "HOUSE", () => { buildHouse = true; mainScroll = null; Rebuild(); }, buildHouse ? new Color(0.62f, 0.48f, 0.22f) : new Color(0.3f, 0.3f, 0.34f), 26);
                if (buildHouse) { BuildHouseList(list); RestoreScroll(); return; }
            }
            else Header(list, "Build");
            Para(list, bm.HasBase ? "Structures must be placed inside your camp ring." : UIFactory.Col("Start by placing a Hearthstone - it founds your camp.", UIFactory.Accent));
            var defs = ContentDB.BuildingList.FindAll(x => x.isCore != bm.HasBase);
            // split into category tabs once the camp exists (MenuUI.Categories)
            int tab = BuildTabs(list, defs, lvl);
            if (tab == ReadyTab) defs = defs.FindAll(x => BuildBlock(x, lvl) == null);
            else if (tab != AllTab) defs = defs.FindAll(x => (int)BuildCatOf(x) == tab);
            if (tab != AllTab) defs.Sort((x, y) => x.unlockLevel.CompareTo(y.unlockLevel));
            if (defs.Count == 0) Para(list, "Nothing you can build right now. The other tabs show what each structure costs.", 22);
            foreach (var d in defs)
            {
                var row = UIFactory.HRow(list, 110, 14);
                row.childForceExpandHeight = false;
                var ic = UIFactory.Rect(row.transform, "I").gameObject.AddComponent<Image>();
                ic.sprite = ItemIcons.Get("bld_" + d.id); ic.preserveAspect = true; ic.raycastTarget = false;
                if (ic.sprite == null) ic.color = new Color(1, 1, 1, 0.08f);
                UIFactory.Size(ic, 96, 96);
                var tx = UIFactory.Label(row.transform, $"<b>{d.displayName}</b>  <size=20>{(d.workType != WorkType.None ? d.workType + " x" + d.workerSlots : "")}</size>\n<size=20>{d.description}{(Tech.BuildingUnlocked(d.id) ? "" : UIFactory.Col($"  Locked: research {Tech.ForBuilding(d.id).name} at a Research Bench.", new Color(1f, 0.8f, 0.5f)))}</size>\n<size=20>{Ingredients(d.cost)}</size>", 26, Color.white);
                tx.supportRichText = true;
                UIFactory.Size(tx, 104, 990);
                var tech = Tech.BuildingUnlocked(d.id) ? null : Tech.ForBuilding(d.id);
                string reason = tech != null ? "Research" : (BaseManager.I != null ? !BaseManager.I.LevelOk(d) : d.unlockLevel > lvl) ? $"Lv {d.unlockLevel}" : (!ItemSources.Has(d.cost) ? "Missing" : null);
                var dd = d;
                var btn = UIFactory.Button(row.transform, reason ?? "BUILD", reason == null ? () => { Close(); BuildPlacement.I.Begin(dd); } : (System.Action)null,
                    reason == null ? new Color(0.5f, 0.4f, 0.2f) : new Color(0.3f, 0.3f, 0.32f), 28);
                UIFactory.Size(btn, 90, 220);
            }
            RestoreScroll();
        }

        bool buildHouse;

        /// <summary>BUILD > HOUSE: floors, walls, doorways, windows, roofs and furniture (Housing), plus the REMOVE tool.</summary>
        void BuildHouseList(RectTransform list)
        {
            Para(list, "Your house goes inside the camp ring, a 3 m square at a time. Lay <b>floors</b> first, then walls, doorways and windows along their edges (stand facing the edge; ROTATE steps round the square), then <b>roofs</b>. Furniture goes on floors. A <b>Bed</b> with a roof over it lets you sleep through the night.", 21);
            UIFactory.Size(UIFactory.Button(list, "REMOVE A PIECE  (half the materials back)", () => { Close(); BuildPlacement.I.BeginRemove(); }, new Color(0.5f, 0.3f, 0.28f), 22), 64);
            // structure first, then furniture, then kin play pieces; blueprint pieces you haven't found are shown greyed with where to look
            string[] heads = { "Structure", "Furniture", "Kin play  <size=20>(idle camp kin play here and come back relaxed: +25% work for 5 minutes)</size>" };
            for (int grp = 0; grp < 3; grp++)
            {
                Header(list, heads[grp], 28);
                foreach (var d in Housing.Pieces)
                {
                    int g = d.kind != HouseKind.Furniture ? 0 : d.play ? 2 : 1;
                    if (g != grp) continue;
                    bool locked = Housing.Locked(d);
                    var row = UIFactory.HRow(list, 96, 14);
                    row.childForceExpandHeight = false;
                    string where = d.blueprint ? UIFactory.Col(locked ? $"  Blueprint: {Chests.WhereFound(d.land, d.caveTier)}." : d.caveTier > 0 ? "  Blueprint from the caves." : $"  Blueprint from {BiomeInfo.Name(d.land)}.", locked ? new Color(1f, 0.8f, 0.5f) : new Color(0.6f, 0.9f, 1f)) : "";
                    var tx = UIFactory.Label(row.transform, $"<b>{d.name}</b>{(d.ground ? "  <size=19>(floor or open ground)</size>" : "")}\n<size=20>{d.desc}{where}</size>\n<size=20>{Ingredients(d.cost)}</size>", 26, locked ? new Color(0.7f, 0.7f, 0.72f) : Color.white);
                    tx.supportRichText = true;
                    UIFactory.Size(tx, 92, 1090);
                    var dd = d;
                    bool can = !locked && (WorldRules.Current.freeBuild || ItemSources.Has(d.cost));
                    UIFactory.Size(UIFactory.Button(row.transform, locked ? "Blueprint" : can ? "BUILD" : "Missing", can ? () => { Close(); BuildPlacement.I.BeginPiece(dd); } : (System.Action)null,
                        can ? new Color(0.5f, 0.4f, 0.2f) : new Color(0.3f, 0.3f, 0.32f), 26), 84, 200);
                }
            }
        }

        // ============================================================ SKILLS
        void BuildSkills()
        {
            var s = GameManager.I.Player.Stats;
            var list = TrackedList(content);
            Header(list, $"Explorer Lv {s.level}  <size=26>XP {s.xp}/{PlayerStats.XpToNext(s.level)}  ·  Skill points: {UIFactory.Col(s.skillPoints.ToString(), UIFactory.Accent)}</size>");
            // how far to the next level, as a bar
            var xpRow = UIFactory.HRow(list, 26, 0);
            var xpBg = UIFactory.Panel(xpRow.transform, "XpBar", new Color(0.12f, 0.14f, 0.18f, 1f));
            UIFactory.Size(xpBg, 22, 900);
            var xpFill = UIFactory.Panel(xpBg.transform, "Fill", new Color(0.45f, 0.8f, 1f, 1f));
            xpFill.rectTransform.anchorMin = Vector2.zero;
            xpFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(s.xp / (float)Mathf.Max(1, PlayerStats.XpToNext(s.level))), 1f);
            xpFill.rectTransform.offsetMin = xpFill.rectTransform.offsetMax = Vector2.zero;
            Para(list, s.skillPoints > 0 ? UIFactory.Col($"You have {s.skillPoints} skill point{(s.skillPoints == 1 ? "" : "s")} to spend - tap + on a skill.", UIFactory.Accent)
                                         : "Earn XP by capturing (new species give double), defeating kin, gathering, crafting and building. Each level gives a skill point.");
            foreach (PlayerSkill sk in PlayerStats.ActiveSkills)
            {
                var k = sk;
                var row = UIFactory.HRow(list, 96, 14);
                int r = s.Rank(sk);
                var t = UIFactory.Label(row.transform, $"<b>{PlayerStats.SkillName(sk)}</b>  {r}/{PlayerStats.MaxSkillRank}\n<size=21>{PlayerStats.SkillDesc(sk)}</size>", 28, Color.white);
                t.supportRichText = true;
                UIFactory.Size(t, 90, 900);
                // a pip per rank, filled up to the rank you have
                for (int i = 0; i < PlayerStats.MaxSkillRank; i++)
                {
                    var pip = UIFactory.Panel(t.transform, "Pip", i < r ? new Color(0.45f, 0.85f, 0.55f, 1f) : new Color(1f, 1f, 1f, 0.12f));
                    pip.raycastTarget = false;
                    pip.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(330 + i * 30, -24), new Vector2(24, 14));
                }
                bool can = s.skillPoints > 0 && r < PlayerStats.MaxSkillRank;
                var b = UIFactory.Button(row.transform, r >= PlayerStats.MaxSkillRank ? "MAX" : "+", can ? () => { if (s.TrySpend(k)) { SynthAudio.Play(Sfx.LevelUp, 1.2f, 0.6f); GameEvents.RaiseInventoryChanged(); } } : (System.Action)null,
                    can ? new Color(0.25f, 0.55f, 0.4f) : new Color(0.24f, 0.25f, 0.28f), r >= PlayerStats.MaxSkillRank ? 26 : 40);
                if (!can) b.GetComponentInChildren<Text>().color = new Color(1f, 1f, 1f, 0.3f);
                UIFactory.Size(b, 90, 120);
            }

            // hidden relics
            var rel = Relics.I;
            int total = rel != null && rel.Ready ? rel.Total : Relics.IslandTypes * Relics.PerType + ((int)RelicType.Count - Relics.IslandTypes) * Relics.OuterPerType;
            Header(list, $"Relics  <size=24>{s.relicsFound.Count}/{total} found</size>", 32);
            Para(list, "Relics hide in hard-to-reach places: high mountain ledges, summits, mesa tops, canyon floors, out on lakes. Wardstones, Kinhearts and Glintstones lie across the sea, one of each on every outer land's high ground. You'll sense one when you get close. Each gives a permanent bonus.", 21);
            for (int t = 0; t < (int)RelicType.Count; t++)
            {
                var rt = (RelicType)t;
                int n = s.RelicCount(rt);
                Para(list, $"{UIFactory.Col(Relics.TypeName(rt), Relics.TypeColor(rt))}  {n}/{Relics.CountOf(rt)}   <size=20>{(n > 0 ? Relics.TypeBonusTotal(rt, n) : Relics.TypeBonus(rt) + " each")}</size>", 24, Color.white);
            }
        }

        // ============================================================ MAP
        void BuildMap()
        {
            if (Caves.InCave && Caves.I.MapTexture != null) { BuildCaveMap(); return; }
            var ws = Waystones.I;
            var map = MapView.Create(content, new Vector2(900, 900), ConfirmTravel);
            map.GetComponent<RectTransform>().Place(new Vector2(0.4f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 900));

            var legend = UIFactory.Rect(content, "Legend");
            legend.Anchors(new Vector2(0.78f, 0), new Vector2(1, 1), new Vector2(0, 20), new Vector2(-20, -20));
            var v = legend.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 10; v.childControlHeight = true; v.childControlWidth = true; v.childForceExpandHeight = false;
            Header(legend, "Rimewild Isle", 32);
            System.Func<BiomeId, string, string> land = (bi, text) => UIFactory.Col(BiomeInfo.Name(bi), BiomeInfo.Tint(bi)) + " " + text;
            Para(legend, land(BiomeId.Meadow, "(south) - hills, lakes, forests. Lv 1-10.") + "\n" +
                         land(BiomeId.Badlands, "(east) - mesas, ore, cinder. Lv 8-20.") + "\n" +
                         land(BiomeId.Peaks, "(north-west) - snow, crystal. Lv 12-28.") + "\n" +
                         "<size=17>" + UIFactory.Col("Outer lands - surf across on a water kin:", UIFactory.TextDim) + "</size>\n" +
                         land(BiomeId.Isles, "(south-west) - shallow lagoon, islets. Lv 5-12.") + "\n" +
                         land(BiomeId.Swamp, "(east) - mangroves and mud. Lv 12-20.") + "\n" +
                         land(BiomeId.Desert, "(north-east) - dunes, hot by day. Lv 20-29.") + "\n" +
                         land(BiomeId.Blight, "(north) - poison pools. Lv 28-38.") + "\n" +
                         land(BiomeId.Glow, "(west) - glowing forest. Lv 34-45.") + "\n" +
                         "<size=17>Drag to pan, pinch or scroll to zoom, tap a waystone to preview it.</size>", 19, Color.white);

            Header(legend, $"Fast travel  <size=22>{ws.All.FindAll(x => ws.IsActive(x)).Count}/{ws.All.Count} attuned</size>", 28);
            var tl = UIFactory.ScrollList(legend, out _);
            UIFactory.Size(tl.parent.GetComponent<RectTransform>(), 360);
            if (BaseManager.I.HasBase) UIFactory.Size(UIFactory.Button(tl, "Camp (Hearthstone)", () => map.Select(Waystones.CampId, "your camp", BaseManager.I.CorePosition), new Color(0.25f, 0.5f, 0.42f), 22), 64);
            if (BaseManager.I.HasBuilding("waypad"))
                foreach (var op in Outposts.All)
                {
                    var oid = Outposts.TravelId(op); var oname = Outposts.LandName(op) + " outpost"; var opos = op.Pos;
                    UIFactory.Size(UIFactory.Button(tl, "Outpost: " + Outposts.LandName(op), () => map.Select(oid, oname, opos), new Color(0.5f, 0.42f, 0.22f), 22), 64);
                }
            foreach (var w in ws.All)
            {
                if (!ws.IsActive(w)) continue;
                var wid = w.id; var wname = w.name; var wpos = w.pos;
                UIFactory.Size(UIFactory.Button(tl, w.name.Replace(" Waystone", ""), () => map.Select(wid, wname, wpos), new Color(0.2f, 0.42f, 0.55f), 22), 64);
            }
            if (ws.StatsRef().waystonesActive.Count == 0) Para(tl, "Find glowing waystones in the world and ATTUNE them to travel here instantly.", 20);
        }

        void ConfirmTravel(string id, string name)
        {
            if (!Waystones.I.CanTravel(out string reason)) { if (!string.IsNullOrEmpty(reason)) GameEvents.Warn(reason); return; }
            Confirm("Fast travel?", $"Travel to {name}?", () => { Close(); Waystones.I.TravelTo(id); });
        }

        // ============================================================ SETTINGS
        void BuildSettings()
        {
            var gm = GameManager.I;
            var list = TrackedList(content);
            Header(list, "Settings");
            var lookRow = UIFactory.HRow(list, 84, 12);
            UIFactory.Size(UIFactory.Button(lookRow.transform, "MY LOOK  (body, hair, outfit, colours)", OpenLook, new Color(0.5f, 0.35f, 0.7f), 26), 80, 560);
            UIFactory.Size(UIFactory.Button(lookRow.transform, "PHOTO MODE", () => { Close(); PhotoMode.Enter(); }, new Color(0.3f, 0.42f, 0.7f), 26), 80, 320);
            if (GameInfo.DevBuild)
            {
                // the tester's switch: adds a DEV button to the HUD and the DEV menu (Core/Dev.cs)
                var devRow = UIFactory.HRow(list, 84, 12);
                bool dev = gm.Settings.devMode;
                UIFactory.Size(UIFactory.Button(devRow.transform, "DEV MODE: " + (dev ? "ON" : "OFF"), () => { gm.Settings.devMode = !gm.Settings.devMode; gm.Settings.Save(); if (!gm.Settings.devMode) Dev.AllOff(); ApplyKidTabs(); Rebuild(); },
                    dev ? new Color(0.5f, 0.28f, 0.5f) : UIFactory.ButtonCol, 26), 80, 330);
                if (dev) UIFactory.Size(UIFactory.Button(devRow.transform, "OPEN DEV MENU", OpenDev, new Color(0.5f, 0.28f, 0.5f), 26), 80, 330);
                var dl = UIFactory.Label(devRow.transform, dev ? "Give, spawn, force and travel. Test on a spare world." : "For testing: a menu to give, spawn and force anything.", 20, UIFactory.TextDim);
                UIFactory.Size(dl, 80, 520);
            }
            var q = UIFactory.HRow(list, 84, 12);
            var ql = UIFactory.Label(q.transform, "Graphics", 26, Color.white); UIFactory.Size(ql, 80, 300);
            foreach (QualityTier t in System.Enum.GetValues(typeof(QualityTier)))
            {
                var tt = t;
                UIFactory.Size(UIFactory.Button(q.transform, t.ToString().ToUpperInvariant(), () => { gm.Settings.quality = (int)tt; gm.ApplySettings(); Rebuild(); },
                    QualityProfile.Current.tier == t ? new Color(0.25f, 0.55f, 0.45f) : UIFactory.ButtonCol, 26), 80, 220);
            }
            Para(list, "Low: 30 fps, short view, no shadows. Medium: 60 fps. High: long view distance, more wildlife, shadows.", 20);
            MakeSlider(list, "Look sensitivity", Mathf.InverseLerp(0.3f, 2.5f, gm.Settings.lookSensitivity), v => { gm.Settings.lookSensitivity = Mathf.Lerp(0.3f, 2.5f, v); gm.ApplySettings(); });
            MakeSlider(list, "Music", gm.Settings.musicVolume, v => { gm.Settings.musicVolume = v; gm.ApplySettings(); });
            MakeSlider(list, "Sound FX", gm.Settings.sfxVolume, v => { gm.Settings.sfxVolume = v; gm.ApplySettings(); });
            var toggles = UIFactory.HRow(list, 84, 12);
            UIFactory.Size(UIFactory.Button(toggles.transform, "Invert Y: " + (gm.Settings.invertY ? "ON" : "OFF"), () => { gm.Settings.invertY = !gm.Settings.invertY; gm.ApplySettings(); Rebuild(); }), 80, 300);
            UIFactory.Size(UIFactory.Button(toggles.transform, "FPS: " + (gm.Settings.showFps ? "ON" : "OFF"), () => { gm.Settings.showFps = !gm.Settings.showFps; gm.ApplySettings(); Rebuild(); }), 80, 240);
            UIFactory.Size(UIFactory.Button(toggles.transform, "Camp ring: " + (gm.ShowCampRing ? "ON" : "OFF"), () => { gm.ShowCampRing = !gm.ShowCampRing; Rebuild(); }), 80, 320);
            UIFactory.Size(UIFactory.Button(toggles.transform, "Auto-tool: " + (gm.Player.Stats.autoTool ? "ON" : "OFF"), () => { gm.Player.Stats.autoTool = !gm.Player.Stats.autoTool; Rebuild(); }), 80, 300);

            ToyBoxSection(list);   // only on worlds created with the Toy Box on (MenuUI.ToyBox)

            Header(list, "Save", 30);
            var sv = UIFactory.HRow(list, 84, 12);
            UIFactory.Size(UIFactory.Button(sv.transform, "SAVE NOW", () => { SaveSystem.Save(); GameEvents.Good("Game saved."); }, new Color(0.25f, 0.5f, 0.42f)), 80, 280);
            UIFactory.Size(UIFactory.Button(sv.transform, "RELOAD", () => { Close(); SaveSystem.LoadAndApply(); }), 80, 220);
            UIFactory.Size(UIFactory.Button(sv.transform, "SAVE & MAIN MENU", () => { Close(); gm.QuitToMenu(); }, new Color(0.35f, 0.3f, 0.5f)), 80, 380);
            var w = WorldRules.Current;
            Header(list, $"World: {w.worldName}  <size=22>{w.preset} rules · seed {w.seed}</size>", 30);
            Para(list, $"Spawns x{w.wildSpawnRate:0.##} · Capture x{w.captureRate:0.##} · Shimmer x{w.shimmerRate:0.##} · XP x{w.expRate:0.##} · Gather x{w.gatherRate:0.##} · Regrowth x{w.resourceRespawnRate:0.##}\n" +
                       $"Damage taken x{w.wildDamage:0.##} · dealt x{w.playerDamage:0.##} · Hunger x{w.hungerRate:0.##} · Stamina x{w.staminaDrain:0.##} · Day {w.dayLengthMinutes:0} min\n" +
                       $"Camp workers {w.campWorkersBase} (+{w.campWorkersPerLevel}/upgrade) · Work x{w.workSpeed:0.##} · Alpha respawn {w.alphaRespawnMinutes:0} min · Fast travel {(w.fastTravel ? "on" : "off")} · Offline progress {(w.offlineProgress ? "on" : "off")}", 20, Color.white);
            var offRow = UIFactory.HRow(list, 76, 12);
            UIFactory.Size(UIFactory.Button(offRow.transform, "CAMP WORKS WHILE THE GAME IS CLOSED: " + (w.offlineProgress ? "ON" : "OFF"), () => { w.offlineProgress = !w.offlineProgress; Rebuild(); },
                w.offlineProgress ? new Color(0.5f, 0.38f, 0.25f) : new Color(0.3f, 0.36f, 0.42f), 22), 72, 760);
            Para(list, "<size=19>" + UIFactory.Col("Off = your camp only works while you play (sleeping in a bed still lets it work through the night).", UIFactory.TextDim) + "</size>", 19);
            Para(list, $"Autosaves every {gm.AutosaveInterval:0}s and when the app is paused.\nSave file: {SaveSystem.FilePath}", 20);
            Para(list, "Controls (keyboard): WASD move · Right-mouse look · Wheel zoom · F/Click attack · G seed · E use · Space jump · Q roll · C sneak · Shift hold-sprint · Pad L3 run toggle · 1-4 hotbar · R kin · K kin bar · Y companion behavior · T kin skill · B build · Tab menu · X rotate · Enter place", 20);
        }
    }
}
