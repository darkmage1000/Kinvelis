using UnityEngine;

namespace Wildbond
{
    /// <summary>Armor + accessories. Armor tiers: Explorer (fluff/fiber) -> Ironbark (ingot) -> Rimecrystal / Emberforge.
    /// Accessories: crafted utility pieces + alpha-dropped trophies with unique effects. No durability.</summary>
    public static partial class ContentSeed
    {
        static ItemDef Gear(Bundle b, string id, string name, EquipSlot slot, Rarity r, int style, string col, string trim, string desc, UniqueEffect fx, params StatMod[] mods)
        {
            string glyph = slot == EquipSlot.Head ? "Hd" : slot == EquipSlot.Body ? "Ar" : slot == EquipSlot.Boots ? "Bt" : slot == EquipSlot.Amulet ? "Am" : slot == EquipSlot.Bracelet ? "Br" : "Rg";
            var d = Item(b, id, name, ItemCategory.Gear, glyph, Hex(col), desc, 1);
            d.equipSlot = slot; d.rarity = r; d.gearStyle = style; d.trimColor = Hex(trim); d.unique = fx; d.mods = mods;
            return d;
        }

        static StatMod S(StatKind k, float v) => new StatMod(k, v);
        static StatMod Res(Element e, float v) => new StatMod(StatKind.ElementResist, v, e);

        static void BuildGear(Bundle b)
        {
            var N = UniqueEffect.None;
            // ---------------- Explorer (Lv 2, fluff + fiber) ----------------
            Gear(b, "explorer_hood", "Explorer Hood", EquipSlot.Head, Rarity.Common, 0, "#5a8a5a", "#d8b070", "Padded hood. Keeps the wind out.", N,
                S(StatKind.Defense, 3), S(StatKind.ColdResist, 0.15f));
            Gear(b, "explorer_coat", "Explorer Coat", EquipSlot.Body, Rarity.Common, 0, "#4f8a7a", "#d8b070", "Many-pocketed field coat.", N,
                S(StatKind.Defense, 5), S(StatKind.MaxHp, 10));
            Gear(b, "explorer_boots", "Explorer Boots", EquipSlot.Boots, Rarity.Common, 0, "#7a5638", "#d8b070", "Soft soles for long treks.", N,
                S(StatKind.Defense, 2), S(StatKind.MoveSpeed, 0.05f), S(StatKind.MaxStamina, 10));
            Recipe(b, "explorer_hood", 1, CraftStation.Workbench, 15, 2, ("pelt", 2), ("fiber", 6));
            Recipe(b, "explorer_coat", 1, CraftStation.Workbench, 20, 2, ("pelt", 4), ("fiber", 10));
            Recipe(b, "explorer_boots", 1, CraftStation.Workbench, 15, 2, ("pelt", 2), ("fiber", 5), ("wood", 2));

            // ---------------- Ironbark (Lv 5, ingot + wood) ----------------
            Gear(b, "ironbark_helm", "Ironbark Helm", EquipSlot.Head, Rarity.Uncommon, 1, "#b8bccc", "#8a5a30", "Metal cap riveted to a hardwood brim.", N,
                S(StatKind.Defense, 7), S(StatKind.MaxHp, 15));
            Gear(b, "ironbark_mail", "Ironbark Mail", EquipSlot.Body, Rarity.Uncommon, 1, "#aab0c2", "#8a5a30", "Overlapping plates on a timber frame. Heavy.", N,
                S(StatKind.Defense, 13), S(StatKind.MaxHp, 20), S(StatKind.MoveSpeed, -0.03f));
            Gear(b, "ironbark_greaves", "Ironbark Greaves", EquipSlot.Boots, Rarity.Uncommon, 1, "#a0a6b8", "#6a4424", "Shin plates that never seem to tire you.", N,
                S(StatKind.Defense, 5), S(StatKind.MaxStamina, 15));
            Recipe(b, "ironbark_helm", 1, CraftStation.Workbench, 30, 5, ("ingot", 3), ("wood", 3), ("pelt", 1));
            Recipe(b, "ironbark_mail", 1, CraftStation.Workbench, 40, 5, ("ingot", 6), ("wood", 4), ("pelt", 2));
            Recipe(b, "ironbark_greaves", 1, CraftStation.Workbench, 30, 5, ("ingot", 3), ("wood", 2), ("fiber", 4));

            // ---------------- Rimecrystal (Lv 8, crystal) ----------------
            Gear(b, "rime_crown", "Rimecrystal Crown", EquipSlot.Head, Rarity.Rare, 2, "#8fe8ff", "#e8f8ff", "Humming crystal circlet from the Peaks.", N,
                S(StatKind.Defense, 8), Res(Element.Frost, 0.2f), S(StatKind.ColdResist, 0.3f), S(StatKind.CaptureChance, 0.05f));
            Gear(b, "rimecloak", "Rimecloak", EquipSlot.Body, Rarity.Rare, 2, "#6ac8f0", "#e8f8ff", "Woven with rime threads. Snow slides right off.", UniqueEffect.Rimecloak,
                S(StatKind.Defense, 11), Res(Element.Frost, 0.25f), S(StatKind.MaxHp, 20));
            Gear(b, "rime_treads", "Rimecrystal Treads", EquipSlot.Boots, Rarity.Rare, 2, "#7ad8ff", "#e8f8ff", "Crystal cleats. Sure footing on ice.", N,
                S(StatKind.Defense, 6), S(StatKind.MoveSpeed, 0.08f), S(StatKind.StaminaRegen, 0.15f));
            Recipe(b, "rime_crown", 1, CraftStation.Workbench, 40, 8, ("crystal", 4), ("ingot", 2));
            Recipe(b, "rimecloak", 1, CraftStation.Workbench, 50, 8, ("crystal", 5), ("pelt", 6), ("ingot", 2));
            Recipe(b, "rime_treads", 1, CraftStation.Workbench, 40, 8, ("crystal", 3), ("ingot", 2), ("pelt", 2));

            // ---------------- Emberforge (Lv 8, cinder) ----------------
            Gear(b, "ember_helm", "Emberforge Helm", EquipSlot.Head, Rarity.Rare, 3, "#4a3434", "#ff7a2a", "Still warm from the vent forge.", N,
                S(StatKind.Defense, 8), S(StatKind.Attack, 3), Res(Element.Ember, 0.2f));
            Gear(b, "ember_plate", "Emberforge Plate", EquipSlot.Body, Rarity.Rare, 3, "#3e3030", "#ff6a1a", "Cinder-tempered plate. Shrugs off heat.", N,
                S(StatKind.Defense, 15), S(StatKind.Attack, 4), Res(Element.Ember, 0.25f), S(StatKind.HeatResist, 0.6f));
            Gear(b, "ember_greaves", "Emberforge Greaves", EquipSlot.Boots, Rarity.Rare, 3, "#443232", "#ffb03a", "Glowing seams. Strikes land harder.", N,
                S(StatKind.Defense, 6), S(StatKind.Crit, 0.05f), S(StatKind.MaxStamina, 10));
            Recipe(b, "ember_helm", 1, CraftStation.Workbench, 40, 8, ("cinder", 4), ("ingot", 2));
            Recipe(b, "ember_plate", 1, CraftStation.Workbench, 50, 8, ("cinder", 6), ("ingot", 4));
            Recipe(b, "ember_greaves", 1, CraftStation.Workbench, 40, 8, ("cinder", 3), ("ingot", 2), ("pelt", 2));

            // ---------------- crafted uniques ----------------
            Gear(b, "thornweave_vest", "Thornweave Vest", EquipSlot.Body, Rarity.Rare, 4, "#4a7a3a", "#a0d060", "Living bramble vest. Bites back.", UniqueEffect.Thornweave,
                S(StatKind.Defense, 7), S(StatKind.MaxHp, 10));
            Gear(b, "featherstep_boots", "Featherstep Boots", EquipSlot.Boots, Rarity.Rare, 0, "#e8e0c8", "#8fd0ff", "Gel-cushioned soles. Jump off anything.", UniqueEffect.Featherstep,
                S(StatKind.Defense, 3), S(StatKind.MoveSpeed, 0.04f));
            Recipe(b, "thornweave_vest", 1, CraftStation.Workbench, 30, 4, ("fiber", 20), ("pelt", 3), ("wood", 6));
            Recipe(b, "featherstep_boots", 1, CraftStation.Workbench, 30, 3, ("pelt", 4), ("gel", 3), ("fiber", 6));

            // ---------------- accessories (crafted) ----------------
            Gear(b, "mercy_charm", "Mercy Charm", EquipSlot.Amulet, Rarity.Uncommon, 0, "#ffd0e0", "#f0e0a0", "A soft charm that stays the killing blow. Great for catching.", UniqueEffect.Mercy);
            Gear(b, "glowstone_pendant", "Glowstone Pendant", EquipSlot.Amulet, Rarity.Rare, 2, "#fff0a0", "#c0a060", "A crystal that drinks daylight and sheds it at night.", UniqueEffect.Glowstone,
                S(StatKind.MaxHp, 10));
            Gear(b, "hearthlink_pendant", "Hearthlink Pendant", EquipSlot.Amulet, Rarity.Rare, 1, "#ff9a5a", "#d8d8e6", "Warm to the touch whenever your camp is busy.", UniqueEffect.Hearthlink);
            Gear(b, "hearty_amulet", "Hearty Amulet", EquipSlot.Amulet, Rarity.Uncommon, 0, "#ff5a7a", "#d8b070", "Dried sunberries on a cord. You feel fuller.", N,
                S(StatKind.MaxHp, 25), S(StatKind.HungerReduction, 0.15f));
            Gear(b, "gatherers_bracelet", "Gatherer's Bracelet", EquipSlot.Bracelet, Rarity.Common, 0, "#9fd36b", "#8a5a30", "Braided fiber. Your hands know where to pull.", N,
                S(StatKind.GatherYield, 0.15f));
            Gear(b, "binders_bracelet", "Binder's Bracelet", EquipSlot.Bracelet, Rarity.Uncommon, 0, "#8fd96a", "#c9b27a", "Seed-vine wrapped wrist. Throws bind tighter.", N,
                S(StatKind.CaptureChance, 0.1f));
            Gear(b, "windrunner_ring", "Windrunner Ring", EquipSlot.Ring, Rarity.Uncommon, 1, "#d8d8e6", "#8fe8ff", "Light as a breath.", N,
                S(StatKind.MaxStamina, 15), S(StatKind.StaminaRegen, 0.1f));
            Gear(b, "guard_ring", "Guard Ring", EquipSlot.Ring, Rarity.Uncommon, 1, "#b8bccc", "#8a5a30", "A thick band that turns blows.", N,
                S(StatKind.Defense, 4), S(StatKind.MaxHp, 10));
            Gear(b, "keen_ring", "Keen Ring", EquipSlot.Ring, Rarity.Rare, 3, "#d8d8e6", "#ff6a2a", "A cinder chip set in steel.", N,
                S(StatKind.Attack, 3), S(StatKind.Crit, 0.05f));
            Recipe(b, "mercy_charm", 1, CraftStation.Workbench, 15, 2, ("fiber", 6), ("gel", 2), ("berry", 3));
            Recipe(b, "glowstone_pendant", 1, CraftStation.Workbench, 30, 6, ("crystal", 3), ("fiber", 4));
            Recipe(b, "hearthlink_pendant", 1, CraftStation.Workbench, 35, 5, ("ingot", 3), ("cinder", 1), ("wood", 5));
            Recipe(b, "hearty_amulet", 1, CraftStation.Workbench, 20, 3, ("berry", 10), ("fiber", 4), ("pelt", 2));
            Recipe(b, "gatherers_bracelet", 1, CraftStation.Workbench, 15, 2, ("fiber", 10), ("wood", 3));
            Recipe(b, "binders_bracelet", 1, CraftStation.Workbench, 20, 3, ("fiber", 8), ("gel", 2), ("stone", 4));
            Recipe(b, "windrunner_ring", 1, CraftStation.Workbench, 25, 4, ("ingot", 1), ("gel", 2));
            Recipe(b, "guard_ring", 1, CraftStation.Workbench, 25, 4, ("ingot", 2), ("stone", 4));
            Recipe(b, "keen_ring", 1, CraftStation.Workbench, 30, 6, ("ingot", 1), ("cinder", 2));

            // ---------------- alpha trophies (dropped only) ----------------
            Gear(b, "tamers_band", "Tamer's Band", EquipSlot.Bracelet, Rarity.Epic, 1, "#e8c05a", "#8fd96a", "Taken from an alpha that respected you. Kin feel its pull.", UniqueEffect.Tamer,
                S(StatKind.CaptureChance, 0.05f)).alphaDrop = true;
            Gear(b, "kinbond_ring", "Kinbond Ring", EquipSlot.Ring, Rarity.Epic, 2, "#ffe98a", "#ff9ad0", "Two bands grown into one. Your partner fights harder.", UniqueEffect.Kinbond).alphaDrop = true;
            Gear(b, "magpie_bracelet", "Magpie Bracelet", EquipSlot.Bracelet, Rarity.Epic, 1, "#3a3a4a", "#ffd36a", "Glittering bits an alpha hoarded.", UniqueEffect.Magpie,
                S(StatKind.GatherYield, 0.05f)).alphaDrop = true;
            Gear(b, "emberward_bracelet", "Emberward Bracelet", EquipSlot.Bracelet, Rarity.Epic, 3, "#ff6a1a", "#ffd36a", "An ember alpha's scale, cool on your skin.", UniqueEffect.Emberward,
                Res(Element.Ember, 0.3f)).alphaDrop = true;
            Gear(b, "frostward_bracelet", "Frostward Bracelet", EquipSlot.Bracelet, Rarity.Epic, 2, "#8fe8ff", "#ffffff", "A frost alpha's shard. It never melts.", UniqueEffect.Frostward,
                Res(Element.Frost, 0.3f)).alphaDrop = true;
            Gear(b, "alpha_fang_ring", "Alpha Fang Ring", EquipSlot.Ring, Rarity.Epic, 1, "#f2e6c9", "#c43a3a", "A fang band. Predators recognise it.", N,
                S(StatKind.Attack, 6), S(StatKind.Crit, 0.06f)).alphaDrop = true;
            Gear(b, "alpha_heart_amulet", "Alpha Heart Amulet", EquipSlot.Amulet, Rarity.Epic, 3, "#c43a3a", "#ffd36a", "Still beats, faintly.", N,
                S(StatKind.MaxHp, 40), S(StatKind.Defense, 4), S(StatKind.StaminaRegen, 0.1f)).alphaDrop = true;
        }

        /// <summary>Trophy an alpha of this element can drop (ids from BuildGear).</summary>
        public static string[] AlphaTrophyPool(Element e)
        {
            switch (e)
            {
                case Element.Ember: return new[] { "emberward_bracelet", "alpha_heart_amulet", "magpie_bracelet" };
                case Element.Frost: return new[] { "frostward_bracelet", "tamers_band", "kinbond_ring" };
                case Element.Tide: return new[] { "frostward_bracelet", "magpie_bracelet", "tamers_band" };
                case Element.Stone: return new[] { "alpha_heart_amulet", "emberward_bracelet", "alpha_fang_ring" };
                case Element.Shade: return new[] { "alpha_fang_ring", "magpie_bracelet", "kinbond_ring" };
                default: return new[] { "tamers_band", "kinbond_ring", "alpha_fang_ring", "magpie_bracelet", "alpha_heart_amulet" };
            }
        }
    }
}
