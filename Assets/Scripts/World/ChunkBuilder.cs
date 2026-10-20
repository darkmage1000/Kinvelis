using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    public struct PropSpawn
    {
        public PropType type;
        public Vector3 pos;
        public float rotY;
        public float scale;
        public int index; // stable index within chunk => node key
    }

    /// <summary>Output of a background terrain build. Plain arrays only (no Unity objects).</summary>
    public sealed class ChunkMeshData
    {
        public int cx, cz, lod;
        public Vector3[] verts;
        public Vector3[] normals;
        public Color32[] colors;
        public int[] tris;
        public float minY, maxY;
        public List<PropSpawn> props; // null when props were already cached
        public BiomeId dominantBiome;
    }

    /// <summary>Thread-safe builders for terrain meshes and deterministic prop scatter.</summary>
    public static class ChunkBuilder
    {
        public static int ResolutionForLod(int lod) => lod == 0 ? 32 : (lod == 1 ? 16 : 8);

        public static ChunkMeshData BuildTerrain(int cx, int cz, int lod, bool wantProps)
        {
            int res = ResolutionForLod(lod);
            float size = WorldGen.ChunkSize;
            float step = size / res;
            float ox = cx * size, oz = cz * size;

            // heights on a (res+3)^2 grid including a 1-cell border for seamless normals
            int g = res + 3;
            var hgt = new float[g * g];
            var wts = new Vector3[g * g];
            for (int z = 0; z < g; z++)
            for (int x = 0; x < g; x++)
            {
                float wx = ox + (x - 1) * step, wz = oz + (z - 1) * step;
                WorldGen.Weights(wx, wz, out float wm, out float wb, out float wp);
                hgt[z * g + x] = WorldGen.HeightW(wx, wz, wm, wb, wp);
                wts[z * g + x] = new Vector3(wm, wb, wp);
            }

            int n = res + 1;
            int skirtCount = res * 4 + 4;
            var verts = new Vector3[n * n + skirtCount];
            var normals = new Vector3[verts.Length];
            var colors = new Color32[verts.Length];
            float minY = float.MaxValue, maxY = float.MinValue;
            Vector3 biomeSum = Vector3.zero;

            for (int z = 0; z < n; z++)
            for (int x = 0; x < n; x++)
            {
                int gi = (z + 1) * g + (x + 1);
                float h = hgt[gi];
                float hl = hgt[gi - 1], hr = hgt[gi + 1], hd = hgt[gi - g], hu = hgt[gi + g];
                var nrm = new Vector3(hl - hr, 2f * step, hd - hu).normalized;
                int vi = z * n + x;
                verts[vi] = new Vector3(x * step, h, z * step);
                normals[vi] = nrm;
                Vector3 w = wts[gi];
                biomeSum += w;
                colors[vi] = WorldGen.TerrainColor(ox + x * step, oz + z * step, h, nrm.y, w.x, w.y, w.z, step);
                if (h < minY) minY = h;
                if (h > maxY) maxY = h;
            }

            // triangles
            var tris = new List<int>(res * res * 6 + skirtCount * 6);
            for (int z = 0; z < res; z++)
            for (int x = 0; x < res; x++)
            {
                int i0 = z * n + x, i1 = i0 + 1, i2 = i0 + n, i3 = i2 + 1;
                // alternate diagonal for less directional artifacts
                if (((x + z) & 1) == 0)
                {
                    tris.Add(i0); tris.Add(i2); tris.Add(i1);
                    tris.Add(i1); tris.Add(i2); tris.Add(i3);
                }
                else
                {
                    tris.Add(i0); tris.Add(i2); tris.Add(i3);
                    tris.Add(i0); tris.Add(i3); tris.Add(i1);
                }
            }

            // skirts: duplicate the border ring pushed down, hides cracks between LODs
            float skirtDepth = lod == 0 ? 3f : (lod == 1 ? 6f : 10f);
            var ring = new List<int>(skirtCount);
            for (int x = 0; x < n; x++) ring.Add(x);                       // bottom edge (z=0) left->right
            for (int z = 1; z < n; z++) ring.Add(z * n + res);             // right edge upward
            for (int x = res - 1; x >= 0; x--) ring.Add(res * n + x);      // top edge right->left
            for (int z = res - 1; z >= 1; z--) ring.Add(z * n);            // left edge downward
            int sBase = n * n;
            for (int i = 0; i < ring.Count; i++)
            {
                int src = ring[i];
                verts[sBase + i] = verts[src] - new Vector3(0, skirtDepth, 0);
                normals[sBase + i] = normals[src];
                colors[sBase + i] = colors[src];
            }
            for (int i = 0; i < ring.Count; i++)
            {
                int a = ring[i], b = ring[(i + 1) % ring.Count];
                int sa = sBase + i, sb = sBase + (i + 1) % ring.Count;
                // outward-facing (ring runs counter-clockwise seen from above)
                tris.Add(a); tris.Add(sb); tris.Add(sa);
                tris.Add(a); tris.Add(b); tris.Add(sb);
            }
            // trim unused skirt verts
            int used = sBase + ring.Count;
            if (used < verts.Length)
            {
                System.Array.Resize(ref verts, used);
                System.Array.Resize(ref normals, used);
                System.Array.Resize(ref colors, used);
            }

            BiomeId dom = BiomeId.Meadow;
            if (biomeSum.y > biomeSum.x && biomeSum.y > biomeSum.z) dom = BiomeId.Badlands;
            else if (biomeSum.z > biomeSum.x && biomeSum.z > biomeSum.y) dom = BiomeId.Peaks;
            float midX = ox + size * 0.5f, midZ = oz + size * 0.5f;
            if (WorldGen.OuterBiome(midX, midZ, WorldGen.Height(midX, midZ), out var outerDom)) dom = outerDom;

            return new ChunkMeshData
            {
                cx = cx, cz = cz, lod = lod, verts = verts, normals = normals, colors = colors, tris = tris.ToArray(),
                minY = minY - skirtDepth, maxY = maxY, props = wantProps ? ScatterProps(cx, cz) : null, dominantBiome = dom
            };
        }

        /// <summary>Slice of the prop roll that becomes an ore vein on the outer lands (about 1 cell in 110).</summary>
        public const float OreVeinFrom = 0.966f, OreVeinTo = 0.975f;

        /// <summary>Prism Vein placement (see ScatterProps): about 55-60 veins on the whole island.</summary>
        public const float PrismVeinChance = 0.03f, PrismVeinMinHeight = 60f, PrismVeinMinDist = 1800f;

        /// <summary>Deterministic jittered-grid scatter. Same chunk => same props (node keys stay stable across sessions).</summary>
        public static List<PropSpawn> ScatterProps(int cx, int cz)
        {
            var list = new List<PropSpawn>(160);
            const float cell = 5f;
            int cells = (int)(WorldGen.ChunkSize / cell);
            float ox = cx * WorldGen.ChunkSize, oz = cz * WorldGen.ChunkSize;
            int idx = 0;
            for (int gz = 0; gz < cells; gz++)
            for (int gx = 0; gx < cells; gx++)
            {
                var rng = new Rng(cx * cells + gx, cz * cells + gz, WorldGen.Seed * 7 + 3);
                float x = ox + (gx + rng.Range(0.1f, 0.9f)) * cell;
                float z = oz + (gz + rng.Range(0.1f, 0.9f)) * cell;
                int myIndex = idx++;
                if (!WorldGen.InsideWorld(x, z)) continue;
                WorldGen.Weights(x, z, out float wm, out float wb, out float wp);
                float h = WorldGen.HeightW(x, z, wm, wb, wp);
                if (h < 0.8f)
                {
                    // Pearl Oysters on Shellstrand's tidal flats (bare only at low tide). Ground this low never held a
                    // prop before, so nothing existing moves.
                    if (h > -0.7f && h < 0.2f && SubRegions.Shellstrand >= 0)
                    {
                        var ss = SubRegions.Get(SubRegions.Shellstrand);
                        float sdx = x - ss.center.x, sdz = z - ss.center.y;
                        if (sdx * sdx + sdz * sdz < ss.radius * ss.radius * 1.5f && SubRegions.At(x, z, BiomeId.Isles, out float tw) == ss.index && tw > 0.3f && rng.Value < 0.04f)
                            list.Add(new PropSpawn { type = PropType.PearlOyster, pos = new Vector3(x, h - 0.03f, z), rotY = rng.Range(0f, 360f), scale = rng.Range(0.9f, 1.25f), index = myIndex });
                    }
                    continue;
                }
                float ny = WorldGen.Normal(x, z, 1f).y;

                // pick a biome for this cell proportionally to weights => natural mixing at borders
                float br = rng.Value;
                BiomeId b = br < wm ? BiomeId.Meadow : (br < wm + wb ? BiomeId.Badlands : BiomeId.Peaks);
                if (WorldGen.OuterBiome(x, z, h, out var outer)) b = outer;
                float r = rng.Value;
                float roll = r;
                PropType t = PropType.Count;

                if (ny < 0.72f)
                {
                    if (r < 0.05f) t = b == BiomeId.Peaks && rng.Chance(0.3f) ? PropType.CrystalCluster : PropType.Rock;
                    else if (r < 0.065f) t = PropType.OreRock;
                }
                else if (b == BiomeId.Meadow)
                {
                    float forest = WorldGen.Forest(x, z);
                    float treeP = 0.02f + forest * 0.26f;
                    if (r < treeP) t = PropType.OakTree;
                    else if ((r -= treeP) < 0.028f) t = PropType.BerryBush;
                    else if ((r -= 0.028f) < 0.03f) t = PropType.FiberPlant;
                    else if ((r -= 0.03f) < 0.018f) t = PropType.Rock;
                    else if ((r -= 0.018f) < 0.003f) t = PropType.OreRock;
                    else if ((r -= 0.003f) < 0.30f) t = PropType.GrassTuft;
                    else if ((r -= 0.30f) < 0.09f) t = PropType.Flower;
                    else if ((r -= 0.09f) < 0.02f + forest * 0.08f) t = PropType.FallenSticks;
                    else if ((r -= 0.02f + forest * 0.08f) < 0.035f) t = PropType.LooseStones;
                    else if ((r -= 0.035f) < 0.035f) t = PropType.FiberBundle;
                }
                else if (b == BiomeId.Isles)
                {
                    if (h > 1.6f && r < 0.03f) t = PropType.PalmTree;
                    else if ((r -= 0.03f) < 0.02f && h > 2.2f) t = PropType.BerryBush;
                    else if ((r -= 0.02f) < 0.015f) t = PropType.Rock;
                    else if ((r -= 0.015f) < 0.012f && h > 2.2f) t = PropType.FiberPlant;
                    else if ((r -= 0.012f) < 0.06f) t = PropType.DryGrass;
                    else if ((r -= 0.06f) < 0.04f && h > 2.5f) t = PropType.Flower;
                    else if ((r -= 0.04f) < 0.035f) t = PropType.LooseStones;
                    else if ((r -= 0.035f) < 0.02f) t = PropType.FallenSticks;
                    else if ((r -= 0.02f) < 0.02f) t = PropType.FiberBundle;
                }
                else if (b == BiomeId.Swamp)
                {
                    if (r < 0.085f) t = PropType.MangroveTree;
                    else if ((r -= 0.085f) < 0.025f) t = PropType.BerryBush;
                    else if ((r -= 0.025f) < 0.045f) t = PropType.FiberPlant;
                    else if ((r -= 0.045f) < 0.01f) t = PropType.Rock;
                    else if ((r -= 0.01f) < 0.2f) t = PropType.Reeds;
                    else if ((r -= 0.2f) < 0.07f) t = PropType.GrassTuft;
                    else if ((r -= 0.07f) < 0.025f) t = PropType.FallenSticks;
                    else if ((r -= 0.025f) < 0.02f) t = PropType.FiberBundle;
                    else if (roll > 0.985f) t = PropType.MelonVine;
                }
                else if (b == BiomeId.Desert)
                {
                    if (r < 0.018f) t = PropType.Cactus;
                    else if ((r -= 0.018f) < 0.006f) t = PropType.Spinepalm;
                    else if ((r -= 0.006f) < 0.025f) t = PropType.Rock;
                    else if ((r -= 0.025f) < 0.014f) t = PropType.OreRock;
                    else if ((r -= 0.014f) < 0.006f) t = PropType.CinderRock;
                    else if ((r -= 0.006f) < 0.06f) t = PropType.DryGrass;
                    else if ((r -= 0.06f) < 0.07f) t = PropType.Pebble;
                    else if ((r -= 0.07f) < 0.03f) t = PropType.LooseStones;
                    else if ((r -= 0.03f) < 0.008f) t = PropType.FallenSticks;
                    else if (roll > 0.988f) t = PropType.PepperBush;
                }
                else if (b == BiomeId.Blight)
                {
                    if (r < 0.045f) t = PropType.BlightTree;
                    else if ((r -= 0.045f) < 0.02f) t = PropType.Rock;
                    else if ((r -= 0.02f) < 0.016f) t = PropType.OreRock;
                    else if ((r -= 0.016f) < 0.01f) t = PropType.CrystalCluster;
                    else if ((r -= 0.01f) < 0.13f) t = PropType.ToxicShroom;
                    else if ((r -= 0.13f) < 0.05f) t = PropType.Pebble;
                    else if ((r -= 0.05f) < 0.025f) t = PropType.LooseStones;
                    else if ((r -= 0.025f) < 0.012f) t = PropType.FallenSticks;
                }
                else if (b == BiomeId.Glow)
                {
                    if (r < 0.075f) t = PropType.GlowTree;
                    else if ((r -= 0.075f) < 0.02f) t = PropType.CrystalCluster;
                    else if ((r -= 0.02f) < 0.012f) t = PropType.Rock;
                    else if ((r -= 0.012f) < 0.02f) t = PropType.FiberPlant;
                    else if ((r -= 0.02f) < 0.15f) t = PropType.GlowShroom;
                    else if ((r -= 0.15f) < 0.14f) t = PropType.GlowTuft;
                    else if ((r -= 0.14f) < 0.018f) t = PropType.FallenSticks;
                    else if ((r -= 0.018f) < 0.02f) t = PropType.LooseStones;
                }
                else if (b == BiomeId.Badlands)
                {
                    if (r < 0.014f) t = PropType.DeadTree;
                    else if ((r -= 0.014f) < 0.024f) t = PropType.Spinepalm;
                    else if ((r -= 0.024f) < 0.04f) t = PropType.Rock;
                    else if ((r -= 0.04f) < 0.018f) t = PropType.OreRock;
                    else if ((r -= 0.018f) < 0.012f) t = PropType.CinderRock;
                    else if ((r -= 0.012f) < 0.02f) t = PropType.FiberPlant;
                    else if ((r -= 0.02f) < 0.1f) t = PropType.Pebble;
                    else if ((r -= 0.1f) < 0.1f) t = PropType.DryGrass;
                    else if ((r -= 0.1f) < 0.04f) t = PropType.LooseStones;
                    else if ((r -= 0.04f) < 0.015f) t = PropType.FallenSticks;
                    else if ((r -= 0.015f) < 0.018f) t = PropType.FiberBundle;
                }
                else
                {
                    float treeP = h < 75f ? 0.07f + WorldGen.Forest(x, z) * 0.12f : 0.01f;
                    if (r < treeP) t = PropType.PineTree;
                    else if ((r -= treeP) < 0.04f) t = PropType.Rock;
                    else if ((r -= 0.04f) < 0.012f) t = PropType.OreRock;
                    else if ((r -= 0.012f) < 0.014f) t = PropType.CrystalCluster;
                    else if ((r -= 0.014f) < 0.012f) t = PropType.FiberPlant;
                    else if ((r -= 0.012f) < 0.1f) t = PropType.SnowTuft;
                    else if ((r -= 0.1f) < 0.035f) t = PropType.LooseStones;
                    else if ((r -= 0.035f) < 0.015f + (h < 75f ? WorldGen.Forest(x, z) * 0.05f : 0f)) t = PropType.FallenSticks;
                    else if ((r -= 0.015f + (h < 75f ? WorldGen.Forest(x, z) * 0.05f : 0f)) < 0.015f) t = PropType.FiberBundle;
                }
                // wild crops take unused slices at the top of the roll, so nothing that was already there moves
                if (t == PropType.Count && ny >= 0.72f)
                {
                    if (b == BiomeId.Meadow) { if (roll > 0.988f) t = PropType.GrainStalks; else if (roll > 0.979f) t = PropType.MelonVine; }
                    else if (b == BiomeId.Badlands) { if (roll > 0.988f) t = PropType.PepperBush; }
                    else if (b == BiomeId.Peaks && roll > 0.956f && roll <= 0.97f) t = PropType.MintPatch;
                }
                // Prism Veins: only deep in the Rimefang Peaks, high up and far from the meadow (the island's highest-level
                // ground). They take the unused top of the roll, so no existing prop moves.
                if (t == PropType.Count && b == BiomeId.Peaks && ny >= 0.72f && roll > 1f - PrismVeinChance && wp > 0.8f && h > PrismVeinMinHeight &&
                    Vector2.Distance(new Vector2(x, z), WorldGen.MeadowCenter) > PrismVeinMinDist) t = PropType.PrismVein;
                // outer-land ore veins: one more unused slice of the roll per land
                if (t == PropType.Count && ny >= 0.72f && roll > OreVeinFrom && roll <= OreVeinTo)
                {
                    if (b == BiomeId.Isles) { if (h > 1.8f) t = PropType.SaltglassVein; }
                    else if (b == BiomeId.Swamp) t = PropType.BogIronVein;
                    else if (b == BiomeId.Desert) t = PropType.SunsteelVein;
                    else if (b == BiomeId.Blight) t = PropType.BlightVein;
                    else if (b == BiomeId.Glow) t = PropType.StarcrystalVein;
                }
                // named sub-regions: their own plants fill cells the biome left empty (so no existing prop moves), and
                // they may swap one ground cover for another
                if (ny >= 0.72f)
                {
                    int si = SubRegions.At(x, z, b, out float sw);
                    if (si >= 0)
                    {
                        if (t == PropType.Count) t = SubRegions.Extra(si, sw, rng.Value, h);
                        else t = SubRegions.Swapped(si, sw, t, Hash.F(cx * cells + gx, cz * cells + gz, 913));
                    }
                }
                // a land's own ore stays out of the Sunmeadow (user, 2026-10-20: "cinder vents outside the area"): the
                // border mix above lets a cell 100+ m into the meadow roll the Badlands or Peaks table, so a Cinder Vent
                // or Rime Crystal on ground the meadow owns becomes a plain boulder in the same spot (nothing else moves)
                if ((t == PropType.CinderRock || t == PropType.CrystalCluster) && MeadowOwns(b, wm, wb, wp)) t = PropType.Rock;
                if (t == PropType.Count) continue;
                // keep the start plateau and its cliffs an open glade: no trees / boulders (pickups, bushes & grass are fine)
                if (t < PropType.LooseStones && t != PropType.BerryBush && t != PropType.FiberPlant &&
                    WorldGen.StartDistance(x, z) < WorldGen.StartR + 6f) continue;

                float scale = t <= PropType.DeadTree || (PropCatalog.IsTree(t) && t != PropType.Spinepalm) ? rng.Range(0.8f, 1.35f) : rng.Range(0.75f, 1.2f);
                // sink slightly so props sit into slopes
                list.Add(new PropSpawn { type = t, pos = new Vector3(x, h - 0.1f, z), rotY = rng.Range(0f, 360f), scale = scale, index = myIndex });
            }
            return list;
        }

        /// <summary>True on the old island where the Sunmeadow is the strongest land (not on an outer land).</summary>
        public static bool MeadowOwns(BiomeId cellBiome, float wm, float wb, float wp) =>
            (cellBiome == BiomeId.Meadow || cellBiome == BiomeId.Badlands || cellBiome == BiomeId.Peaks) && wm >= wb && wm >= wp;
    }
}
