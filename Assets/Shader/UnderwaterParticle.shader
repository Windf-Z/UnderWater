Shader "Underwater/Particle"
{
    Properties
    {
        _Color ("Tint", Color) = (0.8, 0.92, 1, 1)
        _Intensity ("Intensity", Float) = 0.6
        _Softness ("Softness", Range(0.5, 4)) = 1.8
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
            Name "Particle"
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
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Intensity;
                float _Softness;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float d = length(i.uv - 0.5) * 2.0;
                float mask = saturate(1.0 - d);
                mask = pow(mask, _Softness);

                half a = mask * i.color.a * _Intensity;
                return half4(_Color.rgb * i.color.rgb * a, a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
