// Gradient sky dome whose horizon matches the fog color exactly, plus sun/moon disc and stars. One draw call.
Shader "Wildbond/Sky"
{
    Properties
    {
        _ZenithColor ("Zenith", Color) = (0.35, 0.6, 1, 1)
        _HorizonColor ("Horizon", Color) = (0.8, 0.9, 1, 1)
        _SunColor ("Sun", Color) = (1, 0.95, 0.8, 1)
        _SunDir ("Sun Dir", Vector) = (0, 1, 0, 0)
        _Stars ("Stars", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Background" "Queue"="Background" "RenderPipeline"="UniversalPipeline" "PreviewType"="Skybox" }
        Pass
        {
            Name "Sky"
            Tags { "LightMode"="UniversalForward" }
            ZWrite Off
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ZenithColor;
                half4 _HorizonColor;
                half4 _SunColor;
                float4 _SunDir;
                half _Stars;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
            #if UNITY_REVERSED_Z
                o.positionCS.z = o.positionCS.w * 0.000001;
            #else
                o.positionCS.z = o.positionCS.w * 0.999999;
            #endif
                o.dir = v.positionOS.xyz;
                return o;
            }

            float hash(float3 p) { p = frac(p * 0.3183099 + 0.1); p *= 17.0; return frac(p.x * p.y * p.z * (p.x + p.y + p.z)); }

            half4 frag (Varyings i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float t = saturate(d.y);
                half3 col = lerp(_HorizonColor.rgb, _ZenithColor.rgb, pow(t, 0.55));
                if (d.y < 0) col = _HorizonColor.rgb;
                float3 sd = normalize(_SunDir.xyz);
                float s = dot(d, sd);
                col += _SunColor.rgb * (smoothstep(0.9985, 0.9993, s) * 2.0 + pow(saturate(s), 64.0) * 0.35);
                // moon opposite the sun
                float m = dot(d, -sd);
                col += half3(0.8, 0.85, 1.0) * smoothstep(0.9990, 0.9995, m) * _Stars;
                // stars
                float3 cell = floor(d * 180.0);
                float st = step(0.9965, hash(cell)) * saturate(d.y * 3.0) * _Stars;
                col += st * 0.9;
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
