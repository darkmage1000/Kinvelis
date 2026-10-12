using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>
    /// Category tabs for the long lists (user request 2026-10-09: no single scrolling list for crafting or building).
    /// A strip of tabs sits above the list and stays put while the list scrolls. Each tab shows how many entries it
    /// holds; the first, CAN MAKE / CAN BUILD, is everything you could make right now.
    /// Recipes are sorted by what they make (CatOf, from the item itself) and buildings by BuildingDef.category
    /// (Auto = worked out from the def), so new content lands in a tab without touching this file.
    /// Lists shorter than TabsFrom stay as one list.
    /// </summary>
    public partial class MenuUI
    {
        public enum RecipeCat { Tools, Weapons, Seeds, Armor, Accessories, Kit, Meals, Food, Materials, Count }
        static readonly string[] RecipeCatNames = { "TOOLS", "WEAPONS", "SEEDS", "ARMOR", "ACCESSORIES", "KIT", "BUFF MEALS", "FOOD", "MATERIALS" };
        static readonly string[] BuildCatNames = { "", "CRAFTING", "PRODUCTION", "STORAGE", "KIN CARE", "UTILITY" };
        const int ReadyTab = -1, AllTab = -2;
        const int TabsFrom = 10;
        const float TabStripHeight = 76f;

        readonly Dictionary<CraftStation, int> recipeTab = new Dictionary<CraftStation, int>();
        int buildTab = AllTab;

        public static RecipeCat CatOf(RecipeDef r)
        {
            var d = ContentDB.Item(r.outputItem);
            if (HandStyles.IsAmmoItem(d.id)) return RecipeCat.Weapons;   // arrows and bolts sit next to the bows
            switch (d.category)
            {
                case ItemCategory.Tool: return RecipeCat.Tools;
                case ItemCategory.Weapon: return RecipeCat.Weapons;
                case ItemCategory.CaptureSeed: return RecipeCat.Seeds;
                case ItemCategory.Gear: return GearSlots.IsArmor(d.equipSlot) ? RecipeCat.Armor : RecipeCat.Accessories;
                case ItemCategory.Food: return d.mealMods != null && d.mealMods.Length > 0 ? RecipeCat.Meals : RecipeCat.Food;
                case ItemCategory.Resource: return RecipeCat.Materials;
                default: return RecipeCat.Kit;
            }
        }

        public static BuildCategory BuildCatOf(BuildingDef d)
        {
            if (d.category != BuildCategory.Auto) return d.category;
            if (d.station != CraftStation.Hand) return BuildCategory.Crafting;
            if (!string.IsNullOrEmpty(d.outputItem)) return BuildCategory.Production;
            if (d.storageSlots > 0 || d.workType == WorkType.Chilling) return BuildCategory.Storage;
            if (d.beds > 0 || d.feeds) return BuildCategory.KinCare;
            return BuildCategory.Utility;
        }

        /// <summary>Why this recipe can't be made this moment (null = it can).</summary>
        string RecipeBlock(RecipeDef r, bool inCamp)
        {
            var bm = BaseManager.I;
            var st = r.station;
            if (!Tech.RecipeUnlocked(r.id)) return "Research";
            if (r.unlockLevel > GameManager.I.Player.Stats.level) return $"Lv {r.unlockLevel}";
            if (st != CraftStation.Hand && !bm.HasStation(st)) return "Not built";
            if (st != CraftStation.Hand && !inCamp) return "Go to camp";
            if (!ItemSources.Has(r.inputs)) return "Missing";
            return null;
        }

        /// <summary>Why this can't be built this moment (null = it can).</summary>
        static string BuildBlock(BuildingDef d, int level)
        {
            if (!Tech.BuildingUnlocked(d.id)) return "Research";
            if (WorldRules.Current.freeBuild) return null;   // free-building worlds: no level, no cost
            if (BaseManager.I != null ? !BaseManager.I.LevelOk(d) : d.unlockLevel > level) return $"Lv {d.unlockLevel}";
            return ItemSources.Has(d.cost) ? null : "Missing";
        }

        /// <summary>A row of tabs fixed above a scroll list (the list is pushed down to make room for it).</summary>
        void TabStrip(RectTransform list, List<string> labels, List<int> counts, int selected, Action<int> pick)
        {
            var view = (RectTransform)list.parent;
            view.offsetMax = new Vector2(view.offsetMax.x, view.offsetMax.y - TabStripHeight - 8f);
            var bar = UIFactory.Rect(view.parent, "TabStrip");
            bar.anchorMin = new Vector2(0, 1); bar.anchorMax = new Vector2(1, 1); bar.pivot = new Vector2(0.5f, 1);
            bar.offsetMin = new Vector2(view.offsetMin.x, -TabStripHeight); bar.offsetMax = new Vector2(view.offsetMax.x, 0);
            var row = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 6;
            row.childControlHeight = true; row.childControlWidth = true;
            row.childForceExpandHeight = true; row.childForceExpandWidth = true;
            for (int i = 0; i < labels.Count; i++)
            {
                int index = i;
                bool on = i == selected;
                Color c = on ? new Color(0.25f, 0.55f, 0.45f, 1f) : counts[i] == 0 ? new Color(0.2f, 0.22f, 0.26f, 0.9f) : new Color(0.26f, 0.32f, 0.42f, 0.95f);
                var b = UIFactory.Button(bar, $"{labels[i]}\n{counts[i]}", on ? (Action)null : () => pick(index), c, 22);
                if (counts[i] == 0 && !on) b.GetComponentInChildren<Text>().color = new Color(1f, 1f, 1f, 0.45f);
            }
        }

        /// <summary>Builds the tab strip for a station's recipes when the list is long. Returns the tab to list (AllTab = no tabs).</summary>
        int RecipeTabs(RectTransform list, CraftStation st, List<RecipeDef> recs, bool inCamp)
        {
            if (recs.Count < TabsFrom) return AllTab;
            var counts = new int[(int)RecipeCat.Count];
            int ready = 0;
            foreach (var r in recs)
            {
                counts[(int)CatOf(r)]++;
                if (RecipeBlock(r, inCamp) == null) ready++;
            }
            var present = new List<int>();
            for (int i = 0; i < counts.Length; i++) if (counts[i] > 0) present.Add(i);
            if (present.Count < 2) return AllTab;
            if (!recipeTab.TryGetValue(st, out int sel) || (sel != ReadyTab && (sel < 0 || sel >= counts.Length || counts[sel] == 0))) sel = present[0];
            recipeTab[st] = sel;
            var labels = new List<string> { st == CraftStation.Campfire ? "CAN COOK" : "CAN MAKE" };
            var nums = new List<int> { ready };
            int shown = 0;
            for (int i = 0; i < present.Count; i++)
            {
                labels.Add(RecipeCatNames[present[i]]);
                nums.Add(counts[present[i]]);
                if (present[i] == sel) shown = i + 1;
            }
            TabStrip(list, labels, nums, shown, i =>
            {
                recipeTab[st] = i == 0 ? ReadyTab : present[i - 1];
                mainScroll = null;   // a new tab starts at the top
                Rebuild();
            });
            return sel;
        }

        /// <summary>Tab strip for the Build menu. Returns the category to list (AllTab = everything).</summary>
        int BuildTabs(RectTransform list, List<BuildingDef> defs, int level)
        {
            if (defs.Count < TabsFrom) return AllTab;
            var counts = new int[BuildCatNames.Length];
            int ready = 0;
            foreach (var d in defs)
            {
                counts[(int)BuildCatOf(d)]++;
                if (BuildBlock(d, level) == null) ready++;
            }
            var present = new List<int>();
            for (int i = 1; i < counts.Length; i++) if (counts[i] > 0) present.Add(i);
            if (present.Count < 2) return AllTab;
            if (buildTab != ReadyTab && (buildTab < 1 || buildTab >= counts.Length || counts[buildTab] == 0)) buildTab = present[0];
            var labels = new List<string> { "CAN BUILD" };
            var nums = new List<int> { ready };
            int shown = 0;
            for (int i = 0; i < present.Count; i++)
            {
                labels.Add(BuildCatNames[present[i]]);
                nums.Add(counts[present[i]]);
                if (present[i] == buildTab) shown = i + 1;
            }
            TabStrip(list, labels, nums, shown, i =>
            {
                buildTab = i == 0 ? ReadyTab : present[i - 1];
                mainScroll = null;
                Rebuild();
            });
            return buildTab;
        }
    }
}
