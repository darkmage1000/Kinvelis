using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Wildbond
{
    /// <summary>
    /// Builds animated, vertex-colored models from BodyPart plans. Each BodyGroup becomes one child
    /// transform with one combined mesh (so a creature is ~4-10 draw calls, all SRP-batched).
    /// Meshes are cached per model key (species + variant) and shared by every instance.
    /// </summary>
    public static class ModelBuilder
    {
        static readonly Dictionary<string, Mesh[]> cache = new Dictionary<string, Mesh[]>();
        static readonly MeshBuilder mb = new MeshBuilder();
        const int GroupCount = (int)BodyGroup.Count;

        /// <summary>Which group a group hangs from (Count = straight off the model). Only the human skeleton nests.</summary>
        public static BodyGroup ParentOf(BodyGroup g)
        {
            switch (g)
            {
                case BodyGroup.Head: case BodyGroup.ArmL: case BodyGroup.ArmR: return BodyGroup.Torso;
                case BodyGroup.ForearmL: return BodyGroup.ArmL;
                case BodyGroup.ForearmR: return BodyGroup.ArmR;
                case BodyGroup.ShinL: return BodyGroup.LegFL;
                case BodyGroup.ShinR: return BodyGroup.LegFR;
                default: return BodyGroup.Count;
            }
        }

        public static void AddPart(MeshBuilder b, BodyPart p, Color col, Vector3 offset)
        {
            var m = Matrix4x4.TRS(p.pos - offset, Quaternion.Euler(p.rot), p.scale);
            Color32 c = col;
            switch (p.shape)
            {
                case PartShape.Sphere: b.Sphere(m, c, Darker(col, 0.8f), 10, 7); break;
                case PartShape.Box: b.Box(m, Darker(col, 0.9f), c); break;
                case PartShape.Cylinder: b.Cylinder(m, Darker(col, 0.85f), c, 8, 1f, true); break;
                case PartShape.Cone: b.Cylinder(m, c, Lighter(col, 1.15f), 8, 0f, true); break;
                case PartShape.Capsule: b.Capsule(m, c, 8); break;
                case PartShape.Crystal: b.Crystal(m, c, Lighter(col, 1.3f)); break;
                case PartShape.Rock: b.Rock(m, Darker(col, 0.85f), c, (uint)(p.pos.GetHashCode() | 1), 6, 5, 0.15f); break;
                case PartShape.Dome: b.Dome(m, c, 12, 5); break;
            }
        }

        static Color32 Darker(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, 1f);
        static Color32 Lighter(Color c, float k) => new Color(Mathf.Min(1f, c.r * k), Mathf.Min(1f, c.g * k), Mathf.Min(1f, c.b * k), 1f);

        static Vector3 PivotOf(GroupPivot[] pivots, BodyGroup g)
        {
            if (pivots != null)
                for (int i = 0; i < pivots.Length; i++)
                    if (pivots[i].group == g) return pivots[i].pivot;
            return Vector3.zero;
        }

        /// <summary>Returns meshes per BodyGroup (null where empty). Extra parts for variants are injected here.</summary>
        static Mesh[] GetMeshes(CreatureData d)
        {
            string key = d.ModelKey;
            if (cache.TryGetValue(key, out var meshes)) return meshes;
            var sp = d.Species;
            var pal = d.Palette;
            var parts = new List<BodyPart>(sp.parts);

            // ---- variant geometry ----
            float topY = 0f, headTop = 0f; Vector3 bodyC = Vector3.zero; int bodyN = 0;
            foreach (var p in sp.parts)
            {
                float t = p.pos.y + p.scale.y * 0.5f;
                if (p.group == BodyGroup.Body) { topY = Mathf.Max(topY, t); bodyC += p.pos; bodyN++; }
                if (p.group == BodyGroup.Head) headTop = Mathf.Max(headTop, t);
            }
            if (bodyN > 0) bodyC /= bodyN;
            if (d.mutation == Mutation.Crystalline)
            {
                parts.Add(new BodyPart(PartShape.Crystal, BodyGroup.Body, ColorSlot.Glow, new Vector3(0.08f, topY - 0.08f, bodyC.z), new Vector3(0.14f, 0.35f, 0.14f), new Vector3(0, 0, -15)));
                parts.Add(new BodyPart(PartShape.Crystal, BodyGroup.Body, ColorSlot.Glow, new Vector3(-0.1f, topY - 0.1f, bodyC.z - 0.12f), new Vector3(0.11f, 0.28f, 0.11f), new Vector3(10, 0, 20)));
                parts.Add(new BodyPart(PartShape.Crystal, BodyGroup.Body, ColorSlot.Glow, new Vector3(0f, topY - 0.08f, bodyC.z + 0.14f), new Vector3(0.1f, 0.24f, 0.1f), new Vector3(-12, 0, 4)));
            }
            if (d.mutation == Mutation.Twintail)
            {
                foreach (var p in sp.parts)
                {
                    if (p.group != BodyGroup.Tail) continue;
                    var a = p; a.pos.x += 0.09f; a.rot.y += 18f; parts.Add(a);
                }
            }
            if (d.alpha)
            {
                float y = headTop > 0 ? headTop : topY;
                var hg = headTop > 0 ? BodyGroup.Head : BodyGroup.Body;
                Vector3 hc = Vector3.zero; int hn = 0;
                foreach (var p in sp.parts) if (p.group == hg) { hc += p.pos; hn++; }
                if (hn > 0) hc /= hn;
                var gold = ColorSlot.Accent;
                parts.Add(new BodyPart(PartShape.Cone, hg, gold, new Vector3(hc.x, y + 0.02f, hc.z), new Vector3(0.08f, 0.16f, 0.08f)));
                parts.Add(new BodyPart(PartShape.Cone, hg, gold, new Vector3(hc.x + 0.08f, y - 0.01f, hc.z), new Vector3(0.06f, 0.12f, 0.06f), new Vector3(0, 0, -20)));
                parts.Add(new BodyPart(PartShape.Cone, hg, gold, new Vector3(hc.x - 0.08f, y - 0.01f, hc.z), new Vector3(0.06f, 0.12f, 0.06f), new Vector3(0, 0, 20)));
            }

            meshes = new Mesh[GroupCount];
            for (int g = 0; g < GroupCount; g++)
            {
                mb.Clear();
                var grp = (BodyGroup)g;
                Vector3 pivot = PivotOf(sp.pivots, grp);
                foreach (var p in parts)
                {
                    if (p.group != grp) continue;
                    Color col = pal.Get(p.slot);
                    if (d.alpha && p.slot == ColorSlot.Accent) col = Color.Lerp(col, new Color(1f, 0.8f, 0.25f), 0.6f);
                    int v0 = mb.cols.Count;
                    AddPart(mb, p, col, pivot);
                    // a legendary's glowing parts (and its orbiting orbs) are self-lit: vertex alpha 0 tells the shader so
                    if (sp.rarity == Rarity.Legendary && (p.slot == ColorSlot.Glow || (p.slot == ColorSlot.White && p.group == BodyGroup.Extra)))
                        for (int i = v0; i < mb.cols.Count; i++) { var vc = mb.cols[i]; vc.a = 0; mb.cols[i] = vc; }
                }
                if (mb.VertexCount > 0) meshes[g] = mb.ToMesh($"{sp.id}_{grp}");
            }
            cache[key] = meshes;
            return meshes;
        }

        /// <summary>Creates the model hierarchy under parent and returns the rig.</summary>
        public static CreatureRig Build(CreatureData d, Transform parent, Material mat)
        {
            var sp = d.Species;
            var meshes = GetMeshes(d);
            var model = new GameObject("Model").transform;
            model.SetParent(parent, false);
            float s = sp.size * d.SizeScale;
            model.localScale = Vector3.one * s;
            var rig = parent.gameObject.GetComponent<CreatureRig>();
            if (rig == null) rig = parent.gameObject.AddComponent<CreatureRig>();
            rig.Setup(model, sp.locomotion, s);
            rig.ExtraMode = sp.extraAnim;
            for (int g = 0; g < GroupCount; g++)
            {
                if (meshes[g] == null) continue;
                var grp = (BodyGroup)g;
                var go = new GameObject(grp.ToString());
                go.transform.SetParent(model, false);
                go.transform.localPosition = PivotOf(sp.pivots, grp);
                go.AddComponent<MeshFilter>().sharedMesh = meshes[g];
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = QualityProfile.Current.shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                mr.receiveShadows = true;
                rig.Register(grp, go.transform, mr);
            }
            return rig;
        }

        /// <summary>Animated model from an arbitrary plan (used for the player avatar).</summary>
        public static CreatureRig BuildCustom(string key, BodyPart[] parts, GroupPivot[] pivots, Palette pal, Transform parent, Material mat, Locomotion loco, float scale)
        {
            if (!cache.TryGetValue(key, out var meshes))
            {
                meshes = new Mesh[GroupCount];
                for (int g = 0; g < GroupCount; g++)
                {
                    mb.Clear();
                    var grp = (BodyGroup)g;
                    Vector3 pivot = PivotOf(pivots, grp);
                    foreach (var p in parts) if (p.group == grp) AddPart(mb, p, pal.Get(p.slot), pivot);
                    if (mb.VertexCount > 0) meshes[g] = mb.ToMesh($"{key}_{grp}");
                }
                cache[key] = meshes;
            }
            var model = new GameObject("Model").transform;
            model.SetParent(parent, false);
            model.localScale = Vector3.one * scale;
            var rig = parent.gameObject.GetComponent<CreatureRig>();
            if (rig == null) rig = parent.gameObject.AddComponent<CreatureRig>();
            rig.Setup(model, loco, scale);
            var made = new Transform[GroupCount];
            var rends = new Renderer[GroupCount];
            for (int g = 0; g < GroupCount; g++)
            {
                if (meshes[g] == null) continue;
                var grp = (BodyGroup)g;
                var go = new GameObject(grp.ToString());
                go.transform.SetParent(model, false);
                go.transform.localPosition = PivotOf(pivots, grp);
                go.AddComponent<MeshFilter>().sharedMesh = meshes[g];
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = ShadowCastingMode.On;
                made[g] = go.transform;
                rends[g] = mr;
            }
            // a skeleton where the plan has one: forearms hang from arms, shins from legs, head and arms from the torso
            for (int g = 0; g < GroupCount; g++)
            {
                if (made[g] == null) continue;
                var par = ParentOf((BodyGroup)g);
                if (par == BodyGroup.Count || made[(int)par] == null) continue;
                made[g].SetParent(made[(int)par], false);
                made[g].localPosition = PivotOf(pivots, (BodyGroup)g) - PivotOf(pivots, par);
            }
            for (int g = 0; g < GroupCount; g++)
                if (made[g] != null) rig.Register((BodyGroup)g, made[g], rends[g]);
            return rig;
        }

        /// <summary>Static building/landmark mesh from a part list (single combined mesh).</summary>
        public static Mesh BuildStatic(BodyPart[] parts, Palette pal, string name)
        {
            mb.Clear();
            foreach (var p in parts) AddPart(mb, p, pal.Get(p.slot), Vector3.zero);
            return mb.ToMesh(name);
        }
    }
}
