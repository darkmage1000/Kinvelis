using UnityEngine;
using UnityEngine.Rendering;

namespace Wildbond
{
    /// <summary>
    /// Dressing + blockers for the start plateau (terrain shape lives in WorldGen.ApplyStart): a ring of boulders
    /// along the rim that leaves only the front open, and a stone gate framing the exit drop. One combined mesh,
    /// circle obstacles for collision.
    /// </summary>
    public static class StartArea
    {
        const float GapDeg = 22f;   // half-width of the open exit

        public static void Build(Transform parent)
        {
            var c = WorldGen.StartCenter;
            var fwd = WorldGen.StartFwd;
            float fwdAng = Mathf.Atan2(fwd.y, fwd.x);
            float r = WorldGen.StartR - 0.9f;
            float top = WorldGen.StartTop;
            var mb = new MeshBuilder();
            Color32 stone = new Color(0.55f, 0.55f, 0.6f), stoneTop = new Color(0.68f, 0.68f, 0.72f);
            Color32 moss = new Color(0.42f, 0.6f, 0.34f);

            // boulder rim (everything except the exit)
            float step = 2.3f / r;
            uint seed = 1;
            for (float a = 0f; a < Mathf.PI * 2f; a += step)
            {
                float rel = Mathf.DeltaAngle(fwdAng * Mathf.Rad2Deg, a * Mathf.Rad2Deg);
                if (Mathf.Abs(rel) < GapDeg) continue;
                var p = new Vector3(c.x + Mathf.Cos(a) * r, top, c.y + Mathf.Sin(a) * r);
                float s = 1.7f + Hash.F((int)(a * 100f), 7, WorldGen.Seed) * 1.1f;
                var m = Matrix4x4.TRS(p + Vector3.up * s * 0.35f, Quaternion.Euler(0f, a * 57f, 0f), new Vector3(s * 1.2f, s, s * 1.1f));
                mb.Rock(m, stone, (seed % 3 == 0) ? moss : stoneTop, seed++, 6, 4);
                WorldCollision.AddObstacle(p, s * 0.62f + 0.2f, null);
            }

            // exit gate: two standing stones and a lintel, just inside the drop
            float gr = WorldGen.StartR - 1.2f;
            Vector3 L = Vector3.zero, R = Vector3.zero;
            for (int k = -1; k <= 1; k += 2)
            {
                float a = fwdAng + k * (GapDeg + 1f) * Mathf.Deg2Rad;
                var p = new Vector3(c.x + Mathf.Cos(a) * gr, top, c.y + Mathf.Sin(a) * gr);
                mb.Cylinder(Matrix4x4.TRS(p + Vector3.up * 2.6f, Quaternion.Euler(0f, a * 57f, 0f), new Vector3(1.1f, 5.2f, 1.1f)), stone, stoneTop, 5, 0.8f, true);
                mb.Box(Matrix4x4.TRS(p + Vector3.up * 3.2f, Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, 0f), new Vector3(0.9f, 0.5f, 0.12f)), new Color(0.35f, 0.9f, 1f));
                WorldCollision.AddObstacle(p, 0.8f, null);
                if (k < 0) L = p; else R = p;
            }
            var mid = (L + R) * 0.5f;
            var dir = R - L; dir.y = 0f;
            mb.Box(Matrix4x4.TRS(mid + Vector3.up * 5.5f, Quaternion.LookRotation(dir.normalized, Vector3.up), new Vector3(1.1f, 0.8f, dir.magnitude + 1.6f)), stone, stoneTop);

            var go = new GameObject("StartPlateau");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("start_plateau");
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.Building;
            mr.shadowCastingMode = ShadowCastingMode.On;
        }
    }
}
