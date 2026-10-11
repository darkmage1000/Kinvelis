using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Wildbond
{
    /// <summary>
    /// Accumulates vertex-colored low-poly primitives into one mesh.
    /// Every creature, prop, building and landmark in the game is built from these so the whole
    /// world renders with one shared vertex-color material (SRP-batcher / instancing friendly).
    /// </summary>
    public sealed class MeshBuilder
    {
        public readonly List<Vector3> verts = new List<Vector3>(512);
        public readonly List<Vector3> norms = new List<Vector3>(512);
        public readonly List<Color32> cols = new List<Color32>(512);
        public readonly List<int> tris = new List<int>(1024);

        public int VertexCount => verts.Count;
        public void Clear() { verts.Clear(); norms.Clear(); cols.Clear(); tris.Clear(); }

        static Matrix4x4 NormalMatrix(Matrix4x4 m) => m.inverse.transpose;

        void AddVert(Matrix4x4 m, Matrix4x4 nm, Vector3 p, Vector3 n, Color32 c)
        {
            verts.Add(m.MultiplyPoint3x4(p));
            norms.Add(nm.MultiplyVector(n).normalized);
            cols.Add(c);
        }

        public static Matrix4x4 TRS(Vector3 pos, Vector3 eulerDeg, Vector3 scale) =>
            Matrix4x4.TRS(pos, Quaternion.Euler(eulerDeg), scale);

        // ---------------------------------------------------------------- BOX
        static readonly Vector3[] BoxN = { Vector3.up, Vector3.down, Vector3.forward, Vector3.back, Vector3.right, Vector3.left };

        public void Box(Matrix4x4 m, Color32 c) => Box(m, c, c);

        /// <summary>Unit cube centered on origin (size 1). Top face can use a different color.</summary>
        public void Box(Matrix4x4 m, Color32 c, Color32 top)
        {
            var nm = NormalMatrix(m);
            for (int f = 0; f < 6; f++)
            {
                Vector3 n = BoxN[f];
                Vector3 u = Mathf.Abs(n.y) > 0.5f ? Vector3.right : Vector3.up;
                Vector3 v = Vector3.Cross(n, u);
                int b = verts.Count;
                Color32 fc = f == 0 ? top : c;
                AddVert(m, nm, (n - u - v) * 0.5f, n, fc);
                AddVert(m, nm, (n + u - v) * 0.5f, n, fc);
                AddVert(m, nm, (n + u + v) * 0.5f, n, fc);
                AddVert(m, nm, (n - u + v) * 0.5f, n, fc);
                tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
                tris.Add(b); tris.Add(b + 2); tris.Add(b + 3);
            }
        }

        // ---------------------------------------------------------------- SPHERE
        /// <summary>Unit-diameter UV sphere. Smooth normals.</summary>
        public void Sphere(Matrix4x4 m, Color32 c, int lon = 10, int lat = 7) => Sphere(m, c, c, lon, lat);

        public void Sphere(Matrix4x4 m, Color32 c, Color32 bottom, int lon, int lat)
        {
            var nm = NormalMatrix(m);
            int b = verts.Count;
            for (int y = 0; y <= lat; y++)
            {
                float v = (float)y / lat;
                float phi = v * Mathf.PI;
                float sy = Mathf.Cos(phi), r = Mathf.Sin(phi);
                Color32 rc = Color32.Lerp(c, bottom, v);
                for (int x = 0; x <= lon; x++)
                {
                    float th = (float)x / lon * Mathf.PI * 2f;
                    var n = new Vector3(Mathf.Cos(th) * r, sy, Mathf.Sin(th) * r);
                    AddVert(m, nm, n * 0.5f, n, rc);
                }
            }
            int row = lon + 1;
            for (int y = 0; y < lat; y++)
            for (int x = 0; x < lon; x++)
            {
                int i0 = b + y * row + x, i1 = i0 + 1, i2 = i0 + row, i3 = i2 + 1;
                tris.Add(i0); tris.Add(i1); tris.Add(i2);
                tris.Add(i1); tris.Add(i3); tris.Add(i2);
            }
        }

        /// <summary>Upper half of a unit sphere (dome), flat base at y=0 (not capped).</summary>
        public void Dome(Matrix4x4 m, Color32 c, int lon = 10, int lat = 4)
        {
            var nm = NormalMatrix(m);
            int b = verts.Count;
            for (int y = 0; y <= lat; y++)
            {
                float phi = (float)y / lat * Mathf.PI * 0.5f;
                float sy = Mathf.Cos(phi), r = Mathf.Sin(phi);
                for (int x = 0; x <= lon; x++)
                {
                    float th = (float)x / lon * Mathf.PI * 2f;
                    var n = new Vector3(Mathf.Cos(th) * r, sy, Mathf.Sin(th) * r);
                    AddVert(m, nm, n * 0.5f, n, c);
                }
            }
            int row = lon + 1;
            for (int y = 0; y < lat; y++)
            for (int x = 0; x < lon; x++)
            {
                int i0 = b + y * row + x, i1 = i0 + 1, i2 = i0 + row, i3 = i2 + 1;
                tris.Add(i0); tris.Add(i1); tris.Add(i2);
                tris.Add(i1); tris.Add(i3); tris.Add(i2);
            }
        }

        // ---------------------------------------------------------------- CYLINDER / CONE
        /// <summary>Cylinder of height 1 (y -0.5..0.5), diameter 1. topScale 0 = cone.</summary>
        public void Cylinder(Matrix4x4 m, Color32 c, int segs = 8, float topScale = 1f, bool caps = true) =>
            Cylinder(m, c, c, segs, topScale, caps);

        public void Cylinder(Matrix4x4 m, Color32 bottomCol, Color32 topCol, int segs, float topScale, bool caps)
        {
            var nm = NormalMatrix(m);
            int b = verts.Count;
            float slope = (1f - topScale) * 0.5f; // radius delta over unit height
            for (int i = 0; i <= segs; i++)
            {
                float a = (float)i / segs * Mathf.PI * 2f;
                float cx = Mathf.Cos(a), cz = Mathf.Sin(a);
                var n = new Vector3(cx, slope, cz).normalized;
                AddVert(m, nm, new Vector3(cx * 0.5f, -0.5f, cz * 0.5f), n, bottomCol);
                AddVert(m, nm, new Vector3(cx * 0.5f * topScale, 0.5f, cz * 0.5f * topScale), n, topCol);
            }
            for (int i = 0; i < segs; i++)
            {
                int i0 = b + i * 2;
                tris.Add(i0); tris.Add(i0 + 1); tris.Add(i0 + 2);
                tris.Add(i0 + 1); tris.Add(i0 + 3); tris.Add(i0 + 2);
            }
            if (!caps) return;
            // bottom cap
            int cb = verts.Count;
            AddVert(m, nm, new Vector3(0, -0.5f, 0), Vector3.down, bottomCol);
            for (int i = 0; i <= segs; i++)
            {
                float a = (float)i / segs * Mathf.PI * 2f;
                AddVert(m, nm, new Vector3(Mathf.Cos(a) * 0.5f, -0.5f, Mathf.Sin(a) * 0.5f), Vector3.down, bottomCol);
            }
            for (int i = 0; i < segs; i++) { tris.Add(cb); tris.Add(cb + i + 1); tris.Add(cb + i + 2); }
            if (topScale <= 0.001f) return;
            int ct = verts.Count;
            AddVert(m, nm, new Vector3(0, 0.5f, 0), Vector3.up, topCol);
            for (int i = 0; i <= segs; i++)
            {
                float a = (float)i / segs * Mathf.PI * 2f;
                AddVert(m, nm, new Vector3(Mathf.Cos(a) * 0.5f * topScale, 0.5f, Mathf.Sin(a) * 0.5f * topScale), Vector3.up, topCol);
            }
            for (int i = 0; i < segs; i++) { tris.Add(ct); tris.Add(ct + i + 2); tris.Add(ct + i + 1); }
        }

        public void Cone(Matrix4x4 m, Color32 c, int segs = 8) => Cylinder(m, c, c, segs, 0f, true);

        /// <summary>Capsule along Y: total height 1, diameter = radiusScale.</summary>
        public void Capsule(Matrix4x4 m, Color32 c, int segs = 8)
        {
            // cylinder body 0.5 tall + two hemispheres of diameter 1 scaled to fit (ellipsoid caps)
            Cylinder(m * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1f, 0.5f, 1f)), c, segs, 1f, false);
            Sphere(m * Matrix4x4.TRS(new Vector3(0, 0.25f, 0), Quaternion.identity, new Vector3(1f, 0.5f, 1f)), c, segs, 4);
            Sphere(m * Matrix4x4.TRS(new Vector3(0, -0.25f, 0), Quaternion.identity, new Vector3(1f, 0.5f, 1f)), c, segs, 4);
        }

        // ---------------------------------------------------------------- FLAT-SHADED SHAPES
        void FlatTri(Matrix4x4 m, Matrix4x4 nm, Vector3 a, Vector3 b, Vector3 c, Color32 col)
        {
            Vector3 n = Vector3.Cross(b - a, c - a).normalized;
            int i = verts.Count;
            AddVert(m, nm, a, n, col);
            AddVert(m, nm, b, n, col);
            AddVert(m, nm, c, n, col);
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
        }

        /// <summary>Elongated octahedron (crystal). Unit height, y 0..1.</summary>
        public void Crystal(Matrix4x4 m, Color32 c, Color32 tip)
        {
            var nm = NormalMatrix(m);
            Vector3 top = new Vector3(0, 1f, 0), bot = new Vector3(0, 0f, 0);
            Vector3[] ring = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                ring[i] = new Vector3(Mathf.Cos(a) * 0.3f, 0.3f, Mathf.Sin(a) * 0.3f);
            }
            for (int i = 0; i < 6; i++)
            {
                var r0 = ring[i]; var r1 = ring[(i + 1) % 6];
                FlatTri(m, nm, r1, r0, top, tip);
                FlatTri(m, nm, r0, r1, bot, c);
            }
        }

        /// <summary>Faceted rock: jittered low-poly sphere with flat shading. Unit diameter.</summary>
        public void Rock(Matrix4x4 m, Color32 c, Color32 top, uint seed, int lon = 7, int lat = 5, float jitter = 0.18f)
        {
            var nm = NormalMatrix(m);
            var rng = new Rng(seed);
            var pts = new Vector3[(lat + 1) * lon];
            for (int y = 0; y <= lat; y++)
            {
                float phi = (float)y / lat * Mathf.PI;
                for (int x = 0; x < lon; x++)
                {
                    float th = (float)x / lon * Mathf.PI * 2f + (y % 2) * 0.4f;
                    float r = Mathf.Sin(phi);
                    var p = new Vector3(Mathf.Cos(th) * r, Mathf.Cos(phi), Mathf.Sin(th) * r) * 0.5f;
                    if (y > 0 && y < lat) p *= 1f + rng.Range(-jitter, jitter);
                    pts[y * lon + x] = p;
                }
            }
            for (int y = 0; y < lat; y++)
            for (int x = 0; x < lon; x++)
            {
                int x1 = (x + 1) % lon;
                var a = pts[y * lon + x]; var b = pts[y * lon + x1];
                var cc = pts[(y + 1) * lon + x]; var d = pts[(y + 1) * lon + x1];
                Color32 col = y < lat / 2 ? top : c;
                FlatTri(m, nm, a, b, cc, col);
                FlatTri(m, nm, b, d, cc, col);
            }
        }

        // ---------------------------------------------------------------- OUTPUT
        public Mesh ToMesh(string name = "wb_mesh")
        {
            var mesh = new Mesh { name = name };
            if (verts.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetColors(cols);
            mesh.SetTriangles(tris, 0, true);
            mesh.UploadMeshData(false);
            return mesh;
        }

        /// <summary>Append all geometry of another builder (with transform).</summary>
        public void Append(MeshBuilder other, Matrix4x4 m)
        {
            var nm = NormalMatrix(m);
            int b = verts.Count;
            for (int i = 0; i < other.verts.Count; i++)
            {
                verts.Add(m.MultiplyPoint3x4(other.verts[i]));
                norms.Add(nm.MultiplyVector(other.norms[i]).normalized);
                cols.Add(other.cols[i]);
            }
            for (int i = 0; i < other.tris.Count; i++) tris.Add(other.tris[i] + b);
        }
    }
}
