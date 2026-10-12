using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Storage boxes, feed boxes and the night.
    ///
    /// **Boxes.** Camp storage is still one pool (`Storage`) as far as crafting, building and workers are concerned,
    /// but its slots now belong to particular structures: the list is laid out structure by structure in the order
    /// they were built (`Building.BoxStart / BoxLen`), so each Storage Chest, vault and the Hearthstone has contents
    /// of its own that the player can open, fill and empty (MenuUI.Boxes). Anything added to the pool lands in the
    /// first box with room. Slots past the last box are overflow (kept so nothing is ever lost) and show up in the
    /// Hearthstone's box.
    ///
    /// **Feed boxes.** A Feed Box has its own small inventory (`Building.Feed`) and camp kin eat only from feed boxes.
    /// By default a box keeps itself topped up from camp storage (`Building.FeedAuto`, about `FeedKeep` pieces), so a
    /// camp that worked before still does; switch that off to choose exactly what your kin are fed.
    ///
    /// **Night.** Camp kin sleep from dusk to dawn unless they are nocturnal (`CreatureData.Nocturnal`: a night
    /// species, or the Night Owl passive) - see CreatureActor.Worker's TickNight for the bodies, and here for beds
    /// and for the far / offline simulation.
    /// </summary>
    public partial class BaseManager
    {
        // ============================================================ BOXES
        int SlotsOf(Building b) => b.Def.storageSlots + (b.Def.isCore ? (CoreLevel - 1) * StoragePerCoreLevel : 0);

        /// <summary>A storage structure was added or grew: give it its slots without moving anyone else's contents.</summary>
        void RecalcStorage()
        {
            int idx = 0;
            foreach (var b in Buildings)
            {
                int want = SlotsOf(b), had = b.BoxLen;
                if (want <= 0 && had <= 0) continue;
                while (Storage.slots.Count < idx + had) Storage.slots.Add(new ItemStack());
                for (int i = had; i < want; i++) Storage.slots.Insert(idx + i, new ItemStack());
                b.BoxStart = idx;
                b.BoxLen = Mathf.Max(want, had);
                idx += b.BoxLen;
            }
        }

        /// <summary>After a save's slots are loaded: the boxes are wherever the save left them (same structures, same order).</summary>
        public void LayoutStorage()
        {
            int idx = 0;
            foreach (var b in Buildings)
            {
                int want = SlotsOf(b);
                b.BoxStart = idx;
                b.BoxLen = want;
                idx += want;
            }
            while (Storage.slots.Count < idx) Storage.slots.Add(new ItemStack());
        }

        int BoxTotal { get { int n = 0; foreach (var b in Buildings) n += b.BoxLen; return n; } }

        /// <summary>Every structure that holds storage, in build order.</summary>
        public List<Building> Boxes()
        {
            var l = new List<Building>();
            foreach (var b in Buildings) if (b != null && b.BoxLen > 0) l.Add(b);
            return l;
        }

        /// <summary>"Storage Chest 2": the name with a number when there are several of the kind.</summary>
        public string BoxName(Building b)
        {
            int n = 0, mine = 0;
            foreach (var o in Buildings)
            {
                if (o.Def != b.Def) continue;
                n++;
                if (o == b) mine = n;
            }
            return n > 1 ? $"{b.Def.displayName} {mine}" : b.Def.displayName;
        }

        /// <summary>This box's slots (the Hearthstone also shows any overflow).</summary>
        public List<ItemStack> BoxSlots(Building b)
        {
            var l = new List<ItemStack>();
            if (b == null) return l;
            if (b.Def.feeds) { l.AddRange(b.Feed.slots); return l; }
            for (int i = 0; i < b.BoxLen && b.BoxStart + i < Storage.slots.Count; i++) l.Add(Storage.slots[b.BoxStart + i]);
            if (b.Def.isCore) for (int i = BoxTotal; i < Storage.slots.Count; i++) l.Add(Storage.slots[i]);
            return l;
        }

        public int BoxUsed(Building b) { int n = 0; foreach (var s in BoxSlots(b)) if (!s.Empty) n++; return n; }
        public int BoxCapacity(Building b) => b.Def.feeds ? b.Feed.Capacity : b.BoxLen;
        public int BoxCount(Building b, string id) { int n = 0; foreach (var s in BoxSlots(b)) if (s.id == id) n += s.count; return n; }

        /// <summary>Puts items into one particular box. Returns what did not fit.</summary>
        public int BoxAdd(Building b, string id, int count)
        {
            if (count <= 0 || string.IsNullOrEmpty(id) || b == null) return count;
            if (b.Def.feeds) return IsKinFood(id) ? b.Feed.Add(id, count) : count;
            var def = ContentDB.Item(id);
            int stack = def != null ? def.maxStack : 99;
            var slots = BoxSlots(b);
            foreach (var s in slots)
            {
                if (count <= 0) break;
                if (s.id != id || s.count >= stack) continue;
                int add = Mathf.Min(stack - s.count, count);
                s.count += add; count -= add;
            }
            foreach (var s in slots)
            {
                if (count <= 0) break;
                if (!s.Empty) continue;
                int add = Mathf.Min(stack, count);
                s.id = id; s.count = add; count -= add;
            }
            return count;
        }

        /// <summary>Takes items out of one particular box. Returns how many came out.</summary>
        public int BoxRemove(Building b, string id, int count)
        {
            if (b == null) return 0;
            if (b.Def.feeds) return b.Feed.Remove(id, count);
            int removed = 0;
            var slots = BoxSlots(b);
            for (int i = slots.Count - 1; i >= 0 && removed < count; i--)
            {
                var s = slots[i];
                if (s.id != id) continue;
                int take = Mathf.Min(s.count, count - removed);
                s.count -= take; removed += take;
                if (s.count <= 0) { s.id = ""; s.count = 0; }
            }
            return removed;
        }

        /// <summary>A hauler's load: into the box it walked to if there is room, the rest wherever the camp has space. Returns what did not fit anywhere.</summary>
        public int Deposit(Building box, string id, int count)
        {
            int left = box != null && box.BoxLen > 0 ? BoxAdd(box, id, count) : count;
            return left > 0 ? Storage.Add(id, left) : 0;
        }

        /// <summary>A storage structure is being pulled down: its slots leave the pool and what was in them is put somewhere else.</summary>
        void RemoveBox(Building b, List<ItemStack> spill)
        {
            if (b.Def.feeds) foreach (var s in b.Feed.slots) if (!s.Empty) spill.Add(new ItemStack { id = s.id, count = s.count });
            if (b.BoxLen <= 0) return;
            int n = Mathf.Min(b.BoxLen, Mathf.Max(0, Storage.slots.Count - b.BoxStart));
            for (int i = 0; i < n; i++) { var s = Storage.slots[b.BoxStart + i]; if (!s.Empty) spill.Add(s); }
            Storage.slots.RemoveRange(b.BoxStart, n);
            b.BoxLen = 0;
        }

        void Respill(List<ItemStack> spill)
        {
            foreach (var s in spill)
            {
                int left = Storage.Add(s.id, s.count);
                if (left > 0) left = GameManager.I.Player.Stats.Bag.Add(s.id, left);
                if (left > 0) Storage.slots.Add(new ItemStack { id = s.id, count = left });   // overflow: nothing is thrown away
            }
        }

        // ============================================================ FEED BOXES
        public const int FeedSlots = 6;
        /// <summary>How many pieces of food a self-stocking Feed Box keeps in it.</summary>
        public const int FeedKeep = 20;
        float feedTimer;

        public static bool IsKinFood(string id) => System.Array.IndexOf(Crops.KinFoods, id) >= 0;

        public IEnumerable<Building> FeedBoxes() { foreach (var b in Buildings) if (b != null && b.Def.feeds) yield return b; }

        public int FeedCount(Building fb) { int n = 0; foreach (var s in fb.Feed.slots) if (!s.Empty) n += s.count; return n; }

        bool StorageHasFood { get { foreach (var id in Crops.KinFoods) if (Storage.Count(id) > 0) return true; return false; } }

        /// <summary>The feed box a hungry kin should walk to: the nearest with something in it (or that can stock itself).</summary>
        public Building FeedBoxFor(Vector3 p)
        {
            Building best = null; float bd = float.MaxValue; bool storage = StorageHasFood;
            foreach (var fb in FeedBoxes())
            {
                if (fb.Feed.IsEmpty && !(fb.FeedAuto && storage)) continue;
                float d = (fb.transform.position - p).sqrMagnitude;
                if (d < bd) { bd = d; best = fb; }
            }
            return best ?? FeedBox;
        }

        /// <summary>Tops a self-stocking box up from camp storage, cheapest food first.</summary>
        void Restock(Building fb)
        {
            if (!fb.FeedAuto) return;
            int have = FeedCount(fb);
            foreach (var id in Crops.KinFoods)
            {
                if (have >= FeedKeep) break;
                int n = Mathf.Min(FeedKeep - have, Storage.Count(id));
                if (n <= 0) continue;
                int moved = n - fb.Feed.Add(id, n);
                if (moved <= 0) break;
                Storage.Remove(id, moved);
                have += moved;
            }
        }

        void TickFeed(float dt)
        {
            feedTimer -= dt;
            if (feedTimer > 0f) return;
            feedTimer = 3f;
            foreach (var fb in FeedBoxes()) Restock(fb);
        }

        // ============================================================ NIGHT
        /// <summary>Set by the offline catch-up, which keeps a clock of its own; otherwise the world's.</summary>
        bool? nightOverride;
        public bool NightNow => nightOverride ?? (EnvironmentSystem.I != null && EnvironmentSystem.I.IsNight);
        /// <summary>Is this camp kin in bed (so not working, and less hungry)?</summary>
        public bool Asleep(CreatureData d) => NightNow && !d.Nocturnal;

        public int SleeperCount
        {
            get { int n = 0; foreach (var d in GameManager.I.Party.In(KinLocation.Base)) if (!d.Fainted && Asleep(d)) n++; return n; }
        }

        /// <summary>Where a camp kin beds down: sleepers take the Kin Nests two at a time in roster order, the rest curl up round the hearth.</summary>
        public Vector3 BedSpot(CreatureActor a)
        {
            int mine = 0, i = 0;
            foreach (var d in GameManager.I.Party.In(KinLocation.Base))
            {
                if (d.Nocturnal || d.Fainted) continue;
                if (d == a.Data) mine = i;
                i++;
            }
            int bed = 0;
            Vector3 p;
            foreach (var b in Buildings)
            {
                if (b.Def.beds <= 0) continue;
                if (mine < bed + b.Def.beds)
                {
                    float ang = (b.RotY + 90f + (mine - bed) * (360f / Mathf.Max(1, b.Def.beds))) * Mathf.Deg2Rad;
                    p = b.transform.position + new Vector3(Mathf.Sin(ang), 0, Mathf.Cos(ang)) * (b.Def.footprint + 0.55f);
                    p.y = WorldGen.Height(p.x, p.z);
                    return p;
                }
                bed += b.Def.beds;
            }
            float t = (mine - bed) * 2.4f;
            p = CorePosition + new Vector3(Mathf.Cos(t), 0, Mathf.Sin(t)) * (3.2f + ((mine - bed) % 3) * 0.9f);
            p.y = WorldGen.Height(p.x, p.z);
            return p;
        }
    }
}
