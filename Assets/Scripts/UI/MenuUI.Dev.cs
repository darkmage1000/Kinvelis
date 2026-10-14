using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>
    /// The DEV menu (hidden tab MenuTab.Dev; Settings > DEV MODE, then the DEV button on the HUD or OPEN DEV MENU).
    /// Five pages: CHEATS (switches and the explorer), ITEMS (packs, then any item), KIN (spawn wild or add to your
    /// roster, at any level, alpha or shimmer; the kin that is out; wild kin nearby), CAMP (power, steps, orders,
    /// Hearthstone, raid, spoiling, unlock everything) and WORLD (time, weather, events, chests, travel).
    /// Every button calls one method of `Dev`: this file is layout only.
    /// </summary>
    public partial class MenuUI
    {
        int devTab, devItemCat, devKinLevel = 10;
        bool devAlpha, devShimmer, devAddToRoster;
        static readonly Color DevCol = new Color(0.5f, 0.28f, 0.5f), DevOn = new Color(0.25f, 0.6f, 0.4f), DevOff = new Color(0.3f, 0.3f, 0.34f);

        public void OpenDev() { if (Dev.On) Open(MenuTab.Dev); }

        void DevButtons(RectTransform list, float w, params (string label, System.Action act)[] btns)
        {
            var row = UIFactory.HRow(list, 76, 10);
            foreach (var b in btns)
            {
                var act = b.act;
                UIFactory.Size(UIFactory.Button(row.transform, b.label, () => { act(); Rebuild(); }, DevCol, 22), 72, w);
            }
        }

        void DevToggle(Transform row, string label, bool on, System.Action flip, float w = 400)
        {
            UIFactory.Size(UIFactory.Button(row, $"{label}\n{(on ? "ON" : "off")}", () => { flip(); Rebuild(); }, on ? DevOn : DevOff, 22), 76, w);
        }

        void BuildDev()
        {
            if (!Dev.On) { Open(MenuTab.Settings); return; }
            SaveScroll();
            var gm = GameManager.I; var s = gm.Player.Stats;
            var list = TrackedList(content);
            Header(list, UIFactory.Col("DEV MODE", new Color(1f, 0.7f, 0.95f)) + "  <size=22>for testing: these change your save like playing would, so use a spare world</size>", 34);
            var tabs = UIFactory.HRow(list, 76, 10);
            string[] names = { "CHEATS", "ITEMS", "KIN", "CAMP", "WORLD" };
            for (int i = 0; i < names.Length; i++)
            {
                int ti = i;
                UIFactory.Size(UIFactory.Button(tabs.transform, names[i], () => { devTab = ti; Rebuild(); }, devTab == i ? new Color(0.25f, 0.55f, 0.45f) : UIFactory.ButtonCol, 26), 72, 300);
            }
            switch (devTab)
            {
                case 1: DevItems(list); break;
                case 2: DevKin(list); break;
                case 3: DevCamp(list); break;
                case 4: DevWorld(list); break;
                default: DevCheats(list, s); break;
            }
            RestoreScroll();
        }

        // ============================================================ CHEATS
        void DevCheats(RectTransform list, PlayerStats s)
        {
            Header(list, "Switches  <size=20>(on until you switch them off or close the game)</size>", 28);
            var r1 = UIFactory.HRow(list, 84, 10);
            DevToggle(r1.transform, "GOD MODE: nothing hurts you", Dev.god, () => Dev.god = !Dev.god);
            DevToggle(r1.transform, "ENDLESS STAMINA", Dev.stamina, () => Dev.stamina = !Dev.stamina);
            DevToggle(r1.transform, "ONE HIT: you and your kin", Dev.oneHit, () => Dev.oneHit = !Dev.oneHit);
            var r2 = UIFactory.HRow(list, 84, 10);
            DevToggle(r2.transform, "SURE CATCH: every seed binds", Dev.sureCatch, () => Dev.sureCatch = !Dev.sureCatch);
            DevToggle(r2.transform, "FAST CAMP: work x10", Dev.fastWork, () => Dev.fastWork = !Dev.fastWork);
            DevToggle(r2.transform, "KIN NEVER SLEEP", Dev.noSleep, () => Dev.noSleep = !Dev.noSleep);
            var r3 = UIFactory.HRow(list, 84, 10);
            DevToggle(r3.transform, "EVERYTHING FREE AND UNLOCKED: no costs, levels, research, Sigils, blueprints or keys", Dev.freeAll, () => Dev.freeAll = !Dev.freeAll, 1220);
            DevButtons(list, 400, ("ALL SWITCHES OFF", Dev.AllOff));

            Header(list, $"Explorer  <size=22>level {s.level}  ·  {s.glimmer} Glimmer  ·  {s.skillPoints} skill points</size>", 28);
            DevButtons(list, 235, ("LEVEL +1", () => Dev.SetLevel(s.level + 1)), ("LEVEL +5", () => Dev.SetLevel(s.level + 5)), ("LEVEL 1", () => Dev.SetLevel(1)), ("LEVEL 25", () => Dev.SetLevel(25)), ("LEVEL 50", () => Dev.SetLevel(50)));
            DevButtons(list, 235, ("FULL HEAL", Dev.Heal), ("HEALTH 40%", () => Dev.Hurt(0.4f)), ("HEALTH 5%", () => Dev.Hurt(0.05f)), ("+1000 GLIMMER", () => Dev.Glimmer(1000)), ("+5 SKILL PTS", () => Dev.SkillPoints(5)));
        }

        // ============================================================ ITEMS
        static readonly string[] DevCats = { "MATERIALS", "FOOD", "TOOLS", "WEAPONS", "SEEDS", "GEAR", "OTHER" };

        static int DevCatOf(ItemDef d)
        {
            if (BaseManager.IsFoodItem(d)) return 1;
            switch (d.category)
            {
                case ItemCategory.Resource: return 0;
                case ItemCategory.Tool: return 2;
                case ItemCategory.Weapon: return 3;
                case ItemCategory.CaptureSeed: return 4;
                case ItemCategory.Gear: return 5;
                default: return 6;
            }
        }

        void DevItems(RectTransform list)
        {
            Header(list, "Packs  <size=20>(into your bag; what does not fit goes to camp storage)</size>", 28);
            DevButtons(list, 400, ("ALL MATERIALS x99", () => Dev.GivePack("materials")), ("ALL FOOD x20", () => Dev.GivePack("food")), ("ALL SEEDS x30", () => Dev.GivePack("seeds")));
            DevButtons(list, 400, ("EVERY TOOL AND WEAPON + AMMO", () => Dev.GivePack("tools")), ("EVERY PIECE OF GEAR", () => Dev.GivePack("gear")), ("THE 24 BLUEPRINT ITEMS", () => Dev.GivePack("blueprints")));
            DevButtons(list, 400, ("5 CHEST KEYS", () => Dev.Give(Chests.KeyItem, 5)), ("10 KIN TREATS", () => Dev.Give("kin_treat", 10)), ("5 WARDEN HEARTS", () => Dev.Give(Wardens.HeartItem, 5)));

            Header(list, "Any item", 28);
            var cats = UIFactory.HRow(list, 68, 8);
            for (int i = 0; i < DevCats.Length; i++)
            {
                int ci = i;
                UIFactory.Size(UIFactory.Button(cats.transform, DevCats[i], () => { devItemCat = ci; Rebuild(); }, devItemCat == i ? new Color(0.25f, 0.55f, 0.45f) : UIFactory.ButtonCol, 20), 64, 200);
            }
            var items = new List<ItemDef>();
            foreach (var d in ContentDB.Items.Values) if (DevCatOf(d) == devItemCat) items.Add(d);
            items.Sort((a, b) => string.CompareOrdinal(a.displayName, b.displayName));
            foreach (var d in items)
            {
                string id = d.id;
                var row = UIFactory.HRow(list, 70, 8);
                UIFactory.Size(UIFactory.ItemIcon(row.transform, d, 62), 62, 62);
                var nm = UIFactory.Label(row.transform, $"{d.displayName}  <size=19>{UIFactory.Col(ItemStatLine(d), UIFactory.TextDim)}</size>", 23, Color.white);
                nm.supportRichText = true; UIFactory.Size(nm, 62, 900);
                UIFactory.Size(UIFactory.Button(row.transform, "+1", () => Dev.Give(id, 1), UIFactory.ButtonCol, 22), 62, 90);
                UIFactory.Size(UIFactory.Button(row.transform, "+10", () => Dev.Give(id, 10), UIFactory.ButtonCol, 22), 62, 100);
                UIFactory.Size(UIFactory.Button(row.transform, "+99", () => Dev.Give(id, 99), UIFactory.ButtonCol, 22), 62, 100);
            }
        }

        // ============================================================ KIN
        void DevKin(RectTransform list)
        {
            var gm = GameManager.I;
            var comp = gm.Party.Companion;
            Header(list, comp != null ? $"The kin that is out  <size=22>{comp.Data.DisplayName} Lv {comp.Data.level}, bond {comp.Data.BondLevel}</size>" : "The kin that is out  <size=22>none</size>", 28);
            DevButtons(list, 300, ("+1 LEVEL", () => Dev.LevelCompanion(1)), ("+5 LEVELS", () => Dev.LevelCompanion(5)), ("+20 LEVELS", () => Dev.LevelCompanion(20)), ("MAX BOND", Dev.MaxBond));
            DevButtons(list, 400, ("EVOLVE IT NOW (FREE)", () => { if (comp != null) EvolveWithShow(comp.Data, true); else GameEvents.Say("No kin is out."); }));

            Header(list, "Wild kin within 25 m", 28);
            DevButtons(list, 235, ("HEALTH 8%", () => Dev.NearbyWild(0.08f)), ("HEALTH 50%", () => Dev.NearbyWild(0.5f)), ("DEFEAT", () => Dev.NearbyWild(0f)), ("DROWSY", () => Dev.NearbyWild(1f, StatusType.Drowsy)), ("BURN", () => Dev.NearbyWild(1f, StatusType.Burn)));

            Header(list, "Make a kin", 28);
            var opt = UIFactory.HRow(list, 84, 10);
            UIFactory.Size(UIFactory.Button(opt.transform, "-5", () => { devKinLevel = Mathf.Max(1, devKinLevel - 5); Rebuild(); }, UIFactory.ButtonCol, 26), 76, 90);
            UIFactory.Size(UIFactory.Label(opt.transform, $"Level {devKinLevel}", 28, Color.white, TextAnchor.MiddleCenter), 76, 170);
            UIFactory.Size(UIFactory.Button(opt.transform, "+5", () => { devKinLevel = Mathf.Min(60, devKinLevel + 5); Rebuild(); }, UIFactory.ButtonCol, 26), 76, 90);
            DevToggle(opt.transform, "ALPHA", devAlpha, () => devAlpha = !devAlpha, 170);
            DevToggle(opt.transform, "SHIMMER", devShimmer, () => devShimmer = !devShimmer, 170);
            // one button, two meanings: what tapping a kin below does
            UIFactory.Size(UIFactory.Button(opt.transform, devAddToRoster ? "TAPPING A KIN: IT JOINS YOU" : "TAPPING A KIN: A WILD ONE APPEARS", () => { devAddToRoster = !devAddToRoster; Rebuild(); },
                devAddToRoster ? new Color(0.25f, 0.45f, 0.6f) : new Color(0.55f, 0.35f, 0.25f), 22), 76, 430);

            var all = new List<SpeciesDef>(ContentDB.SpeciesList);
            all.Sort((a, b) => a.dexNumber.CompareTo(b.dexNumber));
            HorizontalLayoutGroup row = null;
            int n = 0;
            foreach (var sp in all)
            {
                if (n % 4 == 0) row = UIFactory.HRow(list, 62, 8);
                n++;
                var spd = sp;
                UIFactory.Size(UIFactory.Button(row.transform, $"#{sp.dexNumber:000} {sp.displayName}", () =>
                {
                    if (devAddToRoster) Dev.AddKin(spd, devKinLevel, devShimmer);
                    else { Dev.SpawnWild(spd, devKinLevel, devAlpha, devShimmer); Close(); }
                }, sp.rarity == Rarity.Legendary ? new Color(0.55f, 0.45f, 0.2f) : sp.rarity == Rarity.Epic ? new Color(0.42f, 0.3f, 0.55f) : UIFactory.ButtonCol, 20), 58, 305);
            }
        }

        // ============================================================ CAMP
        void DevCamp(RectTransform list)
        {
            var bm = BaseManager.I;
            if (bm == null || !bm.HasBase) { Para(list, "No camp yet: place a Hearthstone (the EVERYTHING FREE switch on CHEATS lets you build anything at once).", 24, Color.white); }
            else Para(list, $"Hearthstone Lv {bm.CoreLevel}  ·  charge {WorkStages.Clock(bm.Charge)} of {WorkStages.Clock(bm.ChargeCapacity)}  ·  cold {WorkStages.Clock(bm.Cold)} of {WorkStages.Clock(bm.ColdCapacity)}  ·  {bm.Orders.Count} orders", 22, Color.white);
            Header(list, "Now", 28);
            DevButtons(list, 400, ("FINISH THE CURRENT STEP EVERYWHERE", Dev.FinishSteps), ("CRAFT EVERYTHING QUEUED", Dev.FinishOrders), ("HEARTHSTONE +1 LEVEL", Dev.UpgradeHearth));
            DevButtons(list, 400, ("CHARGE AND COLD: FULL", () => Dev.Power(true)), ("CHARGE AND COLD: EMPTY", () => Dev.Power(false)), ("A DAY OF FOOD SPOILING", Dev.SpoilDay));
            DevButtons(list, 400, ("START A RAID", Dev.Raid), ("SORT ALL STORAGE", () => { if (bm != null && bm.HasBase) bm.SortStorage(); }));
            Header(list, "Eggs", 28);
            DevButtons(list, 300, ("EGG, READY NOW", () => Dev.Egg(false)), ("SHIMMER EGG, READY NOW", () => Dev.Egg(true)), ("ALL EGGS READY", Dev.EggsReady), ("HATCH ONE", OpenHatchery));
            Header(list, "Unlock", 28);
            DevButtons(list, 400, ("ALL RESEARCH DONE", Dev.AllResearch), ("ALL BLUEPRINTS LEARNED", Dev.AllBlueprints), ("ALL 8 SIGILS", Dev.AllSigils));
            Header(list, "Time", 28);
            DevButtons(list, 300, ("DAWN", () => Dev.SetTime(0.24f, "dawn")), ("NOON", () => Dev.SetTime(0.5f, "noon")), ("EVENING", () => Dev.SetTime(0.66f, "evening")), ("NIGHT", () => Dev.SetTime(0.8f, "night")));
        }

        // ============================================================ WORLD
        void DevWorld(RectTransform list)
        {
            Header(list, "Time and weather", 28);
            DevButtons(list, 300, ("DAWN", () => Dev.SetTime(0.24f, "dawn")), ("NOON", () => Dev.SetTime(0.5f, "noon")), ("EVENING", () => Dev.SetTime(0.66f, "evening")), ("NIGHT", () => Dev.SetTime(0.8f, "night")));
            var wr = UIFactory.HRow(list, 76, 8);
            foreach (WeatherType w in System.Enum.GetValues(typeof(WeatherType)))
            {
                var ww = w;
                UIFactory.Size(UIFactory.Button(wr.transform, w.ToString().ToUpperInvariant(), () => { Dev.Weather(ww); Rebuild(); }, DevCol, 20), 72, 172);
            }
            Header(list, "Events and chests", 28);
            DevButtons(list, 300, ("METEOR SHOWER", () => Dev.Event(WorldEventType.MeteorShower)), ("HERD MIGRATION", () => Dev.Event(WorldEventType.HerdMigration)), ("STORM ALPHA", () => Dev.Event(WorldEventType.StormAlpha)), ("SHIMMER NIGHT", () => Dev.Event(WorldEventType.ShimmerNight)));
            var cr = UIFactory.HRow(list, 84, 10);
            UIFactory.Size(UIFactory.Button(cr.transform, "WOODEN CHEST", () => { Dev.Chest(ChestTier.Wooden); Close(); }, DevCol, 22), 76, 300);
            UIFactory.Size(UIFactory.Button(cr.transform, "IRON CHEST", () => { Dev.Chest(ChestTier.Iron); Close(); }, DevCol, 22), 76, 300);
            UIFactory.Size(UIFactory.Button(cr.transform, "GILDED CHEST", () => { Dev.Chest(ChestTier.Gilded); Close(); }, DevCol, 22), 76, 300);
            DevToggle(cr.transform, "NEXT CHEST HOLDS A BLUEPRINT", Dev.forceBlueprint, () => Dev.forceBlueprint = !Dev.forceBlueprint, 320);

            Header(list, "Travel", 28);
            DevButtons(list, 400, ("EVERY WAYSTONE ATTUNED", Dev.AllWaystones));
            var places = new List<(string, Vector3)>();
            places.Add(("The start", WorldGen.StartExit));
            var bm = BaseManager.I;
            if (bm != null && bm.HasBase) places.Add(("Your camp", bm.CorePosition + new Vector3(0, 0, -6f)));
            foreach (var st in Settlements.All) places.Add((st.name, st.pos + new Vector3(0, 0, -14f)));
            foreach (var l in WorldGen.Lands) places.Add((BiomeInfo.Name(l.biome), new Vector3(l.center.x, 0, l.center.y)));
            foreach (var w in Wardens.ByLevel()) places.Add(($"Warden: {w.place} (Lv {w.level})", w.shrine + new Vector3(0, 0, -6f)));
            HorizontalLayoutGroup row = null;
            int n = 0;
            foreach (var pl in places)
            {
                if (n % 3 == 0) row = UIFactory.HRow(list, 70, 8);
                n++;
                var to = pl.Item2; string name = pl.Item1;
                UIFactory.Size(UIFactory.Button(row.transform, name, () => { Dev.Teleport(to, name); Close(); }, UIFactory.ButtonCol, 20), 66, 410);
            }
        }
    }
}
