Shader "Custom/TransparentShadowOnly"
{
    Properties
    {
        _ShadowOpacity ("Shadow Opacity", Range(0, 1)) = 0.5 // Opacity of the shadow
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 200

        // Pass to render the object as fully transparent
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ColorMask 0 // Disable color writing (fully transparent)
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(0, 0, 0, 0); // Fully transparent
            }
            ENDCG
        }

        // Pass to render shadows with custom opacity
        Pass
        {
            Tags { "LightMode"="ShadowCaster" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

            float _ShadowOpacity;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                V2F_SHADOW_CASTER;
            };

            v2f vert (appdata v)
            {
                v2f o;
                TRANSFER_SHADOW_CASTER_NOPOS(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
                return fixed4(0, 0, 0, _ShadowOpacity); // Black shadow with custom opacity
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}