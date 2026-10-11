using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Stage 2 roster (#011-#047): 37 more kin so the island has 50 (47 wild + 3 partners).
    /// Split per biome: RosterMeadow / RosterBadlands / RosterPeaks. Shared abilities + plan helpers live here.
    /// </summary>
    public static partial class ContentSeed
    {
        static void BuildRoster(Bundle b)
        {
            BuildRosterAbilities(b);
            BuildRosterMeadow(b);
            BuildRosterBadlands(b);
            BuildRosterPeaks(b);
        }

        static void BuildRosterAbilities(Bundle b)
        {
            Ab(b, "seed_gust", "Seed Gust", Element.Gale, AbilityKind.Projectile, 15, 12, 2.6f, StatusType.None, 0, 3, 17f);
            Ab(b, "root_ram", "Root Ram", Element.Flora, AbilityKind.Melee, 20, 1.9f, 3f);
            Ab(b, "aqua_tail", "Aqua Tail", Element.Tide, AbilityKind.Melee, 21, 2.1f, 3.2f, StatusType.Soaked, 0.3f);
            Ab(b, "static_dart", "Static Dart", Element.Volt, AbilityKind.Projectile, 15, 12, 2.4f, StatusType.Shock, 0.2f, 3, 20f);
            Ab(b, "needle_volley", "Needle Volley", Element.Flora, AbilityKind.Projectile, 18, 11, 3f, StatusType.None, 0, 3, 18f);
            Ab(b, "glint_beam", "Glint Beam", Element.Lumen, AbilityKind.Projectile, 19, 13, 3.2f, StatusType.Drowsy, 0.12f, 3, 22f);
            Ab(b, "pollen_burst", "Pollen Burst", Element.Flora, AbilityKind.Burst, 12, 4f, 6.5f, StatusType.Drowsy, 0.35f, 4f);
            Ab(b, "gust_slash", "Gust Slash", Element.Gale, AbilityKind.Melee, 21, 2.2f, 3f);
            Ab(b, "cyclone", "Cyclone", Element.Gale, AbilityKind.Burst, 23, 4.5f, 6.5f, StatusType.None, 0, 5f);
            Ab(b, "petal_storm", "Petal Storm", Element.Flora, AbilityKind.Burst, 30, 5f, 6f, StatusType.Drowsy, 0.25f, 5.5f);
            Ab(b, "tongue_lash", "Tongue Lash", Element.Tide, AbilityKind.Melee, 16, 3.2f, 2.4f, StatusType.Soaked, 0.2f);
            Ab(b, "mud_shot", "Mud Shot", Element.Stone, AbilityKind.Projectile, 16, 11, 2.8f, StatusType.Soaked, 0.2f, 3, 14f);
            Ab(b, "sand_kick", "Sand Kick", Element.Stone, AbilityKind.Melee, 19, 2.1f, 2.8f);
            Ab(b, "roll_crush", "Roll Crush", Element.Stone, AbilityKind.Dash, 25, 9, 5f, StatusType.None, 0, 7f);
            Ab(b, "solar_flare", "Solar Flare", Element.Lumen, AbilityKind.Burst, 32, 5f, 7f, StatusType.Burn, 0.3f, 5.5f);
            Ab(b, "coil_strike", "Coil Strike", Element.Shade, AbilityKind.Dash, 23, 8, 4.5f, StatusType.Drowsy, 0.15f, 6f);
            Ab(b, "steam_blast", "Steam Blast", Element.Tide, AbilityKind.Burst, 20, 4f, 6f, StatusType.Burn, 0.2f, 4f);
            Ab(b, "snowball", "Snowball", Element.Frost, AbilityKind.Projectile, 14, 11, 2.4f, StatusType.Chill, 0.25f, 3, 15f);
            Ab(b, "ice_fang", "Ice Fang", Element.Frost, AbilityKind.Melee, 25, 2.2f, 3.6f, StatusType.Chill, 0.3f);
            Ab(b, "aurora_veil", "Aurora Veil", Element.Lumen, AbilityKind.Burst, 19, 5f, 7f, StatusType.Drowsy, 0.3f, 5.5f);
        }

        /// <summary>Configures common species fields in one call.</summary>
        static SpeciesDef Kin(Bundle b, int dex, string id, string name, Element e1, Element e2, Rarity r, BiomeMask biomes,
            Temperament temp, Locomotion loco, int hp, int atk, int def, float walk, float run, float size,
            string lore, string personality, float weight, int minG, int maxG, string[] abil, string[] rare, WorkSkill[] work,
            CompanionPerk perk, string role, DropEntry[] drops, Palette pal, Palette shimmer)
        {
            var d = Sp(b, dex, id, name, e1, e2, r, biomes, temp, loco, hp, atk, def, walk, run, size);
            d.lore = lore; d.personality = personality; d.spawnWeight = weight; d.minGroup = minG; d.maxGroup = maxG;
            d.abilities = abil; d.rareAbilities = rare ?? new string[0]; d.work = work; d.perk = perk; d.overworldRole = role;
            d.drops = drops; d.palette = pal; d.shimmerPalette = shimmer;
            return d;
        }

        static void Mount(SpeciesDef d, float speed, float jump, Vector3 seat)
        {
            d.mountable = true; d.mountSpeed = speed; d.mountJump = jump; d.mountSeat = seat;
            d.overworldRole += " MOUNT: rideable with a Kin Harness.";
        }

        partial class Plan
        {
            /// <summary>Four cylinder legs (+ optional feet) with correct hip pivots.</summary>
            public Plan Legs4(ColorSlot c, float x, float zf, float zb, float len, float w, ColorSlot? foot = null)
            {
                Mirror(PartShape.Cylinder, BodyGroup.LegFL, BodyGroup.LegFR, c, V(x, len * 0.5f, zf), V(w, len, w));
                Mirror(PartShape.Cylinder, BodyGroup.LegBL, BodyGroup.LegBR, c, V(x, len * 0.5f, zb), V(w, len, w));
                if (foot.HasValue)
                {
                    Mirror(PartShape.Cylinder, BodyGroup.LegFL, BodyGroup.LegFR, foot.Value, V(x, 0.025f, zf), V(w * 1.15f, 0.05f, w * 1.15f));
                    Mirror(PartShape.Cylinder, BodyGroup.LegBL, BodyGroup.LegBR, foot.Value, V(x, 0.025f, zb), V(w * 1.15f, 0.05f, w * 1.15f));
                }
                PivotPair(BodyGroup.LegFL, BodyGroup.LegFR, V(x, len, zf));
                PivotPair(BodyGroup.LegBL, BodyGroup.LegBR, V(x, len, zb));
                return this;
            }

            /// <summary>Two biped legs (capsules) with hip pivots.</summary>
            public Plan Legs2(ColorSlot c, float x, float len, float w, ColorSlot? foot = null)
            {
                Mirror(PartShape.Capsule, BodyGroup.LegFL, BodyGroup.LegFR, c, V(x, len * 0.5f, 0), V(w, len, w));
                if (foot.HasValue) Mirror(PartShape.Sphere, BodyGroup.LegFL, BodyGroup.LegFR, foot.Value, V(x, 0.03f, 0.05f), V(w * 1.1f, 0.07f, w * 1.4f));
                PivotPair(BodyGroup.LegFL, BodyGroup.LegFR, V(x, len, 0));
                return this;
            }

            /// <summary>Segmented serpent body along -Z: first half in Body, rest in Tail.</summary>
            public Plan Serpent(ColorSlot c, ColorSlot band, float y, float r0, int segs, float spacing)
            {
                for (int i = 0; i < segs; i++)
                {
                    float r = Mathf.Lerp(r0, r0 * 0.4f, i / (float)(segs - 1));
                    var g = i < segs / 2 ? BodyGroup.Body : BodyGroup.Tail;
                    Add(PartShape.Sphere, g, i % 2 == 0 ? c : band, V(0, y - (r0 - r) * 0.4f, -i * spacing), V(r, r * 0.92f, r * 1.1f));
                }
                Pivot(BodyGroup.Tail, V(0, y, -(segs / 2) * spacing + spacing * 0.5f));
                return this;
            }
        }
    }
}
