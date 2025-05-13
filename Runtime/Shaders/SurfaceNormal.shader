Shader "Hidden/NormalColor_Unlit" {
    Properties {
        _NormalTex("Normal Map", 2D) = "bump" {}
        _ColorMultiplier("Color Multiplier", Range(0.5, 2)) = 1.0
        _Brightness("Brightness", Range(0, 1)) = 0.5
        _Contrast("Contrast", Range(0, 2)) = 1.0
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _NormalTex;
            half _ColorMultiplier;
            half _Brightness;
            half _Contrast;
            
            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target {
                // Sample the normal map
                fixed4 normalSample = tex2D(_NormalTex, i.uv);
                float3 normalColor = normalSample.rgb;
                
                // Apply contrast
                normalColor = (normalColor - 0.5) * _Contrast + 0.5;
                
                // Apply brightness and multiplier
                normalColor = normalColor * _ColorMultiplier + _Brightness - 0.5;
                
                return fixed4(saturate(normalColor), 1);
            }
            ENDCG
        }
    }
    Fallback Off
}