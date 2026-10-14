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
        /// <summary>Only in the way for things between these heights (house furniture: its own storey).</summary>
        public float yLo = float.NegativeInfinity, yHi = float.PositiveInfinity;
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
            for (int i = 0; i < obsBuf.Count; i++) { var o = obsBuf[i]; if (pos.y < o.yLo || pos.y > o.yHi) continue; hit |= Resolve(ref pos, o.pos, r + o.radius); }
            hit |= Housing.PushOut(ref pos, r);   // house walls are segments, not circles
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

        /// <summary>
        /// The first round obstacle (structure, landmark piece, tree, rock, furniture) a walker of radius r would bump into
        /// going straight from a to b. One it is already touching counts if it is in front; one its goal is right beside
        /// (it is walking up to it) doesn't count. Used by CreatureActor to steer round things.
        /// </summary>
        public static bool FirstBlocker(Vector3 a, Vector3 b, float r, Vector3 goal, out Vector3 center, out float radius)
        {
            center = default; radius = 0f;
            if (Caves.InZone(a.x, a.z)) return false;
            Vector3 mid = (a + b) * 0.5f;
            float half = new Vector2(b.x - a.x, b.z - a.z).magnitude * 0.5f, best = float.MaxValue;
            bool found = false;
            var cm = ChunkManager.I;
            if (cm != null)
            {
                nodeBuf.Clear();
                cm.Nodes.Query(mid, half + 3f, nodeBuf);
                for (int i = 0; i < nodeBuf.Count; i++)
                {
                    var n = nodeBuf[i];
                    if (n.alive && n.Radius > 0f) Consider(a, b, r, goal, n.pos, n.Radius, ref best, ref center, ref radius, ref found);
                }
            }
            obsBuf.Clear();
            Obstacles.Query(mid, half + 6f, obsBuf);
            for (int i = 0; i < obsBuf.Count; i++)
            {
                var o = obsBuf[i];
                if (a.y < o.yLo || a.y > o.yHi) continue;
                Consider(a, b, r, goal, o.pos, o.radius, ref best, ref center, ref radius, ref found);
            }
            return found;
        }

        /// <summary>Every round obstacle (living nodes, obstacles in the way at height y) within radius of center, as (x, radius, z) (KinNav stamps them).</summary>
        public static void CirclesIn(Vector3 center, float radius, float y, List<Vector3> into)
        {
            into.Clear();
            var cm = ChunkManager.I;
            if (cm != null)
            {
                nodeBuf.Clear();
                cm.Nodes.Query(center, radius + 3f, nodeBuf);
                foreach (var n in nodeBuf) if (n.alive && n.Radius > 0f) into.Add(new Vector3(n.pos.x, n.Radius, n.pos.z));
            }
            obsBuf.Clear();
            Obstacles.Query(center, radius + 6f, obsBuf);
            foreach (var o in obsBuf) if (y >= o.yLo && y <= o.yHi) into.Add(new Vector3(o.pos.x, o.radius, o.pos.z));
        }

        /// <summary>The first round obstacle a circle at p of radius r overlaps (at p's height), if any.</summary>
        public static bool Overlapping(Vector3 p, float r, out Vector3 center, out float radius)
        {
            center = default; radius = 0f;
            var cm = ChunkManager.I;
            if (cm != null)
            {
                nodeBuf.Clear();
                cm.Nodes.Query(p, r + 3f, nodeBuf);
                for (int i = 0; i < nodeBuf.Count; i++)
                {
                    var n = nodeBuf[i];
                    if (!n.alive || n.Radius <= 0f) continue;
                    float dx = p.x - n.pos.x, dz = p.z - n.pos.z, m = r + n.Radius;
                    if (dx * dx + dz * dz < m * m) { center = n.pos; radius = n.Radius; return true; }
                }
            }
            obsBuf.Clear();
            Obstacles.Query(p, r + 6f, obsBuf);
            for (int i = 0; i < obsBuf.Count; i++)
            {
                var o = obsBuf[i];
                if (p.y < o.yLo || p.y > o.yHi) continue;
                float dx = p.x - o.pos.x, dz = p.z - o.pos.z, m = r + o.radius;
                if (dx * dx + dz * dz < m * m) { center = o.pos; radius = o.radius; return true; }
            }
            return false;
        }

        static void Consider(Vector3 a, Vector3 b, float r, Vector3 goal, Vector3 c, float cr, ref float best, ref Vector3 center, ref float radius, ref bool found)
        {
            float rr = r + cr;
            float ax = a.x - c.x, az = a.z - c.z;
            float gx = goal.x - c.x, gz = goal.z - c.z, near = rr + 0.6f;
            if (gx * gx + gz * gz < near * near) return;                                 // walking up to it
            float dx = b.x - a.x, dz = b.z - a.z, len2 = dx * dx + dz * dz;
            if (len2 < 1e-4f) return;
            if (ax * ax + az * az < rr * rr)
            {
                // already touching it: in the way if it is in front
                float fwd = -(ax * dx + az * dz) / (Mathf.Sqrt(len2) * Mathf.Max(1e-4f, Mathf.Sqrt(ax * ax + az * az)));
                if (fwd > 0.3f && 0f < best) { best = 0f; center = c; radius = cr; found = true; }
                return;
            }
            float t = Mathf.Clamp01(-(ax * dx + az * dz) / len2);
            float px = ax + dx * t, pz = az + dz * t;
            if (px * px + pz * pz >= rr * rr || t >= best) return;
            best = t; center = c; radius = cr; found = true;
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
