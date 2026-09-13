Shader "UI/TitanicCinematicBlur_URP"
{
    Properties
    {
        [Header(Panel Colors)]
        _DimColor ("Left Tint (Deep Navy/Mahogany)", Color) = (0.02, 0.04, 0.07, 0.85)
        _GoldColor ("Accent Line Color (Brass/Gold)", Color) = (0.92, 0.78, 0.45, 1.0)

        [Header(Blur and Vignette)]
        _MaxBlur ("Max Blur Strength", Range(0.001, 0.015)) = 0.006
        _FadeStart ("Fade Start", Range(0.0, 1.0)) = 0.35
        _FadeEnd ("Fade End", Range(0.0, 1.0)) = 0.90

        [Header(Rounded Corners)]
        _CornerRadius ("Corner Radius", Range(0.001, 0.3)) = 0.08

        [Header(Top Gold Accent)]
        _TopAccentOffset ("Abstand von oberer Kante", Range(0.0, 0.15)) = 0.03
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
            float _MaxBlur;
            float _FadeStart;
            float _FadeEnd;
            float _CornerRadius;

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

                // 1. Echte kreisförmige Rundung ohne Knick / Diagonale Kante
                // Distanz von der jeweiligen Kante (0 bis 0.5)
                float2 distToEdge = min(input.uv, 1.0 - input.uv);
                
                // Nur im Eckbereich (wo beide Achsen kleiner als der Radius sind) kreisförmig runden
                float2 cornerDelta = max(float2(0.0, 0.0), _CornerRadius - distToEdge);
                float cornerDist = length(cornerDelta);
                
                // Sanftes Ausblenden an den runden Ecken (keine harten Linien)
                float cornerAlpha = 1.0 - smoothstep(0.0, _CornerRadius, cornerDist);

                // 2. Cinematische Vignette (Verlauf nach rechts)
                float vignetteFactor = 1.0 - smoothstep(_FadeStart, _FadeEnd, input.uv.x);
                float totalAlpha = vignetteFactor * cornerAlpha;

                // Wenn Pixel komplett unsichtbar sind -> abbrechen
                if (totalAlpha <= 0.001) return half4(0, 0, 0, 0);

                // 3. Blur abhängig von der Vignette skalieren
                float currentBlur = _MaxBlur * vignetteFactor;
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

                // 4. Panel abdunkeln
                half4 blended = lerp(col, _DimColor, _DimColor.a * vignetteFactor);

                // 5. Gold-Akzent NUR OBEN
                float targetY = 1.0 - _TopAccentOffset;
                float distToTop = abs(input.uv.y - targetY);
                
                float goldLine = 1.0 - smoothstep(0.0, _TopAccentThickness, distToTop);

                // Leiste blendet links sanft vor der Rundung aus, damit sie oben links nicht herausschaut
                goldLine *= smoothstep(_CornerRadius * 0.5, _CornerRadius * 1.2, input.uv.x);

                // Verläuft nach rechts weich synchron mit der Vignette aus
                goldLine *= vignetteFactor * _GoldColor.a;

                // Auf das Bild auftragen
                half3 finalGold = _GoldColor.rgb * _TopAccentBrightness;
                blended.rgb = lerp(blended.rgb, finalGold, goldLine);

                blended.a = totalAlpha;

                return blended;
            }
            ENDHLSL
        }
    }
}