using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Wardens' own moves (2026-10-19, user: "the wardens have the same fight moves? summon, giant aoe circles, a super
    /// dash"). A Warden used to fight with its species' two moves plus the strongest shared moves of its elements, so
    /// Wave Crash, Solar Flare, Cyclone, Coil Strike and Venom Spit turned up on two or three of them and most had a dash.
    /// Now the Warden that wakes at a shrine knows four moves that belong to it alone (`WardenMoves`, given to its
    /// `CreatureData.abilities` in `Wardens.Challenge`), and only Solarion, Djinnado and Stormcrest dash. Ids start with
    /// `warden_`, which keeps them out of the shared pool (`ContentDB.IsFormSkill`). Wind-ups are longer than a kin's so
    /// the big blows can be read and rolled. Its signature (`Wardens.Signature.cs`) is the arena mechanic on top.
    /// </summary>
    public static partial class ContentSeed
    {
        /// <summary>The four moves each Warden fights with, in learn order (a Lv 13 Warden knows the first three).</summary>
        public static readonly Dictionary<string, string[]> WardenMoves = new Dictionary<string, string[]>
        {
            { "bloomwyrm",  new[] { "warden_thornwhip", "warden_pollen_nova", "warden_dawn_ray", "warden_grove_slam" } },
            { "reefwarden", new[] { "warden_coral_jaw", "warden_reef_breaker", "warden_brine_shot", "warden_tidal_slam" } },
            { "solarion",   new[] { "warden_sunfire_horn", "warden_corona", "warden_flare_bolt", "warden_blazing_charge" } },
            { "hydrabog",   new[] { "warden_triple_bite", "warden_bog_wave", "warden_venom_volley", "warden_drowning_grip" } },
            { "krakolos",   new[] { "warden_tentacle_lash", "warden_maelstrom", "warden_ink_jet", "warden_ink_cloud" } },
            { "aurorox",    new[] { "warden_glacier_horn", "warden_aurora_wave", "warden_rime_breath", "warden_polar_stomp" } },
            { "djinnado",   new[] { "warden_scimitar_gust", "warden_sirocco", "warden_wind_blade", "warden_dust_devil" } },
            { "venomarch",  new[] { "warden_crown_fang", "warden_blight_burst", "warden_plague_spit", "warden_rockfall_tail" } },
            { "astralisk",  new[] { "warden_star_fang", "warden_supernova", "warden_comet_shard", "warden_void_coil" } },
            { "stormcrest", new[] { "warden_thunder_talon", "warden_tempest", "warden_bolt_strike", "warden_skyfall_dive" } },
        };

        /// <summary>One Warden move: like `Ab`, with a longer wind-up so a big blow can be seen coming.</summary>
        static void Wm(Bundle b, string id, string name, Element e, AbilityKind k, float power, float range, float cd,
            StatusType st = StatusType.None, float stChance = 0f, float radius = 3f, float projSpeed = 16f)
        {
            Ab(b, id, name, e, k, power, range, cd, st, stChance, radius, projSpeed);
            var a = b.abilities[b.abilities.Count - 1];
            a.windup = k == AbilityKind.Melee ? 0.45f : k == AbilityKind.Dash ? 0.65f : k == AbilityKind.Burst ? 0.7f : 0.55f;
        }

        static void BuildWardenSkills(Bundle b)
        {
            // Bloomwyrm (Flora / Lumen) - the grove
            Wm(b, "warden_thornwhip", "Thornwhip Lash", Element.Flora, AbilityKind.Melee, 28, 3.2f, 3.5f, StatusType.Drowsy, 0.2f);
            Wm(b, "warden_pollen_nova", "Pollen Nova", Element.Flora, AbilityKind.Burst, 26, 5f, 8f, StatusType.Drowsy, 0.35f, 6f);
            Wm(b, "warden_dawn_ray", "Dawn Ray", Element.Lumen, AbilityKind.Projectile, 24, 14f, 5f, StatusType.None, 0f, 3f, 20f);
            Wm(b, "warden_grove_slam", "Grove Slam", Element.Flora, AbilityKind.Burst, 30, 4f, 10f, StatusType.None, 0f, 5f);
            // Reefwarden (Tide / Stone) - the coral throne
            Wm(b, "warden_coral_jaw", "Coral Jaw", Element.Stone, AbilityKind.Melee, 30, 3f, 3.5f);
            Wm(b, "warden_reef_breaker", "Reef Breaker", Element.Tide, AbilityKind.Burst, 26, 5f, 8f, StatusType.Soaked, 0.4f, 6f);
            Wm(b, "warden_brine_shot", "Brine Shot", Element.Tide, AbilityKind.Projectile, 22, 13f, 4.5f, StatusType.Soaked, 0.3f, 3f, 18f);
            Wm(b, "warden_tidal_slam", "Tidal Slam", Element.Tide, AbilityKind.Melee, 30, 3.2f, 6f, StatusType.Soaked, 0.3f);
            // Solarion (Ember / Lumen) - the caldera lion: the charge is its own
            Wm(b, "warden_sunfire_horn", "Sunfire Horn", Element.Ember, AbilityKind.Melee, 30, 3f, 3.5f, StatusType.Burn, 0.3f);
            Wm(b, "warden_corona", "Corona", Element.Lumen, AbilityKind.Burst, 26, 5f, 8f, StatusType.Shock, 0.25f, 6f);
            Wm(b, "warden_flare_bolt", "Flare Bolt", Element.Ember, AbilityKind.Projectile, 24, 14f, 4.5f, StatusType.Burn, 0.3f, 3f, 20f);
            Wm(b, "warden_blazing_charge", "Blazing Charge", Element.Ember, AbilityKind.Dash, 30, 9f, 9f, StatusType.Burn, 0.35f, 9f);
            // Hydrabog (Tide / Shade) - three heads
            Wm(b, "warden_triple_bite", "Triple Bite", Element.Shade, AbilityKind.Melee, 30, 3.2f, 3.5f, StatusType.Poison, 0.35f);
            Wm(b, "warden_bog_wave", "Bog Wave", Element.Tide, AbilityKind.Burst, 26, 5f, 8f, StatusType.Soaked, 0.35f, 6f);
            Wm(b, "warden_venom_volley", "Venom Volley", Element.Shade, AbilityKind.Projectile, 22, 13f, 4.5f, StatusType.Poison, 0.4f, 3f, 17f);
            Wm(b, "warden_drowning_grip", "Drowning Grip", Element.Tide, AbilityKind.Melee, 30, 3.2f, 6.5f, StatusType.Soaked, 0.4f);
            // the Kraken (Tide / Shade)
            Wm(b, "warden_tentacle_lash", "Tentacle Lash", Element.Tide, AbilityKind.Melee, 30, 4f, 3.5f);
            Wm(b, "warden_maelstrom", "Maelstrom", Element.Tide, AbilityKind.Burst, 28, 6f, 9f, StatusType.Soaked, 0.4f, 7f);
            Wm(b, "warden_ink_jet", "Ink Jet", Element.Shade, AbilityKind.Projectile, 24, 14f, 4.5f, StatusType.Drowsy, 0.2f, 3f, 18f);
            Wm(b, "warden_ink_cloud", "Ink Cloud", Element.Shade, AbilityKind.Burst, 20, 4f, 10f, StatusType.Drowsy, 0.5f, 5f);
            // Aurorox (Lumen / Frost) - the glacier
            Wm(b, "warden_glacier_horn", "Glacier Horn", Element.Frost, AbilityKind.Melee, 30, 3.2f, 3.5f, StatusType.Chill, 0.3f);
            Wm(b, "warden_aurora_wave", "Aurora Wave", Element.Lumen, AbilityKind.Burst, 26, 5f, 8f, StatusType.None, 0f, 6.5f);
            Wm(b, "warden_rime_breath", "Rime Breath", Element.Frost, AbilityKind.Projectile, 24, 13f, 4.5f, StatusType.Chill, 0.4f, 3f, 17f);
            Wm(b, "warden_polar_stomp", "Polar Stomp", Element.Frost, AbilityKind.Burst, 30, 4f, 10f, StatusType.Chill, 0.45f, 5f);
            // Djinnado (Gale / Ember) - the sun temple: a whirlwind that dashes
            Wm(b, "warden_scimitar_gust", "Scimitar Gust", Element.Gale, AbilityKind.Melee, 30, 3.2f, 3.5f);
            Wm(b, "warden_sirocco", "Sirocco", Element.Ember, AbilityKind.Burst, 26, 5f, 8f, StatusType.Burn, 0.35f, 6f);
            Wm(b, "warden_wind_blade", "Wind Blade", Element.Gale, AbilityKind.Projectile, 24, 15f, 4.5f, StatusType.None, 0f, 3f, 22f);
            Wm(b, "warden_dust_devil", "Dust Devil", Element.Gale, AbilityKind.Dash, 30, 9f, 9f, StatusType.None, 0f, 9f);
            // Venomarch (Shade / Stone) - the blight crater
            Wm(b, "warden_crown_fang", "Crown Fang", Element.Shade, AbilityKind.Melee, 32, 3.2f, 3.5f, StatusType.Poison, 0.4f);
            Wm(b, "warden_blight_burst", "Blight Burst", Element.Shade, AbilityKind.Burst, 28, 5f, 8f, StatusType.Poison, 0.4f, 6f);
            Wm(b, "warden_plague_spit", "Plague Spit", Element.Shade, AbilityKind.Projectile, 24, 13f, 4.5f, StatusType.Poison, 0.45f, 3f, 16f);
            Wm(b, "warden_rockfall_tail", "Rockfall Tail", Element.Stone, AbilityKind.Melee, 30, 3.5f, 6.5f);
            // Astralisk (Lumen / Shade) - the star hollow
            Wm(b, "warden_star_fang", "Star Fang", Element.Lumen, AbilityKind.Melee, 32, 3.2f, 3.5f);
            Wm(b, "warden_supernova", "Supernova", Element.Lumen, AbilityKind.Burst, 30, 6f, 9f, StatusType.Shock, 0.25f, 7f);
            Wm(b, "warden_comet_shard", "Comet Shard", Element.Shade, AbilityKind.Projectile, 26, 15f, 4.5f, StatusType.None, 0f, 3f, 22f);
            Wm(b, "warden_void_coil", "Void Coil", Element.Shade, AbilityKind.Melee, 30, 3.5f, 6.5f, StatusType.Drowsy, 0.35f);
            // Stormcrest (Volt / Gale) - the finale: it dives from the sky
            Wm(b, "warden_thunder_talon", "Thunder Talon", Element.Volt, AbilityKind.Melee, 32, 3.2f, 3.5f, StatusType.Shock, 0.3f);
            Wm(b, "warden_tempest", "Tempest", Element.Gale, AbilityKind.Burst, 30, 6f, 9f, StatusType.None, 0f, 7f);
            Wm(b, "warden_bolt_strike", "Bolt Strike", Element.Volt, AbilityKind.Projectile, 28, 15f, 4.5f, StatusType.Shock, 0.35f, 3f, 24f);
            Wm(b, "warden_skyfall_dive", "Skyfall Dive", Element.Volt, AbilityKind.Dash, 32, 10f, 9f, StatusType.Shock, 0.35f, 10f);
        }

        public static bool IsWardenSkill(string id) => id != null && id.StartsWith("warden_");
    }
}
