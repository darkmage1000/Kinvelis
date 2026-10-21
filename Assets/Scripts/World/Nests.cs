using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// A small encounter: now and then a wild mother is found guarding a nest with one egg. She is a normal wild kin
    /// (a little tougher, and she fights for the nest: get within a few steps and she charges). Beat her, catch her,
    /// or dart in and TAKE EGG while she is busy - taking it while she lives enrages her. The egg goes to the Hatchery
    /// list and hatches into her species with a better-than-wild roll. One nest at a time; it is gone if you wander
    /// far off. Never in the starter fields, settlements, arenas or camps.
    /// </summary>
    public class Nests : MonoBehaviour
    {
        public static Nests I { get; private set; }

        class Nest
        {
            public Vector3 pos;
            public BiomeId biome;
            public SpeciesDef sp;
            public CreatureActor mother;
            public CreatureData motherData;
            public GameObject go, egg;
            public bool taken, warned, angry;
            public float life, sparkle;
        }

        Nest cur;
        float timer = 150f;

        /// <summary>Where the nest is, once you have noticed it (minimap dot).</summary>
        public Vector3? Marker => cur != null && cur.warned && !cur.taken ? cur.pos : (Vector3?)null;

        public void Init()
        {
            I = this;
            cur = null;
            timer = 150f;
        }

        bool MotherAlive(Nest n) =>
            n.mother != null && n.mother.gameObject.activeSelf && n.mother.Data == n.motherData && n.mother.Mode == CreatureMode.Wild && n.mother.State != AIState.Dead && n.mother.State != AIState.Captured;

        public void Tick(float dt, PlayerController pl)
        {
            if (Caves.InCave) return;
            Vector3 pp = pl.Position;
            if (cur == null)
            {
                timer -= dt;
                if (timer > 0f) return;
                timer = 25f;   // try again soon if no good ground was found
                if (TrySpawn(pl)) timer = 0f;
                return;
            }
            var n = cur;
            n.life -= dt;
            float dx = n.pos.x - pp.x, dz = n.pos.z - pp.z, d2 = dx * dx + dz * dz;
            if (d2 > 240f * 240f || (n.life <= 0f && d2 > 70f * 70f) || (n.taken && d2 > 60f * 60f)) { Remove(); return; }
            bool alive = MotherAlive(n);
            if (alive)
            {
                n.mother.Home = n.pos;
                if (!n.warned && d2 < 26f * 26f)
                {
                    n.warned = true;
                    GameEvents.Say($"A {n.sp.displayName} is guarding a nest here. She will fight anything that comes near her egg.", new Color(1f, 0.85f, 0.6f));
                    SynthAudio.Play(Sfx.UiOpen, 0.7f);
                }
                if (!n.angry && !n.taken && d2 < 7.5f * 7.5f && pl.IsAlive)
                {
                    n.angry = true;
                    n.mother.Provoke(pl);
                    GameEvents.Warn($"The mother {n.sp.displayName} charges!");
                }
            }
            else if (!n.warned && d2 < 26f * 26f) n.warned = true;
            // a soft glint on the egg
            if (!n.taken && d2 < 45f * 45f)
            {
                n.sparkle -= dt;
                if (n.sparkle <= 0f) { n.sparkle = 1.4f; Fx.Sparkle(n.pos + Vector3.up * 0.7f, Color.Lerp(n.sp.palette.glow, Color.white, 0.5f), 3, 0.4f); }
            }
        }

        public string Prompt(Vector3 pp, out System.Action act)
        {
            act = null;
            if (cur == null || cur.taken) return null;
            float dx = cur.pos.x - pp.x, dz = cur.pos.z - pp.z;
            if (dx * dx + dz * dz > 2.5f * 2.5f) return null;
            act = Take;
            return "TAKE EGG";
        }

        void Take()
        {
            var n = cur;
            if (n == null || n.taken) return;
            var gm = GameManager.I;
            var s = gm.Player.Stats;
            if (!KinGrowth.EggRoom(out string full)) { GameEvents.Warn(full); return; }
            var child = CreatureFactory.RollWild(n.sp, 1, n.biome, false);
            child.location = KinLocation.Box;
            if (!child.shimmer && Random.value < 0.06f) Shimmer.Make(child);
            for (int i = 0; i < 3; i++) child.apt[i] = Mathf.Min(100, child.apt[i] + 8);
            KinGrowth.AddEgg(new KinEgg { child = child, readyUnix = Expeditions.Now + KinGrowth.HatchMinutes * 60L, parents = "a wild nest" });
            n.taken = true;
            if (n.egg != null) n.egg.SetActive(false);
            gm.AddPlayerXp(25);
            Fx.Burst(n.pos + Vector3.up * 0.6f, Color.Lerp(n.sp.palette.glow, Color.white, 0.4f), 16, 4f, 0.25f, 0.6f, true);
            SynthAudio.Play(Sfx.CaptureSuccess, 1.3f);
            GameEvents.Good(KinGrowth.HasHatchery ? $"You take the {n.sp.displayName} egg. It hatches in {KinGrowth.HatchMinutes} minutes (Camp > Hatchery)."
                                                  : $"You take the {n.sp.displayName} egg. Build a Hatchery at camp to hatch it.");
            if (MotherAlive(n))
            {
                // she saw that
                n.angry = true;
                n.mother.BossAtkMul = 1.25f;
                n.mother.Provoke(gm.Player);
                GameEvents.Warn($"The {n.sp.displayName} is enraged!");
            }
            GameEvents.RaiseInventoryChanged();
        }

        void Remove()
        {
            var n = cur;
            cur = null;
            timer = Random.Range(240f, 420f);
            if (n == null) return;
            if (MotherAlive(n) && !n.mother.Engaged) n.mother.Despawn();
            else if (n.mother != null && n.mother.Data == n.motherData) n.mother.Persistent = false;   // let the world clean her up
            if (n.go != null) Destroy(n.go);
        }

        // ================================================================ SPAWN
        bool TrySpawn(PlayerController pl)
        {
            Vector3 pp = pl.Position;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                float a = Random.value * Mathf.PI * 2f, d = Random.Range(75f, 115f);
                float x = pp.x + Mathf.Cos(a) * d, z = pp.z + Mathf.Sin(a) * d;
                if (!WorldGen.InsideWorld(x, z)) continue;
                float h = WorldGen.Height(x, z);
                if (h < 1.6f || WorldGen.Normal(x, z, 3f).y < 0.93f) continue;
                if (WorldGen.StartDistance(x, z) < WorldGen.StarterZone) continue;
                var p = new Vector3(x, h, z);
                if (Settlements.Inside(p, Settlements.QuietRing) || Wardens.Inside(p, Wardens.QuietRing) || Caves.BlocksNode(p) || Pois.BlocksNode(p) || Landmarks.NearLandmark(p, 25f)) continue;
                if (BaseManager.I != null && BaseManager.I.HasBase && BaseManager.I.IsInsideBase(p, 30f)) continue;
                var biome = WorldGen.Biome(x, z);
                var pool = new List<SpeciesDef>();
                foreach (var sp in ContentDB.SpeciesList)
                    if (!sp.starter && !sp.bossOnly && sp.spawnWeight > 0f && sp.LivesIn(biome) && sp.rarity >= Rarity.Uncommon && sp.rarity <= Rarity.Rare) pool.Add(sp);
                if (pool.Count == 0) return false;
                var pick = pool[Random.Range(0, pool.Count)];
                var lv = BiomeInfo.LevelRange(biome, p);
                var data = CreatureFactory.RollWildMaybeEvolved(pick, lv.y + 2, biome, false);
                var mother = CreatureSpawner.I.SpawnActor(data, CreatureMode.Wild, p + new Vector3(1.8f, 0f, 1.2f));
                if (mother == null) return false;
                mother.Persistent = true;
                mother.Toughen(1.4f);
                mother.Home = p;
                cur = new Nest { pos = p, biome = biome, sp = pick, mother = mother, motherData = data, life = 480f };
                Build(cur);
                return true;
            }
            return false;
        }

        static Matrix4x4 M(float x, float y, float z, float sx, float sy, float sz, float rx = 0, float ry = 0, float rz = 0) =>
            Matrix4x4.TRS(new Vector3(x, y, z), Quaternion.Euler(rx, ry, rz), new Vector3(sx, sy, sz));

        void Build(Nest n)
        {
            var mb = new MeshBuilder();
            Color32 twig = new Color(0.42f, 0.3f, 0.18f), twigLt = new Color(0.58f, 0.44f, 0.28f), moss = new Color(0.36f, 0.5f, 0.28f);
            mb.Cylinder(M(0, 0.12f, 0, 2.6f, 0.26f, 2.6f), twig, twigLt, 12, 0.86f, true);
            mb.Cylinder(M(0, 0.26f, 0, 1.7f, 0.08f, 1.7f), moss, 10);
            for (int i = 0; i < 16; i++)
            {
                float a = i * 0.3927f;
                mb.Cylinder(M(Mathf.Cos(a) * 1.15f, 0.3f + (i % 3) * 0.06f, Mathf.Sin(a) * 1.15f, 0.09f, 1.3f, 0.09f, 78f + (i % 2) * 10f, a * Mathf.Rad2Deg + 70f + (i % 3) * 12f, 0), i % 2 == 0 ? twig : twigLt, 4);
            }
            for (int i = 0; i < 5; i++)
                mb.Sphere(M(Mathf.Cos(i * 1.3f) * 0.9f, 0.34f, Mathf.Sin(i * 1.3f) * 0.9f, 0.5f, 0.06f, 0.24f, 0, i * 70f, 0), Color.Lerp(n.sp.palette.primary, Color.white, 0.35f), 6, 3);   // shed feathers / fur
            n.go = new GameObject("Nest");
            n.go.transform.SetParent(transform, false);
            n.go.transform.position = n.pos;
            n.go.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("nest");
            n.go.AddComponent<MeshRenderer>().sharedMaterial = RenderAssets.Building;

            // the egg wears its mother's colours
            var eb = new MeshBuilder();
            Color32 shell = Color.Lerp(n.sp.palette.primary, Color.white, 0.55f), shellLow = Color.Lerp(n.sp.palette.primary, Color.white, 0.25f), spot = n.sp.palette.secondary;
            eb.Sphere(M(0, 0.62f, 0, 0.66f, 0.86f, 0.66f), shell, shellLow, 10, 8);
            for (int i = 0; i < 5; i++)
            {
                float a = i * 1.256f, y = 0.5f + (i % 3) * 0.16f;
                eb.Sphere(M(Mathf.Cos(a) * 0.29f, y, Mathf.Sin(a) * 0.29f, 0.16f, 0.2f, 0.1f, 0, -a * Mathf.Rad2Deg + 90f, 0), spot, 5, 4);
            }
            n.egg = new GameObject("Egg");
            n.egg.transform.SetParent(n.go.transform, false);
            n.egg.AddComponent<MeshFilter>().sharedMesh = eb.ToMesh("nest_egg");
            n.egg.AddComponent<MeshRenderer>().sharedMaterial = RenderAssets.Creature;
        }
    }
}
