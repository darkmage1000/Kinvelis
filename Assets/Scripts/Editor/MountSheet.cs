using UnityEngine;

namespace Wildbond.EditorTools
{
    /// <summary>
    /// Dev harness (Play mode, via execute_code): rides each mount in turn and photographs the rider from the side, to
    /// check saddle heights - Previews/Mounts/<id>.png and a contact sheet (sheet.png, 5 a row, in the order given).
    /// </summary>
    public static class MountSheet
    {
        public static readonly string[] NewMounts = { "galewing", "thornback", "bouldermaw", "tidewing", "kingmane", "cindermaw", "mangrowl", "zephyrion", "pyrovern", "glowmaw", "mirageon", "hydrabog", "halcyrie", "djinnado", "gemhorn" };

        public static string Run(string ids = null)
        {
            // without a harness a land mount counts as surf-only and sets you down at once
            var bag = GameManager.I.Player.Stats.Bag;
            if (bag.Count("kin_harness") == 0) bag.Add("kin_harness", 1);
            var go = new GameObject("MountSheetRunner");
            var r = go.AddComponent<Runner>();
            r.ids = string.IsNullOrEmpty(ids) ? NewMounts : ids.Split(',');
            return "riding " + r.ids.Length + " mounts";
        }

        class Runner : MonoBehaviour
        {
            public string[] ids;
            int i = -1;
            float wait;
            Texture2D sheet;
            const int Px = 360;

            void Update()
            {
                wait -= Time.unscaledDeltaTime;
                if (wait > 0f) return;
                var gm = GameManager.I;
                var pl = gm.Player;
                if (i >= 0 && pl.IsMounted) Shoot(ids[i]);
                i++;
                if (pl.IsMounted) pl.Dismount(true);
                if (i >= ids.Length)
                {
                    gm.Party.Recall(false);
                    System.IO.Directory.CreateDirectory("Previews/Mounts");
                    if (sheet != null) System.IO.File.WriteAllBytes("Previews/Mounts/sheet.png", sheet.EncodeToPNG());
                    Destroy(gameObject);
                    return;
                }
                var sp = ContentDB.Sp(ids[i]);
                if (sp == null) { wait = 0f; return; }
                var d = CreatureFactory.RollWild(sp, 30, BiomeId.Meadow, false);
                foreach (var o in gm.Party.All) if (o.location == KinLocation.Party) o.location = KinLocation.Box;   // a full party recalls a sixth
                d.location = KinLocation.Party;
                gm.Party.All.Add(d);
                gm.Party.Summon(d);
                var c = gm.Party.Companion;
                c.transform.position = pl.Position;
                c.transform.rotation = pl.transform.rotation;
                typeof(PlayerController).GetMethod("MountUp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(pl, new object[] { c });
                wait = 0.9f;
            }

            void Shoot(string id)
            {
                var pl = GameManager.I.Player;
                var m = pl.Mount;
                float s = Mathf.Max(1.2f, m.Sp.size * m.Data.SizeScale * 1.5f + 1.4f);
                Vector3 target = pl.Position + Vector3.up * (1.2f + pl.RiderLift * 0.6f);
                Vector3 from = target + pl.transform.right * s * 1.6f + pl.transform.forward * s * 0.35f + Vector3.up * 0.4f;
                var go = new GameObject("MountShotCam");
                var cam = go.AddComponent<Camera>();
                cam.CopyFrom(Camera.main);
                cam.fieldOfView = 45f;
                go.transform.SetPositionAndRotation(from, Quaternion.LookRotation(target - from));
                var rt = new RenderTexture(Px, Px, 24);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(Px, Px, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, Px, Px), 0, 0); tex.Apply();
                System.IO.Directory.CreateDirectory("Previews/Mounts");
                System.IO.File.WriteAllBytes("Previews/Mounts/" + id + ".png", tex.EncodeToPNG());
                int cols = 5, rows = (ids.Length + cols - 1) / cols;
                if (sheet == null) { sheet = new Texture2D(Px * cols, Px * rows, TextureFormat.RGB24, false); var fill = new Color32[sheet.width * sheet.height]; sheet.SetPixels32(fill); }
                int k = System.Array.IndexOf(ids, id);
                sheet.SetPixels((k % cols) * Px, (rows - 1 - k / cols) * Px, Px, Px, tex.GetPixels());
                sheet.Apply();
                RenderTexture.active = null; cam.targetTexture = null;
                Object.Destroy(rt); Object.Destroy(tex); Object.Destroy(go);
            }
        }
    }
}
