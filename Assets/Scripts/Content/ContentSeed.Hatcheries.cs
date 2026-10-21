namespace Wildbond
{
    /// <summary>
    /// The hatchery ladder (user, 2026-10-21: "as separate buildings like benches, it gets better each building"):
    /// Hatchery (1 egg warming at a time) -> Brood House (2) -> Grand Incubator (5, and it takes waiting eggs from camp
    /// storage by itself). The best one in camp counts (KinGrowth.Places), so a new one replaces the old.
    /// </summary>
    public static partial class ContentSeed
    {
        static void BuildHatcheries(Bundle b)
        {
            var brood = Bld(b, "brood_house", "Brood House",
                "A warm, straw-lined hut for eggs: warms 2 eggs at once (the Hatchery warms 1). Pair kin and hatch eggs here too. Replaces the Hatchery.",
                14, 1.5f, ("wood", 40), ("fiber", 30), ("pelt", 12), ("crystal", 6), ("ingot", 8));
            brood.palette = Pal(Hex("#b88a52"), Hex("#7a5432"), Hex("#fff0d0"), Hex("#ffb060"));
            brood.parts = new[]
            {
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, 0, 0.12f, 0, 2.8f, 0.24f, 2.8f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Primary, 0, 0.75f, 0, 2.4f, 1.1f, 2.4f),
                P(PartShape.Cone, BodyGroup.Body, ColorSlot.Secondary, 0, 1.75f, 0, 2.9f, 1.2f, 2.9f),       // thatched roof
                P(PartShape.Box, BodyGroup.Body, ColorSlot.Secondary, 0, 0.6f, -1.15f, 0.9f, 1.0f, 0.2f),     // doorway
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Accent, -0.45f, 0.5f, -1.35f, 0.42f, 0.56f, 0.42f),   // two eggs in straw by the door
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Accent, 0.45f, 0.48f, -1.35f, 0.4f, 0.52f, 0.4f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Glow, -0.45f, 0.24f, -1.35f, 0.7f, 0.12f, 0.7f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Glow, 0.45f, 0.24f, -1.35f, 0.7f, 0.12f, 0.7f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, 0, 2.45f, 0, 0.25f, 0.25f, 0.25f),          // a warm lamp on top
            };

            var grand = Bld(b, "grand_incubator", "Grand Incubator",
                "A crystal-heated incubator ringed with nests: warms 5 eggs at once and takes waiting eggs from camp storage by itself whenever a place frees up. Pair kin and hatch eggs here too. Replaces the Brood House.",
                24, 1.8f, ("sunsteel_bar", 6), ("crystal", 12), ("pelt", 20), ("stone", 40), ("warden_heart", 1));
            grand.palette = Pal(Hex("#a8a0b8"), Hex("#6a6278"), Hex("#fff4e0"), Hex("#ffd27a"));
            grand.parts = new[]
            {
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, 0, 0.15f, 0, 3.4f, 0.3f, 3.4f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Primary, 0, 0.4f, 0, 2.9f, 0.25f, 2.9f),
                P(PartShape.Crystal, BodyGroup.Body, ColorSlot.Glow, 0, 0.5f, 0, 0.95f, 3.2f, 0.95f),         // the heart crystal
                P(PartShape.Crystal, BodyGroup.Body, ColorSlot.Glow, 0.35f, 0.5f, 0.2f, 0.5f, 1.8f, 0.5f, 0, 0, -18),
                P(PartShape.Crystal, BodyGroup.Body, ColorSlot.Glow, -0.35f, 0.5f, -0.15f, 0.45f, 1.6f, 0.45f, 0, 0, 20),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Accent, 1.05f, 0.75f, 0, 0.42f, 0.56f, 0.42f),   // five eggs round it
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Accent, 0.32f, 0.75f, 1.0f, 0.42f, 0.56f, 0.42f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Accent, -0.85f, 0.75f, 0.62f, 0.42f, 0.56f, 0.42f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Accent, -0.85f, 0.75f, -0.62f, 0.42f, 0.56f, 0.42f),
                P(PartShape.Sphere, BodyGroup.Body, ColorSlot.Accent, 0.32f, 0.75f, -1.0f, 0.42f, 0.56f, 0.42f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, 1.4f, 1.1f, 1.4f, 0.22f, 2.2f, 0.22f),    // four pillars
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, -1.4f, 1.1f, 1.4f, 0.22f, 2.2f, 0.22f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, -1.4f, 1.1f, -1.4f, 0.22f, 2.2f, 0.22f),
                P(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, 1.4f, 1.1f, -1.4f, 0.22f, 2.2f, 0.22f),
                P(PartShape.Crystal, BodyGroup.Body, ColorSlot.Glow, 1.4f, 2.2f, 1.4f, 0.22f, 0.6f, 0.22f),
                P(PartShape.Crystal, BodyGroup.Body, ColorSlot.Glow, -1.4f, 2.2f, 1.4f, 0.22f, 0.6f, 0.22f),
                P(PartShape.Crystal, BodyGroup.Body, ColorSlot.Glow, -1.4f, 2.2f, -1.4f, 0.22f, 0.6f, 0.22f),
                P(PartShape.Crystal, BodyGroup.Body, ColorSlot.Glow, 1.4f, 2.2f, -1.4f, 0.22f, 0.6f, 0.22f),
            };
        }
    }
}
