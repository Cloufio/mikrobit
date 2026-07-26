Shader "MicroBit/Toxic Ocean"
{
    Properties
    {
        [MainTexture] _MainTex("Sprite Texture", 2D) = "white" {}
        [MainColor] _Color("Tint", Color) = (1, 1, 1, 1)
        _OilColor("Oil-Black Water", Color) = (0.025, 0.10, 0.12, 1)
        _MuckColor("Murky Contamination", Color) = (0.13, 0.26, 0.24, 1)
        _ToxicColor("Toxic Bloom", Color) = (0.48, 0.76, 0.12, 1)
        _HazardColor("Hazard Glow", Color) = (0.80, 0.96, 0.22, 1)
        _WaterHueThreshold("Water Hue Threshold", Range(0.02, 0.5)) = 0.12
        _FlowSpeed("Flow Speed", Range(0.1, 4)) = 0.26
        _ContaminationScale("Contamination Scale", Range(0.1, 8)) = 1.45
        _ToxicStrength("Toxic Bloom Strength", Range(0, 1)) = 0.58
        _GlowStrength("Hazard Glow Strength", Range(0, 1)) = 0.14
        _PixelDensity("Pixel Density", Range(1, 64)) = 16
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "CanUseSpriteAtlas" = "True"
        }

        Pass
        {
            Name "ToxicOcean"
            Tags { "LightMode" = "Universal2D" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _OilColor;
                float4 _MuckColor;
                float4 _ToxicColor;
                float4 _HazardColor;
                float _WaterHueThreshold;
                float _FlowSpeed;
                float _ContaminationScale;
                float _ToxicStrength;
                float _GlowStrength;
                float _PixelDensity;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 positionWS : TEXCOORD1;
            };

            float Hash21(float2 value)
            {
                value = frac(value * float2(163.34, 291.45));
                value += dot(value, value + 29.345);
                return frac(value.x * value.y);
            }

            float ValueNoise(float2 value)
            {
                float2 cell = floor(value);
                float2 local = frac(value);
                local = local * local * (3.0 - 2.0 * local);
                return lerp(
                    lerp(Hash21(cell), Hash21(cell + float2(1, 0)), local.x),
                    lerp(Hash21(cell + float2(0, 1)), Hash21(cell + float2(1, 1)), local.x),
                    local.y);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 spriteSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float3 sourceColor = spriteSample.rgb * input.color.rgb * _Color.rgb;

                // Sand, debris, and non-water artwork preserve their original texture.
                float blueHue = (sourceColor.b - sourceColor.r) + (sourceColor.g - sourceColor.r) * 0.25;
                float waterMask = smoothstep(_WaterHueThreshold * 0.65, _WaterHueThreshold * 1.45, blueHue);
                float2 pixelWorld = floor(input.positionWS.xy * _PixelDensity) / _PixelDensity;
                float time = _Time.y * _FlowSpeed;

                float broadMuck = ValueNoise(pixelWorld * (_ContaminationScale * 0.46) + float2(time * 0.11, -time * 0.07));
                float oilyGrain = ValueNoise(pixelWorld * (_ContaminationScale * 2.15) + float2(-time * 0.18, time * 0.14));
                float3 contaminatedWater = lerp(_OilColor.rgb, _MuckColor.rgb, saturate(broadMuck * 0.80 + oilyGrain * 0.18));

                float bloomField = ValueNoise(pixelWorld * (_ContaminationScale * 0.80) + float2(-time * 0.15, time * 0.09));
                float toxicBloom = smoothstep(0.62, 0.90, bloomField) * smoothstep(0.42, 0.82, broadMuck);
                contaminatedWater = lerp(contaminatedWater, _ToxicColor.rgb, toxicBloom * _ToxicStrength);

                float slickLines = sin((pixelWorld.x * 2.3 - pixelWorld.y * 1.1) * _ContaminationScale + oilyGrain * 6.0 + time * 1.2);
                float glow = smoothstep(0.965, 0.998, slickLines) * toxicBloom;
                contaminatedWater = lerp(contaminatedWater, _HazardColor.rgb, glow * _GlowStrength);

                float3 finalColor = lerp(sourceColor, contaminatedWater, waterMask * 0.96);
                return half4(finalColor, spriteSample.a * input.color.a * _Color.a);
            }
            ENDHLSL
        }
    }
}
