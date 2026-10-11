using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Shimmer ("shiny") kin. Every species has three shimmer colours: its own hand-picked Radiant coat plus two
    /// themed coats chosen per species from <see cref="Themes"/> (never close to its normal colours or to each other).
    /// A shimmer kin sparkles in the world, has +1 in every camp job its species can do, and its colour can be
    /// switched between the three with a Shimmer Prism.
    /// </summary>
    public static class Shimmer
    {
        public const int Variants = 3;
        public const string PrismItem = "shimmer_prism", ShardItem = "prism_shard";
        /// <summary>Extra levels in every camp job the species already has.</summary>
        public const int WorkBonus = 1;

        struct Theme
        {
            public string name;
            public Color primary, secondary, accent, glow;
            public bool glowEyes;   // dark coats: eyes take the glow colour so the face still reads
        }

        static Color H(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }
        static Theme T(string name, string p, string s, string a, string g, bool glowEyes = false) =>
            new Theme { name = name, primary = H(p), secondary = H(s), accent = H(a), glow = H(g), glowEyes = glowEyes };

        static readonly Theme[] Themes =
        {
            T("Gilded", "#e8b83a", "#fff3c8", "#a8701e", "#fff0a0"),
            T("Moonsilver", "#d8dde8", "#f6f8ff", "#7a86a8", "#9ad8ff"),
            T("Obsidian", "#1c1a24", "#3a3648", "#c8303a", "#ff5a6a", true),
            T("Rosequartz", "#ffb0c8", "#fff0f4", "#d85a8a", "#ffe0f0"),
            T("Verdigris", "#3aa89a", "#c8f0e0", "#b8703a", "#7affd8"),
            T("Amethyst", "#8a5ad0", "#e0d0ff", "#4a2a8a", "#ff9af0"),
            T("Sunset", "#ff7a4a", "#ffd8a0", "#8a3a8a", "#ffe070"),
            T("Glacier", "#7ac8f0", "#f0faff", "#3a6ab8", "#c8ffff"),
            T("Venom", "#9ae03a", "#2a2a30", "#5a1a8a", "#d0ff5a", true),
            T("Autumn", "#b85a2a", "#f0d8a8", "#5a2a1a", "#ffb04a"),
        };

        static readonly Dictionary<string, int[]> picks = new Dictionary<string, int[]>();

        static float Dist(Color a, Color b) => Mathf.Sqrt((a.r - b.r) * (a.r - b.r) + (a.g - b.g) * (a.g - b.g) + (a.b - b.b) * (a.b - b.b));

        /// <summary>The two themes this species uses for shimmer colours 2 and 3 (stable per species).</summary>
        static int[] ThemesFor(SpeciesDef s)
        {
            if (picks.TryGetValue(s.id, out var p)) return p;
            int n = Themes.Length;
            bool Clash(int i, int other) =>
                i == other || Dist(Themes[i].primary, s.palette.primary) < 0.3f || Dist(Themes[i].primary, s.shimmerPalette.primary) < 0.3f ||
                (other >= 0 && Dist(Themes[i].primary, Themes[other].primary) < 0.3f);
            int a = (s.dexNumber * 7) % n;
            for (int k = 0; k < n && Clash(a, -1); k++) a = (a + 1) % n;
            int b = (a + 3 + s.dexNumber % 4) % n;
            for (int k = 0; k < n && Clash(b, a); k++) b = (b + 1) % n;
            p = new[] { a, b };
            picks[s.id] = p;
            return p;
        }

        static bool IsSet(Palette p) => p.primary.a > 0.01f;

        public static Palette PaletteFor(SpeciesDef s, int variant)
        {
            variant = Mathf.Clamp(variant, 0, Variants - 1);
            if (variant == 0) return s.shimmerPalette;
            var over = variant == 1 ? s.shimmerPalette2 : s.shimmerPalette3;
            if (IsSet(over)) return over;
            var t = Themes[ThemesFor(s)[variant - 1]];
            return new Palette { primary = t.primary, secondary = t.secondary, accent = t.accent, glow = t.glow, eye = t.glowEyes ? t.glow : s.palette.eye };
        }

        public static string NameFor(SpeciesDef s, int variant)
        {
            variant = Mathf.Clamp(variant, 0, Variants - 1);
            if (variant == 0) return "Radiant";
            var over = variant == 1 ? s.shimmerPalette2 : s.shimmerPalette3;
            return IsSet(over) ? (variant == 1 ? "Lustrous" : "Prismatic") : Themes[ThemesFor(s)[variant - 1]].name;
        }

        /// <summary>Turns a kin shimmer (trait + a random one of the three colours, or the given one).</summary>
        public static void Make(CreatureData d, int variant = -1)
        {
            d.shimmer = true;
            d.shimmerVariant = variant >= 0 ? Mathf.Clamp(variant, 0, Variants - 1) : Random.Range(0, Variants);
            if (!d.traits.Contains("radiant")) d.traits.Add("radiant");
        }

        public static bool HasPrism => GameManager.I != null && GameManager.I.Player.Stats.Bag.Count(PrismItem) > 0;

        /// <summary>Shimmer Prism: switch a shimmer kin to another of its three colours. The prism is not used up.</summary>
        public static bool Recolor(CreatureData d, int variant)
        {
            if (d == null || !d.shimmer || !HasPrism) return false;
            variant = Mathf.Clamp(variant, 0, Variants - 1);
            if (variant == d.shimmerVariant) return false;
            var gm = GameManager.I;
            bool wasOut = gm.Party.Companion != null && gm.Party.Companion.Data == d;
            if (wasOut) gm.Party.Recall(false);
            d.shimmerVariant = variant;
            var worker = BaseManager.I != null ? BaseManager.I.ActorFor(d) : null;
            if (worker != null) BaseManager.I.RespawnWorker(d);
            if (wasOut) gm.Party.Summon(d);
            var pal = d.Palette;
            GameEvents.Good($"{d.DisplayName} now wears its {NameFor(d.Species, variant)} coat.");
            SynthAudio.Play(Sfx.LevelUp, 1.2f, 0.7f);
            Fx.Sparkle(gm.Player.Position + Vector3.up * 1.5f, pal.glow, 30, 1.2f);
            GameEvents.RaiseRosterChanged();
            return true;
        }
    }
}
