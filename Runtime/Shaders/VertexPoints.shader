Shader "Custom/ProceduralPointAroundVertices"
{
    Properties
    {
        _DefaultColor("Default Color", Color) = (1, 0, 0, 1)
        _SelectedColor("Selected Color", Color) = (0, 1, 0, 1)
        _PointSize("Point Size", Float) = 2.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2g
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
            };

            struct g2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
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

            float _PointSize;
            float4 _DefaultColor;
            float4 _SelectedColor;
            StructuredBuffer<VertexProperties> _VertexProps;

            ByteAddressBuffer _VertexSelectedBits;
            float4x4 _ObjectToWorld;

            v2g vert(uint vid : SV_VertexID)
            {
                v2g o;

                // Read position from buffer and transform to world space
                float3 vertex = _VertexProps[vid].pos + _VertexProps[vid].posMod;
                float3 worldPos = mul(_ObjectToWorld, float4(vertex, 1.0)).xyz;
                o.pos = UnityWorldToClipPos(worldPos);

                // Selection logic
                uint wordIndex = vid >> 5;
                uint bitPosition = vid & 31;
                uint selectionWord = _VertexSelectedBits.Load(wordIndex * 4);
                uint isBitSet = (selectionWord >> bitPosition) & 1;
                
                o.color = lerp(_DefaultColor, _SelectedColor, isBitSet);
                return o;
            }

            [maxvertexcount(6)]
            void geom(point v2g input[1], inout TriangleStream<g2f> triStream)
            {
                float size = _PointSize * 0.01;
                float aspectRatio = _ScreenParams.y / _ScreenParams.x;
                float4 center = input[0].pos;
                float2 extensions = float2(size * aspectRatio, size);

                g2f output;
                output.color = input[0].color;

                // Generate quad vertices
                float4 v[4];
                v[0] = center + float4(-extensions.x, -extensions.y, 0, 0);
                v[1] = center + float4(extensions.x, -extensions.y, 0, 0);
                v[2] = center + float4(extensions.x, extensions.y, 0, 0);
                v[3] = center + float4(-extensions.x, extensions.y, 0, 0);

                // Output triangles
                output.pos = v[0]; triStream.Append(output);
                output.pos = v[1]; triStream.Append(output);
                output.pos = v[2]; triStream.Append(output);
                triStream.RestartStrip();

                output.pos = v[0]; triStream.Append(output);
                output.pos = v[2]; triStream.Append(output);
                output.pos = v[3]; triStream.Append(output);
                triStream.RestartStrip();
            }

            fixed4 frag(g2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}