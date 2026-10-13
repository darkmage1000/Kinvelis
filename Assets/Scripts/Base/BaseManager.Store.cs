using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>What a storage box will take. Append only: saves store the number.</summary>
    public enum BoxFilter { Any, Food, Basics, Metals, Finds, Equipment }

    /// <summary>
    /// Camp storage that knows where things belong: adding to it goes through `BaseManager.StoreAdd`, so anything
    /// that enters the pool (crafting, hauling, the conveyor, outposts, refunds) is put in the right box.
    /// </summary>
    public class CampStore : Inventory
    {
        public override int Add(string id, int count)
        {
            var bm = BaseManager.I;
            return bm != null && bm.Buildings.Count > 0 ? bm.StoreAdd(id, count) : base.Add(id, count);
        }

        /// <summary>The plain slot-by-slot add (used before the boxes are laid out).</summary>
        public int AddPlain(string id, int count) => base.Add(id, count);
    }

    /// <summary>
    /// Chest filters and the Frost Larder as cold food storage.
    ///
    /// **Filters.** A storage box can be told what it holds (`Building.Filter`): anything, food, wood / stone /
    /// fiber, ore and metal, kin drops and finds, or tools / seeds / gear. `StoreAdd` puts an item in a box
    /// that asks for it first (topping up stacks, then empty slots), then in a box that takes anything; a box
    /// that asks for something else is never used. The Hearthstone always takes anything, so nothing is ever
    /// refused for want of a filter. Haulers walk to a box that takes what they carry (`StorageFor`).
    /// `SortStorage` empties the whole pool and deals it back out by the same rule.
    ///
    /// **The larder.** A Frost Larder is a storage box that takes food only, and food in it keeps `ColdKeeps`
    /// times as long while the larder holds cold (spoiling is worked out box by box, `SpoilStore`). Food goes
    /// there before anywhere else; kin food taken for a Feed Box comes out of the warm boxes first.
    /// </summary>
    public partial class BaseManager
    {
        public const int SaveLayout = 2;   // 2: larders hold storage slots

        public static readonly string[] FilterNames = { "Anything", "Food", "Wood, stone, fiber", "Ore and metal", "Kin drops and finds", "Tools, seeds, gear" };
        public static readonly string[] FilterShort = { "ANY", "FOOD", "BASICS", "METAL", "FINDS", "GEAR" };

        static bool IsMetal(string id) =>
            id == "ore" || id == "ingot" || id == "crystal" || id == "cinder" || id == Shimmer.ShardItem || id == "saltglass" || id == "bog_iron" || id == "starcrystal"
            || id.EndsWith("_ore") || id.EndsWith("_bar");

        public static bool IsFoodItem(ItemDef d) => d != null && (d.foodValue > 0f || d.category == ItemCategory.Food);

        public static BoxFilter KindOf(ItemDef d)
        {
            if (d == null) return BoxFilter.Finds;
            if (IsFoodItem(d)) return BoxFilter.Food;
            if (d.category != ItemCategory.Resource) return BoxFilter.Equipment;
            if (d.id == "wood" || d.id == "stone" || d.id == "fiber") return BoxFilter.Basics;
            return IsMetal(d.id) ? BoxFilter.Metals : BoxFilter.Finds;
        }

        /// <summary>What this box takes: a larder is food only, the Hearthstone anything, a chest whatever it was told.</summary>
        public static BoxFilter FilterOf(Building b) => b == null || b.Def.isCore ? BoxFilter.Any : IsLarder(b) ? BoxFilter.Food : b.Filter;

        public static bool BoxTakes(Building b, string id)
        {
            var f = FilterOf(b);
            return f == BoxFilter.Any || KindOf(ContentDB.Item(id)) == f;
        }

        /// <summary>Is food in this box being kept cold right now?</summary>
        public bool IsCold(Building b) => IsLarder(b) && LarderActive;

        static int AddTo(List<ItemStack> slots, string id, int count, int stack, bool topUp)
        {
            for (int i = 0; i < slots.Count && count > 0; i++)
            {
                var s = slots[i];
                if (topUp ? (s.id != id || s.count >= stack) : !s.Empty) continue;
                int add = Mathf.Min(topUp ? stack - s.count : stack, count);
                if (!topUp) s.id = id;
                s.count += add;
                count -= add;
            }
            return count;
        }

        readonly List<Building> routeBoxes = new List<Building>();

        /// <summary>Adds to camp storage, each piece into the box it belongs in. Returns what did not fit.</summary>
        public int StoreAdd(string id, int count)
        {
            if (count <= 0 || string.IsNullOrEmpty(id)) return 0;
            var def = ContentDB.Item(id);
            int stack = def != null ? def.maxStack : 99;
            var kind = KindOf(def);
            // boxes that ask for this kind first (a cold larder before a plain food chest), then those that take anything
            routeBoxes.Clear();
            foreach (var b in Buildings) if (b.BoxLen > 0 && IsLarder(b) && kind == BoxFilter.Food) routeBoxes.Add(b);
            foreach (var b in Buildings) if (b.BoxLen > 0 && !IsLarder(b) && FilterOf(b) != BoxFilter.Any && FilterOf(b) == kind) routeBoxes.Add(b);
            int asked = routeBoxes.Count;
            foreach (var b in Buildings) if (b.BoxLen > 0 && FilterOf(b) == BoxFilter.Any) routeBoxes.Add(b);
            for (int pass = 0; pass < 2 && count > 0; pass++)
            {
                int from = pass == 0 ? 0 : asked, to = pass == 0 ? asked : routeBoxes.Count;
                for (int i = from; i < to && count > 0; i++) count = AddTo(BoxSlots(routeBoxes[i]), id, count, stack, true);
                for (int i = from; i < to && count > 0; i++) count = AddTo(BoxSlots(routeBoxes[i]), id, count, stack, false);
            }
            return count;
        }

        /// <summary>How many more of this item a box could take.</summary>
        public int BoxSpace(Building b, string id)
        {
            if (b == null || !BoxTakes(b, id)) return 0;
            var def = ContentDB.Item(id);
            int stack = def != null ? def.maxStack : 99, n = 0;
            foreach (var s in BoxSlots(b)) n += s.Empty ? stack : s.id == id ? stack - s.count : 0;
            return n;
        }

        /// <summary>Where a hauler should carry this: the nearest box that asks for it and has room, else the nearest that takes anything.</summary>
        public Building StorageFor(string id, Vector3 p)
        {
            Building best = null; float bd = float.MaxValue; int bestRank = -1;
            foreach (var b in Buildings)
            {
                if (b.BoxLen <= 0 || BoxSpace(b, id) <= 0) continue;
                int rank = IsLarder(b) ? 2 : FilterOf(b) != BoxFilter.Any ? 1 : 0;   // a larder, then a chest that asks for it, then any chest
                float d = (b.transform.position - p).sqrMagnitude;
                if (rank > bestRank || (rank == bestRank && d < bd)) { best = b; bd = d; bestRank = rank; }
            }
            return best ?? NearestStorage(p);
        }

        /// <summary>How many more of this item the whole camp could store, filters respected.</summary>
        public int StoreSpace(string id)
        {
            int n = 0;
            foreach (var b in Buildings) if (b.BoxLen > 0) n += BoxSpace(b, id);
            return n;
        }

        /// <summary>
        /// The few camp problems worth a line on the HUD wherever you are: nowhere to put what is being made, nothing
        /// for the kin to eat, kin starving, machines without power. (The Camp menu's alerts list everything else:
        /// a structure nobody can work is a standing fact, not news.)
        /// </summary>
        public List<string> UrgentAlerts()
        {
            var l = new List<string>();
            var party = GameManager.I.Party;
            int workers = party.CountIn(KinLocation.Base);
            string stuck = null;
            foreach (var b in Buildings)
                if (b.IsProducer && b.BufferCount > 0 && !string.IsNullOrEmpty(b.BufferItem) && StoreSpace(b.BufferItem) <= 0) { stuck = b.BufferItem; break; }
            if (stuck != null) l.Add($"no room in storage for {ContentDB.ItemName(stuck)}");
            else if (StorageFull) l.Add("storage is full");
            if (workers > 0 && !HasFeedBox) l.Add("no Feed Box - your kin can't eat");
            else if (workers > 0 && !HasFood) l.Add("the Feed Box is empty");
            int starving = 0;
            foreach (var d in party.In(KinLocation.Base)) if (!d.Fainted && d.hunger <= 0f) starving++;
            if (starving > 0) l.Add($"{starving} kin {(starving == 1 ? "is" : "are")} starving");
            if (HasBuilding("volt_pylon") && DrawsPower && !CampPowered && !PowerAsleep) l.Add("no power - the Volt Pylon needs a Charging kin");
            return l;
        }

        /// <summary>Kin food for a Feed Box: out of the warm boxes first (it goes off soonest there), the larder last.</summary>
        int TakeFood(string id, int count)
        {
            int got = 0;
            for (int pass = 0; pass < 2 && got < count; pass++)
                foreach (var b in Buildings)
                {
                    if (got >= count) break;
                    if (b.BoxLen <= 0 || IsLarder(b) != (pass == 1)) continue;
                    got += BoxRemove(b, id, count - got);
                }
            if (got < count) got += Storage.Remove(id, count - got);   // anything past the boxes
            return got;
        }

        /// <summary>Tells a chest what to hold, then deals the whole pool back out so it is true at once.</summary>
        public void SetFilter(Building b, BoxFilter f)
        {
            if (b == null || b.Def.isCore || IsLarder(b) || b.BoxLen <= 0) return;
            b.Filter = f;
            SortStorage();
        }

        /// <summary>Takes everything out of camp storage and puts it back where it belongs. Nothing is lost: what no box asks for or has room for stays in any free slot.</summary>
        public void SortStorage()
        {
            var totals = new Dictionary<string, int>();
            var order = new List<string>();
            foreach (var s in Storage.slots)
            {
                if (s.Empty) continue;
                if (!totals.ContainsKey(s.id)) { totals[s.id] = 0; order.Add(s.id); }
                totals[s.id] += s.count;
                s.id = ""; s.count = 0;
            }
            order.Sort((x, y) =>
            {
                int kx = (int)KindOf(ContentDB.Item(x)), ky = (int)KindOf(ContentDB.Item(y));
                return kx != ky ? kx.CompareTo(ky) : string.CompareOrdinal(x, y);
            });
            var left = new List<ItemStack>();
            foreach (var id in order)
            {
                int rest = StoreAdd(id, totals[id]);
                if (rest > 0) left.Add(new ItemStack { id = id, count = rest });
            }
            foreach (var s in left)
            {
                int rest = ((CampStore)Storage).AddPlain(s.id, s.count);   // filters full: any free slot will do
                if (rest > 0) Storage.slots.Add(new ItemStack { id = s.id, count = rest });
            }
            GameEvents.RaiseInventoryChanged();
        }

        // ============================================================ SPOILING, BOX BY BOX
        int SpoilStore(float step)
        {
            int lost = 0;
            foreach (var b in Buildings)
            {
                if (b.BoxLen <= 0) continue;
                lost += SpoilSlots(BoxSlots(b), b.Uid, step, IsCold(b));
            }
            return lost;
        }

        int SpoilSlots(List<ItemStack> slots, string key, float step, bool cold)
        {
            spoilCounts.Clear();
            foreach (var s in slots)
            {
                if (s.Empty) continue;
                var def = ContentDB.Item(s.id);
                if (def == null || def.foodValue <= 0f) continue;
                spoilCounts.TryGetValue(s.id, out int n);
                spoilCounts[s.id] = n + s.count;
            }
            if (spoilCounts.Count == 0) return 0;
            spoilIds.Clear(); spoilIds.AddRange(spoilCounts.Keys);
            int lost = 0;
            foreach (var id in spoilIds)
            {
                float life = KeepSeconds(ContentDB.Item(id), cold);
                if (life <= 0f) continue;
                string k = key + ":" + id;
                spoilCarry.TryGetValue(k, out float carry);
                carry += spoilCounts[id] * (1f - Mathf.Exp(-step / life));
                int gone = Mathf.FloorToInt(carry);
                if (gone > 0)
                {
                    int took = 0;
                    for (int i = slots.Count - 1; i >= 0 && took < gone; i--)
                    {
                        var s = slots[i];
                        if (s.id != id) continue;
                        int t = Mathf.Min(s.count, gone - took);
                        s.count -= t; took += t;
                        if (s.count <= 0) { s.id = ""; s.count = 0; }
                    }
                    carry -= took; lost += took;
                }
                spoilCarry[k] = carry;
            }
            return lost;
        }

        /// <summary>Pieces of food kept cold in larders right now.</summary>
        public int ColdFood
        {
            get
            {
                int n = 0;
                foreach (var b in Buildings) if (IsLarder(b)) foreach (var s in BoxSlots(b)) if (!s.Empty) n += s.count;
                return n;
            }
        }
    }
}
