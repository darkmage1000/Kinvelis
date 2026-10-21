namespace Wildbond
{
    /// <summary>
    /// Tonics (KinTonics) and the Kin Egg bag item (KinGrowth.SyncEggSlots), 2026-10-21.
    /// Tonics are dear on purpose: 5 Prism Shards each (deep Peaks veins), so maxing one stat on one kin is 50 shards.
    /// Prism Shards and Rime Crystal put them on the Forge Bench (Stations.Tier).
    /// </summary>
    public static partial class ContentSeed
    {
        static void BuildTonics(Bundle b)
        {
            Item(b, "tonic_vigor", "Vigor Tonic", ItemCategory.Resource, "Vt", Hex("#e0607a"),
                "Permanent training for one kin: +5% health per tonic, up to 10 (+50%). Give it from the Kin tab (pick the kin, Training).", 20);
            Item(b, "tonic_might", "Might Tonic", ItemCategory.Resource, "Mt", Hex("#e88a3a"),
                "Permanent training for one kin: +5% attack per tonic, up to 10 (+50%). Give it from the Kin tab (pick the kin, Training).", 20);
            Item(b, "tonic_ironhide", "Ironhide Tonic", ItemCategory.Resource, "It", Hex("#6a8ab8"),
                "Permanent training for one kin: +5% defence per tonic, up to 10 (+50%). Toughens a hard hitter that goes down too easily. Give it from the Kin tab (pick the kin, Training).", 20);
            Recipe(b, "tonic_vigor", 1, CraftStation.Workbench, 40, 14, ("prism_shard", 5), ("crystal", 3), ("pelt", 2));
            Recipe(b, "tonic_might", 1, CraftStation.Workbench, 40, 14, ("prism_shard", 5), ("crystal", 3), ("cinder", 3));
            Recipe(b, "tonic_ironhide", 1, CraftStation.Workbench, 40, 14, ("prism_shard", 5), ("crystal", 3), ("ingot", 6));

            Item(b, KinGrowth.EggItem, "Kin Egg", ItemCategory.KeyItem, "Eg", Hex("#f0e2c0"),
                "A kin egg. Each takes one bag slot. It only warms in a Hatchery: put it in there, or leave it in camp storage and the Hatchery takes it when a place is free. It hatches 10 minutes later.", 1);
        }
    }
}
