using System.Text;
using UnityEngine;

namespace Wildbond.EditorTools
{
    /// <summary>
    /// Dev harness for the cave pass (Play mode, via execute_code): enter a cave by id, list what it holds, and take
    /// pictures from any point with the game's own camera settings (fog, clear colour) - Previews/Caves.
    /// </summary>
    public static class CaveTest
    {
        public static string Enter(string id)
        {
            var cv = Caves.I;
            if (cv == null) return "no caves";
            if (Caves.InCave) cv.Abort();
            var e = cv.Entrances.Find(x => x.id == id);
            if (e == null) return "no cave " + id;
            cv.Enter(e);
            return "entering " + e.name + " (" + Caves.TierOf(e) + ")";
        }

        static object Run => typeof(Caves).GetField("run", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(Caves.I);

        public static string Stats()
        {
            var r = Run;
            if (r == null) return "not in a cave";
            var t = r.GetType();
            object G(string n) => t.GetField(n).GetValue(r);
            var sb = new StringBuilder();
            var rooms = (System.Collections.IList)G("rooms");
            sb.Append(G("tier")).Append(", ").Append(rooms.Count).Append(" chambers: ");
            foreach (var rm in rooms) { var rt = rm.GetType(); sb.Append(rt.GetField("role").GetValue(rm)).Append((int)rt.GetField("branch").GetValue(rm) >= 0 ? "*" : "").Append(' '); }
            foreach (var n in new[] { "guards", "kin", "chests", "nests", "nodes", "hazards", "watches", "traps" }) sb.Append("| ").Append(n).Append(' ').Append(((System.Collections.IList)G(n)).Count).Append(' ');
            var guards = (System.Collections.IList)G("guards");
            foreach (var g in guards)
            {
                var gt = g.GetType();
                var a = (CreatureActor)gt.GetField("a").GetValue(g);
                sb.Append("\n  ").Append(gt.GetField("role").GetValue(g)).Append(": ").Append(a.Data.DisplayName).Append(" Lv ").Append(a.Data.level).Append(a.Data.alpha ? " alpha" : "").Append(" hp ").Append(Mathf.RoundToInt(a.MaxHp));
            }
            var kin = (System.Collections.IList)G("kin");
            var names = new System.Collections.Generic.Dictionary<string, int>();
            foreach (CreatureActor k in kin) { if (k == null) continue; string s = k.Sp.displayName; names[s] = names.TryGetValue(s, out var c) ? c + 1 : 1; }
            sb.Append("\n  wild: ");
            foreach (var kv in names) sb.Append(kv.Key).Append(" x").Append(kv.Value).Append(", ");
            var root = (GameObject)G("root");
            int verts = 0;
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>()) if (mf.sharedMesh != null) verts += mf.sharedMesh.vertexCount;
            sb.Append("\n  verts ").Append(verts);
            return sb.ToString();
        }

        /// <summary>Room centres (role: x, z) so pictures can be aimed.</summary>
        public static string Rooms()
        {
            var r = Run;
            if (r == null) return "not in a cave";
            var rooms = (System.Collections.IList)r.GetType().GetField("rooms").GetValue(r);
            var sb = new StringBuilder();
            foreach (var rm in rooms)
            {
                var rt = rm.GetType();
                var c = (Vector2)rt.GetField("c").GetValue(rm);
                sb.Append(rt.GetField("role").GetValue(rm)).Append(" ").Append(c.x.ToString("F0")).Append(",").Append(c.y.ToString("F0")).Append(" r").Append(((float)rt.GetField("r").GetValue(rm)).ToString("F1")).Append("; ");
            }
            return sb.ToString();
        }

        /// <summary>A picture from 'pos' looking along yaw/pitch, saved as Previews/Caves/name.png.</summary>
        public static string Shot(string name, Vector3 pos, float yaw, float pitch = 12f, float fov = 60f)
        {
            var go = new GameObject("CaveShotCam");
            var cam = go.AddComponent<Camera>();
            cam.CopyFrom(Camera.main);
            cam.fieldOfView = fov;
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(pitch, yaw, 0f));
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
            System.IO.Directory.CreateDirectory("Previews/Caves");
            string path = "Previews/Caves/" + name + ".png";
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            RenderTexture.active = null; cam.targetTexture = null;
            Object.Destroy(rt); Object.Destroy(tex); Object.Destroy(go);
            return path;
        }

        /// <summary>A picture from inside a chamber: standing at its edge, eye height, looking across it.</summary>
        public static string RoomShot(string name, int room, float from = 0.75f, float height = 2.4f, float side = 0f)
        {
            var r = Run;
            var rooms = (System.Collections.IList)r.GetType().GetField("rooms").GetValue(r);
            var rm = rooms[room]; var rt = rm.GetType();
            var c = (Vector2)rt.GetField("c").GetValue(rm);
            float rad = (float)rt.GetField("r").GetValue(rm);
            var dir = Quaternion.Euler(0, side, 0) * Vector3.right;
            var p = new Vector3(c.x, 0f, c.y) - dir * rad * from;
            p.y = Caves.FloorAt(p.x, p.z) + height;
            return Shot(name, p, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg, 8f);
        }

        /// <summary>The player's own view: the game camera as it is now.</summary>
        public static string PlayerView(string name)
        {
            var cam = Camera.main;
            return Shot(name, cam.transform.position, cam.transform.eulerAngles.y, cam.transform.eulerAngles.x, cam.fieldOfView);
        }
    }
}
