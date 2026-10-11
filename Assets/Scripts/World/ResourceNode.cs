using System.Collections.Generic;
using UnityEngine;

namespace Wildbond
{
    /// <summary>A gatherable prop instance. Rendered via instancing, never its own GameObject.</summary>
    public sealed class ResourceNode : ISpatialItem
    {
        public long key;
        public PropType type;
        public Vector3 pos;
        public float rotY;
        public float scale;
        public float hp;
        public bool alive;
        public float wobble;        // >0 while shaking after a hit
        public object chunk;        // owning ChunkManager.Chunk

        public Vector3 SpatialPosition => pos;
        public PropInfo Info => PropCatalog.Info[(int)type];
        public float Radius => Info.radius * scale;

        public static long MakeKey(int cx, int cz, int index) =>
            ((long)(cx + 4096) << 40) | ((long)(cz + 4096) << 20) | (long)(index & 0xFFFFF);
    }

    /// <summary>Static circular obstacle (building, landmark piece).</summary>
    public sealed class Obstacle : ISpatialItem
    {
        public Vector3 pos;
        public float radius;
        public object owner;
        public Vector3 SpatialPosition => pos;
    }

    /// <summary>Cheap physics-free collision: circles on the XZ plane against nodes + obstacles.</summary>
    public static class WorldCollision
    {
        public static readonly SpatialHash<Obstacle> Obstacles = new SpatialHash<Obstacle>(16f);
        static readonly List<ResourceNode> nodeBuf = new List<ResourceNode>(32);
        static readonly List<Obstacle> obsBuf = new List<Obstacle>(16);

        public static Obstacle AddObstacle(Vector3 pos, float radius, object owner)
        {
            var o = new Obstacle { pos = pos, radius = radius, owner = owner };
            Obstacles.Insert(o);
            return o;
        }

        public static void RemoveObstacle(Obstacle o) { if (o != null) Obstacles.Remove(o); }

        /// <summary>Pushes a circle of radius r out of every overlapping obstacle. Returns true if anything was hit.</summary>
        public static bool PushOut(ref Vector3 pos, float r)
        {
            if (Caves.InZone(pos.x, pos.z)) return Caves.PushOut(ref pos, r);   // cave walls instead of overworld props
            bool hit = false;
            var cm = ChunkManager.I;
            if (cm != null)
            {
                nodeBuf.Clear();
                cm.Nodes.Query(pos, r + 3f, nodeBuf);
                for (int i = 0; i < nodeBuf.Count; i++)
                {
                    var n = nodeBuf[i];
                    if (!n.alive) continue;
                    float nr = n.Radius;
                    if (nr <= 0f) continue;
                    hit |= Resolve(ref pos, n.pos, r + nr);
                }
            }
            obsBuf.Clear();
            Obstacles.Query(pos, r + 6f, obsBuf);
            for (int i = 0; i < obsBuf.Count; i++) hit |= Resolve(ref pos, obsBuf[i].pos, r + obsBuf[i].radius);
            return hit;
        }

        static bool Resolve(ref Vector3 pos, Vector3 c, float minDist)
        {
            float dx = pos.x - c.x, dz = pos.z - c.z;
            float d2 = dx * dx + dz * dz;
            if (d2 >= minDist * minDist) return false;
            float d = Mathf.Sqrt(d2);
            if (d < 0.0001f) { dx = 1f; dz = 0f; d = 1f; }
            float push = minDist - d;
            pos.x += dx / d * push;
            pos.z += dz / d * push;
            return true;
        }

        /// <summary>True if a circle overlaps any obstacle (used by build placement).</summary>
        public static bool Overlaps(Vector3 pos, float r, bool includeNodes)
        {
            obsBuf.Clear();
            Obstacles.Query(pos, r + 6f, obsBuf);
            for (int i = 0; i < obsBuf.Count; i++)
            {
                float dx = pos.x - obsBuf[i].pos.x, dz = pos.z - obsBuf[i].pos.z;
                float m = r + obsBuf[i].radius;
                if (dx * dx + dz * dz < m * m) return true;
            }
            if (includeNodes && ChunkManager.I != null)
            {
                nodeBuf.Clear();
                ChunkManager.I.Nodes.Query(pos, r + 3f, nodeBuf);
                for (int i = 0; i < nodeBuf.Count; i++)
                {
                    var n = nodeBuf[i];
                    if (!n.alive || n.Radius <= 0f) continue;
                    float dx = pos.x - n.pos.x, dz = pos.z - n.pos.z;
                    float m = r + n.Radius;
                    if (dx * dx + dz * dz < m * m) return true;
                }
            }
            return false;
        }
    }
}
