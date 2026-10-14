using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Wildbond
{
    /// <summary>
    /// The copy a Decoy skill leaves behind (AbilityEffect.Decoy): a still image of the kin that enemies believe in for
    /// a few seconds. It is a real ICombatant so melee, dashes and shots land on it and wear it down, but it never acts.
    /// Everything that was fooled goes back to its old target when the copy pops.
    /// </summary>
    public class Decoy : ICombatant
    {
        static readonly List<Decoy> active = new List<Decoy>();
        static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        public static IReadOnlyList<Decoy> Active => active;

        struct Fooled { public CreatureActor who; public ICombatant was; }

        GameObject go;
        readonly List<Fooled> fooled = new List<Fooled>();
        float until, hp, fx, def, radius;
        int level;
        bool ended;
        Element e1, e2;
        Team team;
        Color glow;

        public Vector3 Position => go != null ? go.transform.position : Vector3.zero;
        public float Radius => radius;
        public bool IsAlive => go != null && hp > 0f && Time.time < until;
        public Team Team => team;
        public int Level => level;
        public float AttackStat => 1f;
        public float DefenseStat => def;
        public Element Element1 => e1;
        public Element Element2 => e2;

        public void ReceiveHit(in HitInfo hit)
        {
            if (go == null) return;
            hp -= hit.damage;
            Fx.Burst(Position + Vector3.up * radius, glow, 6, 2.5f, 0.2f, 0.35f, true);
            if (hp <= 0f) End();
        }

        public static Decoy Spawn(CreatureActor owner, Vector3 pos, Quaternion rot, float seconds)
        {
            var d = new Decoy();
            string key = owner.Data.ModelKey;
            if (!meshes.TryGetValue(key, out var mesh))
            {
                mesh = ModelBuilder.BuildStatic(owner.Data.Parts, owner.Data.Palette, "decoy_" + key);
                meshes[key] = mesh;
            }
            d.go = new GameObject("Decoy");
            d.go.transform.SetPositionAndRotation(pos, rot);
            d.go.transform.localScale = Vector3.one * (owner.Sp.size * owner.Data.SizeScale);
            d.go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = d.go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.Creature;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            d.until = Time.time + seconds;
            d.hp = owner.MaxHp * SkillEffects.DecoyHp;
            d.def = owner.DefenseStat;
            d.radius = owner.BodyRadius;
            d.level = owner.Level;
            d.e1 = owner.Element1; d.e2 = owner.Element2;
            d.team = owner.Team;
            d.glow = owner.Data.Palette.glow;
            active.Add(d);
            return d;
        }

        /// <summary>This kin now believes the copy is the real thing.</summary>
        public void Fool(CreatureActor who, ICombatant was)
        {
            fooled.Add(new Fooled { who = who, was = was });
            who.Distract(was, this);
        }

        public void End()
        {
            if (ended) return;
            ended = true;
            if (go != null)   // (already gone if the scene was reloaded under it)
            {
                Vector3 p = Position + Vector3.up * radius;
                Fx.Burst(p, glow, 16, 3.5f, 0.3f, 0.5f, true);
                Fx.Burst(p, new Color(1f, 1f, 1f, 0.6f), 8, 2f, 0.4f, 0.4f);
                SynthAudio.Play(Sfx.Whoosh, 1.6f, 0.4f);
                Object.Destroy(go);
            }
            go = null;
            // they blink and look for whoever they were really after
            foreach (var f in fooled)
                if (f.who != null && f.who.IsAlive) f.who.Distract(this, f.was != null && f.was.IsAlive ? f.was : null);
            fooled.Clear();
            active.Remove(this);
        }

        public static void TickAll(float dt)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var d = active[i];
                if (d.go == null || Time.time >= d.until || d.hp <= 0f) { d.End(); continue; }
                d.fx -= dt;
                if (d.fx > 0f) continue;
                d.fx = 0.14f;
                // a shimmer the real one doesn't have, for anyone looking closely
                Fx.Mote(d.Position + Vector3.up * (d.radius * 1.2f) + Random.insideUnitSphere * d.radius, Vector3.up * 0.5f, d.glow, 0.1f, 0.5f, true);
            }
        }

        public static void ClearAll()
        {
            for (int i = active.Count - 1; i >= 0; i--) active[i].End();
        }
    }
}
