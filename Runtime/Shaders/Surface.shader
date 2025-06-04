// Shader "Hidden/GsplEdit/Surface" 
// {
//     Properties 
//     {
//         _MainTex("Texture", 2D) = "white" {}
//         _Color("Color", Color) = (1,1,1,1)
//         _DepthTex("Depth Map", 2D) = "white" {}
//         _MinDepth("Min Depth", Range(0, 1)) = 0.0
//         _MaxDepth("Max Depth", Range(0, 1)) = 1.0
//         _NormalTex("Normal Map", 2D) = "bump" {}
//         _NormalContrast("Normal Contrast", Range(0.1, 4)) = 1.0
//         _DiffuseComponent("Diffuse Component", Range(0,1)) = 0.5
//         _ShadowStrength("Shadow Strength", Range(0, 1)) = 1.0
//         _AmbientLight("Ambient Light", Range(0, 1)) = 0.3
//         _RenderMode("Render Mode", Int) = 0
//         _NormalIntensity("Normal Overlay Intensity", Range(0,1)) = 0.0
//         _DepthDisplacement("Depth Overlay Intensity", Range(0,1)) = 0.0
//     }

//     SubShader 
//     {
//         Tags { "RenderType" = "Opaque" }

//         // Main pass with geometry shader for face normals
//         Pass 
//         {
//             Tags { "LightMode" = "ForwardBase" }
//             CGPROGRAM
//             #pragma vertex vert
//             #pragma geometry geom
//             #pragma fragment frag
//             #pragma multi_compile_fwdbase
//             #pragma multi_compile_fwdadd
//             #pragma target 4.0
//             #include "UnityCG.cginc"
//             #include "Lighting.cginc"
//             #include "AutoLight.cginc"
//             #include "UnityStandardBRDF.cginc"
            
//             sampler2D _MainTex;
//             sampler2D _NormalTex;
//             sampler2D _DepthTex;
//             fixed4 _Color;
//             float _NormalContrast;
//             half _DiffuseComponent;
//             half _ShadowStrength;
//             half _AmbientLight;
//             int _RenderMode;
//             float _NormalIntensity;
//             float _DepthDisplacement;
//             float _MinDepth;
//             float _MaxDepth;

//             struct appdata 
//             {
//                 float4 vertex : POSITION;
//                 float3 normal : NORMAL;
//                 float2 uv : TEXCOORD0;
//             };

//             struct v2g 
//             {
//                 float4 vertex : POSITION;
//                 float3 normal : NORMAL;
//                 float2 uv : TEXCOORD0;
//                 float3 worldPos : TEXCOORD1;
//             };

//             struct g2f 
//             {
//                 float4 pos : SV_POSITION;
//                 float2 uv : TEXCOORD0;
//                 float3 faceNormal : TEXCOORD1;
//                 float3 worldPos : TEXCOORD2;
//                 SHADOW_COORDS(3)
//             };

//             v2g vert(appdata v) 
//             {
//                 v2g o;
                
//                 // Apply depth displacement before world position calculation
//                 half depth = tex2Dlod(_DepthTex, float4(v.uv, 0, 0)).r * 0.37;
//                 half3 displaced = v.vertex.xyz + v.normal * _DepthDisplacement * (depth - 0.07);
//                 o.vertex = UnityObjectToClipPos(float4(displaced, 1.0));

//                 o.normal = v.normal;
//                 o.uv = v.uv;
//                 o.worldPos = mul(unity_ObjectToWorld, float4(displaced, 1.0)).xyz;
//                 return o;
//             }

//             [maxvertexcount(3)]
//             void geom(triangle v2g input[3], inout TriangleStream<g2f> stream) 
//             {
//                 // Calculate face normal in world space
//                 float3 edge1 = input[1].worldPos - input[0].worldPos;
//                 float3 edge2 = input[2].worldPos - input[0].worldPos;
//                 float3 faceNormal = normalize(cross(edge1, edge2));
                
//                 // Output each vertex with the same face normal
//                 for (int i = 0; i < 3; i++) 
//                 {
//                     g2f o;
//                     o.pos = input[i].vertex + float4(0,0,1,0); // temp fix for incorrect clipping planes
//                     o.uv = input[i].uv;
//                     o.faceNormal = faceNormal;
//                     o.worldPos = input[i].worldPos;
//                     TRANSFER_SHADOW(o);
//                     stream.Append(o);
//                 }
//                 stream.RestartStrip();
//             }

//             fixed4 frag(g2f i) : SV_Target 
//             {
//                 if (_RenderMode != 0) discard;
                
//                 // Sample textures
//                 fixed4 baseColor = tex2D(_MainTex, i.uv) * _Color;
//                 float3 normalMap = tex2D(_NormalTex, i.uv) * 2 -1;
//                 float3 depthMap = tex2D(_DepthTex, i.uv);
//                 normalMap.xy = lerp(float3(0,0,1), normalMap, _NormalIntensity);
                
//                 // Reconstruct tangent space from face normal
//                 float3 worldNormal = normalize(i.faceNormal);
//                 float3 worldUp = abs(worldNormal.y) < 0.999 ? float3(0,1,0) : float3(1,0,0);
//                 float3 worldTangent = normalize(cross(worldUp, worldNormal));
//                 float3 worldBitangent = cross(worldNormal, worldTangent);
                
//                 // Transform normal map to world space using face normal
//                 float3 finalNormal = normalize(
//                     normalMap.x * worldTangent +
//                     normalMap.y * worldBitangent +
//                     normalMap.z * worldNormal
//                 );
                
//                 // Lighting calculations
//                 float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
//                 half NdotL = max(0, dot(finalNormal, lightDir));
//                 float atten = SHADOW_ATTENUATION(i);
                
//                 // Combine lighting components
//                 half diffuseFactor = lerp(1.0, NdotL, _DiffuseComponent);
//                 half lightContribution = atten * diffuseFactor * _ShadowStrength;
//                 half modifiedAtten = lerp(_AmbientLight, 1.0, lightContribution);
//                 half4 c;
//                 c.rgb = float3(1,0,0);// baseColor.rgb * diffuseFactor * modifiedAtten * _LightColor0.rgb;
//                 c.a = 1;

//                 return c;
//             }
//             ENDCG
//         }

//         // Shadow caster pass
//         Pass 
//         {
//             Name "ShadowCaster"
//             Tags { "LightMode" = "ShadowCaster" }
            
//             CGPROGRAM
//             #pragma vertex vert
//             #pragma fragment frag
//             #pragma multi_compile_shadowcaster
//             #include "UnityCG.cginc"
            
//             struct v2f 
//             { 
//                 V2F_SHADOW_CASTER;
//                 float2 uv : TEXCOORD1;
//             };
            
//             v2f vert(appdata_base v)
//             {
//                 v2f o;
//                 o.uv = v.texcoord.xy;
//                 TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
//                 return o;
//             }
            
//             float4 frag(v2f i) : SV_Target
//             {
//                 SHADOW_CASTER_FRAGMENT(i)
//             }
//             ENDCG
//         }

//         // Unlit modes pass (color/normal/depth visualization)
//         Pass 
//         {
//             Name "UnlitModes"
//             Tags { "LightMode" = "Always" }

//             CGPROGRAM
//             #pragma vertex vert
//             #pragma fragment frag
//             #include "UnityCG.cginc"

//             sampler2D _MainTex;
//             sampler2D _NormalTex;
//             sampler2D _DepthTex;
//             fixed4 _Color;
//             float _NormalContrast;
//             int _RenderMode;
//             float _MinDepth;
//             float _MaxDepth;

//             struct appdata 
//             {
//                 float4 vertex : POSITION;
//                 float2 uv : TEXCOORD0;
//             };

//             struct v2f 
//             {
//                 float4 pos : SV_POSITION;
//                 float2 uv : TEXCOORD0;
//             };

//             v2f vert(appdata v) 
//             {
//                 v2f o;
//                 o.pos = UnityObjectToClipPos(v.vertex);
//                 o.uv = v.uv;
//                 return o;
//             }

//             fixed4 frag(v2f i) : SV_Target 
//             {
//                 if (_RenderMode == 0) discard;
            
//                 fixed4 baseColor = tex2D(_MainTex, i.uv) * _Color;
//                 float3 normalMap = UnpackNormal(tex2D(_NormalTex, i.uv));
//                 normalMap.xy *= _NormalContrast;
//                 half depth = tex2D(_DepthTex, i.uv).r;
//                 depth = (depth - _MinDepth) / (_MaxDepth - _MinDepth);
            
//                 if (_RenderMode == 1) 
//                 {
//                     return fixed4(baseColor.rgb, 1); // Color mode
//                 }
//                 else if (_RenderMode == 2) 
//                 {
//                     return fixed4(normalMap * 0.5 + 0.5, 1); // Normal mode
//                 }
//                 else if (_RenderMode == 3) 
//                 {
//                     return fixed4(depth, depth, depth, 1); // Depth mode
//                 }
            
//                 return fixed4(0,0,0,1);
//             }            
//             ENDCG
//         }
//     }
//     Fallback "Diffuse"
// }


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
        #pragma surface surf SimpleLambert fullforwardshadows addshadow vertex:vert 
        // tessellate:tessFunction
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
            v.vertex.xyz += vertexNormal * _DepthDisplacement * (depth - 0.3);
            v.color.rgb = vertexNormal;
        }

        inline half4 LightingSimpleLambert(SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
        {
            half NdotL = max(0, dot(s.Normal, lightDir));
            half diffuseFactor = lerp(1.0, NdotL, _DiffuseComponent);
            half lightContribution = atten * NdotL * _ShadowStrength;
            half modifiedAtten = lerp(_AmbientLight, 1.0, lightContribution);
            half4 c;
            c.rgb = s.Albedo.rgb * modifiedAtten * _LightColor0.rgb;
            c.a = 1;
            return c;
        }

    //     inline half4 LightingSimpleLambert(SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
    // {
    //     half NdotL = dot(s.Normal, half3(-lightDir.z, lightDir.z, lightDir.y));  // Lambert cosine term
    //     half diffuseFactor = lerp(1.0, NdotL, _DiffuseComponent);
        
    //     // Multiply by attenuation and shadow strength
    //     half lightContribution = atten * NdotL * _ShadowStrength;
        
    //     // Mix ambient light with direct light contribution
    //     half modifiedAtten = lerp(_AmbientLight, 1.0, lightContribution);
        
    //     half4 c;
    //     // Apply light color, albedo, and attenuation
    //     c.rgb = NdotL;//s.Albedo.rgb * modifiedAtten * _LightColor0.rgb;
    //     c.a = 1;
        
    //     return c;
    // }

        
        void surf(Input IN, inout SurfaceOutput o) {
            if (_RenderMode != 0) {
                o.Albedo = 0;
                o.Alpha = 0;
                return;
            }
        
            // Sample base color
            fixed4 baseColor = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            
            // Sample and process normal map properly
            float3 normalMap = (tex2D(_NormalTex, IN.uv_NormalTex)) * 2 - 1;
            normalMap = lerp(float3(0,0,1), normalMap, _NormalIntensity);

            half3 vertexNormal = normalize(IN.color) * 2.0 - 1.0;    
            float3 worldUp = float3(1,0,0);//abs(vertexNormal.y) < 0.999 ? float3(0,1,0) : float3(1,0,0);
            float3 worldTangent = normalize(cross(worldUp, vertexNormal));
            float3 worldBitangent = cross(vertexNormal, worldTangent);
            
            // Transform normal map to world space using face normal
            float3 finalNormal = normalize(
                normalMap.x * worldTangent +
                normalMap.y * worldBitangent +
                normalMap.z * vertexNormal
            );

            o.Albedo = baseColor.rgb;
            o.Normal = (finalNormal + 1) / 2;
            o.Alpha = baseColor.a;
        }
        
        ENDCG

               // Unlit modes pass (color/normal/depth visualization)
        Pass 
        {
            Name "UnlitModes"
            Tags { "LightMode" = "Always" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _NormalTex;
            sampler2D _DepthTex;
            fixed4 _Color;
            float _NormalContrast;
            int _RenderMode;
            float _MinDepth;
            float _MaxDepth;

            struct appdata 
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f 
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v) 
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target 
            {
                if (_RenderMode == 0) discard;
            
                fixed4 baseColor = tex2D(_MainTex, i.uv) * _Color;
                float3 normalMap = normalize(tex2D(_NormalTex, i.uv));
                normalMap.xy *= _NormalContrast;

            
                if (_RenderMode == 1) 
                {
                    return fixed4(baseColor.rgb, 1); // Color mode
                }
                else if (_RenderMode == 2) 
                {
                    return fixed4(normalMap, 1); // Normal mode
                }
                else if (_RenderMode == 3) 
                {
                    float rawDepth = tex2D(_DepthTex, i.uv).r;
                    float normalizedDepth = (rawDepth - _MinDepth) / (_MaxDepth - _MinDepth);
                    return lerp(fixed4(1,0,0,1), fixed4(0,0,1,1), normalizedDepth); // Depth mode
                }
            
                return fixed4(0,0,0,1);
            }            
            ENDCG
        }
    }

    Fallback Off
}