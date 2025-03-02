Shader "Custom/ProceduralWireframeFaces"
{
    Properties
    {
        _FillColor("Fill colour", color) = (1.0, 1.0, 1.0, 1.0)
        _SurfaceOpacity("Surface Opacity", Range(0, 1)) = 0.5
        _SpecColor("Specular Color", Color) = (1,1,1,1)
        _Shininess("Shininess", Range(0.01, 1)) = 0.7
        _LightColor("Light Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Cull Back
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            Tags { "RenderType"="Transparent" "Queue"="Transparent" }

            CGPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom
            
            #include "UnityCG.cginc"

            struct v2g
            {
                float4 vertex : SV_POSITION;
                uint vid : TEXCOORD0;
                float3 normal : NORMAL;  // Add normal
            };

            struct g2f 
            {
                float4 pos : SV_POSITION;
                // float3 barycentric : TEXCOORD0;
                // float3 worldPos : TEXCOORD1;  // Add world position
                float3 worldNormal : NORMAL;  // Add world normal
            };

            struct VertexProperties {
                float3 pos;
                float3 normal;
                uint colorIds[4];
                float3 posMod;
                float4 rotMod;
                float3 scaleMod;
                uint colorModIds[4];
            };

            StructuredBuffer<VertexProperties> _VertexProps;
            StructuredBuffer<int> _IndexBuffer;
            float4x4 _ObjectToWorld;

            v2g vert(uint vid : SV_VertexID)
            {
                v2g o;
                int index = _IndexBuffer[vid];
                float3 worldPos = mul(_ObjectToWorld, float4(_VertexProps[index].pos + _VertexProps[index].posMod, 1.0)).xyz;
                o.vertex = UnityWorldToClipPos(worldPos);
                o.vid = vid;
                o.normal = UnityObjectToWorldNormal(_VertexProps[index].normal);  // Transform normal to world space
                return o;
            }

            float3 RotateVectorByQuaternion(float4 q, float3 v)
            {
                float3 qv = cross(q.xyz, v);
                return v + 2.0 * cross(q.xyz, qv + q.w * v);
            }

            float3 EvaluateCubicBezierTriangle(
                float3 V0, float3 V1, float3 V2,
                float3 CPE0A, float3 CPE0B,
                float3 CPE1A, float3 CPE1B,
                float3 CPE2A, float3 CPE2B,
                float3 CPCenter,
                float u, float v, float w)
            {
                // Cubic Bézier triangle formula with more robust blending
                return 
                    pow(w, 3) * V0 + 
                    3 * pow(w, 2) * u * CPE0A + 
                    3 * pow(u, 2) * w * CPE0B +
                    pow(u, 3) * V1 + 
                    3 * pow(u, 2) * v * CPE1A + 
                    3 * pow(v, 2) * u * CPE1B + 
                    pow(v, 3) * V2 +
                    3 * pow(v, 2) * w * CPE2A + 
                    3 * pow(w, 2) * v * CPE2B + 
                    6 * w * u * v * CPCenter;
            }

            float3 InterpolateNormal(float3 N0, float3 N1, float3 N2, float u, float v, float w)
            {
                // Barycentric interpolation of normals
                float3 interpolatedNormal = N0 * w + N1 * u + N2 * v;
                return normalize(interpolatedNormal);
            }

            
            [maxvertexcount(3 * 40)]
            void geom(triangle v2g IN[3], inout TriangleStream<g2f> triStream)
            {
                int numSegments = 6; // Increased resolution
        
                // Original triangle vertices
                float3 V0 = mul(_ObjectToWorld, float4(_VertexProps[_IndexBuffer[IN[0].vid]].pos + _VertexProps[_IndexBuffer[IN[0].vid]].posMod, 1.0)).xyz;
                float3 V1 = mul(_ObjectToWorld, float4(_VertexProps[_IndexBuffer[IN[1].vid]].pos + _VertexProps[_IndexBuffer[IN[1].vid]].posMod, 1.0)).xyz;
                float3 V2 = mul(_ObjectToWorld, float4(_VertexProps[_IndexBuffer[IN[2].vid]].pos + _VertexProps[_IndexBuffer[IN[2].vid]].posMod, 1.0)).xyz;

                float3 N0 = IN[0].normal;
                float3 N1 = IN[1].normal;
                float3 N2 = IN[2].normal;

                float3 P0 = mul(_ObjectToWorld, float4(_VertexProps[_IndexBuffer[IN[0].vid]].pos, 1.0)).xyz;
                float3 P1 = mul(_ObjectToWorld, float4(_VertexProps[_IndexBuffer[IN[1].vid]].pos, 1.0)).xyz;
                float3 P2 = mul(_ObjectToWorld, float4(_VertexProps[_IndexBuffer[IN[2].vid]].pos, 1.0)).xyz;

        
                // Compute control points for cubic Bézier surface
                float3 CPE0A = V0 + RotateVectorByQuaternion(_VertexProps[_IndexBuffer[IN[0].vid]].rotMod, P1 - P0);
                float3 CPE0B = V1 + RotateVectorByQuaternion(_VertexProps[_IndexBuffer[IN[1].vid]].rotMod, P0 - P1);
                float3 CPE1A = V1 + RotateVectorByQuaternion(_VertexProps[_IndexBuffer[IN[1].vid]].rotMod, P2 - P1);
                float3 CPE1B = V2 + RotateVectorByQuaternion(_VertexProps[_IndexBuffer[IN[2].vid]].rotMod, P1 - P2);
                float3 CPE2A = V2 + RotateVectorByQuaternion(_VertexProps[_IndexBuffer[IN[2].vid]].rotMod, P0 - P2);
                float3 CPE2B = V0 + RotateVectorByQuaternion(_VertexProps[_IndexBuffer[IN[0].vid]].rotMod, P2 - P0);
        
                float3 CPCenter = (V0 + V1 + V2) / 3.0;
        
                // Iterate through segments to create a grid of triangles
                for (int u = 0; u < numSegments; u++)
                {
                    for (int v = 0; v < numSegments; v++)
                    {
                        if (v < numSegments - u) {
                            // Calculate the barycentric coordinates for the first triangle in the grid
                            float uT0 = (float)u / (float)numSegments;
                            float vT0 = (float)v / (float)numSegments;
                            float wT0 = 1.0f - uT0 - vT0;

                            // Second triangle: adjacent in the x-direction
                            float uT1 = (float)(u + 1) / (float)numSegments;
                            float vT1 = (float)v / (float)numSegments;
                            float wT1 = 1.0f - uT1 - vT1;

                            // Third triangle: adjacent in the y-direction
                            float uT2 = (float)u / (float)numSegments;
                            float vT2 = (float)(v + 1) / (float)numSegments;
                            float wT2 = 1.0f - uT2 - vT2;

                            // Compute Bézier surface points
                            float3 P0 = EvaluateCubicBezierTriangle(V0, V1, V2, 
                                CPE0A, CPE0B, CPE1A, CPE1B, CPE2A, CPE2B, 
                                CPCenter, uT0, vT0, wT0);
            
                            float3 P1 = EvaluateCubicBezierTriangle(V0, V1, V2, 
                                CPE0A, CPE0B, CPE1A, CPE1B, CPE2A, CPE2B, 
                                CPCenter, uT1, vT1, wT1);
            
                            float3 P2 = EvaluateCubicBezierTriangle(V0, V1, V2, 
                                CPE0A, CPE0B, CPE1A, CPE1B, CPE2A, CPE2B, 
                                CPCenter, uT2, vT2, wT2);
            
                                float3 interpolatedN0 = InterpolateNormal(N0, N1, N2, uT0, vT0, wT0);
                                float3 interpolatedN1 = InterpolateNormal(N0, N1, N2, uT1, vT1, wT1);
                                float3 interpolatedN2 = InterpolateNormal(N0, N1, N2, uT2, vT2, wT2);
    
                                g2f o0, o1, o2;
                            
                                o0.pos = UnityWorldToClipPos(float4(P0, 1.0));
                                // o0.barycentric = float3(uT0, vT0, wT0);
                                
                                o1.pos = UnityWorldToClipPos(float4(P1, 1.0));
                                // o1.barycentric = float3(uT1, vT1, wT1);
                                
                                o2.pos = UnityWorldToClipPos(float4(P2, 1.0));
                                // o2.barycentric = float3(uT2, vT2, wT2);

                                // o0.worldPos = P0;
                                o0.worldNormal = normalize(cross(P1 - P0, P2 - P0));  // Calculate face normal
                                
                                // o1.worldPos = P1;
                                o1.worldNormal = o0.worldNormal;
                                
                                // o2.worldPos = P2;
                                o2.worldNormal = o0.worldNormal;
                    
                                triStream.Append(o0);
                                triStream.Append(o1);
                                triStream.Append(o2);
                                triStream.RestartStrip();
                        } else {
                            // Triangular region - key changes here
                            float uT0 = (float)(numSegments - u) / (float)numSegments;
                            float vT0 = (float)(numSegments - v) / (float)numSegments;
                            float wT0 = 1.0f - uT0 - vT0;

                            // Second triangle: adjacent in the x-direction
                            float uT1 = (float)(numSegments - u - 1) / (float)numSegments;
                            float vT1 = (float)(numSegments - v) / (float)numSegments;
                            float wT1 = 1.0f - uT1 - vT1;

                            // Third triangle: adjacent in the y-direction
                            float uT2 = (float)(numSegments - u) / (float)numSegments;
                            float vT2 = (float)(numSegments - v - 1) / (float)numSegments;
                            float wT2 = 1.0f - uT2 - vT2;

                            // Compute Bézier surface points
                            float3 P0 = EvaluateCubicBezierTriangle(V0, V1, V2, 
                                CPE0A, CPE0B, CPE1A, CPE1B, CPE2A, CPE2B, 
                                CPCenter, uT0, vT0, wT0);
                
                            float3 P1 = EvaluateCubicBezierTriangle(V0, V1, V2, 
                                CPE0A, CPE0B, CPE1A, CPE1B, CPE2A, CPE2B, 
                                CPCenter, uT1, vT1, wT1);
                
                            float3 P2 = EvaluateCubicBezierTriangle(V0, V1, V2, 
                                CPE0A, CPE0B, CPE1A, CPE1B, CPE2A, CPE2B, 
                                CPCenter, uT2, vT2, wT2);
                
                            g2f o0, o1, o2;
                            
                            o0.pos = UnityWorldToClipPos(float4(P0, 1.0));
                            // o0.barycentric = float3(uT0, vT0, wT0);
                            
                            o1.pos = UnityWorldToClipPos(float4(P1, 1.0));
                            // o1.barycentric = float3(uT1, vT1, wT1);
                            
                            o2.pos = UnityWorldToClipPos(float4(P2, 1.0));
                            // o2.barycentric = float3(uT2, vT2, wT2);

                            // o0.worldPos = P0;
                            o0.worldNormal = normalize(cross(P1 - P0, P2 - P0));  // Calculate face normal
                            
                            // o1.worldPos = P1;
                            o1.worldNormal = o0.worldNormal;
                            
                            // o2.worldPos = P2;
                            o2.worldNormal = o0.worldNormal;
                
                            triStream.Append(o0);
                            triStream.Append(o1);
                            triStream.Append(o2);
                            triStream.RestartStrip();
                        }
                    }
                }
            }
    

            fixed4 _FillColor;
            fixed4 _LightColor;
            fixed4 _SpecColor;
            float _Shininess;
            float _SurfaceOpacity;

            fixed4 frag(g2f i) : SV_Target
            {
                // float3 unitWidth = fwidth(i.barycentric);
                // float3 aliased = smoothstep(float3(0.0, 0.0, 0.0), unitWidth * 1.5, i.barycentric);
                
                // Modify alpha calculation
                // float alpha = min(min(i.barycentric.x, i.barycentric.y), i.barycentric.z);
                // alpha = 1.0 - smoothstep(0.0, 0.01, alpha);
            
                fixed4 surfaceColor = _FillColor;
                
                // Lighting calculations remain the same
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 viewDir = normalize(_WorldSpaceCameraPos);
                float3 halfDir = normalize(lightDir + viewDir);
            
                float ndl = max(0, dot(i.worldNormal, lightDir));
                float spec = pow(max(0, dot(i.worldNormal, halfDir)), _Shininess * 128);
            
                surfaceColor.rgb *= ndl * _LightColor.rgb;
                surfaceColor.rgb += spec * _SpecColor.rgb;
            
                // Use the calculated alpha with surface opacity
                surfaceColor.a = 0.5 * _SurfaceOpacity;
                return float4(0,0,0,0);
            }
            ENDCG
        }
    }
}