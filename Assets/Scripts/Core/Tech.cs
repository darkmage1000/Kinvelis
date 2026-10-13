using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    public class TechDef
    {
        public string id, name, desc;
        /// <summary>Warden id whose Sigil this needs (null = none).</summary>
        public string sigil;
        public string prereq;
        public Ingredient[] cost;
        public float work;
        /// <summary>Recipe ids (= output item ids) and building ids this unlocks.</summary>
        public string[] recipes = new string[0], buildings = new string[0];
        /// <summary>0 = camp know-how, 1 = metals of the outer lands.</summary>
        public int group;
        /// <summary>Explorer (player) level needed to start it: about where a player is when they reach the land it belongs to.</summary>
        public int level = 1;
    }

    /// <summary>
    /// Research: blueprints studied at a Research Bench. Each one costs materials up front, then takes work (a Crafting
    /// kin on the bench, or you standing at it). Most need a land's Sigil - beating that land's Warden is what opens its
    /// metal and its machines. Recipes and buildings named by a tech stay locked until it is done; everything else in
    /// the game is unaffected. State lives in PlayerStats (techDone, researchId, researchProgress).
    /// </summary>
    public static class Tech
    {
        const string Heart = Wardens.HeartItem;

        static TechDef T(string id, string name, int group, int level, string sigil, string prereq, float work, string desc, Ingredient[] cost, string[] recipes, string[] buildings = null)
        {
            return new TechDef { id = id, name = name, group = group, level = level, sigil = sigil, prereq = prereq, work = work, desc = desc, cost = cost, recipes = recipes ?? new string[0], buildings = buildings ?? new string[0] };
        }

        static Ingredient[] C(params (string, int)[] c)
        {
            var a = new Ingredient[c.Length];
            for (int i = 0; i < c.Length; i++) a[i] = new Ingredient(c[i].Item1, c[i].Item2);
            return a;
        }

        static string[] R(params string[] s) => s;

        public static readonly TechDef[] All =
        {
            // ---------------- camp know-how ----------------
            T("forge", "Alloy Forging", 0, 12, null, null, 60f, "A hotter furnace for metals the Smelter can't melt. Every outer-land metal starts here.",
                C(("ingot", 8), ("stone", 30), ("cinder", 4)), null, R("alloy_forge")),
            T("chests", "Joinery", 0, 4, null, null, 30f, "Banded trunks: a Large Chest holds twice what a Storage Chest does.",
                C(("wood", 30), ("fiber", 15)), null, R("large_chest")),
            T("storage", "Bulk Storage", 0, 7, null, null, 50f, "A tall silo that holds as much as three chests.",
                C(("wood", 40), ("stone", 20), ("ingot", 4)), null, R("bulk_silo")),
            T("vaults", "Ironbound Vaults", 0, 10, null, "chests", 70f, "An iron-doored vault that holds as much as five chests.",
                C(("ingot", 10), ("stone", 30), ("wood", 20)), null, R("iron_vault")),
            T("grandvault", "Grand Vault", 0, 21, "badlands", "vaults", 120f, "A stone treasury: nine chests' worth under one roof.",
                C(("ingot", 20), ("crystal", 8), ("stone", 40), (Heart, 1)), null, R("grand_vault")),
            T("medicine", "Kin Medicine", 0, 12, "meadow", null, 80f, "A clinic where fainted kin are back on their feet three times sooner.",
                C(("berry", 20), ("gel", 6), (Heart, 1)), null, R("kin_clinic")),
            T("training", "Training Methods", 0, 19, "badlands", null, 100f, "A yard where camp kin learn from their work twice as fast.",
                C(("ingot", 10), ("pelt", 10), (Heart, 1)), null, R("training_yard")),
            T("defenses", "Volt Defenses", 0, 26, "peaks", null, 120f, "Turrets that shock raiders while a Volt Pylon is running.",
                C(("ingot", 12), ("crystal", 8), (Heart, 2)), null, R("volt_turret")),
            T("outposts", "Outpost Logistics", 0, 14, "isles", null, 100f, "Outpost Kits: found a small camp on an outer land, station kin there, and a caravan brings what they gather home.",
                C(("wood", 30), ("fiber", 30), ("ingot", 6), (Heart, 1)), R("outpost_kit")),
            // ---------------- metals of the outer lands ----------------
            T("saltglass", "Saltglass Working", 1, 15, "isles", "forge", 100f, "Driftkey saltglass: a clear, hard blade and the panes for a Greenhouse.",
                C(("saltglass", 15), (Heart, 1)), R("saltglass_bar", "saltglass_saber"), R("greenhouse")),
            T("bogsteel", "Bogsteel Smelting", 1, 20, "swamp", "forge", 130f, "Mirefen bog iron makes the first tools that bite into sunsteel and blight ore.",
                C(("bog_iron", 20), (Heart, 2)), R("bogsteel_bar", "bogsteel_pick", "bogsteel_axe", "bogsteel_glaive", "bogsteel_crossbow")),
            T("sunsteel", "Sunsteel Tempering", 1, 28, "desert", "bogsteel", 160f, "Golden Sunscour steel: a blade, a full suit of armor, and the Waypad that links your outposts.",
                C(("sunsteel_ore", 20), (Heart, 2)), R("sunsteel_bar", "sunsteel_blade", "sunsteel_longbow", "sunsteel_helm", "sunsteel_plate", "sunsteel_greaves"), R("waypad")),
            T("blightsteel", "Blightsteel Quenching", 1, 35, "blight", "bogsteel", 200f, "Dark Blightmoor steel quenched in its own pools: the best pick and axe, a fang of a sword, and thorned armor.",
                C(("blight_ore", 25), (Heart, 3)), R("blightsteel_bar", "blightsteel_pick", "blightsteel_axe", "blightsteel_fang", "blightsteel_hood", "blightsteel_mail", "blightsteel_boots")),
            T("starsteel", "Starsteel Casting", 1, 41, "glow", "blightsteel", 240f, "Glowdeep starcrystal cast into steel: the finest edge and armor there is.",
                C(("starcrystal", 25), (Heart, 3)), R("starsteel_bar", "starsteel_edge", "starsteel_greatbow", "starsteel_crown", "starsteel_raiment", "starsteel_treads")),
        };

        static Dictionary<string, TechDef> byId, byRecipe, byBuilding;
        static List<TechDef> byLevel;

        /// <summary>Every project, lowest level first (the order menus and the wiki list them in).</summary>
        public static List<TechDef> ByLevel
        {
            get
            {
                if (byLevel == null)
                {
                    byLevel = new List<TechDef>(All);
                    byLevel.Sort((a, b) => a.level != b.level ? a.level.CompareTo(b.level) : System.Array.IndexOf(All, a).CompareTo(System.Array.IndexOf(All, b)));
                }
                return byLevel;
            }
        }

        static void Index()
        {
            if (byId != null) return;
            byId = new Dictionary<string, TechDef>(); byRecipe = new Dictionary<string, TechDef>(); byBuilding = new Dictionary<string, TechDef>();
            foreach (var t in All)
            {
                byId[t.id] = t;
                foreach (var r in t.recipes) byRecipe[r] = t;
                foreach (var b in t.buildings) byBuilding[b] = t;
            }
        }

        public static TechDef Get(string id) { Index(); return !string.IsNullOrEmpty(id) && byId.TryGetValue(id, out var t) ? t : null; }
        public static TechDef ForRecipe(string recipeId) { Index(); return recipeId != null && byRecipe.TryGetValue(recipeId, out var t) ? t : null; }
        public static TechDef ForBuilding(string buildingId) { Index(); return buildingId != null && byBuilding.TryGetValue(buildingId, out var t) ? t : null; }

        static PlayerStats S => GameManager.I.Player.Stats;

        public static bool Has(string id) => S.techDone.Contains(id);
        public static bool RecipeUnlocked(string recipeId) { var t = ForRecipe(recipeId); return t == null || Has(t.id) || Dev.FreeAll; }
        public static bool BuildingUnlocked(string buildingId) { var t = ForBuilding(buildingId); return t == null || Has(t.id) || Dev.FreeAll; }
        public static int DoneCount => S.techDone.Count;

        public static TechDef Active => Get(S.researchId);
        public static float Progress01 { get { var t = Active; return t == null ? 0f : Mathf.Clamp01(S.researchProgress / Mathf.Max(1f, t.work)); } }

        /// <summary>Why this can't be started right now (null = ready). "Done" and "Researching" are states, not problems.</summary>
        public static string Blocker(TechDef t)
        {
            if (Has(t.id)) return "Done";
            if (S.researchId == t.id) return "Researching";
            if (Dev.FreeAll) return Active != null ? "Bench is busy" : null;
            if (S.level < t.level) return $"Reach Lv {t.level}";
            if (t.prereq != null && !Has(t.prereq)) return $"Needs {Get(t.prereq).name}";
            if (t.sigil != null && !Wardens.Beaten(t.sigil))
            {
                var w = Wardens.Get(t.sigil);
                return w != null ? $"Needs the Sigil of {w.land}" : "Needs a Sigil";
            }
            if (Active != null) return "Bench is busy";
            if (!ItemSources.Has(t.cost)) return "Missing materials";
            return null;
        }

        public static string Start(TechDef t)
        {
            string why = Blocker(t);
            if (why != null) return why;
            ItemSources.Consume(t.cost);
            S.researchId = t.id;
            S.researchProgress = 0f;
            GameEvents.Good($"Research started: {t.name}. A Crafting kin on the Research Bench works on it - or stand at the bench yourself.");
            SynthAudio.Play(Sfx.Craft, 0.9f, 0.7f);
            GameEvents.RaiseInventoryChanged();
            return null;
        }

        /// <summary>Drops the current research and returns its materials to camp storage (then the bag).</summary>
        public static void Cancel()
        {
            var t = Active;
            if (t == null) return;
            var bm = BaseManager.I;
            foreach (var c in t.cost)
            {
                int left = bm != null && bm.HasBase ? bm.Storage.Add(c.itemId, c.count) : c.count;
                if (left > 0) S.Bag.Add(c.itemId, left);
            }
            S.researchId = null;
            S.researchProgress = 0f;
            GameEvents.Say($"Stopped researching {t.name}. Materials returned.");
            GameEvents.RaiseInventoryChanged();
        }

        public static void AddWork(float w)
        {
            var t = Active;
            if (t == null) return;
            S.researchProgress += w * S.CraftSpeedMul;
            if (S.researchProgress < t.work) return;
            S.researchId = null;
            S.researchProgress = 0f;
            if (!S.techDone.Contains(t.id)) S.techDone.Add(t.id);
            var parts = new List<string>();
            foreach (var b in t.buildings) { var d = ContentDB.Building(b); if (d != null) parts.Add(d.displayName + " (Build)"); }
            foreach (var r in t.recipes) parts.Add(ContentDB.ItemName(r));
            GameEvents.Good($"Research complete: {t.name}!  Unlocked: {string.Join(", ", parts)}.");
            SynthAudio.Play(Sfx.LevelUp, 1f, 0.9f);
            GameManager.I.AddPlayerXp(40 + (int)t.work / 2);
            GameEvents.RaiseBaseChanged();
            GameEvents.RaiseInventoryChanged();
        }
    }
}
