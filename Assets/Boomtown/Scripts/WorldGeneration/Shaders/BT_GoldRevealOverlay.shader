Shader "Boomtown/Gold Reveal Overlay"
{
    Properties
    {
        _GlobalOpacity ("Global Opacity", Range(0, 1)) = 1
        _PulseSpeed ("Pulse Speed", Range(0, 5)) = 1.2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+80"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Off

        Pass
        {
            Name "GoldReveal"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _GlobalOpacity;
                half _PulseSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;

                output.positionCS =
                    TransformObjectToHClip(
                        input.positionOS.xyz);

                output.color =
                    input.color;

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                if (input.color.a <= 0.001)
                {
                    discard;
                }

                half pulse =
                    0.88 +
                    sin(
                        _Time.y *
                        _PulseSpeed) *
                    0.12;

                return half4(
                    input.color.rgb *
                    pulse,
                    input.color.a *
                    _GlobalOpacity);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
