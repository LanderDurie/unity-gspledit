Shader "Custom/ProceduralWireframeCubes"
{
    Properties
    {
        _WireframeColour("Wireframe front colour", color) = (1.0, 1.0, 1.0, 1.0)
        _WireframeAliasing("Wireframe thickness", float) = 1.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent"}
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
            };

            struct g2f
            {
                float4 pos : SV_POSITION;
                float3 barycentric : TEXCOORD0; // Barycentric coordinates for edge detection
            };

            struct VertexProperties {
                float3 pos;          // 12 bytes (3 * 4)
                float3 normal;       // 12 bytes (3 * 4)
                uint colorIds[4];    // 16 bytes (4 * 4)
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

            // Outputting 4 vertices per quad face
            [maxvertexcount(4)]
            void geom(triangle v2g IN[3], inout TriangleStream<g2f> triStream)
            {
                // Emit a quad by duplicating vertices
                g2f o;

                // Quad vertices (assuming the input triangle is part of a quad)
                for (int i = 0; i < 3; i++)
                {
                    o.pos = IN[i].vertex;
                    o.barycentric = float3(i == 0 ? 1 : 0, i == 1 ? 1 : 0, i == 2 ? 1 : 0);
                    triStream.Append(o);
                }

                // Emit the fourth vertex to complete the quad
                o.pos = IN[0].vertex;
                o.barycentric = float3(1, 0, 0);
                triStream.Append(o);

                triStream.RestartStrip();
            }

            fixed4 _WireframeColour;
            float _WireframeAliasing;

            fixed4 frag(g2f i) : SV_Target
            {
                // Calculate the minimum distance to any edge
                float edgeDistance = min(min(i.barycentric.x, i.barycentric.y), i.barycentric.z);

                // Use smoothstep to create a smooth wireframe effect
                float wire = smoothstep(_WireframeAliasing, 0.0, edgeDistance);

                // Blend between wireframe color and transparent background
                return fixed4(_WireframeColour.rgb, wire * _WireframeColour.a);
            }
            ENDCG
        }
    }
}