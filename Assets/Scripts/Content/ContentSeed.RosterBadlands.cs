using UnityEngine;

namespace Wildbond
{
    public static partial class ContentSeed
    {
        /// <summary>Cinderreach Badlands kin.</summary>
        static void BuildRosterBadlands(Bundle b)
        {
            const BiomeMask B = BiomeMask.Badlands;

            // ---------------------------------------------------------------- 022 CINDERBUG
            {
                var d = Kin(b, 22, "cinderbug", "Cinderbug", Element.Ember, Element.Stone, Rarity.Common, B, Temperament.Docile, Locomotion.Quadruped,
                    60, 11, 14, 1.8f, 4.5f, 0.8f,
                    "A beetle whose shell stores the day's heat. At night the cracks in its back glow like embers.",
                    "Slow, sturdy, loves basking on warm stones.", 2.6f, 1, 3, A("tackle", "ember_spit"), A("roll_crush"),
                    Work((WorkType.Kindling, 1), (WorkType.Quarrying, 1), (WorkType.Hauling, 1)), CompanionPerk.OreSense, "Companion: +50% ore and cinder when mining.",
                    Drops(("cinder", 1, 2, 0.8f), ("stone", 1, 2, 0.6f)),
                    Pal(Hex("#5a3a30"), Hex("#3a2a28"), Hex("#c08040"), Hex("#ff7a2a")), Pal(Hex("#2a4a6a"), Hex("#1a2a3a"), Hex("#6ac0ff"), Hex("#6af0ff")));
                new Plan()
                    .Add(PartShape.Dome, BodyGroup.Body, ColorSlot.Primary, V(0, 0.2f, 0), V(0.62f, 0.52f, 0.82f))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Secondary, V(0, 0.2f, 0), V(0.52f, 0.2f, 0.72f))
                    .Add(PartShape.Box, BodyGroup.Body, ColorSlot.Dark, V(0, 0.45f, 0), V(0.02f, 0.05f, 0.7f))
                    .Mirror(PartShape.Box, BodyGroup.Body, ColorSlot.Glow, V(0.12f, 0.42f, 0.1f), V(0.12f, 0.03f, 0.03f), V(0, 30, 0))
                    .Mirror(PartShape.Box, BodyGroup.Body, ColorSlot.Glow, V(0.14f, 0.4f, -0.14f), V(0.1f, 0.03f, 0.03f), V(0, -25, 0))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Secondary, V(0, 0.22f, 0.45f), V(0.26f, 0.2f, 0.22f))
                    .Add(PartShape.Cone, BodyGroup.Head, ColorSlot.Accent, V(0, 0.34f, 0.55f), V(0.06f, 0.2f, 0.06f), V(50, 0, 0))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Glow, V(0.07f, 0.26f, 0.54f), V(0.05f, 0.05f, 0.04f))
                    .Pivot(BodyGroup.Head, V(0, 0.2f, 0.35f))
                    .Legs4(ColorSlot.Dark, 0.26f, 0.2f, -0.2f, 0.18f, 0.04f)
                    .ApplyTo(d);
            }

            // ---------------------------------------------------------------- 023 DUNESTRIDER (mount)
            {
                var d = Kin(b, 23, "dunestrider", "Dunestrider", Element.Stone, Element.Gale, Rarity.Uncommon, B, Temperament.Docile, Locomotion.Quadruped,
                    90, 13, 14, 2.8f, 8f, 1.3f,
                    "A long-legged sand lizard that can cross a whole mesa between sunrise and noon. Caravans of old were built around them.",
                    "Patient, hardy, grumbles at wet weather.", 1.4f, 1, 2, A("sand_kick", "gust_slash"), A("mud_shot"),
                    Work((WorkType.Hauling, 3)), CompanionPerk.Swiftness, "Companion: +12% run speed. Camp: tireless hauler.",
                    Drops(("pelt", 1, 2, 0.8f), ("stone", 1, 2, 0.5f)),
                    Pal(Hex("#d8b078"), Hex("#b08050"), Hex("#c05a3a"), Hex("#f0d090")), Pal(Hex("#6a8ad8"), Hex("#4a60a0"), Hex("#f0d040"), Hex("#ffffff")));
                Mount(d, 12.5f, 9f, V(0, 1.3f, 0f));
                new Plan()
                    .Add(PartShape.Capsule, BodyGroup.Body, ColorSlot.Primary, V(0, 1.05f, 0), V(0.4f, 1.0f, 0.42f), V(90, 0, 0))
                    .Add(PartShape.Dome, BodyGroup.Body, ColorSlot.Accent, V(0, 1.18f, -0.05f), V(0.42f, 0.34f, 0.52f))
                    .Add(PartShape.Box, BodyGroup.Body, ColorSlot.Glow, V(0, 1.24f, 0.2f), V(0.44f, 0.04f, 0.24f))
                    .Add(PartShape.Cylinder, BodyGroup.Head, ColorSlot.Primary, V(0, 1.4f, 0.55f), V(0.14f, 0.56f, 0.14f), V(20, 0, 0))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0, 1.7f, 0.68f), V(0.22f, 0.2f, 0.34f))
                    .Mirror(PartShape.Cone, BodyGroup.Head, ColorSlot.Secondary, V(0.12f, 1.78f, 0.6f), V(0.08f, 0.2f, 0.04f), V(0, 0, -50))
                    .Eyes(BodyGroup.Head, V(0.08f, 1.74f, 0.78f), 0.05f)
                    .Add(PartShape.Cone, BodyGroup.Tail, ColorSlot.Primary, V(0, 1.0f, -0.82f), V(0.14f, 0.62f, 0.14f), V(-110, 0, 0))
                    .Pivot(BodyGroup.Head, V(0, 1.15f, 0.4f)).Pivot(BodyGroup.Tail, V(0, 1.05f, -0.5f))
                    .Legs4(ColorSlot.Secondary, 0.16f, 0.35f, -0.35f, 0.9f, 0.1f, ColorSlot.Dark)
                    .ApplyTo(d);
            }

            // ---------------------------------------------------------------- 024 SPARKRAT
            {
                var d = Kin(b, 24, "sparkrat", "Sparkrat", Element.Volt, Element.Normal, Rarity.Common, B, Temperament.Curious, Locomotion.Hopper,
                    42, 12, 8, 3f, 8.5f, 0.62f,
                    "A desert jerboa that stores static in its long tail. On dry nights you can see Sparkrats as hopping blue sparks.",
                    "Hyper, nosy, can't sit still.", 2.4f, 2, 3, A("static_dart", "spark_peck"), A("sand_kick"),
                    Work((WorkType.Charging, 2), (WorkType.Foraging, 1)), CompanionPerk.Glow, "Companion: a crackling light at night.",
                    Drops(("fiber", 1, 1, 0.6f), ("cinder", 1, 1, 0.1f)),
                    Pal(Hex("#e8c070"), Hex("#fff0d0"), Hex("#3a3a48"), Hex("#6af0ff")), Pal(Hex("#6a6a7a"), Hex("#c8c8d8"), Hex("#f0f0f0"), Hex("#ff6af0")));
                new Plan()
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0, 0.28f, 0), V(0.34f, 0.36f, 0.42f))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Secondary, V(0, 0.24f, 0.08f), V(0.26f, 0.26f, 0.3f))
                    .Mirror(PartShape.Capsule, BodyGroup.Body, ColorSlot.Secondary, V(0.1f, 0.12f, -0.1f), V(0.08f, 0.24f, 0.1f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0, 0.44f, 0.22f), V(0.3f, 0.28f, 0.28f))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0.14f, 0.64f, 0.18f), V(0.12f, 0.26f, 0.05f), V(0, 0, -25))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Glow, V(0.14f, 0.64f, 0.2f), V(0.07f, 0.18f, 0.02f), V(0, 0, -25))
                    .Eyes(BodyGroup.Head, V(0.08f, 0.48f, 0.33f), 0.07f)
                    .Add(PartShape.Cylinder, BodyGroup.Tail, ColorSlot.Accent, V(0, 0.3f, -0.46f), V(0.03f, 0.6f, 0.03f), V(-80, 0, 0))
                    .Add(PartShape.Sphere, BodyGroup.Tail, ColorSlot.Glow, V(0, 0.34f, -0.77f), V(0.14f, 0.14f, 0.14f))
                    .Pivot(BodyGroup.Head, V(0, 0.36f, 0.14f)).Pivot(BodyGroup.Tail, V(0, 0.28f, -0.18f))
                    .ApplyTo(d);
            }

            // ---------------------------------------------------------------- 025 KILNBACK
            {
                var d = Kin(b, 25, "kilnback", "Kilnback", Element.Ember, Element.Normal, Rarity.Uncommon, B, Temperament.Territorial, Locomotion.Quadruped,
                    72, 13, 16, 1.7f, 4.8f, 0.95f,
                    "An armadillo with chimneys on its back. It rolls into a ball and bakes clay in its own shell.",
                    "Grumpy, dependable, snores smoke rings.", 1.6f, 1, 2, A("ember_spit", "roll_crush"), A("steam_blast"),
                    Work((WorkType.Kindling, 2), (WorkType.Crafting, 1), (WorkType.Quarrying, 1)), CompanionPerk.Warmth, "Companion: warmth aura (slower hunger) + light.",
                    Drops(("cinder", 1, 2, 0.9f), ("stone", 1, 2, 0.6f)),
                    Pal(Hex("#8a5a48"), Hex("#c89078"), Hex("#4a4040"), Hex("#ffa030")), Pal(Hex("#d8d8e0"), Hex("#f0f0f8"), Hex("#6a6a7a"), Hex("#6af0ff")));
                new Plan()
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0, 0.42f, 0), V(0.6f, 0.55f, 0.8f))
                    .Add(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, V(0, 0.44f, 0.2f), V(0.62f, 0.05f, 0.56f), V(90, 0, 0))
                    .Add(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, V(0, 0.46f, 0f), V(0.64f, 0.05f, 0.58f), V(90, 0, 0))
                    .Add(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Secondary, V(0, 0.44f, -0.2f), V(0.62f, 0.05f, 0.56f), V(90, 0, 0))
                    .Add(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Accent, V(0.12f, 0.74f, -0.1f), V(0.1f, 0.22f, 0.1f))
                    .Add(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Accent, V(-0.12f, 0.72f, 0.1f), V(0.1f, 0.2f, 0.1f))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, V(0.12f, 0.86f, -0.1f), V(0.1f, 0.06f, 0.1f))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, V(-0.12f, 0.83f, 0.1f), V(0.1f, 0.06f, 0.1f))
                    .Add(PartShape.Cone, BodyGroup.Head, ColorSlot.Secondary, V(0, 0.32f, 0.56f), V(0.22f, 0.3f, 0.2f), V(90, 0, 0))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Secondary, V(0.1f, 0.45f, 0.45f), V(0.07f, 0.07f, 0.04f))
                    .Eyes(BodyGroup.Head, V(0.07f, 0.38f, 0.52f), 0.045f)
                    .Add(PartShape.Cone, BodyGroup.Tail, ColorSlot.Primary, V(0, 0.3f, -0.56f), V(0.1f, 0.36f, 0.1f), V(-110, 0, 0))
                    .Pivot(BodyGroup.Head, V(0, 0.35f, 0.38f)).Pivot(BodyGroup.Tail, V(0, 0.34f, -0.4f))
                    .Legs4(ColorSlot.Secondary, 0.2f, 0.26f, -0.26f, 0.2f, 0.1f)
                    .ApplyTo(d);
            }

            // ---------------------------------------------------------------- 026 MIRAGEON
            {
                var d = Kin(b, 26, "mirageon", "Mirageon", Element.Lumen, Element.Gale, Rarity.Rare, B, Temperament.Timid, Locomotion.Flyer,
                    80, 17, 13, 2.6f, 7f, 1.2f,
                    "A manta that swims through heat shimmer instead of water. Most travellers who see one think it's a mirage.",
                    "Elusive, graceful, drawn to bright light.", 0.7f, 1, 2, A("glint_beam", "cyclone"), A("solar_flare"),
                    Work((WorkType.Charging, 2), (WorkType.Hauling, 2)), CompanionPerk.Glow, "Companion: soft light at night.",
                    Drops(("crystal", 1, 1, 0.5f), ("gel", 1, 2, 0.6f)),
                    Pal(Hex("#f0c890"), Hex("#fff4e0"), Hex("#e0906a"), Hex("#fff6a0")), Pal(Hex("#6a4ae0"), Hex("#d0c8ff"), Hex("#3a2a8a"), Hex("#6af0ff")));
                new Plan()
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0, 1.0f, 0), V(0.7f, 0.2f, 0.85f))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Secondary, V(0, 0.96f, 0), V(0.56f, 0.12f, 0.72f))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, V(0.12f, 1.09f, -0.05f), V(0.1f, 0.03f, 0.1f))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Glow, V(-0.12f, 1.09f, 0.08f), V(0.08f, 0.03f, 0.08f))
                    .Mirror(PartShape.Sphere, BodyGroup.WingL, BodyGroup.WingR, ColorSlot.Accent, V(0.55f, 1.0f, -0.05f), V(0.62f, 0.07f, 0.46f), V(0, 0, -5))
                    .Mirror(PartShape.Cone, BodyGroup.Head, ColorSlot.Glow, V(0.15f, 1.02f, 0.44f), V(0.06f, 0.18f, 0.06f), V(80, 0, 0))
                    .Eyes(BodyGroup.Head, V(0.2f, 1.05f, 0.3f), 0.05f)
                    .Add(PartShape.Cylinder, BodyGroup.Tail, ColorSlot.Accent, V(0, 1.0f, -0.82f), V(0.03f, 0.8f, 0.03f), V(-90, 0, 0))
                    .Add(PartShape.Crystal, BodyGroup.Tail, ColorSlot.Glow, V(0, 1.0f, -1.22f), V(0.08f, 0.16f, 0.08f), V(-90, 0, 0))
                    .Pivot(BodyGroup.Head, V(0, 1.0f, 0.3f)).PivotPair(BodyGroup.WingL, BodyGroup.WingR, V(0.3f, 1.0f, 0)).Pivot(BodyGroup.Tail, V(0, 1.0f, -0.42f))
                    .ApplyTo(d);
            }

            // ---------------------------------------------------------------- 027 ASHCROW
            {
                var d = Kin(b, 27, "ashcrow", "Ashcrow", Element.Shade, Element.Ember, Rarity.Uncommon, B, Temperament.Aggressive, Locomotion.Biped,
                    52, 15, 9, 2.8f, 8f, 0.85f,
                    "A crow with ember eyes that nests in cooling lava. Flocks follow wildfires to pick through the ash.",
                    "Clever, spiteful, remembers faces for years.", 1.8f, 2, 3, A("hex_flicker", "ember_spit"), A("gust_slash"),
                    Work((WorkType.Hauling, 1), (WorkType.Foraging, 2)), CompanionPerk.OreSense, "Companion: +50% ore and cinder when mining.",
                    Drops(("pelt", 1, 1, 0.7f), ("cinder", 1, 1, 0.3f)),
                    Pal(Hex("#2a2530"), Hex("#3a3440"), Hex("#5a4a5a"), Hex("#ff6a2a")), Pal(Hex("#f0f0f4"), Hex("#d8d8e0"), Hex("#a0a0b0"), Hex("#6af0ff")));
                new Plan()
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0, 0.5f, 0), V(0.4f, 0.45f, 0.56f), V(-15, 0, 0))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0, 0.8f, 0.15f), V(0.28f, 0.26f, 0.28f))
                    .Add(PartShape.Cone, BodyGroup.Head, ColorSlot.Dark, V(0, 0.78f, 0.36f), V(0.07f, 0.2f, 0.07f), V(95, 0, 0))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Glow, V(0.09f, 0.84f, 0.25f), V(0.06f, 0.06f, 0.04f))
                    .Add(PartShape.Cone, BodyGroup.Head, ColorSlot.Glow, V(0, 0.96f, 0.1f), V(0.06f, 0.16f, 0.06f), V(-25, 0, 0))
                    .Mirror(PartShape.Sphere, BodyGroup.WingL, BodyGroup.WingR, ColorSlot.Secondary, V(0.24f, 0.52f, -0.05f), V(0.1f, 0.3f, 0.46f))
                    .Mirror(PartShape.Box, BodyGroup.WingL, BodyGroup.WingR, ColorSlot.Glow, V(0.26f, 0.4f, -0.24f), V(0.04f, 0.1f, 0.14f))
                    .Add(PartShape.Box, BodyGroup.Tail, ColorSlot.Primary, V(0, 0.42f, -0.36f), V(0.2f, 0.04f, 0.3f), V(-20, 0, 0))
                    .Pivot(BodyGroup.Head, V(0, 0.72f, 0.1f)).PivotPair(BodyGroup.WingL, BodyGroup.WingR, V(0.18f, 0.62f, 0)).Pivot(BodyGroup.Tail, V(0, 0.45f, -0.25f))
                    .Legs2(ColorSlot.Dark, 0.08f, 0.28f, 0.05f, ColorSlot.Dark)
                    .ApplyTo(d);
            }

            // ---------------------------------------------------------------- 028 CACTOAD
            {
                var d = Kin(b, 28, "cactoad", "Cactoad", Element.Flora, Element.Stone, Rarity.Common, B, Temperament.Territorial, Locomotion.Hopper,
                    60, 12, 13, 1.8f, 4.5f, 0.8f,
                    "A toad disguised as a cactus. It stores a whole rain season's water in its belly and blooms once a year.",
                    "Stubborn, patient, blooms when happy.", 2.4f, 1, 3, A("needle_volley", "tackle"), A("mud_shot"),
                    Work((WorkType.Irrigating, 1), (WorkType.Cultivating, 1), (WorkType.Foraging, 1)), CompanionPerk.ForageBoost, "Companion: +50% berries and fiber.",
                    Drops(("fiber", 1, 2, 1f), ("gel", 1, 1, 0.3f)),
                    Pal(Hex("#5aa06a"), Hex("#e0d8a0"), Hex("#3a7a4a"), Hex("#ff6aa0")), Pal(Hex("#b07ae0"), Hex("#f0e0ff"), Hex("#7a4ab0"), Hex("#ffe04a")));
                new Plan()
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0, 0.3f, 0), V(0.56f, 0.46f, 0.56f))
                    .Mirror(PartShape.Cone, BodyGroup.Body, ColorSlot.White, V(0.24f, 0.42f, 0.05f), V(0.03f, 0.1f, 0.03f), V(0, 0, -60))
                    .Mirror(PartShape.Cone, BodyGroup.Body, ColorSlot.White, V(0.18f, 0.48f, -0.16f), V(0.03f, 0.1f, 0.03f), V(-40, 0, -40))
                    .Add(PartShape.Cone, BodyGroup.Body, ColorSlot.White, V(0, 0.52f, -0.2f), V(0.03f, 0.1f, 0.03f), V(-50, 0, 0))
                    .Mirror(PartShape.Sphere, BodyGroup.Body, ColorSlot.Secondary, V(0.2f, 0.08f, 0.16f), V(0.14f, 0.08f, 0.18f))
                    .Mirror(PartShape.Sphere, BodyGroup.Body, ColorSlot.Accent, V(0.22f, 0.1f, -0.14f), V(0.16f, 0.12f, 0.24f))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0.14f, 0.5f, 0.2f), V(0.14f, 0.14f, 0.14f))
                    .Eyes(BodyGroup.Head, V(0.14f, 0.52f, 0.25f), 0.065f)
                    .Add(PartShape.Box, BodyGroup.Head, ColorSlot.Dark, V(0, 0.3f, 0.28f), V(0.3f, 0.02f, 0.02f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Glow, V(0, 0.62f, 0.02f), V(0.16f, 0.1f, 0.16f))
                    .Pivot(BodyGroup.Head, V(0, 0.35f, 0.1f))
                    .ApplyTo(d);
            }

            // ---------------------------------------------------------------- 029 RUSTMAW (mount)
            {
                var d = Kin(b, 29, "rustmaw", "Rustmaw", Element.Stone, Element.Volt, Rarity.Rare, B, Temperament.Aggressive, Locomotion.Quadruped,
                    120, 19, 19, 2.3f, 7f, 1.25f,
                    "A boar with iron-scale hide that roots through ore veins for fun. Its tusks spark when it charges.",
                    "Reckless, brave, adores being scratched behind the ears.", 0.6f, 1, 1, A("roll_crush", "spark_peck"), A("thunder_chirp"),
                    Work((WorkType.Quarrying, 3), (WorkType.Charging, 1), (WorkType.Hauling, 1)), CompanionPerk.OreSense, "Companion: +50% ore and cinder when mining. Camp: master quarrier.",
                    Drops(("ore", 2, 4, 1f), ("pelt", 1, 2, 0.5f)),
                    Pal(Hex("#9a5a3a"), Hex("#6a6a74"), Hex("#c0c0cc"), Hex("#6af0ff")), Pal(Hex("#2a2a32"), Hex("#b09040"), Hex("#f0d060"), Hex("#ff6a3a")));
                Mount(d, 12f, 8f, V(0, 1.14f, -0.02f));
                new Plan()
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0, 0.62f, 0), V(0.76f, 0.7f, 1.12f))
                    .Mirror(PartShape.Rock, BodyGroup.Body, ColorSlot.Secondary, V(0.28f, 0.82f, 0f), V(0.3f, 0.3f, 0.62f))
                    .Add(PartShape.Cone, BodyGroup.Body, ColorSlot.Accent, V(0, 1.0f, 0.24f), V(0.1f, 0.26f, 0.1f))
                    .Add(PartShape.Cone, BodyGroup.Body, ColorSlot.Accent, V(0, 1.02f, -0.02f), V(0.1f, 0.28f, 0.1f))
                    .Add(PartShape.Cone, BodyGroup.Body, ColorSlot.Accent, V(0, 0.98f, -0.3f), V(0.1f, 0.24f, 0.1f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0, 0.62f, 0.72f), V(0.5f, 0.45f, 0.5f))
                    .Add(PartShape.Cylinder, BodyGroup.Head, ColorSlot.Secondary, V(0, 0.55f, 0.97f), V(0.22f, 0.12f, 0.22f), V(90, 0, 0))
                    .Mirror(PartShape.Cone, BodyGroup.Head, ColorSlot.White, V(0.14f, 0.56f, 0.93f), V(0.05f, 0.22f, 0.05f), V(-30, 0, -20))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Glow, V(0.14f, 0.72f, 0.9f), V(0.05f, 0.05f, 0.04f))
                    .Mirror(PartShape.Cone, BodyGroup.Head, ColorSlot.Primary, V(0.2f, 0.86f, 0.62f), V(0.1f, 0.16f, 0.05f), V(0, 0, -30))
                    .Add(PartShape.Cylinder, BodyGroup.Tail, ColorSlot.Primary, V(0, 0.72f, -0.6f), V(0.04f, 0.22f, 0.04f), V(-45, 0, 0))
                    .Add(PartShape.Sphere, BodyGroup.Tail, ColorSlot.Glow, V(0, 0.78f, -0.7f), V(0.07f, 0.07f, 0.07f))
                    .Pivot(BodyGroup.Head, V(0, 0.62f, 0.5f)).Pivot(BodyGroup.Tail, V(0, 0.7f, -0.54f))
                    .Legs4(ColorSlot.Secondary, 0.26f, 0.35f, -0.35f, 0.36f, 0.16f, ColorSlot.Dark)
                    .ApplyTo(d);
            }

            // ---------------------------------------------------------------- 030 SOLARION (Epic mount)
            {
                var d = Kin(b, 30, "solarion", "Solarion", Element.Ember, Element.Lumen, Rarity.Epic, B, Temperament.Territorial, Locomotion.Quadruped,
                    150, 25, 19, 2.6f, 8.5f, 1.4f,
                    "A lion whose mane is a crown of sunlight. Where it roars at dawn, the badlands' cold night breaks instantly.",
                    "Majestic, proud, protective of the weak.", 0.12f, 1, 1, A("solar_flare", "flame_dash"), A("glint_beam"),
                    Work((WorkType.Kindling, 4), (WorkType.Charging, 2)), CompanionPerk.Warmth, "Companion: warmth aura + light. Camp: blazing smelter-master.",
                    Drops(("cinder", 3, 5, 1f), ("crystal", 1, 1, 0.8f)),
                    Pal(Hex("#e8b050"), Hex("#f8e0a8"), Hex("#f07a2a"), Hex("#fff0a0")), Pal(Hex("#e8e8f8"), Hex("#ffffff"), Hex("#6a8aff"), Hex("#aef0ff")));
                Mount(d, 14.5f, 10f, V(0, 1.12f, -0.08f));
                var p = new Plan()
                    .Add(PartShape.Capsule, BodyGroup.Body, ColorSlot.Primary, V(0, 0.8f, 0), V(0.5f, 1.12f, 0.5f), V(90, 0, 0))
                    .Add(PartShape.Capsule, BodyGroup.Body, ColorSlot.Secondary, V(0, 0.72f, 0.04f), V(0.4f, 0.9f, 0.36f), V(90, 0, 0))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Accent, V(0, 1.12f, 0.58f), V(0.56f, 0.56f, 0.36f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0, 1.1f, 0.72f), V(0.32f, 0.3f, 0.3f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Secondary, V(0, 1.03f, 0.86f), V(0.16f, 0.12f, 0.12f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Dark, V(0, 1.06f, 0.92f), V(0.06f, 0.04f, 0.04f))
                    .Eyes(BodyGroup.Head, V(0.09f, 1.15f, 0.84f), 0.05f)
                    .Add(PartShape.Cylinder, BodyGroup.Tail, ColorSlot.Primary, V(0, 0.95f, -0.78f), V(0.05f, 0.62f, 0.05f), V(-60, 0, 0))
                    .Add(PartShape.Cone, BodyGroup.Tail, ColorSlot.Glow, V(0, 1.12f, -1.05f), V(0.12f, 0.24f, 0.12f), V(-40, 0, 0))
                    .Pivot(BodyGroup.Head, V(0, 0.95f, 0.5f)).Pivot(BodyGroup.Tail, V(0, 0.85f, -0.55f))
                    .Legs4(ColorSlot.Primary, 0.18f, 0.38f, -0.38f, 0.6f, 0.13f, ColorSlot.Secondary);
                for (int i = 0; i < 8; i++)
                {
                    float a = i * 45f;
                    float r = a * Mathf.Deg2Rad;
                    p.Add(PartShape.Cone, BodyGroup.Head, ColorSlot.Glow, V(Mathf.Cos(r) * 0.3f, 1.12f + Mathf.Sin(r) * 0.3f, 0.54f), V(0.12f, 0.3f, 0.1f), V(0, 0, a - 90f));
                }
                p.ApplyTo(d);
            }

            // ---------------------------------------------------------------- 031 SANDSNARE (night serpent)
            {
                var d = Kin(b, 31, "sandsnare", "Sandsnare", Element.Stone, Element.Shade, Rarity.Uncommon, B, Temperament.Aggressive, Locomotion.Slither,
                    70, 16, 11, 2.2f, 6.5f, 1f,
                    "A hooded sand serpent that hunts by moonlight, lying buried with only its violet eyes above the dunes.",
                    "Patient, sly, hypnotic stare.", 1.6f, 1, 1, A("coil_strike", "mud_shot"), A("hex_flicker"),
                    Work((WorkType.Quarrying, 2), (WorkType.Hauling, 1)), CompanionPerk.StoneBoost, "Companion: +50% stone and ore from rocks.",
                    Drops(("stone", 1, 2, 0.8f), ("pelt", 1, 1, 0.5f)),
                    Pal(Hex("#c8a070"), Hex("#4a3a3a"), Hex("#8a6a4a"), Hex("#b070ff")), Pal(Hex("#e0e0e8"), Hex("#3a3a4a"), Hex("#b0b0c0"), Hex("#ff4a4a")));
                d.spawnTime = SpawnTime.Night;
                new Plan()
                    .Serpent(ColorSlot.Primary, ColorSlot.Secondary, 0.2f, 0.34f, 7, 0.26f)
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0, 0.42f, 0.3f), V(0.28f, 0.24f, 0.34f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Accent, V(0, 0.46f, 0.2f), V(0.52f, 0.36f, 0.08f))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Glow, V(0.09f, 0.48f, 0.44f), V(0.06f, 0.05f, 0.04f))
                    .Add(PartShape.Cone, BodyGroup.Head, ColorSlot.Dark, V(0, 0.36f, 0.5f), V(0.02f, 0.1f, 0.02f), V(90, 0, 0))
                    .Pivot(BodyGroup.Head, V(0, 0.28f, 0.12f))
                    .ApplyTo(d);
            }

            // ---------------------------------------------------------------- 032 STEAMKIT
            {
                var d = Kin(b, 32, "steamkit", "Steamkit", Element.Tide, Element.Ember, Rarity.Uncommon, B, Temperament.Curious, Locomotion.Quadruped,
                    62, 12, 11, 2f, 5.5f, 0.85f,
                    "A seal pup born in the badlands' hot springs. It keeps a little geyser under its cap and puffs steam when excited.",
                    "Bubbly, clumsy, always warm to hug.", 1.5f, 1, 2, A("steam_blast", "bubble_burst"), A("ember_spit"),
                    Work((WorkType.Irrigating, 2), (WorkType.Kindling, 1)), CompanionPerk.Mending, "Companion: slowly restores your HP.",
                    Drops(("gel", 1, 2, 0.9f), ("cinder", 1, 1, 0.3f)),
                    Pal(Hex("#7ab0d8"), Hex("#e8f4ff"), Hex("#d87a4a"), Hex("#ffb070")), Pal(Hex("#ffb0c8"), Hex("#fff0f4"), Hex("#6a6ae0"), Hex("#ffffff")));
                new Plan()
                    .Add(PartShape.Capsule, BodyGroup.Body, ColorSlot.Primary, V(0, 0.3f, 0), V(0.42f, 0.82f, 0.38f), V(90, 0, 0))
                    .Add(PartShape.Capsule, BodyGroup.Body, ColorSlot.Secondary, V(0, 0.24f, 0.08f), V(0.3f, 0.6f, 0.26f), V(90, 0, 0))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0, 0.46f, 0.42f), V(0.34f, 0.3f, 0.32f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Secondary, V(0, 0.42f, 0.56f), V(0.16f, 0.12f, 0.12f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Dark, V(0, 0.45f, 0.62f), V(0.05f, 0.04f, 0.04f))
                    .Eyes(BodyGroup.Head, V(0.09f, 0.5f, 0.54f), 0.07f)
                    .Add(PartShape.Cylinder, BodyGroup.Head, ColorSlot.Accent, V(0, 0.62f, 0.38f), V(0.12f, 0.1f, 0.12f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.White, V(0, 0.74f, 0.38f), V(0.18f, 0.12f, 0.18f))
                    .Mirror(PartShape.Sphere, BodyGroup.LegFL, BodyGroup.LegFR, ColorSlot.Secondary, V(0.2f, 0.07f, 0.2f), V(0.18f, 0.05f, 0.12f), V(0, -20, -20))
                    .Add(PartShape.Box, BodyGroup.Tail, ColorSlot.Secondary, V(0, 0.16f, -0.5f), V(0.32f, 0.04f, 0.14f))
                    .Pivot(BodyGroup.Head, V(0, 0.36f, 0.3f)).Pivot(BodyGroup.Tail, V(0, 0.2f, -0.38f))
                    .PivotPair(BodyGroup.LegFL, BodyGroup.LegFR, V(0.16f, 0.14f, 0.2f))
                    .ApplyTo(d);
            }
        }
    }
}
