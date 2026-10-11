using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Wildbond
{
    [Serializable]
    public class OutpostSave
    {
        public int biome;
        public float x, z;
        public List<string> kin = new List<string>();
        public List<string> items = new List<string>();
        public List<int> counts = new List<int>();
        public long lastUnix, caravanUnix;
        public Vector3 Pos => new Vector3(x, WorldGen.Height(x, z), z);
        public BiomeId Biome => (BiomeId)biome;
    }

    /// <summary>
    /// Outposts: a tent on an outer land, founded with an Outpost Kit (one per land). Up to two kin are stationed
    /// there and gather that land's goods in real time, online or not (ore for Quarrying kin, wood for Logging,
    /// fiber and the local crop for Foraging / Cultivating, gel for Irrigating). Every ten minutes a caravan carries
    /// the stock to camp storage (five with a Hauling kin on the crew), or you can COLLECT it there yourself.
    /// With a Waypad in camp each outpost is a fast-travel destination. State lives in PlayerStats.outposts.
    /// </summary>
    public class Outposts : MonoBehaviour
    {
        public static Outposts I { get; private set; }
        public const string KitItem = "outpost_kit";
        public const int Slots = 2, StockCap = 80, CaravanMinutes = 10, MaxCatchUpMinutes = 480;
        const float Reach = 4.5f;

        class View
        {
            public OutpostSave save;
            public GameObject root;
            public string crew;
            public readonly List<CreatureRig> rigs = new List<CreatureRig>();
            public readonly List<GameObject> crewGos = new List<GameObject>();
            public Obstacle obstacle;
        }

        readonly List<View> views = new List<View>();
        static readonly List<Vector3> sites = new List<Vector3>();
        float tickTimer;

        static PlayerStats S => GameManager.I.Player.Stats;
        public static List<OutpostSave> All => S.outposts;

        public void Init() { I = this; sites.Clear(); }

        public static bool BlocksNode(Vector3 p)
        {
            for (int i = 0; i < sites.Count; i++)
            {
                float dx = p.x - sites[i].x, dz = p.z - sites[i].z;
                if (dx * dx + dz * dz < 5f * 5f) return true;
            }
            return false;
        }

        public static string LandName(OutpostSave o) => BiomeInfo.Name(o.Biome);
        public static string TravelId(OutpostSave o) => "outpost:" + o.biome;

        public static OutpostSave ByTravelId(string id)
        {
            if (id == null || !id.StartsWith("outpost:")) return null;
            foreach (var o in All) if (TravelId(o) == id) return o;
            return null;
        }

        /// <summary>A kin left its outpost (moved, released): drop it from the crew.</summary>
        public static void Forget(CreatureData d)
        {
            if (GameManager.I == null || GameManager.I.Player == null) return;
            foreach (var o in All) o.kin.Remove(d.uid);
        }

        // ============================================================ FOUNDING
        public void TryFound(PlayerController pl)
        {
            Vector3 p = pl.Position;
            float h = WorldGen.Height(p.x, p.z);
            if (Caves.InCave) { GameEvents.Warn("Not underground."); return; }
            if (!WorldGen.OuterBiome(p.x, p.z, h, out var biome)) { GameEvents.Warn("Outposts go on the outer lands. Cross the sea first, then use the kit there."); return; }
            if (h < 1.2f) { GameEvents.Warn("Too wet here - find dry ground."); return; }
            if (WorldGen.Normal(p.x, p.z, 2f).y < 0.9f) { GameEvents.Warn("The ground is too steep here."); return; }
            if (Wardens.Inside(p, 70f)) { GameEvents.Warn("Too close to a Warden's arena."); return; }
            foreach (var o in All)
                if (o.Biome == biome) { GameEvents.Warn($"You already have an outpost in {BiomeInfo.Name(biome)}. Pack it up first to move it."); return; }
            S.Bag.Remove(KitItem, 1);
            var save = new OutpostSave { biome = (int)biome, x = p.x, z = p.z, lastUnix = Expeditions.Now, caravanUnix = Expeditions.Now };
            All.Add(save);
            pl.transform.position = p + pl.transform.forward * -2.5f;   // step back so the tent doesn't land on you
            Sync();
            var cm = ChunkManager.I;
            if (cm != null)
            {
                var buf = new List<ResourceNode>();
                cm.Nodes.Query(p, 8f, buf);
                foreach (var n in buf) if (n.alive && BlocksNode(n.pos)) cm.Deplete(n);
            }
            Fx.Sparkle(p + Vector3.up * 1.5f, new Color(1f, 0.85f, 0.5f), 30, 1.6f);
            SynthAudio.Play(Sfx.Build, 1f);
            GameEvents.Good($"Outpost founded in {BiomeInfo.Name(biome)}! Tap OUTPOST at the tent to station two kin - they gather while you're away.");
            GameEvents.RaiseInventoryChanged();
            GameEvents.RaiseBaseChanged();
        }

        public void PackUp(OutpostSave o)
        {
            var gm = GameManager.I;
            foreach (var uid in o.kin.ToArray())
            {
                var d = gm.Party.All.Find(k => k.uid == uid);
                if (d != null) gm.Party.Move(d, KinLocation.Box);
            }
            Collect(o, true);
            int left = S.Bag.Add(KitItem, 1);
            if (left > 0 && BaseManager.I.HasBase) BaseManager.I.Storage.Add(KitItem, 1);
            All.Remove(o);
            Sync();
            GameEvents.Say($"Outpost in {LandName(o)} packed up. The kit is back in your bag and its kin are in the Hearth Box.");
            GameEvents.RaiseInventoryChanged();
            GameEvents.RaiseBaseChanged();
        }

        // ============================================================ CREW
        public static bool CanStation(CreatureData d)
        {
            var party = GameManager.I.Party;
            if (d.Fainted || (party.Companion != null && party.Companion.Data == d)) return false;
            return d.location == KinLocation.Box || d.location == KinLocation.Party;
        }

        public string Station(OutpostSave o, CreatureData d)
        {
            if (o.kin.Count >= Slots) return "This outpost is fully crewed.";
            if (!CanStation(d)) return $"{d.DisplayName} can't go right now.";
            var party = GameManager.I.Party;
            if (d.location == KinLocation.Party && party.CountIn(KinLocation.Party) <= 1) return "Keep at least one kin with you.";
            party.Move(d, KinLocation.Outpost);
            o.kin.Add(d.uid);
            GameEvents.Good($"{d.DisplayName} heads to the {LandName(o)} outpost: {JobText(o, d)}.");
            GameEvents.RaiseRosterChanged();
            return null;
        }

        public void Recall(OutpostSave o, CreatureData d)
        {
            GameManager.I.Party.Move(d, KinLocation.Box);   // Move() drops it from the crew
            GameEvents.Say($"{d.DisplayName} returns to the Hearth Box.");
        }

        public static List<CreatureData> Crew(OutpostSave o)
        {
            var list = new List<CreatureData>();
            foreach (var uid in o.kin)
            {
                var d = GameManager.I.Party.All.Find(k => k.uid == uid);
                if (d != null) list.Add(d);
            }
            return list;
        }

        // ============================================================ PRODUCTION
        public static string OreOf(BiomeId b, out int hardness)
        {
            switch (b)
            {
                case BiomeId.Isles: hardness = 3; return "saltglass";
                case BiomeId.Swamp: hardness = 3; return "bog_iron";
                case BiomeId.Desert: hardness = 4; return "sunsteel_ore";
                case BiomeId.Blight: hardness = 4; return "blight_ore";
                default: hardness = 5; return "starcrystal";
            }
        }

        static string ForageOf(BiomeId b)
        {
            switch (b)
            {
                case BiomeId.Isles: return "berry";
                case BiomeId.Swamp: return "dewmelon";
                case BiomeId.Desert: return "emberpepper";
                case BiomeId.Blight: return "gel";
                default: return "frostmint";
            }
        }

        enum Job { Scraps, Mine, Log, Forage, Water }

        static Job JobOf(CreatureData d, out int level)
        {
            int q = d.WorkLevel(WorkType.Quarrying), l = d.WorkLevel(WorkType.Logging);
            int f = Mathf.Max(d.WorkLevel(WorkType.Foraging), d.WorkLevel(WorkType.Cultivating)), w = d.WorkLevel(WorkType.Irrigating);
            level = Mathf.Max(Mathf.Max(q, l), Mathf.Max(f, w));
            if (level <= 0) { level = 1; return Job.Scraps; }
            if (q == level) return Job.Mine;
            if (l == level) return Job.Log;
            if (f == level) return Job.Forage;
            return Job.Water;
        }

        /// <summary>Items per minute a kin brings in at this outpost.</summary>
        static float Rate(CreatureData d, Job job, int level) => (job == Job.Scraps ? 0.3f : 0.7f * level) * d.WorkMul * WorldRules.Current.workSpeed;

        static bool CanMine(OutpostSave o, int quarryLevel, out string ore)
        {
            ore = OreOf(o.Biome, out int hard);
            return quarryLevel >= hard - 2;
        }

        public static string JobText(OutpostSave o, CreatureData d)
        {
            var job = JobOf(d, out int lv);
            float r = Rate(d, job, lv);
            string haul = d.WorkLevel(WorkType.Hauling) > 0 ? " and speeds the caravan" : "";
            switch (job)
            {
                case Job.Mine:
                    return CanMine(o, lv, out string ore) ? $"mines {ContentDB.ItemName(ore)} and stone (~{r:0.0}/min){haul}"
                        : $"quarries stone (~{r:0.0}/min) - {ContentDB.ItemName(ore)} needs Quarrying Lv {lv + 1}{haul}";
                case Job.Log: return $"cuts wood (~{r:0.0}/min){haul}";
                case Job.Forage: return $"forages fiber and {ContentDB.ItemName(ForageOf(o.Biome))} (~{r:0.0}/min){haul}";
                case Job.Water: return $"collects gel (~{r:0.0}/min){haul}";
                default: return $"picks up scraps (~{r:0.0}/min) - it has no gathering job{haul}";
            }
        }

        static void Add(OutpostSave o, string item, float amount)
        {
            int n = Mathf.FloorToInt(amount);
            if (UnityEngine.Random.value < amount - n) n++;
            if (n <= 0) return;
            int i = o.items.IndexOf(item);
            if (i < 0) { o.items.Add(item); o.counts.Add(0); i = o.items.Count - 1; }
            o.counts[i] = Mathf.Min(StockCap, o.counts[i] + n);
        }

        static void Gather(OutpostSave o, CreatureData d, int minutes)
        {
            var job = JobOf(d, out int lv);
            float total = Rate(d, job, lv) * minutes;
            switch (job)
            {
                case Job.Mine:
                    if (CanMine(o, lv, out string ore)) { Add(o, ore, total * 0.6f); Add(o, "stone", total * 0.4f); }
                    else Add(o, "stone", total);
                    break;
                case Job.Log:
                    if (o.Biome == BiomeId.Desert) { Add(o, "wood", total * 0.5f); Add(o, "fiber", total * 0.5f); }
                    else Add(o, "wood", total);
                    break;
                case Job.Forage: Add(o, "fiber", total * 0.5f); Add(o, ForageOf(o.Biome), total * 0.5f); break;
                case Job.Water: Add(o, "gel", total * 0.6f); break;
                default: Add(o, "stone", total * 0.5f); Add(o, "fiber", total * 0.5f); break;
            }
        }

        public static int Stock(OutpostSave o) { int n = 0; foreach (var c in o.counts) n += c; return n; }

        static bool HasHauler(OutpostSave o)
        {
            foreach (var d in Crew(o)) if (d.WorkLevel(WorkType.Hauling) > 0) return true;
            return false;
        }

        public static int CaravanEvery(OutpostSave o) => (HasHauler(o) ? CaravanMinutes / 2 : CaravanMinutes) * 60;

        /// <summary>Seconds until the next caravan leaves (-1 = none: no crew, no camp or nothing to carry).</summary>
        public static int CaravanIn(OutpostSave o)
        {
            if (o.kin.Count == 0 || !BaseManager.I.HasBase || Stock(o) == 0) return -1;
            return (int)Mathf.Max(0, o.caravanUnix + CaravanEvery(o) - Expeditions.Now);
        }

        void Produce(OutpostSave o)
        {
            long now = Expeditions.Now;
            if (o.lastUnix <= 0) o.lastUnix = now;
            long mins = (now - o.lastUnix) / 60;
            if (mins > 0)
            {
                o.lastUnix += mins * 60;
                int m = (int)Mathf.Min(mins, MaxCatchUpMinutes);
                o.kin.RemoveAll(uid => GameManager.I.Party.All.Find(k => k.uid == uid) == null);
                foreach (var d in Crew(o)) Gather(o, d, m);
            }
            if (o.kin.Count == 0 || !BaseManager.I.HasBase) { o.caravanUnix = now; return; }
            if (now - o.caravanUnix < CaravanEvery(o)) return;
            o.caravanUnix = now;
            int moved = 0;
            var store = BaseManager.I.Storage;
            for (int i = o.items.Count - 1; i >= 0; i--)
            {
                int left = store.Add(o.items[i], o.counts[i]);
                moved += o.counts[i] - left;
                if (left > 0) o.counts[i] = left;
                else { o.items.RemoveAt(i); o.counts.RemoveAt(i); }
            }
            if (moved > 0)
            {
                GameEvents.Say($"A caravan from your {LandName(o)} outpost delivered {moved} items to camp storage.", new Color(0.7f, 1f, 0.8f));
                GameEvents.RaiseInventoryChanged();
            }
        }

        /// <summary>Stock into the bag (then camp storage when packing up). Returns how many items moved.</summary>
        public int Collect(OutpostSave o, bool overflowToCamp = false)
        {
            int moved = 0;
            for (int i = o.items.Count - 1; i >= 0; i--)
            {
                int left = S.Bag.Add(o.items[i], o.counts[i]);
                if (left > 0 && overflowToCamp && BaseManager.I.HasBase) left = BaseManager.I.Storage.Add(o.items[i], left);
                moved += o.counts[i] - left;
                if (left > 0) o.counts[i] = left;
                else { o.items.RemoveAt(i); o.counts.RemoveAt(i); }
            }
            if (moved > 0) { SynthAudio.Play(Sfx.Pickup, 1f); GameEvents.RaiseInventoryChanged(); }
            return moved;
        }

        // ============================================================ TICK + VISUALS
        public OutpostSave InReach(Vector3 p)
        {
            foreach (var v in views)
            {
                float dx = p.x - v.save.x, dz = p.z - v.save.z;
                if (dx * dx + dz * dz < Reach * Reach) return v.save;
            }
            return null;
        }

        public bool PlayerAt(OutpostSave o)
        {
            Vector3 p = GameManager.I.Player.Position;
            float dx = p.x - o.x, dz = p.z - o.z;
            return dx * dx + dz * dz < 14f * 14f;
        }

        public void Tick(float dt, PlayerController pl)
        {
            if (views.Count != All.Count) Sync();
            tickTimer -= dt;
            if (tickTimer <= 0f)
            {
                tickTimer = 5f;
                foreach (var o in All) Produce(o);
                foreach (var v in views) if (v.crew != string.Join(",", v.save.kin)) BuildCrew(v);
            }
            Vector3 pp = pl.Position;
            foreach (var v in views)
            {
                float dx = pp.x - v.save.x, dz = pp.z - v.save.z;
                if (dx * dx + dz * dz > 90f * 90f) continue;
                foreach (var r in v.rigs) r.Animate(dt, 0f, true, false);
            }
        }

        /// <summary>Rebuilds the tents to match the save (after loading, founding or packing up).</summary>
        void Sync()
        {
            foreach (var v in views)
            {
                WorldCollision.RemoveObstacle(v.obstacle);
                Landmarks.List.RemoveAll(l => l.go == v.root);
                Destroy(v.root);
            }
            views.Clear();
            sites.Clear();
            foreach (var o in All)
            {
                var v = new View { save = o };
                BuildTent(v);
                BuildCrew(v);
                views.Add(v);
                sites.Add(o.Pos);
            }
        }

        static Color32 Hx(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
        static Matrix4x4 M(float x, float y, float z, float sx, float sy, float sz, float rx = 0, float ry = 0, float rz = 0) =>
            Matrix4x4.TRS(new Vector3(x, y, z), Quaternion.Euler(rx, ry, rz), new Vector3(sx, sy, sz));

        void BuildTent(View v)
        {
            var o = v.save;
            Vector3 pos = o.Pos;
            v.root = new GameObject("Outpost_" + o.Biome);
            v.root.transform.SetParent(transform, false);
            v.root.transform.position = pos;
            Color tint = BiomeInfo.Tint(o.Biome);
            Color32 canvas = Color.Lerp(new Color(0.86f, 0.78f, 0.6f), tint, 0.35f), trim = Color.Lerp(tint, Color.white, 0.2f);
            Color32 wood = Hx("#6a4a2e"), dark = Hx("#4a3420"), stone = Hx("#8a8a92");
            var mb = new MeshBuilder();
            mb.Cylinder(M(0, 1.25f, 0, 3.4f, 2.5f, 3.4f), canvas, trim, 8, 0.08f, false);          // tent
            mb.Cylinder(M(0, 2.9f, 0, 0.1f, 1.0f, 0.1f), wood, wood, 5, 1f, true);                  // pole
            mb.Box(M(0.42f, 3.15f, 0, 0.8f, 0.45f, 0.04f), trim, trim);                             // pennant
            mb.Box(M(0, 0.75f, 1.52f, 0.9f, 1.5f, 0.06f, -12, 0, 0), dark, dark);                   // door flap
            mb.Box(M(2.2f, 0.4f, 0.6f, 0.9f, 0.8f, 0.9f, 0, 20, 0), wood, dark);                    // crates
            mb.Box(M(2.3f, 1.05f, 0.5f, 0.6f, 0.5f, 0.6f, 0, -15, 0), wood, dark);
            mb.Box(M(2.9f, 0.3f, -0.3f, 0.7f, 0.6f, 0.7f, 0, 40, 0), wood, dark);
            for (int i = 0; i < 6; i++)                                                              // fire ring
            {
                float a = i / 6f * Mathf.PI * 2f;
                mb.Rock(M(-2.4f + Mathf.Cos(a) * 0.5f, 0.12f, 0.8f + Mathf.Sin(a) * 0.5f, 0.3f, 0.24f, 0.3f), stone, stone, (uint)(i + 3), 5, 4, 0.15f);
            }
            mb.Cone(M(-2.4f, 0.4f, 0.8f, 0.4f, 0.7f, 0.4f), Hx("#ff8a2a"), 6);
            v.root.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("outpost_" + o.biome);
            var mr = v.root.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.Building;
            mr.shadowCastingMode = ShadowCastingMode.On;
            v.obstacle = WorldCollision.AddObstacle(pos, 1.7f, this);
            Landmarks.List.Add(new Landmarks.Landmark { name = "Outpost: " + LandName(o), pos = pos, color = new Color(1f, 0.85f, 0.5f), biome = o.Biome, go = v.root, icon = MapIcon.Outpost });
        }

        void BuildCrew(View v)
        {
            foreach (var g in v.crewGos) if (g != null) Destroy(g);
            v.crewGos.Clear();
            v.rigs.Clear();
            var crew = Crew(v.save);
            for (int i = 0; i < crew.Count; i++)
            {
                var d = crew[i];
                var go = new GameObject("Crew_" + d.Species.id);
                go.transform.SetParent(v.root.transform, false);
                Vector3 p = v.save.Pos + new Vector3(i == 0 ? -2.6f : 2.4f, 0, i == 0 ? -1.4f : -1.6f);
                p.y = WorldGen.Height(p.x, p.z);
                go.transform.position = p;
                go.transform.rotation = Quaternion.Euler(0, i == 0 ? 60f : -70f, 0);
                v.crewGos.Add(go);
                v.rigs.Add(ModelBuilder.Build(d, go.transform, RenderAssets.Creature));
            }
            v.crew = string.Join(",", v.save.kin);
        }
    }
}
