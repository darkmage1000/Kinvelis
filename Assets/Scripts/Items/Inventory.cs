using System;
using System.Collections.Generic;

namespace Wildbond
{
    [Serializable]
    public class ItemStack
    {
        public string id = "";
        public int count;
        public bool Empty => string.IsNullOrEmpty(id) || count <= 0;
    }

    /// <summary>Slot-based stacking container used for the player bag and base storage.</summary>
    [Serializable]
    public class Inventory
    {
        public List<ItemStack> slots = new List<ItemStack>();

        public Inventory() { }
        public Inventory(int capacity) { Resize(capacity); }

        public int Capacity => slots.Count;

        public void Resize(int capacity)
        {
            while (slots.Count < capacity) slots.Add(new ItemStack());
            // shrinking only drops empty tail slots
            for (int i = slots.Count - 1; i >= capacity && i >= 0; i--)
                if (slots[i].Empty) slots.RemoveAt(i); else break;
        }

        public int Count(string id)
        {
            int n = 0;
            for (int i = 0; i < slots.Count; i++) if (slots[i].id == id) n += slots[i].count;
            return n;
        }

        /// <summary>How many of id could still be added.</summary>
        public int SpaceFor(string id)
        {
            var def = ContentDB.Item(id);
            int stack = def != null ? def.maxStack : 99;
            int n = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].Empty) n += stack;
                else if (slots[i].id == id) n += stack - slots[i].count;
            }
            return n;
        }

        /// <summary>Adds items, returns the amount that did not fit.</summary>
        public virtual int Add(string id, int count)
        {
            if (count <= 0 || string.IsNullOrEmpty(id)) return 0;
            var def = ContentDB.Item(id);
            int stack = def != null ? def.maxStack : 99;
            for (int i = 0; i < slots.Count && count > 0; i++)
            {
                var s = slots[i];
                if (s.id != id || s.count >= stack) continue;
                int add = Math.Min(stack - s.count, count);
                s.count += add;
                count -= add;
            }
            for (int i = 0; i < slots.Count && count > 0; i++)
            {
                var s = slots[i];
                if (!s.Empty) continue;
                int add = Math.Min(stack, count);
                s.id = id;
                s.count = add;
                count -= add;
            }
            return count;
        }

        /// <summary>
        /// Drag and drop between two slots: into an empty slot it moves, onto the same item it stacks (what does not fit
        /// stays behind), onto a different item the two swap. Returns false if nothing changed.
        /// </summary>
        public bool MoveSlot(int from, int to)
        {
            if (from == to || from < 0 || to < 0 || from >= slots.Count || to >= slots.Count) return false;
            var a = slots[from];
            var b = slots[to];
            if (a.Empty) return false;
            if (!b.Empty && b.id == a.id)
            {
                var def = ContentDB.Item(a.id);
                int stack = def != null ? def.maxStack : 99;
                int move = Math.Min(stack - b.count, a.count);
                if (move <= 0) { slots[from] = b; slots[to] = a; return true; }   // full stack: just swap places
                b.count += move;
                a.count -= move;
                if (a.count <= 0) { a.id = ""; a.count = 0; }
                return true;
            }
            slots[from] = b;
            slots[to] = a;
            return true;
        }

        /// <summary>Removes up to count, returns amount actually removed.</summary>
        public int Remove(string id, int count)
        {
            int removed = 0;
            for (int i = slots.Count - 1; i >= 0 && removed < count; i--)
            {
                var s = slots[i];
                if (s.id != id) continue;
                int take = Math.Min(s.count, count - removed);
                s.count -= take;
                removed += take;
                if (s.count <= 0) { s.id = ""; s.count = 0; }
            }
            return removed;
        }

        public bool IsEmpty
        {
            get
            {
                for (int i = 0; i < slots.Count; i++) if (!slots[i].Empty) return false;
                return true;
            }
        }

        public void Clear()
        {
            for (int i = 0; i < slots.Count; i++) { slots[i].id = ""; slots[i].count = 0; }
        }

        public void CopyFrom(Inventory other)
        {
            slots.Clear();
            foreach (var s in other.slots) slots.Add(new ItemStack { id = s.id ?? "", count = s.count });
        }
    }

    /// <summary>Crafting/building pulls from the bag first, then base storage when the player stands inside the base.</summary>
    public static class ItemSources
    {
        static Inventory Bag => GameManager.I.Player.Stats.Bag;
        static Inventory Storage => BaseManager.I != null && BaseManager.I.HasBase && BaseManager.I.IsInsideBase(GameManager.I.Player.Position, 0f) ? BaseManager.I.Storage : null;

        public static bool UsingStorage => Storage != null;

        public static int Count(string id) => Bag.Count(id) + (Storage?.Count(id) ?? 0);

        public static bool Has(Ingredient[] list, int times = 1)
        {
            if (Dev.FreeAll) return true;
            foreach (var ing in list) if (Count(ing.itemId) < ing.count * times) return false;
            return true;
        }

        public static void Consume(Ingredient[] list, int times = 1)
        {
            if (Dev.FreeAll) return;
            foreach (var ing in list)
            {
                int need = ing.count * times;
                need -= Bag.Remove(ing.itemId, need);
                if (need > 0 && Storage != null) Storage.Remove(ing.itemId, need);
            }
            GameEvents.RaiseInventoryChanged();
        }
    }
}
