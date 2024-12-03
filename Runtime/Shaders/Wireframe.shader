Shader "Custom/ProceduralWireframe"
{
    Properties
    {
        _WireframeFrontColour("Wireframe front colour", color) = (1.0, 1.0, 1.0, 1.0)
        _WireframeBackColour("Wireframe back colour", color) = (1.0, 1.0, 1.0, 1.0)
        _WireframeAliasing("Wireframe aliasing", float) = 1.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent"}
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha

                // Front faces
        Pass
        {
            // Cull Front
            // CGPROGRAM
            // #pragma target 5.0
            // #pragma vertex vert
            // #pragma fragment frag
            // #pragma geometry geom
            
            // #include "UnityCG.cginc"

            // struct v2g
            // {
            //     float4 vertex : SV_POSITION;
            //     uint vid : TEXCOORD0;
            // };

            // struct g2f 
            // {
            //     float4 pos : SV_POSITION;
            //     float3 barycentric : TEXCOORD0;
            // };

            // struct VertexProperties {
            //     float3 pos;          // 12 bytes (3 * 4)
            //     float3 normal;       // 12 bytes (3 * 4)
            //     int4 colors;         // 16 bytes (4 * 4)
            //     float3 posMod;       // 12 bytes (3 * 4)
            //     float4 rotMod;       // 16 bytes (4 * 4)
            //     float3 scaleMod;     // 12 bytes (3 * 4)
            //     float3 colorMods[4]; // 48 bytes (4 * 12)
            //     // Total size: 128 bytes
            // };

            // StructuredBuffer<VertexProperties> _VertexProps;
            // StructuredBuffer<int> _IndexBuffer;
            // float4x4 _ObjectToWorld;

            // v2g vert(uint vid : SV_VertexID)
            // {
            //     v2g o;
            //     // Get the actual vertex index from the index buffer
            //     int index = _IndexBuffer[vid];
            //     float3 worldPos = mul(_ObjectToWorld, float4(_VertexProps[index].pos + _VertexProps[index].posMod, 1.0)).xyz;
            //     o.vertex = UnityWorldToClipPos(worldPos);
            //     o.vid = vid;
            //     return o;
            // }
            
            // // Outputting 2 vertices per edge, each edge will be divided into 16 segments
            // [maxvertexcount(6)] 
            // void geom(triangle v2g IN[3], inout LineStream<g2f> lineStream)
            // {

            //                     // Loop over the edges of the triangle (3 edges in total)
            //             for (int edge = 0; edge < 3; edge++) 
            //             {
            //                 // Get the two vertices for the current edge
            //                 uint vid0 = edge;
            //                 uint vid1 = (edge + 1) % 3;

            //                 // Create the first vertex for the edge
            //                 g2f o;
            //                 o.pos = IN[vid0].vertex;  // Position of the first vertex of the edge
            //                 o.barycentric = float3(1.0, 0.0, 0.0);  // Barycentric coordinates for edge 1
            //                 lineStream.Append(o);

            //                 // Create the second vertex for the edge
            //                 o.pos = IN[vid1].vertex;  // Position of the second vertex of the edge
            //                 o.barycentric = float3(0.0, 1.0, 0.0);  // Barycentric coordinates for edge 2
            //                 lineStream.Append(o);
            //             }
            // }

            // fixed4 _WireframeBackColour;
            // float _WireframeAliasing;

            // fixed4 frag(g2f i) : SV_Target
            // {
            //     float3 unitWidth = fwidth(i.barycentric);
            //     float3 aliased = smoothstep(float3(0.0, 0.0, 0.0), unitWidth * _WireframeAliasing, i.barycentric);
            //     float alpha = 1 - min(aliased.x, min(aliased.y, aliased.z));
            //     return fixed4(_WireframeBackColour.rgb, alpha);
            // }
            // ENDCG
        }

        
        // Front faces
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
            };

            struct g2f 
            {
                float4 pos : SV_POSITION;
                float3 barycentric : TEXCOORD0;
            };

            struct VertexProperties {
                float3 pos;          // 12 bytes (3 * 4)
                float3 normal;       // 12 bytes (3 * 4)
                uint colorIds[4];         // 16 bytes (4 * 4)
                float3 posMod;       // 12 bytes (3 * 4)
                float4 rotMod;       // 16 bytes (4 * 4)
                float3 scaleMod;     // 12 bytes (3 * 4)
                uint colorModIds[4]; // 48 bytes (4 * 12)
                // Total size: 128 bytes
            };

            StructuredBuffer<VertexProperties> _VertexProps;
            StructuredBuffer<int> _IndexBuffer;
            float4x4 _ObjectToWorld;

            v2g vert(uint vid : SV_VertexID)
            {
                v2g o;
                // Get the actual vertex index from the index buffer
                int index = _IndexBuffer[vid];
                float3 worldPos = mul(_ObjectToWorld, float4(_VertexProps[index].pos + _VertexProps[index].posMod, 1.0)).xyz;
                o.vertex = UnityWorldToClipPos(worldPos);
                o.vid = vid;
                return o;
            }

            float3 RotateVectorByQuaternion(float4 q, float3 v)
            {
                // Quaternion rotation formula: v' = q * v * q^-1
                float3 qv = cross(q.xyz, v);
                return v + 2.0 * cross(q.xyz, qv + q.w * v);
            }
            
            // Outputting 2 vertices per edge, each edge will be divided into 16 segments
            [maxvertexcount(3 * 16)] 
            void geom(triangle v2g IN[3], inout LineStream<g2f> lineStream)
            {
                int numSegments = 16;

                // Loop over the edges of the triangle (3 edges in total)
                for (int edge = 0; edge < 3; edge++)
                {
                // Vertex indices for the current edge
                uint vid0 = IN[edge].vid;
                uint vid1 = IN[(edge + 1) % 3].vid;

                // Get positions and normals
                float3 P0 = mul(_ObjectToWorld, float4(_VertexProps[_IndexBuffer[vid0]].pos + _VertexProps[_IndexBuffer[vid0]].posMod, 1.0)).xyz;
                float3 P1 = mul(_ObjectToWorld, float4(_VertexProps[_IndexBuffer[vid1]].pos + _VertexProps[_IndexBuffer[vid1]].posMod, 1.0)).xyz;

                float3 PO0 = mul(_ObjectToWorld, float4(_VertexProps[_IndexBuffer[vid0]].pos, 1.0)).xyz;
                float3 PO1 = mul(_ObjectToWorld, float4(_VertexProps[_IndexBuffer[vid1]].pos, 1.0)).xyz;

                    
                float3 N0 = normalize(RotateVectorByQuaternion(_VertexProps[_IndexBuffer[vid0]].rotMod, 
                                                            _VertexProps[_IndexBuffer[vid0]].normal));
                float3 N1 = normalize(RotateVectorByQuaternion(_VertexProps[_IndexBuffer[vid1]].rotMod, 
                                                            _VertexProps[_IndexBuffer[vid1]].normal));


                float3 minBoundsBox = min(P0, P1);
                float3 maxBoundsBox = max(P0, P1);
                
                float3 boxSize = abs(P0 - P1);

                // Clamp the point to be within the box

                // Compute direction vector

                float3 a = RotateVectorByQuaternion(_VertexProps[_IndexBuffer[vid0]].rotMod, PO1 - PO0);
                float3 b = RotateVectorByQuaternion(_VertexProps[_IndexBuffer[vid1]].rotMod, PO0 - PO1);

                a = clamp(P0 + a, minBoundsBox, maxBoundsBox) - P0;
                b = clamp(P1 + b, minBoundsBox, maxBoundsBox) - P1;

                float3 T0 = a;
                float3 T1 = b;

                T0 = normalize(T0) * boxSize/2;
                T1 = normalize(T1) * boxSize/2;

                float3 CP0 = P0 + T0;
                float3 CP1 = P1 + T1;

                // Create the first vertex for the edge
                g2f o;
                o.pos = UnityWorldToClipPos(float4(P0, 1.0));
                o.barycentric = float3(1.0, 0.0, 0.0);
                lineStream.Append(o);

                // Generate cubic Bezier curve points
                for (int i = 1; i <= numSegments; i++) 
                {
                    float t = float(i) / float(numSegments);

                    // Cubic Bezier interpolation
                    float3 B = pow(1.0 - t, 3.0) * P0 
                            + 3.0 * pow(1.0 - t, 2.0) * t * CP0 
                            + 3.0 * (1.0 - t) * pow(t, 2.0) * CP1 
                            + pow(t, 3.0) * P1;

                    // Emit interpolated vertex
                    o.pos = UnityWorldToClipPos(float4(B, 1.0));
                    
                    // Vary barycentric coordinates for potential thickness/shading
                    o.barycentric = float3(1.0 - t, t, 0.0);
                    lineStream.Append(o);
                }

                // Create the second vertex for the edge
                o.pos = UnityWorldToClipPos(float4(P1, 1.0));
                o.barycentric = float3(0.0, 1.0, 0.0);
                lineStream.Append(o);

                }
            }

            fixed4 _WireframeFrontColour;
            float _WireframeAliasing;

            fixed4 frag(g2f i) : SV_Target
            {
                float3 unitWidth = fwidth(i.barycentric);
                float3 aliased = smoothstep(float3(0.0, 0.0, 0.0), unitWidth * _WireframeAliasing, i.barycentric);
                float alpha = 1 - min(aliased.x, min(aliased.y, aliased.z));
                // Explicitly account for the alpha from the colour property
                return fixed4(_WireframeFrontColour.rgb, alpha * _WireframeFrontColour.a);
            }
            ENDCG
        }

    }
}