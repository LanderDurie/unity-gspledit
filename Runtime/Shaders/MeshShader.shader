Shader "Custom/ProceduralMeshShader"
{
    Properties
    {
        _Color("Main Color", Color) = (1.0, 1.0, 1.0, 0.7)
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
        _ReceiveShadows("Receive Shadows", Float) = 1
        _ReflectionStrength("Reflection Strength", Range(0,1)) = 0.5
        _FresnelPower("Fresnel Power", Range(0,10)) = 5.0
        _CubemapBlur("Cubemap Blur", Range(0,7)) = 0
        [NoScaleOffset] _CubeMap("Reflection Cubemap", Cube) = "black" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent+100" "RenderType"="Transparent" "DisableBatching"="True" }
        LOD 200

        // Depth prepass (write to depth buffer)
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            ZWrite On
            ZTest LEqual
            Cull Back
            ColorMask 0 // Disable color writing

            CGPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct VertexProperties {
                float3 pos;
                float3 posMod;
                float3 normal;
                float2 uv;
            };

            StructuredBuffer<VertexProperties> _MeshVertexPos;
            StructuredBuffer<int> _IndexBuffer;
            float4x4 _ObjectToWorld;

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(uint vid : SV_VertexID)
            {
                v2f o;
                int index = _IndexBuffer[vid];

                float3 worldPos = mul(_ObjectToWorld, float4(_MeshVertexPos[index].pos + _MeshVertexPos[index].posMod, 1.0)).xyz;
                o.pos = UnityWorldToClipPos(worldPos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return fixed4(0, 0, 0, 0); // No color output
            }
            ENDCG
        }

        // Main pass (renders the mesh with transparency, reflections, and lighting)
        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            Cull Back
            ZWrite Off // Disable depth writing for transparency
            ZTest LEqual // Ensure depth testing works correctly
            Blend SrcAlpha OneMinusSrcAlpha // Enable alpha blending
            Offset 0, -1 // Apply a small depth bias

            CGPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            #include "AutoLight.cginc"
            #include "UnityStandardUtils.cginc"

            struct VertexProperties {
                float3 pos;
                float3 posMod;
                float3 normal;
                float2 uv;
            };

            StructuredBuffer<VertexProperties> _MeshVertexPos;
            StructuredBuffer<int> _IndexBuffer;
            float4x4 _ObjectToWorld;
            float4 _Color;
            float _Glossiness;
            float _Metallic;
            float _ReflectionStrength;
            float _FresnelPower;
            float _CubemapBlur;
            samplerCUBE _CubeMap;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldViewDir : TEXCOORD2;
                SHADOW_COORDS(3)  // Shadow coordinates
                UNITY_FOG_COORDS(4)
            };

            v2f vert(uint vid : SV_VertexID)
            {
                v2f o;
                int index = _IndexBuffer[vid];

                float3 localPos = _MeshVertexPos[index].pos + _MeshVertexPos[index].posMod;
                o.worldPos = mul(_ObjectToWorld, float4(localPos, 1.0)).xyz;
                o.pos = UnityWorldToClipPos(o.worldPos);

                o.worldNormal = UnityObjectToWorldNormal(_MeshVertexPos[index].normal);
                o.worldViewDir = normalize(UnityWorldSpaceViewDir(o.worldPos));

                TRANSFER_SHADOW(o);  // Transfer shadow data
                UNITY_TRANSFER_FOG(o, o.pos);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 worldNormal = normalize(i.worldNormal);
                float3 viewDir = normalize(i.worldViewDir);

                // Lighting
                float3 lightDir = normalize(UnityWorldSpaceLightDir(i.worldPos));
                float3 halfDir = normalize(lightDir + viewDir);

                // Calculate basic lighting
                float ndotl = max(0, dot(worldNormal, lightDir));
                float ndoth = max(0, dot(worldNormal, halfDir));

                // Shadows - properly sample shadow map
                fixed shadow = SHADOW_ATTENUATION(i);

                // Light attenuation (for point/spot lights)
                UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);

                // Ambient lighting
                float3 ambient = ShadeSH9(float4(worldNormal, 1));

                // Diffuse lighting with shadows
                float3 diffuse = _LightColor0.rgb * ndotl * shadow * atten;

                // Specular highlights
                float specularPower = exp2(_Glossiness * 11) + 2;
                float specular = pow(ndoth, specularPower) * _Glossiness;

                // Reflections
                float fresnelEffect = pow(1.0 - saturate(dot(worldNormal, viewDir)), _FresnelPower);
                float reflectionStrength = _ReflectionStrength * fresnelEffect;

                float mip = (1.0 - _Glossiness) * _CubemapBlur;
                float3 reflectDir = reflect(-viewDir, worldNormal);
                float3 reflection = texCUBElod(_CubeMap, float4(reflectDir, mip)).rgb * reflectionStrength;

                // Combine lighting components
                float3 finalColor = _Color.rgb * (ambient + diffuse) +
                                (_LightColor0.rgb * specular * shadow * atten) +
                                (reflection * (1.0 - _Metallic * 0.5));

                fixed4 finalOutput = fixed4(finalColor, _Color.a);

                // Apply fog
                UNITY_APPLY_FOG(i.fogCoord, finalOutput);

                return finalOutput;
            }
            ENDCG
        }

        // Shadow caster pass - CORRECTED for procedural mesh
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Back

            CGPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster

            #include "UnityCG.cginc"

            struct VertexProperties {
                float3 pos;
                float3 posMod;
                float3 normal;
                float2 uv;
            };

            StructuredBuffer<VertexProperties> _MeshVertexPos;
            StructuredBuffer<int> _IndexBuffer;
            float4x4 _ObjectToWorld;

            struct v2f
            {
                V2F_SHADOW_CASTER;
            };

            v2f vert(uint vid : SV_VertexID)
            {
                v2f o;
                int index = _IndexBuffer[vid];

                float3 localPos = _MeshVertexPos[index].pos + _MeshVertexPos[index].posMod;
                float3 worldPos = mul(_ObjectToWorld, float4(localPos, 1.0)).xyz;
                float3 worldNormal = UnityObjectToWorldNormal(_MeshVertexPos[index].normal);
                
                // Manually setting up the shadow cast position without using the built-in macros
                // that rely on 'v' which doesn't exist in this procedural mesh setup
                #if defined(SHADOWS_DEPTH)
                    // Directional light shadows
                    o.pos = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
                    #if defined(UNITY_REVERSED_Z)
                        o.pos.z += unity_LightShadowBias.x / o.pos.w;
                    #else
                        o.pos.z -= unity_LightShadowBias.x / o.pos.w;
                    #endif
                    
                    float clamped = max(o.pos.z, 0.0);
                    o.pos.z = lerp(o.pos.z, clamped, unity_LightShadowBias.y);
                #endif
                
                #if defined(SHADOWS_CUBE)
                    // Point light shadows
                    o.pos = UnityObjectToClipPos(float4(localPos, 1.0));
                    o.vec = mul(_ObjectToWorld, float4(localPos, 1.0)).xyz - _LightPositionRange.xyz;
                #endif
                
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
        
        // Add another pass for additional lights - CORRECTED for procedural mesh
        Pass
        {
            Tags{"LightMode" = "ForwardAdd"}
            Blend One One
            ZWrite Off
            ZTest LEqual
            
            CGPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdadd_fullshadows
            
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            #include "AutoLight.cginc"
            
            struct VertexProperties {
                float3 pos;
                float3 posMod;
                float3 normal;
                float2 uv;
            };

            StructuredBuffer<VertexProperties> _MeshVertexPos;
            StructuredBuffer<int> _IndexBuffer;
            float4x4 _ObjectToWorld;
            float4 _Color;
            float _Glossiness;
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : NORMAL;
                float4 lightPos : TEXCOORD1; // Light coordinates for shadows/attenuation
            };

            v2f vert(uint vid : SV_VertexID)
            {
                v2f o;
                int index = _IndexBuffer[vid];

                float3 localPos = _MeshVertexPos[index].pos + _MeshVertexPos[index].posMod;
                o.worldPos = mul(_ObjectToWorld, float4(localPos, 1.0)).xyz;
                o.pos = UnityWorldToClipPos(o.worldPos);
                o.worldNormal = UnityObjectToWorldNormal(_MeshVertexPos[index].normal);
                
                // Manually handle light position for attenuation instead of using UNITY_TRANSFER_LIGHTING
                // which relies on 'v'
                float3 lightDir = _WorldSpaceLightPos0.xyz - o.worldPos * _WorldSpaceLightPos0.w;
                
                #if defined(POINT) || defined(SPOT)
                    // Point or Spot light calculations
                    o.lightPos = mul(unity_WorldToLight, float4(o.worldPos, 1.0));
                #else
                    // Directional light
                    o.lightPos = float4(0, 0, 0, 0);
                #endif
                
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                float3 worldNormal = normalize(i.worldNormal);
                float3 lightDir;
                
                #if defined(POINT) || defined(SPOT)
                    lightDir = normalize(_WorldSpaceLightPos0.xyz - i.worldPos);
                #else
                    lightDir = normalize(_WorldSpaceLightPos0.xyz);
                #endif
                
                float3 viewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
                float3 halfDir = normalize(lightDir + viewDir);
                
                float ndotl = max(0, dot(worldNormal, lightDir));
                
                // Manually calculate attenuation based on light type
                float atten = 1.0;
                
                #if defined(POINT)
                    float distance = length(_WorldSpaceLightPos0.xyz - i.worldPos);
                    atten = 1.0 / (1.0 + distance * distance);
                    // You could also sample the light texture:
                    // atten = tex2D(_LightTexture0, dot(i.lightPos.xyz, i.lightPos.xyz).xx).UNITY_ATTEN_CHANNEL;
                #elif defined(SPOT)
                    float distance = length(_WorldSpaceLightPos0.xyz - i.worldPos);
                    atten = 1.0 / (1.0 + distance * distance);
                    // For spot, you'd normally also compute the cone factor and sample texture
                #endif
                
                float3 diffuse = _LightColor0.rgb * ndotl * atten * _Color.rgb;
                
                // Specular
                float specularPower = exp2(_Glossiness * 11) + 2;
                float specular = pow(max(0, dot(worldNormal, halfDir)), specularPower) * _Glossiness;
                float3 specularColor = _LightColor0.rgb * specular * atten;
                
                return fixed4(diffuse + specularColor, 0);
            }
            ENDCG
        }
    }
    FallBack "Standard"
}