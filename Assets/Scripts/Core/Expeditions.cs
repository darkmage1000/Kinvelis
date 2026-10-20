using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    [Serializable]
    public class Expedition
    {
        public string id;
        public BiomeId biome;
        public int minutes;
        public long startUnix, endUnix;
        public List<string> kin = new List<string>();
        public bool announced;
    }

    /// <summary>
    /// Expeditions (Camp > Expeditions): send 1-3 idle kin on a real-time trip (15 min / 1 h / 4 h) to a biome you've
    /// reached. They come back with biome loot scaled by level and Combat/Travel quality, plus Glimmer and XP. Runs
    /// while the game is closed. Concurrent trips: 1 + Hearthstone level.
    /// </summary>
    public static class Expeditions
    {
        public static readonly int[] Durations = { 15, 60, 240 };
        public const int MaxTeam = 3;

        static PlayerStats S => GameManager.I.Player.Stats;
        public static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        public static int MaxConcurrent => 1 + (BaseManager.I != null && BaseManager.I.HasBase ? BaseManager.I.CoreLevel : 0);

        public static bool BiomeOpen(BiomeId b)
        {
            if (b == BiomeId.Meadow) return true;
            var ws = Waystones.I;
            if (ws == null) return false;
            foreach (var s in ws.All) if (s.biome == b && ws.IsFound(s)) return true;
            return false;
        }

        /// <summary>Kin that can be sent: not summoned, not fainted, not already away.</summary>
        public static bool Available(CreatureData d)
        {
            var party = GameManager.I.Party;
            if (d.location == KinLocation.Expedition || d.location == KinLocation.Outpost || d.Fainted) return false;
            if (party.Companion != null && party.Companion.Data == d) return false;
            return d.location == KinLocation.Box || d.location == KinLocation.Party;
        }

        public static string Start(BiomeId biome, int minutes, List<CreatureData> team)
        {
            var s = S;
            if (s.expeditions.Count >= MaxConcurrent) return $"All {MaxConcurrent} expedition slots are busy (upgrade the Hearthstone for more).";
            if (team.Count == 0) return "Pick at least one kin.";
            if (!BiomeOpen(biome)) return "Find a waystone in that region first.";
            var e = new Expedition { id = CreatureData.NewUid(), biome = biome, minutes = minutes, startUnix = Now, endUnix = Now + minutes * 60L };
            foreach (var d in team)
            {
                if (!Available(d)) continue;
                d.location = KinLocation.Expedition;
                d.assignedBuilding = null;
                e.kin.Add(d.uid);
            }
            if (e.kin.Count == 0) return "Those kin can't go right now.";
            s.expeditions.Add(e);
            GameEvents.RaiseRosterChanged();
            GameEvents.Good($"Expedition to {BiomeInfo.Name(biome)} set out ({Label(minutes)}).");
            return null;
        }

        public static string Label(int minutes) => minutes < 60 ? $"{minutes} min" : $"{minutes / 60} h";

        public static float Progress01(Expedition e) => Mathf.Clamp01((float)(Now - e.startUnix) / Mathf.Max(1, e.endUnix - e.startUnix));
        public static bool Done(Expedition e) => Now >= e.endUnix;

        /// <summary>Called a few times a second: announce returns.</summary>
        public static void Tick()
        {
            foreach (var e in S.expeditions)
                if (!e.announced && Done(e))
                {
                    e.announced = true;
                    GameEvents.Good($"Your expedition to {BiomeInfo.Name(e.biome)} is back! Collect it at Camp > Expeditions.");
                    SynthAudio.Play(Sfx.UiOpen, 1.1f);
                }
        }

        public static List<CreatureData> Team(Expedition e)
        {
            var l = new List<CreatureData>();
            foreach (var d in GameManager.I.Party.All) if (e.kin.Contains(d.uid)) l.Add(d);
            return l;
        }

        /// <summary>Loot to camp storage (overflow to the bag), XP to the team, kin back to the Hearth Box.</summary>
        public static void Collect(Expedition e)
        {
            if (!Done(e)) return;
            var gm = GameManager.I;
            var team = Team(e);
            float power = 0f;
            foreach (var d in team) power += d.level * (0.5f + (d.Apt(KinPath.Combat) + d.Apt(KinPath.Travel)) / 200f);
            float hours = e.minutes / 60f;
            float scale = Mathf.Max(1f, power * 0.35f) * Mathf.Pow(hours, 0.85f);
            var rng = new System.Random((int)(e.startUnix % int.MaxValue));
            var loot = new Dictionary<string, int>();
            void Add(string id, float amount) { int n = Mathf.Max(0, Mathf.RoundToInt(amount * (0.8f + (float)rng.NextDouble() * 0.4f))); if (n > 0) { loot.TryGetValue(id, out int c); loot[id] = c + n; } }
            switch (e.biome)
            {
                case BiomeId.Meadow:
                    Add("wood", 6 * scale); Add("fiber", 5 * scale); Add("berry", 4 * scale); Add("pelt", 1.2f * scale); Add("gel", 1f * scale); Add("tether_seed", 1.5f * scale);
                    break;
                case BiomeId.Badlands:
                    Add("ore", 3 * scale); Add("cinder", 1.2f * scale); Add("stone", 5 * scale); Add("ingot", 0.5f * scale); Add("cinder_seed", 0.6f * scale);
                    break;
                default:
                    Add("crystal", 1.2f * scale); Add("pelt", 2f * scale); Add("ingot", 0.6f * scale); Add("ore", 2f * scale); if (rng.NextDouble() < 0.3 * hours) Add("rime_seed", 1);
                    break;
            }
            int glimmer = Mathf.RoundToInt(6f * hours * team.Count + power * 0.2f * hours);
            S.glimmer += glimmer;
            var lines = new List<string>();
            foreach (var kv in loot)
            {
                int left = BaseManager.I != null && BaseManager.I.HasBase ? BaseManager.I.Storage.Add(kv.Key, kv.Value) : kv.Value;
                if (left > 0) left = S.Bag.Add(kv.Key, left);
                lines.Add($"{kv.Value - left}x {ContentDB.ItemName(kv.Key)}");
            }
            // gear find on longer trips
            if (rng.NextDouble() < 0.06 * hours * team.Count)
            {
                string[] gear = e.biome == BiomeId.Meadow ? new[] { "explorer_hood", "explorer_boots", "gatherers_bracelet", "hearty_amulet" }
                              : e.biome == BiomeId.Badlands ? new[] { "ironbark_helm", "guard_ring", "keen_ring", "ember_greaves" }
                              : new[] { "rime_crown", "rime_treads", "windrunner_ring", "glowstone_pendant" };
                string g = gear[rng.Next(gear.Length)];
                if (ContentDB.Item(g) != null) { gm.GiveItem(g, 1, gm.Player.Position + Vector3.up); lines.Add("1x " + ContentDB.ItemName(g) + " (gear!)"); }
            }
            foreach (var d in team)
            {
                d.location = KinLocation.Box;
                gm.GrantKinXp(d, Mathf.RoundToInt(e.minutes * 2.5f), true);
                d.bond += 2f;
            }
            S.expeditions.Remove(e);
            GameEvents.Good($"Expedition loot: +{glimmer} Glimmer, {string.Join(", ", lines)}");
            SynthAudio.Play(Sfx.CaptureSuccess, 1.1f);
            GameEvents.RaiseRosterChanged();
            GameEvents.RaiseInventoryChanged();
        }

        /// <summary>Bring a trip home early: kin return, no loot.</summary>
        public static void Recall(Expedition e)
        {
            foreach (var d in Team(e)) d.location = KinLocation.Box;
            S.expeditions.Remove(e);
            GameEvents.Say("The expedition turned back early - no loot.");
            GameEvents.RaiseRosterChanged();
        }
    }
}
