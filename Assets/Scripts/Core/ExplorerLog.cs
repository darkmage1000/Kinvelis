using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// The Explorer's log: how much of each of the eight lands you have found, counted from things the save already
    /// knows (named places, ways underground and their guardians, lookouts, finds, relics, alpha lairs, the Warden, the
    /// land's own plant). Four milestones per land (25 / 50 / 75 / 100%) pay Glimmer and goods; a land at 100% also
    /// gives Prism Shards. Journey > LOG opens the page (MenuUI.Explore.cs).
    /// </summary>
    public static class ExplorerLog
    {
        public static readonly BiomeId[] Lands =
            { BiomeId.Meadow, BiomeId.Isles, BiomeId.Badlands, BiomeId.Swamp, BiomeId.Peaks, BiomeId.Desert, BiomeId.Blight, BiomeId.Glow };
        public static readonly int[] Steps = { 25, 50, 75, 100 };

        public struct Line { public string label; public int have, total; }

        /// <summary>0 (Sunmeadow) .. 7 (Glowdeep): how far along the journey a land is.</summary>
        public static int Tier(BiomeId b) { int i = System.Array.IndexOf(Lands, b); return i < 0 ? 0 : i; }

        /// <summary>The seed a land's rewards hand out: the tier you need there (Capture/Seeds.cs).</summary>
        public static string LandSeed(BiomeId b)
        {
            string[] byTier = { "tether_seed", "ironbark_seed", "cinder_seed", "cinder_seed", "rime_seed", "rime_seed", "sunforged_seed", "starbloom_seed" };
            return byTier[Mathf.Clamp(Tier(b), 0, byTier.Length - 1)];
        }

        /// <summary>The item that only grows in this land.</summary>
        public static string RegionalItem(BiomeId b)
        {
            switch (b)
            {
                case BiomeId.Meadow: return "honeybloom";
                case BiomeId.Badlands: return "obsidian";
                case BiomeId.Peaks: return "frostcap";
                case BiomeId.Isles: return "sea_pearl";
                case BiomeId.Swamp: return "lanternbud";
                case BiomeId.Desert: return "sunfig";
                case BiomeId.Blight: return "sporecap";
                default: return "starmoss";
            }
        }

        static bool IsRegional(string id)
        {
            foreach (var b in Lands) if (RegionalItem(b) == id) return true;
            return false;
        }

        /// <summary>Called whenever the player is given an item: remembers the first of each land's own plant.</summary>
        public static void OnItem(PlayerStats s, string id)
        {
            if (s.regionalFound.Contains(id) || !IsRegional(id)) return;
            s.regionalFound.Add(id);
            var d = ContentDB.Item(id);
            GameEvents.Good($"First {(d != null ? d.displayName : id)}! Logged in your Explorer's log.");
        }

        static readonly Dictionary<string, BiomeId> biomeCache = new Dictionary<string, BiomeId>();
        static int cacheSeed;

        /// <summary>Which land a fixed thing stands in (cached: the page asks for every relic on every rebuild).</summary>
        static BiomeId BiomeOf(string key, Vector3 pos)
        {
            if (cacheSeed != WorldGen.Seed) { biomeCache.Clear(); cacheSeed = WorldGen.Seed; }
            if (!biomeCache.TryGetValue(key, out var b)) biomeCache[key] = b = WorldGen.Biome(pos.x, pos.z);
            return b;
        }

        // ================================================================ COUNTS
        public static List<Line> Lines(BiomeId b, PlayerStats s)
        {
            var list = new List<Line>();
            int have = 0, total = 0;

            foreach (var r in SubRegions.All) if (r.biome == b) { total++; if (s.regionsFound.Contains(r.id)) have++; }
            list.Add(new Line { label = "Named places walked", have = have, total = total });

            if (Caves.I != null)
            {
                have = total = 0; int beat = 0;
                foreach (var e in Caves.I.Entrances)
                {
                    if (e.biome != b) continue;
                    total++;
                    if (s.cavesFound.Contains(e.id)) have++;
                    var rec = s.caveRecords.Find(x => x.id == e.id);
                    if (rec != null && rec.clearedAt > -1e8) beat++;
                }
                list.Add(new Line { label = "Caves, dens and ruins found", have = have, total = total });
                list.Add(new Line { label = "Their guardians beaten", have = beat, total = total });
            }

            have = total = 0; int looks = 0, lookTotal = 0;
            foreach (var p in Pois.All)
            {
                if (p.biome != b) continue;
                if (p.kind == PoiKind.Lookout) { lookTotal++; if (s.lookouts.Contains(p.id)) looks++; continue; }
                total++;
                if (Pois.Done(p)) have++;
            }
            list.Add(new Line { label = "Lookouts climbed", have = looks, total = lookTotal });
            list.Add(new Line { label = "Wrecks, camps and ruins searched", have = have, total = total });

            if (Relics.I != null && Relics.I.Ready)
            {
                have = total = 0;
                foreach (var r in Relics.I.All)
                {
                    if (BiomeOf("r" + r.id, r.pos) != b) continue;
                    total++;
                    if (s.relicsFound.Contains(r.id)) have++;
                }
                if (total > 0) list.Add(new Line { label = "Relics found", have = have, total = total });
            }

            have = total = 0;
            foreach (var a in Landmarks.AlphaSites)
            {
                if (BiomeOf("a" + a.id, a.pos) != b) continue;
                total++;
                if (s.alphaTrophies.Contains(a.species)) have++;
            }
            if (total > 0) list.Add(new Line { label = "Alphas beaten at their lairs", have = have, total = total });

            foreach (var w in Wardens.All)
                if (w.biome == b && !w.optional && !w.final) { list.Add(new Line { label = "Warden beaten: " + w.Name, have = Wardens.Beaten(w.id) ? 1 : 0, total = 1 }); break; }

            string item = RegionalItem(b);
            var def = ContentDB.Item(item);
            list.Add(new Line { label = "Gathered: " + (def != null ? def.displayName : item), have = s.regionalFound.Contains(item) ? 1 : 0, total = 1 });
            return list;
        }

        public static int Percent(BiomeId b, PlayerStats s)
        {
            int have = 0, total = 0;
            foreach (var l in Lines(b, s)) { have += l.have; total += l.total; }
            return total == 0 ? 0 : Mathf.FloorToInt(have * 100f / total);
        }

        public static int Overall(PlayerStats s)
        {
            int have = 0, total = 0;
            foreach (var b in Lands) foreach (var l in Lines(b, s)) { have += l.have; total += l.total; }
            return total == 0 ? 0 : Mathf.FloorToInt(have * 100f / total);
        }

        // ================================================================ MILESTONES
        static string Key(BiomeId b, int step) => (int)b + ":" + step;
        public static bool Claimed(PlayerStats s, BiomeId b, int step) => s.explorerClaimed.Contains(Key(b, step));
        public static bool Ready(PlayerStats s, BiomeId b, int step) => !Claimed(s, b, step) && Percent(b, s) >= step;

        public static int ReadyCount(PlayerStats s)
        {
            int n = 0;
            foreach (var b in Lands)
            {
                int pct = Percent(b, s);
                foreach (int st in Steps) if (pct >= st && !Claimed(s, b, st)) n++;
            }
            return n;
        }

        public static int Glimmer(BiomeId b, int step)
        {
            int baseG = step == 25 ? 60 : step == 50 ? 120 : step == 75 ? 220 : 400;
            return Mathf.RoundToInt(baseG * (1f + Tier(b) * 0.5f) / 10f) * 10;
        }

        /// <summary>The goods that come with a milestone.</summary>
        public static (string item, int count) Goods(BiomeId b, int step)
        {
            int t = Tier(b);
            string seed = LandSeed(b);
            switch (step)
            {
                case 25: return (seed, 5);
                case 50: return ("salve", 3 + t / 2);
                case 75: return (seed, 10);
                default: return (Shimmer.ShardItem, 10);
            }
        }

        public static string RewardText(BiomeId b, int step)
        {
            var g = Goods(b, step);
            return $"{Glimmer(b, step)} Glimmer, {g.count} {ContentDB.ItemName(g.item)}";
        }

        public static void Claim(BiomeId b, int step)
        {
            var gm = GameManager.I;
            var s = gm.Player.Stats;
            if (!Ready(s, b, step)) return;
            s.explorerClaimed.Add(Key(b, step));
            int glim = Glimmer(b, step);
            s.glimmer += glim;
            var g = Goods(b, step);
            gm.GiveItem(g.item, g.count, gm.Player.Position + Vector3.up);
            GameEvents.Good(step == 100 ? $"{BiomeInfo.Name(b)} fully explored! +{glim} Glimmer, {g.count} {ContentDB.ItemName(g.item)}."
                                        : $"{BiomeInfo.Name(b)} {step}% explored: +{glim} Glimmer, {g.count} {ContentDB.ItemName(g.item)}.");
            SynthAudio.Play(Sfx.LevelUp, 1f);
            GameEvents.RaiseInventoryChanged();
        }
    }
}
