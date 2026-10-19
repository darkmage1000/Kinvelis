using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Wild population around the player + LOD'd simulation for every creature actor.
    ///  - capped population (QualityProfile.wildCreatureCap)
    ///  - full AI/animation inside creatureFullSimRange, 1/4-rate AI up to creatureSimRange, despawn beyond
    ///  - pooled actors keyed by model (species + variant)
    ///  - alpha sites near landmarks with respawn cooldowns
    /// </summary>
    public class CreatureSpawner : MonoBehaviour
    {
        public static CreatureSpawner I { get; private set; }

        [SerializeField] float spawnInterval = 0.6f;
        [SerializeField] float minSpawnDist = 30f;
        [SerializeField] float alphaActivateRange = 110f;
        [SerializeField] float alphaRespawnSeconds = 1800f;

        readonly Dictionary<string, Stack<CreatureActor>> pool = new Dictionary<string, Stack<CreatureActor>>();
        readonly List<CreatureActor> tickList = new List<CreatureActor>(128);
        readonly Dictionary<string, CreatureActor> activeAlphas = new Dictionary<string, CreatureActor>();
        /// <summary>Alpha site id -> world time when it may respawn. Persisted.</summary>
        public readonly Dictionary<string, double> AlphaCooldowns = new Dictionary<string, double>();

        float spawnTimer;
        int frame;

        public void Init() { I = this; }

        /// <summary>Population cap = quality budget x world spawn rate.</summary>
        int WildCap => Mathf.Clamp(Mathf.RoundToInt(QualityProfile.Current.wildCreatureCap * WorldRules.Current.wildSpawnRate), 4, 90);

        // ============================================================ POOL
        public CreatureActor Get(CreatureData d)
        {
            string key = d.ModelKey;
            if (pool.TryGetValue(key, out var stack) && stack.Count > 0) return stack.Pop();
            var go = new GameObject("Kin_" + d.speciesId);
            go.transform.SetParent(transform, false);
            go.SetActive(false);
            var a = go.AddComponent<CreatureActor>();
            a.Build(d);
            return a;
        }

        public void ReturnToPool(CreatureActor a)
        {
            if (!pool.TryGetValue(a.PoolKey, out var stack)) pool[a.PoolKey] = stack = new Stack<CreatureActor>();
            if (stack.Count >= 8) { Destroy(a.gameObject); return; }
            stack.Push(a);
        }

        public CreatureActor SpawnActor(CreatureData d, CreatureMode mode, Vector3 pos)
        {
            var a = Get(d);
            a.Spawn(d, mode, pos);
            return a;
        }

        // ============================================================ TICK
        public void Tick(float dt, Vector3 playerPos, Camera cam)
        {
            var q = QualityProfile.Current;
            frame++;

            spawnTimer -= dt;
            if (spawnTimer <= 0f)
            {
                float rate = Mathf.Max(0.1f, WorldRules.Current.wildSpawnRate);
                spawnTimer = spawnInterval / rate;
                if (!Caves.InCave)   // caves place their own inhabitants
                {
                    if (CreatureRegistry.WildCount < WildCap) TrySpawnGroup(playerPos, cam);
                    TickAlphaSites(playerPos);
                }
            }

            tickList.Clear();
            tickList.AddRange(CreatureRegistry.All);
            float despawn = q.creatureSimRange + 25f;
            for (int i = 0; i < tickList.Count; i++)
            {
                var a = tickList[i];
                if (!a.gameObject.activeSelf) continue;
                Vector3 d = a.Position - playerPos; d.y = 0;
                float dist = d.magnitude;
                a.DistToPlayer = dist;
                if (a.Mode == CreatureMode.Wild && !a.DexSeen && dist < 26f && a.IsAlive) { a.DexSeen = true; GameManager.I.OnKinSeen(a); }

                if (a.Mode == CreatureMode.Wild)
                {
                    bool far = dist > despawn || (a.Aggro && dist > despawn + 40f);
                    if (far && a.State != AIState.Captured && !a.Persistent) { if (a.Data.alpha) activeAlphas.Remove(AlphaKey(a)); a.Despawn(); continue; }
                }

                if (dist <= q.creatureFullSimRange || a.Mode == CreatureMode.Companion || a.Engaged)
                {
                    if (a.State != AIState.Captured) a.Rig.SetVisible(true);   // a kin inside a capture seed stays hidden
                    a.Tick(dt, true);
                }
                else
                {
                    // reduced-rate AI; visible only inside fog
                    if (a.State != AIState.Captured) a.Rig.SetVisible(dist < q.fogEnd * 0.9f);
                    int slot = (frame + a.GetInstanceID()) & 3;
                    if (slot == 0) a.Tick(dt * 4f, false);
                }
            }
        }

        string AlphaKey(CreatureActor a)
        {
            foreach (var kv in activeAlphas) if (kv.Value == a) return kv.Key;
            return "";
        }

        void TrySpawnGroup(Vector3 playerPos, Camera cam)
        {
            var q = QualityProfile.Current;
            var env = EnvironmentSystem.I;
            for (int attempt = 0; attempt < 4; attempt++)
            {
                float ang = Random.value * Mathf.PI * 2f;
                float dist = Random.Range(minSpawnDist, q.creatureSimRange - 8f);
                var p = new Vector3(playerPos.x + Mathf.Cos(ang) * dist, 0, playerPos.z + Mathf.Sin(ang) * dist);
                if (!WorldGen.InsideWorld(p.x, p.z)) continue;
                float h = WorldGen.Height(p.x, p.z);
                if (h < 0.5f) continue;
                if (WorldGen.Normal(p.x, p.z, 1.5f).y < 0.75f) continue;
                p.y = h;
                if (BaseManager.I != null && BaseManager.I.IsInsideBase(p, 12f)) continue;
                if (Settlements.Inside(p, Settlements.QuietRing)) continue;   // villages are safe ground
                if (Wardens.Inside(p, Wardens.QuietRing)) continue;           // nothing wanders into a Warden's arena
                if (WorldGen.StartDistance(p.x, p.z) < WorldGen.StartR + 8f) continue;   // nothing spawns on the start plateau
                if (cam != null && dist < 50f)
                {
                    Vector3 v = cam.WorldToViewportPoint(p);
                    if (v.z > 0 && v.x > -0.1f && v.x < 1.1f && v.y > -0.1f && v.y < 1.1f) continue; // don't pop in on screen
                }

                bool nearWater = WorldGen.Height(p.x + 10f, p.z) < 0f || WorldGen.Height(p.x - 10f, p.z) < 0f ||
                                 WorldGen.Height(p.x, p.z + 10f) < 0f || WorldGen.Height(p.x, p.z - 10f) < 0f;
                var biome = WorldGen.Biome(p.x, p.z);
                bool night = env != null && env.IsNight;
                bool gentle = WorldGen.StartDistance(p.x, p.z) < WorldGen.StarterZone;
                var sp = CreatureFactory.PickSpecies(biome, night, nearWater, env, gentle, SubRegions.Find(p));
                if (sp == null) continue;

                var lv = BiomeInfo.LevelRange(biome, p);
                int nightLv = Nightfall.LevelBonus(biome, night);
                lv = new Vector2Int(lv.x + nightLv, lv.y + nightLv);
                if (WorldRules.Current.levelMatched)
                {
                    // never more than a level above the player's strongest kin, wherever they wander
                    int cap = GameManager.I.MatchedLevelCap;
                    lv = new Vector2Int(Mathf.Min(lv.x, cap), Mathf.Min(lv.y, cap));
                }
                int group = Random.Range(sp.minGroup, sp.maxGroup + 1);
                if (gentle) group = Random.value < 0.2f && sp.maxGroup > 1 ? 2 : 1;   // starter area: mostly singles
                for (int i = 0; i < group && CreatureRegistry.WildCount < WildCap; i++)
                {
                    var off = Random.insideUnitCircle * 4f;
                    var sp2 = new Vector3(p.x + off.x, 0, p.z + off.y);
                    var data = CreatureFactory.RollWildMaybeEvolved(sp, Random.Range(lv.x, lv.y + 1), biome, false);
                    SpawnActor(data, CreatureMode.Wild, sp2);
                }
                return;
            }
        }

        void TickAlphaSites(Vector3 playerPos)
        {
            double now = GameManager.WorldTime;
            foreach (var site in Landmarks.AlphaSites)
            {
                float d = Vector2.Distance(new Vector2(site.pos.x, site.pos.z), new Vector2(playerPos.x, playerPos.z));
                if (activeAlphas.TryGetValue(site.id, out var act))
                {
                    if (act == null || !act.gameObject.activeSelf || act.Data == null || !act.Data.alpha || act.State == AIState.Dead)
                        activeAlphas.Remove(site.id);
                    continue;
                }
                if (d > alphaActivateRange || d < 25f) continue;
                if (AlphaCooldowns.TryGetValue(site.id, out double t) && t > now) continue;
                var sp = ContentDB.Sp(site.species);
                if (sp == null) continue;
                var biome = WorldGen.Biome(site.pos.x, site.pos.z);
                var lv = BiomeInfo.LevelRange(biome, site.pos);
                var data = CreatureFactory.RollWild(sp, lv.y + 4, biome, true);
                var a = SpawnActor(data, CreatureMode.Wild, site.pos);
                activeAlphas[site.id] = a;
                GameEvents.Say($"An ALPHA {sp.displayName} prowls nearby...", new Color(1f, 0.75f, 0.3f));
            }
        }

        /// <summary>Called when an alpha is defeated or captured.</summary>
        public void OnAlphaGone(CreatureActor a)
        {
            string key = AlphaKey(a);
            if (string.IsNullOrEmpty(key)) return;
            activeAlphas.Remove(key);
            AlphaCooldowns[key] = GameManager.WorldTime + Mathf.Max(60f, WorldRules.Current.alphaRespawnMinutes * 60f);
        }

        public void DespawnAllWild()
        {
            tickList.Clear();
            tickList.AddRange(CreatureRegistry.All);
            foreach (var a in tickList) if (a.Mode == CreatureMode.Wild) a.Despawn();
            activeAlphas.Clear();
        }
    }
}
