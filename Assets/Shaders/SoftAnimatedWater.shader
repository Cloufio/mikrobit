Shader "MicroBit/Soft Animated Water"
{
    Properties
    {
        [MainTexture] _MainTex("Sprite Texture", 2D) = "white" {}
        [MainColor] _Color("Tint", Color) = (1, 1, 1, 1)
        _DeepColor("Deep Water", Color) = (0.22, 0.47, 0.65, 1)
        _ShallowColor("Sunlit Water", Color) = (0.48, 0.70, 0.79, 1)
        _RippleColor("Ripple Highlight", Color) = (0.85, 0.96, 0.94, 1)
        _WaterHueThreshold("Water Hue Threshold", Range(0.02, 0.5)) = 0.14
        _FlowSpeed("Flow Speed", Range(0.1, 4)) = 0.55
        _WaveScale("Wave Scale", Range(0.1, 8)) = 1.35
        _RippleStrength("Ripple Strength", Range(0, 1)) = 0.32
        _SparkleStrength("Sparkle Strength", Range(0, 1)) = 0.08
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
            Name "SoftAnimatedWater"
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
                float4 _RippleColor;
                float _WaterHueThreshold;
                float _FlowSpeed;
                float _WaveScale;
                float _RippleStrength;
                float _SparkleStrength;
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

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 cell = floor(p);
                float2 local = frac(p);
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

                // Only replace blue water pixels. Sand, coastline, and non-water tiles keep their artwork.
                float blueHue = (sourceColor.b - sourceColor.r) + (sourceColor.g - sourceColor.r) * 0.25;
                float waterMask = smoothstep(_WaterHueThreshold * 0.65, _WaterHueThreshold * 1.45, blueHue);
                float2 pixelWorld = floor(input.positionWS.xy * _PixelDensity) / _PixelDensity;
                float time = _Time.y * _FlowSpeed;

                float broadFlow = ValueNoise(pixelWorld * (_WaveScale * 0.38) + float2(time * 0.14, -time * 0.08));
                float fineFlow = ValueNoise(pixelWorld * (_WaveScale * 1.75) + float2(-time * 0.22, time * 0.16));
                float sourceLight = dot(sourceColor, float3(0.18, 0.64, 0.18));
                float depth = saturate(0.20 + broadFlow * 0.54 + fineFlow * 0.18 + sourceLight * 0.12);
                float3 water = lerp(_DeepColor.rgb, _ShallowColor.rgb, depth);

                float ripplePhase = sin((pixelWorld.x * 2.4 + pixelWorld.y * 1.5) * _WaveScale + fineFlow * 7.0 + time * 1.7);
                float ripple = smoothstep(0.76, 0.96, ripplePhase) * smoothstep(0.48, 0.78, broadFlow);
                water = lerp(water, _RippleColor.rgb, ripple * _RippleStrength);

                float2 sparkleCell = floor(pixelWorld * (_WaveScale * 3.6) + float2(time * 0.35, -time * 0.2));
                float sparkleGate = Hash21(sparkleCell);
                float sparkle = step(0.985 - _SparkleStrength * 0.09, sparkleGate);
                sparkle *= step(0.66, broadFlow);
                water = saturate(water + sparkle * _RippleColor.rgb * 0.8);

                float3 finalColor = lerp(sourceColor, water, waterMask * 0.92);
                return half4(finalColor, spriteSample.a * input.color.a * _Color.a);
            }
            ENDHLSL
        }
    }
}
