using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Wildbond
{
    /// <summary>
    /// Red skull that floats over wild kin far above the player's level: a "back off" warning you can read from a distance.
    /// One shared two-part mesh (bone + dark sockets), self-lit through the creature shader's flash color so it shows at night.
    /// </summary>
    public static class DangerSkull
    {
        /// <summary>A wild kin this many levels (or more) above the player gets the skull.</summary>
        public const int LevelGap = 10;
        public const float ShowRange = 80f;

        static Mesh bone, dark;
        static MaterialPropertyBlock boneMpb, darkMpb;
        static readonly int FlashId = Shader.PropertyToID("_FlashColor");
        static readonly Dictionary<string, float> tops = new Dictionary<string, float>();

        public static bool IsDanger(CreatureData d) =>
            d != null && GameManager.I != null && d.level >= GameManager.I.Player.Stats.level + LevelGap;

        static Matrix4x4 M(float x, float y, float z, float sx, float sy, float sz) =>
            Matrix4x4.TRS(new Vector3(x, y, z), Quaternion.identity, new Vector3(sx, sy, sz));

        static void BuildMeshes()
        {
            var mb = new MeshBuilder();
            Color32 red = new Color(0.9f, 0.12f, 0.1f), deep = new Color(0.6f, 0.05f, 0.06f), black = new Color(0.08f, 0.02f, 0.03f);
            // cranium + jaw (faces +Z)
            mb.Sphere(M(0, 0.12f, 0, 0.92f, 0.84f, 0.86f), red, deep, 12, 8);
            mb.Box(M(0, -0.36f, 0.06f, 0.5f, 0.3f, 0.46f), deep, red);
            bone = mb.ToMesh("danger_skull_bone");
            mb.Clear();
            // eye sockets, nose, gaps between the teeth
            mb.Sphere(M(0.2f, 0.08f, 0.34f, 0.28f, 0.32f, 0.22f), black, 8, 6);
            mb.Sphere(M(-0.2f, 0.08f, 0.34f, 0.28f, 0.32f, 0.22f), black, 8, 6);
            mb.Box(M(0, -0.13f, 0.4f, 0.1f, 0.14f, 0.1f), black);
            for (int i = -1; i <= 1; i++) mb.Box(M(i * 0.12f, -0.4f, 0.28f, 0.035f, 0.2f, 0.06f), black);
            dark = mb.ToMesh("danger_skull_dark");
            boneMpb = new MaterialPropertyBlock();
            boneMpb.SetColor(FlashId, new Color(1f, 0.1f, 0.08f, 0.7f));
            darkMpb = new MaterialPropertyBlock();
            darkMpb.SetColor(FlashId, new Color(0.05f, 0f, 0.01f, 0.9f));
        }

        /// <summary>Creates the skull as a child of the kin (hidden until shown).</summary>
        public static Transform Create(Transform parent)
        {
            if (bone == null) BuildMeshes();
            var root = new GameObject("DangerSkull").transform;
            root.SetParent(parent, false);
            Add(root, bone, boneMpb);
            Add(root, dark, darkMpb);
            root.gameObject.SetActive(false);
            return root;
        }

        static void Add(Transform root, Mesh mesh, MaterialPropertyBlock mpb)
        {
            var go = new GameObject(mesh.name);
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.Creature;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.SetPropertyBlock(mpb);
        }

        /// <summary>Top of a species' model in model space (before size scaling), so the skull clears horns and crests.</summary>
        public static float ModelTop(SpeciesDef sp)
        {
            if (tops.TryGetValue(sp.id, out float t)) return t;
            t = 0.5f;
            foreach (var p in sp.parts)
            {
                // half-height of the part's rotated bounding box
                var q = Quaternion.Euler(p.rot);
                float up = Mathf.Abs((q * Vector3.right).y) * p.scale.x + Mathf.Abs((q * Vector3.up).y) * p.scale.y + Mathf.Abs((q * Vector3.forward).y) * p.scale.z;
                t = Mathf.Max(t, p.pos.y + up * 0.5f);
            }
            tops[sp.id] = t;
            return t;
        }
    }
}
