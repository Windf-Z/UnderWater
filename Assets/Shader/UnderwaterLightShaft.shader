Shader "Underwater/LightShaft"
{
    Properties
    {
        _Color ("Shaft Color", Color) = (1.0, 0.92, 0.8, 1)
        _Intensity ("Intensity", Float) = 0.55
        _NoiseScale ("Noise Scale", Float) = 1.4
        _NoiseSpeed ("Noise Speed", Float) = 0.12
        _EdgeBoost ("Edge Boost", Range(0, 1)) = 0.75
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
            Name "LightShaft"
            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Intensity;
                float _NoiseScale;
                float _NoiseSpeed;
                float _EdgeBoost;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.color = v.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float t = _Time.y * _NoiseSpeed;

                // 竖向水纹：光柱里流动的条带
                float n1 = sin(i.positionWS.y * _NoiseScale + t * 2.1) * 0.5 + 0.5;
                float n2 = sin(i.positionWS.y * _NoiseScale * 0.37 - t * 1.3 + i.positionWS.x * 0.05) * 0.5 + 0.5;
                float streak = lerp(0.55, 1.0, n1 * 0.65 + n2 * 0.35);

                // 边缘更亮、正对镜头的地方更透，避免看成实心圆锥
                float3 viewDir = normalize(GetWorldSpaceViewDir(i.positionWS));
                float facing = saturate(1.0 - abs(dot(normalize(i.normalWS), viewDir)));
                float edge = lerp(1.0 - _EdgeBoost, 1.0, pow(facing, 1.5));

                // 慢速的水下晃动
                float sway = 0.92 + 0.08 * sin(t * 1.7 + i.positionWS.y * 0.1);

                half a = i.color.a * saturate(_Intensity * streak * sway * edge);
                return half4(_Color.rgb * a, a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
