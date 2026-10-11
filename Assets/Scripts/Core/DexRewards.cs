using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// What the Kin Dex pays back. Milestones: prizes for the number of different species caught (claimed from the Dex
    /// tab or the Journey page), each one also adding a permanent +2% catch chance. Research ranks: per species, from
    /// how many you have caught and defeated - Known gives +10% catch chance on that species, Mastered +10% damage
    /// to it and half again as many drops. Boss-only kin and the two partners you didn't pick don't count.
    /// </summary>
    public static class DexRewards
    {
        public class Milestone
        {
            public int count;           // species caught (0 = every one of them)
            public int glimmer;
            public string item, item2;
            public int n, n2;
        }

        public const float CatchPerMilestone = 0.02f;

        public static readonly Milestone[] Milestones =
        {
            new Milestone { count = 10, glimmer = 150, item = "ironbark_seed", n = 10 },
            new Milestone { count = 25, glimmer = 300, item = "kin_treat", n = 5, item2 = "satchel", n2 = 1 },
            new Milestone { count = 50, glimmer = 600, item = "starbloom_seed", n = 8, item2 = Wardens.HeartItem, n2 = 1 },
            new Milestone { count = 75, glimmer = 900, item = "kin_treat", n = 10, item2 = "satchel", n2 = 1 },
            new Milestone { count = 100, glimmer = 1500, item = "starbloom_seed", n = 15, item2 = Wardens.HeartItem, n2 = 2 },
            new Milestone { count = 140, glimmer = 2500, item = Wardens.HeartItem, n = 3, item2 = Shimmer.ShardItem, n2 = 25 },
            new Milestone { count = 0, glimmer = 5000, item = Wardens.HeartItem, n = 5, item2 = Shimmer.ShardItem, n2 = 50 },
        };

        // every kind counts: the partners can be earned (Quests: PARTNERS) and the Kraken can be bound with a Deepcall Seed
        public static bool Counts(SpeciesDef sp) => sp != null;

        public static int Total
        {
            get { int n = 0; foreach (var sp in ContentDB.SpeciesList) if (Counts(sp)) n++; return n; }
        }

        public static int Caught(PlayerStats s)
        {
            int n = 0;
            foreach (var sp in ContentDB.SpeciesList)
            {
                if (!Counts(sp)) continue;
                var e = s.DexFor(sp.id, false);
                if (e != null && e.caught > 0) n++;
            }
            return n;
        }

        public static int Need(Milestone m) => m.count > 0 ? m.count : Total;
        public static bool Claimed(PlayerStats s, int i) => s.dexClaimed.Contains(i);
        public static bool Ready(PlayerStats s, int i) => !Claimed(s, i) && Caught(s) >= Need(Milestones[i]);

        public static int ReadyCount(PlayerStats s)
        {
            int caught = Caught(s), n = 0;
            for (int i = 0; i < Milestones.Length; i++) if (!Claimed(s, i) && caught >= Need(Milestones[i])) n++;
            return n;
        }

        /// <summary>The next milestone not yet claimed (-1 = all done).</summary>
        public static int NextIndex(PlayerStats s)
        {
            for (int i = 0; i < Milestones.Length; i++) if (!Claimed(s, i)) return i;
            return -1;
        }

        public static string RewardText(Milestone m)
        {
            var parts = new List<string> { $"{m.glimmer} Glimmer" };
            if (!string.IsNullOrEmpty(m.item)) parts.Add($"{m.n}x {ContentDB.ItemName(m.item)}");
            if (!string.IsNullOrEmpty(m.item2)) parts.Add($"{m.n2}x {ContentDB.ItemName(m.item2)}");
            parts.Add($"+{Mathf.RoundToInt(CatchPerMilestone * 100f)}% catch chance for good");
            return string.Join(", ", parts);
        }

        public static void Claim(int i)
        {
            var gm = GameManager.I;
            var s = gm.Player.Stats;
            if (!Ready(s, i)) return;
            var m = Milestones[i];
            s.dexClaimed.Add(i);
            s.glimmer += m.glimmer;
            Vector3 at = gm.Player.Position + Vector3.up;
            if (!string.IsNullOrEmpty(m.item)) gm.GiveItem(m.item, m.n, at);
            if (!string.IsNullOrEmpty(m.item2)) gm.GiveItem(m.item2, m.n2, at);
            SynthAudio.Play(Sfx.LevelUp, 1f, 0.9f);
            GameEvents.Good($"Dex milestone - {Need(m)} species caught!  {RewardText(m)}.");
            GameEvents.RaiseInventoryChanged();
        }

        public static float MilestoneCatchMul(PlayerStats s) => 1f + CatchPerMilestone * s.dexClaimed.Count;

        // ============================================================ RESEARCH RANKS
        public const int KnownCaught = 3, KnownDefeated = 5, MasterCaught = 5, MasterDefeated = 10;

        /// <summary>0 unknown, 1 Sighted, 2 Studied (caught one), 3 Known, 4 Mastered.</summary>
        public static int Rank(DexEntry e)
        {
            if (e == null || e.seen <= 0) return 0;
            if (e.caught <= 0) return 1;
            if (e.caught >= MasterCaught && e.defeated >= MasterDefeated) return 4;
            if (e.caught >= KnownCaught || e.defeated >= KnownDefeated) return 3;
            return 2;
        }

        public static string RankName(int r) => r >= 4 ? "Mastered" : r == 3 ? "Known" : r == 2 ? "Studied" : r == 1 ? "Sighted" : "Unknown";
        public static Color RankColor(int r) => r >= 4 ? new Color(1f, 0.8f, 0.35f) : r == 3 ? new Color(0.6f, 0.9f, 1f) : r == 2 ? new Color(0.7f, 1f, 0.7f) : new Color(0.8f, 0.8f, 0.8f);

        /// <summary>What the current rank gives and what the next one needs.</summary>
        public static string RankDetail(DexEntry e)
        {
            int r = Rank(e);
            string have = r >= 4 ? "+10% catch chance, +10% damage and +50% drops against this species."
                        : r == 3 ? "+10% catch chance on this species."
                        : "No bonus yet.";
            if (r >= 4) return have;
            string next;
            if (r <= 1) next = "Catch one to study it.";
            else if (r == 2) next = $"Known (+10% catch chance): catch {Mathf.Max(0, KnownCaught - e.caught)} more, or defeat {Mathf.Max(0, KnownDefeated - e.defeated)} more.";
            else next = $"Mastered (+10% damage, +50% drops): caught {Mathf.Min(e.caught, MasterCaught)}/{MasterCaught}, defeated {Mathf.Min(e.defeated, MasterDefeated)}/{MasterDefeated}.";
            return have + "  Next - " + next;
        }

        public static float CatchMul(PlayerStats s, string speciesId) => Rank(s.DexFor(speciesId, false)) >= 3 ? 1.1f : 1f;
        public static float DamageMul(PlayerStats s, string speciesId) => Rank(s.DexFor(speciesId, false)) >= 4 ? 1.1f : 1f;
        public static float DropMul(PlayerStats s, string speciesId) => Rank(s.DexFor(speciesId, false)) >= 4 ? 1.5f : 1f;
    }
}
