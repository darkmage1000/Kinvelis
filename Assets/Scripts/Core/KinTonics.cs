using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Tonics: dear, permanent stat training for one kin (user, 2026-10-21: "an item that can cost a lot but be used to
    /// boost kin stats... some of my kin have very low def yet very high attack, they get one shot so easy").
    /// Three tonics (health, attack, defence), crafted at the Forge Bench from Prism Shards and Rime Crystal, each giving
    /// one rank (+5%) of its stat to the kin you give it to, up to 10 ranks (+50%) per stat. Ranks live on the kin
    /// (`CreatureData.tonic`) and are read by `MaxHp / Atk / Def`, so they carry into fights, evolutions and trades.
    /// Given from the Kin tab (Training section).
    /// </summary>
    public static class KinTonics
    {
        public const float PerRank = 0.05f;
        public const int MaxRank = 10;

        public static readonly string[] Items = { "tonic_vigor", "tonic_might", "tonic_ironhide" };
        public static readonly string[] Stats = { "Health", "Attack", "Defence" };

        public static int BagCount(int stat) => GameManager.I.Player.Stats.Bag.Count(Items[stat]);

        /// <summary>Gives one tonic of this stat to the kin. Returns an error, or null when it worked.</summary>
        public static string Give(CreatureData d, int stat)
        {
            if (d == null || stat < 0 || stat >= Items.Length) return "Pick a kin first.";
            if (d.tonic == null || d.tonic.Length < 3) { var t = new int[3]; if (d.tonic != null) for (int i = 0; i < d.tonic.Length && i < 3; i++) t[i] = d.tonic[i]; d.tonic = t; }
            if (d.tonic[stat] >= MaxRank) return $"{d.DisplayName}'s {Stats[stat].ToLowerInvariant()} is fully trained.";
            var bag = GameManager.I.Player.Stats.Bag;
            if (bag.Count(Items[stat]) <= 0) return $"You have no {ContentDB.Item(Items[stat])?.displayName ?? "tonic"}. Make them at a Forge Bench.";
            // keep the same share of health: training health also tops the new part up
            float hpFrac = d.hp < 0f ? -1f : d.CurrentHp / Mathf.Max(1f, d.MaxHp);
            bag.Remove(Items[stat], 1);
            d.tonic[stat]++;
            if (hpFrac >= 0f) d.hp = d.MaxHp * hpFrac;
            var comp = GameManager.I.Party.Companion;
            if (comp != null && comp.Data == d)
            {
                float before = comp.MaxHp;
                comp.RecomputeStats();
                if (comp.MaxHp > before) comp.HealAmount(comp.MaxHp - before);
            }
            var worker = BaseManager.I != null ? BaseManager.I.ActorFor(d) : null;
            if (worker != null) worker.RecomputeStats();
            GameEvents.Good($"{d.DisplayName}: {Stats[stat]} rank {d.tonic[stat]}/{MaxRank} (+{d.tonic[stat] * PerRank * 100f:0}%).");
            SynthAudio.Play(Sfx.LevelUp, 1.15f, 0.7f);
            GameEvents.RaiseInventoryChanged();
            GameEvents.RaiseRosterChanged();
            return null;
        }

        /// <summary>"Health 3/10 (+15%)" lines for menus.</summary>
        public static string Line(CreatureData d, int stat) => $"{Stats[stat]} {d.TonicRank(stat)}/{MaxRank} (+{d.TonicRank(stat) * PerRank * 100f:0}%)";
    }
}
