using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace Wildbond
{
    /// <summary>
    /// Streams the 4 km island in 64 m chunks around the player:
    ///  - terrain meshes built on worker threads, uploaded on the main thread under a per-frame budget
    ///  - 3 LOD rings (32/16/8 quads) with skirts to hide seams
    ///  - props/resources rendered with GPU instancing per chunk (no GameObjects per tree)
    ///  - gatherable nodes registered in a spatial hash only within the prop radius
    ///  - chunk GameObjects + meshes pooled
    /// </summary>
    public class ChunkManager : MonoBehaviour
    {
        public static ChunkManager I { get; private set; }

        public class Chunk
        {
            public int cx, cz, ring, lod = -1, requestedLod = -1;
            public GameObject go;
            public Mesh mesh;
            public Bounds bounds;
            public bool hasMesh;
            public BiomeId biome;
            public List<PropSpawn> props;
            public bool propsActive, decorActive;
            public readonly List<ResourceNode> nodes = new List<ResourceNode>();
            public List<Matrix4x4>[] batches;
            public bool batchesDirty;
        }

        [SerializeField] int maxJobsInFlight = 3;
        [SerializeField] float uploadBudgetMs = 2.5f;

        public readonly SpatialHash<ResourceNode> Nodes = new SpatialHash<ResourceNode>(16f);
        /// <summary>Node key -> world time when it respawns. Persisted in the save.</summary>
        public readonly Dictionary<long, double> Depleted = new Dictionary<long, double>();

        readonly Dictionary<long, Chunk> chunks = new Dictionary<long, Chunk>();
        readonly Dictionary<long, List<PropSpawn>> propCache = new Dictionary<long, List<PropSpawn>>();
        readonly ConcurrentQueue<ChunkMeshData> done = new ConcurrentQueue<ChunkMeshData>();
        readonly List<Chunk> pending = new List<Chunk>();
        readonly Stack<GameObject> goPool = new Stack<GameObject>();
        readonly Stack<ResourceNode> nodePool = new Stack<ResourceNode>();
        readonly HashSet<long> desired = new HashSet<long>();
        readonly List<long> toRemove = new List<long>();
        readonly List<ResourceNode> wobbling = new List<ResourceNode>();
        readonly Plane[] frustum = new Plane[6];

        Material terrainMat, propMat;
        int inFlight;
        int lastPcx = int.MinValue, lastPcz = int.MinValue;
        bool forceRefresh = true;
        float respawnTimer;
        bool threaded;

        public int LoadedChunks => chunks.Count;
        public int PendingJobs => pending.Count + inFlight;
        public bool Ready { get; private set; }

        public void Init(Material terrain, Material props)
        {
            I = this;
            terrainMat = terrain;
            propMat = props;
            threaded = SystemInfo.processorCount > 1 && Application.platform != RuntimePlatform.WebGLPlayer;
            GameEvents.QualityChanged += () => forceRefresh = true;
        }

        static long Key(int cx, int cz) => ((long)cx << 32) ^ (uint)cz;

        static int LodForRing(int ring) => ring <= 1 ? 0 : (ring <= 3 ? 1 : 2);

        public BiomeId BiomeAt(Vector3 p) => WorldGen.Biome(p.x, p.z);

        // ============================================================ UPDATE
        public void Tick(Vector3 focus, Camera cam)
        {
            int pcx = Mathf.FloorToInt(focus.x / WorldGen.ChunkSize);
            int pcz = Mathf.FloorToInt(focus.z / WorldGen.ChunkSize);
            if (forceRefresh || pcx != lastPcx || pcz != lastPcz)
            {
                forceRefresh = false;
                lastPcx = pcx; lastPcz = pcz;
                RefreshDesired(pcx, pcz);
            }

            ApplyCompleted();
            Dispatch();

            respawnTimer -= Time.deltaTime;
            if (respawnTimer <= 0f) { respawnTimer = 2f; CheckRespawns(); }

            DrawProps(cam);
        }

        void RefreshDesired(int pcx, int pcz)
        {
            var q = QualityProfile.Current;
            int R = q.viewRadiusChunks;
            int half = WorldGen.ChunksPerSide / 2;
            desired.Clear();
            float rr = (R + 0.5f) * (R + 0.5f);
            for (int dz = -R; dz <= R; dz++)
            for (int dx = -R; dx <= R; dx++)
            {
                if (dx * dx + dz * dz > rr) continue;
                int cx = pcx + dx, cz = pcz + dz;
                if (cx < -half || cx >= half || cz < -half || cz >= half) continue;
                long k = Key(cx, cz);
                desired.Add(k);
                int ring = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz));
                if (!chunks.TryGetValue(k, out var c))
                {
                    c = new Chunk { cx = cx, cz = cz };
                    chunks[k] = c;
                }
                c.ring = ring;
                int lod = LodForRing(ring);
                bool wantProps = ring <= q.propRadiusChunks;
                if (c.requestedLod != lod && c.lod != lod)
                {
                    c.requestedLod = lod;
                    if (!pending.Contains(c)) pending.Add(c);
                }
                if (wantProps && !c.propsActive)
                {
                    if (propCache.TryGetValue(k, out var cached)) ActivateProps(c, cached);
                    else if (!pending.Contains(c)) { c.requestedLod = lod; pending.Add(c); }
                }
                else if (!wantProps && c.propsActive) DeactivateProps(c);

                bool wantDecor = ring <= q.decorRadiusChunks;
                if (c.decorActive != wantDecor && c.propsActive) { c.decorActive = wantDecor; c.batchesDirty = true; }
            }

            toRemove.Clear();
            foreach (var kv in chunks) if (!desired.Contains(kv.Key)) toRemove.Add(kv.Key);
            foreach (var k in toRemove) Unload(chunks[k], k);

            pending.Sort((a, b) => a.ring.CompareTo(b.ring));
        }

        void Dispatch()
        {
            while (pending.Count > 0 && inFlight < maxJobsInFlight)
            {
                var c = pending[0];
                pending.RemoveAt(0);
                long k = Key(c.cx, c.cz);
                if (!chunks.ContainsKey(k)) continue;
                int cx = c.cx, cz = c.cz, lod = c.requestedLod < 0 ? LodForRing(c.ring) : c.requestedLod;
                bool wantProps = !propCache.ContainsKey(k);
                inFlight++;
                if (threaded)
                {
                    Task.Run(() =>
                    {
                        try { done.Enqueue(ChunkBuilder.BuildTerrain(cx, cz, lod, wantProps)); }
                        catch (System.Exception e)
                        {
                            Debug.LogException(e);
                            done.Enqueue(new ChunkMeshData { cx = cx, cz = cz, lod = -1 });
                        }
                    });
                }
                else
                {
                    done.Enqueue(ChunkBuilder.BuildTerrain(cx, cz, lod, wantProps));
                    break; // one synchronous build per frame
                }
            }
        }

        void ApplyCompleted()
        {
            float start = Time.realtimeSinceStartup;
            while ((Time.realtimeSinceStartup - start) * 1000f < uploadBudgetMs && done.TryDequeue(out var d))
            {
                inFlight--;
                long k = Key(d.cx, d.cz);
                if (d.props != null && !propCache.ContainsKey(k)) propCache[k] = d.props;
                if (d.lod < 0) continue;
                if (!chunks.TryGetValue(k, out var c)) continue; // unloaded meanwhile

                if (c.go == null) AcquireGo(c);
                var m = c.mesh;
                m.Clear();
                if (d.verts.Length > 65000) m.indexFormat = IndexFormat.UInt32;
                m.SetVertices(d.verts);
                m.SetNormals(d.normals);
                m.SetColors(d.colors);
                m.SetTriangles(d.tris, 0, false);
                float midY = (d.minY + d.maxY) * 0.5f;
                m.bounds = new Bounds(new Vector3(WorldGen.ChunkSize * 0.5f, midY, WorldGen.ChunkSize * 0.5f),
                    new Vector3(WorldGen.ChunkSize, Mathf.Max(1f, d.maxY - d.minY), WorldGen.ChunkSize));
                c.bounds = new Bounds(new Vector3(d.cx * WorldGen.ChunkSize + 32f, midY + 4f, d.cz * WorldGen.ChunkSize + 32f),
                    new Vector3(WorldGen.ChunkSize + 8f, d.maxY - d.minY + 20f, WorldGen.ChunkSize + 8f));
                c.lod = d.lod;
                c.hasMesh = true;
                c.biome = d.dominantBiome;
                c.go.SetActive(true);
                if (c.requestedLod == d.lod) c.requestedLod = -1;

                if (!c.propsActive && c.ring <= QualityProfile.Current.propRadiusChunks && propCache.TryGetValue(k, out var props))
                {
                    c.decorActive = c.ring <= QualityProfile.Current.decorRadiusChunks;
                    ActivateProps(c, props);
                }
            }
            if (!Ready && pending.Count == 0 && inFlight == 0 && chunks.Count > 0) Ready = true;
        }

        void AcquireGo(Chunk c)
        {
            GameObject go;
            if (goPool.Count > 0) go = goPool.Pop();
            else
            {
                go = new GameObject("Chunk");
                go.transform.SetParent(transform, false);
                var mf = go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = terrainMat;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = true;
                var mesh = new Mesh { name = "terrain" };
                mesh.MarkDynamic();
                mf.sharedMesh = mesh;
            }
            go.name = $"Chunk_{c.cx}_{c.cz}";
            go.transform.position = new Vector3(c.cx * WorldGen.ChunkSize, 0f, c.cz * WorldGen.ChunkSize);
            c.go = go;
            c.mesh = go.GetComponent<MeshFilter>().sharedMesh;
        }

        void Unload(Chunk c, long k)
        {
            if (c.propsActive) DeactivateProps(c);
            if (c.go != null)
            {
                c.go.SetActive(false);
                goPool.Push(c.go);
                c.go = null;
            }
            pending.Remove(c);
            chunks.Remove(k);
            // keep the prop cache bounded (deterministic, can always be rebuilt)
            if (propCache.Count > 600) propCache.Remove(k);
        }

        // ============================================================ PROPS / NODES
        void ActivateProps(Chunk c, List<PropSpawn> props)
        {
            c.props = props;
            c.propsActive = true;
            c.decorActive = c.ring <= QualityProfile.Current.decorRadiusChunks;
            double now = GameManager.WorldTime;
            for (int i = 0; i < props.Count; i++)
            {
                var p = props[i];
                if (!PropCatalog.IsNode(p.type)) continue;
                var n = nodePool.Count > 0 ? nodePool.Pop() : new ResourceNode();
                n.key = ResourceNode.MakeKey(c.cx, c.cz, p.index);
                n.type = p.type; n.pos = p.pos; n.rotY = p.rotY; n.scale = p.scale;
                n.hp = PropCatalog.Info[(int)p.type].hp;
                n.wobble = 0f;
                n.chunk = c;
                n.alive = !(Depleted.TryGetValue(n.key, out double t) && t > now);
                if (n.alive && BaseManager.I != null && BaseManager.I.HasBase && BaseManager.I.BlocksNode(n.pos, n.Radius)) n.alive = false;
                if (n.alive && Caves.BlocksNode(n.pos)) n.alive = false;
                if (n.alive && Settlements.BlocksNode(n.pos)) n.alive = false;
                if (n.alive && Wardens.BlocksNode(n.pos)) n.alive = false;
                if (n.alive && Pois.BlocksNode(n.pos)) n.alive = false;
                if (n.alive && Outposts.BlocksNode(n.pos)) n.alive = false;
                if (n.alive && Trails.BlocksNode(n.pos)) n.alive = false;
                if (n.alive && Relics.BlocksNode(n.pos)) n.alive = false;
                c.nodes.Add(n);
                Nodes.Insert(n);
            }
            if (c.batches == null)
            {
                c.batches = new List<Matrix4x4>[(int)PropType.Count];
                for (int i = 0; i < c.batches.Length; i++) c.batches[i] = new List<Matrix4x4>(16);
            }
            c.batchesDirty = true;
        }

        void DeactivateProps(Chunk c)
        {
            for (int i = 0; i < c.nodes.Count; i++)
            {
                Nodes.Remove(c.nodes[i]);
                wobbling.Remove(c.nodes[i]);
                c.nodes[i].chunk = null;
                nodePool.Push(c.nodes[i]);
            }
            c.nodes.Clear();
            c.propsActive = false;
            if (c.batches != null) for (int i = 0; i < c.batches.Length; i++) c.batches[i].Clear();
        }

        void RebuildBatches(Chunk c)
        {
            c.batchesDirty = false;
            for (int i = 0; i < c.batches.Length; i++) c.batches[i].Clear();
            var q = QualityProfile.Current;
            // nodes
            for (int i = 0; i < c.nodes.Count; i++)
            {
                var n = c.nodes[i];
                if (!n.alive || n.wobble > 0f) continue;
                c.batches[(int)n.type].Add(Matrix4x4.TRS(n.pos, Quaternion.Euler(0, n.rotY, 0), Vector3.one * n.scale));
            }
            // decor
            if (c.decorActive)
            {
                for (int i = 0; i < c.props.Count; i++)
                {
                    var p = c.props[i];
                    if (PropCatalog.IsNode(p.type)) continue;
                    if (((p.index * 2654435761u) & 1023) / 1023f > q.decorDensity) continue;
                    c.batches[(int)p.type].Add(Matrix4x4.TRS(p.pos, Quaternion.Euler(0, p.rotY, 0), Vector3.one * p.scale));
                }
            }
        }

        void DrawProps(Camera cam)
        {
            if (cam == null || propMat == null) return;
            GeometryUtility.CalculateFrustumPlanes(cam, frustum);
            var q = QualityProfile.Current;
            foreach (var kv in chunks)
            {
                var c = kv.Value;
                if (!c.propsActive || !c.hasMesh) continue;
                if (c.batchesDirty) RebuildBatches(c);
                if (!GeometryUtility.TestPlanesAABB(frustum, c.bounds)) continue;
                bool low = c.ring >= 2;
                var rp = new RenderParams(propMat)
                {
                    shadowCastingMode = q.shadows && c.ring <= 1 ? ShadowCastingMode.On : ShadowCastingMode.Off,
                    receiveShadows = true,
                    worldBounds = c.bounds
                };
                for (int t = 0; t < c.batches.Length; t++)
                {
                    var list = c.batches[t];
                    if (list.Count == 0) continue;
                    Graphics.RenderMeshInstanced(rp, PropCatalog.Mesh((PropType)t, low), 0, list);
                }
            }

            // individually drawn shaking nodes
            for (int i = wobbling.Count - 1; i >= 0; i--)
            {
                var n = wobbling[i];
                n.wobble -= Time.deltaTime;
                if (n.wobble <= 0f || !n.alive)
                {
                    n.wobble = 0f;
                    wobbling.RemoveAt(i);
                    if (n.chunk is Chunk c) c.batchesDirty = true;
                    continue;
                }
                float s = Mathf.Sin(n.wobble * 60f) * n.wobble * 12f;
                var rot = Quaternion.Euler(s * 0.6f, n.rotY, s);
                var rp = new RenderParams(propMat) { shadowCastingMode = ShadowCastingMode.Off, receiveShadows = true };
                Graphics.RenderMesh(rp, PropCatalog.Mesh(n.type, false), 0, Matrix4x4.TRS(n.pos, rot, Vector3.one * n.scale));
            }
        }

        public void Wobble(ResourceNode n)
        {
            if (n.wobble <= 0f) wobbling.Add(n);
            n.wobble = 0.3f;
            if (n.chunk is Chunk c) c.batchesDirty = true;
        }

        public void Deplete(ResourceNode n)
        {
            if (n.key < 0) { n.alive = false; Caves.I?.NodeGone(n); return; }   // a cave vein: gone until the cave refreshes
            n.alive = false;
            Depleted[n.key] = GameManager.WorldTime + n.Info.respawn / Mathf.Max(0.1f, WorldRules.Current.resourceRespawnRate);
            if (n.chunk is Chunk c) c.batchesDirty = true;
        }

        void CheckRespawns()
        {
            double now = GameManager.WorldTime;
            foreach (var kv in chunks)
            {
                var c = kv.Value;
                if (!c.propsActive) continue;
                for (int i = 0; i < c.nodes.Count; i++)
                {
                    var n = c.nodes[i];
                    if (n.alive) continue;
                    if (!Depleted.TryGetValue(n.key, out double t) || t <= now)
                    {
                        if (BaseManager.I != null && BaseManager.I.HasBase && BaseManager.I.BlocksNode(n.pos, n.Radius)) continue;
                        if (Caves.BlocksNode(n.pos)) continue;
                        if (Settlements.BlocksNode(n.pos)) continue;
                        if (Wardens.BlocksNode(n.pos)) continue;
                        if (Pois.BlocksNode(n.pos)) continue;
                        if (Outposts.BlocksNode(n.pos)) continue;
                        if (Trails.BlocksNode(n.pos)) continue;
                        if (Relics.BlocksNode(n.pos)) continue;
                        Depleted.Remove(n.key);
                        n.alive = true;
                        n.hp = n.Info.hp;
                        c.batchesDirty = true;
                    }
                }
            }
            // prune stale depletion entries for unloaded chunks occasionally
            if (Depleted.Count > 4000)
            {
                toRemove.Clear();
                foreach (var kv in Depleted) if (kv.Value <= now) toRemove.Add(kv.Key);
                foreach (var k in toRemove) Depleted.Remove(k);
            }
        }

        readonly List<ResourceNode> queryBuf = new List<ResourceNode>(32);

        /// <summary>Best alive node near pos, favouring ones in front.</summary>
        public ResourceNode FindNode(Vector3 pos, Vector3 forward, float range)
        {
            if (Caves.InCave) return Caves.I.FindNode(pos, forward, range);   // a cave's veins are its own
            queryBuf.Clear();
            Nodes.Query(pos, range + 2f, queryBuf);
            ResourceNode best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < queryBuf.Count; i++)
            {
                var n = queryBuf[i];
                if (!n.alive) continue;
                Vector3 d = n.pos - pos; d.y = 0;
                float dist = d.magnitude - n.Radius;
                if (dist > range) continue;
                float facing = dist > 0.01f ? Vector3.Dot(d.normalized, forward) : 1f;
                float score = dist - facing * 1.2f;
                if (score < bestScore) { bestScore = score; best = n; }
            }
            return best;
        }

        public bool IsChunkReady(Vector3 p)
        {
            int cx = Mathf.FloorToInt(p.x / WorldGen.ChunkSize), cz = Mathf.FloorToInt(p.z / WorldGen.ChunkSize);
            return chunks.TryGetValue(Key(cx, cz), out var c) && c.hasMesh;
        }
    }
}
