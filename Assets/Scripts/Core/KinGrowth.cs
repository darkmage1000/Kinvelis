using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    [Serializable]
    public class KinEgg
    {
        public CreatureData child;
        public long readyUnix;
        public string parents;
        /// <summary>
        /// Carried, not warming: it sits in your bag or camp storage as a Kin Egg and waits for a place in a Hatchery
        /// (KinGrowth.Warm). Old saves' eggs load false = already warming.
        /// </summary>
        public bool held;
    }

    /// <summary>
    /// Evolution (Ascended at Lv 20, Exalted at Lv 35: bigger, glowing, +15% stats per stage, +10% work) and
    /// breeding at the Hatchery (two parents -> an egg that inherits the best of their quality grades and traits,
    /// small shimmer / mutation chance, hatches after 10 minutes of real time).
    /// </summary>
    public static class KinGrowth
    {
        // ================================================================ EVOLUTION
        public const int Stage1Level = 20, Stage2Level = 35;

        static string ElementMaterial(Element e)
        {
            switch (e)
            {
                case Element.Ember: return "cinder";
                case Element.Frost: case Element.Lumen: return "crystal";
                case Element.Tide: return "gel";
                case Element.Stone: case Element.Volt: return "ore";
                default: return "pelt";
            }
        }

        public static Ingredient[] EvolveCost(CreatureData d)
        {
            if (d.stage == 0) return new[] { new Ingredient("ingot", 4), new Ingredient(ElementMaterial(d.Element1), 6) };
            return new[] { new Ingredient("ingot", 8), new Ingredient("crystal", 6), new Ingredient("cinder", 6) };
        }

        public static int EvolveLevel(CreatureData d) => d.stage == 0 ? Stage1Level : Stage2Level;
        public static int EvolveGlimmer(CreatureData d) => d.stage == 0 ? 0 : 200;
        public static bool CanEvolveStage(CreatureData d) => d.stage < 2;

        public static string WhyNot(CreatureData d)
        {
            if (!CanEvolveStage(d)) return "Fully evolved.";
            if (d.level < EvolveLevel(d)) return $"Needs Lv {EvolveLevel(d)}.";
            if (!ItemSources.Has(EvolveCost(d))) return "Missing materials.";
            if (GameManager.I.Player.Stats.glimmer < EvolveGlimmer(d)) return $"Needs {EvolveGlimmer(d)} Glimmer.";
            return null;
        }

        /// <summary>quiet = the evolution show tells the player (no toast, sound or sparkle); free = dev mode, no level or cost.</summary>
        public static bool Evolve(CreatureData d, bool quiet = false, bool free = false)
        {
            if (free ? !CanEvolveStage(d) : WhyNot(d) != null) return false;
            var gm = GameManager.I;
            if (!free)
            {
                ItemSources.Consume(EvolveCost(d));
                gm.Player.Stats.glimmer -= EvolveGlimmer(d);
            }
            float hpFrac = d.CurrentHp / d.MaxHp;
            bool wasOut = gm.Party.Companion != null && gm.Party.Companion.Data == d;
            if (wasOut) gm.Party.Recall(false);
            d.stage++;
            d.ResetLearnset();   // an Exalted form may bring an element and a skill
            if (d.hp >= 0f) d.hp = d.MaxHp * hpFrac;
            var worker = BaseManager.I != null ? BaseManager.I.ActorFor(d) : null;
            if (worker != null) BaseManager.I.RespawnWorker(d);
            if (wasOut) gm.Party.Summon(d);
            GameEvents.RaiseRosterChanged();
            GameEvents.RaiseInventoryChanged();
            if (quiet) return true;
            GameEvents.Good($"{d.Species.displayName} evolved into its {CreatureData.StageName(d.stage)} form! (+15% HP/ATK/DEF, bigger, +10% work)");
            SynthAudio.Play(Sfx.LevelUp, 0.8f);
            Fx.Sparkle(gm.Player.Position + Vector3.up * 1.5f, ElementInfo.Color(d.Element1), 40, 1.4f);
            GameEvents.RaiseRosterChanged();
            GameEvents.RaiseInventoryChanged();
            return true;
        }

        // ================================================================ BREEDING
        /// <summary>
        /// Eggs (user, 2026-10-21: "eggs should take up an inv slot each", then "hatchery gets higher level tiers, allow 2
        /// eggs at a time, allow 5 eggs at a time and auto grab them from storage"). An egg only warms inside a Hatchery:
        /// a level 1 Hatchery has 1 place, level 2 has 2, level 3 has 5 (`PlacesAt`; upgraded like any structure, MANAGE >
        /// UPGRADE). Until it has a place an egg is `held`: one `EggItem` in your bag or camp storage, a slot each. A
        /// Hatchery with a free place takes the next egg from camp storage by itself (`SyncEggSlots`), or you put one in
        /// from your bag standing at it (`WarmFromBag`). The `KinEgg` (its kin, its timer) stays in `PlayerStats.eggs`;
        /// the items only stand for the held ones, kept in step by `SyncEggSlots`.
        /// </summary>
        public const string EggItem = "kin_egg";
        static readonly int[] PlacesByLevel = { 1, 2, 5 };
        public static int PlacesAt(int level) => PlacesByLevel[Mathf.Clamp(level, 1, PlacesByLevel.Length) - 1];

        /// <summary>Warming places in every Hatchery in camp.</summary>
        public static int Places
        {
            get
            {
                if (BaseManager.I == null || !BaseManager.I.HasBase) return 0;
                int n = 0;
                foreach (var b in BaseManager.I.Buildings) if (b.Def.id == "hatchery") n += PlacesAt(b.Level);
                return n;
            }
        }
        public static int WarmingCount { get { int n = 0; foreach (var e in GameManager.I.Player.Stats.eggs) if (!e.held) n++; return n; } }
        public static int HeldCount { get { int n = 0; foreach (var e in GameManager.I.Player.Stats.eggs) if (e.held) n++; return n; } }
        public static bool FreePlace => WarmingCount < Places;

        /// <summary>Room for one more carried egg: a free bag slot beyond any held egg still waiting for one.</summary>
        public static bool EggRoom(out string why)
        {
            var s = GameManager.I.Player.Stats;
            int items = s.Bag.Count(EggItem) + (BaseManager.I != null && BaseManager.I.HasBase ? BaseManager.I.Storage.Count(EggItem) : 0);
            int waiting = Mathf.Max(0, HeldCount - items);
            bool ok = s.Bag.SpaceFor(EggItem) > waiting;
            why = ok ? null : "Your bag is full - an egg needs a free slot.";
            return ok;
        }

        /// <summary>A new egg you carry: into the egg list and a bag slot. It starts warming once it is in a Hatchery.</summary>
        public static void AddEgg(KinEgg e)
        {
            var s = GameManager.I.Player.Stats;
            e.held = true;
            s.eggs.Add(e);
            s.Bag.Add(EggItem, 1);
            GameEvents.RaiseInventoryChanged();
        }

        /// <summary>The oldest held egg starts warming in a free Hatchery place (its Kin Egg item is taken by the caller).</summary>
        static KinEgg Warm()
        {
            foreach (var e in GameManager.I.Player.Stats.eggs)
                if (e.held) { e.held = false; e.readyUnix = Expeditions.Now + HatchMinutes * 60L; return e; }
            return null;
        }

        /// <summary>At the Hatchery: put an egg from your bag in a free place.</summary>
        public static string WarmFromBag()
        {
            var s = GameManager.I.Player.Stats;
            if (!FreePlace) return "Every place in the Hatchery is warming an egg. Upgrade it for more places.";
            if (s.Bag.Count(EggItem) <= 0) return "You have no eggs in your bag.";
            s.Bag.Remove(EggItem, 1);
            Warm();
            GameEvents.Good($"The egg is warming. It hatches in {HatchMinutes} minutes.");
            SynthAudio.Play(Sfx.Build, 1.3f, 0.5f);
            GameEvents.RaiseInventoryChanged();
            return null;
        }

        /// <summary>
        /// Twice a second: one Kin Egg item (bag or camp storage) per held egg - extras go, a held egg with no item yet (a
        /// full bag) gets one when there is room - and a Hatchery with a free place takes the next egg from camp storage.
        /// </summary>
        public static void SyncEggSlots()
        {
            var s = GameManager.I.Player.Stats;
            var store = BaseManager.I != null && BaseManager.I.HasBase ? BaseManager.I.Storage : null;
            int inBag = s.Bag.Count(EggItem), inStore = store != null ? store.Count(EggItem) : 0;
            int held = HeldCount, items = inBag + inStore;
            bool changed = false;
            if (items > held)
            {
                int extra = items - held, fromBag = Mathf.Min(extra, inBag);
                s.Bag.Remove(EggItem, fromBag);
                if (extra > fromBag && store != null) store.Remove(EggItem, extra - fromBag);
                changed = true;
            }
            else if (items < held)
            {
                int left = s.Bag.Add(EggItem, held - items);
                if (left > 0 && store != null) store.Add(EggItem, left);
                changed = true;
            }
            // the Hatchery helps itself to eggs left in camp storage
            while (store != null && FreePlace && store.Count(EggItem) > 0 && HeldCount > 0)
            {
                store.Remove(EggItem, 1);
                Warm();
                GameEvents.Good($"The Hatchery took an egg from storage. It hatches in {HatchMinutes} minutes.");
                changed = true;
            }
            if (changed) GameEvents.RaiseInventoryChanged();
        }
        public const int HatchMinutes = 10;
        public static Ingredient[] BreedCost => new[] { new Ingredient("berry_tart", 2) };

        public static bool HasHatchery => BaseManager.I != null && BaseManager.I.HasBase && BaseManager.I.Buildings.Exists(b => b.Def.id == "hatchery");

        public static bool CanParent(CreatureData d) => d != null && !d.Fainted && d.location != KinLocation.Expedition && d.location != KinLocation.Outpost;

        public static string Breed(CreatureData a, CreatureData b)
        {
            var s = GameManager.I.Player.Stats;
            if (!HasHatchery) return "Build a Hatchery at camp first.";
            if (a == null || b == null || a == b) return "Pick two different kin.";
            if (!CanParent(a) || !CanParent(b)) return "Both parents must be rested and at home.";
            bool place = FreePlace;
            if (!place && !EggRoom(out _)) return "Every Hatchery place is taken and your bag is full - an egg needs a free slot.";
            if (!ItemSources.Has(BreedCost)) return "Needs 2 Berry Tarts (cook them at a Campfire).";
            ItemSources.Consume(BreedCost);
            var child = MakeChild(a, b);
            var egg = new KinEgg { child = child, parents = $"{a.DisplayName} + {b.DisplayName}" };
            if (place)
            {
                // straight into a free place
                egg.readyUnix = Expeditions.Now + HatchMinutes * 60L;
                s.eggs.Add(egg);
                GameEvents.Good($"An egg! It is warming and hatches in {HatchMinutes} minutes.");
            }
            else
            {
                AddEgg(egg);
                GameEvents.Good("An egg! Every place is warming one, so it's in your bag - the Hatchery takes it from camp storage when a place frees up.");
            }
            SynthAudio.Play(Sfx.CaptureSuccess, 1.3f);
            GameEvents.RaiseInventoryChanged();
            return null;
        }

        static readonly HashSet<string> NoInherit = new HashSet<string> { "apex", "radiant", "crystalline", "colossal", "luminous", "twinned" };

        /// <summary>Species from either parent; each quality path = the better parent's ±; good traits pass on.</summary>
        static CreatureData MakeChild(CreatureData a, CreatureData b)
        {
            var sp = UnityEngine.Random.value < 0.5f ? a.Species : b.Species;
            var src = sp == a.Species ? a : b;
            var d = new CreatureData
            {
                uid = CreatureData.NewUid(),
                speciesId = sp.id,
                level = 1,
                regional = src.regional,
                shimmer = UnityEngine.Random.value < (a.shimmer || b.shimmer ? 0.06f : 0.012f),
                abilities = (string[])sp.abilities.Clone(),
                location = KinLocation.Box
            };
            // rare ability combo passes on 60% of the time when a same-species parent has it
            foreach (var p in new[] { a, b })
                if (p.speciesId == sp.id && p.abilities != null && sp.rareAbilities != null)
                    foreach (var ab in p.abilities)
                        if (Array.IndexOf(sp.rareAbilities, ab) >= 0 && UnityEngine.Random.value < 0.6f)
                        { var l = new List<string>(d.abilities); if (l.Count >= 2) l[1] = ab; else l.Add(ab); d.abilities = l.ToArray(); }

            d.look = KinLook.Inherit(a.Look, b.Look);   // it takes after both of them
            a.EnsureAptitude(); b.EnsureAptitude();
            d.apt = new int[3];
            int best = 0;
            for (int i = 0; i < 3; i++)
            {
                d.apt[i] = Mathf.Clamp(Mathf.Max(a.apt[i], b.apt[i]) + UnityEngine.Random.Range(-6, 5), 25, 100);
                if (d.apt[i] > d.apt[best]) best = i;
            }
            d.spec = best;
            if (UnityEngine.Random.value < 0.15f) d.apt[best] = Mathf.Min(100, d.apt[best] + 5);   // "bloom"

            // good traits from either parent (max 2), mutations are rare
            var pool = new List<string>();
            foreach (var p in new[] { a, b })
                foreach (var t in p.traits)
                {
                    var td = ContentDB.Trait(t);
                    if (td != null && td.positive && !NoInherit.Contains(t) && !pool.Contains(t)) pool.Add(t);
                }
            foreach (var t in pool) if (d.traits.Count < 2 && UnityEngine.Random.value < 0.5f) d.traits.Add(t);
            if (UnityEngine.Random.value < 0.03f)
            {
                d.mutation = (Mutation)UnityEngine.Random.Range(1, 5);
                d.traits.Add(d.mutation == Mutation.Crystalline ? "crystalline" : d.mutation == Mutation.Giant ? "colossal" : d.mutation == Mutation.Twintail ? "twinned" : "luminous");
            }
            if (d.shimmer) Shimmer.Make(d);
            return d;
        }

        /// <summary>Trader's mystery egg: a random kin you've seen (any rarity up to Rare), with a better-than-wild roll.</summary>
        public static KinEgg MysteryEgg()
        {
            var s = GameManager.I.Player.Stats;
            var pool = new List<SpeciesDef>();
            foreach (var sp in ContentDB.SpeciesList)
                if (!sp.starter && sp.rarity <= Rarity.Rare && s.DexFor(sp.id, false) != null) pool.Add(sp);
            if (pool.Count == 0) foreach (var sp in ContentDB.SpeciesList) if (!sp.starter && sp.rarity <= Rarity.Uncommon) pool.Add(sp);
            var pick = pool[UnityEngine.Random.Range(0, pool.Count)];
            var d = CreatureFactory.RollWild(pick, 1, BiomeId.Meadow, false);
            d.location = KinLocation.Box;
            if (!d.shimmer && UnityEngine.Random.value < 0.04f) Shimmer.Make(d);
            for (int i = 0; i < 3; i++) d.apt[i] = Mathf.Min(100, d.apt[i] + 6);
            return new KinEgg { child = d, readyUnix = Expeditions.Now + HatchMinutes * 60L, parents = Trader.Name };
        }

        public static bool EggReady(KinEgg e) => e != null && !e.held && Expeditions.Now >= e.readyUnix;

        /// <summary>The first egg that is ready to hatch, or null.</summary>
        public static KinEgg FirstReady()
        {
            var s = GameManager.I != null ? GameManager.I.Player.Stats : null;
            if (s == null) return null;
            foreach (var e in s.eggs) if (EggReady(e)) return e;
            return null;
        }

        /// <summary>"Your egg hatches in 4m 10s." / "No eggs." for prompts.</summary>
        public static string EggLine()
        {
            var s = GameManager.I.Player.Stats;
            int held = HeldCount;
            string carried = held > 0 ? $"  {held} egg{(held == 1 ? "" : "s")} waiting for a place (bag or camp storage)." : "";
            if (WarmingCount == 0) return (held > 0 ? "No egg is warming yet." : "No eggs in the Hatchery.") + carried;
            long left = long.MaxValue;
            foreach (var e in s.eggs) if (!e.held) left = System.Math.Min(left, e.readyUnix - Expeditions.Now);
            return (left <= 0 ? "An egg is ready to hatch!" : $"The next egg hatches in {left / 60}m {left % 60}s.") + carried;
        }

        /// <summary>The egg's kin joins the roster. quiet = the hatching show tells the player, so no toasts.</summary>
        public static CreatureData Hatch(KinEgg e, bool quiet = false)
        {
            if (e == null || !EggReady(e)) return null;
            var s = GameManager.I.Player.Stats;
            if (!s.eggs.Remove(e)) return null;   // a warming egg has no item: nothing to take from the bag
            GameEvents.RaiseInventoryChanged();
            var d = e.child;
            GameManager.I.OnKinHatched(d, quiet);
            return d;
        }

        static readonly HashSet<string> told = new HashSet<string>();

        /// <summary>Twice a second (GameManager.UpdateGoals): one note when an egg becomes ready to hatch.</summary>
        public static void TickReady()
        {
            var s = GameManager.I.Player.Stats;
            foreach (var e in s.eggs)
            {
                if (e.child == null || told.Contains(e.child.uid) || !EggReady(e)) continue;
                told.Add(e.child.uid);
                GameEvents.Good(WorldRules.Current.simpleControls ? "An egg is wobbling! Go to the Hatchery to see who is inside." : "An egg is wobbling - it's ready to hatch! Walk up to the Hatchery.");
                SynthAudio.Play(Sfx.Shake, 1.1f, 0.8f);
            }
        }
    }
}
