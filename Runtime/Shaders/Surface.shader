Shader "Hidden/GsplEdit/Surface" {
    Properties {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
        _DepthTex("Depth Map", 2D) = "white" {}
        _MinDepth("Min Depth", Range(0, 1)) = 0.0
        _MaxDepth("Max Depth", Range(0, 1)) = 1.0
        _NormalTex("Normal Map", 2D) = "bump" {}
        _NormalContrast("Normal Contrast", Range(0.1, 4)) = 1.0
        _DiffuseComponent("Diffuse Component", Range(0,1)) = 0.5
        _ShadowStrength("Shadow Strength", Range(0, 1)) = 1.0
        _AmbientLight("Ambient Light", Range(0, 1)) = 0.3
        _RenderMode("Render Mode", Int) = 0
        _NormalIntensity("Normal Overlay Intensity", Range(0,1)) = 0.0
        _DepthDisplacement("Depth Overlay Intensity", Range(0,1)) = 0.0
    }

    SubShader {
        Tags { "RenderType" = "Opaque" }

        // ===== Surface-Lit Mode =====
        CGPROGRAM
        #pragma target 5.0
        #pragma surface surf SimpleLambert fullforwardshadows addshadow vertex:vert tessellate:tessFunction
        #include "UnityCG.cginc"
        #include "Tessellation.cginc"

        sampler2D _MainTex;
        fixed4 _Color;
        sampler2D _NormalTex;
        float _NormalContrast;
        half _DiffuseComponent;
        half _ShadowStrength;
        half _AmbientLight;
        int _RenderMode;
        float _NormalIntensity;
        float _DepthDisplacement;
        sampler2D _DepthTex;
        float _MinDepth;
        float _MaxDepth;

        float tessFunction(appdata_full v0, appdata_full v1, appdata_full v2)
        {
            return 8.0;
        }
        

        struct Input {
            float2 uv_MainTex;
            float2 uv_NormalTex;
            float2 uv_DepthTex;
            float3 vertexNormal;
            fixed3 color : COLOR;
            INTERNAL_DATA
        };
        
        void vert(inout appdata_full v)
        {
            half3 vertexNormal = normalize(v.normal);

            half depth = tex2Dlod(_DepthTex, float4(v.texcoord.xy, 0, 0)).r * 0.37;
            v.vertex.xyz += vertexNormal * _DepthDisplacement * (sqrt(depth) - 0.3) * 0.2;
            v.color.rgb = vertexNormal * 0.5 + 0.5; // encode [-1,1] into [0,1]
        }

        inline half4 LightingSimpleLambert(SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
        {
            half NdotL = max(0, dot(s.Normal, lightDir));
            half diffuseFactor = lerp(1.0, NdotL, _DiffuseComponent);
            
            // Combine light attenuation with shadow strength and ambient
            half lightContribution = atten * NdotL * _ShadowStrength;
            half modifiedAtten = lerp(_AmbientLight, 1.0, lightContribution);
            
            half4 c;
            c.rgb = s.Albedo * diffuseFactor * _LightColor0.rgb * modifiedAtten * diffuseFactor;
            c.a = s.Alpha;
            return c;
            // return half4(s.Albedo, 1);
        }
        
        void surf(Input IN, inout SurfaceOutput o) {
            if (_RenderMode != 0) {
                o.Albedo = 0;
                o.Alpha = 0;
                return;
            }
        
            // Sample base color
            fixed4 baseColor = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            
            // Sample and process normal map properly
            float3 normalMap = normalize(tex2D(_NormalTex, IN.uv_NormalTex));

            half3 vertexNormal = normalize(IN.color * 2.0 - 1.0);    
                half3 up = abs(vertexNormal.y) < 0.999 ? half3(0,1,0) : half3(1,0,0);
            half3 tangent = normalize(cross(up, vertexNormal));
            half3 bitangent = cross(vertexNormal, tangent);
            half3 finalNormal = normalize(
                normalMap.x * tangent +
                normalMap.y * bitangent +
                normalMap.z * vertexNormal
            );

            float3 depth = tex2D(_DepthTex, IN.uv_DepthTex);

            o.Albedo = baseColor.rgb;
            o.Normal = finalNormal;
            o.Alpha = baseColor.a;
        }
        
        ENDCG

        // ===== Unlit Color/Normal/Depth Mode =====
        Pass {
            Name "UnlitModes"
            Tags { "LightMode" = "Always" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            sampler2D _DepthTex;
            float _MinDepth;
            float _MaxDepth;
            sampler2D _NormalTex;
            float _NormalContrast;
            int _RenderMode;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMALS;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMALS;
            };

            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.normal = v.normal;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target 
            {
                if (_RenderMode == 0) discard;
            
                fixed4 baseColor = tex2D(_MainTex, i.uv) * _Color;
                half3 normalMap = tex2D(_NormalTex, i.uv);
                half depth = tex2D(_DepthTex, i.uv).r * 0.37;
                depth -= 0.3;
                depth /= 0.37;
            
                // --- COMBINE NORMALS PROPERLY ---
                // Remap normal map from [0,1] to [-1,1]
                // normalMap = normalMap * 2 - 1;
            
                // Assume i.normal is the interpolated vertex normal in view space
                half3 vertexNormal = (i.normal);
            
                // If you don't have tangent/bitangent, assume an approximate tangent space
                // Create a fake tangent basis
                half3 up = abs(vertexNormal.y) < 0.999 ? half3(0,1,0) : half3(1,0,0);
                half3 tangent = normalize(cross(up, vertexNormal));
                half3 bitangent = cross(vertexNormal, tangent);
            
                // Transform normal from tangent space to view/world space
                half3 finalNormal = normalize(
                    normalMap.x * tangent +
                    normalMap.y * bitangent +
                    normalMap.z * vertexNormal
                );
            
                if (_RenderMode == 1) {
                    return fixed4(baseColor.rgb, baseColor.a); // Color mode
                }
                else if (_RenderMode == 2) {
                    return fixed4(finalNormal, 1); // Normal mode (visualize normal)
                }
                else if (_RenderMode == 3) {
                    if (depth < 0) {
                        return fixed4(abs(depth), abs(depth), abs(depth), 1); // Depth mode
                    } else {
                        return fixed4(depth, 0, 0, 1); // Depth mode 
                    }
                }
            
                return 0;
            }            
            ENDCG
        }
    }

    Fallback Off
}