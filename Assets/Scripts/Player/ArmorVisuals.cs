using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Wildbond
{
    /// <summary>
    /// Procedural armor/accessory meshes for the explorer (styles: 0 cloth, 1 metal, 2 rime crystal, 3 ember, 4 thorn).
    /// Pieces are built in model space, split per body group so they follow the skeleton (chest pieces on the Torso,
    /// boots on the Shins, a bracelet on the left Forearm, head gear on the Head),
    /// and cached per item id. Also provides the inventory icon mesh for gear.
    /// </summary>
    public static class GearModels
    {
        struct Piece { public BodyGroup group; public Mesh mesh; }
        static readonly Dictionary<string, List<Piece>> cache = new Dictionary<string, List<Piece>>();
        static readonly Dictionary<BodyGroup, MeshBuilder> builders = new Dictionary<BodyGroup, MeshBuilder>();
        static Vector3 off;     // -pivot of the group being written

        static Matrix4x4 M(float px, float py, float pz, float sx, float sy, float sz, float rx = 0, float ry = 0, float rz = 0) =>
            Matrix4x4.TRS(new Vector3(px, py, pz) + off, Quaternion.Euler(rx, ry, rz), new Vector3(sx, sy, sz));

        static MeshBuilder G(BodyGroup g)
        {
            if (!builders.TryGetValue(g, out var mb)) { mb = new MeshBuilder(); builders[g] = mb; }
            off = -Pivot(g);
            return mb;
        }

        static Vector3 Pivot(BodyGroup g)
        {
            foreach (var p in PlayerLook.Pivots) if (p.group == g) return p.pivot;
            return Vector3.zero;
        }

        /// <summary>Meshes per body group for one gear item (model space relative to each group's pivot).</summary>
        public static void Attach(ItemDef d, CreatureRig rig, List<GameObject> spawned)
        {
            if (d == null) return;
            if (!cache.TryGetValue(d.id, out var pieces))
            {
                pieces = new List<Piece>();
                foreach (var mb in builders.Values) mb.Clear();
                Write(d);
                foreach (var kv in builders)
                    if (kv.Value.VertexCount > 0) pieces.Add(new Piece { group = kv.Key, mesh = kv.Value.ToMesh("gear_" + d.id + "_" + kv.Key) });
                cache[d.id] = pieces;
            }
            foreach (var p in pieces)
            {
                var parent = rig.GetGroup(p.group);
                if (parent == null) continue;
                var go = new GameObject("Gear_" + d.id);
                go.layer = parent.gameObject.layer;
                go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = p.mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = RenderAssets.Creature;
                mr.shadowCastingMode = ShadowCastingMode.On;
                spawned.Add(go);
            }
        }

        /// <summary>Single combined mesh for the inventory icon.</summary>
        public static Mesh Icon(ItemDef d)
        {
            foreach (var mb in builders.Values) mb.Clear();
            Write(d);
            var all = new MeshBuilder();
            foreach (var kv in builders)
                if (kv.Value.VertexCount > 0) all.Append(kv.Value, Matrix4x4.Translate(Pivot(kv.Key)));
            // accessories are tiny on the body: icons get a dedicated, larger model
            if (!GearSlots.IsArmor(d.equipSlot)) { all.Clear(); AccessoryIcon(all, d); }
            return all.VertexCount > 0 ? all.ToMesh("itemicon_" + d.id) : null;
        }

        // ------------------------------------------------------------------ pieces
        static void Write(ItemDef d)
        {
            Color32 a = d.iconColor, t = d.trimColor;
            Color32 dark = Color.Lerp(d.iconColor, Color.black, 0.35f);
            switch (d.equipSlot)
            {
                case EquipSlot.Head: Head(d.gearStyle, a, t, dark); break;
                case EquipSlot.Body: Body(d.gearStyle, a, t, dark); break;
                case EquipSlot.Boots: Boot(G(BodyGroup.ShinR), 0.11f, d.gearStyle, a, t, dark); Boot(G(BodyGroup.ShinL), -0.11f, d.gearStyle, a, t, dark); break;   // below the knee
                case EquipSlot.Amulet:
                {
                    var mb = G(BodyGroup.Torso);
                    mb.Cylinder(M(0, 1.37f, 0.02f, 0.3f, 0.02f, 0.26f), t, 10, 1f, false);
                    if (d.gearStyle == 2) mb.Crystal(M(0, 1.2f, 0.17f, 0.07f, 0.13f, 0.05f), a, Color.white);
                    else mb.Sphere(M(0, 1.26f, 0.17f, 0.08f, 0.09f, 0.05f), a, 8, 5);
                    break;
                }
                case EquipSlot.Bracelet:
                {
                    var mb = G(BodyGroup.ForearmL);   // on the wrist
                    mb.Cylinder(M(-0.29f, 0.93f, 0, 0.16f, 0.06f, 0.16f), a, t, 8, 1f, true);
                    break;
                }
                // rings are too small to read on the body
            }
        }

        static void Head(int style, Color32 a, Color32 t, Color32 dark)
        {
            var mb = G(BodyGroup.Head);
            switch (style)
            {
                case 0: // hood
                    mb.Sphere(M(0, 1.63f, -0.05f, 0.38f, 0.38f, 0.38f), a, dark, 10, 7);
                    mb.Cylinder(M(0, 1.47f, -0.01f, 0.34f, 0.05f, 0.32f), t, 10, 1f, true);
                    mb.Cone(M(0, 1.66f, -0.26f, 0.12f, 0.18f, 0.08f, -120, 0, 0), a, 6);
                    break;
                case 1: // helm
                    mb.Dome(M(0, 1.62f, -0.01f, 0.4f, 0.4f, 0.4f), a, 10, 4);
                    mb.Cylinder(M(0, 1.62f, -0.01f, 0.45f, 0.04f, 0.45f), t, 12, 1f, true);
                    mb.Box(M(0, 1.6f, 0.17f, 0.04f, 0.12f, 0.03f), a);
                    mb.Sphere(M(0, 1.83f, -0.01f, 0.07f, 0.06f, 0.07f), t, 6, 4);
                    break;
                case 2: // crown
                    mb.Cylinder(M(0, 1.72f, 0, 0.34f, 0.06f, 0.34f), t, 12, 1f, true);
                    mb.Crystal(M(0, 1.72f, 0.14f, 0.07f, 0.2f, 0.06f), a, Color.white);
                    mb.Crystal(M(0.12f, 1.72f, 0.08f, 0.05f, 0.13f, 0.05f, 0, 0, -18), a, Color.white);
                    mb.Crystal(M(-0.12f, 1.72f, 0.08f, 0.05f, 0.13f, 0.05f, 0, 0, 18), a, Color.white);
                    mb.Crystal(M(0.15f, 1.72f, -0.06f, 0.04f, 0.1f, 0.04f, 0, 0, -30), a, Color.white);
                    mb.Crystal(M(-0.15f, 1.72f, -0.06f, 0.04f, 0.1f, 0.04f, 0, 0, 30), a, Color.white);
                    break;
                case 3: // horned helm
                    mb.Dome(M(0, 1.61f, -0.01f, 0.4f, 0.42f, 0.4f), a, 10, 4);
                    mb.Cylinder(M(0, 1.61f, -0.01f, 0.43f, 0.05f, 0.43f), dark, 12, 1f, true);
                    mb.Box(M(0, 1.66f, 0.19f, 0.22f, 0.03f, 0.03f), t);
                    mb.Cone(M(0.15f, 1.8f, 0, 0.07f, 0.2f, 0.07f, 0, 0, -28), t, 6);
                    mb.Cone(M(-0.15f, 1.8f, 0, 0.07f, 0.2f, 0.07f, 0, 0, 28), t, 6);
                    break;
                case 5: // petal crown: a green band with a ring of petals and a few buds
                    mb.Cylinder(M(0, 1.71f, 0, 0.35f, 0.04f, 0.35f), t, 12, 1f, true);
                    for (int i = 0; i < 8; i++)
                    {
                        float ang = i * 45f * Mathf.Deg2Rad;
                        mb.Sphere(M(Mathf.Sin(ang) * 0.185f, 1.745f, Mathf.Cos(ang) * 0.185f, 0.13f, 0.06f, 0.13f, Mathf.Cos(ang) * 28f, 0, -Mathf.Sin(ang) * 28f), i % 2 == 0 ? a : (Color32)Color.Lerp(a, Color.white, 0.5f), 6, 4);
                        if (i % 2 == 0) mb.Sphere(M(Mathf.Sin(ang) * 0.185f, 1.775f, Mathf.Cos(ang) * 0.185f, 0.045f, 0.045f, 0.045f), new Color(1f, 0.85f, 0.3f), 5, 4);
                    }
                    break;
                case 8: // frost hood: a deep hood with a thick fur rim and two shards at the brow
                    mb.Sphere(M(0, 1.63f, -0.05f, 0.4f, 0.4f, 0.4f), a, dark, 10, 7);
                    // the fur rim: tufts round the face opening, open at the chin
                    for (int i = 0; i < 9; i++)
                    {
                        float ang = (-120f + i * 30f) * Mathf.Deg2Rad;
                        mb.Sphere(M(Mathf.Sin(ang) * 0.185f, 1.63f + Mathf.Cos(ang) * 0.185f, 0.085f, 0.1f, 0.1f, 0.09f), t, 6, 4);
                    }
                    mb.Cone(M(0, 1.7f, -0.28f, 0.14f, 0.2f, 0.1f, -115, 0, 0), a, 6);
                    mb.Crystal(M(0.1f, 1.8f, 0.1f, 0.035f, 0.1f, 0.035f, 15, 0, -20), new Color(0.56f, 0.91f, 1f), Color.white);
                    mb.Crystal(M(-0.1f, 1.8f, 0.1f, 0.035f, 0.1f, 0.035f, 15, 0, 20), new Color(0.56f, 0.91f, 1f), Color.white);
                    break;
                case 10: // dune wrap: wound cloth, a gold band and a stone at the front, a tail of cloth down the back
                    mb.Sphere(M(0, 1.7f, -0.01f, 0.42f, 0.24f, 0.42f), a, dark, 10, 6);
                    mb.Sphere(M(0.03f, 1.78f, -0.02f, 0.36f, 0.18f, 0.36f, 0, 0, 12), a, 10, 5);
                    mb.Sphere(M(-0.02f, 1.85f, -0.02f, 0.26f, 0.14f, 0.26f, 0, 0, -10), (Color32)Color.Lerp(a, Color.white, 0.25f), 8, 5);
                    mb.Cylinder(M(0, 1.66f, -0.01f, 0.43f, 0.035f, 0.43f), t, 12, 1f, true);
                    mb.Sphere(M(0, 1.72f, 0.2f, 0.07f, 0.08f, 0.05f), new Color(0.3f, 0.8f, 0.65f, 0f), 6, 4);
                    mb.Box(M(0.06f, 1.42f, -0.2f, 0.13f, 0.38f, 0.03f, 8, 0, 6), a, dark);
                    break;
                default: // thorn circlet
                    mb.Cylinder(M(0, 1.7f, 0, 0.34f, 0.05f, 0.34f), a, 10, 1f, true);
                    for (int i = 0; i < 7; i++)
                    {
                        float ang = i * 51f * Mathf.Deg2Rad;
                        mb.Cone(M(Mathf.Sin(ang) * 0.17f, 1.76f, Mathf.Cos(ang) * 0.17f, 0.05f, 0.12f, 0.05f, Mathf.Cos(ang) * 20f, 0, -Mathf.Sin(ang) * 20f), t, 5);
                    }
                    break;
            }
        }

        static void Body(int style, Color32 a, Color32 t, Color32 dark)
        {
            var mb = G(BodyGroup.Torso);   // worn on the chest: it turns and leans with the waist
            switch (style)
            {
                case 0: // field coat
                    mb.Capsule(M(0, 1.05f, 0, 0.5f, 0.68f, 0.36f), a, 10);
                    mb.Cylinder(M(0, 0.86f, 0, 0.51f, 0.07f, 0.37f), t, 10, 1f, true);
                    mb.Cylinder(M(0, 1.37f, 0, 0.36f, 0.07f, 0.3f), t, 10, 1f, true);
                    mb.Box(M(0.12f, 1.0f, 0.17f, 0.1f, 0.1f, 0.03f), dark);
                    mb.Box(M(-0.12f, 1.0f, 0.17f, 0.1f, 0.1f, 0.03f), dark);
                    break;
                case 1: // mail
                    mb.Box(M(0, 1.12f, 0.01f, 0.5f, 0.46f, 0.36f), a, t);
                    mb.Cylinder(M(0, 0.86f, 0, 0.5f, 0.08f, 0.36f), t, 10, 1f, true);
                    mb.Box(M(0, 0.95f, 0.17f, 0.46f, 0.02f, 0.03f), dark);
                    mb.Box(M(0, 1.1f, 0.19f, 0.46f, 0.02f, 0.03f), dark);
                    mb.Sphere(M(0.29f, 1.32f, 0, 0.22f, 0.15f, 0.22f), a, dark, 8, 5);
                    mb.Sphere(M(-0.29f, 1.32f, 0, 0.22f, 0.15f, 0.22f), a, dark, 8, 5);
                    break;
                case 2: // cloak
                    mb.Cylinder(M(0, 1.0f, -0.03f, 0.58f, 0.8f, 0.44f), t, a, 10, 0.62f, true);
                    mb.Sphere(M(0, 1.38f, -0.06f, 0.4f, 0.16f, 0.34f), a, 10, 5);
                    mb.Crystal(M(0, 1.28f, 0.17f, 0.06f, 0.12f, 0.05f), a, Color.white);
                    mb.Crystal(M(0.22f, 1.36f, -0.05f, 0.05f, 0.14f, 0.05f, 0, 0, -30), a, Color.white);
                    mb.Crystal(M(-0.22f, 1.36f, -0.05f, 0.05f, 0.14f, 0.05f, 0, 0, 30), a, Color.white);
                    break;
                case 3: // plate
                    mb.Box(M(0, 1.12f, 0.01f, 0.52f, 0.48f, 0.37f), a, dark);
                    mb.Box(M(0, 1.12f, 0.195f, 0.05f, 0.38f, 0.02f), t);
                    mb.Box(M(0, 1.2f, 0.195f, 0.34f, 0.04f, 0.02f), t);
                    mb.Cylinder(M(0, 0.86f, 0, 0.52f, 0.08f, 0.38f), dark, 10, 1f, true);
                    mb.Sphere(M(0.3f, 1.33f, 0, 0.24f, 0.16f, 0.24f), a, 8, 5);
                    mb.Sphere(M(-0.3f, 1.33f, 0, 0.24f, 0.16f, 0.24f), a, 8, 5);
                    mb.Cone(M(0.32f, 1.44f, 0, 0.06f, 0.14f, 0.06f, 0, 0, -20), t, 5);
                    mb.Cone(M(-0.32f, 1.44f, 0, 0.06f, 0.14f, 0.06f, 0, 0, 20), t, 5);
                    break;
                case 6: // shellback mantle: a ribbed reef shell across the back and a small one on each shoulder
                    mb.Capsule(M(0, 1.06f, 0, 0.48f, 0.62f, 0.34f), dark, 10);
                    // the shell: a flattened round back-plate, with ridges that follow its curve (thin slices of the same shape, standing just proud of it)
                    mb.Sphere(M(0, 1.06f, -0.2f, 0.62f, 0.72f, 0.3f), a, dark, 10, 7);
                    for (int i = -2; i <= 2; i++) mb.Sphere(M(i * 0.11f, 1.06f, -0.215f, 0.035f, 0.74f - Mathf.Abs(i) * 0.1f, 0.33f - Mathf.Abs(i) * 0.03f), t, 6, 6);
                    mb.Dome(M(0.29f, 1.33f, 0, 0.24f, 0.14f, 0.24f), a, 8, 3);
                    mb.Dome(M(-0.29f, 1.33f, 0, 0.24f, 0.14f, 0.24f), a, 8, 3);
                    mb.Cylinder(M(0, 0.86f, 0, 0.5f, 0.06f, 0.36f), t, 10, 1f, true);
                    break;
                case 9: // reedweave cloak: a collar and a skirt of hanging reeds, lighter and darker by turns
                    mb.Capsule(M(0, 1.08f, 0, 0.48f, 0.58f, 0.34f), dark, 10);
                    mb.Cylinder(M(0, 1.36f, 0, 0.42f, 0.09f, 0.34f), t, 10, 1f, true);
                    for (int i = 0; i < 14; i++)
                    {
                        float ang = i * (360f / 14f) * Mathf.Deg2Rad;
                        float len = 0.5f + (i % 3) * 0.05f;
                        mb.Box(M(Mathf.Sin(ang) * 0.27f, 1.34f - len * 0.5f, Mathf.Cos(ang) * 0.2f, 0.085f, len, 0.03f, Mathf.Cos(ang) * 9f, ang * Mathf.Rad2Deg, 0), i % 2 == 0 ? a : t, i % 2 == 0 ? dark : a);
                    }
                    break;
                case 12: // starweave mantle: night cloth to the hips, a high collar, and lights sewn into it
                    mb.Cylinder(M(0, 1.0f, -0.03f, 0.6f, 0.82f, 0.46f), dark, a, 10, 0.6f, true);
                    mb.Sphere(M(0, 1.38f, -0.05f, 0.42f, 0.16f, 0.36f), a, 10, 5);
                    mb.Cylinder(M(0, 1.44f, -0.03f, 0.3f, 0.1f, 0.26f), a, 10, 1.25f, false);
                    for (int i = 0; i < 9; i++)
                    {
                        float ang = (i * 40f + (i % 2) * 14f) * Mathf.Deg2Rad;
                        float y = 0.74f + ((i * 7) % 5) * 0.11f;
                        float r = Mathf.Lerp(0.3f, 0.2f, (y - 0.7f) / 0.6f);
                        mb.Sphere(M(Mathf.Sin(ang) * r, y, Mathf.Cos(ang) * r * 0.78f - 0.03f, 0.035f, 0.035f, 0.035f), new Color(t.r / 255f, t.g / 255f, t.b / 255f, 0f), 5, 4);
                    }
                    mb.Crystal(M(0, 1.28f, 0.18f, 0.05f, 0.1f, 0.04f), new Color(1f, 1f, 1f, 0f), Color.white);
                    break;
                default: // thorn vest
                    mb.Capsule(M(0, 1.07f, 0, 0.48f, 0.6f, 0.34f), a, 10);
                    for (int i = 0; i < 10; i++)
                    {
                        float ang = i * 36f * Mathf.Deg2Rad;
                        float y = 0.92f + (i % 3) * 0.16f;
                        mb.Cone(M(Mathf.Sin(ang) * 0.24f, y, Mathf.Cos(ang) * 0.17f, 0.04f, 0.1f, 0.04f, Mathf.Cos(ang) * 70f, 0, -Mathf.Sin(ang) * 70f), t, 4);
                    }
                    break;
            }
        }

        static void Boot(MeshBuilder mb, float x, int style, Color32 a, Color32 t, Color32 dark)
        {
            mb.Box(M(x, 0.07f, 0.04f, 0.2f, 0.15f, 0.3f), a, a);
            mb.Cylinder(M(x, 0.26f, 0, 0.19f, 0.3f, 0.19f), a, 8, 1f, false);
            mb.Cylinder(M(x, 0.41f, 0, 0.21f, 0.05f, 0.21f), t, 8, 1f, true);
            switch (style)
            {
                case 1: mb.Box(M(x, 0.3f, 0.09f, 0.16f, 0.3f, 0.05f), t, a); mb.Sphere(M(x, 0.47f, 0.07f, 0.12f, 0.1f, 0.08f), a, 6, 4); break;
                case 2: mb.Crystal(M(x, 0.3f, -0.1f, 0.05f, 0.16f, 0.05f, -35, 0, 0), t, Color.white); break;
                case 3: mb.Box(M(x, 0.26f, 0.1f, 0.04f, 0.26f, 0.01f), t); break;
                case 4: mb.Cone(M(x, 0.3f, -0.1f, 0.04f, 0.1f, 0.04f, -70, 0, 0), t, 4); break;
                case 7: // ventwalker greaves: a shin plate with a glowing slot, and a vent at the heel
                    mb.Box(M(x, 0.3f, 0.09f, 0.17f, 0.3f, 0.05f), dark, a);
                    mb.Box(M(x, 0.3f, 0.12f, 0.04f, 0.22f, 0.02f), new Color(1f, 0.5f, 0.12f, 0f));
                    mb.Cylinder(M(x, 0.1f, -0.13f, 0.09f, 0.1f, 0.09f, 60, 0, 0), dark, 6, 0.7f, true);
                    mb.Sphere(M(x, 0.11f, -0.17f, 0.05f, 0.05f, 0.05f), new Color(1f, 0.6f, 0.2f, 0f), 5, 4);
                    break;
                case 11: // stalker boots: soft boots bound with three wraps, a tab at the back of the calf
                    for (int i = 0; i < 3; i++) mb.Cylinder(M(x, 0.16f + i * 0.09f, 0, 0.205f, 0.03f, 0.205f), t, 8, 1f, false);
                    mb.Box(M(x, 0.42f, -0.1f, 0.07f, 0.12f, 0.02f, -12, 0, 0), dark);
                    mb.Sphere(M(x, 0.07f, 0.18f, 0.16f, 0.1f, 0.12f), dark, 6, 4);
                    break;
            }
        }

        static void AccessoryIcon(MeshBuilder mb, ItemDef d)
        {
            off = Vector3.zero;
            Color32 a = d.iconColor, t = d.trimColor;
            switch (d.equipSlot)
            {
                case EquipSlot.Amulet:
                    Band(mb, new Vector3(0, 0.62f, 0), 0.34f, 0.035f, t, 18, true);
                    if (d.gearStyle == 2) mb.Crystal(M(0, -0.2f, 0.02f, 0.32f, 0.56f, 0.24f), a, Color.white);
                    else { mb.Sphere(M(0, 0.24f, 0.02f, 0.3f, 0.34f, 0.16f), a, Color.Lerp(d.iconColor, Color.black, 0.3f), 10, 7); mb.Cylinder(M(0, 0.24f, 0, 0.36f, 0.06f, 0.36f, 90, 0, 0), t, 12, 1f, true); }
                    break;
                case EquipSlot.Bracelet:
                    Band(mb, new Vector3(0, 0.3f, 0), 0.34f, 0.1f, a, 16, false);
                    mb.Sphere(M(0, 0.3f, 0.36f, 0.16f, 0.16f, 0.1f), t, 8, 5);
                    break;
                default: // ring
                    Band(mb, new Vector3(0, 0.3f, 0), 0.24f, 0.07f, a, 14, false);
                    if (d.gearStyle == 2) mb.Crystal(M(0, 0.52f, 0, 0.12f, 0.22f, 0.12f), t, Color.white);
                    else mb.Sphere(M(0, 0.56f, 0, 0.16f, 0.14f, 0.16f), t, 8, 5);
                    break;
            }
        }

        /// <summary>Ring of boxes. vertical = hanging loop (XY plane), else lying band (XY plane viewed 3/4).</summary>
        static void Band(MeshBuilder mb, Vector3 c, float r, float th, Color32 col, int segs, bool vertical)
        {
            float seg = 2f * Mathf.PI * r / segs * 1.15f;
            for (int i = 0; i < segs; i++)
            {
                float ang = i * 360f / segs;
                float rad = ang * Mathf.Deg2Rad;
                Vector3 p = c + new Vector3(Mathf.Cos(rad) * r, Mathf.Sin(rad) * r, 0f);
                mb.Box(Matrix4x4.TRS(p, Quaternion.Euler(0, 0, ang), new Vector3(th, seg, vertical ? th : th * 1.6f)), col);
            }
        }
    }

    /// <summary>Keeps the explorer's worn gear in sync with PlayerStats (hat hidden under head gear).</summary>
    public static class ArmorVisuals
    {
        public static void Apply(CreatureRig rig, PlayerStats stats, List<GameObject> spawned)
        {
            foreach (var go in spawned) if (go != null) Object.Destroy(go);
            spawned.Clear();
            if (rig == null || stats?.gear == null) return;
            foreach (var id in stats.gear)
                if (!string.IsNullOrEmpty(id)) GearModels.Attach(ContentDB.Item(id), rig, spawned);
        }
    }
}
