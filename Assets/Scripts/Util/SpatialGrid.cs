using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    public interface ISpatialItem
    {
        Vector3 SpatialPosition { get; }
    }

    /// <summary>Uniform XZ grid for fast radius queries (resource nodes, obstacles). Main-thread only.</summary>
    public sealed class SpatialHash<T> where T : class, ISpatialItem
    {
        readonly float cellSize;
        readonly Dictionary<long, List<T>> cells = new Dictionary<long, List<T>>();
        readonly Stack<List<T>> listPool = new Stack<List<T>>();

        public SpatialHash(float cellSize) { this.cellSize = cellSize; }

        long Key(int cx, int cz) => ((long)cx << 32) ^ (uint)cz;
        int Cell(float v) => Mathf.FloorToInt(v / cellSize);

        public void Insert(T item)
        {
            Vector3 p = item.SpatialPosition;
            long k = Key(Cell(p.x), Cell(p.z));
            if (!cells.TryGetValue(k, out var list))
            {
                list = listPool.Count > 0 ? listPool.Pop() : new List<T>(8);
                cells[k] = list;
            }
            list.Add(item);
        }

        public void Remove(T item)
        {
            Vector3 p = item.SpatialPosition;
            long k = Key(Cell(p.x), Cell(p.z));
            if (cells.TryGetValue(k, out var list))
            {
                list.Remove(item);
                if (list.Count == 0)
                {
                    cells.Remove(k);
                    listPool.Push(list);
                }
            }
        }

        /// <summary>Appends all items whose XZ distance to center is &lt;= radius (+ optional per-item pad via filter).</summary>
        public void Query(Vector3 center, float radius, List<T> results)
        {
            int x0 = Cell(center.x - radius), x1 = Cell(center.x + radius);
            int z0 = Cell(center.z - radius), z1 = Cell(center.z + radius);
            float r2 = radius * radius;
            for (int cx = x0; cx <= x1; cx++)
            for (int cz = z0; cz <= z1; cz++)
            {
                if (!cells.TryGetValue(Key(cx, cz), out var list)) continue;
                for (int i = 0; i < list.Count; i++)
                {
                    Vector3 p = list[i].SpatialPosition;
                    float dx = p.x - center.x, dz = p.z - center.z;
                    if (dx * dx + dz * dz <= r2) results.Add(list[i]);
                }
            }
        }

        public void Clear()
        {
            foreach (var kv in cells) { kv.Value.Clear(); listPool.Push(kv.Value); }
            cells.Clear();
        }
    }
}
