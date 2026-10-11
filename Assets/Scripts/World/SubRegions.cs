using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Named places inside each land: three per biome (plus Dawnfield around the start), each with its own ground colour,
    /// extra plants and favoured kin. A sub-region never changes the height of the ground and never moves a prop that was
    /// already there: its plants only fill cells the biome left empty, and it may swap one kind of ground cover for another.
    /// Everything here is pure and thread-safe after Init (chunk builders and the map call it from worker threads).
    /// </summary>
    public static class SubRegions
    {
        public class SubRegion
        {
            public int index;
            public string id, name, blurb;
            public BiomeId biome;
            public Vector2 center;
            public float radius;
            public Color tint;
            public float strength;
            /// <summary>Also colour the shallows (coral, tar).</summary>
            public bool wet;
            public Element[] favour = new Element[0];
            public (PropType type, float chance)[] extra = new (PropType, float)[0];
            public PropType swapFrom = PropType.Count, swapTo = PropType.Count;
            public float swapChance;
            public Vector3 Pos => new Vector3(center.x, WorldGen.Height(center.x, center.y), center.y);
        }

        public static readonly List<SubRegion> All = new List<SubRegion>();
        static SubRegion[] arr = new SubRegion[0];

        /// <summary>Where the player is now (-1 = open country).</summary>
        public static int Current { get; private set; } = -1;
        public static SubRegion CurrentRegion => Current >= 0 && Current < arr.Length ? arr[Current] : null;
        static float checkTimer;

        // ================================================================ TABLE
        /// <summary>Call right after WorldGen.Init (Dawnfield sits on the start).</summary>
        public static void Init()
        {
            All.Clear();
            Current = -1;
            var st = WorldGen.StartCenter;
            Add("dawnfield", "Dawnfield", "The quiet fields where every explorer starts.", BiomeId.Meadow, st.x, st.y, 300f, Color.white, 0f, E(Element.Normal));

            // ---- Sunmeadow Vale ----
            Add("petalrun", "Petalrun", "Hills thick with blossom. Flora and Lumen kin gather here.", BiomeId.Meadow, 330f, -500f, 270f, C(0.97f, 0.62f, 0.8f), 0.58f, E(Element.Flora, Element.Lumen))
                .Plants((PropType.Honeybloom, 0.02f), (PropType.Flower, 0.16f), (PropType.BerryBush, 0.012f)).Swap(PropType.GrassTuft, PropType.Flower, 0.65f);
            Add("oldgrowth", "Oldgrowth Weald", "Old, dark woodland. Good logging, and Flora kin in every shadow.", BiomeId.Meadow, -430f, -330f, 300f, C(0.17f, 0.4f, 0.2f), 0.62f, E(Element.Flora, Element.Normal))
                .Plants((PropType.OakTree, 0.17f), (PropType.FallenSticks, 0.045f), (PropType.FiberPlant, 0.015f));
            Add("honeydew", "Honeydew Downs", "Open golden grass under a wide sky. Gale and Normal kin roam it.", BiomeId.Meadow, 130f, -150f, 270f, C(0.88f, 0.76f, 0.34f), 0.6f, E(Element.Normal, Element.Gale))
                .Plants((PropType.DryGrass, 0.2f), (PropType.GrainStalks, 0.012f)).Swap(PropType.GrassTuft, PropType.DryGrass, 0.7f);

            // ---- Cinderreach Badlands ----
            Add("ashfall", "Ashfall Flats", "Grey ash and steam vents. Ember and Shade kin.", BiomeId.Badlands, 1400f, 260f, 260f, C(0.5f, 0.48f, 0.47f), 0.78f, E(Element.Ember, Element.Shade))
                .Plants((PropType.DeadTree, 0.02f), (PropType.CinderRock, 0.014f)).Swap(PropType.DryGrass, PropType.Pebble, 0.8f);
            Add("redrock", "Redrock Canyons", "Deep red stone, rich in ore. Stone kin.", BiomeId.Badlands, 1040f, 790f, 280f, C(0.68f, 0.2f, 0.14f), 0.5f, E(Element.Stone, Element.Ember))
                .Plants((PropType.Rock, 0.03f), (PropType.OreRock, 0.014f));
            Add("glasswaste", "Glasswaste", "Black glass left by an old fire. Shade kin, and crystal.", BiomeId.Badlands, 1500f, 770f, 240f, C(0.13f, 0.12f, 0.17f), 0.78f, E(Element.Shade, Element.Stone))
                .Plants((PropType.ObsidianSpire, 0.022f), (PropType.CrystalCluster, 0.022f), (PropType.CinderRock, 0.02f)).Swap(PropType.DryGrass, PropType.Pebble, 0.9f);

            // ---- Rimefang Peaks ----
            Add("frostpine", "Frostpine Hollow", "A sheltered pinewood below the snow. Frost and Flora kin.", BiomeId.Peaks, -700f, 760f, 260f, C(0.28f, 0.5f, 0.38f), 0.48f, E(Element.Frost, Element.Flora))
                .Plants((PropType.PineTree, 0.15f), (PropType.FallenSticks, 0.03f), (PropType.MintPatch, 0.01f));
            Add("shiverglass", "Shiverglass Field", "A sheet of blue ice - mind where it has gone thin. Frost and Tide kin.", BiomeId.Peaks, -1380f, 470f, 260f, C(0.58f, 0.84f, 0.98f), 0.72f, E(Element.Frost, Element.Tide))
                .Plants((PropType.Frostcap, 0.022f), (PropType.CrystalCluster, 0.02f));
            Add("stormspine", "Stormspine Ridge", "Bare dark rock that draws the lightning. Volt and Gale kin.", BiomeId.Peaks, -1200f, 1060f, 280f, C(0.27f, 0.28f, 0.36f), 0.62f, E(Element.Volt, Element.Gale))
                .Plants((PropType.Rock, 0.04f), (PropType.OreRock, 0.02f), (PropType.CrystalCluster, 0.012f));

            // ---- outer lands: three each, set around the land's middle ----
            Outer("coral", "Coral Shallows", "Pink coral sand and warm pools. Tide and Lumen kin.", BiomeId.Isles, 20f, C(0.99f, 0.62f, 0.6f), 0.68f, E(Element.Tide, Element.Lumen), true)
                .Plants((PropType.Flower, 0.05f), (PropType.Rock, 0.02f));
            Outer("palmreach", "Palmreach", "The green heart of the isles. Flora and Gale kin.", BiomeId.Isles, 140f, C(0.3f, 0.62f, 0.3f), 0.56f, E(Element.Flora, Element.Gale), true)
                .Plants((PropType.PalmTree, 0.075f), (PropType.BerryBush, 0.02f), (PropType.FiberPlant, 0.02f), (PropType.GrassTuft, 0.12f));
            Outer("shellstrand", "Shellstrand", "White sand heaped with shells. Stone and Tide kin.", BiomeId.Isles, 260f, C(0.99f, 0.97f, 0.91f), 0.7f, E(Element.Stone, Element.Tide), true)
                .Plants((PropType.LooseStones, 0.05f), (PropType.Pebble, 0.09f));

            Outer("lanternfen", "Lanternfen", "Lights drift over this water after dark. Lumen kin.", BiomeId.Swamp, 40f, C(0.2f, 0.52f, 0.48f), 0.52f, E(Element.Lumen, Element.Tide), true)
                .Plants((PropType.Lanternbud, 0.03f), (PropType.GlowShroom, 0.09f), (PropType.Reeds, 0.08f));
            Outer("blackroot", "Blackroot Tangle", "Mangroves so thick the day stays dim. Shade and Flora kin.", BiomeId.Swamp, 160f, C(0.12f, 0.15f, 0.1f), 0.62f, E(Element.Shade, Element.Flora), true)
                .Plants((PropType.MangroveTree, 0.11f), (PropType.FallenSticks, 0.04f));
            Outer("lilymere", "Lilymere", "Bright shallows carpeted in lilies. Tide and Flora kin.", BiomeId.Swamp, 280f, C(0.52f, 0.76f, 0.4f), 0.52f, E(Element.Tide, Element.Flora), true)
                .Plants((PropType.Flower, 0.13f), (PropType.GrassTuft, 0.1f), (PropType.MelonVine, 0.012f));

            Outer("glassdunes", "Glass Dunes", "Pale dunes that sing in the wind. Lumen and Gale kin.", BiomeId.Desert, 70f, C(0.98f, 0.96f, 0.86f), 0.62f, E(Element.Lumen, Element.Gale))
                .Plants((PropType.CrystalCluster, 0.012f));
            Outer("redstone", "Redstone Reach", "Red rock shelves and hidden ore. Stone and Ember kin.", BiomeId.Desert, 190f, C(0.74f, 0.3f, 0.2f), 0.56f, E(Element.Stone, Element.Ember))
                .Plants((PropType.Rock, 0.04f), (PropType.OreRock, 0.018f), (PropType.Spinepalm, 0.01f));
            Outer("oasis", "Verdant Oasis", "Green ground in the middle of the sand. Flora and Normal kin.", BiomeId.Desert, 310f, C(0.4f, 0.66f, 0.3f), 0.62f, E(Element.Flora, Element.Normal))
                .Plants((PropType.Sunfig, 0.022f), (PropType.PalmTree, 0.06f), (PropType.BerryBush, 0.03f), (PropType.GrassTuft, 0.2f), (PropType.Flower, 0.05f), (PropType.PepperBush, 0.012f));

            Outer("sporefall", "Sporefall", "Fungus as far as you can see, and the air is thick with it. Flora and Shade kin.", BiomeId.Blight, 10f, C(0.68f, 0.36f, 0.64f), 0.56f, E(Element.Flora, Element.Shade))
                .Plants((PropType.Sporecap, 0.022f), (PropType.ToxicShroom, 0.22f));
            Outer("seep", "The Seep", "Black tar wells up through the ground. Shade and Tide kin.", BiomeId.Blight, 130f, C(0.08f, 0.07f, 0.1f), 0.78f, E(Element.Shade, Element.Tide), true)
                .Plants((PropType.Rock, 0.02f), (PropType.CrystalCluster, 0.01f));
            Outer("briar", "Withered Briar", "A dead thorn-wood. Shade and Stone kin.", BiomeId.Blight, 250f, C(0.46f, 0.4f, 0.34f), 0.52f, E(Element.Shade, Element.Stone))
                .Plants((PropType.BlightTree, 0.09f), (PropType.DeadTree, 0.03f), (PropType.FallenSticks, 0.04f));

            Outer("starmoss", "Starmoss Fields", "Moss that shines like a night sky underfoot. Lumen and Flora kin.", BiomeId.Glow, 100f, C(0.2f, 0.86f, 0.7f), 0.42f, E(Element.Lumen, Element.Flora))
                .Plants((PropType.Starmoss, 0.025f), (PropType.GlowTuft, 0.25f), (PropType.Flower, 0.03f));
            Outer("crystalreach", "Crystal Reach", "Violet crystal breaks through the ground. Stone and Frost kin.", BiomeId.Glow, 220f, C(0.37f, 0.32f, 0.76f), 0.52f, E(Element.Stone, Element.Frost))
                .Plants((PropType.CrystalCluster, 0.07f), (PropType.Rock, 0.02f));
            Outer("mooncap", "Mooncap Grove", "Pale giant mushrooms close overhead. Shade and Lumen kin.", BiomeId.Glow, 340f, C(0.72f, 0.62f, 0.92f), 0.46f, E(Element.Shade, Element.Lumen))
                .Plants((PropType.GlowTree, 0.1f), (PropType.GlowShroom, 0.15f));

            arr = All.ToArray();
            Shellstrand = All.FindIndex(x => x.id == "shellstrand");
        }

        /// <summary>Index of Shellstrand (its tidal flats grow Pearl Oysters, see ChunkBuilder).</summary>
        public static int Shellstrand { get; private set; } = -1;

        /// <summary>The sub-region a region-only plant grows in ("Petalrun"), for item text.</summary>
        public static string HomeOf(PropType t)
        {
            if (t == PropType.PearlOyster) return "Shellstrand";
            foreach (var s in All) foreach (var e in s.extra) if (e.type == t) return s.name;
            return null;
        }

        static Color C(float r, float g, float b) => new Color(r, g, b);
        static Element[] E(params Element[] e) => e;

        static SubRegion Add(string id, string name, string blurb, BiomeId biome, float x, float z, float radius, Color tint, float strength, Element[] favour, bool wet = false)
        {
            var s = new SubRegion { index = All.Count, id = id, name = name, blurb = blurb, biome = biome, center = new Vector2(x, z), radius = radius, tint = tint, strength = strength, favour = favour, wet = wet };
            All.Add(s);
            return s;
        }

        static SubRegion Outer(string id, string name, string blurb, BiomeId biome, float angle, Color tint, float strength, Element[] favour, bool wet = false)
        {
            WorldGen.TryLand(biome, out var land);
            float a = angle * Mathf.Deg2Rad;
            return Add(id, name, blurb, biome, land.center.x + Mathf.Cos(a) * land.radius * 0.46f, land.center.y + Mathf.Sin(a) * land.radius * 0.46f, land.radius * 0.34f, tint, strength, favour, wet);
        }

        static SubRegion Plants(this SubRegion s, params (PropType, float)[] extra) { s.extra = extra; return s; }
        static SubRegion Swap(this SubRegion s, PropType from, PropType to, float chance) { s.swapFrom = from; s.swapTo = to; s.swapChance = chance; return s; }

        // ================================================================ LOOKUP (thread-safe)
        /// <summary>Sub-region at a point of biome b, or -1. w: 0 at its ragged edge, 1 well inside.</summary>
        public static int At(float x, float z, BiomeId b, out float w)
        {
            w = 0f;
            var a = arr;
            int best = -1;
            for (int i = 0; i < a.Length; i++)
            {
                var s = a[i];
                if (s.biome != b) continue;
                float dx = x - s.center.x, dz = z - s.center.y;
                float lim = s.radius * 1.2f;
                if (dx > lim || dx < -lim || dz > lim || dz < -lim) continue;
                float d = Mathf.Sqrt(dx * dx + dz * dz) / s.radius;
                if (d > 1.2f) continue;
                d += WorldGen.EdgeNoise(x, z) * 0.28f;   // ragged, natural edge
                float ww = WorldGen.Smooth(1f, 0.74f, d);
                if (ww > w) { w = ww; best = i; }
            }
            return best;
        }

        public static SubRegion Get(int i) => i >= 0 && i < arr.Length ? arr[i] : null;

        public static SubRegion Find(Vector3 p)
        {
            int i = At(p.x, p.z, WorldGen.Biome(p.x, p.z), out float w);
            return i >= 0 && w > 0.35f ? arr[i] : null;
        }

        /// <summary>Ground colour inside a sub-region (called from WorldGen.TerrainColor).</summary>
        public static Color Tint(int i, Color col, float w, float h, float ny, float cn)
        {
            var s = arr[i];
            if (s.strength <= 0f) return col;
            float k = w * s.strength;
            if (!s.wet) k *= WorldGen.Smooth(0.7f, 2.4f, h);          // beaches keep their sand
            else k *= WorldGen.Smooth(-5f, -1.5f, h);                 // shallows take the colour too, deep water does not
            k *= Mathf.Lerp(0.4f, 1f, WorldGen.Smooth(0.6f, 0.86f, ny));   // cliffs stay mostly rock
            Color t = s.tint * (0.9f + cn * 0.12f);
            return Color.Lerp(col, t, k);
        }

        /// <summary>A plant for an empty prop cell (PropType.Count = stay empty). roll is a fresh 0..1 random for the cell.</summary>
        public static PropType Extra(int i, float w, float roll, float h)
        {
            var s = arr[i];
            if (w < 0.25f) return PropType.Count;
            float r = roll / Mathf.Min(1f, w * 1.4f);   // thins out toward the edge
            for (int k = 0; k < s.extra.Length; k++)
            {
                if (r < s.extra[k].chance)
                {
                    var t = s.extra[k].type;
                    if (t == PropType.PalmTree && h < 1.6f) return PropType.Count;
                    return t;
                }
                r -= s.extra[k].chance;
            }
            return PropType.Count;
        }

        /// <summary>Ground cover swap (decor only, so nothing saved depends on it).</summary>
        public static PropType Swapped(int i, float w, PropType t, float roll)
        {
            var s = arr[i];
            return t == s.swapFrom && roll < s.swapChance * w ? s.swapTo : t;
        }

        /// <summary>Spawn weight for a species in a sub-region: its favoured elements turn up far more often.</summary>
        public static float SpawnMul(SubRegion s, SpeciesDef sp)
        {
            if (s == null) return 1f;
            float m = 1f;
            foreach (var e in s.favour)
            {
                if (sp.element1 == e) m = Mathf.Max(m, 2.4f);
                else if (sp.element2 == e) m = Mathf.Max(m, 1.6f);
            }
            return m;
        }

        // ================================================================ RUNTIME
        public static void Tick(float dt, PlayerController pl)
        {
            checkTimer -= dt;
            if (checkTimer > 0f) return;
            checkTimer = 0.5f;
            if (Caves.InCave) { Current = -1; return; }
            var s = Find(pl.Position);
            int now = s != null ? s.index : -1;
            if (now == Current) return;
            Current = now;
            if (s == null) return;
            var st = pl.Stats;
            if (!st.regionsFound.Contains(s.id))
            {
                st.regionsFound.Add(s.id);
                GameEvents.Good($"Discovered {s.name}. {s.blurb}");
                SynthAudio.Play(Sfx.UiOpen, 0.9f);
                if (s.strength > 0f) GameManager.I.AddPlayerXp(20);
            }
            else GameEvents.Say(s.name, Color.Lerp(s.tint, Color.white, 0.5f));
        }

        public static bool Found(SubRegion s) => GameManager.I.Player.Stats.regionsFound.Contains(s.id);
    }
}
