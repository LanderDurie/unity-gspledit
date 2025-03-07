Shader "Custom/ProceduralWireframe"
{
    Properties
    {
        _WireframeColour("Wireframe front colour", color) = (1.0, 1.0, 1.0, 1.0)
        _WireframeAliasing("Wireframe aliasing", float) = 1.5
        _Enable("Enable / disable", float) = 1.0 // 1.0 for enabled, 0.0 for disabled
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

            // #include "../Core/Mesh/Compute/MeshUtilities.hlsl"


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
                float3 pos;
                float3 posMod; 
                float3 normal;
                float2 uv;
            };

            StructuredBuffer<VertexProperties> _MeshVertexPos;
            StructuredBuffer<int> _IndexBuffer;
            float4x4 _ObjectToWorld;
            float _Enable;

            v2g vert(uint vid : SV_VertexID)
            {
                v2g o;
                // Get the actual vertex index from the index buffer
                int index = _IndexBuffer[vid];
                float3 worldPos = mul(_ObjectToWorld, float4(_MeshVertexPos[index].pos + _MeshVertexPos[index].posMod, 1.0)).xyz;
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
                if (_Enable < 0.5) discard;

                float3 unitWidth = fwidth(i.barycentric);
                float3 aliased = smoothstep(float3(0.0, 0.0, 0.0), unitWidth * _WireframeAliasing, i.barycentric);
                float alpha = 1 - min(aliased.x, min(aliased.y, aliased.z));
                // Explicitly account for the alpha from the colour property
                return fixed4(_WireframeColour.rgb, alpha * _WireframeColour.a);
            }
            ENDCG
        }

    }
}