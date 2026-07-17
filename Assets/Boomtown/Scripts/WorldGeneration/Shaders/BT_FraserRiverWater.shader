Shader "Boomtown/Fraser River Water"
{
    Properties
    {
        _ShallowColor ("Shallow Water", Color) = (0.34, 0.59, 0.62, 0.82)
        _DeepColor ("Deep Water", Color) = (0.09, 0.31, 0.40, 0.92)
        _FoamColor ("Foam", Color) = (0.86, 0.91, 0.86, 0.92)

        _Transparency ("Transparency", Range(0.2, 1)) = 0.82
        _FlowSpeed ("Flow Speed", Range(0, 2)) = 0.42
        _WaveScale ("Wave Scale", Range(0.01, 0.4)) = 0.075
        _WaveStrength ("Wave Strength", Range(0, 0.5)) = 0.12
        _ShoreFoam ("Shore Foam", Range(0, 1.5)) = 0.72
        _RapidFoam ("Rapid Foam", Range(0, 1.5)) = 0.58
        _Smoothness ("Smoothness", Range(0, 1)) = 0.84
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "FraserRiverForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                half4 _FoamColor;
                half _Transparency;
                half _FlowSpeed;
                half _WaveScale;
                half _WaveStrength;
                half _ShoreFoam;
                half _RapidFoam;
                half _Smoothness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 color : COLOR;
                half fogFactor : TEXCOORD3;
            };

            float WaveField(float2 p, float timeValue)
            {
                float waveA =
                    sin(p.x * 1.7 + p.y * 0.55 + timeValue);

                float waveB =
                    sin(p.x * -0.8 + p.y * 1.35 + timeValue * 1.31);

                float waveC =
                    sin(p.x * 2.4 + p.y * -0.32 + timeValue * 0.73);

                return
                    (waveA + waveB + waveC) /
                    3.0;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;

                float3 positionWS =
                    TransformObjectToWorld(
                        input.positionOS.xyz);

                float speed =
                    lerp(0.65, 1.65, input.color.r);

                float timeValue =
                    _Time.y *
                    _FlowSpeed *
                    speed;

                float wave =
                    WaveField(
                        positionWS.xz *
                        _WaveScale,
                        timeValue);

                // Fast constricted sections receive slightly stronger surface
                // motion, while the river remains a stable navigable surface.
                positionWS.y +=
                    wave *
                    _WaveStrength *
                    lerp(0.45, 1.0, input.color.r);

                output.positionWS =
                    positionWS;

                output.positionCS =
                    TransformWorldToHClip(
                        positionWS);

                half3 normalWS =
                    TransformObjectToWorldNormal(
                        input.normalOS);

                float epsilon = 0.24;

                float waveX =
                    WaveField(
                        (positionWS.xz +
                         float2(epsilon, 0)) *
                        _WaveScale,
                        timeValue);

                float waveZ =
                    WaveField(
                        (positionWS.xz +
                         float2(0, epsilon)) *
                        _WaveScale,
                        timeValue);

                half3 proceduralNormal =
                    normalize(
                        half3(
                            (wave - waveX) *
                            2.3,
                            1.0,
                            (wave - waveZ) *
                            2.3));

                output.normalWS =
                    normalize(
                        lerp(
                            normalWS,
                            proceduralNormal,
                            0.72));

                output.uv =
                    input.uv;

                output.color =
                    input.color;

                output.fogFactor =
                    ComputeFogFactor(
                        output.positionCS.z);

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float timeValue =
                    _Time.y *
                    _FlowSpeed *
                    lerp(
                        0.65,
                        1.65,
                        input.color.r);

                float flowA =
                    sin(
                        input.uv.y * 5.2 -
                        timeValue * 5.0 +
                        input.uv.x * 7.0);

                float flowB =
                    sin(
                        input.uv.y * 10.4 -
                        timeValue * 8.2 -
                        input.uv.x * 12.0);

                float surfaceVariation =
                    saturate(
                        0.5 +
                        flowA * 0.22 +
                        flowB * 0.12);

                float distanceFromCentre =
                    abs(
                        input.uv.x -
                        0.5) *
                    2.0;

                float shallowMask =
                    smoothstep(
                        0.15,
                        1.0,
                        distanceFromCentre);

                half4 waterColor =
                    lerp(
                        _DeepColor,
                        _ShallowColor,
                        shallowMask *
                        0.82 +
                        surfaceVariation *
                        0.12);

                float shoreFoam =
                    smoothstep(
                        0.72,
                        0.98,
                        distanceFromCentre +
                        flowB * 0.055) *
                    input.color.b *
                    _ShoreFoam;

                float rapidLines =
                    smoothstep(
                        0.57,
                        0.88,
                        surfaceVariation);

                float rapidFoam =
                    rapidLines *
                    input.color.r *
                    lerp(
                        0.35,
                        1.0,
                        input.color.g) *
                    _RapidFoam;

                float foam =
                    saturate(
                        shoreFoam +
                        rapidFoam);

                half3 normalWS =
                    normalize(
                        input.normalWS);

                half3 viewDirection =
                    SafeNormalize(
                        GetWorldSpaceViewDir(
                            input.positionWS));

                half3 lightDirection =
                    normalize(
                        _MainLightPosition.xyz);

                half diffuse =
                    saturate(
                        dot(
                            normalWS,
                            lightDirection)) *
                    0.22 +
                    0.78;

                half fresnel =
                    pow(
                        1.0 -
                        saturate(
                            dot(
                                normalWS,
                                viewDirection)),
                        4.0);

                half specular =
                    pow(
                        saturate(
                            dot(
                                reflect(
                                    -lightDirection,
                                    normalWS),
                                viewDirection)),
                        lerp(
                            24.0,
                            110.0,
                            _Smoothness));

                half3 finalColor =
                    waterColor.rgb *
                    diffuse;

                finalColor +=
                    fresnel *
                    half3(
                        0.20,
                        0.28,
                        0.31);

                finalColor +=
                    specular *
                    0.42;

                finalColor =
                    lerp(
                        finalColor,
                        _FoamColor.rgb,
                        foam);

                finalColor =
                    MixFog(
                        finalColor,
                        input.fogFactor);

                half alpha =
                    saturate(
                        _Transparency +
                        fresnel * 0.10 +
                        foam * 0.12);

                return half4(
                    finalColor,
                    alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
