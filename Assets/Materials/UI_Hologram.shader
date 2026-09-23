Shader "UI/Hologram"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _HoloColor ("Hologram Color", Color) = (0.0, 0.9, 1.0, 1.0)
        _ScanlineDensity ("Scanline Density", Float) = 40.0
        _ScanlineSpeed ("Scanline Speed", Float) = 2.0
        _FlickerSpeed ("Flicker Speed", Float) = 15.0
        _FlickerIntensity ("Flicker Intensity", Range(0, 0.3)) = 0.08
        _BaseAlpha ("Base Alpha", Range(0, 1)) = 0.85
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _HoloColor;
            float _ScanlineDensity;
            float _ScanlineSpeed;
            float _FlickerSpeed;
            float _FlickerIntensity;
            float _BaseAlpha;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, IN.texcoord);

                // 1. In Graustufen umwandeln (Luminanz)
                float lum = dot(texColor.rgb, float3(0.299, 0.587, 0.114));

                // 2. Horizontale Scanlines berechnen
                float lines = sin((IN.texcoord.y + _Time.y * _ScanlineSpeed * 0.1) * _ScanlineDensity * 3.14159);
                lines = (lines * 0.5 + 0.5); // Zwischen 0 und 1 normalisieren
                lines = lerp(0.65, 1.15, lines);

                // 3. Subtiles Flackern
                float flicker = 1.0 - (sin(_Time.y * _FlickerSpeed) * _FlickerIntensity);

                // 4. Farbe zusammensetzen: Graustufen * Hologrammfarbe * Effekte
                fixed3 finalRGB = lum * _HoloColor.rgb * lines * flicker;

                // 5. Alpha: Beachtet die Textur-Alpha und wird an dunklen Stellen leicht transparenter
                float alpha = texColor.a * _BaseAlpha * (0.4 + lum * 0.6) * IN.color.a;

                return fixed4(finalRGB, alpha);
            }
            ENDCG
        }
    }
}