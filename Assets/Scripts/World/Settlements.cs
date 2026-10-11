using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Wildbond
{
    /// <summary>
    /// Three settlements, one per biome: houses round a fire, villagers and their kin walking between jobs, and a
    /// Kinkeeper's stall that buys and sells kin for Glimmer (stock restocks every half hour of real time).
    /// Villagers and their kin are light scripted agents, not CreatureActors: they can't be fought or captured.
    /// Wild kin don't spawn nearby, trees and rocks are cleared, and camps can't be founded on top of one.
    /// </summary>
    public class Settlements : MonoBehaviour
    {
        public static Settlements I { get; private set; }
        /// <summary>The built-up area: cleared of nodes, no camp buildings.</summary>
        public const float Radius = 24f;
        /// <summary>Extra ring where wild kin never spawn.</summary>
        public const float QuietRing = 26f;
        public const int RestockSeconds = 30 * 60;
        public const float SellRatio = 0.25f;

        public class Settlement
        {
            public int index;
            public string id, name, keeper;
            public BiomeId biome;
            public Vector3 pos, keeperPos, boardPos;
            public Color color;
            public GameObject root;
            public bool greeted;
            public readonly List<Agent> agents = new List<Agent>();
            public readonly List<Spot> spots = new List<Spot>();
        }

        public struct Spot { public Vector3 pos, face; public bool work; public WorkType job; }

        public class Agent
        {
            public Transform tr;
            public CreatureRig rig;
            public bool human, keeper;
            public string name;
            public float speed, timer, radius;
            public int home = -1, at = -1;
            public Vector3 target, face;
            public bool moving, working;
        }

        public class Offer { public CreatureData kin; public int price; public string key; }

        public static readonly List<Settlement> All = new List<Settlement>();
        static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        readonly Dictionary<string, List<Offer>> offers = new Dictionary<string, List<Offer>>();
        readonly Dictionary<string, long> offerBucket = new Dictionary<string, long>();

        // ============================================================ PLACEMENT
        /// <summary>Chooses the three sites from the terrain. Call once the world seed is set and before chunks stream in.</summary>
        public static void Prepare()
        {
            All.Clear();
            Vector2 toMeadow = (WorldGen.MeadowCenter - new Vector2(WorldGen.StartExit.x, WorldGen.StartExit.z)).normalized;
            var nearStart = WorldGen.StartExit + new Vector3(toMeadow.x, 0, toMeadow.y) * 260f;
            Add("hearthmere", "Hearthmere", "Kinkeeper Juno", BiomeId.Meadow, nearStart, new Color(1f, 0.8f, 0.45f));
            Add("cinderhold", "Cinderhold", "Kinkeeper Brask", BiomeId.Badlands, new Vector3(1060f, 0, 360f), new Color(1f, 0.55f, 0.3f));
            Add("frostgate", "Frostgate", "Kinkeeper Sella", BiomeId.Peaks, new Vector3(-880f, 0, 470f), new Color(0.65f, 0.9f, 1f));
        }

        static void Add(string id, string name, string keeper, BiomeId biome, Vector3 near, Color color)
        {
            All.Add(new Settlement { index = All.Count, id = id, name = name, keeper = keeper, biome = biome, pos = FindFlat(near, biome), color = color });
        }

        /// <summary>Flattest dry spot of the right biome within ~150 m of the wanted position.</summary>
        public static Vector3 FindFlat(Vector3 near, BiomeId biome)
        {
            Vector3 best = new Vector3(near.x, WorldGen.Height(near.x, near.z), near.z);
            float bestScore = float.MaxValue;
            for (int ix = -6; ix <= 6; ix++)
            for (int iz = -6; iz <= 6; iz++)
            {
                float x = near.x + ix * 25f, z = near.z + iz * 25f;
                if (!WorldGen.InsideWorld(x, z)) continue;
                float h = WorldGen.Height(x, z);
                if (h < 3f) continue;
                if (WorldGen.StartDistance(x, z) < WorldGen.StartR + 70f) continue;
                float rough = 0f, low = h;
                for (int k = 0; k < 12; k++)
                {
                    float a = k / 12f * Mathf.PI * 2f, r = k % 2 == 0 ? 20f : 10f;
                    float hh = WorldGen.Height(x + Mathf.Cos(a) * r, z + Mathf.Sin(a) * r);
                    rough = Mathf.Max(rough, Mathf.Abs(hh - h));
                    low = Mathf.Min(low, hh);
                }
                if (low < 1.5f) continue;   // would dip into water
                float score = rough * 5f + new Vector2(ix, iz).magnitude * 0.6f + (WorldGen.Biome(x, z) != biome ? 60f : 0f)
                              + (Landmarks.NearLandmark(new Vector3(x, 0, z), 45f) ? 40f : 0f);
                if (score < bestScore) { bestScore = score; best = new Vector3(x, h, z); }
            }
            return best;
        }

        public static bool Inside(Vector3 p, float extra = 0f)
        {
            for (int i = 0; i < All.Count; i++)
            {
                float dx = p.x - All[i].pos.x, dz = p.z - All[i].pos.z, r = Radius + extra;
                if (dx * dx + dz * dz < r * r) return true;
            }
            return false;
        }

        /// <summary>Trees and rocks don't grow inside a settlement.</summary>
        public static bool BlocksNode(Vector3 p) => Inside(p, 1f);

        // ============================================================ BUILD
        public void Init()
        {
            I = this;
            foreach (var s in All) Build(s);
        }

        static Vector3 OnGround(Vector3 p) { p.y = WorldGen.Height(p.x, p.z); return p; }
        static Vector3 Polar(Settlement s, float deg, float r) =>
            OnGround(s.pos + new Vector3(Mathf.Sin(deg * Mathf.Deg2Rad), 0, Mathf.Cos(deg * Mathf.Deg2Rad)) * r);

        void Build(Settlement s)
        {
            s.root = new GameObject("Settlement_" + s.name);
            s.root.transform.SetParent(transform, false);
            Landmarks.List.Add(new Landmarks.Landmark { name = s.name, pos = s.pos, color = s.color, biome = s.biome, go = s.root, icon = MapIcon.Village });

            // heart of the village: fire, well, banner
            var fire = ContentDB.Building("campfire");
            if (fire != null) Static(s, DefMesh(fire), RenderAssets.Building, s.pos, 0f, fire.footprint);
            Static(s, Cached("well", BuildWell), RenderAssets.Building, Polar(s, 150f, 5.5f), 0f, 1.1f);
            Static(s, BannerMesh(s), RenderAssets.Building, Polar(s, 0f, 20f), 180f, 0.3f);
            for (int i = 0; i < 4; i++) Static(s, Cached("lamp", BuildLamp), RenderAssets.Building, Polar(s, 45f + i * 90f, 9.5f), 0f, 0.2f);
            for (int i = 0; i < 3; i++) s.spots.Add(new Spot { pos = Polar(s, 60f + i * 120f, 2.6f), face = s.pos });

            // homes
            float[] houseAt = { 25f, 115f, 205f, 295f };
            for (int i = 0; i < houseAt.Length; i++)
            {
                var hp = Polar(s, houseAt[i], 15.5f);
                Static(s, Cached("house_" + s.biome, mb => BuildHouse(mb, s.biome)), RenderAssets.Building, hp, houseAt[i] + 180f, 3.1f);
                s.spots.Add(new Spot { pos = Polar(s, houseAt[i], 11.2f), face = hp });
            }

            // the Kinkeeper's stall, facing the fire
            var stall = Polar(s, 70f, 8.5f);
            Static(s, Cached("stall", BuildStall), RenderAssets.Building, stall, 70f + 180f, 1.5f);
            s.keeperPos = Polar(s, 70f, 9.7f);

            // request board by the gate
            s.boardPos = Polar(s, 345f, 16.5f);
            Static(s, Cached("board", BuildBoard), RenderAssets.Building, s.boardPos, 165f, 0.5f);

            // workplaces, each with a kin on the job
            string[] jobs = s.biome == BiomeId.Meadow ? new[] { "lumber_mill", "berry_plot", "berry_plot", "forage_hut", "storage_chest" }
                          : s.biome == BiomeId.Badlands ? new[] { "stone_quarry", "smelter", "deep_drill", "workbench", "storage_chest" }
                          : new[] { "lumber_mill", "stone_quarry", "frost_larder", "workbench", "storage_chest" };
            float[] jobAt = { 160f, 245f, 262f, 335f, 110f };
            float[] jobR = { 9f, 9.5f, 13.5f, 9f, 8f };
            var workSpots = new List<int>();
            for (int i = 0; i < jobs.Length; i++)
            {
                var def = ContentDB.Building(jobs[i]);
                if (def == null) continue;
                var bp = Polar(s, jobAt[i], jobR[i]);
                Static(s, DefMesh(def), RenderAssets.Building, bp, jobAt[i] + 180f, def.footprint);
                Vector3 inward = (s.pos - bp); inward.y = 0; inward.Normalize();
                workSpots.Add(s.spots.Count);
                s.spots.Add(new Spot { pos = OnGround(bp + inward * (def.footprint + 0.9f)), face = bp, work = def.workType != WorkType.None, job = def.workType });
            }

            // people
            var rng = new System.Random(WorldGen.Seed * 31 + s.index * 977);
            SpawnHuman(s, s.keeper, 0, s.keeperPos, true, -1);
            string[] names = { "Tam", "Oryn", "Bessa", "Halk", "Mirel", "Dov", "Sana", "Quill", "Ira", "Jorn" };
            for (int i = 0; i < 4; i++)
            {
                int home = i < workSpots.Count ? workSpots[i] : rng.Next(s.spots.Count);
                SpawnHuman(s, names[(s.index * 3 + i) % names.Length], 1 + (s.index * 2 + i) % (Looks.Length - 1), s.spots[home].pos + new Vector3(0.9f, 0, 0.4f), false, home);
            }
            // their kin
            for (int i = 0; i < workSpots.Count; i++) SpawnKin(s, rng, workSpots[i]);
            SpawnKin(s, rng, 0);   // one lazing by the fire
        }

        void Static(Settlement s, Mesh mesh, Material mat, Vector3 pos, float rotY, float obstacle)
        {
            var go = new GameObject(mesh.name);
            go.transform.SetParent(s.root.transform, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, rotY, 0));
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = QualityProfile.Current.shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            if (obstacle > 0f) WorldCollision.AddObstacle(pos, obstacle, this);
        }

        static Mesh Cached(string key, System.Action<MeshBuilder> build)
        {
            if (meshes.TryGetValue(key, out var m) && m != null) return m;
            var mb = new MeshBuilder();
            build(mb);
            m = mb.ToMesh("settle_" + key);
            meshes[key] = m;
            return m;
        }

        static Mesh DefMesh(BuildingDef def)
        {
            string key = "def_" + def.id;
            if (meshes.TryGetValue(key, out var m) && m != null) return m;
            m = ModelBuilder.BuildStatic(def.parts, def.palette, "settle_" + def.id);
            meshes[key] = m;
            return m;
        }

        static Matrix4x4 M(float x, float y, float z, float sx, float sy, float sz, float rx = 0, float ry = 0, float rz = 0) =>
            Matrix4x4.TRS(new Vector3(x, y, z), Quaternion.Euler(rx, ry, rz), new Vector3(sx, sy, sz));
        static Color32 C(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        /// <summary>Cottage: door on +Z. Walls run below ground so slopes never show a gap.</summary>
        static void BuildHouse(MeshBuilder mb, BiomeId biome)
        {
            Color32 wall, wall2, roof, roof2, trim;
            if (biome == BiomeId.Badlands) { wall = C("#c89a6a"); wall2 = C("#e0b888"); roof = C("#a0482e"); roof2 = C("#c8623a"); trim = C("#6a3a24"); }
            else if (biome == BiomeId.Peaks) { wall = C("#5a4636"); wall2 = C("#7a6048"); roof = C("#c8d8e8"); roof2 = C("#f0f6ff"); trim = C("#3a2c22"); }
            else { wall = C("#d8c8a8"); wall2 = C("#f0e4c8"); roof = C("#a8452e"); roof2 = C("#d0603a"); trim = C("#6a4a30"); }
            mb.Box(M(0, 0.8f, 0, 4.4f, 3.6f, 3.8f), wall, wall2);
            // roof: a low pyramid, or a flat adobe slab in the badlands
            if (biome == BiomeId.Badlands) mb.Box(M(0, 2.75f, 0, 4.8f, 0.3f, 4.2f), roof, roof2);
            else mb.Cylinder(M(0, 3.55f, 0, 6.8f, 1.9f, 6.2f, 0, 45, 0), roof, roof2, 4, 0f, true);
            mb.Box(M(0, 1.0f, 1.92f, 0.9f, 1.9f, 0.1f), trim);                                   // door
            mb.Box(M(1.35f, 1.6f, 1.92f, 0.7f, 0.7f, 0.08f), C("#ffe9a0"));                      // lit windows
            mb.Box(M(-1.35f, 1.6f, 1.92f, 0.7f, 0.7f, 0.08f), C("#ffe9a0"));
            mb.Box(M(2.22f, 1.6f, 0, 0.08f, 0.7f, 0.8f), C("#ffe9a0"));
            mb.Box(M(-1.3f, biome == BiomeId.Badlands ? 3.2f : 3.6f, -0.9f, 0.5f, 1.3f, 0.5f), trim);   // chimney
            mb.Box(M(0, 0.12f, 2.5f, 1.4f, 0.24f, 1.0f), C("#8a8078"));                           // step
        }

        /// <summary>Market stall: counter toward +Z (the customer side), striped awning, crates and a kin perch.</summary>
        static void BuildStall(MeshBuilder mb)
        {
            Color32 wood = C("#7a5232"), wood2 = C("#9a6c42"), red = C("#d84a3a"), cream = C("#fff0d0"), gold = C("#f0c040");
            mb.Box(M(0, 0.55f, 0, 2.8f, 1.1f, 0.8f), wood, wood2);
            for (int i = -1; i <= 1; i += 2)
            {
                mb.Cylinder(M(i * 1.3f, 1.4f, -0.5f, 0.12f, 2.8f, 0.12f), wood, 6, 1f, true);
                mb.Cylinder(M(i * 1.3f, 1.25f, 0.5f, 0.12f, 2.5f, 0.12f), wood, 6, 1f, true);
            }
            for (int i = 0; i < 5; i++)
                mb.Box(M(-1.2f + i * 0.6f, 2.72f, 0, 0.6f, 0.08f, 1.5f, 12, 0, 0), i % 2 == 0 ? red : cream);
            mb.Box(M(-0.8f, 1.25f, 0, 0.5f, 0.3f, 0.5f), C("#c8a060"));                // egg crate
            mb.Sphere(M(-0.8f, 1.5f, 0, 0.3f, 0.38f, 0.3f), cream, 8, 6);
            mb.Sphere(M(0.6f, 1.2f, 0.05f, 0.26f, 0.2f, 0.26f), gold, 8, 5);            // coin dish
            mb.Box(M(1.9f, 0.4f, -0.2f, 0.9f, 0.8f, 0.9f), wood2, wood);                // spare crate
            mb.Box(M(0, 3.05f, 0.7f, 1.3f, 0.5f, 0.08f), gold, cream);                  // sign
        }

        static void BuildBoard(MeshBuilder mb)
        {
            Color32 wood = C("#6a4a30"), wood2 = C("#8a6442"), paper = C("#f4ecd4");
            for (int i = -1; i <= 1; i += 2) mb.Cylinder(M(i * 0.9f, 1.1f, 0, 0.14f, 2.2f, 0.14f), wood, 6, 1f, true);
            mb.Box(M(0, 1.5f, 0, 2.1f, 1.3f, 0.12f), wood, wood2);
            mb.Box(M(0, 2.25f, 0, 2.4f, 0.14f, 0.5f, 0, 0, 0), wood2);
            mb.Box(M(-0.55f, 1.6f, 0.07f, 0.55f, 0.7f, 0.03f, 0, 0, 4), paper);
            mb.Box(M(0.1f, 1.5f, 0.07f, 0.5f, 0.6f, 0.03f, 0, 0, -5), paper);
            mb.Box(M(0.65f, 1.65f, 0.07f, 0.45f, 0.55f, 0.03f, 0, 0, 3), C("#f0d8a0"));
        }

        static void BuildWell(MeshBuilder mb)
        {
            Color32 stone = C("#8a8a92"), stone2 = C("#a8a8b0"), wood = C("#6a4a30"), water = C("#4a9ad8");
            mb.Cylinder(M(0, 0.45f, 0, 1.7f, 0.9f, 1.7f), stone, stone2, 10, 1f, true);
            mb.Cylinder(M(0, 0.9f, 0, 1.3f, 0.05f, 1.3f), water, 10, 1f, true);
            for (int i = -1; i <= 1; i += 2) mb.Cylinder(M(i * 0.75f, 1.6f, 0, 0.12f, 1.6f, 0.12f), wood, 6, 1f, true);
            mb.Cylinder(M(0, 2.6f, 0, 2.2f, 0.7f, 2.2f, 0, 45, 0), C("#a8452e"), C("#d0603a"), 4, 0f, true);
        }

        static void BuildLamp(MeshBuilder mb)
        {
            mb.Cylinder(M(0, 1.2f, 0, 0.1f, 2.4f, 0.1f), C("#4a3a2c"), 6, 1f, true);
            mb.Box(M(0, 2.5f, 0, 0.3f, 0.36f, 0.3f), C("#ffd870"));
            mb.Cylinder(M(0, 2.76f, 0, 0.44f, 0.16f, 0.44f, 0, 45, 0), C("#4a3a2c"), 4, 0f, true);
        }

        static Mesh BannerMesh(Settlement s)
        {
            return Cached("banner_" + s.id, mb =>
            {
                Color32 c = s.color, dark = new Color(s.color.r * 0.6f, s.color.g * 0.6f, s.color.b * 0.6f);
                // gate posts with a hanging banner: the first thing you see from a distance
                for (int i = -1; i <= 1; i += 2) mb.Cylinder(M(i * 2.2f, 2.6f, 0, 0.3f, 5.2f, 0.3f), C("#5a4030"), 7, 1f, true);
                mb.Box(M(0, 5.0f, 0, 5.0f, 0.3f, 0.36f), C("#5a4030"), C("#7a5a40"));
                mb.Box(M(0, 3.9f, 0, 2.2f, 1.8f, 0.06f), dark, c);
                mb.Sphere(M(0, 4.1f, 0.05f, 0.7f, 0.7f, 0.1f), C("#fff6dc"), 8, 6);
            });
        }

        // ============================================================ AGENTS
        static readonly Palette[] Looks =
        {
            Look("#7a3a8a", "#2e2630", "#f0c040", "#e8b890"),   // keeper: purple coat, gold hat
            Look("#c85a3a", "#4a3a2c", "#e8d8a0", "#f0c8a0"),
            Look("#3a7ac8", "#3a3a44", "#c8a060", "#c89868"),
            Look("#5a9a4a", "#5a4630", "#8a5a3a", "#8a6248"),
            Look("#d8a83a", "#3a2c2c", "#b04a3a", "#f4d4b4"),
            Look("#8a8a98", "#2c2c38", "#4ac0c8", "#a87858"),
        };

        static Palette Look(string coat, string legs, string hat, string skin)
        {
            ColorUtility.TryParseHtmlString(coat, out var a); ColorUtility.TryParseHtmlString(legs, out var b);
            ColorUtility.TryParseHtmlString(hat, out var c); ColorUtility.TryParseHtmlString(skin, out var d);
            return new Palette { primary = a, secondary = b, accent = c, glow = d, eye = new Color(0.1f, 0.08f, 0.1f) };
        }

        void SpawnHuman(Settlement s, string name, int look, Vector3 pos, bool keeper, int home)
        {
            var go = new GameObject(keeper ? "Kinkeeper" : "Villager_" + name);
            go.transform.SetParent(s.root.transform, false);
            go.transform.position = OnGround(pos);
            bool hat = keeper || look % 2 == 1;
            var rig = ModelBuilder.BuildCustom("villager" + look + (hat ? "h" : "n"), hat ? PlayerLook.Parts : PlayerLook.PartsNoHat, PlayerLook.Pivots,
                Looks[look % Looks.Length], go.transform, RenderAssets.Creature, Locomotion.Biped, keeper ? 1.05f : 0.94f + look * 0.02f);
            var a = new Agent { tr = go.transform, rig = rig, human = true, keeper = keeper, name = name, speed = 1.5f, radius = 0.35f, home = home, at = home,
                                timer = Random.Range(2f, 8f), face = keeper ? s.pos : (home >= 0 ? s.spots[home].face : s.pos), working = home >= 0 && s.spots[home].work };
            s.agents.Add(a);
        }

        void SpawnKin(Settlement s, System.Random rng, int home)
        {
            // a local kin that knows the job, if there is one
            var job = s.spots[home].job;
            var pool = new List<SpeciesDef>();
            foreach (var sp in ContentDB.SpeciesList)
                if (!sp.starter && sp.rarity <= Rarity.Uncommon && sp.LivesIn(s.biome) && sp.locomotion != Locomotion.Slither && (job == WorkType.None || sp.WorkLevel(job) > 0)) pool.Add(sp);
            if (pool.Count == 0) foreach (var sp in ContentDB.SpeciesList) if (!sp.starter && sp.rarity <= Rarity.Uncommon && sp.LivesIn(s.biome)) pool.Add(sp);
            if (pool.Count == 0) return;
            var pick = pool[rng.Next(pool.Count)];
            var d = new CreatureData { uid = "village", speciesId = pick.id, level = 5, apt = new[] { 50, 50, 50 }, spec = 1 };
            var go = new GameObject("VillageKin_" + pick.id);
            go.transform.SetParent(s.root.transform, false);
            go.transform.position = OnGround(s.spots[home].pos + new Vector3(-0.8f, 0, -0.5f));
            var rig = ModelBuilder.Build(d, go.transform, RenderAssets.Creature);
            s.agents.Add(new Agent { tr = go.transform, rig = rig, name = pick.displayName, speed = Mathf.Clamp(pick.walkSpeed, 1.2f, 2.4f), radius = 0.3f * pick.size,
                                     home = home, at = home, timer = Random.Range(3f, 12f), face = s.spots[home].face, working = s.spots[home].work });
        }

        public void Tick(float dt, PlayerController pl)
        {
            Vector3 pp = pl.Position;
            foreach (var s in All)
            {
                if (s.root == null || !s.root.activeSelf) continue;
                float d2 = (pp - s.pos).sqrMagnitude;
                if (d2 > 130f * 130f) continue;
                if (!s.greeted && d2 < (Radius + 6f) * (Radius + 6f))
                {
                    s.greeted = true;
                    GameEvents.Good($"{s.name} - a settlement. {s.keeper} buys and sells kin at the striped stall.");
                }
                for (int i = 0; i < s.agents.Count; i++) TickAgent(s, s.agents[i], dt, pp);
            }
        }

        void TickAgent(Settlement s, Agent a, float dt, Vector3 playerPos)
        {
            Vector3 pos = a.tr.position;
            if (a.keeper)
            {
                Vector3 look = (playerPos - pos).sqrMagnitude < 14f * 14f ? playerPos - pos : s.pos - pos;
                Turn(a, look, dt);
                a.rig.Animate(dt, 0f, false, false);
                return;
            }
            if (a.moving)
            {
                Vector3 to = a.target - pos; to.y = 0;
                float d = to.magnitude;
                if (d < 0.35f || a.timer <= 0f)
                {
                    a.moving = false;
                    a.timer = Random.Range(7f, 16f);
                    a.rig.Animate(dt, 0f, a.working, false);
                    return;
                }
                a.timer -= dt;   // give up on a blocked path after a while
                Vector3 next = pos + to / d * Mathf.Min(d, a.speed * dt);
                WorldCollision.PushOut(ref next, a.radius);
                next.y = WorldGen.Height(next.x, next.z);
                a.tr.position = next;
                Turn(a, to, dt);
                a.rig.Animate(dt, a.speed, false, false);
                return;
            }
            a.timer -= dt;
            Turn(a, a.face - pos, dt);
            a.rig.Animate(dt, 0f, a.working, false);
            if (a.timer > 0f) return;
            // off to the next place: home job <-> somewhere else in the village
            int next2 = a.at == a.home || a.home < 0 ? Random.Range(0, s.spots.Count) : a.home;
            if (next2 == a.at) next2 = (next2 + 1) % s.spots.Count;
            a.at = next2;
            var spot = s.spots[next2];
            Vector3 side = Vector3.Cross(Vector3.up, spot.face - spot.pos).normalized * (a.human ? 0.8f : -0.8f);
            a.target = spot.pos + side;
            a.face = spot.face;
            a.working = spot.work;
            a.moving = true;
            a.timer = 40f;
        }

        static void Turn(Agent a, Vector3 dir, float dt)
        {
            dir.y = 0;
            if (dir.sqrMagnitude < 0.01f) return;
            a.tr.rotation = Quaternion.RotateTowards(a.tr.rotation, Quaternion.LookRotation(dir), dt * 360f);
        }

        // ============================================================ INTERACTION
        public Settlement NearKeeper(Vector3 p)
        {
            foreach (var s in All)
                if (s.root != null && s.root.activeSelf && (p - s.keeperPos).sqrMagnitude < 3.4f * 3.4f) return s;
            return null;
        }

        public Settlement NearBoard(Vector3 p)
        {
            foreach (var s in All)
                if (s.root != null && s.root.activeSelf && (p - s.boardPos).sqrMagnitude < 3f * 3f) return s;
            return null;
        }

        public Agent NearVillager(Vector3 p, out Settlement where)
        {
            where = null;
            foreach (var s in All)
            {
                if (s.root == null || !s.root.activeSelf || (p - s.pos).sqrMagnitude > (Radius + 10f) * (Radius + 10f)) continue;
                foreach (var a in s.agents)
                    if (a.human && !a.keeper && (p - a.tr.position).sqrMagnitude < 2.2f * 2.2f) { where = s; return a; }
            }
            return null;
        }

        static readonly string[] Lines =
        {
            "The Kinkeeper gets new kin in every half hour. Good ones go fast.",
            "See a red skull over a kin's head? Walk the other way.",
            "A kin that knows Logging will chop right beside you. Mine does.",
            "Shimmer kin work a level better at every job. Worth every Glimmer.",
            "They say rainbow crystal grows on the far slopes of the Rimefang Peaks.",
            "Wild kin keep clear of the village fire. You're safe here.",
            "Sell the Kinkeeper your spares. Better than letting them idle in a box.",
            "Every kin is a little different. Catch two of the same and compare their grades.",
            "Check the board by the gate. Do right by us and the Kinkeeper cuts you a better price.",
            "There are tamers camped a short walk out. Red flag on crossed poles. They don't go easy.",
            "Some kin only come out in the rain, or a blizzard, or right at dawn.",
            "Plant something besides Sunberries. A good meal does more than fill you up.",
        };

        public void Talk(Agent a, Settlement s)
        {
            int i = Mathf.Abs((a.name.GetHashCode() + (int)(GameManager.WorldTime / 90.0)) % Lines.Length);
            GameEvents.Say($"{a.name} of {s.name}: \"{Lines[i]}\"", new Color(1f, 0.92f, 0.75f));
            SynthAudio.Play(Sfx.UiOpen, 1.1f, 0.6f);
        }

        // ============================================================ KIN SHOP
        static int Round5(float v) => Mathf.Max(5, Mathf.RoundToInt(v / 5f) * 5);

        /// <summary>What a kin is worth: rarity, level, its best quality grade, and anything special about it.</summary>
        public static int Value(CreatureData d)
        {
            var sp = d.Species;
            float v = sp.rarity == Rarity.Common ? 60f : sp.rarity == Rarity.Uncommon ? 130f : sp.rarity == Rarity.Rare ? 320f : sp.rarity == Rarity.Epic ? 1200f : 4000f;
            v *= 1f + 0.06f * (d.level - 1);
            v *= 0.7f + 0.9f * d.Apt((KinPath)Mathf.Clamp(d.spec, 0, 2)) / 100f;
            if (d.shimmer) v *= 3f;
            if (d.alpha) v *= 2f;
            v *= 1f + 0.5f * d.stage;
            return Round5(v);
        }

        public static int SellPrice(CreatureData d) => Round5(Value(d) * SellRatio);

        public static string WhyNotSell(CreatureData d)
        {
            if (d.Species.starter) return "Partner";
            if (d.location == KinLocation.Expedition || d.location == KinLocation.Outpost) return "Away";
            if (GameManager.I.Party.All.Count <= 1) return "Last kin";
            return null;
        }

        public static long Bucket => Expeditions.Now / RestockSeconds;
        public static int RestockInSeconds => (int)(RestockSeconds - Expeditions.Now % RestockSeconds);

        /// <summary>What an offer costs you here, after your standing discount.</summary>
        public static int PriceFor(Settlement s, Offer o) => Round5(o.price * Requests.PriceMul(s));

        /// <summary>Six kin are rolled each restock; you see the first four, plus up to two more as your standing grows.</summary>
        public List<Offer> VisibleOffers(Settlement s)
        {
            var all = OffersFor(s);
            int n = Mathf.Min(all.Count, 4 + Requests.ExtraOffers(s));
            return all.GetRange(0, n);
        }

        /// <summary>Kin for sale: two common, one uncommon, one uncommon-or-rare, then two standing extras. Same until the restock.</summary>
        public List<Offer> OffersFor(Settlement s)
        {
            long bucket = Bucket;
            if (offers.TryGetValue(s.id, out var list) && offerBucket[s.id] == bucket) return list;
            list = new List<Offer>();
            var state = Random.state;
            Random.InitState(WorldGen.Seed * 131 + s.index * 7919 + (int)(bucket % 100000));
            var lv = BiomeInfo.LevelRange(s.biome, s.pos);
            for (int i = 0; i < 6; i++)
            {
                Rarity want = i < 2 ? Rarity.Common : i == 5 ? Rarity.Rare : (i == 3 && Random.value < 0.4f ? Rarity.Rare : Rarity.Uncommon);
                var pool = new List<SpeciesDef>();
                foreach (var sp in ContentDB.SpeciesList) if (!sp.starter && sp.rarity == want && sp.LivesIn(s.biome)) pool.Add(sp);
                if (pool.Count == 0) continue;
                var pick = pool[Random.Range(0, pool.Count)];
                var d = CreatureFactory.RollWild(pick, Random.Range(lv.x, lv.y + 1) + (i >= 3 ? 2 : 0), s.biome, false);
                d.hp = -1f;
                list.Add(new Offer { kin = d, price = Value(d), key = $"{s.id}|{bucket}|{i}" });
            }
            Random.state = state;
            offers[s.id] = list;
            offerBucket[s.id] = bucket;
            // forget sold tags from earlier restocks
            var sold = GameManager.I.Player.Stats.kinShopSold;
            sold.RemoveAll(k => !k.Contains("|" + bucket + "|"));
            return list;
        }

        public bool IsSold(Offer o) => GameManager.I.Player.Stats.kinShopSold.Contains(o.key);

        public string Buy(Settlement s, Offer o)
        {
            var gm = GameManager.I;
            var st = gm.Player.Stats;
            if (IsSold(o)) return "Already sold.";
            int price = PriceFor(s, o);
            if (st.glimmer < price) return "Not enough Glimmer.";
            st.glimmer -= price;
            st.kinShopSold.Add(o.key);
            o.kin.uid = CreatureData.NewUid();
            gm.OnKinBought(o.kin, s.keeper);
            SynthAudio.Play(Sfx.CaptureSuccess, 1.1f);
            GameEvents.RaiseInventoryChanged();
            return null;
        }

        public string Sell(CreatureData d)
        {
            string why = WhyNotSell(d);
            if (why != null) return why;
            var gm = GameManager.I;
            int price = SellPrice(d);
            string name = d.DisplayName;
            gm.Party.Release(d);
            gm.Player.Stats.glimmer += price;
            GameEvents.Good($"Sold {name} for {price} Glimmer. It has a good home in the village.");
            SynthAudio.Play(Sfx.Pickup, 0.9f);
            GameEvents.RaiseInventoryChanged();
            return null;
        }
    }
}
