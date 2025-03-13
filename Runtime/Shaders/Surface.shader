Shader "Custom/FlatLitCharacter" {
    Properties {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1) // Uniform base color
        _DiffuseComponent("Diffuse Component", Range(0,1)) = 0.8 // Controls mix between flat and diffuse shading
    }
    SubShader {
        Tags { "RenderType" = "Opaque" }
        CGPROGRAM
        #pragma surface surf SimpleLambert fullforwardshadows

        #include "UnityCG.cginc"

        fixed4 _Color;
        sampler2D _MainTex;
        half _DiffuseComponent; // Blending factor between flat and diffuse shading

        half4 LightingSimpleLambert(SurfaceOutput s, half3 lightDir, half atten) {
            half NdotL = max(0, dot(s.Normal, lightDir));
            half diffuseFactor = lerp(1.0, NdotL, _DiffuseComponent); // Blend between flat (1) and Lambert (NdotL)
            
            half4 c;
            c.rgb = s.Albedo * _LightColor0.rgb * atten * diffuseFactor;
            c.a = s.Alpha;
            return c;
        }

        struct Input {
            float2 uv_MainTex;
        };

        void surf(Input IN, inout SurfaceOutput o) {
            o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb * _Color.rgb;
            o.Normal = float3(0, 0, 1); // Default normal for flat shading
        }
        ENDCG
    }
    Fallback "Diffuse"
}
