using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Wildbond
{
    [System.Flags]
    public enum WardenMech { None = 0, Adds = 1, Pools = 2, Barrage = 4, Shield = 8, Enrage = 16, Regen = 32 }

    [System.Serializable]
    public class WardenRecord { public string id; public int wins; public long unix; }

    /// <summary>
    /// The Warden journey: one boss per land, each waiting at its own arena landmark (a ring of standing stones with a
    /// shrine and a tall beacon). Tap WARDEN at the shrine to read up on it and CHALLENGE. The fight has three phases
    /// (at 66% and 33% health) built from a few mechanics: guards, hazard pools, marked ground strikes, a shield,
    /// an enrage, and healing while guards stand. A first win earns that land's Sigil; all eight open Stormcrest's
    /// Aerie. The Kraken on its lone rock is optional. Wardens can't be caught and nothing wild wanders into an arena.
    /// </summary>
    public class Wardens : MonoBehaviour
    {
        public static Wardens I { get; private set; }
        public const int RematchSeconds = 30 * 60;
        public const float ArenaRadius = 22f, LeaveRadius = 62f, QuietRing = 55f;
        public const string HeartItem = "warden_heart";
        /// <summary>The one seed that can bind a boss: the Kraken's (CaptureSystem.BossSeedHp).</summary>
        public const string KrakenSeed = "deepcall_seed";
        static string SeedFor(string species) => species == "krakolos" ? KrakenSeed : null;

        /// <summary>Is a boss of this kind awake right now? (Lets its own seed be the one in hand.)</summary>
        public static bool BossOut(string species) => I != null && I.Active != null && I.boss != null && I.Active.species == species;

        string seedBefore;
        bool caughtNow;

        /// <summary>Puts the boss's own seed in hand for the fight, if the player carries one (put back when the fight ends).</summary>
        void ReadyBossSeed(Warden w)
        {
            seedBefore = null;
            string seed = SeedFor(w.species);
            if (seed == null || S.Bag.Count(seed) <= 0) return;
            seedBefore = S.selectedSeed ?? "";
            S.selectedSeed = seed;
            GameEvents.Say($"{ContentDB.ItemName(seed)} readied. It cannot take hold until {w.Name} is at {CaptureSystem.BossSeedHp * 100f:0}% health or less.", new Color(0.7f, 0.85f, 1f));
        }

        /// <summary>The boss was bound by its seed: that wins the fight (CaptureSystem.Succeed).</summary>
        public void BossCaught(CreatureActor a)
        {
            if (Active == null || boss == null || !ReferenceEquals(a, boss)) return;
            caughtNow = true;
            End(true, null);
            caughtNow = false;
        }
        const float HpMul = 2f, BaseAtk = 0.55f, EnrageAtk = 0.8f;   // 0.75 / 1.1 until 2026-10-18 (Warden hits took a fifth to half of a kin at its level)

        public class Warden
        {
            public string id, title, place, land, species, guard, taunt;
            public BiomeId biome;
            public int level, style;
            public Color color;
            public WardenMech phase2, phase3;
            public StatusType hazard;
            public Vector3 pos, shrine;
            public bool final, optional;
            public GameObject root;
            public SpeciesDef Sp => ContentDB.Sp(species);
            public string Name => Sp != null ? Sp.displayName : title;
        }

        public static readonly List<Warden> All = new List<Warden>();

        // ============================================================ SITES
        static Warden Def(string id, string place, string land, BiomeId biome, string species, string guard, int level, int style, string color,
            WardenMech p2, WardenMech p3, StatusType hazard, string taunt, Vector3 near)
        {
            ColorUtility.TryParseHtmlString(color, out var c);
            var w = new Warden { id = id, place = place, land = land, biome = biome, species = species, guard = guard, level = level, style = style, color = c,
                phase2 = p2, phase3 = p3, hazard = hazard, taunt = taunt };
            Vector3 p = Settlements.FindFlat(near, biome);
            // keep clear of villages and their tamers
            for (int tries = 0; tries < 4 && Settlements.Inside(p, 170f); tries++)
                p = Settlements.FindFlat(near + new Vector3(260f * (tries + 1), 0, 180f * (tries + 1)), biome);
            w.pos = p;
            All.Add(w);
            return w;
        }

        static Vector3 LandCenter(BiomeId b)
        {
            return WorldGen.TryLand(b, out var l) ? new Vector3(l.center.x, 0, l.center.y) : Vector3.zero;
        }

        /// <summary>Picks the arena sites (pure functions of the world seed). Runs before chunks build so arenas stay clear of trees.</summary>
        public static void Prepare()
        {
            All.Clear();
            const WardenMech A = WardenMech.Adds, P = WardenMech.Pools, B = WardenMech.Barrage, S = WardenMech.Shield, E = WardenMech.Enrage, R = WardenMech.Regen;
            Def("meadow", "Bloomwyrm Grove", "Sunmeadow Vale", BiomeId.Meadow, "bloomwyrm", "sproutle", 13, 0, "#7ad06a", A, P | A, StatusType.Drowsy,
                "The grove stirs. Petals lift off the ground.", new Vector3(-700f, 0, -200f));
            Def("isles", "Coral Throne", "Driftkey Isles", BiomeId.Isles, "reefwarden", "cocorab", 15, 3, "#ff7a8a", A, B, StatusType.Soaked,
                "The reef rises to its feet.", LandCenter(BiomeId.Isles));
            Def("badlands", "Solarion's Caldera", "Cinderreach Badlands", BiomeId.Badlands, "solarion", "emberkit", 22, 1, "#ff8a3a", B, P | E, StatusType.Burn,
                "The air shimmers with heat.", new Vector3(1450f, 0, 250f));
            Def("swamp", "Sunken Heart", "Mirefen", BiomeId.Swamp, "hydrabog", "leechlash", 23, 4, "#8ac86a", A | R, P | A | R, StatusType.Poison,
                "Three heads turn towards you at once.", LandCenter(BiomeId.Swamp));
            var k = Def("kraken", "Kraken's Rock", "the open sea", BiomeId.Isles, "krakolos", "krakling", 28, 8, "#b07ad0", B, A | P, StatusType.Drowsy,
                "The sea around the rock goes black.", new Vector3(WorldGen.KrakenIsle.x, 0, WorldGen.KrakenIsle.y));
            k.optional = true;
            k.pos = new Vector3(WorldGen.KrakenIsle.x, WorldGen.Height(WorldGen.KrakenIsle.x, WorldGen.KrakenIsle.y), WorldGen.KrakenIsle.y);
            Def("peaks", "Aurora Glacier", "Rimefang Peaks", BiomeId.Peaks, "aurorox", "chillpip", 30, 2, "#9ae0ff", P, B | S, StatusType.Chill,
                "Light ripples across the ice.", new Vector3(-1250f, 0, 1150f));
            Def("desert", "Sun Temple", "Sunscour Dunes", BiomeId.Desert, "djinnado", "dunelet", 32, 5, "#f0c040", S | B, E | B, StatusType.Burn,
                "The wind laughs, low and long.", LandCenter(BiomeId.Desert));
            Def("blight", "Blight Crater", "Blightmoor", BiomeId.Blight, "venomarch", "stingnat", 41, 6, "#b8ff40", P, A | P | E, StatusType.Poison,
                "Something heavy uncoils in the crater.", LandCenter(BiomeId.Blight));
            Def("glow", "Star Hollow", "Glowdeep", BiomeId.Glow, "astralisk", "prismfox", 48, 7, "#b0a0ff", B, S | A | B, StatusType.Drowsy,
                "Every light in the hollow turns to look.", LandCenter(BiomeId.Glow));
            var f = Def("stormcrest", "Stormcrest's Aerie", "the heart of the island", WorldGen.Biome(-150f, 320f), "stormcrest", "voltbeak", 52, 9, "#ffe060", B | A, S | B | E,
                StatusType.Shock, "Thunder answers from a clear sky.", new Vector3(-150f, 0, 320f));
            f.final = true;
            foreach (var w in All) w.shrine = w.pos + new Vector3(0, 0, ArenaRadius - 3f);
        }

        public static bool Inside(Vector3 p, float r)
        {
            for (int i = 0; i < All.Count; i++)
            {
                float dx = p.x - All[i].pos.x, dz = p.z - All[i].pos.z;
                if (dx * dx + dz * dz < r * r) return true;
            }
            return false;
        }

        public static bool BlocksNode(Vector3 p) => Inside(p, ArenaRadius + 4f);

        // ============================================================ PROGRESS
        static PlayerStats S => GameManager.I.Player.Stats;

        public static Warden Get(string id) { foreach (var w in All) if (w.id == id) return w; return null; }

        public static int Wins(Warden w)
        {
            foreach (var r in S.wardenWins) if (r.id == w.id) return r.wins;
            return 0;
        }

        /// <summary>True once the Warden with this id (meadow, badlands, peaks, isles, swamp, desert, blight, glow, kraken, stormcrest) has been beaten.</summary>
        public static bool Beaten(string id) { var w = Get(id); return w != null && Wins(w) > 0; }

        public static int Cooldown(Warden w)
        {
            long last = 0;
            foreach (var r in S.wardenWins) if (r.id == w.id) last = r.unix;
            return (int)Mathf.Max(0, last + RematchSeconds - Expeditions.Now);
        }

        public const int SigilsNeeded = 8;
        public static int Sigils
        {
            get { int n = 0; foreach (var w in All) if (!w.final && !w.optional && Wins(w) > 0) n++; return n; }
        }

        public static int Purse(Warden w) => Mathf.RoundToInt((200 + w.level * 30) * (Wins(w) == 0 ? 1f : 0.5f) / 10f) * 10;
        public static int Hearts(Warden w) => Wins(w) == 0 ? (w.final ? 5 : 3) : 1;

        /// <summary>Wardens in the order the journey suggests them.</summary>
        public static List<Warden> ByLevel()
        {
            var l = new List<Warden>(All);
            l.Sort((a, b) => a.level.CompareTo(b.level));
            return l;
        }

        /// <summary>HUD goal line once the first steps are done (null when the journey is complete).</summary>
        public static string NextGoal()
        {
            if (GameManager.I == null || GameManager.I.Player == null) return null;
            foreach (var w in ByLevel())
            {
                if (w.optional || Wins(w) > 0) continue;
                if (w.final) return Sigils >= SigilsNeeded ? $"All eight Sigils are yours. Climb to {w.place} (Lv {w.level})." : null;
                return $"Journey: challenge the Warden at {w.place} in {w.land} (Lv {w.level}). See JOURNEY in the menu.";
            }
            return null;
        }

        public static string Compass(Vector3 from, Vector3 to)
        {
            Vector3 d = to - from; d.y = 0;
            float m = d.magnitude;
            string[] names = { "north", "north-east", "east", "south-east", "south", "south-west", "west", "north-west" };
            int i = Mathf.RoundToInt(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg / 45f);
            string dist = m >= 1000f ? $"{m / 1000f:0.0} km" : $"{Mathf.RoundToInt(m / 10f) * 10} m";
            return $"{dist} {names[((i % 8) + 8) % 8]}";
        }

        public static string Describe(WardenMech m, Warden w)
        {
            var parts = new List<string>();
            string guard = ContentDB.Sp(w.guard) != null ? ContentDB.Sp(w.guard).displayName : "guards";
            if ((m & WardenMech.Shield) != 0) parts.Add((m & WardenMech.Adds) != 0 ? $"shields itself until its {guard} guards fall" : "shields itself for a few seconds");
            if ((m & WardenMech.Adds) != 0 && (m & WardenMech.Shield) == 0) parts.Add($"calls {guard} guards");
            if ((m & WardenMech.Regen) != 0) parts.Add("heals while any guard stands");
            if ((m & WardenMech.Pools) != 0) parts.Add($"leaves {w.hazard.ToString().ToLowerInvariant()} pools under you - step out of them");
            if ((m & WardenMech.Barrage) != 0) parts.Add("marks the ground before it strikes - get off the circles");
            if ((m & WardenMech.Enrage) != 0) parts.Add("hits half again as hard");
            return string.Join("; ", parts) + ".";
        }

        // ============================================================ ARENAS
        public void Init()
        {
            I = this;
            foreach (var w in All) Build(w);
        }

        struct Style { public Color32 stone, dark, accent, glow; }

        static Color32 Hx(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

        static readonly Style[] Styles =
        {
            new Style { stone = Hx("#7a5a3c"), dark = Hx("#5a4028"), accent = Hx("#4aa850"), glow = Hx("#ffb0d8") },   // 0 grove
            new Style { stone = Hx("#3a3236"), dark = Hx("#241e22"), accent = Hx("#ff6a2a"), glow = Hx("#ffd060") },   // 1 caldera
            new Style { stone = Hx("#a8d8f0"), dark = Hx("#6a9ac0"), accent = Hx("#e8f8ff"), glow = Hx("#a0f0ff") },   // 2 glacier
            new Style { stone = Hx("#d8c8a0"), dark = Hx("#b0a078"), accent = Hx("#ff6a7a"), glow = Hx("#ffd0e0") },   // 3 coral
            new Style { stone = Hx("#4a4032"), dark = Hx("#352c22"), accent = Hx("#5a8a4a"), glow = Hx("#c0ff80") },   // 4 mangrove
            new Style { stone = Hx("#d8b070"), dark = Hx("#b08a50"), accent = Hx("#f0c040"), glow = Hx("#fff0a0") },   // 5 sun temple
            new Style { stone = Hx("#3a3444"), dark = Hx("#28222e"), accent = Hx("#7a4aa0"), glow = Hx("#b8ff40") },   // 6 blight
            new Style { stone = Hx("#3a3a6a"), dark = Hx("#28284a"), accent = Hx("#8a6ae0"), glow = Hx("#b0f4ff") },   // 7 star
            new Style { stone = Hx("#6a5440"), dark = Hx("#4a3a2c"), accent = Hx("#8a8a80"), glow = Hx("#b07ad0") },   // 8 wreck
            new Style { stone = Hx("#8a8a96"), dark = Hx("#5e5e6a"), accent = Hx("#f0c040"), glow = Hx("#ffe98a") },   // 9 storm
        };

        static Matrix4x4 M(Vector3 p, Vector3 euler, Vector3 s) => Matrix4x4.TRS(p, Quaternion.Euler(euler), s);

        void Build(Warden w)
        {
            w.root = new GameObject("Warden_" + w.id);
            w.root.transform.SetParent(transform, false);
            w.root.transform.position = w.pos;
            var st = Styles[w.style];
            var mb = new MeshBuilder();
            // ring of standing stones (a gap faces the shrine side so the way in is obvious)
            const int n = 12;
            for (int i = 0; i < n; i++)
            {
                float a = (i + 0.5f) / n * Mathf.PI * 2f;
                Vector3 p = w.pos + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * ArenaRadius;
                p.y = WorldGen.Height(p.x, p.z);
                if (p.y < WorldGen.WaterLevel - 0.3f) continue;
                Pillar(mb, p - w.pos, w.style, st, i, a * Mathf.Rad2Deg);
                WorldCollision.AddObstacle(p, 1.3f, this);
            }
            // shrine: a stepped dais with a beacon you can see from far off
            Vector3 s = w.shrine; s.y = WorldGen.Height(s.x, s.z);
            w.shrine = s;
            Vector3 o = s - w.pos + new Vector3(0, 0, 2.6f);
            mb.Cylinder(M(o + Vector3.up * 0.2f, Vector3.zero, new Vector3(5.2f, 0.4f, 5.2f)), st.dark, st.stone, 10, 1f, true);
            mb.Cylinder(M(o + Vector3.up * 0.55f, Vector3.zero, new Vector3(3.8f, 0.3f, 3.8f)), st.dark, st.stone, 10, 1f, true);
            mb.Cylinder(M(o + Vector3.up * 1.6f, Vector3.zero, new Vector3(1.3f, 1.8f, 1.3f)), st.stone, st.dark, 6, 0.8f, true);
            mb.Crystal(M(o + Vector3.up * 2.4f, Vector3.zero, new Vector3(2.6f, 19f, 2.6f)), st.accent, st.glow);
            mb.Crystal(M(o + new Vector3(1.1f, 2.2f, 0.2f), new Vector3(0, 0, -24), new Vector3(1.2f, 7f, 1.2f)), st.accent, st.glow);
            mb.Crystal(M(o + new Vector3(-1.0f, 2.2f, -0.2f), new Vector3(8, 0, 22), new Vector3(1.1f, 6f, 1.1f)), st.accent, st.glow);
            mb.Sphere(M(o + new Vector3(0, 0.95f, -1.5f), Vector3.zero, new Vector3(0.7f, 0.5f, 0.7f)), st.glow, 8, 6);   // offering bowl: stand here
            WorldCollision.AddObstacle(s + new Vector3(0, 0, 2.6f), 1.2f, this);

            var mf = w.root.AddComponent<MeshFilter>();
            mf.sharedMesh = mb.ToMesh("warden_arena_" + w.id);
            var mr = w.root.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.Building;
            mr.shadowCastingMode = ShadowCastingMode.On;
            Landmarks.List.Add(new Landmarks.Landmark { name = w.place, pos = w.pos, color = w.color, biome = w.biome, go = w.root, icon = MapIcon.Warden });
        }

        static void Pillar(MeshBuilder mb, Vector3 o, int style, Style st, int i, float yawDeg)
        {
            float h = 4.6f + (i * 37 % 5) * 0.45f;
            var yaw = new Vector3(0, yawDeg, 0);
            var T = Matrix4x4.TRS(o, Quaternion.identity, Vector3.one * 1.5f);
            o = Vector3.zero;
            switch (style)
            {
                case 0:   // carved trunks with a leafy crown
                    mb.Cylinder(T * M(o + Vector3.up * h * 0.5f, yaw, new Vector3(1.1f, h, 1.1f)), st.dark, st.stone, 7, 0.75f, true);
                    mb.Sphere(T * M(o + Vector3.up * (h + 0.5f), Vector3.zero, new Vector3(2.4f, 1.7f, 2.4f)), st.accent, 8, 6);
                    mb.Sphere(T * M(o + Vector3.up * (h * 0.6f) + Quaternion.Euler(yaw) * new Vector3(0, 0, -0.5f), Vector3.zero, Vector3.one * 0.4f), st.glow, 6, 5);
                    break;
                case 1:   // black rock teeth with a molten tip
                    mb.Rock(T * M(o + Vector3.up * h * 0.45f, yaw, new Vector3(1.8f, h, 1.6f)), st.dark, st.stone, (uint)(i * 7 + 3), 7, 5, 0.2f);
                    mb.Cone(T * M(o + Vector3.up * (h * 0.95f), new Vector3(0, yawDeg, 8), new Vector3(0.7f, 1.4f, 0.7f)), st.accent, 6);
                    break;
                case 2:   // ice shards
                    mb.Crystal(T * M(o, new Vector3(0, yawDeg, i % 2 == 0 ? 8 : -6), new Vector3(1.6f, h + 1.4f, 1.6f)), st.dark, st.accent);
                    mb.Crystal(T * M(o + Quaternion.Euler(yaw) * new Vector3(0.8f, 0, 0), new Vector3(0, yawDeg, -22), new Vector3(0.8f, h * 0.5f, 0.8f)), st.stone, st.accent);
                    break;
                case 3:   // sandstone stubs grown over with coral
                    mb.Cylinder(T * M(o + Vector3.up * 1.3f, yaw, new Vector3(1.5f, 2.6f, 1.5f)), st.dark, st.stone, 8, 0.85f, true);
                    mb.Cylinder(T * M(o + new Vector3(0, 3.3f, 0), new Vector3(0, yawDeg, 0), new Vector3(0.36f, 2.4f, 0.36f)), st.accent, st.glow, 6, 0.6f, true);
                    mb.Cylinder(T * M(o + Quaternion.Euler(yaw) * new Vector3(0.55f, 3.5f, 0), new Vector3(0, yawDeg, -40), new Vector3(0.28f, 1.7f, 0.28f)), st.accent, st.glow, 6, 0.6f, true);
                    mb.Cylinder(T * M(o + Quaternion.Euler(yaw) * new Vector3(-0.5f, 3.2f, 0), new Vector3(0, yawDeg, 36), new Vector3(0.26f, 1.4f, 0.26f)), st.accent, st.glow, 6, 0.6f, true);
                    break;
                case 4:   // leaning mangrove roots under moss
                    mb.Cylinder(T * M(o + Vector3.up * h * 0.45f, new Vector3(i % 2 == 0 ? 12 : -10, yawDeg, 9), new Vector3(0.9f, h, 0.9f)), st.dark, st.stone, 6, 0.6f, true);
                    mb.Cylinder(T * M(o + Quaternion.Euler(yaw) * new Vector3(0.9f, 1.2f, 0), new Vector3(0, yawDeg, -34), new Vector3(0.4f, 2.8f, 0.4f)), st.dark, st.stone, 5, 0.7f, false);
                    mb.Sphere(T * M(o + Vector3.up * (h * 0.92f), Vector3.zero, new Vector3(1.9f, 1.1f, 1.9f)), st.accent, 8, 5);
                    break;
                case 5:   // obelisks with gilded caps
                    mb.Cylinder(T * M(o + Vector3.up * (h * 0.5f + 0.3f), new Vector3(0, yawDeg + 45f, 0), new Vector3(1.5f, h + 0.6f, 1.5f)), st.dark, st.stone, 4, 0.6f, true);
                    mb.Cone(T * M(o + Vector3.up * (h + 1.1f), new Vector3(0, yawDeg + 45f, 0), new Vector3(0.95f, 1.1f, 0.95f)), st.accent, 4);
                    mb.Box(T * M(o + Vector3.up * 0.2f, yaw, new Vector3(2f, 0.4f, 2f)), st.dark, st.stone);
                    break;
                case 6:   // dark fangs leaning out of the crater, weeping light
                    mb.Cone(T * M(o + Vector3.up * (h * 0.5f), new Vector3(-14, yawDeg, 0), new Vector3(1.5f, h + 1f, 1.5f)), st.stone, 6);
                    mb.Cone(T * M(o + Quaternion.Euler(yaw) * new Vector3(0.9f, 1.2f, 0.3f), new Vector3(-20, yawDeg, -18), new Vector3(0.8f, 2.6f, 0.8f)), st.dark, 5);
                    mb.Sphere(T * M(o + Quaternion.Euler(yaw) * new Vector3(0, h * 0.5f, -0.6f), Vector3.zero, new Vector3(0.5f, 0.7f, 0.5f)), st.glow, 6, 5);
                    break;
                case 7:   // violet crystals with a star caught above each
                    mb.Crystal(T * M(o, new Vector3(0, yawDeg, i % 2 == 0 ? -7 : 6), new Vector3(1.3f, h + 0.6f, 1.3f)), st.stone, st.accent);
                    mb.Sphere(T * M(o + Vector3.up * (h + 1.6f), Vector3.zero, Vector3.one * 0.55f), st.glow, 6, 5);
                    break;
                case 8:   // the ribs of a wrecked ship
                    mb.Cylinder(T * M(o + Vector3.up * (h * 0.45f), new Vector3(-20, yawDeg, 0), new Vector3(0.6f, h + 0.6f, 0.6f)), st.dark, st.stone, 6, 0.7f, true);
                    mb.Sphere(T * M(o + Vector3.up * 0.3f, Vector3.zero, new Vector3(1.3f, 0.6f, 1.3f)), st.accent, 6, 4);
                    if (i % 3 == 0) mb.Sphere(T * M(o + Quaternion.Euler(yaw) * new Vector3(0, h * 0.55f, -0.9f), Vector3.zero, Vector3.one * 0.4f), st.glow, 6, 5);
                    break;
                default:  // tall storm-pillars tipped with gold rods
                    mb.Cylinder(T * M(o + Vector3.up * (h * 0.5f + 1.2f), yaw, new Vector3(1.3f, h + 2.4f, 1.3f)), st.dark, st.stone, 8, 0.8f, true);
                    mb.Cylinder(T * M(o + Vector3.up * (h + 2.5f), yaw, new Vector3(1.7f, 0.3f, 1.7f)), st.stone, st.stone, 8, 1f, true);
                    mb.Cone(T * M(o + Vector3.up * (h + 3.7f), yaw, new Vector3(0.3f, 2.2f, 0.3f)), st.accent, 6);
                    mb.Sphere(T * M(o + Vector3.up * (h + 4.9f), Vector3.zero, Vector3.one * 0.4f), st.glow, 6, 5);
                    break;
            }
        }

        // ============================================================ FIGHT STATE
        public Warden Active { get; private set; }
        CreatureActor boss;
        int phase;
        WardenMech mech;
        float poolTimer, barrageTimer, addTimer, shieldTimer, retargetTimer, pulseTimer;
        bool shieldOnGuards, returning;
        const float StrayRadius = 30f, HomeRadius = 10f;
        int addsSpawned;
        readonly List<CreatureActor> adds = new List<CreatureActor>();

        class Zone { public Vector3 pos; public float r, fuse, life, tick; public bool pool; public Transform disc; }
        readonly List<Zone> zones = new List<Zone>();
        readonly Stack<Transform> discPool = new Stack<Transform>();
        Mesh warnMesh, poolMesh;

        public Warden InReach(Vector3 p)
        {
            foreach (var w in All)
            {
                float dx = p.x - w.shrine.x, dz = p.z - w.shrine.z;
                if (dx * dx + dz * dz < 4.2f * 4.2f) return w;
            }
            return null;
        }

        public string Challenge(Warden w)
        {
            var gm = GameManager.I;
            if (Active != null) return "A Warden is already awake.";
            if (Tamers.I != null && Tamers.I.Active != null) return "Finish your match first.";
            if (Caves.InCave) return "Not from here.";
            if (w.final && Sigils < SigilsNeeded) return $"The Aerie is sealed. {Sigils}/{SigilsNeeded} Sigils.";
            if (Cooldown(w) > 0) return $"{w.Name} is resting.";
            var sp = w.Sp;
            if (sp == null) return "Nothing answers.";

            Active = w;
            phase = 1; mech = WardenMech.None;
            poolTimer = barrageTimer = addTimer = 0f; shieldTimer = 1.5f; shieldOnGuards = false; returning = false; addsSpawned = 0;
            // legend-sized bosses are already enormous: no alpha size-up for them (their health makes up for it below)
            bool giant = sp.size >= 2f;
            var d = CreatureFactory.RollWild(sp, w.level, w.biome, !giant);
            d.shimmer = false; d.traits.Remove("radiant");
            Vector3 p = w.pos; p.y = Ground.Height(p.x, p.z);
            boss = CreatureSpawner.I.SpawnActor(d, CreatureMode.Wild, p);
            var comp = gm.Party.Companion;
            boss.MakeBoss(w.pos, comp != null && comp.IsAlive ? (ICombatant)comp : gm.Player, HpMul * (giant ? 1.35f : 1f));
            ReadyBossSeed(w);
            boss.Invulnerable = true;
            boss.BossAtkMul = BaseAtk;
            MakeDiscs(w);
            Fx.Burst(p + Vector3.up * 2f, w.color, 60, 9f, 0.5f, 1.2f, true);
            SynthAudio.Play(Sfx.Summon, 0.6f);
            SynthAudio.Play(Sfx.HitHeavy, 0.7f, 0.8f);
            GameEvents.Warn($"{w.taunt}  {w.Name}, Warden of {w.land}, wakes!  Leave the ring and it goes back to sleep.");
            return null;
        }

        void MakeDiscs(Warden w)
        {
            Color hz = StatusState.Color(w.hazard);
            Color32 pool = Color.Lerp(hz, Color.black, 0.25f), poolRim = hz;
            Color32 warn = new Color(1f, 0.82f, 0.3f), warnRim = new Color(1f, 0.3f, 0.2f);
            var mb = new MeshBuilder();
            mb.Cylinder(Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1f, 0.05f, 1f)), poolRim, pool, 20, 1f, true);
            poolMesh = mb.ToMesh("warden_pool");
            mb.Clear();
            mb.Cylinder(Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1f, 0.05f, 1f)), warnRim, warn, 20, 1f, true);
            mb.Cylinder(Matrix4x4.TRS(new Vector3(0, 0.03f, 0), Quaternion.identity, new Vector3(0.55f, 0.05f, 0.55f)), warnRim, warnRim, 16, 1f, true);
            warnMesh = mb.ToMesh("warden_warn");
        }

        Transform Disc(Mesh m)
        {
            Transform t;
            if (discPool.Count > 0) t = discPool.Pop();
            else
            {
                var go = new GameObject("WardenZone");
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = RenderAssets.Building;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                t = go.transform;
            }
            t.GetComponent<MeshFilter>().sharedMesh = m;
            t.gameObject.SetActive(true);
            return t;
        }

        void AddZone(Vector3 at, float r, float fuse, bool pool)
        {
            Vector3 d = at - Active.pos; d.y = 0;
            if (d.magnitude > ArenaRadius - 1.5f) at = Active.pos + d.normalized * (ArenaRadius - 1.5f);
            at.y = Ground.Height(at.x, at.z) + 0.06f;
            var z = new Zone { pos = at, r = r, fuse = fuse, life = pool ? 11f : 0.25f, pool = pool, disc = Disc(pool ? poolMesh : warnMesh) };
            z.disc.position = at;
            z.disc.localScale = new Vector3(r * 2f, 1f, r * 2f);
            zones.Add(z);
        }

        // ============================================================ TICK
        public void Tick(float dt, PlayerController pl)
        {
            if (Active == null) return;
            var w = Active;
            Vector3 pp = pl.Position;
            if (!pl.IsAlive) { End(false, $"{w.Name} settles back to sleep."); return; }
            Vector3 off = pp - w.pos; off.y = 0;
            if (off.sqrMagnitude > LeaveRadius * LeaveRadius || Caves.InCave) { End(false, $"You left the ring. {w.Name} goes back to sleep."); return; }
            if (boss == null || !boss.gameObject.activeSelf) { End(false, null); return; }
            if (boss.State == AIState.Dead || boss.Hp <= 0f) { End(true, null); return; }

            var comp = GameManager.I.Party.Companion;
            bool compUp = comp != null && comp.IsAlive;
            float frac = boss.Hp / boss.MaxHp;

            // ---- phases ----
            if (phase == 1 && frac <= 0.66f) EnterPhase(2, w.phase2);
            else if (phase == 2 && frac <= 0.33f) EnterPhase(3, w.phase3 | w.phase2 & ~(WardenMech.Shield | WardenMech.Adds));

            // ---- a Warden holds its ground: dragged too far from the ring, it shrugs off the fight and walks back ----
            Vector3 stray = boss.Position - w.pos; stray.y = 0;
            if (!returning && stray.sqrMagnitude > StrayRadius * StrayRadius)
            {
                returning = true;
                boss.Migrate(w.pos);
                GameEvents.Say($"{w.Name} turns back to its ring.", new Color(0.8f, 0.9f, 1f));
            }
            else if (returning && stray.sqrMagnitude < HomeRadius * HomeRadius)
            {
                returning = false;
                boss.Provoke(compUp ? (ICombatant)comp : pl);
            }

            // ---- keep it fighting someone ----
            retargetTimer -= dt;
            if (retargetTimer <= 0f)
            {
                retargetTimer = 0.8f;
                if (returning) { if (boss.State != AIState.Return) boss.Migrate(w.pos); }
                else
                {
                    if (boss.Target == null || !boss.Target.IsAlive) boss.Target = compUp ? (ICombatant)comp : pl;
                    boss.Aggro = true;
                }
                for (int i = adds.Count - 1; i >= 0; i--)
                {
                    var a = adds[i];
                    if (a == null || !a.gameObject.activeSelf || a.State == AIState.Dead) { adds.RemoveAt(i); continue; }
                    if (a.Target == null || !a.Target.IsAlive) a.Target = compUp ? (ICombatant)comp : pl;
                    a.Aggro = true;
                }
            }

            // ---- shield ----
            if (shieldTimer > 0f || shieldOnGuards)
            {
                shieldTimer -= dt;
                if (shieldOnGuards && (adds.Count == 0 || shieldTimer < -30f)) { shieldOnGuards = false; GameEvents.Good($"{w.Name}'s shield breaks!"); }
                boss.Invulnerable = shieldTimer > 0f || shieldOnGuards || returning;
                pulseTimer -= dt;
                if (pulseTimer <= 0f) { pulseTimer = 0.25f; Fx.Sparkle(boss.Position + Vector3.up * boss.Radius, new Color(0.8f, 0.9f, 1f), 6, boss.Radius * 1.6f); }
            }
            else boss.Invulnerable = returning;

            // ---- guards ----
            if ((mech & WardenMech.Adds) != 0)
            {
                addTimer -= dt;
                if (addTimer <= 0f && adds.Count < 2 && addsSpawned < 7) { addTimer = 18f; SpawnGuard(pl); }
            }
            if ((mech & WardenMech.Regen) != 0 && adds.Count > 0) boss.BossHeal(boss.MaxHp * 0.008f * dt);

            // ---- hazards ----
            if ((mech & WardenMech.Pools) != 0)
            {
                poolTimer -= dt;
                if (poolTimer <= 0f)
                {
                    poolTimer = phase >= 3 ? 4.5f : 6f;
                    AddZone(pp, 3.2f, 1.1f, true);
                    if (compUp && phase >= 3) AddZone(comp.Position, 3f, 1.1f, true);
                }
            }
            if ((mech & WardenMech.Barrage) != 0)
            {
                barrageTimer -= dt;
                if (barrageTimer <= 0f)
                {
                    barrageTimer = boss.Invulnerable ? 3.2f : (phase >= 3 ? 4.5f : 6f);
                    AddZone(pp + pl.transform.forward * Random.Range(0f, 1.5f), 2.8f, 1.5f, false);
                    if (compUp) AddZone(comp.Position, 2.6f, 1.5f, false);
                    Vector2 r = Random.insideUnitCircle * 7f;
                    AddZone(pp + new Vector3(r.x, 0, r.y), 2.6f, 1.7f, false);
                    if (phase >= 3) { r = Random.insideUnitCircle * 9f; AddZone(pp + new Vector3(r.x, 0, r.y), 2.6f, 1.9f, false); }
                    SynthAudio.Play(Sfx.Whoosh, 0.7f, 0.6f);
                }
            }
            TickZones(dt, w, pl, comp, compUp);

            // ---- HUD ----
            string state = returning ? UIFactory.Col("  RETURNING TO ITS RING", new Color(0.7f, 0.9f, 1f))
                         : boss.Invulnerable ? UIFactory.Col("  SHIELDED", new Color(0.7f, 0.9f, 1f))
                         : (mech & WardenMech.Regen) != 0 && adds.Count > 0 ? UIFactory.Col("  HEALING - drop its guards", new Color(0.6f, 1f, 0.6f))
                         : (mech & WardenMech.Enrage) != 0 ? UIFactory.Col("  ENRAGED", new Color(1f, 0.5f, 0.4f)) : "";
            HUD.I.SetBoss($"{w.Name}  <size=19>Lv {w.level}  ·  phase {phase}/3</size>{state}", frac, Color.Lerp(w.color, new Color(0.95f, 0.3f, 0.4f), 0.35f));
        }

        void EnterPhase(int p, WardenMech m)
        {
            var w = Active;
            phase = p;
            mech = m;
            shieldTimer = 2f;
            shieldOnGuards = false;
            boss.BossAtkMul = (m & WardenMech.Enrage) != 0 ? EnrageAtk : BaseAtk;
            addTimer = 0.6f; poolTimer = 2.5f; barrageTimer = 2f; addsSpawned = 0;
            if ((m & WardenMech.Shield) != 0)
            {
                if ((m & WardenMech.Adds) != 0)
                {
                    shieldOnGuards = true;
                    SpawnGuard(GameManager.I.Player); SpawnGuard(GameManager.I.Player);
                    addTimer = 18f;
                }
                else shieldTimer = 9f;
            }
            else if ((m & WardenMech.Adds) != 0) { SpawnGuard(GameManager.I.Player); if (p >= 3) SpawnGuard(GameManager.I.Player); addTimer = 18f; }
            Fx.Burst(boss.Position + Vector3.up * boss.Radius, w.color, 50, 10f, 0.5f, 1f, true);
            SynthAudio.Play(Sfx.HitHeavy, 0.6f, 0.9f);
            SynthAudio.Play(Sfx.Burst, 0.7f, 0.8f);
            GameEvents.Warn($"{w.Name} - phase {p}: {Describe(m, w)}");
        }

        void SpawnGuard(PlayerController pl)
        {
            var w = Active;
            var sp = ContentDB.Sp(w.guard);
            if (sp == null) return;
            var d = CreatureFactory.RollWild(sp, Mathf.Max(1, w.level - 5), w.biome, false);
            d.shimmer = false; d.traits.Remove("radiant");
            float a = Random.value * Mathf.PI * 2f;
            Vector3 p = w.pos + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * Random.Range(5f, 11f);
            p.y = Ground.Height(p.x, p.z);
            var g = CreatureSpawner.I.SpawnActor(d, CreatureMode.Wild, p);
            var comp = GameManager.I.Party.Companion;
            g.MakeDuelist(w.pos, comp != null && comp.IsAlive && Random.value < 0.5f ? (ICombatant)comp : pl);
            adds.Add(g);
            addsSpawned++;
            Fx.Sparkle(p + Vector3.up, w.color, 14, 0.8f);
            SynthAudio.Play(Sfx.Summon, 1.1f, 0.6f);
        }

        void TickZones(float dt, Warden w, PlayerController pl, CreatureActor comp, bool compUp)
        {
            Color hz = StatusState.Color(w.hazard);
            for (int i = zones.Count - 1; i >= 0; i--)
            {
                var z = zones[i];
                if (z.fuse > 0f)
                {
                    z.fuse -= dt;
                    float k = 1f + Mathf.Sin(Time.time * 18f) * 0.05f;
                    z.disc.localScale = new Vector3(z.r * 2f * k, 1f, z.r * 2f * k);
                    if (z.fuse > 0f) continue;
                    if (!z.pool)
                    {
                        // the strike lands
                        Fx.Burst(z.pos + Vector3.up * 0.4f, hz, 26, 7f, 0.4f, 0.6f, true);
                        SynthAudio.Play(Sfx.Burst, 0.9f, 0.7f);
                        HitInside(z, w, pl, comp, compUp, 0.16f, 0.14f, 0.6f, 5f);
                    }
                    z.disc.localScale = new Vector3(z.r * 2f, 1f, z.r * 2f);
                    continue;
                }
                z.life -= dt;
                if (z.pool)
                {
                    z.tick -= dt;
                    if (z.tick <= 0f)
                    {
                        z.tick = 1f;
                        Vector2 r = Random.insideUnitCircle * z.r;
                        Fx.Mote(z.pos + new Vector3(r.x, 0.1f, r.y), Vector3.up * 1.2f, hz, 0.16f, 0.9f, true);
                        HitInside(z, w, pl, comp, compUp, 0.05f, 0.06f, 0.5f, 0f);
                    }
                }
                if (z.life <= 0f) { Release(z); zones.RemoveAt(i); }
            }
        }

        void HitInside(Zone z, Warden w, PlayerController pl, CreatureActor comp, bool compUp, float playerFrac, float kinFrac, float statusChance, float knock)
        {
            var el = w.Sp != null ? w.Sp.element1 : Element.Normal;
            Vector3 d = pl.Position - z.pos; d.y = 0;
            if (pl.IsAlive && d.magnitude < z.r + 0.3f)
                pl.ReceiveHit(new HitInfo { attacker = boss, damage = pl.Stats.MaxHp * playerFrac, element = el, status = w.hazard, statusChance = statusChance,
                    knockback = knock > 0f ? (d.sqrMagnitude > 0.01f ? d.normalized : Vector3.forward) * knock : Vector3.zero, effectiveness = 1f });
            if (!compUp) return;
            d = comp.Position - z.pos; d.y = 0;
            if (d.magnitude < z.r + comp.Radius * 0.5f)
                comp.ReceiveHit(new HitInfo { attacker = boss, damage = comp.MaxHp * kinFrac, element = el, status = w.hazard, statusChance = statusChance, effectiveness = 1f });
        }

        void Release(Zone z)
        {
            z.disc.gameObject.SetActive(false);
            discPool.Push(z.disc);
        }

        // ============================================================ END
        void End(bool won, string msg)
        {
            var w = Active;
            Active = null;
            if (seedBefore != null) { S.selectedSeed = seedBefore; seedBefore = null; }
            foreach (var z in zones) Release(z);
            zones.Clear();
            foreach (var a in adds) if (a != null && a.gameObject.activeSelf && a.State != AIState.Dead) a.Despawn();
            adds.Clear();
            if (boss != null && !won && boss.gameObject.activeSelf && boss.State != AIState.Dead) boss.Despawn();
            boss = null;
            if (warnMesh != null) Destroy(warnMesh);
            if (poolMesh != null) Destroy(poolMesh);
            if (HUD.I != null) HUD.I.SetBoss(null, 0f, Color.white);
            if (!won) { if (msg != null) GameEvents.Warn(msg); return; }

            var s = S;
            var gm = GameManager.I;
            bool first = Wins(w) == 0;
            int purse = Purse(w), hearts = Hearts(w);
            var rec = s.wardenWins.Find(r => r.id == w.id);
            if (rec == null) { rec = new WardenRecord { id = w.id }; s.wardenWins.Add(rec); }
            rec.wins++;
            rec.unix = Expeditions.Now;
            s.glimmer += purse;
            Vector3 at = gm.Player.Position + Vector3.up;
            gm.GiveItem(HeartItem, hearts, at);
            gm.GiveItem("starbloom_seed", first ? 2 + w.level / 12 : 1, at);
            gm.GiveItem("kin_treat", first ? 3 : 1, at);
            string bossSeed = SeedFor(w.species);
            if (bossSeed != null && !caughtNow) gm.GiveItem(bossSeed, first ? 2 : 1, at);   // beaten, not bound: it leaves the means to bind it next time
            gm.AddPlayerXp((150 + w.level * 20) / (first ? 1 : 3));
            SynthAudio.Play(Sfx.LevelUp, 0.8f, 1f);
            Fx.Burst(w.pos + Vector3.up * 2f, w.color, 80, 10f, 0.5f, 1.6f, true);
            Fx.Sparkle(gm.Player.Position + Vector3.up * 1.5f, new Color(1f, 0.9f, 0.5f), 50, 1.6f);
            if (caughtNow) GameEvents.Good($"{w.Name} is bound - the Kraken sails with you now. +{purse} Glimmer, {hearts} Warden Hearts. Another will rise from the deep in time.");
            else if (!first) GameEvents.Good($"{w.Name} falls again. +{purse} Glimmer, {hearts} Warden Heart.");
            else if (w.final)
            {
                var sp = w.Sp;
                var d = CreatureFactory.RollWild(sp, 50, BiomeId.Peaks, false);
                gm.OnKinCaptured(d);
                GameEvents.Good($"Stormcrest bows its head and comes with you. Your journey across Rimewild is complete - +{purse} Glimmer, {hearts} Warden Hearts. The Wardens will fight you again whenever you like.");
            }
            else if (w.optional) GameEvents.Good($"The Kraken sinks back into the deep, leaving {ContentDB.ItemName(KrakenSeed)}s behind: next time, get it to {CaptureSystem.BossSeedHp * 100f:0}% health and throw one. +{purse} Glimmer, {hearts} Warden Hearts.");
            else
            {
                int n = Sigils;
                GameEvents.Good($"Sigil of {w.land} earned!  {n}/{SigilsNeeded}  ·  +{purse} Glimmer, {hearts} Warden Hearts." +
                                (n >= SigilsNeeded ? "  Stormcrest's Aerie has opened." : ""));
            }
            GameEvents.RaiseInventoryChanged();
            GameEvents.RaiseRosterChanged();
        }

        /// <summary>Called on death / respawn so a fight never dangles.</summary>
        public void Abort() { if (Active != null) End(false, null); }
    }
}
