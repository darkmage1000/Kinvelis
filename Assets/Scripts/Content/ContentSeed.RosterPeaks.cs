using UnityEngine;

namespace Wildbond
{
    public static partial class ContentSeed
    {
        /// <summary>Rimefang Peaks kin + island-wide wanderers.</summary>
        static void BuildRosterPeaks(Bundle b)
        {
            const BiomeMask P = BiomeMask.Peaks;

            // ---------------------------------------------------------------- 033 SNOWMOTE
            {
                var d = Kin(b, 33, "snowmote", "Snowmote", Element.Frost, Element.Normal, Rarity.Common, P, Temperament.Timid, Locomotion.Hopper,
                    45, 9, 10, 2f, 5f, 0.65f,
                    "A living snowball with twig arms. Snowmotes roll downhill for fun and come back twice the size.",
                    "Giggly, shy, melts a little when embarrassed.", 3f, 2, 4, A("snowball", "tackle"), A("frost_breath"),
                    Work((WorkType.Chilling, 1), (WorkType.Hauling, 1)), CompanionPerk.Mending, "Companion: slowly restores your HP.",
                    Drops(("gel", 1, 1, 0.6f), ("pelt", 1, 1, 0.3f)),
                    Pal(Hex("#f4faff"), Hex("#9ad8ff"), Hex("#6a5040"), Hex("#ffb0c0")), Pal(Hex("#fff0c0"), Hex("#ffd070"), Hex("#6a5040"), Hex("#ff8ac0")));
                new Plan()
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0, 0.3f, 0), V(0.56f, 0.52f, 0.56f))
                    .Mirror(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0.2f, 0.42f, -0.1f), V(0.2f, 0.2f, 0.2f))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0, 0.2f, -0.2f), V(0.24f, 0.22f, 0.22f))
                    .Mirror(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Accent, V(0.32f, 0.32f, 0f), V(0.02f, 0.22f, 0.02f), V(0, 0, -60))
                    .Eyes(BodyGroup.Head, V(0.1f, 0.38f, 0.24f), 0.07f)
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Glow, V(0.17f, 0.3f, 0.22f), V(0.08f, 0.05f, 0.04f))
                    .Add(PartShape.Crystal, BodyGroup.Head, ColorSlot.Secondary, V(0, 0.52f, 0f), V(0.12f, 0.22f, 0.12f))
                    .Pivot(BodyGroup.Head, V(0, 0.34f, 0f))
                    .ApplyTo(d);
            }

            // ---------------------------------------------------------------- 034 NEEDLEKIT
            {
                var d = Kin(b, 34, "needlekit", "Needlekit", Element.Flora, Element.Frost, Rarity.Common, P, Temperament.Docile, Locomotion.Biped,
                    55, 11, 12, 2f, 5f, 0.78f,
                    "A pinecone sprite that plants a sapling everywhere it sleeps. Whole Rimepine forests trace a Needlekit's wanderings.",
                    "Earnest, quiet, smells of resin.", 2.4f, 1, 3, A("needle_volley", "frost_breath"), A("root_ram"),
                    Work((WorkType.Logging, 2), (WorkType.Cultivating, 1)), CompanionPerk.LumberBoost, "Companion: +50% wood.",
                    Drops(("wood", 1, 2, 0.9f), ("fiber", 1, 1, 0.6f)),
                    Pal(Hex("#3a7a5a"), Hex("#c8a878"), Hex("#6a4a30"), Hex("#f4faff")), Pal(Hex("#c86a3a"), Hex("#f0d0a0"), Hex("#6a3a20"), Hex("#ffe070")));
                new Plan()
                    .Add(PartShape.Cone, BodyGroup.Body, ColorSlot.Primary, V(0, 0.34f, 0), V(0.46f, 0.42f, 0.46f))
                    .Add(PartShape.Cone, BodyGroup.Body, ColorSlot.Primary, V(0, 0.52f, 0), V(0.38f, 0.34f, 0.38f))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Secondary, V(0, 0.36f, 0.16f), V(0.26f, 0.24f, 0.1f))
                    .Add(PartShape.Cone, BodyGroup.Head, ColorSlot.Primary, V(0, 0.72f, 0), V(0.28f, 0.28f, 0.28f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Glow, V(0, 0.84f, 0), V(0.16f, 0.08f, 0.16f))
                    .Eyes(BodyGroup.Head, V(0.06f, 0.44f, 0.2f), 0.05f)
                    .Mirror(PartShape.Cylinder, BodyGroup.ArmL, BodyGroup.ArmR, ColorSlot.Accent, V(0.26f, 0.34f, 0.02f), V(0.03f, 0.2f, 0.03f), V(0, 0, -40))
                    .Pivot(BodyGroup.Head, V(0, 0.6f, 0)).PivotPair(BodyGroup.ArmL, BodyGroup.ArmR, V(0.2f, 0.4f, 0))
                    .Legs2(ColorSlot.Accent, 0.08f, 0.16f, 0.05f)
                    .ApplyTo(d);
            }

            // ---------------------------------------------------------------- 035 GLACIERMANE (mount)
            {
                var d = Kin(b, 35, "glaciermane", "Glaciermane", Element.Frost, Element.Normal, Rarity.Rare, P, Temperament.Aggressive, Locomotion.Quadruped,
                    115, 21, 15, 3f, 9f, 1.3f,
                    "An ice wolf whose mane is a ridge of living crystal. Packs howl before blizzards - some say they summon them.",
                    "Fierce, loyal, runs for the joy of it.", 0.6f, 1, 2, A("ice_fang", "crystal_shard"), A("cyclone"),
                    Work((WorkType.Hauling, 2), (WorkType.Chilling, 2)), CompanionPerk.Swiftness, "Companion: +12% run speed. Fast mount that ignores blizzards.",
                    Drops(("pelt", 2, 3, 1f), ("crystal", 1, 1, 0.3f)),
                    Pal(Hex("#d8e8f4"), Hex("#8fd8ff"), Hex("#5a7a9a"), Hex("#6af0ff")), Pal(Hex("#3a3a48"), Hex("#b070ff"), Hex("#1a1a24"), Hex("#ff6af0")));
                Mount(d, 13.5f, 9.5f, V(0, 1.16f, -0.06f));
                new Plan()
                    .Add(PartShape.Capsule, BodyGroup.Body, ColorSlot.Primary, V(0, 0.8f, 0), V(0.45f, 1.12f, 0.5f), V(90, 0, 0))
                    .Add(PartShape.Capsule, BodyGroup.Body, ColorSlot.White, V(0, 0.72f, 0.06f), V(0.36f, 0.9f, 0.36f), V(90, 0, 0))
                    .Add(PartShape.Crystal, BodyGroup.Body, ColorSlot.Secondary, V(0, 1.02f, 0.32f), V(0.12f, 0.34f, 0.12f), V(-20, 0, 0))
                    .Add(PartShape.Crystal, BodyGroup.Body, ColorSlot.Secondary, V(0, 1.02f, 0.1f), V(0.12f, 0.3f, 0.12f), V(-20, 0, 0))
                    .Add(PartShape.Crystal, BodyGroup.Body, ColorSlot.Secondary, V(0, 1.0f, -0.12f), V(0.1f, 0.24f, 0.1f), V(-20, 0, 0))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0, 1.05f, 0.64f), V(0.34f, 0.32f, 0.36f))
                    .Add(PartShape.Cone, BodyGroup.Head, ColorSlot.Primary, V(0, 1.0f, 0.84f), V(0.16f, 0.25f, 0.16f), V(90, 0, 0))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Dark, V(0, 1.0f, 0.97f), V(0.06f, 0.05f, 0.05f))
                    .Mirror(PartShape.Cone, BodyGroup.Head, ColorSlot.Primary, V(0.12f, 1.26f, 0.58f), V(0.1f, 0.2f, 0.07f))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Glow, V(0.1f, 1.1f, 0.78f), V(0.05f, 0.05f, 0.04f))
                    .Add(PartShape.Cone, BodyGroup.Tail, ColorSlot.Secondary, V(0, 0.9f, -0.78f), V(0.18f, 0.56f, 0.18f), V(-120, 0, 0))
                    .Pivot(BodyGroup.Head, V(0, 0.95f, 0.45f)).Pivot(BodyGroup.Tail, V(0, 0.85f, -0.55f))
                    .Legs4(ColorSlot.Primary, 0.16f, 0.38f, -0.38f, 0.6f, 0.12f, ColorSlot.Accent)
                    .ApplyTo(d);
            }

            // ---------------------------------------------------------------- 036 HOOTEN
            {
                var d = Kin(b, 36, "hooten", "Hooten", Element.Gale, Element.Frost, Rarity.Uncommon, P, Temperament.Curious, Locomotion.Flyer,
                    55, 13, 10, 2.6f, 7f, 0.8f,
                    "A snowy owl that can spot a glint of ore from the top of a pine. Miners follow Hootens to rich veins.",
                    "Watchful, wise-looking, easily distracted by shiny things.", 1.6f, 1, 2, A("gust_slash", "snowball"), A("glint_beam"),
                    Work((WorkType.Hauling, 2), (WorkType.Foraging, 1)), CompanionPerk.OreSense, "Companion: +50% ore, cinder and crystal when mining.",
                    Drops(("pelt", 1, 2, 1f)),
                    Pal(Hex("#f4f6f8"), Hex("#c0c8d8"), Hex("#3a3a40"), Hex("#ffd03a")), Pal(Hex("#c89060"), Hex("#f0d8b0"), Hex("#3a2a20"), Hex("#6af0ff")));
                new Plan()
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0, 1.0f, 0), V(0.5f, 0.62f, 0.5f))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Dark, V(0.12f, 0.92f, 0.22f), V(0.05f, 0.04f, 0.03f))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Dark, V(-0.1f, 0.86f, 0.22f), V(0.05f, 0.04f, 0.03f))
                    .Mirror(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Accent, V(0.08f, 0.66f, 0.1f), V(0.04f, 0.12f, 0.04f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.White, V(0, 1.12f, 0.2f), V(0.38f, 0.32f, 0.12f))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Glow, V(0.09f, 1.14f, 0.26f), V(0.1f, 0.1f, 0.05f))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Dark, V(0.09f, 1.14f, 0.29f), V(0.05f, 0.05f, 0.02f))
                    .Add(PartShape.Cone, BodyGroup.Head, ColorSlot.Accent, V(0, 1.04f, 0.28f), V(0.05f, 0.1f, 0.05f), V(150, 0, 0))
                    .Mirror(PartShape.Cone, BodyGroup.Head, ColorSlot.Primary, V(0.14f, 1.34f, 0.05f), V(0.06f, 0.16f, 0.06f), V(0, 0, -20))
                    .Mirror(PartShape.Sphere, BodyGroup.WingL, BodyGroup.WingR, ColorSlot.Secondary, V(0.27f, 1.0f, -0.02f), V(0.1f, 0.44f, 0.36f))
                    .Pivot(BodyGroup.Head, V(0, 1.05f, 0)).PivotPair(BodyGroup.WingL, BodyGroup.WingR, V(0.2f, 1.14f, 0))
                    .ApplyTo(d);
            }

            // ---------------------------------------------------------------- 037 QUARTZCLAW
            {
                var d = Kin(b, 37, "quartzclaw", "Quartzclaw", Element.Stone, Element.Frost, Rarity.Uncommon, P, Temperament.Territorial, Locomotion.Quadruped,
                    70, 15, 18, 1.6f, 4.2f, 0.9f,
                    "A mountain-lake crab that grows its shell from rime crystal. Old Quartzclaws look like walking geodes.",
                    "Defensive, stubborn, clacks when happy.", 1.5f, 1, 2, A("shell_slam", "crystal_shard"), A("mud_shot"),
                    Work((WorkType.Quarrying, 2), (WorkType.Crafting, 1)), CompanionPerk.StoneBoost, "Companion: +50% stone and ore from rocks.",
                    Drops(("crystal", 1, 1, 0.4f), ("stone", 1, 2, 0.8f)),
                    Pal(Hex("#c86a5a"), Hex("#9ae8ff"), Hex("#e88a6a"), Hex("#ffffff")), Pal(Hex("#4a4a5a"), Hex("#ffd070"), Hex("#6a6a7a"), Hex("#ffffff")));
                d.nearWater = true;
                new Plan()
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0, 0.3f, 0), V(0.82f, 0.32f, 0.62f))
                    .Add(PartShape.Crystal, BodyGroup.Body, ColorSlot.Secondary, V(0, 0.36f, -0.05f), V(0.18f, 0.36f, 0.18f))
                    .Add(PartShape.Crystal, BodyGroup.Body, ColorSlot.Secondary, V(0.18f, 0.36f, 0.05f), V(0.12f, 0.26f, 0.12f), V(0, 0, -25))
                    .Add(PartShape.Crystal, BodyGroup.Body, ColorSlot.Secondary, V(-0.16f, 0.36f, -0.08f), V(0.12f, 0.24f, 0.12f), V(0, 0, 25))
                    .Mirror(PartShape.Sphere, BodyGroup.ArmL, BodyGroup.ArmR, ColorSlot.Accent, V(0.46f, 0.3f, 0.36f), V(0.28f, 0.2f, 0.24f))
                    .Mirror(PartShape.Cone, BodyGroup.ArmL, BodyGroup.ArmR, ColorSlot.Accent, V(0.46f, 0.32f, 0.52f), V(0.08f, 0.18f, 0.06f), V(90, 0, 0))
                    .Mirror(PartShape.Cylinder, BodyGroup.Head, ColorSlot.Primary, V(0.08f, 0.44f, 0.26f), V(0.03f, 0.16f, 0.03f))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Eye, V(0.08f, 0.54f, 0.26f), V(0.07f, 0.07f, 0.07f))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.White, V(0.09f, 0.56f, 0.29f), V(0.025f, 0.025f, 0.025f))
                    .Pivot(BodyGroup.Head, V(0, 0.36f, 0.25f)).PivotPair(BodyGroup.ArmL, BodyGroup.ArmR, V(0.34f, 0.3f, 0.26f))
                    .Legs4(ColorSlot.Primary, 0.36f, 0.12f, -0.18f, 0.2f, 0.05f)
                    .ApplyTo(d);
            }

            // ---------------------------------------------------------------- 038 AUROROX (Epic mount)
            {
                var d = Kin(b, 38, "aurorox", "Aurorox", Element.Lumen, Element.Frost, Rarity.Epic, P, Temperament.Docile, Locomotion.Quadruped,
                    170, 20, 24, 2f, 6f, 1.5f,
                    "A great musk-ox that carries the northern lights in its mane. Herds migrate along the ridges on the longest nights.",
                    "Gentle, immovable, sings in deep hums.", 0.12f, 1, 1, A("aurora_veil", "frost_breath"), A("glint_beam"),
                    Work((WorkType.Hauling, 3), (WorkType.Chilling, 3), (WorkType.Charging, 1)), CompanionPerk.Warmth, "Companion: warmth aura + light. Camp: legendary hauler.",
                    Drops(("pelt", 4, 6, 1f), ("crystal", 1, 2, 0.8f)),
                    Pal(Hex("#3a3a5a"), Hex("#5a5a7a"), Hex("#8a6aff"), Hex("#6affd0")), Pal(Hex("#f0e8d8"), Hex("#d8c8b0"), Hex("#ff8a6a"), Hex("#ffe070")));
                Mount(d, 11.5f, 7.5f, V(0, 1.36f, -0.08f));
                new Plan()
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0, 0.85f, 0), V(0.95f, 0.85f, 1.3f))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Secondary, V(0, 0.55f, 0), V(0.96f, 0.5f, 1.26f))
                    .Add(PartShape.Box, BodyGroup.Body, ColorSlot.Glow, V(0, 1.27f, 0.1f), V(0.08f, 0.06f, 0.95f))
                    .Add(PartShape.Box, BodyGroup.Body, ColorSlot.Accent, V(0.14f, 1.24f, 0f), V(0.06f, 0.05f, 0.82f))
                    .Add(PartShape.Box, BodyGroup.Body, ColorSlot.Glow, V(-0.14f, 1.23f, -0.05f), V(0.06f, 0.05f, 0.78f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0, 0.8f, 0.84f), V(0.44f, 0.44f, 0.42f))
                    .Mirror(PartShape.Cylinder, BodyGroup.Head, ColorSlot.White, V(0.26f, 0.98f, 0.76f), V(0.1f, 0.38f, 0.1f), V(0, 0, -70))
                    .Mirror(PartShape.Cone, BodyGroup.Head, ColorSlot.White, V(0.46f, 0.88f, 0.78f), V(0.08f, 0.22f, 0.08f), V(0, 0, -150))
                    .Eyes(BodyGroup.Head, V(0.12f, 0.88f, 0.98f), 0.06f)
                    .Add(PartShape.Sphere, BodyGroup.Tail, ColorSlot.Glow, V(0, 0.95f, -0.72f), V(0.2f, 0.2f, 0.24f))
                    .Pivot(BodyGroup.Head, V(0, 0.85f, 0.6f)).Pivot(BodyGroup.Tail, V(0, 0.95f, -0.6f))
                    .Legs4(ColorSlot.Secondary, 0.32f, 0.42f, -0.42f, 0.4f, 0.22f, ColorSlot.Dark)
                    .ApplyTo(d);
            }

            // ---------------------------------------------------------------- 039 YETIKIN
            {
                var d = Kin(b, 39, "yetikin", "Yetikin", Element.Frost, Element.Normal, Rarity.Uncommon, P, Temperament.Territorial, Locomotion.Biped,
                    88, 16, 14, 2.2f, 6f, 1f,
                    "A young yeti that practises being scary on travellers. It usually ends up carrying their bags instead.",
                    "Shy show-off, strong, loves snowball fights.", 1.6f, 1, 2, A("ice_fang", "snowball"), A("shell_slam"),
                    Work((WorkType.Logging, 2), (WorkType.Hauling, 2), (WorkType.Chilling, 1)), CompanionPerk.LumberBoost, "Companion: +50% wood.",
                    Drops(("pelt", 2, 3, 1f)),
                    Pal(Hex("#eef4fa"), Hex("#8aa0c0"), Hex("#5a6a80"), Hex("#8fe8ff")), Pal(Hex("#6a4a3a"), Hex("#c89a7a"), Hex("#3a2a20"), Hex("#ffe070")));
                new Plan()
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0, 0.62f, 0), V(0.62f, 0.72f, 0.52f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0, 1.06f, 0.04f), V(0.46f, 0.42f, 0.42f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Secondary, V(0, 1.02f, 0.2f), V(0.28f, 0.24f, 0.1f))
                    .Mirror(PartShape.Cone, BodyGroup.Head, ColorSlot.Accent, V(0.16f, 1.25f, 0f), V(0.06f, 0.15f, 0.06f), V(0, 0, -20))
                    .Eyes(BodyGroup.Head, V(0.07f, 1.06f, 0.25f), 0.05f)
                    .Add(PartShape.Box, BodyGroup.Head, ColorSlot.Dark, V(0, 0.96f, 0.25f), V(0.1f, 0.02f, 0.01f))
                    .Mirror(PartShape.Capsule, BodyGroup.ArmL, BodyGroup.ArmR, ColorSlot.Primary, V(0.37f, 0.55f, 0.05f), V(0.18f, 0.52f, 0.18f))
                    .Mirror(PartShape.Sphere, BodyGroup.ArmL, BodyGroup.ArmR, ColorSlot.Secondary, V(0.38f, 0.28f, 0.07f), V(0.16f, 0.14f, 0.16f))
                    .Pivot(BodyGroup.Head, V(0, 0.9f, 0)).PivotPair(BodyGroup.ArmL, BodyGroup.ArmR, V(0.3f, 0.85f, 0))
                    .Legs2(ColorSlot.Primary, 0.14f, 0.3f, 0.2f, ColorSlot.Secondary)
                    .ApplyTo(d);
            }

            // ---------------------------------------------------------------- 040 STATICUB
            {
                var d = Kin(b, 40, "staticub", "Staticub", Element.Volt, Element.Frost, Rarity.Common, P, Temperament.Curious, Locomotion.Quadruped,
                    42, 11, 9, 2.6f, 7f, 0.6f,
                    "A fluffy mountain pika that builds up static rolling in snow. Petting one is a shocking experience.",
                    "Chirpy, friendly, accidentally zaps friends.", 2.6f, 2, 4, A("static_dart", "tackle"), A("snowball"),
                    Work((WorkType.Charging, 2), (WorkType.Foraging, 1)), CompanionPerk.Glow, "Companion: a crackling light at night.",
                    Drops(("pelt", 1, 1, 0.8f), ("fiber", 1, 1, 0.4f)),
                    Pal(Hex("#f0e8d8"), Hex("#c8b8a0"), Hex("#6a5a4a"), Hex("#ffe84a")), Pal(Hex("#3a3a44"), Hex("#6a6a78"), Hex("#1a1a20"), Hex("#6af0ff")));
                new Plan()
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0, 0.25f, 0), V(0.42f, 0.38f, 0.46f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0, 0.36f, 0.22f), V(0.32f, 0.3f, 0.28f))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0.13f, 0.52f, 0.16f), V(0.13f, 0.13f, 0.05f))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Glow, V(0.13f, 0.52f, 0.18f), V(0.07f, 0.07f, 0.02f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Dark, V(0, 0.34f, 0.36f), V(0.04f, 0.035f, 0.03f))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Glow, V(0.12f, 0.32f, 0.3f), V(0.06f, 0.04f, 0.03f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Glow, V(0, 0.53f, 0.2f), V(0.08f, 0.13f, 0.08f))
                    .Eyes(BodyGroup.Head, V(0.08f, 0.4f, 0.33f), 0.055f)
                    .Pivot(BodyGroup.Head, V(0, 0.3f, 0.15f))
                    .Legs4(ColorSlot.Secondary, 0.12f, 0.14f, -0.14f, 0.1f, 0.07f)
                    .ApplyTo(d);
            }

            // ---------------------------------------------------------------- 041 STORMCREST (Legendary)
            {
                var d = Kin(b, 41, "stormcrest", "Stormcrest", Element.Volt, Element.Gale, Rarity.Legendary, P, Temperament.Territorial, Locomotion.Flyer,
                    200, 30, 22, 3.2f, 10f, 1.6f,
                    "The thunderbird of Rimefang's summit. When it spreads its wings the whole sky darkens, and lightning follows its cry.",
                    "Imperious, ancient, respects only the brave.", 0.05f, 1, 1, A("thunder_chirp", "cyclone"), A("static_dart"),
                    Work((WorkType.Charging, 5), (WorkType.Hauling, 3)), CompanionPerk.Swiftness, "Companion: +12% run speed. Camp: powers a whole base alone.",
                    Drops(("crystal", 2, 3, 1f), ("pelt", 3, 4, 1f)),
                    Pal(Hex("#2a3a6a"), Hex("#f0f4ff"), Hex("#ffd84a"), Hex("#6af0ff")), Pal(Hex("#f0f0f8"), Hex("#ffe070"), Hex("#ff4a4a"), Hex("#ffffff")));
                new Plan()
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0, 1.0f, 0), V(0.5f, 0.55f, 0.8f), V(-20, 0, 0))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Secondary, V(0, 0.95f, 0.18f), V(0.38f, 0.42f, 0.42f))
                    .Mirror(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Accent, V(0.1f, 0.7f, 0.05f), V(0.06f, 0.22f, 0.06f))
                    .Add(PartShape.Box, BodyGroup.Tail, ColorSlot.Primary, V(0, 0.9f, -0.52f), V(0.3f, 0.04f, 0.5f), V(-18, 0, 0))
                    .Mirror(PartShape.Box, BodyGroup.Tail, ColorSlot.Glow, V(0.1f, 0.82f, -0.86f), V(0.05f, 0.03f, 0.5f), V(-10, 10, 0))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0, 1.32f, 0.36f), V(0.32f, 0.3f, 0.32f))
                    .Add(PartShape.Cone, BodyGroup.Head, ColorSlot.Accent, V(0, 1.28f, 0.58f), V(0.09f, 0.2f, 0.09f), V(110, 0, 0))
                    .Add(PartShape.Box, BodyGroup.Head, ColorSlot.Glow, V(0, 1.52f, 0.3f), V(0.05f, 0.24f, 0.1f), V(22, 0, 0))
                    .Add(PartShape.Box, BodyGroup.Head, ColorSlot.Glow, V(0, 1.64f, 0.2f), V(0.05f, 0.2f, 0.09f), V(-30, 0, 0))
                    .Add(PartShape.Box, BodyGroup.Head, ColorSlot.Glow, V(0, 1.74f, 0.24f), V(0.05f, 0.14f, 0.08f), V(26, 0, 0))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Glow, V(0.11f, 1.36f, 0.48f), V(0.06f, 0.06f, 0.05f))
                    .Mirror(PartShape.Box, BodyGroup.WingL, BodyGroup.WingR, ColorSlot.Primary, V(0.66f, 1.12f, -0.05f), V(1.05f, 0.05f, 0.4f), V(0, 0, -10))
                    .Mirror(PartShape.Box, BodyGroup.WingL, BodyGroup.WingR, ColorSlot.Glow, V(0.7f, 1.12f, -0.27f), V(1.0f, 0.04f, 0.06f), V(0, 0, -10))
                    .Mirror(PartShape.Box, BodyGroup.WingL, BodyGroup.WingR, ColorSlot.Secondary, V(1.26f, 1.23f, -0.1f), V(0.36f, 0.04f, 0.3f), V(0, 0, -10))
                    .Pivot(BodyGroup.Head, V(0, 1.2f, 0.28f)).PivotPair(BodyGroup.WingL, BodyGroup.WingR, V(0.2f, 1.1f, 0)).Pivot(BodyGroup.Tail, V(0, 0.95f, -0.3f))
                    .ApplyTo(d);
            }

            // ---------------------------------------------------------------- 042 WISPLING (island-wide, night)
            {
                var d = Kin(b, 42, "wispling", "Wispling", Element.Shade, Element.Lumen, Rarity.Uncommon, BiomeMask.All, Temperament.Timid, Locomotion.Flyer,
                    45, 14, 8, 2.2f, 6f, 0.62f,
                    "A little lantern spirit that guides lost travellers home - if they can keep up. Wisplings vanish at the first light of dawn.",
                    "Kind, flickery, easily startled.", 1.4f, 1, 3, A("hex_flicker", "glint_beam"), A("aurora_veil"),
                    Work((WorkType.Crafting, 2), (WorkType.Charging, 1)), CompanionPerk.Glow, "Companion: bright lantern light at night.",
                    Drops(("crystal", 1, 1, 0.3f), ("gel", 1, 1, 0.5f)),
                    Pal(Hex("#3a3050"), Hex("#5a4a7a"), Hex("#9a7aff"), Hex("#c8f0ff")), Pal(Hex("#e8d8a0"), Hex("#fff0c0"), Hex("#ffb04a"), Hex("#fff4a0")));
                d.spawnTime = SpawnTime.Night;
                new Plan()
                    .Add(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Primary, V(0, 0.82f, 0), V(0.34f, 0.04f, 0.34f))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, V(0, 1.0f, 0), V(0.3f, 0.36f, 0.3f))
                    .Mirror(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Primary, V(0.14f, 1.0f, 0f), V(0.02f, 0.34f, 0.02f))
                    .Add(PartShape.Cone, BodyGroup.Body, ColorSlot.Primary, V(0, 1.22f, 0), V(0.36f, 0.16f, 0.36f))
                    .Add(PartShape.Cone, BodyGroup.Head, ColorSlot.Accent, V(0, 1.38f, 0), V(0.12f, 0.24f, 0.12f))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Dark, V(0.06f, 1.02f, 0.14f), V(0.05f, 0.07f, 0.03f))
                    .Add(PartShape.Cone, BodyGroup.Tail, ColorSlot.Accent, V(0, 0.7f, -0.02f), V(0.1f, 0.2f, 0.1f), V(180, 0, 0))
                    .Pivot(BodyGroup.Head, V(0, 1.1f, 0)).Pivot(BodyGroup.Tail, V(0, 0.8f, 0))
                    .ApplyTo(d);
            }

            // ---------------------------------------------------------------- 045 CLAYLING (island-wide)
            {
                var d = Kin(b, 45, "clayling", "Clayling", Element.Stone, Element.Normal, Rarity.Common, BiomeMask.All, Temperament.Docile, Locomotion.Biped,
                    65, 11, 14, 1.9f, 4.8f, 0.8f,
                    "A little clay golem that shaped itself from riverbank mud. It copies the gestures of anyone it admires.",
                    "Earnest, mimicking, a born apprentice.", 1.8f, 1, 2, A("mud_shot", "tackle"), A("roll_crush"),
                    Work((WorkType.Crafting, 2), (WorkType.Quarrying, 1), (WorkType.Hauling, 1)), CompanionPerk.StoneBoost, "Companion: +50% stone and ore from rocks.",
                    Drops(("stone", 1, 3, 1f)),
                    Pal(Hex("#c8845a"), Hex("#e0a878"), Hex("#6aa050"), Hex("#ffe070")), Pal(Hex("#8a8a98"), Hex("#b0b0c0"), Hex("#6a6aff"), Hex("#6af0ff")));
                d.regional = new RegionalForm
                {
                    enabled = true, biome = BiomeId.Peaks, formName = "Frostclay", element2 = Element.Frost,
                    palette = Pal(Hex("#b8d0e0"), Hex("#e0f0ff"), Hex("#8fe8ff"), Hex("#aef0ff"))
                };
                new Plan()
                    .Add(PartShape.Box, BodyGroup.Body, ColorSlot.Primary, V(0, 0.52f, 0), V(0.5f, 0.5f, 0.4f))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, V(0, 0.55f, 0.21f), V(0.14f, 0.14f, 0.03f))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Accent, V(0.22f, 0.8f, 0f), V(0.18f, 0.08f, 0.16f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0, 0.92f, 0.02f), V(0.36f, 0.32f, 0.34f))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Glow, V(0.08f, 0.95f, 0.17f), V(0.06f, 0.07f, 0.03f))
                    .Add(PartShape.Box, BodyGroup.Head, ColorSlot.Dark, V(0, 0.85f, 0.18f), V(0.1f, 0.02f, 0.01f))
                    .Mirror(PartShape.Capsule, BodyGroup.ArmL, BodyGroup.ArmR, ColorSlot.Primary, V(0.34f, 0.46f, 0f), V(0.16f, 0.42f, 0.16f))
                    .Pivot(BodyGroup.Head, V(0, 0.78f, 0)).PivotPair(BodyGroup.ArmL, BodyGroup.ArmR, V(0.3f, 0.7f, 0))
                    .Mirror(PartShape.Box, BodyGroup.LegFL, BodyGroup.LegFR, ColorSlot.Secondary, V(0.12f, 0.14f, 0f), V(0.16f, 0.28f, 0.18f))
                    .PivotPair(BodyGroup.LegFL, BodyGroup.LegFR, V(0.12f, 0.28f, 0))
                    .ApplyTo(d);
            }
        }
    }
}
