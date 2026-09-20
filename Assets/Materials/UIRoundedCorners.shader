Shader "UI/RoundedCorners"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (0.1, 0.12, 0.15, 0.9)
        
        // Bereich 0.005 (ganz leicht gerundet) bis 0.15 (weich gerundet)
        _CornerRadius ("Corner Softness / Radius", Range(0.005, 0.15)) = 0.03
        
        _BorderColor ("Border Color", Color) = (0.8, 0.7, 0.4, 0.5)
        _BorderWidth ("Border Width", Range(0.0, 0.05)) = 0.005

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
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

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
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
                float2 uv       : TEXCOORD0;
            };

            fixed4 _Color;
            fixed4 _BorderColor;
            float _CornerRadius;
            float _BorderWidth;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.uv = v.texcoord;
                // v.color enthält die Farbe und das Alpha von Canvas / CanvasGroup!
                OUT.color = v.color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Basis-Farbe mit Vertex-Tint (wichtig für CanvasGroup Alpha!)
                fixed4 baseColor = _Color * fixed4(IN.color.rgb, 1.0);
                fixed4 borderColor = _BorderColor * fixed4(IN.color.rgb, 1.0);

                // Abstand zur nächsten Kante
                float2 distToEdge = min(IN.uv, 1.0 - IN.uv);

                // Ecken-Berechnung
                float2 cornerDelta = max(float2(0.0, 0.0), _CornerRadius - distToEdge);
                float cornerDist = length(cornerDelta);

                // Anti-Aliasing
                float aa = fwidth(cornerDist) + 0.001;
                float alphaMask = 1.0 - smoothstep(_CornerRadius - aa, _CornerRadius + aa, cornerDist);

                // Border-Maske
                float borderMask = 1.0 - smoothstep(_BorderWidth - aa, _BorderWidth + aa, min(distToEdge.x, distToEdge.y));
                fixed4 finalColor = lerp(baseColor, borderColor, borderMask);

                // Hier wird das CanvasGroup-Alpha (IN.color.a) garantiert sauber eingerechnet:
                finalColor.a *= alphaMask * IN.color.a;

                clip(finalColor.a - 0.001);

                return finalColor;
            }
            ENDCG
        }
    }
}