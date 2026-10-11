using UnityEngine;
using UnityEngine.Rendering;

namespace Wildbond
{
    /// <summary>
    /// All one-shot particle effects go through two shared world-space ParticleSystems using Emit()
    /// (no per-effect GameObjects => zero instantiation cost, 2 draw calls total).
    /// </summary>
    public static class Fx
    {
        static ParticleSystem alphaPs, addPs;
        static ParticleSystem.EmitParams ep;

        public static void Init(Transform parent)
        {
            alphaPs = Make("FX_Alpha", RenderAssets.Particle, parent);
            addPs = Make("FX_Add", RenderAssets.ParticleAdd, parent);
        }

        static ParticleSystem Make(string name, Material mat, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 1500;
            main.gravityModifier = 0f;
            var em = ps.emission; em.enabled = false;
            var shape = ps.shape; shape.enabled = false;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f));
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();
            return ps;
        }

        static bool Budget(ParticleSystem ps, int count) =>
            ps != null && ps.particleCount + count <= QualityProfile.Current.particleBudget * 3;

        /// <summary>Radial burst of particles.</summary>
        public static void Burst(Vector3 pos, Color color, int count, float speed = 4f, float size = 0.25f, float life = 0.6f, bool additive = false, float gravity = 0f)
        {
            var ps = additive ? addPs : alphaPs;
            if (!Budget(ps, count)) return;
            for (int i = 0; i < count; i++)
            {
                Vector3 dir = Random.onUnitSphere;
                dir.y = Mathf.Abs(dir.y) * 0.8f + 0.2f;
                ep.position = pos;
                ep.velocity = dir * speed * Random.Range(0.5f, 1.1f) + Vector3.down * gravity * 0f;
                ep.startColor = color;
                ep.startSize = size * Random.Range(0.6f, 1.2f);
                ep.startLifetime = life * Random.Range(0.7f, 1.2f);
                ps.Emit(ep, 1);
            }
        }

        /// <summary>Flat ring (AoE telegraph / impact).</summary>
        public static void Ring(Vector3 pos, Color color, float radius, int count = 24, bool additive = true)
        {
            var ps = additive ? addPs : alphaPs;
            if (!Budget(ps, count)) return;
            for (int i = 0; i < count; i++)
            {
                float a = i / (float)count * Mathf.PI * 2f;
                var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                ep.position = pos + d * radius * 0.2f + Vector3.up * 0.2f;
                ep.velocity = d * radius * 2.2f;
                ep.startColor = color;
                ep.startSize = 0.35f;
                ep.startLifetime = 0.4f;
                ps.Emit(ep, 1);
            }
        }

        /// <summary>Rising sparkles (level up, shimmer, capture success).</summary>
        public static void Sparkle(Vector3 pos, Color color, int count = 10, float radius = 0.6f)
        {
            if (!Budget(addPs, count)) return;
            for (int i = 0; i < count; i++)
            {
                var o = Random.insideUnitSphere * radius;
                ep.position = pos + o;
                ep.velocity = Vector3.up * Random.Range(0.6f, 2f) + o;
                ep.startColor = color;
                ep.startSize = Random.Range(0.08f, 0.2f);
                ep.startLifetime = Random.Range(0.6f, 1.2f);
                addPs.Emit(ep, 1);
            }
        }

        /// <summary>One particle with an explicit velocity (species auras: embers, snow, bubbles...).</summary>
        public static void Mote(Vector3 pos, Vector3 velocity, Color color, float size, float life, bool additive)
        {
            var ps = additive ? addPs : alphaPs;
            if (!Budget(ps, 1)) return;
            ep.position = pos;
            ep.velocity = velocity;
            ep.startColor = color;
            ep.startSize = size;
            ep.startLifetime = life;
            ps.Emit(ep, 1);
        }

        /// <summary>Single trail puff (projectile trails).</summary>
        public static void Puff(Vector3 pos, Color color, float size = 0.2f, float life = 0.3f)
        {
            if (!Budget(addPs, 1)) return;
            ep.position = pos;
            ep.velocity = Random.insideUnitSphere * 0.3f;
            ep.startColor = color;
            ep.startSize = size;
            ep.startLifetime = life;
            addPs.Emit(ep, 1);
        }

        /// <summary>Debris chunks for gathering (alpha-blended).</summary>
        public static void Debris(Vector3 pos, Color color, int count = 6)
        {
            if (!Budget(alphaPs, count)) return;
            for (int i = 0; i < count; i++)
            {
                var d = Random.onUnitSphere; d.y = Mathf.Abs(d.y) + 0.5f;
                ep.position = pos;
                ep.velocity = d * Random.Range(2f, 4f);
                ep.startColor = color;
                ep.startSize = Random.Range(0.12f, 0.25f);
                ep.startLifetime = Random.Range(0.35f, 0.6f);
                alphaPs.Emit(ep, 1);
            }
        }
    }
}
