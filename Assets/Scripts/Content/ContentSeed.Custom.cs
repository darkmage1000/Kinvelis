using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// A kin the player designed in the kin maker (Kin tab > DREAM UP A KIN). Only the choices are saved
    /// (PlayerStats.customKin); the species itself is rebuilt from them every time the world loads
    /// (ContentSeed.BuildCustom -> ContentDB.RegisterCustom), so a saved design is a few numbers.
    /// </summary>
    [Serializable]
    public class CustomKin
    {
        public string id;
        public string name = "Dreamling";
        public int body;            // CustomKinParts.Bodies
        public int ears = 1, horns, tail = 1, wings, face;
        public int element = 1;     // Element as a number
        public int colorA = 6, colorB = 12, colorC = 5;   // indices into AvatarLook.Cloth
        public int size = 1;        // CustomKinParts.Sizes

        public CustomKin Clone() => (CustomKin)MemberwiseClone();
    }

    public static class CustomKinParts
    {
        public static readonly string[] Bodies = { "Four legs", "Two legs", "Round", "Flying" };
        public static readonly string[] Ears = { "None", "Round", "Pointed", "Long" };
        public static readonly string[] Horns = { "None", "Two horns", "One horn" };
        public static readonly string[] Tails = { "None", "Stubby", "Long", "Fluffy" };
        public static readonly string[] Wings = { "None", "Small", "Big" };
        public static readonly string[] Faces = { "Plain", "Snout", "Beak", "Rosy cheeks" };
        public static readonly string[] Sizes = { "Small", "Medium", "Large" };
        public static readonly float[] SizeValues = { 0.75f, 1f, 1.3f };
        public const string PreviewId = "custom_preview";
        public const int MaxPerWorld = 3, MaxToyBox = 12;
    }

    public static partial class ContentSeed
    {
        static WorkType JobOf(Element e)
        {
            switch (e)
            {
                case Element.Flora: return WorkType.Cultivating;
                case Element.Ember: return WorkType.Kindling;
                case Element.Tide: return WorkType.Irrigating;
                case Element.Frost: return WorkType.Chilling;
                case Element.Volt: return WorkType.Charging;
                case Element.Stone: return WorkType.Quarrying;
                case Element.Gale: return WorkType.Foraging;
                case Element.Shade: return WorkType.Crafting;
                case Element.Lumen: return WorkType.Crafting;
                default: return WorkType.Logging;
            }
        }

        /// <summary>
        /// The species for a player's own design: a body from the same chassis helpers the roster uses (Plan), stats of
        /// an Uncommon kin, two plain moves of its element (the level-up learnset fills the rest), a camp job to suit
        /// the element plus hauling. It never appears in the wild, a shop or the Dex totals (SpeciesDef.custom).
        /// </summary>
        public static SpeciesDef BuildCustom(CustomKin k, string idOverride = null)
        {
            var p = new Plan();
            const ColorSlot A = ColorSlot.Primary, B = ColorSlot.Secondary, C = ColorSlot.Accent;
            int body = Mathf.Clamp(k.body, 0, CustomKinParts.Bodies.Length - 1);
            var loco = Locomotion.Quadruped;
            float tailY, tailZ, wingY, wingX;
            switch (body)
            {
                case 1:   // upright
                    p.Biped(0.34f, 0.5f, 0.5f, 0.5f, 0.15f, A, A, ColorSlot.Dark, true);
                    p.Add(PartShape.Sphere, BodyGroup.Body, B, V(0, 0.56f, 0.13f), V(0.34f, 0.36f, 0.2f));
                    loco = Locomotion.Biped; tailY = 0.42f; tailZ = -0.2f; wingY = 0.7f; wingX = 0.2f;
                    break;
                case 2:   // one round body, the face on it
                    p.Blob(0.4f, 0.74f, 0.66f, 0.72f, A);
                    p.Add(PartShape.Sphere, BodyGroup.Body, B, V(0, 0.3f, 0.2f), V(0.5f, 0.4f, 0.36f));
                    p.Mirror(PartShape.Sphere, BodyGroup.LegFL, BodyGroup.LegFR, ColorSlot.Dark, V(0.2f, 0.06f, 0.1f), V(0.24f, 0.12f, 0.3f));
                    p.PivotPair(BodyGroup.LegFL, BodyGroup.LegFR, V(0.2f, 0.12f, 0.05f));
                    loco = Locomotion.Hopper; tailY = 0.32f; tailZ = -0.33f; wingY = 0.5f; wingX = 0.3f;
                    break;
                case 3:   // hovers
                    p.Blob(0.62f, 0.62f, 0.56f, 0.7f, A);
                    p.Add(PartShape.Sphere, BodyGroup.Body, B, V(0, 0.52f, 0.16f), V(0.42f, 0.36f, 0.4f));
                    loco = Locomotion.Flyer; tailY = 0.6f; tailZ = -0.33f; wingY = 0.72f; wingX = 0.24f;
                    break;
                default:  // four legs
                    p.Quad(0.55f, 0.52f, 0.48f, 0.82f, 0.48f, 0.34f, 0.13f, A, A, ColorSlot.Dark);
                    p.Add(PartShape.Sphere, BodyGroup.Body, B, V(0, 0.46f, 0.02f), V(0.44f, 0.34f, 0.62f));
                    tailY = 0.62f; tailZ = -0.4f; wingY = 0.78f; wingX = 0.18f;
                    break;
            }
            p.Face(0.09f);
            switch (k.face)
            {
                case 1: p.Snout(B, 0.22f); break;
                case 2: p.Beak(C, 0.16f, 0.2f); break;
                case 3: p.Cheeks(C); break;
            }
            switch (k.ears)
            {
                case 1: p.Ears(PartShape.Sphere, A, 0.18f, 0.18f); break;
                case 2: p.Ears(PartShape.Cone, A, 0.16f, 0.28f); break;
                case 3: p.Ears(PartShape.Capsule, A, 0.11f, 0.44f, 12f); break;
            }
            switch (k.horns)
            {
                case 1: p.Horns(C, 0.08f, 0.24f); break;
                case 2: p.Horn(C, 0.09f, 0.3f); break;
            }
            switch (k.tail)
            {
                case 1: p.Tail(A, tailY, tailZ, 0.2f, 0.15f, 0.11f); break;
                case 2: p.Tail(A, tailY, tailZ, 0.62f, 0.1f, 0.05f, 0.04f, C, 0.12f, 0.012f); break;
                case 3: p.Tail(A, tailY, tailZ, 0.3f, 0.18f, 0.24f, 0.05f, B, 0.26f); break;
            }
            int wings = body == 3 ? Mathf.Max(1, k.wings) : k.wings;   // something that flies has wings
            if (wings == 1) p.Wings(B, A, C, V(wingX, wingY, -0.02f), 0.44f, 0.28f);
            else if (wings >= 2) p.Wings(B, A, C, V(wingX, wingY, -0.02f), 0.8f, 0.42f);

            var e = (Element)Mathf.Clamp(k.element, 0, System.Enum.GetValues(typeof(Element)).Length - 1);
            var d = ScriptableObject.CreateInstance<SpeciesDef>();
            d.id = idOverride ?? k.id;
            d.name = "Species_custom_" + d.id;
            d.custom = true;
            d.dexNumber = 900;
            d.displayName = string.IsNullOrWhiteSpace(k.name) ? "Dreamling" : k.name.Trim();
            d.element1 = e; d.element2 = e;
            d.rarity = Rarity.Uncommon;
            d.biomes = 0;
            d.temperament = Temperament.Docile;
            d.locomotion = loco;
            d.baseHp = 60; d.baseAtk = 13; d.baseDef = 12;
            d.walkSpeed = loco == Locomotion.Flyer ? 2.7f : 2.4f; d.runSpeed = loco == Locomotion.Flyer ? 7.3f : 6.5f;
            d.size = CustomKinParts.SizeValues[Mathf.Clamp(k.size, 0, CustomKinParts.SizeValues.Length - 1)];
            d.lore = "A kin somebody dreamed up, and then there it was.";
            d.personality = "One of a kind, and knows it.";
            d.spawnWeight = 0f; d.minGroup = 1; d.maxGroup = 1;
            d.abilities = CustomMoves(e);
            d.rareAbilities = new string[0];
            d.work = Work((JobOf(e), 2), (WorkType.Hauling, 1));
            d.perk = CompanionPerk.None;
            d.overworldRole = "Made by you.";
            d.drops = new DropEntry[0];
            Color ca = AvatarLook.Cloth[Mathf.Clamp(k.colorA, 0, AvatarLook.Cloth.Length - 1)], cb = AvatarLook.Cloth[Mathf.Clamp(k.colorB, 0, AvatarLook.Cloth.Length - 1)],
                  cc = AvatarLook.Cloth[Mathf.Clamp(k.colorC, 0, AvatarLook.Cloth.Length - 1)];
            d.palette = Pal(ca, cb, cc, Color.Lerp(cc, Color.white, 0.5f));
            d.shimmerPalette = HueShift(d.palette, 0.42f);
            d.parts = p.parts.ToArray();
            d.pivots = p.pivots.ToArray();
            return d;
        }

        /// <summary>The weakest plain move of the element first, then a middling one: no signatures, nothing that only sets up.</summary>
        static string[] CustomMoves(Element e)
        {
            var pool = new List<AbilityDef>();
            foreach (var a in ContentDB.Abilities.Values)
                if (a.element == e && a.effect == AbilityEffect.None && a.kind != AbilityKind.Blink && a.power > 0f) pool.Add(a);
            pool.Sort((x, y) => x.power.CompareTo(y.power));
            if (pool.Count == 0) return new[] { "tackle" };
            if (pool.Count == 1) return new[] { pool[0].id };
            return new[] { pool[0].id, pool[Mathf.Min(pool.Count - 1, 1 + pool.Count / 3)].id };
        }
    }
}
