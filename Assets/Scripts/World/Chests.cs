using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    public enum ChestTier { Wooden, Iron, Gilded }

    /// <summary>One opened cell: how many times its chest has been found, and when the next one appears.</summary>
    [Serializable]
    public class ChestState
    {
        public int cell;
        public int seq;
        public double readyAt;
    }

    /// <summary>
    /// Treasure chests scattered over every land, easy to spot: a beam of light over each, and a chest icon on the
    /// minimap once you are within reach of it.
    ///
    /// The world is a grid of `Cell`-metre squares and every square with enough dry ground holds one chest. Where it
    /// stands and what tier it is come from the world seed, the square and how many times that square's chest has been
    /// opened (`seq`), so nothing is stored for a chest nobody has touched. Opening one bumps `seq`: after
    /// `RespawnSeconds` a new chest stands somewhere else in the same square (never within 40 m of the old spot).
    /// Only the squares round the player have objects.
    ///
    /// Tiers: Wooden and Iron open freely; Gilded needs a Chest Key (found in lower chests, on alphas and tamers, or
    /// crafted). Loot follows the land: seeds by how far out it is, the land's own ore, Prism Shards from Iron up,
    /// Glimmer, and blueprints - recipes that can only be made once their blueprint has been found in that land
    /// (`Blueprints`, `Locked`). A blueprint is learned once and for good; a land with none left gives extra shards.
    /// </summary>
    public class Chests : MonoBehaviour
    {
        public static Chests I;
        public const string KeyItem = "chest_key";
        const float Cell = 180f, See = 240f, Reach = 2.5f, RespawnSeconds = 240f, MarkRange = 230f;
        static int Side => Mathf.CeilToInt(WorldGen.HalfWorld * 2f / Cell);

        // ---------------------------------------------------------------- blueprints
        public struct Blueprint { public string recipe; public BiomeId land; }
        /// <summary>Chest-only recipes: (recipe id = item id, the land whose chests hold it). Items are in ContentSeed.Chests.cs.</summary>
        public static readonly Blueprint[] Blueprints =
        {
            B("meadow_charm", BiomeId.Meadow), B("forager_band", BiomeId.Meadow),
            B("tidecaller_bangle", BiomeId.Isles), B("coral_ring", BiomeId.Isles),
            B("cinder_signet", BiomeId.Badlands), B("ashwalker_amulet", BiomeId.Badlands),
            B("rimeglass_ring", BiomeId.Peaks), B("aurora_pendant", BiomeId.Peaks),
            B("mire_bracer", BiomeId.Swamp), B("lantern_ring", BiomeId.Swamp),
            B("sunscour_band", BiomeId.Desert), B("oasis_amulet", BiomeId.Desert),
            B("blightfang_ring", BiomeId.Blight), B("miasma_ward", BiomeId.Blight),
            B("starlight_bracelet", BiomeId.Glow), B("nebula_ring", BiomeId.Glow),
        };
        static Blueprint B(string r, BiomeId l) => new Blueprint { recipe = r, land = l };

        static PlayerStats S => GameManager.I.Player.Stats;

        public static bool IsBlueprint(string recipeId, out BiomeId land)
        {
            foreach (var b in Blueprints) if (b.recipe == recipeId) { land = b.land; return true; }
            land = BiomeId.Meadow;
            return false;
        }

        /// <summary>A recipe whose blueprint has not been found yet.</summary>
        public static bool Locked(RecipeDef r) => r != null && IsBlueprint(r.id, out _) && !S.blueprints.Contains(r.id);

        /// <summary>"Blueprint: found in chests in Driftkey Isles." (null if this recipe needs none)</summary>
        public static string BlueprintNote(RecipeDef r) => r != null && IsBlueprint(r.id, out var land) ? $"Blueprint: found in chests in {BiomeInfo.Name(land)}." : null;

        public static int BlueprintsFound(BiomeId land, out int total)
        {
            int n = 0; total = 0;
            foreach (var b in Blueprints) if (b.land == land) { total++; if (S.blueprints.Contains(b.recipe)) n++; }
            return n;
        }

        // ---------------------------------------------------------------- live chests
        class Live
        {
            public int cell, seq;
            public Vector3 pos;
            public ChestTier tier;
            public BiomeId biome;
            public GameObject go;
            public Transform lid, beam;
            public Obstacle obstacle;
            public bool opened;
            public float openT;
        }

        readonly Dictionary<int, Live> live = new Dictionary<int, Live>();
        readonly Dictionary<int, ChestState> states = new Dictionary<int, ChestState>();
        readonly List<int> scratch = new List<int>();
        static readonly Mesh[] bodyMesh = new Mesh[3], lidMesh = new Mesh[3];
        static readonly Mesh[] beamMesh = new Mesh[3];
        float scan;

        public static readonly Color[] TierColor = { new Color(0.95f, 0.75f, 0.45f), new Color(0.7f, 0.85f, 1f), new Color(1f, 0.86f, 0.3f) };
        public static readonly string[] TierName = { "Wooden chest", "Iron chest", "Gilded chest" };

        PlayerStats synced;

        public void Init() { I = this; }

        /// <summary>The opened squares are in the save; re-read them whenever a different save is in play.</summary>
        void Sync()
        {
            if (synced == S) return;
            synced = S;
            states.Clear();
            foreach (var st in S.chests) states[st.cell] = st;
            ClearLive();
        }

        static int CellOf(float x, float z)
        {
            int cx = Mathf.Clamp(Mathf.FloorToInt((x + WorldGen.HalfWorld) / Cell), 0, Side - 1);
            int cz = Mathf.Clamp(Mathf.FloorToInt((z + WorldGen.HalfWorld) / Cell), 0, Side - 1);
            return cz * Side + cx;
        }

        static bool Clear(Vector3 p)
        {
            if (Settlements.Inside(p, 30f) || Wardens.Inside(p, 14f) || Caves.BlocksNode(p) || Outposts.BlocksNode(p) || Pois.BlocksNode(p) || Landmarks.NearLandmark(p, 14f)) return false;
            var bm = BaseManager.I;
            return bm == null || !bm.HasBase || !bm.IsInsideBase(p, 8f);
        }

        /// <summary>Where this square's chest stands for a given count of openings, and its tier. False = no dry, level, free ground there.</summary>
        static bool Place(int cell, int seq, Vector3? avoid, out Vector3 pos, out ChestTier tier)
        {
            var rng = new System.Random(unchecked(WorldGen.Seed * 73856093 ^ cell * 19349663 ^ seq * 83492791));
            float x0 = (cell % Side) * Cell - WorldGen.HalfWorld, z0 = (cell / Side) * Cell - WorldGen.HalfWorld;
            double roll = rng.NextDouble();
            pos = Vector3.zero; tier = ChestTier.Wooden;
            for (int i = 0; i < 16; i++)
            {
                float x = x0 + (float)rng.NextDouble() * Cell, z = z0 + (float)rng.NextDouble() * Cell;
                float h = WorldGen.Height(x, z);
                if (h < Tides.Surface(x, z) + 0.8f) continue;
                float sx = Mathf.Abs(WorldGen.Height(x + 1.5f, z) - WorldGen.Height(x - 1.5f, z)), sz = Mathf.Abs(WorldGen.Height(x, z + 1.5f) - WorldGen.Height(x, z - 1.5f));
                if (sx > 1.1f || sz > 1.1f) continue;
                var p = new Vector3(x, h, z);
                if (avoid.HasValue && (p - avoid.Value).sqrMagnitude < 40f * 40f) continue;
                if (!Clear(p)) continue;
                pos = p;
                bool starter = WorldGen.StartDistance(x, z) < WorldGen.StarterZone;
                tier = starter ? (roll < 0.15 ? ChestTier.Iron : ChestTier.Wooden) : roll < 0.08 ? ChestTier.Gilded : roll < 0.38 ? ChestTier.Iron : ChestTier.Wooden;
                return true;
            }
            return false;
        }

        public void Tick(float dt, PlayerController pl)
        {
            Sync();
            if (Caves.InCave) { if (live.Count > 0) ClearLive(); return; }
            Vector3 pp = pl.Position;
            scan -= dt;
            if (scan <= 0f)
            {
                scan = 0.5f;
                double now = GameManager.WorldTime;
                int cx = Mathf.FloorToInt((pp.x + WorldGen.HalfWorld) / Cell), cz = Mathf.FloorToInt((pp.z + WorldGen.HalfWorld) / Cell);
                for (int dz = -1; dz <= 1; dz++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int x = cx + dx, z = cz + dz;
                        if (x < 0 || z < 0 || x >= Side || z >= Side) continue;
                        int cell = z * Side + x;
                        if (live.ContainsKey(cell)) continue;
                        states.TryGetValue(cell, out var st);
                        if (st != null && st.readyAt > now) continue;   // found a while ago: the next one has not turned up yet
                        int seq = st != null ? st.seq : 0;
                        Vector3? avoid = null;
                        if (seq > 0 && Place(cell, seq - 1, null, out var old, out _)) avoid = old;
                        if (!Place(cell, seq, avoid, out var pos, out var tier)) continue;
                        if ((pos - pp).sqrMagnitude > See * See) continue;
                        live[cell] = Build(cell, seq, pos, tier);
                    }
                scratch.Clear();
                foreach (var kv in live) if ((kv.Value.pos - pp).sqrMagnitude > (See + 40f) * (See + 40f) || (kv.Value.opened && kv.Value.openT > 7f)) scratch.Add(kv.Key);
                foreach (int c in scratch) { Drop(live[c]); live.Remove(c); }
            }
            foreach (var l in live.Values)
            {
                if (l.opened)
                {
                    l.openT += dt;
                    if (l.lid != null) l.lid.localRotation = Quaternion.Euler(-Mathf.Min(110f, l.openT * 320f), 0, 0);
                    if (l.beam != null) l.beam.localScale = new Vector3(1f, Mathf.Max(0f, 1f - l.openT * 2f), 1f);
                    continue;
                }
                if (l.beam != null) l.beam.localRotation = Quaternion.Euler(0, Time.time * 40f, 0);
                if (UnityEngine.Random.value < dt * 1.6f && (l.pos - pp).sqrMagnitude < 60f * 60f)
                    Fx.Mote(l.pos + new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 0.7f, UnityEngine.Random.Range(-0.4f, 0.4f)), Vector3.up * 0.9f, TierColor[(int)l.tier], 0.12f, 1.1f, true);
            }
        }

        void ClearLive()
        {
            foreach (var l in live.Values) Drop(l);
            live.Clear();
        }

        static void Drop(Live l)
        {
            WorldCollision.RemoveObstacle(l.obstacle);
            if (l.go != null) Destroy(l.go);
        }

        // ---------------------------------------------------------------- meshes
        static Matrix4x4 M(float x, float y, float z, float sx, float sy, float sz, float rx = 0, float ry = 0, float rz = 0) =>
            Matrix4x4.TRS(new Vector3(x, y, z), Quaternion.Euler(rx, ry, rz), new Vector3(sx, sy, sz));
        static Color32 C(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

        static void EnsureMeshes()
        {
            if (bodyMesh[0] != null) return;
            string[] wood = { "#70482a", "#5a6474", "#d8a83a" }, top = { "#8a5c34", "#7c8798", "#f2cf5a" }, band = { "#d8b860", "#2c323c", "#b8322e" };
            for (int t = 0; t < 3; t++)
            {
                float w = 1.0f + t * 0.12f, h = 0.56f + t * 0.05f, d = 0.7f + t * 0.06f;
                var mb = new MeshBuilder();
                mb.Box(M(0, h * 0.5f, 0, w, h, d), C(wood[t]), C(top[t]));
                mb.Box(M(-w * 0.34f, h * 0.5f, 0, 0.09f, h + 0.02f, d + 0.03f), C(band[t]));
                mb.Box(M(w * 0.34f, h * 0.5f, 0, 0.09f, h + 0.02f, d + 0.03f), C(band[t]));
                mb.Box(M(0, h * 0.62f, -d * 0.5f - 0.02f, 0.2f, 0.24f, 0.05f), C(t == 2 ? "#fff0a0" : "#e8d090"));   // the lock
                bodyMesh[t] = mb.ToMesh("chest_body_" + t);
                var lb = new MeshBuilder();
                lb.Box(M(0, 0.11f, d * 0.5f, w + 0.04f, 0.22f, d + 0.04f), C(top[t]), C(wood[t]));
                lb.Box(M(-w * 0.34f, 0.115f, d * 0.5f, 0.1f, 0.25f, d + 0.07f), C(band[t]));
                lb.Box(M(w * 0.34f, 0.115f, d * 0.5f, 0.1f, 0.25f, d + 0.07f), C(band[t]));
                if (t == 2) lb.Sphere(M(0, 0.27f, d * 0.5f, 0.2f, 0.16f, 0.2f), C("#ff5a7a"), C("#c02848"), 8, 5);   // a gem on the gilded lid
                lidMesh[t] = lb.ToMesh("chest_lid_" + t);
            }
            // the beam: two crossed upright quads in the tier's colour, bright at the foot and fading to nothing at the top
            // (the particle shader takes its colour from the vertices and its shape from a soft dot: every corner reads the dot's middle)
            for (int t = 0; t < 3; t++)
            {
                var verts = new List<Vector3>(); var cols = new List<Color>(); var uvs = new List<Vector2>(); var tris = new List<int>();
                Color lit = TierColor[t]; lit.a = 0.6f;
                Color off = TierColor[t]; off.a = 0f;
                for (int q = 0; q < 2; q++)
                {
                    Vector3 side = q == 0 ? Vector3.right : Vector3.forward;
                    int b0 = verts.Count;
                    verts.Add(-side * 0.24f); verts.Add(side * 0.24f); verts.Add(side * 0.1f + Vector3.up * 16f); verts.Add(-side * 0.1f + Vector3.up * 16f);
                    cols.Add(lit); cols.Add(lit); cols.Add(off); cols.Add(off);
                    for (int k = 0; k < 4; k++) uvs.Add(new Vector2(0.5f, 0.5f));
                    tris.AddRange(new[] { b0, b0 + 2, b0 + 1, b0, b0 + 3, b0 + 2, b0, b0 + 1, b0 + 2, b0, b0 + 2, b0 + 3 });
                }
                var m = new Mesh { name = "chest_beam_" + t };
                m.SetVertices(verts); m.SetColors(cols); m.SetUVs(0, uvs); m.SetTriangles(tris, 0); m.RecalculateBounds();
                beamMesh[t] = m;
            }
        }

        Live Build(int cell, int seq, Vector3 pos, ChestTier tier)
        {
            EnsureMeshes();
            int t = (int)tier;
            var rng = new System.Random(cell * 31 + seq);
            var go = new GameObject(TierName[t]);
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0));
            go.AddComponent<MeshFilter>().sharedMesh = bodyMesh[t];
            go.AddComponent<MeshRenderer>().sharedMaterial = RenderAssets.Creature;
            float h = 0.56f + t * 0.05f, d = 0.7f + t * 0.06f;
            var lid = new GameObject("Lid");
            lid.transform.SetParent(go.transform, false);
            lid.transform.localPosition = new Vector3(0, h, -d * 0.5f);
            lid.AddComponent<MeshFilter>().sharedMesh = lidMesh[t];
            lid.AddComponent<MeshRenderer>().sharedMaterial = RenderAssets.Creature;
            var beam = new GameObject("Beam");
            beam.transform.SetParent(go.transform, false);
            beam.transform.localPosition = new Vector3(0, h + 0.2f, 0);
            beam.AddComponent<MeshFilter>().sharedMesh = beamMesh[t];
            var br = beam.AddComponent<MeshRenderer>();
            br.sharedMaterial = RenderAssets.ParticleAdd;
            br.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return new Live { cell = cell, seq = seq, pos = pos, tier = tier, biome = WorldGen.Biome(pos.x, pos.z), go = go, lid = lid.transform, beam = beam.transform,
                              obstacle = WorldCollision.AddObstacle(pos, 0.6f, go) };
        }

        // ---------------------------------------------------------------- finding and opening
        /// <summary>Chests within marker range of a point that are still shut (for the minimap).</summary>
        public void Marks(Vector3 pp, List<Vector3> pos, List<int> tiers)
        {
            foreach (var l in live.Values)
            {
                if (l.opened || (l.pos - pp).sqrMagnitude > MarkRange * MarkRange) continue;
                pos.Add(l.pos); tiers.Add((int)l.tier);
            }
        }

        Live Near(Vector3 pp)
        {
            foreach (var l in live.Values)
            {
                if (l.opened) continue;
                float dx = l.pos.x - pp.x, dz = l.pos.z - pp.z;
                if (dx * dx + dz * dz < Reach * Reach) return l;
            }
            return null;
        }

        static bool NeedsKey(Live l) => l.tier == ChestTier.Gilded && !WorldRules.Current.simpleControls;

        public string Prompt(Vector3 pp, out Action act)
        {
            act = null;
            var l = Near(pp);
            if (l == null) return null;
            if (NeedsKey(l) && S.Bag.Count(KeyItem) <= 0)
            {
                act = () => GameEvents.Warn("A Gilded chest needs a Chest Key. Keys turn up in other chests, on alphas and rival tamers, or can be made at a Workbench.");
                return "LOCKED\nneeds a key";
            }
            act = () => Open(l);
            return NeedsKey(l) ? "UNLOCK" : "OPEN";
        }

        static string Ore(BiomeId b)
        {
            switch (b)
            {
                case BiomeId.Badlands: return "cinder";
                case BiomeId.Peaks: return "crystal";
                case BiomeId.Isles: return "saltglass";
                case BiomeId.Swamp: return "bog_iron";
                case BiomeId.Desert: return "sunsteel_ore";
                case BiomeId.Blight: return "blight_ore";
                case BiomeId.Glow: return "starcrystal";
                default: return "ore";
            }
        }

        /// <summary>How far out a land is: 0 Sunmeadow and the Isles, 1 Badlands / Peaks / Mirefen, 2 the Dunes, Blightmoor and Glowdeep.</summary>
        static int Rank(BiomeId b) => b == BiomeId.Meadow || b == BiomeId.Isles ? 0 : b == BiomeId.Desert || b == BiomeId.Blight || b == BiomeId.Glow ? 2 : 1;

        static int R(int a, int b) => UnityEngine.Random.Range(a, b + 1);

        void Open(Live l)
        {
            if (l.opened) return;
            var gm = GameManager.I;
            var s = S;
            if (NeedsKey(l)) { if (s.Bag.Remove(KeyItem, 1) <= 0) return; }
            l.opened = true;
            states.TryGetValue(l.cell, out var st);
            if (st == null) { st = new ChestState { cell = l.cell }; states[l.cell] = st; s.chests.Add(st); }
            st.seq = l.seq + 1;
            st.readyAt = GameManager.WorldTime + RespawnSeconds;
            s.chestsOpened++;

            int rank = Rank(l.biome), t = (int)l.tier;
            var loot = new List<(string id, int n)>();
            // seeds
            if (t == 0) { if (rank < 2) loot.Add(("tether_seed", rank == 0 ? R(3, 5) : R(2, 3))); if (rank > 0) loot.Add(("ironbark_seed", rank == 1 ? R(1, 2) : R(2, 3))); }
            else if (t == 1) { loot.Add(("ironbark_seed", rank == 1 ? R(3, 4) : R(2, 3))); if (rank == 0) loot.Add(("tether_seed", 2)); if (rank == 2) loot.Add(("starbloom_seed", R(1, 2))); }
            else { loot.Add(("starbloom_seed", rank == 2 ? R(3, 5) : R(2, 3))); loot.Add(("ironbark_seed", 3)); }
            // the land's ore
            loot.Add((Ore(l.biome), t == 0 ? R(2, 4) : t == 1 ? R(5, 8) : R(9, 14)));
            if (t == 1) loot.Add(("ingot", R(1, 3)));
            // shimmer
            int shards = t == 2 ? R(3, 6) : t == 1 && UnityEngine.Random.value < 0.35f ? R(1, 2) : 0;
            // a blueprint of this land, while any are left to find
            string bp = null;
            if (UnityEngine.Random.value < (t == 0 ? 0.04f : t == 1 ? 0.15f : 0.65f))
            {
                var open = new List<string>();
                foreach (var b in Blueprints) if (b.land == l.biome && !s.blueprints.Contains(b.recipe)) open.Add(b.recipe);
                if (open.Count > 0) bp = open[UnityEngine.Random.Range(0, open.Count)];
                else if (t > 0) shards += 2;
            }
            if (shards > 0) loot.Add((Shimmer.ShardItem, shards));
            if (UnityEngine.Random.value < (t == 0 ? 0.06f : t == 1 ? 0.15f : 0f)) loot.Add((KeyItem, 1));
            int glim = (t == 0 ? R(8, 20) : t == 1 ? R(25, 50) : R(80, 150)) * (1 + rank);

            var parts = new List<string>();
            Vector3 at = l.pos + Vector3.up * 0.8f;
            foreach (var it in loot) { gm.GiveItem(it.id, it.n, at); parts.Add($"{it.n} {ContentDB.ItemName(it.id)}"); }
            s.glimmer += glim;
            parts.Add($"{glim} Glimmer");
            GameEvents.Good($"{TierName[t]}: {string.Join(", ", parts)}");
            if (bp != null)
            {
                s.blueprints.Add(bp);
                GameEvents.Say($"Blueprint found: {ContentDB.ItemName(bp)}! You can now make it at the Workbench.", new Color(0.6f, 0.9f, 1f));
                gm.Confetti(at);
            }
            gm.AddPlayerXp(t == 0 ? 12 : t == 1 ? 30 : 70);
            Fx.Burst(at, TierColor[t], 14 + t * 8, 3.5f, 0.2f, 0.7f, true);
            Fx.Sparkle(at, TierColor[t], 14, 0.8f);
            SynthAudio.Play(t == 2 ? Sfx.LevelUp : Sfx.Pickup, 1f, 0.7f);
            GameEvents.RaiseInventoryChanged();
        }

        /// <summary>For the dev harness: every chest standing near the player right now.</summary>
        public string Describe()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var l in live.Values) sb.Append($"{TierName[(int)l.tier]} in {BiomeInfo.Name(l.biome)} at {l.pos:0}{(l.opened ? " (opened)" : "")}; ");
            return sb.ToString();
        }

        /// <summary>For the dev harness: opens the nearest standing chest whatever its tier (a key is handed over first).</summary>
        public string OpenNearest(Vector3 pp)
        {
            Live best = null; float bd = float.MaxValue;
            foreach (var l in live.Values) { if (l.opened) continue; float d = (l.pos - pp).sqrMagnitude; if (d < bd) { bd = d; best = l; } }
            if (best == null) return "none";
            if (NeedsKey(best) && S.Bag.Count(KeyItem) <= 0) S.Bag.Add(KeyItem, 1);
            Open(best);
            return TierName[(int)best.tier] + " in " + BiomeInfo.Name(best.biome);
        }
    }
}
