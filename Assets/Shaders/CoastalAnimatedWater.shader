Shader "MicroBit/Coastal Animated Water"
{
    Properties
    {
        [MainTexture] _MainTex("Sprite Texture", 2D) = "white" {}
        [MainColor] _Color("Tint", Color) = (1, 1, 1, 1)
        _ShoreDistanceTex("Shore Distance", 2D) = "white" {}
        _ShoreColor("Shore Water", Color) = (0.72, 0.72, 0.57, 1)
        _ShallowColor("Shallow Water", Color) = (0.56, 0.72, 0.76, 1)
        _OceanColor("Ocean Water", Color) = (0.30, 0.57, 0.70, 1)
        _DeepColor("Deep Water", Color) = (0.24, 0.48, 0.63, 1)
        _RippleColor("Surface Highlight", Color) = (0.90, 0.98, 0.96, 1)
        _WaterHueThreshold("Water Hue Threshold", Range(0.02, 0.5)) = 0.14
        _FlowSpeed("Flow Speed", Range(0.1, 4)) = 0.55
        _WaveScale("Wave Scale", Range(0.1, 8)) = 2.2
        _RippleStrength("Ripple Strength", Range(0, 1)) = 0.5
        _MicroRippleScale("Micro Ripple Scale", Range(0.1, 4)) = 1.65
        _MicroRippleStrength("Micro Ripple Strength", Range(0, 1)) = 0.58
        _MicroRippleLineWidth("Micro Ripple Line Width", Range(0.01, 0.25)) = 0.09
        _MicroRippleSpeed("Micro Ripple Speed", Range(0.05, 3)) = 0.65
        _MicroRippleMotion("Micro Ripple Motion", Range(0, 0.75)) = 0.18
        _GlintDensity("Glint Density", Range(0.1, 2)) = 0.65
        _GlintBlinkSpeed("Glint Blink Speed", Range(0.1, 5)) = 0.8
        _SparkleStrength("Glint Brightness", Range(0, 1)) = 0.42
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
            Name "CoastalAnimatedWater"
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
                float4 _ShoreColor;
                float4 _ShallowColor;
                float4 _OceanColor;
                float4 _DeepColor;
                float4 _RippleColor;
                float4 _ShoreMapWorldMin;
                float4 _ShoreMapWorldSize;
                float _WaterHueThreshold;
                float _FlowSpeed;
                float _WaveScale;
                float _RippleStrength;
                float _MicroRippleScale;
                float _MicroRippleStrength;
                float _MicroRippleLineWidth;
                float _MicroRippleSpeed;
                float _MicroRippleMotion;
                float _GlintDensity;
                float _GlintBlinkSpeed;
                float _SparkleStrength;
                float _PixelDensity;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_ShoreDistanceTex);
            SAMPLER(sampler_ShoreDistanceTex);

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

            float Fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;

                [unroll]
                for (int octave = 0; octave < 4; octave++)
                {
                    value += ValueNoise(p) * amplitude;
                    p = p * 2.03 + 19.17;
                    amplitude *= 0.5;
                }

                return value / 0.9375;
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
                float blueHue = (sourceColor.b - sourceColor.r) + (sourceColor.g - sourceColor.r) * 0.25;
                float waterMask = smoothstep(_WaterHueThreshold * 0.65, _WaterHueThreshold * 1.45, blueHue);

                float2 shoreUv = saturate((input.positionWS.xy - _ShoreMapWorldMin.xy) / _ShoreMapWorldSize.xy);
                float shoreDistance = SAMPLE_TEXTURE2D(_ShoreDistanceTex, sampler_ShoreDistanceTex, shoreUv).r;

                float3 water = lerp(_ShoreColor.rgb, _ShallowColor.rgb, smoothstep(0.02, 0.28, shoreDistance));
                water = lerp(water, _OceanColor.rgb, smoothstep(0.22, 0.64, shoreDistance));
                water = lerp(water, _DeepColor.rgb, smoothstep(0.60, 1.0, shoreDistance));

                float flowTime = _Time.y * _FlowSpeed;
                float2 surfacePosition = input.positionWS.xy * _WaveScale;
                float broadSurface = Fbm(surfacePosition * 0.18 + float2(flowTime * 0.025, -flowTime * 0.018));
                float fineSurface = Fbm(surfacePosition * 0.62 + float2(-flowTime * 0.05, flowTime * 0.035));
                float surfaceLight = (broadSurface - 0.5) * 0.13 + (fineSurface - 0.5) * 0.07;
                water = saturate(water + surfaceLight);

                float ripple = smoothstep(0.66, 0.88, broadSurface * 0.58 + fineSurface * 0.42);
                water = lerp(water, _RippleColor.rgb, ripple * _RippleStrength * 0.18);

                // A slow drift and sideways sway keep the small ripples alive without moving the whole ocean.
                float microTime = _Time.y * _MicroRippleSpeed;
                float2 microPosition = surfacePosition * _MicroRippleScale;
                float2 microDrift = float2(microTime * 0.16, -microTime * 0.10);
                float2 microSway = float2(
                    sin(microPosition.y * 1.7 + microTime * 1.4),
                    cos(microPosition.x * 1.45 - microTime * 1.15)) * _MicroRippleMotion;
                float microSurface = Fbm(microPosition + microDrift + microSway);
                float rippleEdge = abs(frac(microSurface * 5.5) - 0.5);
                float rippleContour = 1.0 - smoothstep(_MicroRippleLineWidth * 0.55, _MicroRippleLineWidth, rippleEdge);
                water = saturate(water + (microSurface - 0.5) * 0.07 * _MicroRippleStrength);
                water = lerp(water, _RippleColor.rgb, rippleContour * _MicroRippleStrength * 0.35);

                float2 glintPosition = input.positionWS.xy * _GlintDensity;
                float2 glintCell = floor(glintPosition);
                float glintSeed = Hash21(glintCell);
                float blink = smoothstep(0.56, 0.82, sin(flowTime * _GlintBlinkSpeed + glintSeed * 6.2831853) * 0.5 + 0.5);
                float2 glintLocal = frac(glintPosition) - 0.5;
                float circularGlint = step(length(glintLocal), 0.025);
                float glint = step(0.965, glintSeed) * blink * circularGlint;
                water = lerp(water, _RippleColor.rgb, glint * _SparkleStrength);

                float3 finalColor = lerp(sourceColor, water, waterMask * 0.94);
                return half4(finalColor, spriteSample.a * input.color.a * _Color.a);
            }
            ENDHLSL
        }
    }
}
