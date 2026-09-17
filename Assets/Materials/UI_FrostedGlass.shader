Shader "UI/TitanicCinematicBlur_URP"
{
    Properties
    {
        [Header(Panel Colors)]
        _DimColor ("Top Tint (Deep Navy/Mahogany)", Color) = (0.02, 0.04, 0.07, 0.85)
        _GoldColor ("Accent Line Color (Brass/Gold)", Color) = (0.92, 0.78, 0.45, 1.0)

        [Header(Edge Blending)]
        _EdgeSoftness ("Edge Softness (Rand-Weichheit)", Range(0.01, 0.5)) = 0.12

        [Header(Blur and Vertical Fade)]
        _MaxBlur ("Max Blur Strength", Range(0.001, 0.015)) = 0.006
        _FadeStart ("Fade Start (von oben gemessen)", Range(0.0, 1.0)) = 0.35
        _FadeEnd ("Fade End (nach unten auslaufend)", Range(0.0, 1.0)) = 0.95

        [Header(Top Gold Accent)]
        _TopAccentOffset ("Abstand von oberer Kante", Range(0.0, 0.2)) = 0.05
        _TopAccentThickness ("Linienstärke", Range(0.001, 0.03)) = 0.006
        _TopAccentBrightness ("Gold Glow / Helligkeit", Range(1.0, 4.0)) = 2.0
    }

    SubShader
    {
        Tags 
        { 
            "Queue" = "Transparent" 
            "RenderType" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            float4 _DimColor;
            float4 _GoldColor;
            float _EdgeSoftness;
            float _MaxBlur;
            float _FadeStart;
            float _FadeEnd;

            float _TopAccentOffset;
            float _TopAccentThickness;
            float _TopAccentBrightness;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 screenUV = input.screenPos.xy / input.screenPos.w;

                // 1. Weiche Randmaske an allen 4 Kanten (keine sichtbaren harten Box-Kanten)
                float edgeLeft   = smoothstep(0.0, _EdgeSoftness, input.uv.x);
                float edgeRight  = smoothstep(0.0, _EdgeSoftness, 1.0 - input.uv.x);
                float edgeTop    = smoothstep(0.0, _EdgeSoftness, 1.0 - input.uv.y);
                float edgeBottom = smoothstep(0.0, _EdgeSoftness, input.uv.y);
                float allEdgesMask = edgeLeft * edgeRight * edgeTop * edgeBottom;

                // 2. Hauptverlauf von oben nach unten
                float distanceDown = 1.0 - input.uv.y;
                float verticalFade = 1.0 - smoothstep(_FadeStart, _FadeEnd, distanceDown);

                // Gesamter Panel-Faktor
                float panelFactor = verticalFade * allEdgesMask;

                // 3. Gold-Akzent oben
                float targetY = 1.0 - _TopAccentOffset;
                float distToTop = abs(input.uv.y - targetY);
                float goldLine = 1.0 - smoothstep(0.0, _TopAccentThickness, distToTop);

                // Goldlinie läuft an den Seiten weich aus
                goldLine *= edgeLeft * edgeRight * _GoldColor.a;

                // Gesamt-Transparenz
                float totalAlpha = max(panelFactor, goldLine);

                if (totalAlpha <= 0.001) return half4(0, 0, 0, 0);

                // 4. Weichzeichner (skaliert mit dem Panel-Faktor)
                float currentBlur = _MaxBlur * panelFactor;
                half4 col = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV) * 0.28;

                if (currentBlur > 0.0001)
                {
                    col += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV + float2(currentBlur, 0)) * 0.12;
                    col += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV - float2(currentBlur, 0)) * 0.12;
                    col += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV + float2(0, currentBlur)) * 0.12;
                    col += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV - float2(0, currentBlur)) * 0.12;

                    float diag = currentBlur * 0.707;
                    col += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV + float2(diag, diag)) * 0.06;
                    col += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV - float2(diag, diag)) * 0.06;
                    col += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV + float2(-diag, diag)) * 0.06;
                    col += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV + float2(diag, -diag)) * 0.06;
                }
                else
                {
                    col = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV);
                }

                // 5. Abdunklung auftragen
                half4 blended = lerp(col, _DimColor, _DimColor.a * panelFactor);

                // 6. Gold-Akzent hinzufügen
                half3 finalGold = _GoldColor.rgb * _TopAccentBrightness;
                blended.rgb = lerp(blended.rgb, finalGold, goldLine);

                blended.a = totalAlpha;

                return blended;
            }
            ENDHLSL
        }
    }
}