using System;
using UnityEngine;

namespace Wildbond
{
    [Serializable]
    public struct BodyPart
    {
        public PartShape shape;
        public BodyGroup group;
        public ColorSlot slot;
        public Vector3 pos;
        public Vector3 scale;
        public Vector3 rot;

        public BodyPart(PartShape shape, BodyGroup group, ColorSlot slot, Vector3 pos, Vector3 scale, Vector3 rot = default)
        {
            this.shape = shape; this.group = group; this.slot = slot; this.pos = pos; this.scale = scale; this.rot = rot;
        }
    }

    [Serializable]
    public struct GroupPivot
    {
        public BodyGroup group;
        public Vector3 pivot;
        public GroupPivot(BodyGroup g, Vector3 p) { group = g; pivot = p; }
    }

    [Serializable]
    public struct Palette
    {
        public Color primary;
        public Color secondary;
        public Color accent;
        public Color eye;
        public Color glow;

        public Color Get(ColorSlot s)
        {
            switch (s)
            {
                case ColorSlot.Primary: return primary;
                case ColorSlot.Secondary: return secondary;
                case ColorSlot.Accent: return accent;
                case ColorSlot.Eye: return eye;
                case ColorSlot.White: return new Color(0.97f, 0.97f, 0.97f);
                case ColorSlot.Dark: return new Color(0.12f, 0.1f, 0.12f);
                case ColorSlot.Glow: return glow;
                default: return primary;
            }
        }
    }

    [Serializable]
    public struct WorkSkill
    {
        public WorkType type;
        [Range(1, 5)] public int level;
        public WorkSkill(WorkType t, int l) { type = t; level = l; }
    }

    [Serializable]
    public struct Ingredient
    {
        public string itemId;
        public int count;
        public Ingredient(string id, int c) { itemId = id; count = c; }
    }

    [Serializable]
    public struct DropEntry
    {
        public string itemId;
        public int min;
        public int max;
        [Range(0, 1)] public float chance;
        public DropEntry(string id, int min, int max, float chance) { itemId = id; this.min = min; this.max = max; this.chance = chance; }
    }

    [Serializable]
    public struct RegionalForm
    {
        public bool enabled;
        public BiomeId biome;
        public string formName;
        public Element element2;
        public Palette palette;
    }

    /// <summary>Overworld utility a kin gives while it is your active companion.</summary>
    public enum CompanionPerk
    {
        None, LumberBoost, StoneBoost, ForageBoost, Swiftness, Glow, Warmth, OreSense, Mending,
        Vanguard,    // starter (combat): +15% player melee damage, companion hits harder when you share a target
        Prospector,  // starter (gathering): +35% yield from every resource node
        Tailwind     // starter (support): +15% move speed, -35% stamina costs, emergency heal when you're low
    }

    /// <summary>How the active companion picks fights.</summary>
    public enum CompanionStance { Passive = 0, Defensive = 1, Aggressive = 2 }
}
