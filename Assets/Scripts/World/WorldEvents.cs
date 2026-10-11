using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Wildbond
{
    public enum WorldEventType { None, MeteorShower, HerdMigration, StormAlpha, ShimmerNight }

    /// <summary>
    /// Occasional world events near the player (never underground): meteor showers at night leave glowing Star Shard
    /// craters to loot, herds of gentle kin migrate across the land, storms bring a storm-touched alpha, and some
    /// nights shimmer (rare colours x5). One at a time, every ~12-20 minutes of play.
    /// </summary>
    public class WorldEvents : MonoBehaviour
    {
        public static WorldEvents I { get; private set; }
        public WorldEventType Active { get; private set; }
        public float TimeLeft { get; private set; }
        public static float ShimmerMul => I != null && I.Active == WorldEventType.ShimmerNight ? 5f : 1f;

        float nextEvent = 600f;
        class Crater { public Vector3 pos; public GameObject go; public bool looted; }
        readonly List<Crater> craters = new List<Crater>();
        readonly List<CreatureActor> eventKin = new List<CreatureActor>();
        int pendingMeteors;
        float meteorTimer;
        static Mesh craterMesh;

        public static string Title(WorldEventType t)
        {
            switch (t)
            {
                case WorldEventType.MeteorShower: return "Meteor Shower";
                case WorldEventType.HerdMigration: return "Herd Migration";
                case WorldEventType.StormAlpha: return "Storm-touched Alpha";
                case WorldEventType.ShimmerNight: return "Shimmer Night";
            }
            return "";
        }

        public void Init() { I = this; nextEvent = Random.Range(420f, 720f); }

        public void Tick(float dt, PlayerController pl)
        {
            if (Active != WorldEventType.None)
            {
                TimeLeft -= dt;
                if (Active == WorldEventType.MeteorShower) TickMeteors(dt, pl);
                if (TimeLeft <= 0f) End();
                return;
            }
            if (Caves.InCave) return;
            nextEvent -= dt;
            if (nextEvent > 0f) return;
            nextEvent = Random.Range(720f, 1200f);
            Begin(Pick(), pl);
        }

        WorldEventType Pick()
        {
            var env = EnvironmentSystem.I;
            bool night = env != null && env.IsNight;
            bool stormy = env != null && (env.Weather == WeatherType.Rain || env.Weather == WeatherType.Blizzard || env.Weather == WeatherType.Ashfall);
            var opts = new List<WorldEventType> { WorldEventType.HerdMigration };
            if (night) { opts.Add(WorldEventType.MeteorShower); opts.Add(WorldEventType.MeteorShower); opts.Add(WorldEventType.ShimmerNight); }
            if (stormy) { opts.Add(WorldEventType.StormAlpha); opts.Add(WorldEventType.StormAlpha); }
            return opts[Random.Range(0, opts.Count)];
        }

        public void Begin(WorldEventType t, PlayerController pl)
        {
            Active = t;
            switch (t)
            {
                case WorldEventType.MeteorShower:
                    TimeLeft = 360f;
                    pendingMeteors = Random.Range(3, 6);
                    meteorTimer = 4f;
                    GameEvents.Good("EVENT: Meteor Shower! Watch for falling stars and loot their craters.");
                    break;
                case WorldEventType.HerdMigration:
                    TimeLeft = 240f;
                    if (!SpawnHerd(pl)) { Active = WorldEventType.None; return; }
                    break;
                case WorldEventType.StormAlpha:
                    TimeLeft = 300f;
                    if (!SpawnStormAlpha(pl)) { Active = WorldEventType.None; return; }
                    break;
                case WorldEventType.ShimmerNight:
                    TimeLeft = 300f;
                    GameEvents.Good("EVENT: Shimmer Night! Rare-coloured kin are 5x more common for a while.");
                    break;
            }
            SynthAudio.Play(Sfx.UiOpen, 0.7f);
        }

        void End()
        {
            if (Active == WorldEventType.MeteorShower) GameEvents.Say("The meteor shower has passed.", new Color(0.8f, 0.85f, 1f));
            foreach (var c in craters) if (c.go != null) Destroy(c.go);
            craters.Clear();
            foreach (var k in eventKin) if (k != null && k.gameObject.activeSelf && k.Mode == CreatureMode.Wild) k.Persistent = false;
            eventKin.Clear();
            Active = WorldEventType.None;
        }

        // ---------------------------------------------------------------- meteors
        void TickMeteors(float dt, PlayerController pl)
        {
            meteorTimer -= dt;
            if (pendingMeteors > 0 && meteorTimer <= 0f)
            {
                meteorTimer = Random.Range(6f, 14f);
                pendingMeteors--;
                for (int tries = 0; tries < 10; tries++)
                {
                    var off = Random.insideUnitCircle.normalized * Random.Range(35f, 110f);
                    var p = pl.Position + new Vector3(off.x, 0, off.y);
                    float h = WorldGen.Height(p.x, p.z);
                    if (h < 1f || Caves.InZone(p.x, p.z)) continue;
                    p.y = h;
                    StartCoroutine(Fall(p));
                    break;
                }
            }
            // looting: walk into a crater
            foreach (var c in craters)
            {
                if (c.looted || c.go == null) continue;
                if ((c.pos - pl.Position).sqrMagnitude < 3f * 3f)
                {
                    c.looted = true;
                    var gm = GameManager.I;
                    gm.GiveItem("crystal", Random.Range(2, 5), c.pos + Vector3.up);
                    int g = Random.Range(15, 31);
                    gm.Player.Stats.glimmer += g;
                    if (Random.value < 0.35f) gm.GiveItem("starbloom_seed", 1, c.pos + Vector3.up);
                    GameEvents.Good($"Star Shards! +{g} Glimmer");
                    Fx.Burst(c.pos + Vector3.up, new Color(0.7f, 0.85f, 1f), 24, 6f, 0.3f, 0.8f, true);
                    SynthAudio.Play(Sfx.CaptureSuccess, 1.4f);
                    Destroy(c.go);
                }
            }
        }

        System.Collections.IEnumerator Fall(Vector3 ground)
        {
            Vector3 start = ground + new Vector3(-40f, 90f, 25f);
            for (float t = 0f; t < 1.2f; t += Time.deltaTime)
            {
                var p = Vector3.Lerp(start, ground, t / 1.2f);
                Fx.Burst(p, new Color(1f, 0.85f, 0.5f), 4, 2f, 0.5f, 0.6f, true);
                yield return null;
            }
            Fx.Burst(ground + Vector3.up, new Color(1f, 0.8f, 0.45f), 40, 9f, 0.5f, 1f, true);
            SynthAudio.Play(Sfx.Burst, 0.5f);
            GameManager.I.CameraRig.Shake(0.4f);
            craters.Add(new Crater { pos = ground, go = MakeCrater(ground) });
            GameEvents.Say("A star fell nearby!", new Color(0.8f, 0.9f, 1f));
        }

        GameObject MakeCrater(Vector3 p)
        {
            if (craterMesh == null)
            {
                var mb = new MeshBuilder();
                Color32 rim = new Color(0.32f, 0.28f, 0.3f), glow = new Color(0.6f, 0.85f, 1f), tip = Color.white;
                for (int i = 0; i < 8; i++)
                {
                    float a = i * 45f * Mathf.Deg2Rad;
                    mb.Rock(Matrix4x4.TRS(new Vector3(Mathf.Cos(a) * 1.8f, 0.2f, Mathf.Sin(a) * 1.8f), Quaternion.identity, new Vector3(1.1f, 0.5f, 1.1f)), rim, rim, (uint)(i + 5), 6, 4);
                }
                mb.Crystal(Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(0.6f, 1.6f, 0.6f)), glow, tip);
                mb.Crystal(Matrix4x4.TRS(new Vector3(0.5f, 0, 0.3f), Quaternion.Euler(0, 0, -25), new Vector3(0.4f, 1f, 0.4f)), glow, tip);
                mb.Crystal(Matrix4x4.TRS(new Vector3(-0.45f, 0, -0.3f), Quaternion.Euler(0, 0, 25), new Vector3(0.35f, 0.9f, 0.35f)), glow, tip);
                craterMesh = mb.ToMesh("meteor_crater");
            }
            var go = new GameObject("StarCrater");
            go.transform.SetParent(transform, false);
            go.transform.position = p;
            go.AddComponent<MeshFilter>().sharedMesh = craterMesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.Creature;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        public IEnumerable<Vector3> CraterPositions() { foreach (var c in craters) if (!c.looted) yield return c.pos; }

        // ---------------------------------------------------------------- herds
        bool SpawnHerd(PlayerController pl)
        {
            var biome = WorldGen.Biome(pl.Position.x, pl.Position.z);
            var pool = new List<SpeciesDef>();
            foreach (var s in ContentDB.SpeciesList)
                if (!s.starter && s.LivesIn(biome) && s.rarity <= Rarity.Uncommon && (s.temperament == Temperament.Docile || s.temperament == Temperament.Timid) && s.locomotion != Locomotion.Flyer)
                    pool.Add(s);
            if (pool.Count == 0) return false;
            var sp = pool[Random.Range(0, pool.Count)];
            var dir = Random.insideUnitCircle.normalized;
            var from = pl.Position + new Vector3(dir.x, 0, dir.y) * 70f + new Vector3(-dir.y, 0, dir.x) * 30f;
            var to = pl.Position - new Vector3(dir.x, 0, dir.y) * 90f;
            if (WorldGen.Height(from.x, from.z) < 1f || WorldGen.Height(to.x, to.z) < 1f) return false;
            var lv = BiomeInfo.LevelRange(biome, pl.Position);
            int n = Random.Range(6, 10);
            for (int i = 0; i < n; i++)
            {
                var off = Random.insideUnitCircle * 6f;
                var d = CreatureFactory.RollWild(sp, Random.Range(lv.x, lv.y + 1), biome, false);
                var a = CreatureSpawner.I.SpawnActor(d, CreatureMode.Wild, from + new Vector3(off.x, 0, off.y));
                a.Persistent = true;
                a.Migrate(to + new Vector3(off.x, 0, off.y));
                eventKin.Add(a);
            }
            GameEvents.Good($"EVENT: A herd of {n} {sp.displayName} is migrating past you!");
            return true;
        }

        // ---------------------------------------------------------------- storm alpha
        bool SpawnStormAlpha(PlayerController pl)
        {
            var biome = WorldGen.Biome(pl.Position.x, pl.Position.z);
            SpeciesDef sp = pl.Stats.level >= 12 ? ContentDB.Sp("stormcrest") : null;
            if (sp == null)
            {
                var pool = new List<SpeciesDef>();
                foreach (var s in ContentDB.SpeciesList) if (!s.starter && s.LivesIn(biome) && s.rarity >= Rarity.Uncommon && s.rarity <= Rarity.Rare) pool.Add(s);
                if (pool.Count == 0) return false;
                sp = pool[Random.Range(0, pool.Count)];
            }
            Vector3 p = pl.Position;
            for (int tries = 0; tries < 10; tries++)
            {
                var off = Random.insideUnitCircle.normalized * 55f;
                p = pl.Position + new Vector3(off.x, 0, off.y);
                if (WorldGen.Height(p.x, p.z) > 1f) break;
            }
            var lv = BiomeInfo.LevelRange(biome, pl.Position);
            var d = CreatureFactory.RollWild(sp, lv.y + 3, biome, true);
            if (!d.shimmer && Random.value < 0.25f) Shimmer.Make(d);
            var a = CreatureSpawner.I.SpawnActor(d, CreatureMode.Wild, p);
            a.Persistent = true;
            eventKin.Add(a);
            GameEvents.Good($"EVENT: A storm-touched ALPHA {sp.displayName} appeared nearby!");
            return true;
        }
    }
}
