Shader "MicroBit/Celebration Ocean"
{
    Properties
    {
        [MainTexture] _MainTex("Sprite Texture", 2D) = "white" {}
        [MainColor] _Color("Tint", Color) = (1, 1, 1, 1)
        _DeepColor("Deep Water", Color) = (0.10, 0.48, 0.63, 1)
        _ShallowColor("Sunlit Water", Color) = (0.35, 0.78, 0.82, 1)
        _FoamColor("Sun Glint", Color) = (0.92, 1.00, 0.88, 1)
        _WaterHueThreshold("Water Hue Threshold", Range(0.02, 0.5)) = 0.12
        _FlowSpeed("Flow Speed", Range(0.1, 4)) = 0.38
        _WaveScale("Wave Scale", Range(0.1, 8)) = 1.25
        _RippleStrength("Ripple Strength", Range(0, 1)) = 0.42
        _GlintStrength("Glint Strength", Range(0, 1)) = 0.24
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
            Name "CelebrationOcean"
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
                float4 _DeepColor;
                float4 _ShallowColor;
                float4 _FoamColor;
                float _WaterHueThreshold;
                float _FlowSpeed;
                float _WaveScale;
                float _RippleStrength;
                float _GlintStrength;
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
                value = frac(value * float2(123.34, 345.45));
                value += dot(value, value + 34.345);
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

                // Coastline and other non-blue artwork remain untouched.
                float blueHue = (sourceColor.b - sourceColor.r) + (sourceColor.g - sourceColor.r) * 0.25;
                float waterMask = smoothstep(_WaterHueThreshold * 0.65, _WaterHueThreshold * 1.45, blueHue);
                float2 pixelWorld = floor(input.positionWS.xy * _PixelDensity) / _PixelDensity;
                float time = _Time.y * _FlowSpeed;

                float broadWave = ValueNoise(pixelWorld * (_WaveScale * 0.46) + float2(time * 0.12, -time * 0.07));
                float fineWave = ValueNoise(pixelWorld * (_WaveScale * 1.9) + float2(-time * 0.20, time * 0.16));
                float sourceLight = dot(sourceColor, float3(0.18, 0.64, 0.18));
                float sunlitDepth = saturate(0.30 + broadWave * 0.44 + fineWave * 0.15 + sourceLight * 0.14);
                float3 cleanWater = lerp(_DeepColor.rgb, _ShallowColor.rgb, sunlitDepth);

                float ripplePhase = sin((pixelWorld.x * 2.2 + pixelWorld.y * 1.35) * _WaveScale + fineWave * 7.0 + time * 1.55);
                float ripple = smoothstep(0.78, 0.97, ripplePhase) * smoothstep(0.46, 0.80, broadWave);
                cleanWater = lerp(cleanWater, _FoamColor.rgb, ripple * _RippleStrength);

                float2 glintCell = floor(pixelWorld * (_WaveScale * 3.2) + float2(time * 0.30, -time * 0.16));
                float glint = step(0.988 - _GlintStrength * 0.07, Hash21(glintCell));
                glint *= step(0.70, broadWave) * step(0.55, fineWave);
                cleanWater = saturate(cleanWater + glint * _FoamColor.rgb * 0.72);

                float3 finalColor = lerp(sourceColor, cleanWater, waterMask * 0.94);
                return half4(finalColor, spriteSample.a * input.color.a * _Color.a);
            }
            ENDHLSL
        }
    }
}
