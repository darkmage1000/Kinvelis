using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Wildbond
{
    /// <summary>What a projectile looks like: a kin's element orb, or something the player shot.</summary>
    public enum ShotKind { Orb, Stone, Arrow, Bolt }

    /// <summary>
    /// Pooled projectiles: kin ability orbs and the player's ranged weapons (sling stones, arrows, crossbow bolts).
    /// Plain data + a pooled visual; hit tests are sphere-vs-combatant. A player shot can lean toward the kin it was
    /// loosed at (a limited turn rate, so a running kin can still get out of the way of a bad shot).
    /// </summary>
    public class ProjectileSystem : MonoBehaviour
    {
        public static ProjectileSystem I { get; private set; }

        class Proj
        {
            public ICombatant owner;
            public Vector3 pos, vel;
            public float life, boost;
            public Transform visual;
            public float trail;
            // what it does when it lands
            public float power, statusChance, knock;
            public Element element;
            public StatusType status;
            public Color color;
            public ShotKind kind;
            public AbilityEffect effect;
            // a shot that bends toward its mark
            public CreatureActor home;
            public float turn;
        }

        readonly List<Proj> active = new List<Proj>(32);
        readonly Stack<Proj> free = new Stack<Proj>();
        readonly Dictionary<Element, Mesh> meshes = new Dictionary<Element, Mesh>();
        readonly Mesh[] shotMeshes = new Mesh[4];

        public void Init() { I = this; }

        Mesh MeshFor(Element e)
        {
            if (meshes.TryGetValue(e, out var m)) return m;
            var mb = new MeshBuilder();
            Color c = ElementInfo.Color(e);
            mb.Sphere(Matrix4x4.Scale(Vector3.one * 0.45f), c, 8, 6);
            mb.Sphere(Matrix4x4.Scale(Vector3.one * 0.25f), Color.Lerp(c, Color.white, 0.6f), 6, 4);
            m = mb.ToMesh("proj_" + e);
            meshes[e] = m;
            return m;
        }

        /// <summary>Stones, arrows and bolts point along +Z (the way they fly).</summary>
        Mesh MeshFor(ShotKind k)
        {
            if (shotMeshes[(int)k] != null) return shotMeshes[(int)k];
            var mb = new MeshBuilder();
            Color wood = new Color(0.62f, 0.46f, 0.28f), steel = new Color(0.85f, 0.87f, 0.93f), feather = new Color(0.92f, 0.9f, 0.84f);
            switch (k)
            {
                case ShotKind.Stone:
                    mb.Sphere(Matrix4x4.Scale(new Vector3(0.2f, 0.17f, 0.22f)), new Color(0.6f, 0.6f, 0.64f), new Color(0.42f, 0.42f, 0.46f), 6, 4);
                    break;
                case ShotKind.Bolt:
                    mb.Cylinder(MeshBuilder.TRS(Vector3.zero, new Vector3(90, 0, 0), new Vector3(0.05f, 0.5f, 0.05f)), wood, 5);
                    mb.Cone(MeshBuilder.TRS(new Vector3(0, 0, 0.32f), new Vector3(90, 0, 0), new Vector3(0.1f, 0.18f, 0.1f)), steel, 4);
                    mb.Box(MeshBuilder.TRS(new Vector3(0, 0, -0.2f), Vector3.zero, new Vector3(0.16f, 0.012f, 0.12f)), feather);
                    break;
                default:
                    mb.Cylinder(MeshBuilder.TRS(Vector3.zero, new Vector3(90, 0, 0), new Vector3(0.032f, 0.8f, 0.032f)), wood, 5);
                    mb.Cone(MeshBuilder.TRS(new Vector3(0, 0, 0.46f), new Vector3(90, 0, 0), new Vector3(0.08f, 0.16f, 0.08f)), steel, 4);
                    mb.Box(MeshBuilder.TRS(new Vector3(0, 0, -0.33f), Vector3.zero, new Vector3(0.14f, 0.01f, 0.14f)), feather);
                    mb.Box(MeshBuilder.TRS(new Vector3(0, 0, -0.33f), Vector3.zero, new Vector3(0.01f, 0.14f, 0.14f)), feather);
                    break;
            }
            return shotMeshes[(int)k] = mb.ToMesh("shot_" + k);
        }

        Proj Take(Mesh mesh, Vector3 origin)
        {
            var p = free.Count > 0 ? free.Pop() : new Proj();
            if (p.visual == null)
            {
                var go = new GameObject("Projectile");
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = RenderAssets.Creature;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                p.visual = go.transform;
            }
            p.visual.GetComponent<MeshFilter>().sharedMesh = mesh;
            p.visual.gameObject.SetActive(true);
            p.visual.position = origin;
            p.visual.rotation = Quaternion.identity;
            p.pos = origin;
            p.trail = 0f;
            p.home = null; p.turn = 0f;
            p.effect = AbilityEffect.None;
            return p;
        }

        public void Fire(ICombatant owner, Vector3 origin, Vector3 velocity, AbilityDef a, float boost)
        {
            var p = Take(MeshFor(a.element), origin);
            p.owner = owner; p.vel = velocity; p.life = a.range / Mathf.Max(1f, a.projectileSpeed) + 0.4f; p.boost = boost;
            p.power = a.power; p.element = a.element; p.status = a.status; p.statusChance = a.statusChance; p.color = a.color; p.knock = 4f;
            p.kind = ShotKind.Orb;
            p.effect = a.effect;
            active.Add(p);
        }

        /// <summary>A ranged weapon's shot. 'home' is the kin it was loosed at (may be null): it leans toward it by up to 'turn' degrees a second.</summary>
        public void FireShot(ICombatant owner, Vector3 origin, Vector3 velocity, float power, float range, ShotKind kind, float knock, CreatureActor home, float turn)
        {
            var p = Take(MeshFor(kind), origin);
            p.owner = owner; p.vel = velocity; p.life = range / Mathf.Max(1f, velocity.magnitude); p.boost = 1f;
            p.power = power; p.element = Element.Normal; p.status = StatusType.None; p.statusChance = 0f; p.knock = knock;
            p.color = kind == ShotKind.Stone ? new Color(0.7f, 0.7f, 0.72f) : new Color(0.95f, 0.9f, 0.75f);
            p.kind = kind;
            p.home = home; p.turn = turn;
            if (velocity.sqrMagnitude > 0.01f) p.visual.rotation = Quaternion.LookRotation(velocity);
            active.Add(p);
        }

        public void Tick(float dt)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var p = active[i];
                p.life -= dt;
                if (p.home != null)
                {
                    if (!p.home.IsAlive || !p.home.gameObject.activeInHierarchy) p.home = null;
                    else
                    {
                        Vector3 want = p.home.Position + Vector3.up * p.home.Radius - p.pos;
                        float speed = p.vel.magnitude;
                        if (want.sqrMagnitude > 0.04f && speed > 0.1f)
                            p.vel = Vector3.RotateTowards(p.vel, want.normalized * speed, p.turn * Mathf.Deg2Rad * dt, 0f);
                    }
                }
                p.pos += p.vel * dt;
                p.visual.position = p.pos;
                if (p.kind == ShotKind.Orb) p.visual.Rotate(0, dt * 720f, 0);
                else if (p.kind == ShotKind.Stone) p.visual.Rotate(dt * 900f, 0, 0);
                else if (p.vel.sqrMagnitude > 0.01f) p.visual.rotation = Quaternion.LookRotation(p.vel);
                p.trail -= dt;
                if (p.trail <= 0f)
                {
                    if (p.kind == ShotKind.Orb) { p.trail = 0.03f; Fx.Puff(p.pos, p.color, 0.25f, 0.25f); }
                    else { p.trail = 0.05f; Fx.Puff(p.pos - p.vel.normalized * 0.3f, new Color(1f, 1f, 1f, 0.5f), 0.1f, 0.16f); }
                }

                bool done = p.life <= 0f || p.pos.y < Ground.Height(p.pos.x, p.pos.z) || Caves.Solid(p.pos);
                if (!done)
                {
                    var hit = FindHit(p);
                    if (hit != null)
                    {
                        Vector3 kd = p.vel; kd.y = 0;
                        CombatSystem.Apply(p.owner, hit, p.power, p.element, p.status, p.statusChance, kd.normalized * p.knock, p.boost, p.effect);
                        if (p.kind != ShotKind.Orb && p.owner is PlayerController) GameManager.I.HitStop(0.03f);
                        done = true;
                    }
                }
                if (done)
                {
                    if (p.kind == ShotKind.Orb) Fx.Burst(p.pos, p.color, 8, 3f, 0.25f, 0.35f, true);
                    else Fx.Burst(p.pos, p.color, 5, 2f, 0.14f, 0.25f, true);
                    p.visual.gameObject.SetActive(false);
                    p.owner = null; p.home = null;
                    active.RemoveAt(i);
                    free.Push(p);
                }
            }
        }

        ICombatant FindHit(Proj p)
        {
            var all = CreatureRegistry.All;
            Team ot = p.owner.Team;
            for (int i = 0; i < all.Count; i++)
            {
                var a = all[i];
                if (a.Team == ot || !a.IsAlive || a.Mode == CreatureMode.Worker) continue;
                Vector3 c = a.Position + Vector3.up * a.Radius;
                float r = a.Radius + 0.35f;
                if ((c - p.pos).sqrMagnitude < r * r) return a;
            }
            // a Decoy's copy soaks up shots meant for the real one
            var decoys = Decoy.Active;
            for (int i = 0; i < decoys.Count; i++)
            {
                var d = decoys[i];
                if (d.Team == ot || !d.IsAlive) continue;
                Vector3 c = d.Position + Vector3.up * d.Radius;
                float r = d.Radius + 0.35f;
                if ((c - p.pos).sqrMagnitude < r * r) return d;
            }
            var pl = GameManager.I.Player;
            if (ot == Team.Wild && pl.IsAlive)
            {
                Vector3 c = pl.Position + Vector3.up * 0.9f;
                if ((c - p.pos).sqrMagnitude < 0.9f * 0.9f) return pl;
            }
            return null;
        }

        public void ClearAll()
        {
            foreach (var p in active) { p.visual.gameObject.SetActive(false); p.owner = null; p.home = null; free.Push(p); }
            active.Clear();
        }
    }
}
