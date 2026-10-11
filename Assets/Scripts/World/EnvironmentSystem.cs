using UnityEngine;
using UnityEngine.Rendering;

namespace Wildbond
{
    public enum WeatherType { Clear, Cloudy, Rain, Heatwave, Ashfall, Snow, Blizzard }

    /// <summary>Day/night cycle, regional weather, sky dome, fog, ambient light and the water plane.</summary>
    public class EnvironmentSystem : MonoBehaviour
    {
        public static EnvironmentSystem I { get; private set; }

        [Header("Time")]
        [SerializeField] float dayLengthSeconds = 1200f;
        [Range(0, 1)] public float timeOfDay = 0.3f; // 0 = midnight, 0.25 sunrise, 0.5 noon, 0.75 sunset

        [Header("Weather")]
        [SerializeField] float weatherMinDuration = 180f;
        [SerializeField] float weatherMaxDuration = 420f;

        public WeatherType Weather { get; private set; } = WeatherType.Clear;
        public BiomeId CurrentBiome { get; private set; } = BiomeId.Meadow;
        public bool IsNight => timeOfDay < 0.23f || timeOfDay > 0.78f;
        public float DayLength => dayLengthSeconds;
        public void SetDayLength(float seconds) => dayLengthSeconds = Mathf.Clamp(seconds, 120f, 7200f);

        Light sun;
        Transform skyDome, water;
        Material skyMat;
        ParticleSystem weatherFx;
        ParticleSystem.EmissionModule emission;
        ParticleSystem.MainModule fxMain;
        ParticleSystemRenderer fxRenderer;
        float weatherTimer;
        float weatherBlend;              // 0..1 visual intensity of current weather
        Color fogColor, zenith;
        float fogEndMul = 1f, lightMul = 1f;
        float glowBlend;
        BiomeWeights biomeW;
        float outerBlend;
        BiomeId outerBiome = BiomeId.Isles;

        struct BiomeWeights { public float m, b, p; }

        public void Init(Light sunLight)
        {
            I = this;
            sun = sunLight;
            BuildSky();
            BuildWater();
            BuildWeatherFx();
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            weatherTimer = 60f;
        }

        void BuildSky()
        {
            var mb = new MeshBuilder();
            mb.Sphere(Matrix4x4.Scale(Vector3.one * 2f), new Color32(255, 255, 255, 255), 24, 12);
            var go = new GameObject("SkyDome");
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("sky");
            var mr = go.AddComponent<MeshRenderer>();
            skyMat = RenderAssets.Sky;
            mr.sharedMaterial = skyMat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            skyDome = go.transform;
        }

        void BuildWater()
        {
            // 640m grid, 80x80 quads, follows the camera snapped to 8m so waves don't swim
            const int n = 80; const float size = 640f;
            var verts = new Vector3[(n + 1) * (n + 1)];
            var tris = new int[n * n * 6];
            for (int z = 0; z <= n; z++)
            for (int x = 0; x <= n; x++)
                verts[z * (n + 1) + x] = new Vector3(x / (float)n * size - size * 0.5f, 0, z / (float)n * size - size * 0.5f);
            int t = 0;
            for (int z = 0; z < n; z++)
            for (int x = 0; x < n; x++)
            {
                int i0 = z * (n + 1) + x, i1 = i0 + 1, i2 = i0 + n + 1, i3 = i2 + 1;
                tris[t++] = i0; tris[t++] = i2; tris[t++] = i1;
                tris[t++] = i1; tris[t++] = i2; tris[t++] = i3;
            }
            var mesh = new Mesh { name = "water", indexFormat = IndexFormat.UInt32 };
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(size, 4f, size));
            var go = new GameObject("Water");
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RenderAssets.Water;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            water = go.transform;
        }

        void BuildWeatherFx()
        {
            var go = new GameObject("WeatherFX");
            go.transform.SetParent(transform, false);
            weatherFx = go.AddComponent<ParticleSystem>();
            weatherFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            fxMain = weatherFx.main;
            fxMain.loop = true;
            fxMain.startLifetime = 2.2f;
            fxMain.startSpeed = 0f;
            fxMain.maxParticles = 700;
            fxMain.simulationSpace = ParticleSystemSimulationSpace.World;
            fxMain.startSize = 0.08f;
            var shape = weatherFx.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(36f, 1f, 36f);
            shape.position = new Vector3(0, 12f, 0);
            emission = weatherFx.emission;
            emission.rateOverTime = 0f;
            var vel = weatherFx.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            fxRenderer = go.GetComponent<ParticleSystemRenderer>();
            fxRenderer.sharedMaterial = RenderAssets.Particle;
            fxRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            fxRenderer.velocityScale = 0.05f;
            fxRenderer.lengthScale = 1f;
            weatherFx.Play();
        }

        // ============================================================ TICK
        // ============================================================ INDOOR (caves)
        public bool Indoor { get; private set; }
        Color indoorGlow = Color.white;

        /// <summary>Caves: no sky, water or weather; dim tinted light and close black fog.</summary>
        public void SetIndoor(bool on, Color glow)
        {
            Indoor = on;
            indoorGlow = glow;
            if (skyDome != null) skyDome.gameObject.SetActive(!on);
            if (water != null) water.gameObject.SetActive(!on);
            if (on && weatherFx != null) { emission.rateOverTime = 0f; weatherFx.Clear(); }
            var cam = Camera.main;
            if (cam != null) cam.backgroundColor = on ? new Color(0.01f, 0.01f, 0.02f) : new Color(0.6f, 0.75f, 0.9f);
        }

        void UpdateIndoorLighting()
        {
            sun.transform.rotation = Quaternion.Euler(62f, 35f, 0f);
            sun.color = Color.Lerp(new Color(1f, 0.92f, 0.82f), indoorGlow, 0.25f);
            sun.intensity = 0.55f;
            sun.shadows = LightShadows.None;
            Color amb = Color.Lerp(new Color(0.16f, 0.15f, 0.2f), indoorGlow, 0.18f);
            RenderSettings.ambientSkyColor = amb;
            RenderSettings.ambientEquatorColor = amb * 0.8f;
            RenderSettings.ambientGroundColor = amb * 0.5f;
            var sh = new SphericalHarmonicsL2();
            sh.AddAmbientLight(amb * 0.9f);
            RenderSettings.ambientProbe = sh;
            RenderSettings.fogColor = new Color(0.01f, 0.01f, 0.02f);
            RenderSettings.fogStartDistance = 14f;
            RenderSettings.fogEndDistance = 52f;
        }

        public void Tick(Vector3 focus, Camera cam, float dt)
        {
            timeOfDay = Mathf.Repeat(timeOfDay + dt / dayLengthSeconds, 1f);
            if (Indoor) { UpdateIndoorLighting(); return; }

            WorldGen.Weights(focus.x, focus.z, out biomeW.m, out biomeW.b, out biomeW.p);
            var newBiome = WorldGen.Biome(focus.x, focus.z);
            // how far into an outer land we are (0 on the original island): blends its sky, fog and light in
            float ow = 0f;
            if ((int)newBiome > (int)BiomeId.Peaks && WorldGen.Outer(focus.x, focus.z, out var ob, out float oww, out _) && ob == newBiome) ow = Mathf.Clamp01(oww * 1.6f);
            outerBlend = Mathf.MoveTowards(outerBlend, ow, dt * 0.5f);
            if (ow > 0f) outerBiome = newBiome;
            if (newBiome != CurrentBiome)
            {
                CurrentBiome = newBiome;
                if (!Allowed(newBiome, Weather)) { RollWeather(); }
                GameEvents.Say(BiomeInfo.Name(newBiome), BiomeInfo.Tint(newBiome));
            }

            weatherTimer -= dt;
            if (weatherTimer <= 0f) RollWeather();
            weatherBlend = Mathf.MoveTowards(weatherBlend, 1f, dt * 0.15f);

            UpdateLighting(focus);
            UpdateWeatherFx(cam, focus);

            if (cam != null)
            {
                skyDome.position = cam.transform.position;
                skyDome.localScale = Vector3.one * (cam.farClipPlane * 0.45f);
            }
            water.position = new Vector3(Mathf.Round(focus.x / 8f) * 8f, Tides.Surface(focus.x, focus.z), Mathf.Round(focus.z / 8f) * 8f);
        }

        void RollWeather()
        {
            weatherTimer = Random.Range(weatherMinDuration, weatherMaxDuration);
            float r = Random.value;
            WeatherType w;
            switch (CurrentBiome)
            {
                case BiomeId.Badlands: w = r < 0.5f ? WeatherType.Clear : (r < 0.78f ? WeatherType.Heatwave : WeatherType.Ashfall); break;
                case BiomeId.Desert: w = r < 0.55f ? WeatherType.Clear : WeatherType.Heatwave; break;
                case BiomeId.Blight: w = r < 0.4f ? WeatherType.Clear : (r < 0.75f ? WeatherType.Cloudy : WeatherType.Ashfall); break;
                case BiomeId.Swamp: w = r < 0.35f ? WeatherType.Clear : (r < 0.6f ? WeatherType.Cloudy : WeatherType.Rain); break;
                case BiomeId.Glow: w = r < 0.7f ? WeatherType.Clear : WeatherType.Cloudy; break;
                case BiomeId.Peaks: w = r < 0.4f ? WeatherType.Clear : (r < 0.62f ? WeatherType.Cloudy : (r < 0.88f ? WeatherType.Snow : WeatherType.Blizzard)); break;
                default: w = r < 0.55f ? WeatherType.Clear : (r < 0.78f ? WeatherType.Cloudy : WeatherType.Rain); break;
            }
            if (w != Weather)
            {
                Weather = w;
                weatherBlend = 0f;
                if (w != WeatherType.Clear) GameEvents.Say("Weather: " + WeatherName(w), new Color(0.8f, 0.9f, 1f));
            }
        }

        static bool Allowed(BiomeId b, WeatherType w)
        {
            switch (w)
            {
                case WeatherType.Clear: return true;
                case WeatherType.Cloudy: return b != BiomeId.Badlands && b != BiomeId.Desert;
                case WeatherType.Rain: return b == BiomeId.Meadow || b == BiomeId.Swamp || b == BiomeId.Isles;
                case WeatherType.Heatwave: return b == BiomeId.Badlands || b == BiomeId.Desert;
                case WeatherType.Ashfall: return b == BiomeId.Badlands || b == BiomeId.Blight;
                default: return b == BiomeId.Peaks;
            }
        }

        public static string WeatherName(WeatherType w)
        {
            switch (w)
            {
                case WeatherType.Heatwave: return "Heat Shimmer";
                case WeatherType.Ashfall: return "Ashfall";
                case WeatherType.Blizzard: return "Blizzard";
                default: return w.ToString();
            }
        }

        void UpdateLighting(Vector3 focus)
        {
            // sun path: rises east (+x), sets west
            float ang = (timeOfDay - 0.25f) * 360f;
            Vector3 sunDir = Quaternion.Euler(0f, 30f, 0f) * (Quaternion.AngleAxis(ang, Vector3.forward) * Vector3.right);
            float elev = sunDir.y;
            float day = Mathf.Clamp01(elev * 3f + 0.25f);         // 0 night .. 1 day
            float golden = Mathf.Clamp01(1f - Mathf.Abs(elev) * 4f) * (elev > -0.15f ? 1f : 0f);
            // Glowstone Pendant: nights brighten around the player
            var gpl = GameManager.I != null ? GameManager.I.Player : null;
            bool glowOn = gpl != null && gpl.Stats != null && gpl.Stats.HasEffect(UniqueEffect.Glowstone);
            glowBlend = Mathf.MoveTowards(glowBlend, glowOn ? 1f : 0f, Time.unscaledDeltaTime * 0.8f);
            float nightGlow = glowBlend * (1f - day);

            // biome-tinted palettes
            Color dayZen = Blend(new Color(0.33f, 0.6f, 0.98f), new Color(0.45f, 0.62f, 0.9f), new Color(0.42f, 0.6f, 0.92f));
            Color dayHor = Blend(new Color(0.78f, 0.9f, 1f), new Color(1f, 0.86f, 0.7f), new Color(0.85f, 0.92f, 1f));
            Color nightZen = new Color(0.03f, 0.05f, 0.14f), nightHor = new Color(0.1f, 0.13f, 0.25f);
            Color goldHor = new Color(1f, 0.6f, 0.4f);

            float wDark = 0f; fogEndMul = 1f; lightMul = 1f;
            Color weatherTint = Color.clear;
            switch (Weather)
            {
                case WeatherType.Cloudy: wDark = 0.25f; fogEndMul = 0.85f; lightMul = 0.75f; weatherTint = new Color(0.7f, 0.74f, 0.8f, 0.4f); break;
                case WeatherType.Rain: wDark = 0.4f; fogEndMul = 0.6f; lightMul = 0.55f; weatherTint = new Color(0.55f, 0.62f, 0.7f, 0.6f); break;
                case WeatherType.Heatwave: fogEndMul = 0.8f; lightMul = 1.1f; weatherTint = new Color(1f, 0.8f, 0.55f, 0.35f); break;
                case WeatherType.Ashfall: wDark = 0.3f; fogEndMul = 0.55f; lightMul = 0.65f; weatherTint = new Color(0.55f, 0.42f, 0.38f, 0.6f); break;
                case WeatherType.Snow: wDark = 0.15f; fogEndMul = 0.65f; lightMul = 0.8f; weatherTint = new Color(0.85f, 0.9f, 0.97f, 0.5f); break;
                case WeatherType.Blizzard: wDark = 0.3f; fogEndMul = 0.32f; lightMul = 0.6f; weatherTint = new Color(0.88f, 0.92f, 0.98f, 0.8f); break;
            }
            float wb = weatherBlend;
            fogEndMul = Mathf.Lerp(1f, fogEndMul, wb);
            lightMul = Mathf.Lerp(1f, lightMul, wb);

            zenith = Color.Lerp(nightZen, dayZen, day);
            fogColor = Color.Lerp(nightHor, dayHor, day);
            fogColor = Color.Lerp(fogColor, goldHor, golden * 0.55f);
            if (weatherTint.a > 0f)
            {
                zenith = Color.Lerp(zenith, weatherTint * Mathf.Lerp(0.25f, 1f, day), weatherTint.a * wb);
                fogColor = Color.Lerp(fogColor, weatherTint * Mathf.Lerp(0.25f, 1f, day), weatherTint.a * wb);
            }

            // outer lands bring their own air
            if (outerBlend > 0.001f)
            {
                Color oZen, oFog; float oFogMul = 1f, oLight = 1f, oFloor = 0.3f;
                switch (outerBiome)
                {
                    case BiomeId.Swamp: oZen = new Color(0.4f, 0.52f, 0.46f); oFog = new Color(0.52f, 0.6f, 0.46f); oFogMul = 0.6f; oLight = 0.8f; break;
                    case BiomeId.Blight: oZen = new Color(0.4f, 0.34f, 0.5f); oFog = new Color(0.52f, 0.62f, 0.34f); oFogMul = 0.5f; oLight = 0.72f; break;
                    case BiomeId.Desert: oZen = new Color(0.42f, 0.66f, 0.98f); oFog = new Color(1f, 0.9f, 0.7f); oLight = 1.12f; break;
                    case BiomeId.Glow: oZen = new Color(0.1f, 0.2f, 0.34f); oFog = new Color(0.16f, 0.42f, 0.46f); oFogMul = 0.7f; oLight = 0.55f; oFloor = 0.85f; break;
                    default: oZen = new Color(0.28f, 0.62f, 1f); oFog = new Color(0.7f, 0.92f, 1f); oFogMul = 1.1f; oLight = 1.05f; break;   // Isles
                }
                float oK = outerBlend * 0.75f, oDim = Mathf.Lerp(oFloor, 1f, day);
                zenith = Color.Lerp(zenith, oZen * oDim, oK);
                fogColor = Color.Lerp(fogColor, oFog * oDim, oK);
                fogEndMul *= Mathf.Lerp(1f, oFogMul, outerBlend);
                lightMul *= Mathf.Lerp(1f, oLight, outerBlend);
            }

            // directional light: sun by day, cool moon by night
            bool isDayLight = elev > -0.05f;
            Vector3 lightDir = isDayLight ? sunDir : -sunDir;
            sun.transform.rotation = Quaternion.LookRotation(-lightDir);
            Color sunCol = Color.Lerp(new Color(1f, 0.62f, 0.4f), new Color(1f, 0.96f, 0.88f), Mathf.Clamp01(elev * 2.5f));
            sun.color = isDayLight ? sunCol : new Color(0.55f, 0.65f, 1f);
            sun.intensity = (isDayLight ? Mathf.Lerp(0.25f, 1.25f, Mathf.Clamp01(elev * 3f)) : 0.28f) * lightMul * (1f - wDark * wb * 0.5f);
            sun.intensity += nightGlow * 0.35f;
            sun.shadows = QualityProfile.Current.shadows ? LightShadows.Soft : LightShadows.None;
            sun.shadowStrength = isDayLight ? 0.75f : 0.4f;

            // ambient
            Color ambSky = Color.Lerp(new Color(0.16f, 0.2f, 0.35f), Color.Lerp(zenith, Color.white, 0.35f), day);
            Color ambEq = Color.Lerp(new Color(0.12f, 0.14f, 0.24f), fogColor, 0.8f) * Mathf.Lerp(0.8f, 0.9f, day);
            Color ambGround = Color.Lerp(new Color(0.08f, 0.08f, 0.12f), new Color(0.42f, 0.4f, 0.34f), day);
            float glowAmb = 1f + nightGlow * 1.6f;
            ambSky *= glowAmb; ambEq *= glowAmb; ambGround *= glowAmb;
            RenderSettings.ambientSkyColor = ambSky * 0.9f;
            RenderSettings.ambientEquatorColor = ambEq * 0.75f;
            RenderSettings.ambientGroundColor = ambGround * 0.6f;
            var sh = new SphericalHarmonicsL2();
            sh.AddAmbientLight(ambEq * 0.7f);
            sh.AddDirectionalLight(Vector3.up, (ambSky - ambEq * 0.5f) * 0.6f, 1f);
            sh.AddDirectionalLight(Vector3.down, ambGround * 0.35f, 1f);
            RenderSettings.ambientProbe = sh;

            // fog
            float fogEnd = QualityProfile.Current.fogEnd * fogEndMul * Nightfall.FogMul;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogStartDistance = fogEnd * 0.3f;
            RenderSettings.fogEndDistance = fogEnd;

            if (skyMat != null)
            {
                skyMat.SetColor("_ZenithColor", zenith);
                skyMat.SetColor("_HorizonColor", fogColor);
                skyMat.SetColor("_SunColor", Color.Lerp(new Color(1f, 0.5f, 0.3f), new Color(1f, 0.95f, 0.8f), Mathf.Clamp01(elev * 3f)) * (1f - wDark * wb));
                skyMat.SetVector("_SunDir", sunDir);
                skyMat.SetFloat("_Stars", Mathf.Clamp01(1f - day * 1.4f) * (1f - wDark * wb));
            }
            var wm = RenderAssets.Water;
            if (wm != null && wm.HasProperty("_SkyColor")) wm.SetColor("_SkyColor", Color.Lerp(fogColor, zenith, 0.4f));
        }

        Color Blend(Color meadow, Color bad, Color peaks) => meadow * biomeW.m + bad * biomeW.b + peaks * biomeW.p;

        void UpdateWeatherFx(Camera cam, Vector3 focus)
        {
            if (weatherFx == null) return;
            weatherFx.transform.position = focus + (cam != null ? cam.transform.forward * 6f : Vector3.zero);
            float rate = 0f;
            var vel = weatherFx.velocityOverLifetime;
            Color c = Color.white;
            float size = 0.08f;
            ParticleSystemRenderMode mode = ParticleSystemRenderMode.Billboard;
            Vector3 v = Vector3.zero;
            float budget = QualityProfile.Current.particleBudget / 250f;
            switch (Weather)
            {
                case WeatherType.Rain: rate = 260f; v = new Vector3(1.5f, -22f, 0.5f); c = new Color(0.7f, 0.8f, 1f, 0.55f); size = 0.05f; mode = ParticleSystemRenderMode.Stretch; break;
                case WeatherType.Ashfall: rate = 140f; v = new Vector3(0.8f, -1.6f, 0.4f); c = new Color(0.45f, 0.4f, 0.38f, 0.8f); size = 0.12f; break;
                case WeatherType.Heatwave: rate = 20f; v = new Vector3(0f, 1.2f, 0f); c = new Color(1f, 0.6f, 0.3f, 0.6f); size = 0.07f; break;
                case WeatherType.Snow: rate = 160f; v = new Vector3(0.6f, -2.2f, 0.3f); c = new Color(1f, 1f, 1f, 0.9f); size = 0.12f; break;
                case WeatherType.Blizzard: rate = 380f; v = new Vector3(9f, -4f, 3f); c = new Color(1f, 1f, 1f, 0.85f); size = 0.1f; mode = ParticleSystemRenderMode.Stretch; break;
            }
            emission.rateOverTime = rate * weatherBlend * budget;
            vel.x = v.x; vel.y = v.y; vel.z = v.z;
            fxMain.startColor = c;
            fxMain.startSize = size;
            fxMain.startLifetime = Weather == WeatherType.Rain ? 0.7f : 3.5f;
            if (fxRenderer.renderMode != mode) fxRenderer.renderMode = mode;
        }

        /// <summary>Spawn-weight modifier for an element under current weather/time.</summary>
        public bool IsDawn => timeOfDay >= 0.21f && timeOfDay < 0.36f;
        public bool IsDusk => timeOfDay >= 0.66f && timeOfDay < 0.84f;

        /// <summary>Is the weather / hour right for a kin with this spawn condition?</summary>
        public bool ConditionMet(SpawnCondition c)
        {
            switch (c)
            {
                case SpawnCondition.Rain: return Weather == WeatherType.Rain;
                case SpawnCondition.Heat: return Weather == WeatherType.Heatwave || Weather == WeatherType.Ashfall;
                case SpawnCondition.Snowfall: return Weather == WeatherType.Snow || Weather == WeatherType.Blizzard;
                case SpawnCondition.Blizzard: return Weather == WeatherType.Blizzard;
                case SpawnCondition.Dawn: return IsDawn;
                case SpawnCondition.Dusk: return IsDusk;
                default: return true;
            }
        }

        public static string ConditionText(SpawnCondition c)
        {
            switch (c)
            {
                case SpawnCondition.Rain: return "in the rain";
                case SpawnCondition.Heat: return "in a heatwave or ashfall";
                case SpawnCondition.Snowfall: return "while it snows";
                case SpawnCondition.Blizzard: return "in a blizzard";
                case SpawnCondition.Dawn: return "around dawn";
                case SpawnCondition.Dusk: return "around dusk";
                default: return "";
            }
        }

        public float ElementSpawnBonus(Element e)
        {
            switch (Weather)
            {
                case WeatherType.Rain: if (e == Element.Tide || e == Element.Flora) return 2f; if (e == Element.Ember) return 0.5f; break;
                case WeatherType.Heatwave: case WeatherType.Ashfall: if (e == Element.Ember) return 2f; if (e == Element.Tide) return 0.6f; break;
                case WeatherType.Snow: case WeatherType.Blizzard: if (e == Element.Frost) return 2f; break;
            }
            if (IsNight && e == Element.Shade) return 1.5f;
            if (!IsNight && e == Element.Lumen) return 1.3f;
            return 1f;
        }

        public float PlayerSpeedMul => Weather == WeatherType.Blizzard ? 0.9f : 1f;

        public string ClockString()
        {
            float h = timeOfDay * 24f;
            int hh = (int)h, mm = (int)((h - hh) * 60f);
            return $"{hh:00}:{mm:00}";
        }

        public void SetWeather(WeatherType w, float remaining)
        {
            Weather = w;
            weatherTimer = Mathf.Max(10f, remaining);
            weatherBlend = 1f;
        }

        public float WeatherRemaining => weatherTimer;
    }

    public static class BiomeInfo
    {
        public static string Name(BiomeId b)
        {
            switch (b)
            {
                case BiomeId.Badlands: return "Cinderreach Badlands";
                case BiomeId.Peaks: return "Rimefang Peaks";
                case BiomeId.Isles: return "Driftkey Isles";
                case BiomeId.Swamp: return "Mirefen";
                case BiomeId.Desert: return "Sunscour Dunes";
                case BiomeId.Blight: return "Blightmoor";
                case BiomeId.Glow: return "Glowdeep";
                default: return "Sunmeadow Vale";
            }
        }

        public static Color Tint(BiomeId b)
        {
            switch (b)
            {
                case BiomeId.Badlands: return new Color(1f, 0.7f, 0.45f);
                case BiomeId.Peaks: return new Color(0.75f, 0.9f, 1f);
                case BiomeId.Isles: return new Color(0.6f, 0.95f, 0.95f);
                case BiomeId.Swamp: return new Color(0.65f, 0.8f, 0.5f);
                case BiomeId.Desert: return new Color(1f, 0.88f, 0.55f);
                case BiomeId.Blight: return new Color(0.78f, 0.62f, 0.95f);
                case BiomeId.Glow: return new Color(0.5f, 1f, 0.95f);
                default: return new Color(0.65f, 1f, 0.6f);
            }
        }

        /// <summary>Wild level range grows the further you travel from the starting vale.</summary>
        public static Vector2Int LevelRange(BiomeId b, Vector3 pos)
        {
            // outer lands: a set band each, rising toward the land's far side
            if ((int)b > (int)BiomeId.Peaks && WorldGen.TryLand(b, out var land))
            {
                float into = Mathf.Clamp01(Vector2.Distance(new Vector2(pos.x, pos.z), land.to) / (land.radius * 1.6f));
                int lo, hi;
                switch (b)
                {
                    case BiomeId.Isles: lo = 5; hi = 12; break;
                    case BiomeId.Swamp: lo = 12; hi = 20; break;
                    case BiomeId.Desert: lo = 20; hi = 29; break;
                    case BiomeId.Blight: lo = 28; hi = 38; break;
                    default: lo = 34; hi = 45; break;   // Glow
                }
                int span = hi - lo, at = lo + Mathf.RoundToInt(span * 0.45f * into);
                return new Vector2Int(at, Mathf.Min(hi, at + Mathf.Max(3, span / 2)));
            }
            float d = Vector2.Distance(new Vector2(pos.x, pos.z), WorldGen.MeadowCenter);
            int bonus = Mathf.FloorToInt(d / 260f);
            float ds = WorldGen.StartDistance(pos.x, pos.z);
            if (b == BiomeId.Meadow)
            {
                // gentle start: lv 1-3 around the start plateau, ramping up with distance from it
                if (ds < WorldGen.StarterZone) return new Vector2Int(1, 3);
                int sb = Mathf.FloorToInt((ds - WorldGen.StarterZone) / 260f);
                return new Vector2Int(1 + (sb + 1) / 2, Mathf.Min(4 + sb, 12));
            }
            switch (b)
            {
                case BiomeId.Badlands: return new Vector2Int(7 + bonus / 2, 12 + bonus);
                case BiomeId.Peaks: return new Vector2Int(12 + bonus / 2, 18 + bonus);
                default: return new Vector2Int(1 + bonus / 2, Mathf.Min(4 + bonus, 12));
            }
        }
    }
}
