Shader "Hidden/GsplEdit/Scaffold" {
    Properties {
        _WireframeColour("Wireframe Color", Color) = (1.0, 0.635, 0, 0.15)
        _WireframeAliasing("Wireframe Aliasing", Float) = 0.5
        _DefaultColor("Default Color", Color) = (1.0, 0.635, 0, 0.08)
        _SelectedColor("Selected Color", Color) = (0.356, 0.878, 0.270, 0.6)
        _PointSize("Point Size", Float) = 1.0
    }

    SubShader {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Pass {
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

            // Properties
            float4 _WireframeColour;
            float _WireframeAliasing;
            float4 _DefaultColor;
            float4 _SelectedColor;
            float _PointSize;

            ByteAddressBuffer _VertexSelectedBits;
            ByteAddressBuffer _VertexDeletedBits;

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;  
                float2 uv : TEXCOORD0;   
                uint vid : SV_VertexID;  
            };

            struct v2g {
                float4 pos : SV_POSITION; 
                float4 color : COLOR;     
                float3 worldPos : TEXCOORD0;
                uint vid : TEXCOORD1;
                float isDeleted : TEXCOORD2;
            };

            struct g2f {
                float4 pos : SV_POSITION;
                float4 color : COLOR;    
                float3 barycentric : TEXCOORD0;
                float isPoint : TEXCOORD1;
            };

            // Helper function to check if a vertex is deleted
            bool IsVertexDeleted(uint vertexId) {
                uint wordIndex = vertexId >> 5;      // Divide by 32
                uint bitPosition = vertexId & 31;    // Modulo 32
                uint word = _VertexDeletedBits.Load(wordIndex * 4);
                return ((word >> bitPosition) & 1) != 0;
            }

            v2g vert(appdata v) {
                v2g o;

                // Transform vertex position to clip space
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.vid = v.vid;

                // Check if vertex is deleted
                o.isDeleted = IsVertexDeleted(v.vid) ? 1.0 : 0.0;

                // Selection logic
                uint wordIndex = v.vid >> 5;
                uint bitPosition = v.vid & 31;
                uint selectionWord = _VertexSelectedBits.Load(wordIndex * 4);
                uint isBitSet = (selectionWord >> bitPosition) & 1;
                o.color = lerp(_DefaultColor, _SelectedColor, isBitSet);

                return o;
            }

            [maxvertexcount(12)]
            void geom(triangle v2g input[3], inout TriangleStream<g2f> triStream) {
                g2f output;

                // Check if any vertex is deleted
                bool anyDeleted = false;
                for (int i = 0; i < 3; i++) {
                    if (input[i].isDeleted > 0.5) {
                        anyDeleted = true;
                        break;
                    }
                }

                // Only proceed with wireframe if no vertices are deleted
                if (!anyDeleted) {
                    // Wireframe mode
                    for (int j = 0; j < 3; j++) {
                        output.pos = input[j].pos;
                        output.color = input[j].color;
                        output.barycentric = float3(j == 0 ? 1 : 0, j == 1 ? 1 : 0, j == 2 ? 1 : 0);
                        output.isPoint = 0;
                        triStream.Append(output);
                    }

                    triStream.RestartStrip();
                }

                // Points mode - only for non-deleted vertices
                for (int i = 0; i < 3; i++) {
                    // Skip deleted vertices
                    if (input[i].isDeleted > 0.5) continue;

                    float size = _PointSize * 0.01;
                    float aspectRatio = _ScreenParams.y / _ScreenParams.x;
                    float4 center = input[i].pos;
                    float2 extensions = float2(size * aspectRatio, size);

                    // Generate quad vertices
                    float4 v[4];
                    v[0] = center + float4(-extensions.x, -extensions.y, 0, 0);
                    v[1] = center + float4(extensions.x, -extensions.y, 0, 0);
                    v[2] = center + float4(extensions.x, extensions.y, 0, 0);
                    v[3] = center + float4(-extensions.x, extensions.y, 0, 0);

                    // Output triangles
                    output.color = input[i].color;
                    output.barycentric = float3(0, 0, 0);
                    output.isPoint = 1;

                    output.pos = v[0]; triStream.Append(output);
                    output.pos = v[1]; triStream.Append(output);
                    output.pos = v[2]; triStream.Append(output);
                    triStream.RestartStrip();

                    output.pos = v[0]; triStream.Append(output);
                    output.pos = v[2]; triStream.Append(output);
                    output.pos = v[3]; triStream.Append(output);
                    triStream.RestartStrip();
                }
            }

            fixed4 frag(g2f i) : SV_Target {
                if (i.isPoint > 0.5) {
                    // Point mode
                    return i.color;
                } else {
                    // Wireframe mode
                    float3 unitWidth = fwidth(i.barycentric);
                    float3 aliased = smoothstep(float3(0.0, 0.0, 0.0), unitWidth * _WireframeAliasing, i.barycentric);
                    float alpha = 1 - min(aliased.x, min(aliased.y, aliased.z));
                    return fixed4(_WireframeColour.rgb, alpha * _WireframeColour.a);
                }
            }
            ENDCG
        }
    }
}