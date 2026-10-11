// Wildbond/VertexLit plus a dark outline: the look of kin, the explorer and other people (RenderAssets.Creature on
// Medium and High quality). The three ordinary passes are the base shader's own (UsePass), so the two can never drift;
// the outline is an "inverted hull" - the back faces pushed out along their normals and painted a dark shade of the
// part's own colour. All passes declare the same per-material block, which keeps the SRP batcher happy.
Shader "Wildbond/VertexLitOutline"
{
    Properties
    {
        _BaseColor ("Tint", Color) = (1,1,1,1)
        _Emission ("Emission", Range(0, 3)) = 0
        _VertexGlow ("Vertex Glow (vertex alpha 0 = self-lit)", Range(0, 2)) = 0
        _Rim ("Rim", Range(0, 1)) = 0.25
        _Wrap ("Light Wrap", Range(0, 1)) = 0.45
        _FlashColor ("Flash Color", Color) = (1,1,1,0)
        _Coat ("Coat (hue turn, saturation, brightness)", Vector) = (0,1,1,0)
        _Outline ("Outline width", Range(0, 3)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        UsePass "Wildbond/VertexLit/FORWARDLIT"

        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Emission;
                half _VertexGlow;
                half _Rim;
                half _Wrap;
                half4 _FlashColor;
                half4 _Coat;
                half _Outline;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                half4  color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3  color      : COLOR;
                half   fog        : TEXCOORD0;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                float3 ws = TransformObjectToWorld(v.positionOS.xyz);
                float3 nws = TransformObjectToWorldNormal(v.normalOS);
                // about the same thickness on screen from close up to the middle distance, thinning to nothing far away
                float dist = distance(ws, _WorldSpaceCameraPos);
                float w = _Outline * 0.0032 * clamp(dist, 1.5, 34.0) * saturate(1.7 - dist / 55.0);
                ws += nws * w;
                o.positionCS = TransformWorldToHClip(ws);
                half3 c = v.color.rgb;
                half grey = dot(c, half3(0.3, 0.59, 0.11));
                c = lerp(half3(grey, grey, grey), c, 0.7) * 0.24 * _Coat.z;   // a dark, slightly greyed shade of the part itself
            #if !defined(UNITY_COLORSPACE_GAMMA)
                c = SRGBToLinear(c);
            #endif
                o.color = lerp(c, _FlashColor.rgb, _FlashColor.a * 0.6);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                return half4(MixFog(i.color, i.fog), 1);
            }
            ENDHLSL
        }

        UsePass "Wildbond/VertexLit/SHADOWCASTER"
        UsePass "Wildbond/VertexLit/DEPTHONLY"
    }
    FallBack "Wildbond/VertexLit"
}
