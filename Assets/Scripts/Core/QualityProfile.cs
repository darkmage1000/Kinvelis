using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Wildbond
{
    public enum QualityTier { Low = 0, Medium = 1, High = 2 }

    /// <summary>Scalable graphics/simulation budget. Everything expensive reads from Current.</summary>
    [System.Serializable]
    public class QualityProfile
    {
        public QualityTier tier;
        public int viewRadiusChunks;      // terrain streaming radius (64m chunks)
        public int propRadiusChunks;      // gatherables/trees drawn + interactive
        public int decorRadiusChunks;     // grass/flowers
        public float decorDensity;        // 0..1
        public float renderScale;
        public bool shadows;
        public float shadowDistance;
        public int wildCreatureCap;
        public float creatureFullSimRange; // full AI + animation
        public float creatureSimRange;     // reduced-rate AI beyond this until despawn
        public int targetFps;
        public float fogEnd;
        public int particleBudget;

        public static QualityProfile Current { get; private set; } = Make(QualityTier.Medium);

        public static QualityProfile Make(QualityTier t)
        {
            switch (t)
            {
                case QualityTier.Low:
                    return new QualityProfile
                    {
                        tier = t, viewRadiusChunks = 4, propRadiusChunks = 2, decorRadiusChunks = 1, decorDensity = 0.35f,
                        renderScale = 0.7f, shadows = false, shadowDistance = 0f, wildCreatureCap = 22,
                        creatureFullSimRange = 30f, creatureSimRange = 75f, targetFps = 30, fogEnd = 230f, particleBudget = 120
                    };
                case QualityTier.High:
                    return new QualityProfile
                    {
                        tier = t, viewRadiusChunks = 7, propRadiusChunks = 4, decorRadiusChunks = 2, decorDensity = 1f,
                        renderScale = 1f, shadows = true, shadowDistance = 60f, wildCreatureCap = 48,
                        creatureFullSimRange = 45f, creatureSimRange = 110f, targetFps = 60, fogEnd = 420f, particleBudget = 400
                    };
                default:
                    return new QualityProfile
                    {
                        tier = QualityTier.Medium, viewRadiusChunks = 5, propRadiusChunks = 3, decorRadiusChunks = 1, decorDensity = 0.7f,
                        renderScale = 0.85f, shadows = true, shadowDistance = 40f, wildCreatureCap = 34,
                        creatureFullSimRange = 38f, creatureSimRange = 90f, targetFps = 60, fogEnd = 310f, particleBudget = 250
                    };
            }
        }

        /// <summary>Applies to URP asset + frame rate. Systems poll Current for the rest.</summary>
        public static void Apply(QualityTier t)
        {
            Current = Make(t);
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null)
            {
                urp.renderScale = Current.renderScale;
                urp.shadowDistance = Mathf.Max(1f, Current.shadowDistance);
            }
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = Current.targetFps;
            GameEvents.RaiseQualityChanged();
        }

        public static QualityTier SuggestTier()
        {
            int mem = SystemInfo.systemMemorySize;
            int cores = SystemInfo.processorCount;
            if (Application.isMobilePlatform)
            {
                if (mem < 3500 || cores < 6) return QualityTier.Low;
                if (mem < 7000) return QualityTier.Medium;
                return QualityTier.High;
            }
            return QualityTier.High;
        }
    }
}
