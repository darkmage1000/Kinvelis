using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Dev mode: everything a tester needs to reach any part of the game in a minute. Switched on in Settings
    /// (`GameSettings.devMode`, only offered while `GameInfo.DevBuild` is true - set that false for a store build),
    /// which adds a DEV button to the HUD and the DEV menu (`MenuUI.Dev.cs`).
    ///
    /// Two halves. **Switches** change a rule while they are on and are read at the use site through the getters here
    /// (so they are all false the moment dev mode is off): God, Stamina, OneHit, SureCatch, WorkMul, NoSleep, FreeAll.
    /// They are not saved: a fresh launch starts with all of them off. **Actions** do one thing now: give, spawn,
    /// level, finish, teleport, force an event. Actions change the save like playing would - test on a spare world.
    /// </summary>
    public static class Dev
    {
        public static bool On => GameInfo.DevBuild && GameManager.I != null && GameManager.I.Settings != null && GameManager.I.Settings.devMode;

        // ---------------------------------------------------------------- switches
        public static bool god, stamina, oneHit, sureCatch, fastWork, noSleep, freeAll;
        /// <summary>Nothing hurts you.</summary>
        public static bool God => god && On;
        /// <summary>Stamina never runs down.</summary>
        public static bool Stamina => stamina && On;
        /// <summary>You and your kin defeat anything in one hit.</summary>
        public static bool OneHit => oneHit && On;
        /// <summary>Every seed that lands binds.</summary>
        public static bool SureCatch => sureCatch && On;
        /// <summary>Camp work and hands-off steps run ten times as fast.</summary>
        public static float WorkMul => fastWork && On ? 10f : 1f;
        /// <summary>Camp kin work through the night.</summary>
        public static bool NoSleep => noSleep && On;
        /// <summary>Crafting, building, research and upgrades cost nothing and ignore level, research, Sigil, blueprint and key gates.</summary>
        public static bool FreeAll => freeAll && On;
        /// <summary>The next chest opened holds a blueprint (if its land has one left).</summary>
        public static bool forceBlueprint;

        static GameManager G => GameManager.I;
        static PlayerController P => G.Player;
        static PlayerStats S => G.Player.Stats;
        static BaseManager B => BaseManager.I;
        static void Say(string s) => GameEvents.Say("[dev] " + s, new Color(1f, 0.75f, 0.95f));

        // ---------------------------------------------------------------- player
        public static void SetLevel(int level)
        {
            S.level = Mathf.Clamp(level, 1, 50);
            S.xp = 0;
            S.hp = S.MaxHp;
            Say($"Explorer level {S.level}.");
        }

        public static void Heal() { S.hp = S.MaxHp; S.stamina = S.MaxStamina; S.hunger = 100f; Say("Health, stamina and food full."); }
        public static void Glimmer(int n) { S.glimmer += n; Say($"+{n} Glimmer ({S.glimmer})."); }
        public static void SkillPoints(int n) { S.skillPoints += n; Say($"+{n} skill points."); }
        public static void Hurt(float frac) { S.hp = Mathf.Max(1f, S.MaxHp * frac); Say($"Health set to {frac * 100f:0}%."); }

        // ---------------------------------------------------------------- items
        /// <summary>Into the bag; what does not fit goes to camp storage.</summary>
        public static int Give(string id, int n, bool quiet = false)
        {
            if (ContentDB.Item(id) == null) return 0;
            int left = S.Bag.Add(id, n);
            if (left > 0 && B != null && B.HasBase) left = B.Storage.Add(id, left);
            GameEvents.RaiseInventoryChanged();
            if (!quiet) Say($"+{n - left} {ContentDB.ItemName(id)}" + (left > 0 ? $" ({left} did not fit)" : ""));
            return n - left;
        }

        public static void GivePack(string pack)
        {
            int kinds = 0;
            foreach (var d in ContentDB.Items.Values)
            {
                int n = 0;
                switch (pack)
                {
                    case "materials": if (d.category == ItemCategory.Resource && !BaseManager.IsFoodItem(d)) n = 99; break;
                    case "food": if (BaseManager.IsFoodItem(d)) n = 20; break;
                    case "tools": if (d.category == ItemCategory.Tool || d.category == ItemCategory.Weapon) n = 1; break;
                    case "seeds": if (d.category == ItemCategory.CaptureSeed && string.IsNullOrEmpty(d.legendFor)) n = 30; break;
                    case "gear": if (d.category == ItemCategory.Gear) n = 1; break;
                    case "blueprints": if (Chests.IsBlueprint(d.id, out _)) n = 1; break;
                }
                if (n > 0 && Give(d.id, n, true) > 0) kinds++;
            }
            if (pack == "tools") { Give("arrow", 99, true); Give("bolt", 99, true); Give("stone", 99, true); }
            Say($"Gave the {pack} pack: {kinds} kinds of item (bag first, then camp storage).");
        }

        // ---------------------------------------------------------------- kin
        static Vector3 Ahead(float d)
        {
            Vector3 p = P.Position + P.Forward * d;
            p.y = Ground.Height(p.x, p.z);
            return p;
        }

        public static void SpawnWild(SpeciesDef sp, int level, bool alpha, bool shimmer)
        {
            Vector3 p = Ahead(7f);
            var d = CreatureFactory.RollWild(sp, level, WorldGen.Biome(p.x, p.z), alpha);
            if (shimmer && !d.shimmer) Shimmer.Make(d);
            var a = CreatureSpawner.I.SpawnActor(d, CreatureMode.Wild, p);
            if (a != null) a.Persistent = true;   // it stays until you deal with it
            Say($"Wild {(alpha ? "alpha " : "")}{(shimmer ? "shimmer " : "")}{sp.displayName} Lv {level} ahead of you.");
        }

        public static void AddKin(SpeciesDef sp, int level, bool shimmer)
        {
            var d = CreatureFactory.RollWild(sp, level, BiomeId.Meadow, false);
            if (shimmer && !d.shimmer) Shimmer.Make(d);
            G.OnKinCaptured(d);   // the same path a catch takes: roster, Dex, quests
            Say($"{sp.displayName} Lv {level} joined you.");
        }

        /// <summary>An egg in the Hatchery that is ready now (no Hatchery or egg limit needed): a mystery egg, or a shimmer one.</summary>
        public static void Egg(bool shimmer)
        {
            var e = KinGrowth.MysteryEgg();
            if (shimmer && !e.child.shimmer) Shimmer.Make(e.child);
            e.readyUnix = Expeditions.Now;
            e.parents = "dev mode";
            S.eggs.Add(e);
            Say($"An egg is ready: a {e.child.Species.displayName}{(shimmer ? " (shimmer)" : "")}. Hatch it at the Hatchery or Camp > Hatchery.");
        }

        public static void EggsReady() { foreach (var e in S.eggs) e.readyUnix = Expeditions.Now; Say($"{S.eggs.Count} egg{(S.eggs.Count == 1 ? " is" : "s are")} ready to hatch."); }

        public static void LevelCompanion(int levels)
        {
            var c = G.Party.Companion;
            if (c == null) { Say("No kin is out."); return; }
            for (int i = 0; i < levels && c.Data.level < 60; i++) G.GrantKinXp(c.Data, CreatureData.XpToNext(c.Data.level), true);
            Say($"{c.Data.DisplayName} is level {c.Data.level}.");
        }

        public static void MaxBond()
        {
            var c = G.Party.Companion;
            if (c == null) { Say("No kin is out."); return; }
            c.Data.bond = 99999f;
            Say($"{c.Data.DisplayName}: bond level {c.Data.BondLevel}.");
        }

        /// <summary>Every wild kin within reach: 0 = defeat it, otherwise set its health to that share.</summary>
        public static void NearbyWild(float hpFrac, StatusType status = StatusType.None)
        {
            int n = 0;
            var all = new List<CreatureActor>(CreatureRegistry.All);
            foreach (var a in all)
            {
                if (a == null || a.Mode != CreatureMode.Wild || !a.IsAlive || (a.Position - P.Position).sqrMagnitude > 25f * 25f) continue;
                n++;
                if (status != StatusType.None) a.ApplyStatus(status);
                else if (hpFrac <= 0f) a.ReceiveHit(new HitInfo { attacker = P, damage = 9999999f, element = Element.Normal, effectiveness = 1f });
                else a.DevSetHp(hpFrac);
            }
            Say(n == 0 ? "No wild kin within 25 m." : status != StatusType.None ? $"{status} on {n} wild kin." : hpFrac <= 0f ? $"Defeated {n} wild kin." : $"{n} wild kin set to {hpFrac * 100f:0}% health.");
        }

        // ---------------------------------------------------------------- camp
        static bool Camp() { if (B != null && B.HasBase) return true; Say("No camp yet: place a Hearthstone first."); return false; }

        public static void Power(bool full) { if (!Camp()) return; B.Charge = full ? B.ChargeCapacity : 0f; B.Cold = full ? B.ColdCapacity : 0f; Say(full ? "Charge and cold full." : "Charge and cold emptied."); }

        /// <summary>Every producer moves on one step (a hands-on step is done, a hands-off one finishes).</summary>
        public static void FinishSteps()
        {
            if (!Camp()) return;
            int n = 0;
            foreach (var b in B.Buildings)
            {
                if (!b.IsProducer) continue;
                int before = b.Stage; int buf = b.BufferCount;
                if (b.Waiting) b.TickStage(99999f); else b.AddWork(99999f);
                if (b.Stage != before || b.BufferCount != buf) n++;
            }
            Say($"{n} structures moved on a step.");
        }

        public static void FinishOrders()
        {
            if (!Camp()) return;
            int before = 0; foreach (var o in B.Orders) before += o.remaining;
            for (int i = 0; i < 400 && B.Orders.Count > 0; i++)
            {
                if (!ContentDB.Recipes.TryGetValue(B.Orders[0].recipeId, out var r)) { B.Orders.RemoveAt(0); continue; }
                int c = B.Orders.Count, rem = B.Orders[0].remaining;
                B.AddCraftWork(r.station, 99999f);
                if (B.Orders.Count == c && B.Orders[0].remaining == rem) break;   // storage full: stop rather than spin
            }
            int after = 0; foreach (var o in B.Orders) after += o.remaining;
            Say($"Crafted {before - after} queued items" + (after > 0 ? $" ({after} left: no room?)" : "."));
        }

        public static void UpgradeHearth() { if (!Camp()) return; B.DevUpgradeCore(); Say($"Hearthstone level {B.CoreLevel}."); }
        public static void Raid() { if (!Camp()) return; int n = B.StartRaid(P.Position); Say(n > 0 ? $"Raid: {n} kin on their way." : "No raid could start (you may be too far from camp, or the land has no hostile kin)."); }
        public static void SpoilDay() { if (!Camp()) return; int lost = B.DevSpoil(EnvironmentSystem.I != null ? EnvironmentSystem.I.DayLength : 1200f); Say($"A day's spoiling: {lost} pieces of food went off."); }

        public static void AllResearch()
        {
            foreach (var t in Tech.All) if (!S.techDone.Contains(t.id)) S.techDone.Add(t.id);
            S.researchId = null; S.researchProgress = 0f;
            Say("Every research project is done.");
        }

        public static void AllBlueprints()
        {
            foreach (var b in Chests.Blueprints) if (!S.blueprints.Contains(b.recipe)) S.blueprints.Add(b.recipe);
            Say("Every blueprint learned.");
        }

        public static void AllSigils()
        {
            int n = 0;
            foreach (var w in Wardens.All)
            {
                if (w.final || w.optional || Wardens.Beaten(w.id)) continue;
                S.wardenWins.Add(new WardenRecord { id = w.id, wins = 1, unix = 0 });
                n++;
            }
            Say($"{n} Sigils granted ({Wardens.Sigils}/{Wardens.SigilsNeeded}).");
        }

        // ---------------------------------------------------------------- world
        public static void SetTime(float t, string name) { if (EnvironmentSystem.I != null) EnvironmentSystem.I.timeOfDay = t; Say("It is " + name + "."); }
        public static void Weather(WeatherType w) { if (EnvironmentSystem.I != null) EnvironmentSystem.I.SetWeather(w, 600f); Say("Weather: " + w + " for ten minutes."); }
        public static void Event(WorldEventType t) { if (WorldEvents.I != null) WorldEvents.I.Begin(t, P); Say("World event: " + t + "."); }

        public static void Teleport(Vector3 to, string name)
        {
            if (Caves.InCave) { Say("Leave the cave first."); return; }
            to.y = Mathf.Max(WorldGen.Height(to.x, to.z), Tides.Surface(to.x, to.z)) + 0.2f;
            P.Teleport(to, 0f);
            G.CameraRig.Init(Camera.main, to, 0f);
            Say("Moved to " + name + ".");
        }

        public static void Chest(ChestTier tier)
        {
            if (Chests.I == null) return;
            Chests.I.DevSpawn(tier, Ahead(3f));
            Say(Chests.TierName[(int)tier] + " ahead of you.");
        }

        public static void AllWaystones()
        {
            if (Waystones.I == null) return;
            foreach (var s in Waystones.I.All)
            {
                if (!S.waystonesFound.Contains(s.id)) S.waystonesFound.Add(s.id);
                if (!S.waystonesActive.Contains(s.id)) S.waystonesActive.Add(s.id);
            }
            Say("Every waystone found and attuned: fast travel anywhere from the Map.");
        }

        public static void AllOff() { god = stamina = oneHit = sureCatch = fastWork = noSleep = freeAll = false; forceBlueprint = false; }
    }
}
