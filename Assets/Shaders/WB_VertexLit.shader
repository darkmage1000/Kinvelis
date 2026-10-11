// Wildbond shared world shader: vertex colors, soft stylized main-light, SH ambient, rim, fog, shadows.
// One material for terrain, props, creatures, buildings => SRP Batcher + instancing friendly on mobile.
Shader "Wildbond/VertexLit"
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
        _Outline ("Outline width (used by Wildbond/VertexLitOutline)", Range(0, 3)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
                half4  color      : COLOR;
                float3 normalWS   : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half   fog        : TEXCOORD2;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                half4 vc = v.color;
                // one kin's own coat (CreatureRig.SetLook): turn the hue about the grey axis, then saturation and brightness
                if (_Coat.x != 0.0)
                {
                    half ang = _Coat.x * 6.2831853;
                    half cs = cos(ang), sn = sin(ang);
                    half3 k = half3(0.57735, 0.57735, 0.57735);
                    vc.rgb = vc.rgb * cs + cross(k, vc.rgb) * sn + k * (dot(k, vc.rgb) * (1.0 - cs));
                }
                half grey = dot(vc.rgb, half3(0.3, 0.59, 0.11));
                vc.rgb = saturate(lerp(half3(grey, grey, grey), vc.rgb, _Coat.y) * _Coat.z);
            #if !defined(UNITY_COLORSPACE_GAMMA)
                vc.rgb = SRGBToLinear(vc.rgb); // palettes are authored in sRGB
            #endif
                o.color = vc * _BaseColor;
                o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                Light l = GetMainLight(shadowCoord);
                half ndl = dot(n, l.direction);
                half wrapped = saturate((ndl + _Wrap) / (1.0 + _Wrap));
                // soft two-tone ramp for a painterly, toy-like read
                half ramp = smoothstep(0.0, 0.55, wrapped) * 0.75 + wrapped * 0.25;
                half shadow = lerp(0.35, 1.0, l.shadowAttenuation);
                half3 ambient = SampleSH(n);
                float3 viewDir = normalize(GetWorldSpaceViewDir(i.positionWS));
                half rim = pow(1.0 - saturate(dot(n, viewDir)), 3.0) * _Rim;
                half3 albedo = i.color.rgb;
                half3 col = albedo * (l.color * ramp * shadow + ambient) + rim * (ambient + l.color * 0.3);
                col += albedo * _Emission;
                // parts painted with vertex alpha 0 are self-lit: never darker than their own colour (legendaries' glowing parts)
                col = max(col, albedo * ((1.0 - i.color.a) * _VertexGlow));
                col = lerp(col, _FlashColor.rgb, _FlashColor.a);
                col = MixFog(col, i.fog);
                return half4(col, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

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

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 vert (Attributes v) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(v);
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDir = normalize(_LightPosition - positionWS);
            #else
                float3 lightDir = _LightDirection;
            #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDir));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                return positionCS;
            }

            half4 frag () : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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

            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };

            float4 vert (Attributes v) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(v);
                return TransformObjectToHClip(v.positionOS.xyz);
            }
            half frag () : SV_Target { return 0; }
            ENDHLSL
        }
    }
    FallBack Off
}
