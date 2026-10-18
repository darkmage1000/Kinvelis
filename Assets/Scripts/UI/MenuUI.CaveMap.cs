using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>
    /// The Map tab underground (cave pass 2, user: "getting lost"): the cave's own map, zoomed to its chambers, with
    /// where you are, the way out and everything found so far (`Caves.MapMarks`), what is still to do
    /// (`Caves.MapSummary`) and a button back to the mouth (or the checkpoint on a lower level).
    /// </summary>
    public partial class MenuUI
    {
        readonly List<Caves.CaveMark> mapMarks = new List<Caves.CaveMark>();

        void BuildCaveMap()
        {
            var cv = Caves.I;
            const float size = 860f;
            var frame = UIFactory.Panel(content, "CaveMapFrame", new Color(0.02f, 0.02f, 0.04f, 1f));
            frame.rectTransform.Place(new Vector2(0.38f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size + 20f, size + 20f));
            var img = UIFactory.Rect(frame.transform, "CaveMap").gameObject.AddComponent<RawImage>();
            img.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
            img.texture = cv.MapTexture;
            var b = cv.MapBounds();
            img.uvRect = b;
            img.raycastTarget = false;
            Vector2 ToMap(Vector3 w) { var uv = Caves.ZoneUV(w); return new Vector2((uv.x - b.x) / b.width - 0.5f, (uv.y - b.y) / b.height - 0.5f) * size; }
            cv.MapMarks(mapMarks);
            foreach (var m in mapMarks)
            {
                var mk = UIFactory.Panel(img.transform, "Mark", m.col, MapIcons.Get(m.icon));
                mk.type = Image.Type.Simple; mk.preserveAspect = true; mk.raycastTarget = false;
                mk.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), ToMap(m.pos), new Vector2(40, 40));
            }
            var pl = GameManager.I.Player;
            var you = UIFactory.Label(img.transform, "^", 54, new Color(1f, 0.95f, 0.4f), TextAnchor.MiddleCenter, FontStyle.Bold);
            you.raycastTarget = false;
            you.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), ToMap(pl.Position), new Vector2(60, 60));
            you.rectTransform.localRotation = Quaternion.Euler(0, 0, -pl.transform.eulerAngles.y);

            var legend = UIFactory.Rect(content, "Legend");
            legend.Anchors(new Vector2(0.76f, 0), new Vector2(1, 1), new Vector2(0, 20), new Vector2(-20, -20));
            var v = legend.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 10; v.childControlHeight = true; v.childControlWidth = true; v.childForceExpandHeight = false;
            Header(legend, cv.CurrentName, 32);
            foreach (var line in cv.MapSummary()) Para(legend, line, 21, Color.white);
            Para(legend, "<size=18>" + UIFactory.Col("Bright = where you have been. Arch = way out, rope = between levels, paw = guardian, gate = vault puzzle.", UIFactory.TextDim) + "</size>", 18);
            ActionBtn(legend, cv.OnLowerLevel ? "BACK TO THE CHECKPOINT" : "BACK TO THE CAVE MOUTH", () => { Close(); Caves.I.ReturnToStart(); }, new Color(0.3f, 0.45f, 0.6f));
        }
    }
}
