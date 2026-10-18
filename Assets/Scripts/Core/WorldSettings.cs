using System;
using UnityEngine;

namespace Wildbond
{
    /// <summary>Explorer = "Little Explorer": a world a young child can't lose in (see the kid rules below). New values go at the end.</summary>
    public enum WorldPreset { Casual, Normal, Hard, Custom, Explorer }

    /// <summary>
    /// Per-world rules chosen when a world is created (stored in its save). Everything gameplay-tunable
    /// that players may want to customise lives here; systems read it through WorldRules.Current.
    /// </summary>
    [Serializable]
    public class WorldSettings
    {
        public string worldName = "Rimewild Isle";
        public int seed = 7129;
        public WorldPreset preset = WorldPreset.Normal;

        [Header("Wildlife")]
        public float wildSpawnRate = 1f;      // population + spawn frequency
        public float captureRate = 1f;        // multiplies capture chance
        public float shimmerRate = 1f;        // multiplies rare-colour odds
        public float alphaRespawnMinutes = 30f;

        [Header("Progression")]
        public float expRate = 1f;            // player + kin XP
        public float gatherRate = 1f;         // resource yield
        public float resourceRespawnRate = 1f;// >1 = trees/rocks regrow faster

        [Header("Combat & survival")]
        public float wildDamage = 1f;         // damage wild kin deal to you & your kin
        public float playerDamage = 1f;       // damage you & your kin deal
        public float hungerRate = 1f;
        public float staminaDrain = 1f;
        public float dayLengthMinutes = 20f;

        [Header("Camp")]
        public int campWorkersBase = 3;       // Hearthstone lv1 worker cap
        public int campWorkersPerLevel = 3;   // + per Hearthstone upgrade
        public float workSpeed = 1f;
        public float raidFrequency = 1f;      // hostile kin raids on your camp (0 = never)

        [Header("Toggles")]
        public bool fastTravel = true;
        public bool offlineProgress = false;   // camp work while the game is closed: off by default since 2026-10-18 (user: "practically cheating")

        // Young players (user request 2026-10-10: a 5-year-old who likes adventure and action, a 7-year-old who loves
        // settings). Each is its own switch, read where it applies; the Explorer preset turns the kid ones on together.
        [Header("Young players")]
        public bool toyBox;           // Settings shows the TOY BOX: every rule can be changed while playing
        public bool simpleControls;   // fewer buttons, SEED throws in one tap, always running
        public bool cantFaint;        // you and your kin get back up instead of fainting
        public bool safeTravel;       // no fall damage, no drowning, swimming costs nothing
        public bool friendlyWild;     // wild kin never start a fight
        public bool levelMatched;     // newly spawned wild kin are never above your strongest kin (+1)
        public bool endlessSeeds;     // Tether Seeds never run out, and ranged weapons need no ammunition
        public bool freeBuild;        // camp structures cost nothing and have no level requirement
        public bool rideAnything;     // any kin big enough can be ridden, no harness
        public bool parade;           // the rest of the party walks behind you (Creatures/Parade.cs)
        public bool kidRoster;        // the three glamorous lady kin (Sirelle, Sekhara, Cindra) don't spawn wild
        [Header("Toys")]
        public float kinSize = 1f;    // every kin, yours and wild
        public float jumpPower = 1f;
        public float moveSpeed = 1f;

        public WorldSettings Clone() => (WorldSettings)MemberwiseClone();

        public static WorldSettings FromPreset(WorldPreset p)
        {
            var s = new WorldSettings { preset = p };
            switch (p)
            {
                case WorldPreset.Casual:
                    s.wildSpawnRate = 0.8f; s.captureRate = 1.5f; s.shimmerRate = 2f; s.expRate = 1.5f; s.gatherRate = 1.5f; s.resourceRespawnRate = 1.5f;
                    s.wildDamage = 0.6f; s.playerDamage = 1.3f; s.hungerRate = 0.5f; s.staminaDrain = 0.7f; s.dayLengthMinutes = 24f;
                    s.campWorkersBase = 6; s.campWorkersPerLevel = 4; s.workSpeed = 1.5f; s.alphaRespawnMinutes = 20f; s.raidFrequency = 0.5f;
                    break;
                case WorldPreset.Hard:
                    s.wildSpawnRate = 1.4f; s.captureRate = 0.7f; s.shimmerRate = 1f; s.expRate = 0.8f; s.gatherRate = 0.8f; s.resourceRespawnRate = 0.6f;
                    s.wildDamage = 1.5f; s.playerDamage = 0.8f; s.hungerRate = 1.5f; s.staminaDrain = 1.3f; s.dayLengthMinutes = 16f;
                    s.campWorkersBase = 2; s.campWorkersPerLevel = 2; s.workSpeed = 0.8f; s.alphaRespawnMinutes = 45f; s.raidFrequency = 1.6f;
                    break;
                case WorldPreset.Explorer:
                    // nothing to lose and plenty to find: fights still happen (and are winnable), but hunger, falls, the sea and
                    // empty pockets never stop the fun
                    s.simpleControls = s.cantFaint = s.safeTravel = s.levelMatched = s.endlessSeeds = true;
                    s.freeBuild = s.rideAnything = s.parade = s.kidRoster = true;
                    s.wildSpawnRate = 1.2f; s.captureRate = 3f; s.shimmerRate = 6f; s.expRate = 2f; s.gatherRate = 2f; s.resourceRespawnRate = 2f;
                    s.wildDamage = 0.4f; s.playerDamage = 1.6f; s.hungerRate = 0f; s.staminaDrain = 0f; s.dayLengthMinutes = 30f;
                    s.campWorkersBase = 8; s.campWorkersPerLevel = 4; s.workSpeed = 2f; s.alphaRespawnMinutes = 20f; s.raidFrequency = 0f;
                    break;
            }
            return s;
        }

        /// <summary>Human-readable summary line for save-slot cards.</summary>
        public string Summary() => $"{(preset == WorldPreset.Explorer ? "Little Explorer" : preset.ToString())}{(toyBox ? " + Toy Box" : "")} · spawns x{wildSpawnRate:0.#} · capture x{captureRate:0.#} · XP x{expRate:0.#}";
    }

    /// <summary>Rules of the world currently being played (defaults when none loaded, e.g. editor tests).</summary>
    public static class WorldRules
    {
        public static WorldSettings Current { get; set; } = new WorldSettings();
    }

    /// <summary>What the main menu asked GameManager to do when the world scene loads.</summary>
    public static class GameSession
    {
        public const int SlotCount = 3;
        /// <summary>Save slot to play (0..SlotCount-1).</summary>
        public static int Slot = 0;
        /// <summary>Non-null => create a brand-new world in Slot with these settings (overwrites the slot).</summary>
        public static WorldSettings NewWorld;
        /// <summary>The explorer made in the character creator for that new world (null = the classic look).</summary>
        public static Appearance NewLook;
        /// <summary>True once the main menu has handed off (false when Main.unity is played directly in the editor).</summary>
        public static bool FromMenu;
        public const string WorldScene = "Main";
        public const string MenuScene = "MainMenu";
    }
}
