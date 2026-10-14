using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Camp defense: hostile wild kin inside the camp ring are "intruders"; worker actors drop their job to fight
    /// them and go back to work when the camp is clear. Raids periodically send a hostile pack at the camp while
    /// the player is nearby (WorldRules.raidFrequency, 0 = off). Fainted workers leave the field and return
    /// once they've rested (KinRoster revive timer).
    /// </summary>
    public partial class BaseManager
    {
        const float RaidBaseMinutes = 11f;
        float raidTimer = -1f;
        float workerCheckTimer;
        readonly List<CreatureActor> raiders = new List<CreatureActor>();
        readonly HashSet<string> defenders = new HashSet<string>();
        bool raidActive;
        float lastAlarm = -99f;

        public bool UnderAttack => FindIntruder(CorePosition) != null;
        public int RaidersLeft { get { raiders.RemoveAll(r => r == null || !r.IsAlive || r.Mode != CreatureMode.Wild); return raiders.Count; } }

        /// <summary>Nearest hostile wild kin inside (or right at the edge of) the camp ring.</summary>
        public CreatureActor FindIntruder(Vector3 from)
        {
            if (!HasBase) return null;
            CreatureActor best = null;
            float bestD = float.MaxValue;
            float r = Radius + 8f;
            var all = CreatureRegistry.All;
            for (int i = 0; i < all.Count; i++)
            {
                var a = all[i];
                if (a.Mode != CreatureMode.Wild || !a.IsAlive) continue;
                if (!a.Raider && !a.Aggro && a.Sp.temperament != Temperament.Aggressive) continue;
                Vector3 dc = a.Position - CorePosition; dc.y = 0;
                if (dc.magnitude > r) continue;
                float d = (a.Position - from).sqrMagnitude;
                if (d < bestD) { bestD = d; best = a; }
            }
            return best;
        }

        /// <summary>Nearest worker actor able to fight (raiders go for these first).</summary>
        public CreatureActor NearestWorkerActor(Vector3 from)
        {
            CreatureActor best = null;
            float bestD = float.MaxValue;
            foreach (var kv in workerActors)
            {
                var a = kv.Value;
                if (a == null || !a.IsAlive || a.Mode != CreatureMode.Worker) continue;
                float d = (a.Position - from).sqrMagnitude;
                if (d < bestD) { bestD = d; best = a; }
            }
            return best;
        }

        public void OnDefenderEngaged(CreatureActor worker)
        {
            defenders.Add(worker.Data.uid);
            if (Time.time - lastAlarm > 20f)
            {
                lastAlarm = Time.time;
                GameEvents.Warn(raidActive ? "Your camp kin are fighting off the raid!" : "Hostile kin in camp - your workers are defending it!");
            }
        }

        public void OnWorkerFainted(CreatureActor a)
        {
            if (a?.Data != null && workerActors.TryGetValue(a.Data.uid, out var cur) && cur == a) workerActors.Remove(a.Data.uid);
        }

        void TickDefense(float dt, Vector3 playerPos, bool near)
        {
            // fainted workers come back once rested
            workerCheckTimer -= dt;
            if (near && actorsOut && workerCheckTimer <= 0f)
            {
                workerCheckTimer = 2f;
                foreach (var d in GameManager.I.Party.In(KinLocation.Base))
                    if (!d.Fainted && !workerActors.ContainsKey(d.uid)) SpawnWorkerActor(d);
            }

            // raid end
            if (raidActive && RaidersLeft == 0)
            {
                raidActive = false;
                GameEvents.Good("Raid repelled! Your camp kin earned XP.");
                foreach (var d in GameManager.I.Party.In(KinLocation.Base))
                    if (defenders.Contains(d.uid)) GameManager.I.GrantKinXp(d, 25 + d.level * 3);
                defenders.Clear();
                SynthAudio.Play(Sfx.LevelUp, 1.1f);
            }

            float freq = WorldRules.Current.raidFrequency;
            if (freq < 0.05f || !near || GameManager.I.Party.CountIn(KinLocation.Base) == 0) return;
            if (raidTimer < 0f) raidTimer = NextRaidDelay(freq) * 0.6f + 240f;   // grace period after loading / founding
            if (raidActive) return;
            raidTimer -= dt;
            if (raidTimer > 0f) return;
            raidTimer = NextRaidDelay(freq);
            StartRaid(playerPos);
        }

        static float NextRaidDelay(float freq) => RaidBaseMinutes * 60f / freq * Random.Range(0.7f, 1.3f);

        /// <summary>Sends a pack of hostile local kin at the camp. Returns how many spawned.</summary>
        public int StartRaid(Vector3 playerPos)
        {
            var biome = WorldGen.Biome(CorePosition.x, CorePosition.z);
            var pool = new List<SpeciesDef>();
            foreach (var s in ContentDB.SpeciesList)
                if (!s.starter && s.LivesIn(biome) && s.rarity <= Rarity.Uncommon &&
                    (s.temperament == Temperament.Aggressive || s.temperament == Temperament.Territorial)) pool.Add(s);
            if (pool.Count == 0) return 0;

            int avg = 0, n = 0;
            foreach (var d in GameManager.I.Party.In(KinLocation.Base)) { avg += d.level; n++; }
            avg = n > 0 ? avg / n : 3;
            bool night = EnvironmentSystem.I != null && EnvironmentSystem.I.IsNight;
            int count = Mathf.Clamp(1 + CoreLevel + (night ? 1 : 0) + (Random.value < 0.3f ? 1 : 0), 2, 5);
            var sp = pool[Random.Range(0, pool.Count)];

            // come in from a random side, on dry land
            Vector3 origin = CorePosition;
            for (int tries = 0; tries < 12; tries++)
            {
                float ang = Random.value * Mathf.PI * 2f;
                var p = CorePosition + new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * (Radius + 26f);
                if (WorldGen.Height(p.x, p.z) > 0.6f) { origin = p; break; }
            }
            if (origin == CorePosition) return 0;

            int spawned = 0;
            for (int i = 0; i < count; i++)
            {
                if (i > 0 && Random.value < 0.35f) sp = pool[Random.Range(0, pool.Count)];
                var off = Random.insideUnitCircle * 4f;
                var pos = origin + new Vector3(off.x, 0, off.y);
                var data = CreatureFactory.RollWildMaybeEvolved(sp, Mathf.Clamp(avg - 2 + Random.Range(-1, 2), 1, 60), biome, false);
                var a = CreatureSpawner.I.SpawnActor(data, CreatureMode.Wild, pos);
                a.MakeRaider(CorePosition, NearestWorkerActor(pos) ?? (ICombatant)GameManager.I.Player);
                raiders.Add(a);
                spawned++;
            }
            raidActive = spawned > 0;
            defenders.Clear();
            GameEvents.Warn($"RAID! {spawned} hostile {sp.displayName} are charging your camp!");
            SynthAudio.Play(Sfx.Burst, 0.7f);
            return spawned;
        }
    }
}
