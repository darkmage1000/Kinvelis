using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Shared materials. Loaded from Resources/Materials (created by Wildbond > Setup Project so
    /// shaders are guaranteed to be included in player builds); falls back to Shader.Find.
    /// </summary>
    public static class RenderAssets
    {
        public static Material Terrain, Props, Creature, Building, Water, Sky, Particle, ParticleAdd, Ghost;

        public static void Init()
        {
            Terrain = Load("WB_Terrain", "Wildbond/VertexLit");
            Props = Load("WB_Props", "Wildbond/VertexLit");
            Props.enableInstancing = true;
            Creature = Load("WB_Creature", "Wildbond/VertexLit");
            if (Creature.HasProperty("_Rim")) Creature.SetFloat("_Rim", 0.4f);
            if (Creature.HasProperty("_VertexGlow")) Creature.SetFloat("_VertexGlow", 1.15f);
            Building = Load("WB_Building", "Wildbond/VertexLit");
            Ghost = Load("WB_Ghost", "Wildbond/VertexLit");
            if (Ghost.HasProperty("_Emission")) Ghost.SetFloat("_Emission", 0.6f);
            Water = Load("WB_Water", "Wildbond/Water");
            Sky = Load("WB_Sky", "Wildbond/Sky");
            Particle = Load("WB_Particle", "Wildbond/Particle");
            ParticleAdd = Load("WB_ParticleAdd", "Wildbond/Particle");
            var soft = SoftDot();
            SetupParticle(Particle, false, soft);
            SetupParticle(ParticleAdd, true, soft);
        }

        /// <summary>Configures a URP particle material as transparent (runtime-safe, no editor GUI needed).</summary>
        public static void SetupParticle(Material m, bool additive, Texture tex)
        {
            if (tex != null && m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", additive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        static Texture2D SoftDot()
        {
            const int s = 32;
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "softdot" };
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = (x + 0.5f) / s * 2f - 1f, dy = (y + 0.5f) / s * 2f - 1f;
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                a = a * a * (3f - 2f * a);
                px[y * s + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            t.SetPixels32(px);
            t.Apply(false, true);
            return t;
        }

        static Material Load(string name, string shader)
        {
            var m = Resources.Load<Material>("Materials/" + name);
            if (m != null) return new Material(m) { name = name };
            var s = Shader.Find(shader);
            if (s == null)
            {
                Debug.LogError($"[RenderAssets] Shader '{shader}' not found - run Wildbond > Setup Project.");
                s = Shader.Find("Universal Render Pipeline/Unlit");
            }
            return new Material(s) { name = name };
        }
    }
}
