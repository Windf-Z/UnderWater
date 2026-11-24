Shader "Underwater/WaterSurface"
{
    Properties
    {
        _DeepColor ("Deep Color", Color) = (0.015, 0.055, 0.075, 1)
        _ShallowColor ("Shallow Color", Color) = (0.07, 0.22, 0.24, 1)
        _HighlightColor ("Highlight Color", Color) = (0.35, 0.58, 0.60, 1)
        _Alpha ("Alpha", Range(0, 1)) = 0.9
        _WaveHeight ("Wave Height", Float) = 0.32
        _WaveSpeed ("Wave Speed", Float) = 1.25
        _Fresnel ("Fresnel", Range(0, 2)) = 1.0
        _Sparkle ("Sparkle", Range(0, 2)) = 1.0
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "WaterSurface"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float crest : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _DeepColor;
                float4 _ShallowColor;
                float4 _HighlightColor;
                float _Alpha;
                float _WaveHeight;
                float _WaveSpeed;
                float _Fresnel;
                float _Sparkle;
            CBUFFER_END

            // 三组方向波叠加：算高度和斜率，斜率顺便当法线用
            void WaterWaves(float2 p, float t, out float h, out float dhdx, out float dhdz)
            {
                float a1 = 0.55, k1 = 0.45, s1 = 1.30;
                float a2 = 0.30, k2 = 0.85, s2 = -0.90;
                float a3 = 0.15, k3 = 1.70, s3 = 2.10;

                float ph1 = p.x * k1 + p.y * 0.25 + t * s1;
                float ph2 = p.y * k2 - p.x * 0.35 + t * s2;
                float ph3 = (p.x + p.y) * k3 + t * s3;

                h = a1 * sin(ph1) + a2 * sin(ph2) + a3 * sin(ph3);

                dhdx = a1 * k1 * cos(ph1) - a2 * 0.35 * cos(ph2) + a3 * k3 * cos(ph3);
                dhdz = a1 * 0.25 * cos(ph1) + a2 * k2 * cos(ph2) + a3 * k3 * cos(ph3);
            }

            Varyings vert(Attributes v)
            {
                Varyings o;

                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                float t = _Time.y * _WaveSpeed;

                float h, dhdx, dhdz;
                WaterWaves(positionWS.xz, t, h, dhdx, dhdz);

                positionWS.y += h * _WaveHeight;

                o.positionWS = positionWS;
                o.positionCS = TransformWorldToHClip(positionWS);
                o.normalWS = normalize(float3(-dhdx * _WaveHeight, 1.0, -dhdz * _WaveHeight));
                o.crest = saturate(h * 0.5 + 0.5);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 N = normalize(i.normalWS);
                float3 V = normalize(GetWorldSpaceViewDir(i.positionWS));
                float t = _Time.y * _WaveSpeed;
                float2 p = i.positionWS.xz;

                float fresnel = pow(1.0 - saturate(dot(N, V)), 4.0);

                // 主色：低处深、浪尖浅
                half3 col = lerp(_DeepColor.rgb, _ShallowColor.rgb, i.crest * 0.8);
                col = lerp(col, _HighlightColor.rgb, fresnel * _Fresnel * 0.8);

                // 会动的闪光点（假一盏天光）
                float3 L = normalize(float3(0.35, 0.75, 0.25));
                float3 H = normalize(L + V);
                float spec = pow(saturate(dot(N, H)), 48.0) * _Sparkle;

                // 浪尖上的泡沫/白边，让起伏一眼看得出来
                float foamNoise = sin(p.x * 2.3 + t * 1.7) * sin(p.y * 1.9 - t * 1.3);
                float foam = saturate(foamNoise) * smoothstep(0.58, 0.92, i.crest) * 0.75;

                col += _HighlightColor.rgb * (spec + foam);
                col += _HighlightColor.rgb * foam * 0.6;

                // 水面和墙/箱子相交的地方画一条亮边：水涨上去，这条线会往上爬
                float eyeDepth = -TransformWorldToView(i.positionWS).z;
                float2 screenUV = GetNormalizedScreenSpaceUV(i.positionCS);
                float sceneZ = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float diff = sceneZ - eyeDepth;
                float edge = diff > 0.0 ? saturate(1.0 - diff / 0.30) : 0.0;
                col += _HighlightColor.rgb * edge * 0.5;

                half a = saturate(_Alpha + fresnel * 0.08 + foam * 0.1 + edge * 0.15);
                return half4(col, a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
