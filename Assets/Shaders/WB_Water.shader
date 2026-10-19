// Cheap stylized water: vertex waves, fresnel sky tint, sparkle bands, fog. No depth texture needed (mobile).
Shader "Wildbond/Water"
{
    Properties
    {
        _ShallowColor ("Shallow", Color) = (0.25, 0.75, 0.85, 0.72)
        _DeepColor ("Deep", Color) = (0.08, 0.32, 0.55, 0.9)
        _SkyColor ("Sky", Color) = (0.7, 0.85, 1, 1)
        _WaveAmp ("Wave Amp", Float) = 0.18
        _WaveFreq ("Wave Freq", Float) = 0.12
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-10" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                half4 _SkyColor;
                float _WaveAmp;
                float _WaveFreq;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                half fog : TEXCOORD2;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                float3 wp = TransformObjectToWorld(v.positionOS.xyz);
                float t = _Time.y;
                float w1 = sin(wp.x * _WaveFreq + t * 1.3) * cos(wp.z * _WaveFreq * 0.8 + t * 0.9);
                float w2 = sin((wp.x + wp.z) * _WaveFreq * 2.1 - t * 1.7) * 0.5;
                wp.y += (w1 + w2) * _WaveAmp;
                float dx = cos(wp.x * _WaveFreq + t * 1.3) * _WaveFreq * _WaveAmp;
                float dz = -sin(wp.z * _WaveFreq * 0.8 + t * 0.9) * _WaveFreq * _WaveAmp;
                o.normalWS = normalize(float3(-dx, 1, -dz));
                o.positionWS = wp;
                o.positionCS = TransformWorldToHClip(wp);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                float3 n = normalize(i.normalWS);
                half fres = pow(1.0 - saturate(dot(n, v)), 3.0);
                Light l = GetMainLight();
                half3 h = normalize(l.direction + v);
                half spec = pow(saturate(dot(n, h)), 90.0) * 1.2;
                // glints that twinkle where they are and only creep (~0.4 m/s): they used to sweep along at ~2.8 m/s,
                // as fast as surfing, which read as the sea flowing one way (user playtest 2026-10-18)
                float2 xz = i.positionWS.xz;
                float band = sin(xz.x * 0.6 + xz.y * 0.4 + _Time.y * 0.3) * sin(xz.y * 0.7 - xz.x * 0.25 - _Time.y * 0.22);
                float cell = frac(sin(dot(floor(xz * 0.35), float2(12.9898, 78.233))) * 43758.5453);
                half twinkle = 0.45 + 0.55 * saturate(sin(_Time.y * (1.1 + cell) + cell * 6.2832) * 0.5 + 0.5);
                half sparkle = smoothstep(0.92, 1.0, band) * 0.35 * twinkle;
                half4 baseCol = lerp(_ShallowColor, _DeepColor, saturate(fres * 0.4 + 0.3));
                half3 col = baseCol.rgb * (l.color * 0.6 + SampleSH(n) * 0.8);
                col = lerp(col, _SkyColor.rgb, fres * 0.6) + (spec + sparkle) * l.color;
                col = MixFog(col, i.fog);
                return half4(col, saturate(baseCol.a + fres * 0.2));
            }
            ENDHLSL
        }
    }
    FallBack Off
}
