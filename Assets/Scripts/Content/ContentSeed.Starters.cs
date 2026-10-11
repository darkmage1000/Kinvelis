using UnityEngine;

namespace Wildbond
{
    public static partial class ContentSeed
    {
        /// <summary>
        /// The three partner choices offered on a new game. All Normal type (no elemental weaknesses),
        /// never spawn wild, and each has a late-game niche via its perk + work skills:
        ///  - Knuckub  (Fighter): best combat stats, Vanguard perk boosts your melee.
        ///  - Chiskit  (Builder): elite camp worker (Logging/Crafting 3), Prospector boosts ALL gathering.
        ///  - Hopling  (Scout):   fastest kin, Tailwind speeds you up + saves you at low HP, Drowsy burst aids captures.
        /// Stat totals are within ~5% of each other so no pick is a trap.
        /// </summary>
        static void BuildStarters(Bundle b)
        {
            // ---- starter abilities (Normal) ----
            Ab(b, "pummel_rush", "Pummel Rush", Element.Normal, AbilityKind.Dash, 24, 9, 4.5f, StatusType.None, 0f, 7f);
            Ab(b, "brace_bash", "Brace Bash", Element.Normal, AbilityKind.Melee, 28, 2.1f, 4f);
            Ab(b, "tail_slap", "Tail Slap", Element.Normal, AbilityKind.Melee, 18, 1.9f, 2.4f);
            Ab(b, "chip_shot", "Chip Shot", Element.Normal, AbilityKind.Projectile, 17, 12, 3f, StatusType.None, 0, 3, 16f);
            Ab(b, "quick_kick", "Quick Kick", Element.Normal, AbilityKind.Melee, 15, 1.8f, 1.8f);
            Ab(b, "dazzle_hop", "Dazzle Hop", Element.Normal, AbilityKind.Burst, 12, 4.5f, 7f, StatusType.Drowsy, 0.35f, 4.5f);

            // ------------------------------------------------------------------ KNUCKUB (fighter)
            {
                var d = Sp(b, 101, "knuckub", "Knuckub", Element.Normal, Element.Normal, Rarity.Rare, BiomeMask.None,
                    Temperament.Curious, Locomotion.Biped, 88, 17, 14, 2.6f, 7.2f, 0.95f);
                d.starter = true; d.role = "Fighter"; d.spawnWeight = 0f;
                d.lore = "A bear cub that wraps its paws in woven reed and spars with anything that moves. It never backs down from a friend's fight.";
                d.personality = "Brave, hot-headed, fiercely loyal.";
                d.abilities = new[] { "pummel_rush", "brace_bash" };
                d.work = Work((WorkType.Hauling, 2), (WorkType.Logging, 1), (WorkType.Quarrying, 1));
                d.perk = CompanionPerk.Vanguard;
                d.overworldRole = "Vanguard: +15% melee damage for you. Hits 20% harder on the kin you're fighting.";
                d.palette = Pal(Hex("#a8683a"), Hex("#f1d9b0"), Hex("#d8443a"), Hex("#f7f1e3"));
                d.shimmerPalette = Pal(Hex("#3a3a44"), Hex("#c8c8d4"), Hex("#f0b020"), Hex("#ffffff"));
                var p = new Plan()
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0, 0.55f, 0), V(0.64f, 0.68f, 0.54f))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Secondary, V(0, 0.52f, 0.15f), V(0.44f, 0.48f, 0.3f))
                    .Add(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Accent, V(0, 0.84f, 0.02f), V(0.5f, 0.08f, 0.44f))
                    .Add(PartShape.Box, BodyGroup.Body, ColorSlot.Accent, V(0.12f, 0.78f, -0.24f), V(0.1f, 0.2f, 0.04f), V(0, 0, 25))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0, 1.06f, 0.06f), V(0.5f, 0.44f, 0.44f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Secondary, V(0, 1.0f, 0.27f), V(0.22f, 0.16f, 0.14f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Dark, V(0, 1.03f, 0.34f), V(0.07f, 0.06f, 0.05f))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0.18f, 1.27f, 0.02f), V(0.15f, 0.15f, 0.08f))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Secondary, V(0.18f, 1.27f, 0.05f), V(0.08f, 0.08f, 0.03f))
                    .Eyes(BodyGroup.Head, V(0.1f, 1.1f, 0.25f), 0.07f)
                    .Mirror(PartShape.Capsule, BodyGroup.ArmL, BodyGroup.ArmR, ColorSlot.Primary, V(0.34f, 0.56f, 0.04f), V(0.16f, 0.32f, 0.16f))
                    .Mirror(PartShape.Sphere, BodyGroup.ArmL, BodyGroup.ArmR, ColorSlot.Glow, V(0.36f, 0.38f, 0.08f), V(0.2f, 0.2f, 0.2f))
                    .Mirror(PartShape.Capsule, BodyGroup.LegFL, BodyGroup.LegFR, ColorSlot.Primary, V(0.14f, 0.16f, 0), V(0.18f, 0.3f, 0.18f))
                    .Mirror(PartShape.Sphere, BodyGroup.LegFL, BodyGroup.LegFR, ColorSlot.Secondary, V(0.14f, 0.03f, 0.05f), V(0.18f, 0.08f, 0.22f))
                    .Pivot(BodyGroup.Head, V(0, 0.86f, 0.04f))
                    .PivotPair(BodyGroup.ArmL, BodyGroup.ArmR, V(0.3f, 0.72f, 0))
                    .PivotPair(BodyGroup.LegFL, BodyGroup.LegFR, V(0.14f, 0.3f, 0));
                p.ApplyTo(d);
            }

            // ------------------------------------------------------------------ CHISKIT (builder / gatherer)
            {
                var d = Sp(b, 102, "chiskit", "Chiskit", Element.Normal, Element.Normal, Rarity.Rare, BiomeMask.None,
                    Temperament.Docile, Locomotion.Biped, 76, 13, 13, 2.5f, 6.2f, 0.95f);
                d.starter = true; d.role = "Builder"; d.spawnWeight = 0f;
                d.lore = "A tidy little beaver who wears a scrap-bark hard hat. It cannot see a loose log without stacking it into something useful.";
                d.personality = "Industrious, particular, hums while it works.";
                d.abilities = new[] { "tail_slap", "chip_shot" };
                d.work = Work((WorkType.Logging, 3), (WorkType.Crafting, 3), (WorkType.Quarrying, 2), (WorkType.Hauling, 1));
                d.perk = CompanionPerk.Prospector;
                d.overworldRole = "Prospector: +35% yield from every tree, rock and plant. Camp: master logger & crafter.";
                d.palette = Pal(Hex("#b07a4a"), Hex("#f2e2c4"), Hex("#f2b21e"), Hex("#fffaf0"));
                d.shimmerPalette = Pal(Hex("#e0e4ea"), Hex("#ffffff"), Hex("#4ac0ff"), Hex("#ffffff"));
                var p = new Plan()
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0, 0.45f, 0), V(0.56f, 0.6f, 0.5f))
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Secondary, V(0, 0.43f, 0.13f), V(0.4f, 0.44f, 0.3f))
                    .Add(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Accent, V(0, 0.32f, 0), V(0.58f, 0.07f, 0.52f))
                    .Add(PartShape.Box, BodyGroup.Body, ColorSlot.Dark, V(0.2f, 0.28f, 0.2f), V(0.08f, 0.12f, 0.06f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0, 0.88f, 0.06f), V(0.44f, 0.4f, 0.42f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Secondary, V(0, 0.82f, 0.22f), V(0.3f, 0.2f, 0.2f))
                    .Add(PartShape.Box, BodyGroup.Head, ColorSlot.Glow, V(0, 0.75f, 0.3f), V(0.1f, 0.1f, 0.03f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Dark, V(0, 0.87f, 0.31f), V(0.07f, 0.05f, 0.05f))
                    .Mirror(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0.18f, 1.02f, 0.0f), V(0.1f, 0.1f, 0.07f))
                    .Add(PartShape.Dome, BodyGroup.Head, ColorSlot.Accent, V(0, 1.0f, 0.05f), V(0.45f, 0.34f, 0.45f))
                    .Add(PartShape.Cylinder, BodyGroup.Head, ColorSlot.Accent, V(0, 1.0f, 0.1f), V(0.52f, 0.02f, 0.54f))
                    .Eyes(BodyGroup.Head, V(0.1f, 0.93f, 0.23f), 0.065f)
                    .Mirror(PartShape.Capsule, BodyGroup.ArmL, BodyGroup.ArmR, ColorSlot.Primary, V(0.3f, 0.46f, 0.06f), V(0.12f, 0.26f, 0.12f))
                    .Mirror(PartShape.Capsule, BodyGroup.LegFL, BodyGroup.LegFR, ColorSlot.Primary, V(0.12f, 0.12f, 0), V(0.15f, 0.24f, 0.15f))
                    .Mirror(PartShape.Sphere, BodyGroup.LegFL, BodyGroup.LegFR, ColorSlot.Dark, V(0.12f, 0.02f, 0.05f), V(0.16f, 0.06f, 0.2f))
                    .Add(PartShape.Box, BodyGroup.Tail, ColorSlot.Dark, V(0, 0.16f, -0.46f), V(0.32f, 0.06f, 0.46f), V(-12, 0, 0))
                    .Pivot(BodyGroup.Head, V(0, 0.72f, 0.03f))
                    .PivotPair(BodyGroup.ArmL, BodyGroup.ArmR, V(0.27f, 0.6f, 0))
                    .PivotPair(BodyGroup.LegFL, BodyGroup.LegFR, V(0.12f, 0.24f, 0))
                    .Pivot(BodyGroup.Tail, V(0, 0.28f, -0.22f));
                p.ApplyTo(d);
            }

            // ------------------------------------------------------------------ HOPLING (support / mobility)
            {
                var d = Sp(b, 103, "hopling", "Hopling", Element.Normal, Element.Normal, Rarity.Rare, BiomeMask.None,
                    Temperament.Timid, Locomotion.Hopper, 70, 13, 11, 3.4f, 9.5f, 0.9f);
                d.starter = true; d.role = "Scout"; d.spawnWeight = 0f;
                d.lore = "A long-eared hare whose scarf always streams as if in a breeze. Travellers say a Hopling can outrun a storm - and bring you home through it.";
                d.personality = "Cheerful, restless, protective of the weary.";
                d.abilities = new[] { "quick_kick", "dazzle_hop" };
                d.work = Work((WorkType.Hauling, 3), (WorkType.Foraging, 2), (WorkType.Cultivating, 1));
                d.perk = CompanionPerk.Tailwind;
                d.overworldRole = "Tailwind: +15% speed, -35% stamina use, and an emergency heal when you drop low. Dazzle Hop makes kin drowsy (easier captures).";
                d.palette = Pal(Hex("#ece8e2"), Hex("#ffb4c4"), Hex("#2cb8b0"), Hex("#fff2a8"));
                d.shimmerPalette = Pal(Hex("#ffd7a0"), Hex("#ff8a8a"), Hex("#8a5ad8"), Hex("#ffffff"));
                var p = new Plan()
                    .Add(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0, 0.34f, 0), V(0.44f, 0.44f, 0.52f))
                    .Add(PartShape.Cylinder, BodyGroup.Body, ColorSlot.Accent, V(0, 0.5f, 0.1f), V(0.34f, 0.08f, 0.34f))
                    .Add(PartShape.Box, BodyGroup.Body, ColorSlot.Accent, V(0.1f, 0.47f, -0.12f), V(0.08f, 0.04f, 0.32f), V(20, 20, 0))
                    .Mirror(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0.12f, 0.05f, 0.12f), V(0.12f, 0.08f, 0.22f))
                    .Mirror(PartShape.Sphere, BodyGroup.Body, ColorSlot.Primary, V(0.14f, 0.1f, -0.12f), V(0.14f, 0.14f, 0.26f))
                    .Add(PartShape.Sphere, BodyGroup.Tail, ColorSlot.White, V(0, 0.36f, -0.3f), V(0.17f, 0.17f, 0.17f))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Primary, V(0, 0.6f, 0.22f), V(0.36f, 0.34f, 0.34f))
                    .Mirror(PartShape.Capsule, BodyGroup.Head, ColorSlot.Primary, V(0.09f, 0.95f, 0.16f), V(0.1f, 0.44f, 0.06f), V(0, 0, -10))
                    .Mirror(PartShape.Capsule, BodyGroup.Head, ColorSlot.Secondary, V(0.09f, 0.95f, 0.185f), V(0.05f, 0.34f, 0.03f), V(0, 0, -10))
                    .Add(PartShape.Sphere, BodyGroup.Head, ColorSlot.Secondary, V(0, 0.58f, 0.39f), V(0.06f, 0.05f, 0.04f))
                    .Eyes(BodyGroup.Head, V(0.1f, 0.64f, 0.34f), 0.075f)
                    .Pivot(BodyGroup.Head, V(0, 0.5f, 0.14f))
                    .Pivot(BodyGroup.Tail, V(0, 0.36f, -0.24f));
                p.ApplyTo(d);
            }
        }
    }
}
