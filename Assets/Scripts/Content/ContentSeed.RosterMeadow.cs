using UnityEngine;

namespace Wildbond
{
    public static partial class ContentSeed
    {
        static string[] A(params string[] s) => s;

        /// <summary>Sunmeadow Vale kin (+ wetland / night dwellers that also roam the vale).</summary>
        static void BuildRosterMeadow(Bundle b)
        {
            const BiomeMask M = BiomeMask.Meadow;

            // ---------------------------------------------------------------- 011 PUFFPOD
            {
                var d = Kin(b, 11, "puffpod", "Puffpod", Element.Flora, Element.Gale, Rarity.Common, M, Temperament.Timid, Locomotion.Flyer,
                    42, 9, 8, 2f, 5f, 0.7f,
                    "A dandelion seed that never landed. It drifts on warm air and plants itself wherever it naps.",
                    "Dreamy, weightless, easily blown off course.", 2.4f, 2, 4, A("seed_gust", "pollen_burst"), A("glint_beam"),
                    Work((WorkType.Cultivating, 1), (WorkType.Foraging, 2)), CompanionPerk.ForageBoost, "Companion: +50% berries and fiber.",
                    Drops(("fiber", 1, 2, 1f)),
                    Pal(Hex("#8fd06a"), Hex("#fff8f0"), Hex("#c8a060"), Hex("#ffe070")), Pal(Hex("#ff9ad0"), Hex("#fff0fa"), Hex("#c060a0"), Hex("#ffffff")));
                new Plan()
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Secondary, V(0, 1.0f, 0), V(0.62f, 0.58f, 0.62f))
                    .Mirror(PartShape.Sphere, BodyGroup.Body, ColorSlot.Secondary, V(0.24f, 1.1f, 0.05f), V(0.3f, 0.3f, 0.3f))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Secondary, V(0, 1.25f, -0.05f), V(0.32f, 0.3f, 0.32f))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0, 0.9f, 0.12f), V(0.3f, 0.3f, 0.26f))
                    .Eyes(BodyGroup.Body, V(0.07f, 0.93f, 0.24f), 0.06f)
                    .Add(PartShape.Cylinder, BodyGroup.Tail, ColorSlot.Accent, V(0, 0.62f, 0), V(0.04f, 0.42f, 0.04f))
                    .Add(PartShape.Sphere, BodyGroup.Tail, ColorSlot.Accent, V(0, 0.38f, 0), V(0.12f, 0.17f, 0.12f))
                    .Pivot(BodyGroup.Tail, V(0, 0.82f, 0))
                    .ApplyTo(d);
            }

            // ---------------------------------------------------------------- 012 MOSSNOUT
            {
                var d = Kin(b, 12, "mossnout", "Mossnout", Element.Flora, Element.Normal, Rarity.Common, M, Temperament.Docile, Locomotion.Quadruped,
                    62, 10, 11, 1.9f, 4.8f, 0.85f,
                    "A piglet that grows a carpet of moss on its back. It snuffles out truffles and hidden berries.",
                    "Greedy, cheerful, loves mud baths.", 2.6f, 1, 3, A("tackle", "root_ram"), A("pollen_burst"),
                    Work((WorkType.Foraging, 2), (WorkType.Cultivating, 1), (WorkType.Hauling, 1)), CompanionPerk.ForageBoost, "Companion: +50% berries and fiber.",
                    Drops(("fiber", 1, 2, 0.8f), ("berry", 1, 3, 0.7f)),
                    Pal(Hex("#d49a82"), Hex("#f0c0b0"), Hex("#6aa040"), Hex("#e04a3a")), Pal(Hex("#f0e0a0"), Hex("#fff4d0"), Hex("#a070e0"), Hex("#4ac0ff")));
                new Plan()
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0, 0.42f, 0), V(0.6f, 0.5f, 0.75f))
                    .Add(PartShape.Dome, BodyGroup.Body, ColorSlot.Accent, V(0, 0.52f, -0.02f), V(0.55f, 0.32f, 0.66f))
                    .Add(PartShape.Cylinder, BodyGroup.Body, ColorSlot.White, V(0.1f, 0.72f, -0.1f), V(0.05f, 0.15f, 0.05f))
                    .Add(PartShape.Dome, BodyGroup.Body, ColorSlot.Glow, V(0.1f, 0.78f, -0.1f), V(0.2f, 0.14f, 0.2f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0, 0.48f, 0.42f), V(0.4f, 0.36f, 0.36f))
                    .Add(PartShape.Cylinder, BodyGroup.Head, ColorSlot.Secondary, V(0, 0.44f, 0.62f), V(0.19f, 0.1f, 0.19f), V(90, 0, 0))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Dark, V(0.04f, 0.44f, 0.675f), V(0.03f, 0.04f, 0.02f))
                    .Mirror(PartShape.Cone, BodyGroup.Head, ColorSlot.Primary, V(0.14f, 0.66f, 0.38f), V(0.1f, 0.15f, 0.06f), V(0, 0, -30))
                    .Eyes(BodyGroup.Head, V(0.1f, 0.54f, 0.56f), 0.06f)
                    .Add(PartShape.Sphere, BodyGroup.Tail, ColorSlot.Primary, V(0, 0.46f, -0.4f), V(0.08f, 0.08f, 0.08f))
                    .Pivot(BodyGroup.Head, V(0, 0.45f, 0.3f)).Pivot(BodyGroup.Tail, V(0, 0.45f, -0.36f))
                    .Legs4(ColorSlot.Primary, 0.17f, 0.2f, -0.22f, 0.2f, 0.12f, ColorSlot.Dark)
                    .ApplyTo(d);
            }

            // ---------------------------------------------------------------- 013 BROOKIT
            {
                var d = Kin(b, 13, "brookit", "Brookit", Element.Tide, Element.Normal, Rarity.Common, M, Temperament.Curious, Locomotion.Quadruped,
                    55, 11, 9, 2.8f, 7f, 0.85f,
                    "A sleek river otter that carries a favourite pebble everywhere. Brookits build little dams just to splash over them.",
                    "Playful, nosy, collects shiny pebbles.", 2.2f, 1, 3, A("bubble_burst", "aqua_tail"), A("mud_shot"),
                    Work((WorkType.Irrigating, 2), (WorkType.Hauling, 1), (WorkType.Crafting, 1)), CompanionPerk.Swiftness, "Companion: +12% run speed.",
                    Drops(("gel", 1, 1, 0.7f), ("pelt", 1, 1, 0.6f)),
                    Pal(Hex("#7a5a44"), Hex("#e8d2b8"), Hex("#8fd0f0"), Hex("#aef0ff")), Pal(Hex("#e8e8f0"), Hex("#ffffff"), Hex("#ffb04a"), Hex("#ffffff")));
                d.nearWater = true;
                new Plan()
                    .Add(PartShape.Capsule, BodyGroup.Body, ColorSlot.Primary, V(0, 0.32f, 0), V(0.3f, 0.82f, 0.3f), V(90, 0, 0))
                    .Add(PartShape.Capsule, BodyGroup.Body, ColorSlot.Secondary, V(0, 0.28f, 0.06f), V(0.24f, 0.6f, 0.22f), V(90, 0, 0))
                    .Add(PartShape.Rock, BodyGroup.Body, ColorSlot.Accent, V(0, 0.36f, 0.3f), V(0.1f, 0.08f, 0.1f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0, 0.44f, 0.48f), V(0.3f, 0.26f, 0.3f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Secondary, V(0, 0.4f, 0.6f), V(0.18f, 0.12f, 0.13f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Dark, V(0, 0.42f, 0.665f), V(0.05f, 0.04f, 0.04f))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0.12f, 0.55f, 0.44f), V(0.07f, 0.07f, 0.04f))
                    .Eyes(BodyGroup.Head, V(0.08f, 0.48f, 0.58f), 0.055f)
                    .Add(PartShape.Capsule, BodyGroup.Tail, ColorSlot.Primary, V(0, 0.25f, -0.56f), V(0.14f, 0.52f, 0.1f), V(-100, 0, 0))
                    .Pivot(BodyGroup.Head, V(0, 0.38f, 0.35f)).Pivot(BodyGroup.Tail, V(0, 0.3f, -0.34f))
                    .Legs4(ColorSlot.Primary, 0.1f, 0.22f, -0.22f, 0.16f, 0.07f, ColorSlot.Dark)
                    .ApplyTo(d);
            }

            // ---------------------------------------------------------------- 014 ZAPFLY
            {
                var d = Kin(b, 14, "zapfly", "Zapfly", Element.Volt, Element.Normal, Rarity.Common, M, Temperament.Curious, Locomotion.Flyer,
                    40, 11, 7, 3f, 7.5f, 0.75f,
                    "A dragonfly that hums at the same pitch as a thunderstorm. Its wings leave a tingle in the air.",
                    "Twitchy, inquisitive, loves shiny puddles.", 2.2f, 1, 3, A("static_dart", "spark_peck"), A("seed_gust"),
                    Work((WorkType.Charging, 1), (WorkType.Foraging, 1)), CompanionPerk.Glow, "Companion: a crackling light at night.",
                    Drops(("fiber", 1, 1, 0.6f), ("crystal", 1, 1, 0.05f)),
                    Pal(Hex("#d8e83a"), Hex("#3a6ad8"), Hex("#d8f0ff"), Hex("#ffe84a")), Pal(Hex("#ff5ad0"), Hex("#6a2ad8"), Hex("#ffe0f8"), Hex("#ffffff")));
                new Plan()
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0, 1.0f, 0.18f), V(0.18f, 0.17f, 0.22f))
                    .Add(PartShape.Capsule, BodyGroup.Tail, ColorSlot.Secondary, V(0, 1.0f, -0.28f), V(0.09f, 0.72f, 0.09f), V(90, 0, 0))
                    .Add(PartShape.Sphere, BodyGroup.Tail, ColorSlot.Glow, V(0, 1.0f, -0.62f), V(0.07f, 0.07f, 0.07f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Secondary, V(0, 1.02f, 0.35f), V(0.2f, 0.18f, 0.18f))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Glow, V(0.07f, 1.05f, 0.4f), V(0.1f, 0.11f, 0.09f))
                    .Mirror(PartShape.Box, BodyGroup.WingL, BodyGroup.WingR, ColorSlot.Accent, V(0.3f, 1.05f, 0.2f), V(0.5f, 0.02f, 0.13f))
                    .Mirror(PartShape.Box, BodyGroup.WingL, BodyGroup.WingR, ColorSlot.Accent, V(0.27f, 1.04f, 0.04f), V(0.44f, 0.02f, 0.11f))
                    .Pivot(BodyGroup.Head, V(0, 1.0f, 0.28f)).PivotPair(BodyGroup.WingL, BodyGroup.WingR, V(0.05f, 1.04f, 0.12f)).Pivot(BodyGroup.Tail, V(0, 1.0f, 0.05f))
                    .ApplyTo(d);
            }

            // ---------------------------------------------------------------- 015 THISTLEHOG
            {
                var d = Kin(b, 15, "thistlehog", "Thistlehog", Element.Flora, Element.Normal, Rarity.Uncommon, M, Temperament.Territorial, Locomotion.Quadruped,
                    58, 13, 15, 1.8f, 4.5f, 0.75f,
                    "A hedgehog whose quills are thistle stems. Crowned with purple blooms in spring, it guards its bramble patch with needle volleys.",
                    "Prickly, proud, secretly cuddly.", 1.5f, 1, 2, A("needle_volley", "tackle"), A("root_ram"),
                    Work((WorkType.Foraging, 1), (WorkType.Crafting, 2)), CompanionPerk.ForageBoost, "Companion: +50% berries and fiber.",
                    Drops(("fiber", 2, 3, 1f)),
                    Pal(Hex("#5aa050"), Hex("#d8b890"), Hex("#3a7a3a"), Hex("#b060e0")), Pal(Hex("#e0e0e8"), Hex("#f0e0d0"), Hex("#b0b0c0"), Hex("#ff6a8a")));
                var p = new Plan()
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Secondary, V(0, 0.3f, 0), V(0.5f, 0.4f, 0.6f))
                    .Add(PartShape.Cone, BodyGroup.Body, ColorSlot.Primary, V(0, 0.5f, -0.02f), V(0.12f, 0.24f, 0.12f))
                    .Mirror(PartShape.Cone, BodyGroup.Body, ColorSlot.Primary, V(0.19f, 0.43f, 0f), V(0.11f, 0.22f, 0.11f), V(0, 0, -45))
                    .Mirror(PartShape.Cone, BodyGroup.Body, ColorSlot.Primary, V(0.14f, 0.44f, -0.17f), V(0.11f, 0.22f, 0.11f), V(-35, 0, -35))
                    .Add(PartShape.Cone, BodyGroup.Body, ColorSlot.Primary, V(0, 0.42f, -0.24f), V(0.11f, 0.22f, 0.11f), V(-50, 0, 0))
                    .Add(PartShape.Cone, BodyGroup.Body, ColorSlot.Primary, V(0, 0.47f, 0.13f), V(0.11f, 0.2f, 0.11f), V(30, 0, 0))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, V(0, 0.64f, -0.02f), V(0.1f, 0.08f, 0.1f))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, V(0.3f, 0.52f, 0f), V(0.08f, 0.07f, 0.08f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Secondary, V(0, 0.3f, 0.34f), V(0.24f, 0.22f, 0.26f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Dark, V(0, 0.28f, 0.48f), V(0.05f, 0.05f, 0.05f))
                    .Eyes(BodyGroup.Head, V(0.07f, 0.34f, 0.44f), 0.05f)
                    .Pivot(BodyGroup.Head, V(0, 0.3f, 0.25f))
                    .Legs4(ColorSlot.Secondary, 0.14f, 0.18f, -0.18f, 0.12f, 0.07f);
                p.ApplyTo(d);
            }

            // ---------------------------------------------------------------- 016 LUMIPUP
            {
                var d = Kin(b, 16, "lumipup", "Lumipup", Element.Lumen, Element.Normal, Rarity.Uncommon, M, Temperament.Curious, Locomotion.Quadruped,
                    58, 13, 10, 2.8f, 7.5f, 0.8f,
                    "A puppy born under a meteor shower, still wearing a halo of starlight. It glows brighter when happy.",
                    "Adoring, bouncy, afraid of the dark (ironically).", 1.4f, 1, 2, A("tackle", "glint_beam"), A("aurora_veil"),
                    Work((WorkType.Hauling, 2), (WorkType.Charging, 1)), CompanionPerk.Glow, "Companion: lights up the night around you.",
                    Drops(("pelt", 1, 2, 0.8f), ("crystal", 1, 1, 0.06f)),
                    Pal(Hex("#fff4dc"), Hex("#ffe0b0"), Hex("#f0c060"), Hex("#fff080")), Pal(Hex("#b0a8ff"), Hex("#d8d0ff"), Hex("#7a6ae0"), Hex("#aef0ff")));
                new Plan()
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0, 0.36f, 0), V(0.4f, 0.38f, 0.56f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0, 0.56f, 0.3f), V(0.38f, 0.34f, 0.34f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Secondary, V(0, 0.5f, 0.46f), V(0.16f, 0.12f, 0.12f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Dark, V(0, 0.52f, 0.52f), V(0.05f, 0.04f, 0.04f))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Accent, V(0.2f, 0.56f, 0.26f), V(0.08f, 0.24f, 0.13f), V(0, 0, 20))
                    .Eyes(BodyGroup.Head, V(0.09f, 0.6f, 0.43f), 0.065f)
                    .Add(PartShape.Cylinder, BodyGroup.Head, ColorSlot.Glow, V(0, 0.82f, 0.28f), V(0.32f, 0.02f, 0.32f))
                    .Add(PartShape.Cone, BodyGroup.Tail, ColorSlot.Glow, V(0, 0.5f, -0.33f), V(0.1f, 0.26f, 0.1f), V(-60, 0, 0))
                    .Pivot(BodyGroup.Head, V(0, 0.45f, 0.2f)).Pivot(BodyGroup.Tail, V(0, 0.42f, -0.26f))
                    .Legs4(ColorSlot.Primary, 0.12f, 0.17f, -0.17f, 0.2f, 0.09f, ColorSlot.Secondary)
                    .ApplyTo(d);
            }

            // ---------------------------------------------------------------- 017 BUMBLELOOM
            {
                var d = Kin(b, 17, "bumbleloom", "Bumbleloom", Element.Flora, Element.Gale, Rarity.Uncommon, M, Temperament.Docile, Locomotion.Flyer,
                    50, 11, 9, 2.4f, 6f, 0.75f,
                    "A fuzzy bee that weaves pollen into tiny gardens. Where a Bumbleloom sleeps, flowers bloom by morning.",
                    "Busy, polite, hums work songs.", 1.6f, 1, 3, A("pollen_burst", "needle_volley"), A("seed_gust"),
                    Work((WorkType.Cultivating, 2), (WorkType.Crafting, 1), (WorkType.Foraging, 1)), CompanionPerk.ForageBoost, "Companion: +50% berries and fiber.",
                    Drops(("berry", 1, 2, 0.8f), ("fiber", 1, 1, 0.5f)),
                    Pal(Hex("#ffd23a"), Hex("#e8f4ff"), Hex("#ff9a3a"), Hex("#fff0a0")), Pal(Hex("#6af0c0"), Hex("#f0fff8"), Hex("#2ab0a0"), Hex("#ffffff")));
                new Plan()
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0, 0.95f, -0.05f), V(0.45f, 0.42f, 0.55f))
                    .Add(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Dark, V(0, 0.95f, -0.12f), V(0.46f, 0.06f, 0.43f), V(90, 0, 0))
                    .Add(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Dark, V(0, 0.95f, -0.25f), V(0.38f, 0.06f, 0.36f), V(90, 0, 0))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.White, V(0, 0.98f, 0.14f), V(0.34f, 0.3f, 0.14f))
                    .Add(PartShape.Cone, BodyGroup.Tail, ColorSlot.Dark, V(0, 0.9f, -0.36f), V(0.06f, 0.14f, 0.06f), V(-90, 0, 0))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Dark, V(0, 1.0f, 0.28f), V(0.3f, 0.28f, 0.28f))
                    .Eyes(BodyGroup.Head, V(0.08f, 1.04f, 0.39f), 0.07f)
                    .Mirror(PartShape.Cylinder, BodyGroup.Head, ColorSlot.Dark, V(0.06f, 1.2f, 0.3f), V(0.02f, 0.16f, 0.02f), V(20, 0, -15))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Accent, V(0.08f, 1.28f, 0.33f), V(0.05f, 0.05f, 0.05f))
                    .Mirror(PartShape.Sphere, BodyGroup.WingL, BodyGroup.WingR, ColorSlot.Secondary, V(0.22f, 1.2f, -0.05f), V(0.3f, 0.05f, 0.2f), V(0, 0, -25))
                    .Pivot(BodyGroup.Head, V(0, 1.0f, 0.2f)).PivotPair(BodyGroup.WingL, BodyGroup.WingR, V(0.12f, 1.15f, -0.05f)).Pivot(BodyGroup.Tail, V(0, 0.92f, -0.3f))
                    .ApplyTo(d);
            }

            // ---------------------------------------------------------------- 018 GALEWING
            {
                var d = Kin(b, 18, "galewing", "Galewing", Element.Gale, Element.Normal, Rarity.Rare, M | BiomeMask.Peaks, Temperament.Territorial, Locomotion.Flyer,
                    85, 18, 12, 3f, 9f, 1.2f,
                    "A great hawk that rides mountain updrafts all day without flapping once. Its screech can flatten tall grass.",
                    "Aloof, sharp-eyed, loyal to one partner for life.", 0.7f, 1, 1, A("gust_slash", "cyclone"), A("static_dart"),
                    Work((WorkType.Hauling, 3), (WorkType.Foraging, 1)), CompanionPerk.Swiftness, "Companion: +12% run speed. Camp: expert hauler.",
                    Drops(("pelt", 2, 3, 1f)),
                    Pal(Hex("#5a7aa8"), Hex("#f0f4f8"), Hex("#f0b030"), Hex("#aef0ff")), Pal(Hex("#2a2a30"), Hex("#d0d0d8"), Hex("#ff4a3a"), Hex("#ffd03a")));
                new Plan()
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0, 1.0f, 0), V(0.45f, 0.5f, 0.72f), V(-20, 0, 0))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Secondary, V(0, 0.96f, 0.16f), V(0.36f, 0.4f, 0.4f))
                    .Mirror(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Accent, V(0.1f, 0.72f, 0.05f), V(0.05f, 0.22f, 0.05f))
                    .Add(PartShape.Box, BodyGroup.Tail, ColorSlot.Primary, V(0, 0.92f, -0.46f), V(0.32f, 0.04f, 0.42f), V(-20, 0, 0))
                    .Add(PartShape.Box, BodyGroup.Tail, ColorSlot.Glow, V(0, 0.86f, -0.64f), V(0.26f, 0.04f, 0.08f), V(-20, 0, 0))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0, 1.3f, 0.33f), V(0.3f, 0.28f, 0.3f))
                    .Add(PartShape.Cone, BodyGroup.Head, ColorSlot.Accent, V(0, 1.26f, 0.53f), V(0.08f, 0.18f, 0.08f), V(110, 0, 0))
                    .Add(PartShape.Cone, BodyGroup.Head, ColorSlot.Primary, V(0, 1.46f, 0.22f), V(0.08f, 0.2f, 0.08f), V(-30, 0, 0))
                    .Eyes(BodyGroup.Head, V(0.1f, 1.34f, 0.44f), 0.055f)
                    .Mirror(PartShape.Box, BodyGroup.WingL, BodyGroup.WingR, ColorSlot.Primary, V(0.58f, 1.1f, -0.05f), V(0.9f, 0.05f, 0.36f), V(0, 0, -10))
                    .Mirror(PartShape.Box, BodyGroup.WingL, BodyGroup.WingR, ColorSlot.Secondary, V(1.08f, 1.19f, -0.08f), V(0.32f, 0.04f, 0.26f), V(0, 0, -10))
                    .Pivot(BodyGroup.Head, V(0, 1.2f, 0.25f)).PivotPair(BodyGroup.WingL, BodyGroup.WingR, V(0.2f, 1.1f, 0)).Pivot(BodyGroup.Tail, V(0, 0.95f, -0.3f))
                    .ApplyTo(d);
            }

            // ---------------------------------------------------------------- 019 BLOOMWYRM (Epic)
            {
                var d = Kin(b, 19, "bloomwyrm", "Bloomwyrm", Element.Flora, Element.Lumen, Rarity.Epic, M, Temperament.Territorial, Locomotion.Slither,
                    150, 24, 20, 2.4f, 7f, 1.4f,
                    "An ancient serpent whose scales are petals. Legends say the Great Bloomtree grew from a Bloomwyrm's shed skin.",
                    "Serene, ancient, terrifying when its garden is trampled.", 0.12f, 1, 1, A("petal_storm", "glint_beam"), A("cyclone"),
                    Work((WorkType.Cultivating, 4), (WorkType.Logging, 2)), CompanionPerk.ForageBoost, "Companion: +50% berries & fiber. Camp: legendary gardener.",
                    Drops(("fiber", 4, 8, 1f), ("crystal", 1, 1, 0.6f)),
                    Pal(Hex("#4aa060"), Hex("#f0e8c0"), Hex("#2a7a40"), Hex("#ff9ad0")), Pal(Hex("#e8d0ff"), Hex("#fff8ff"), Hex("#a070e0"), Hex("#ffe070")));
                new Plan()
                    .Serpent(ColorSlot.Primary, ColorSlot.Accent, 0.3f, 0.48f, 7, 0.34f)
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, V(0, 0.54f, 0f), V(0.22f, 0.08f, 0.22f))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, V(0, 0.5f, -0.68f), V(0.18f, 0.07f, 0.18f))
                    .Add(PartShape.Sphere, BodyGroup.Tail, ColorSlot.Glow, V(0, 0.4f, -1.36f), V(0.14f, 0.06f, 0.14f))
                    .Add(PartShape.Cone, BodyGroup.Tail, ColorSlot.Glow, V(0, 0.2f, -2.3f), V(0.1f, 0.3f, 0.1f), V(-90, 0, 0))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0, 0.5f, 0.42f), V(0.42f, 0.36f, 0.46f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Secondary, V(0, 0.4f, 0.52f), V(0.32f, 0.12f, 0.32f))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Glow, V(0.12f, 0.58f, 0.58f), V(0.07f, 0.07f, 0.06f))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Accent, V(0.15f, 0.72f, 0.34f), V(0.08f, 0.26f, 0.12f), V(-30, 0, -20))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Glow, V(0, 0.74f, 0.38f), V(0.2f, 0.12f, 0.2f))
                    .Pivot(BodyGroup.Head, V(0, 0.4f, 0.22f))
                    .ApplyTo(d);
            }

            // ---------------------------------------------------------------- 020 RIBBLOOM
            {
                var d = Kin(b, 20, "ribbloom", "Ribbloom", Element.Tide, Element.Flora, Rarity.Common, M, Temperament.Docile, Locomotion.Hopper,
                    52, 10, 10, 2f, 5f, 0.72f,
                    "A pond frog that wears a lily pad as a sunhat. Its croaks make water plants grow faster.",
                    "Laid-back, sings at dusk, hates dry weather.", 2.2f, 2, 3, A("bubble_burst", "tongue_lash"), A("pollen_burst"),
                    Work((WorkType.Irrigating, 2), (WorkType.Cultivating, 1)), CompanionPerk.Mending, "Companion: slowly restores your HP.",
                    Drops(("gel", 1, 2, 0.8f), ("fiber", 1, 1, 0.4f)),
                    Pal(Hex("#6ac070"), Hex("#e8f0b0"), Hex("#4aa050"), Hex("#ffb0d0")), Pal(Hex("#4a8ae0"), Hex("#d8f0ff"), Hex("#2a60b0"), Hex("#ffe04a")));
                d.nearWater = true;
                new Plan()
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0, 0.25f, 0), V(0.5f, 0.36f, 0.5f))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Secondary, V(0, 0.2f, 0.1f), V(0.38f, 0.22f, 0.36f))
                    .Mirror(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0.22f, 0.12f, -0.12f), V(0.16f, 0.14f, 0.24f))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0.13f, 0.42f, 0.16f), V(0.15f, 0.15f, 0.15f))
                    .Eyes(BodyGroup.Head, V(0.13f, 0.44f, 0.22f), 0.075f)
                    .Add(PartShape.Box, BodyGroup.Head, ColorSlot.Dark, V(0, 0.28f, 0.26f), V(0.26f, 0.02f, 0.02f))
                    .Add(PartShape.Cylinder, BodyGroup.Head, ColorSlot.Accent, V(0, 0.52f, 0.02f), V(0.5f, 0.03f, 0.5f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Glow, V(0.05f, 0.56f, 0.02f), V(0.13f, 0.1f, 0.13f))
                    .Pivot(BodyGroup.Head, V(0, 0.3f, 0.1f))
                    .ApplyTo(d);
            }

            // ---------------------------------------------------------------- 021 BURROWBY
            {
                var d = Kin(b, 21, "burrowby", "Burrowby", Element.Stone, Element.Normal, Rarity.Common, M | BiomeMask.Badlands, Temperament.Timid, Locomotion.Quadruped,
                    50, 10, 12, 1.8f, 5f, 0.7f,
                    "A near-blind mole with enormous digging claws. It tunnels so fast the ground ripples behind it.",
                    "Shy, sleepy, tidy about its tunnels.", 2.2f, 1, 2, A("tackle", "mud_shot"), A("roll_crush"),
                    Work((WorkType.Quarrying, 2), (WorkType.Foraging, 1)), CompanionPerk.StoneBoost, "Companion: +50% stone and ore from rocks.",
                    Drops(("stone", 1, 3, 1f), ("ore", 1, 1, 0.2f)),
                    Pal(Hex("#5a4a44"), Hex("#f0a8a0"), Hex("#8a7a70"), Hex("#ff8aa0")), Pal(Hex("#e8e0d0"), Hex("#ffd0d8"), Hex("#c8b8a8"), Hex("#ff6a8a")));
                d.regional = new RegionalForm
                {
                    enabled = true, biome = BiomeId.Badlands, formName = "Dust", element2 = Element.Gale,
                    palette = Pal(Hex("#c89a6a"), Hex("#f0c0a0"), Hex("#a07a5a"), Hex("#ff9a7a"))
                };
                new Plan()
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0, 0.25f, 0), V(0.5f, 0.4f, 0.62f))
                    .Add(PartShape.Cone, BodyGroup.Head, ColorSlot.Secondary, V(0, 0.26f, 0.44f), V(0.16f, 0.22f, 0.16f), V(90, 0, 0))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Glow, V(0, 0.26f, 0.56f), V(0.07f, 0.07f, 0.07f))
                    .Eyes(BodyGroup.Head, V(0.08f, 0.34f, 0.35f), 0.035f)
                    .Mirror(PartShape.Box, BodyGroup.LegFL, BodyGroup.LegFR, ColorSlot.Secondary, V(0.22f, 0.06f, 0.28f), V(0.18f, 0.05f, 0.15f), V(0, 20, 0))
                    .Add(PartShape.Cone, BodyGroup.Tail, ColorSlot.Secondary, V(0, 0.2f, -0.36f), V(0.05f, 0.12f, 0.05f), V(-100, 0, 0))
                    .Pivot(BodyGroup.Head, V(0, 0.26f, 0.25f)).Pivot(BodyGroup.Tail, V(0, 0.22f, -0.3f))
                    .Legs4(ColorSlot.Primary, 0.16f, 0.22f, -0.2f, 0.12f, 0.08f)
                    .ApplyTo(d);
            }

            // ---------------------------------------------------------------- 043 GLOOMTOAD (night wetland)
            {
                var d = Kin(b, 43, "gloomtoad", "Gloomtoad", Element.Shade, Element.Tide, Rarity.Common, M, Temperament.Docile, Locomotion.Hopper,
                    55, 11, 11, 1.8f, 4.5f, 0.78f,
                    "A warty toad that only surfaces after dark. Its glowing spots lure moths - and lost travellers - to the water's edge.",
                    "Gloomy, philosophical, surprisingly gentle.", 2f, 1, 2, A("tongue_lash", "hex_flicker"), A("bubble_burst"),
                    Work((WorkType.Irrigating, 1), (WorkType.Foraging, 1)), CompanionPerk.Mending, "Companion: slowly restores your HP.",
                    Drops(("gel", 1, 2, 1f)),
                    Pal(Hex("#4a3a5a"), Hex("#7a6a8a"), Hex("#2a2438"), Hex("#b0ff6a")), Pal(Hex("#e0d8c0"), Hex("#fff8e0"), Hex("#a09070"), Hex("#ff6ab0")));
                d.nearWater = true; d.spawnTime = SpawnTime.Night;
                new Plan()
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0, 0.28f, 0), V(0.56f, 0.4f, 0.56f))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, V(0.14f, 0.44f, -0.05f), V(0.08f, 0.05f, 0.08f))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, V(-0.1f, 0.45f, -0.12f), V(0.07f, 0.05f, 0.07f))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, V(0f, 0.47f, 0.05f), V(0.06f, 0.04f, 0.06f))
                    .Mirror(PartShape.Sphere, BodyGroup.Body, ColorSlot.Accent, V(0.24f, 0.12f, -0.12f), V(0.18f, 0.14f, 0.26f))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0.14f, 0.46f, 0.18f), V(0.14f, 0.12f, 0.14f))
                    .Eyes(BodyGroup.Head, V(0.14f, 0.46f, 0.24f), 0.07f)
                    .Mirror(PartShape.Box, BodyGroup.Head, ColorSlot.Secondary, V(0.14f, 0.51f, 0.22f), V(0.14f, 0.03f, 0.08f), V(20, 0, 0))
                    .Add(PartShape.Box, BodyGroup.Head, ColorSlot.Dark, V(0, 0.3f, 0.28f), V(0.3f, 0.02f, 0.02f))
                    .Pivot(BodyGroup.Head, V(0, 0.32f, 0.1f))
                    .ApplyTo(d);
            }

            // ---------------------------------------------------------------- 044 BRAMBLEBAT (night)
            {
                var d = Kin(b, 44, "bramblebat", "Bramblebat", Element.Shade, Element.Flora, Rarity.Uncommon, M | BiomeMask.Peaks, Temperament.Aggressive, Locomotion.Flyer,
                    50, 15, 9, 3f, 8f, 0.72f,
                    "A bat whose wings are woven from thorny leaves. Swarms of Bramblebats strip berry bushes bare in a single night.",
                    "Mischievous, hungry, loyal to its roost.", 1.8f, 2, 3, A("shadow_pounce", "needle_volley"), A("pollen_burst"),
                    Work((WorkType.Foraging, 2)), CompanionPerk.ForageBoost, "Companion: +50% berries and fiber.",
                    Drops(("fiber", 1, 2, 0.8f), ("pelt", 1, 1, 0.5f)),
                    Pal(Hex("#3a2a3a"), Hex("#8a4a4a"), Hex("#4a7a3a"), Hex("#ff4a6a")), Pal(Hex("#f0e8d8"), Hex("#d8a0a0"), Hex("#e0a030"), Hex("#6af0ff")));
                d.spawnTime = SpawnTime.Night;
                new Plan()
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0, 1.0f, 0), V(0.3f, 0.34f, 0.3f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0, 1.18f, 0.08f), V(0.26f, 0.24f, 0.24f))
                    .Mirror(PartShape.Cone, BodyGroup.Head, ColorSlot.Primary, V(0.1f, 1.36f, 0.05f), V(0.1f, 0.2f, 0.05f), V(0, 0, -15))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Glow, V(0.06f, 1.2f, 0.18f), V(0.05f, 0.05f, 0.04f))
                    .Mirror(PartShape.Cone, BodyGroup.Head, ColorSlot.White, V(0.03f, 1.08f, 0.18f), V(0.02f, 0.05f, 0.02f), V(180, 0, 0))
                    .Mirror(PartShape.Sphere, BodyGroup.WingL, BodyGroup.WingR, ColorSlot.Accent, V(0.35f, 1.03f, -0.02f), V(0.56f, 0.05f, 0.36f), V(0, 0, -15))
                    .Mirror(PartShape.Cone, BodyGroup.WingL, BodyGroup.WingR, ColorSlot.Secondary, V(0.6f, 1.1f, -0.1f), V(0.04f, 0.12f, 0.04f), V(0, 0, -60))
                    .Pivot(BodyGroup.Head, V(0, 1.1f, 0.05f)).PivotPair(BodyGroup.WingL, BodyGroup.WingR, V(0.12f, 1.05f, 0))
                    .ApplyTo(d);
            }

            // ---------------------------------------------------------------- 046 TIDALITH (mount)
            {
                var d = Kin(b, 46, "tidalith", "Tidalith", Element.Tide, Element.Stone, Rarity.Rare, M | BiomeMask.Badlands, Temperament.Territorial, Locomotion.Quadruped,
                    140, 16, 24, 1.8f, 4.5f, 1.4f,
                    "A giant turtle carrying a tiny island on its back - complete with a sapling. Birds nest on sleeping Tidaliths for years.",
                    "Unhurried, wise, hates being rushed.", 0.5f, 1, 1, A("aqua_tail", "shell_slam"), A("mud_shot"),
                    Work((WorkType.Irrigating, 3), (WorkType.Hauling, 2)), CompanionPerk.Mending, "Companion: slowly restores your HP.",
                    Drops(("gel", 2, 3, 1f), ("stone", 2, 3, 0.8f)),
                    Pal(Hex("#5a8a8a"), Hex("#8ac0b0"), Hex("#8a8a80"), Hex("#6ac050")), Pal(Hex("#d8c8a0"), Hex("#f0e0c0"), Hex("#b0a080"), Hex("#ff9ad0")));
                d.nearWater = true;
                Mount(d, 9.5f, 6f, V(0, 0.98f, -0.12f));
                new Plan()
                    .Add(PartShape.Dome, BodyGroup.Body, ColorSlot.Primary, V(0, 0.32f, 0), V(1.2f, 0.9f, 1.4f))
                    .Add(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, V(0, 0.33f, 0), V(1.16f, 0.12f, 1.36f))
                    .Add(PartShape.Rock, BodyGroup.Body, ColorSlot.Accent, V(0.25f, 0.68f, 0.2f), V(0.34f, 0.24f, 0.3f))
                    .Add(PartShape.Rock, BodyGroup.Body, ColorSlot.Accent, V(-0.28f, 0.66f, -0.22f), V(0.3f, 0.22f, 0.3f))
                    .Add(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Dark, V(0.18f, 0.9f, -0.12f), V(0.06f, 0.34f, 0.06f))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, V(0.18f, 1.1f, -0.12f), V(0.32f, 0.26f, 0.32f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Secondary, V(0, 0.5f, 0.84f), V(0.34f, 0.3f, 0.4f))
                    .Eyes(BodyGroup.Head, V(0.1f, 0.58f, 0.97f), 0.05f)
                    .Add(PartShape.Cone, BodyGroup.Tail, ColorSlot.Secondary, V(0, 0.3f, -0.8f), V(0.12f, 0.24f, 0.12f), V(-90, 0, 0))
                    .Pivot(BodyGroup.Head, V(0, 0.45f, 0.6f)).Pivot(BodyGroup.Tail, V(0, 0.3f, -0.66f))
                    .Legs4(ColorSlot.Secondary, 0.45f, 0.45f, -0.45f, 0.3f, 0.26f)
                    .ApplyTo(d);
            }

            // ---------------------------------------------------------------- 047 NIMBUFF
            {
                var d = Kin(b, 47, "nimbuff", "Nimbuff", Element.Gale, Element.Normal, Rarity.Common, M | BiomeMask.Peaks, Temperament.Timid, Locomotion.Flyer,
                    50, 9, 10, 2.2f, 6f, 0.8f,
                    "A cloud that decided to become a sheep. When a herd of Nimbuffs gets upset, it drizzles.",
                    "Fluffy, anxious, sleeps in the sky.", 2.4f, 2, 4, A("seed_gust", "tackle"), A("cyclone"),
                    Work((WorkType.Irrigating, 1), (WorkType.Foraging, 1), (WorkType.Hauling, 1)), CompanionPerk.Swiftness, "Companion: +12% run speed.",
                    Drops(("pelt", 1, 2, 1f)),
                    Pal(Hex("#f8faff"), Hex("#6a7080"), Hex("#d8c890"), Hex("#aee4ff")), Pal(Hex("#ffd8e8"), Hex("#8a5a70"), Hex("#ffe0a0"), Hex("#ffffff")));
                new Plan()
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0, 1.0f, 0), V(0.62f, 0.46f, 0.72f))
                    .Mirror(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0.22f, 1.14f, 0.12f), V(0.34f, 0.3f, 0.34f))
                    .Mirror(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0.2f, 1.1f, -0.2f), V(0.34f, 0.3f, 0.34f))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0, 1.2f, -0.02f), V(0.36f, 0.3f, 0.36f))
                    .Mirror(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, V(0.14f, 0.72f, 0.14f), V(0.06f, 0.16f, 0.06f))
                    .Mirror(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, V(0.14f, 0.72f, -0.16f), V(0.06f, 0.16f, 0.06f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Secondary, V(0, 0.98f, 0.36f), V(0.24f, 0.22f, 0.16f))
                    .Mirror(PartShape.Cone, BodyGroup.Head, ColorSlot.Accent, V(0.1f, 1.1f, 0.32f), V(0.05f, 0.1f, 0.05f), V(0, 0, -30))
                    .Eyes(BodyGroup.Head, V(0.06f, 1.0f, 0.43f), 0.045f)
                    .Pivot(BodyGroup.Head, V(0, 0.98f, 0.28f))
                    .ApplyTo(d);
            }
        }
    }
}
