using UnityEngine;

namespace Wildbond
{
    public enum Element { Normal, Flora, Ember, Tide, Frost, Volt, Stone, Gale, Shade, Lumen }

    public enum Rarity { Common, Uncommon, Rare, Epic, Legendary }

    public enum BiomeId { Meadow = 0, Badlands = 1, Peaks = 2, Swamp = 3, Blight = 4, Desert = 5, Glow = 6, Isles = 7 }

    [System.Flags]
    public enum BiomeMask { None = 0, Meadow = 1, Badlands = 2, Peaks = 4, Swamp = 8, Blight = 16, Desert = 32, Glow = 64, Isles = 128, All = 255 }

    public enum SpawnTime { Any, Day, Night }

    /// <summary>An extra condition some kin need before they show themselves in the wild.</summary>
    public enum SpawnCondition { None, Rain, Heat, Snowfall, Blizzard, Dawn, Dusk }

    /// <summary>Base jobs a kin can perform. Level 0 = cannot do it.</summary>
    public enum WorkType { None, Logging, Quarrying, Kindling, Irrigating, Cultivating, Crafting, Hauling, Chilling, Charging, Foraging, Fishing }

    /// <summary>How a wild kin reacts to the player.</summary>
    public enum Temperament { Timid, Docile, Curious, Territorial, Aggressive }

    public enum StatusType { None, Burn, Chill, Shock, Drowsy, Soaked, Poison }

    /// <summary>Blink = vanish and reappear behind the target: a light strike that stuns it for a moment and makes the user's next hit a sure crit.</summary>
    public enum AbilityKind { Melee, Projectile, Burst, Dash, Blink }

    /// <summary>
    /// A signature twist on top of an ability's kind (SkillEffects has the rules and the menu text). New values go
    /// at the end: ability assets store the number.
    /// </summary>
    public enum AbilityEffect
    {
        None, Iaido, Guard, Flurry, Riposte, Hex, Decoy, Tether, Tonic, Anthem, Encore, Sear, Temper, Reap, Drain,
        Quickdraw, Plunder, Pull, Wrath, Rally
    }

    public enum ItemCategory { Resource, Food, Tool, Weapon, CaptureSeed, Consumable, Upgrade, KeyItem, Gear }

    public enum ToolKind { None, Axe, Pick, Sickle }

    public enum CraftStation { Hand, Workbench, Campfire, AlloyForge, Research }

    /// <summary>Which tab of the Build menu a structure is listed under. Auto = worked out from what it does (MenuUI.BuildCatOf).</summary>
    public enum BuildCategory { Auto, Crafting, Production, Storage, KinCare, Utility }

    public enum KinLocation { Party, Base, Box, Expedition, Outpost }

    public enum Mutation { None, Crystalline, Giant, Twintail, Glowing }

    /// <summary>Procedural body part groups. Each group becomes one animated transform.</summary>
    /// <summary>
    /// Parts of a model that move on their own. The last five are the human skeleton (PlayerLook): Torso turns at the
    /// waist and carries the head and arms; a Forearm hangs from each arm at the elbow, a Shin from each leg at the knee
    /// (see ModelBuilder.ParentOf). New values go at the end: species assets store these as numbers.
    /// </summary>
    public enum BodyGroup { Body, Head, LegFL, LegFR, LegBL, LegBR, ArmL, ArmR, Tail, WingL, WingR, Extra, Torso, ForearmL, ForearmR, ShinL, ShinR, Count }

    public enum PartShape { Sphere, Box, Cylinder, Cone, Capsule, Crystal, Rock, Dome }

    public enum ColorSlot { Primary, Secondary, Accent, Eye, White, Dark, Glow }

    /// <summary>How a species' Extra part group moves (orbiting shards, propellers, bobbing smoke puffs).</summary>
    public enum ExtraAnim { None, Orbit, Spin, Bob, Pulse }

    /// <summary>Ambient particles a species gives off while it is near the camera.</summary>
    public enum AuraFx { None, Embers, Sparks, Snow, Petals, Bubbles, Gloom, Glitter, Breeze }

    /// <summary>Animation style the procedural animator uses.</summary>
    public enum Locomotion { Biped, Quadruped, Hopper, Flyer, Slither }

    public static class ElementInfo
    {
        public static Color Color(Element e)
        {
            switch (e)
            {
                case Element.Flora: return new Color(0.38f, 0.8f, 0.35f);
                case Element.Ember: return new Color(1f, 0.45f, 0.18f);
                case Element.Tide: return new Color(0.25f, 0.6f, 1f);
                case Element.Frost: return new Color(0.6f, 0.9f, 1f);
                case Element.Volt: return new Color(1f, 0.87f, 0.2f);
                case Element.Stone: return new Color(0.66f, 0.55f, 0.42f);
                case Element.Gale: return new Color(0.7f, 0.95f, 0.8f);
                case Element.Shade: return new Color(0.5f, 0.35f, 0.75f);
                case Element.Lumen: return new Color(1f, 0.95f, 0.7f);
                default: return new Color(0.9f, 0.84f, 0.72f); // Normal
            }
        }

        // attacker -> defenders it is strong against
        static readonly Element[][] Strong =
        {
            new Element[0],                                   // Normal
            new[] { Element.Tide, Element.Stone },            // Flora
            new[] { Element.Flora, Element.Frost },           // Ember
            new[] { Element.Ember, Element.Stone },           // Tide
            new[] { Element.Flora, Element.Gale },            // Frost
            new[] { Element.Tide, Element.Gale },             // Volt
            new[] { Element.Volt, Element.Ember },            // Stone
            new[] { Element.Flora, Element.Volt },            // Gale  (wind scatters sparks)
            new[] { Element.Lumen },                          // Shade
            new[] { Element.Shade },                          // Lumen
        };

        public static bool IsStrong(Element atk, Element def)
        {
            var arr = Strong[(int)atk];
            for (int i = 0; i < arr.Length; i++) if (arr[i] == def) return true;
            return false;
        }

        /// <summary>Damage multiplier vs a (possibly dual-typed) defender.</summary>
        public static float Multiplier(Element atk, Element def1, Element def2)
        {
            float m = Single(atk, def1);
            if (def2 != Element.Normal && def2 != def1) m *= Single(atk, def2);
            return Mathf.Clamp(m, 0.35f, 2.25f);
        }

        /// <summary>
        /// The chart's three numbers (user, 2026-10-19: "should same type do reduced damage, and countered elements do very
        /// little to things that counter them?"): x1.5 into what it beats, x0.5 into what beats it (was 0.67), x0.75 into
        /// its own element (was 1). Normal neither gives nor takes. Two elements multiply, within 0.35-2.25.
        /// </summary>
        public const float Strong_ = 1.5f, Countered = 0.5f, SameType = 0.75f;

        static float Single(Element atk, Element def)
        {
            if (atk == Element.Normal || def == Element.Normal) return 1f;
            if (IsStrong(atk, def)) return Strong_;
            if (IsStrong(def, atk)) return Countered;
            if (atk == def) return SameType;   // a kin shrugs off its own element
            return 1f;
        }

        public static string Short(Element e) => e.ToString().Substring(0, 3).ToUpperInvariant();
    }

    public static class RarityInfo
    {
        public static Color Color(Rarity r)
        {
            switch (r)
            {
                case Rarity.Uncommon: return new Color(0.4f, 0.9f, 0.45f);
                case Rarity.Rare: return new Color(0.35f, 0.65f, 1f);
                case Rarity.Epic: return new Color(0.75f, 0.45f, 1f);
                case Rarity.Legendary: return new Color(1f, 0.75f, 0.2f);
                default: return new Color(0.85f, 0.85f, 0.85f);
            }
        }
    }

    public static class WorkInfo
    {
        public static string Icon(WorkType w)
        {
            switch (w)
            {
                case WorkType.Logging: return "LOG";
                case WorkType.Quarrying: return "QRY";
                case WorkType.Kindling: return "KND";
                case WorkType.Irrigating: return "IRR";
                case WorkType.Cultivating: return "CLT";
                case WorkType.Crafting: return "CRF";
                case WorkType.Hauling: return "HAU";
                case WorkType.Chilling: return "CHL";
                case WorkType.Charging: return "CHG";
                case WorkType.Foraging: return "FOR";
                case WorkType.Fishing: return "FSH";
                default: return "-";
            }
        }
    }
}
