using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>
    /// One screen per crafting station (Campfire = cooking, Workbench = crafting, ...): only that station's recipes,
    /// orders and workers. Driven by <see cref="CraftStation"/>, so a new station gets its own menu with no UI code.
    /// </summary>
    public partial class MenuUI
    {
        CraftStation stationSel = CraftStation.Workbench;

        public void OpenStation(CraftStation st)
        {
            if (st == CraftStation.Hand) { Open(MenuTab.Craft); return; }
            if (st != stationSel) savedScroll = 1f;
            stationSel = st;
            Open(MenuTab.Station);
        }

        public static string StationName(CraftStation st)
        {
            foreach (var b in ContentDB.BuildingList) if (b.station == st && st != CraftStation.Hand) return b.displayName;
            return st.ToString();
        }

        /// <summary>What you do there: shown in the title and on the recipe buttons.</summary>
        public static string StationVerb(CraftStation st) =>
            st == CraftStation.Campfire ? "COOK" : st == CraftStation.AlloyForge ? "SMELT" : st == CraftStation.Research ? "RESEARCH" : "CRAFT";
        public static string StationActivity(CraftStation st) =>
            st == CraftStation.Campfire ? "Cooking" : st == CraftStation.AlloyForge ? "Smelting" : st == CraftStation.Research ? "Research" : "Crafting";

        /// <summary>Stations that have at least one recipe (Hand excluded).</summary>
        public static List<CraftStation> RecipeStations()
        {
            var list = new List<CraftStation>();
            foreach (CraftStation st in System.Enum.GetValues(typeof(CraftStation)))
                if (st != CraftStation.Hand && ContentDB.RecipeList.Exists(r => r.station == st)) list.Add(st);
            return list;
        }

        void BuildStation()
        {
            if (stationSel == CraftStation.Research) { BuildResearch(); return; }
            SaveScroll();
            var bm = BaseManager.I;
            var st = stationSel;
            string name = StationName(st);
            bool built = bm.HasStation(st);
            bool inCamp = bm.AtStation(st);   // you work a station standing at it
            LeftRight(out var left, out var right, 0.63f);

            // ---------------- recipes ----------------
            var list = TrackedList(left);
            Header(list, $"{name}  <size=26>{UIFactory.Col(StationActivity(st), UIFactory.Accent)}</size>", 36);
            Para(list, !built ? $"You haven't built a {name} yet (Build tab). These are the recipes it unlocks."
                     : !inCamp ? $"This is a view. Walk up to your {name} ({bm.WayTo(bm.NearestStation(st))}) to {StationVerb(st).ToLower()}, queue orders or keep things in stock."
                     : "Ingredients come from your bag and camp storage. Finished items go to camp storage. KEEP makes the camp restock that item by itself, from camp storage.", 21);
            RecipeRows(list, st, 520);
            RestoreScroll();

            // ---------------- orders + workers ----------------
            var rl = UIFactory.ScrollList(right, out _, 10f);
            ((RectTransform)rl.parent).Stretch();
            bool anyKept = false;
            foreach (var so in bm.Standing.ToArray())
            {
                if (!ContentDB.Recipes.TryGetValue(so.recipeId, out var sr) || sr.station != st) continue;
                if (!anyKept) { anyKept = true; Header(rl, "Kept in stock", 30); }
                var skeep = so;
                var sdef = ContentDB.Item(sr.outputItem);
                int have = bm.Storage.Count(sr.outputItem);
                string why = bm.StandingBlock(sr);
                var srow = UIFactory.HRow(rl, 76, 10);
                var sic = UIFactory.ItemIcon(srow.transform, sdef, 60); UIFactory.Size(sic, 60, 60);
                var stx = UIFactory.Label(srow.transform, $"<b>{sdef.displayName}</b>\n<size=19>{UIFactory.Col($"{have} of {so.keep}", have >= so.keep ? Good : Color.white)}{(why != null ? UIFactory.Col("  ·  " + why, Idle) : "")}</size>", 22, Color.white);
                stx.supportRichText = true; UIFactory.Size(stx, 70, 350);
                if (inCamp) UIFactory.Size(UIFactory.Button(srow.transform, "OFF", () => { bm.SetKeep(skeep.recipeId, 0); Rebuild(); }, new Color(0.45f, 0.25f, 0.25f), 20), 60, 80);
            }
            Header(rl, "In progress", 30);
            bool any = false;
            foreach (var o in bm.Orders.ToArray())
            {
                if (!ContentDB.Recipes.TryGetValue(o.recipeId, out var r) || r.station != st) continue;
                any = true;
                var oo = o;
                var outDef = ContentDB.Item(r.outputItem);
                var row = UIFactory.HRow(rl, 76, 10);
                var ic = UIFactory.ItemIcon(row.transform, outDef, 60); UIFactory.Size(ic, 60, 60);
                var t = UIFactory.Label(row.transform, $"<b>{outDef.displayName}</b>\n<size=19>{o.remaining} left{(o.auto ? "  ·  restocking" : "")}</size>", 22, Color.white);
                t.supportRichText = true; UIFactory.Size(t, 70, 190);
                var bar = UIFactory.Bar(row.transform, UIFactory.Accent, out var fill);
                UIFactory.Size(bar, 24, 150);
                fill.SetFill(bm.OrderProgress01(o));
                if (inCamp) UIFactory.Size(UIFactory.Button(row.transform, "X", () => CancelOrder(oo), new Color(0.45f, 0.25f, 0.25f), 22), 60, 64);
            }
            if (!any) Para(rl, $"Nothing queued. Pick a recipe on the left to {StationVerb(st).ToLower()} it, or tap KEEP to have the camp keep it in stock.", 21);

            Header(rl, "Workers", 30);
            WorkType job = WorkType.None;
            var crew = new List<CreatureData>();
            foreach (var b in bm.Buildings)
            {
                if (b.Def.station != st) continue;
                job = b.Def.workType;
                foreach (var d in GameManager.I.Party.In(KinLocation.Base)) if (d.assignedBuilding == b.Uid) crew.Add(d);
            }
            if (!built) Para(rl, $"Build a {name} first.", 21);
            else if (crew.Count == 0)
                Para(rl, job != WorkType.None && bm.AnyoneSuits(bm.Buildings.Find(x => x.Def.station == st))
                    ? $"No kin assigned here. Any free {job} kin comes over when there is an order; assign one in Camp > Workers to make this its first choice."
                    : $"No {job} kin in camp. Orders only move while you stand at the {name}, or with a powered Auto-Assembler.", 21, new Color(1f, 0.8f, 0.5f));
            else
                foreach (var d in crew)
                    Para(rl, $"<b>{d.DisplayName}</b> Lv{d.level}  <size=19>{job} Lv {d.WorkLevel(job)}{(d.hunger <= 0f ? UIFactory.Col("  hungry", new Color(1f, 0.55f, 0.5f)) : "")}</size>", 23, Color.white);
        }

        const int EffectFont = 19;

        static int PlainLength(string rich) => System.Text.RegularExpressions.Regex.Replace(rich ?? "", "<.*?>", "").Length;

        /// <summary>The numbers behind an item in one line (food, heal, damage, tool tier, seed rating, gear stats). Empty if it has none.</summary>
        public static string ItemStatLine(ItemDef d)
        {
            if (d == null) return "";
            var parts = new List<string>();
            if (d.foodValue > 0) { parts.Add($"Food +{d.foodValue:0}"); parts.Add(BaseManager.KeepText(d)); }
            if (d.healValue > 0) parts.Add($"Heal +{d.healValue:0}");
            if (d.kinHealValue > 0) parts.Add(d.kinHealValue >= 999f ? "Full heal for party kin" : $"Kin heal +{d.kinHealValue:0}");
            if (d.mealMods != null && d.mealMods.Length > 0)
            {
                var buffs = new List<string>();
                foreach (var m in d.mealMods) buffs.Add(GearInfo.Format(m));
                parts.Add($"{string.Join(", ", buffs)} for {d.mealMinutes:0} min");
            }
            if (d.weaponDamage > 0)
            {
                parts.Add($"Attack power {d.weaponDamage:0}");
                if (d.hitElement != Element.Normal || d.hitStatus != StatusType.None)
                    parts.Add((d.hitElement != Element.Normal ? $"{d.hitElement} damage" : "") + (d.hitElement != Element.Normal && d.hitStatus != StatusType.None ? ", " : "")
                            + (d.hitStatus != StatusType.None ? $"{d.hitStatusChance * 100f:0}% {d.hitStatus}" : ""));
                string how = HandStyles.Describe(HandStyles.Of(d));
                if (how != null) parts.Add(how);
            }
            if (d.toolPower > 0) parts.Add($"{d.toolKind} tier {d.toolPower}");
            if (d.bonusInventorySlots > 0) parts.Add($"+{d.bonusInventorySlots} bag slots");
            if (d.category == ItemCategory.CaptureSeed)
            {
                int cap = CaptureSystem.SeedLevelCap(d);
                if (d.id == Wardens.KrakenSeed)
                {
                    // a boss's seed follows its own rule (CaptureSystem.Chance): nothing until it is nearly beaten
                    parts.Add("Binds only the Kraken");
                    parts.Add($"no hold above {CaptureSystem.BossSeedHp * 100f:0}% health, then {CaptureSystem.BossSeedMin * 100f:0}-{CaptureSystem.BossSeedMax * 100f:0}%");
                }
                else
                {
                    parts.Add(cap >= 99 ? "Any level kin" : $"Kin up to Lv {cap}");
                    if (CaptureSystem.SeedBonus(d) > 1.001f) parts.Add($"catch x{CaptureSystem.SeedBonus(d):0.00}");
                    if (CaptureSystem.IsLegendSeed(d)) parts.Add("certain catch at 10% health or less");
                }
                if (d.shimmerSeed) parts.Add("the catch is a shimmer");
            }
            if (PlayerStats.IsGear(d))
            {
                parts.Add($"{d.rarity} {d.equipSlot}");
                if (d.mods != null) foreach (var m in d.mods) parts.Add(GearInfo.Format(m));
                if (d.unique != UniqueEffect.None) parts.Add($"{GearInfo.EffectName(d.unique)}: {GearInfo.EffectDesc(d.unique)}");
            }
            return string.Join(" · ", parts);
        }

        /// <summary>Recipe list for one station (or Hand). Shared by the Craft tab and the station screens.</summary>
        void RecipeRows(RectTransform list, CraftStation st, float textWidth)
        {
            var bm = BaseManager.I;
            var stats = GameManager.I.Player.Stats;
            bool inCamp = bm.AtStation(st);   // standing at a station of this kind (always true for hand recipes)
            // tools & supplies first, then armor, then accessories (each by unlock level)
            var recs = ContentDB.RecipeList.FindAll(x => x.station == st);
            recs.Sort((x, y) => CraftGroup(x) != CraftGroup(y) ? CraftGroup(x).CompareTo(CraftGroup(y)) : (CraftGroup(x) == 0 ? 0 : x.unlockLevel.CompareTo(y.unlockLevel)));
            // long lists are split into category tabs (MenuUI.Categories)
            int tab = RecipeTabs(list, st, recs, inCamp);
            if (tab == ReadyTab) recs = recs.FindAll(x => RecipeBlock(x, inCamp) == null);
            else if (tab != AllTab)
            {
                recs = recs.FindAll(x => (int)CatOf(x) == tab);
                recs.Sort((x, y) => x.unlockLevel.CompareTo(y.unlockLevel));
            }
            if (recs.Count == 0) Para(list, "Nothing you can make right now. The other tabs show what each recipe needs.", 22);
            int lastGroup = 0;
            foreach (var r in recs)
            {
                int grp = CraftGroup(r);
                if (tab == AllTab && grp != lastGroup) { lastGroup = grp; Header(list, grp == 1 ? "Armor" : "Accessories", 26); }
                var outDef = ContentDB.Item(r.outputItem);
                // what it does: stat line + description, wrapped under the name (row grows to fit)
                string stat = ItemStatLine(outDef), desc = outDef.description ?? "";
                var tech = Tech.RecipeUnlocked(r.id) ? null : Tech.ForRecipe(r.id);
                if (tech != null) desc += $"  Locked: research {tech.name} at a Research Bench.";
                bool noPlan = Chests.Locked(r);
                string plan = Chests.BlueprintNote(r);
                if (plan != null) desc += "  " + (noPlan ? plan : "Made from a blueprint you found.");
                int plain = PlainLength(stat) + desc.Length + (stat.Length > 0 && desc.Length > 0 ? 3 : 0);
                int perLine = Mathf.Max(20, Mathf.FloorToInt(textWidth / (EffectFont * 0.55f)));
                int lines = plain > 0 ? Mathf.CeilToInt(plain / (float)perLine) : 0;
                float rowH = 96 + lines * (EffectFont + 5);
                string effect = lines == 0 ? "" : $"\n<size={EffectFont}>{(stat.Length > 0 ? UIFactory.Col(stat, UIFactory.Accent) + (desc.Length > 0 ? "   " : "") : "")}{UIFactory.Col(desc, UIFactory.TextDim)}</size>";
                var row = UIFactory.HRow(list, rowH, 14);
                row.childForceExpandHeight = false;   // icon and button keep their size while the text grows
                var ic = UIFactory.ItemIcon(row.transform, outDef, 80);
                UIFactory.Size(ic, 80, 80);
                var tx = UIFactory.Label(row.transform, $"<b>{outDef.displayName} x{r.outputCount}</b>{effect}\n<size=21>{Ingredients(r.inputs)}</size>", 26, Color.white);
                tx.supportRichText = true;
                UIFactory.Size(tx, rowH - 6, textWidth);
                string reason = null;
                if (tech != null) reason = "Research";
                else if (noPlan) reason = "Blueprint";
                else if (r.unlockLevel > stats.level && !Dev.FreeAll) reason = $"Lv {r.unlockLevel}";
                else if (st != CraftStation.Hand && !bm.HasStation(st)) reason = "Not built";
                else if (st != CraftStation.Hand && !inCamp) reason = "Walk to it";
                else if (!ItemSources.Has(r.inputs)) reason = "Missing";
                var rec = r;
                var btn = UIFactory.Button(row.transform, reason ?? StationVerb(st),
                    reason == null ? () => Craft(rec) : (System.Action)null, reason == null ? new Color(0.25f, 0.55f, 0.4f) : new Color(0.3f, 0.3f, 0.32f), 26);
                UIFactory.Size(btn, 84, 200);
                if (st != CraftStation.Hand && inCamp && tech == null && !noPlan && r.unlockLevel <= stats.level && bm.HasStation(st))
                {
                    // standing order: tap to step through 5 / 10 / 20 / 50 / off (BaseManager.Standing)
                    int keep = bm.KeepOf(r.id);
                    var kb = UIFactory.Button(row.transform, keep > 0 ? $"KEEP\n{keep}" : "KEEP\nin stock", () => { bm.SetKeep(rec.id, bm.NextKeep(rec.id)); SynthAudio.Play(Sfx.UiClick); Rebuild(); },
                        keep > 0 ? new Color(0.5f, 0.42f, 0.2f) : UIFactory.ButtonCol, 20);
                    UIFactory.Size(kb, 84, 130);
                }
            }
        }
    }
}
