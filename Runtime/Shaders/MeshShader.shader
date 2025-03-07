Shader "Custom/ProceduralMeshShader"
{
    Properties
    {
        _Color("Main Color", Color) = (1.0, 1.0, 1.0, 0.7) // Increase alpha to reduce flickering
        _ReceiveShadows("Receive Shadows", Float) = 1 // Add this property
    }
    SubShader
    {
        Tags { "Queue"="Transparent+100" "RenderType"="Transparent" }
        LOD 100

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

        // Main pass (renders the mesh with transparency)
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            Cull Back
            ZWrite Off // Disable depth writing for transparency
            ZTest LEqual // Ensure depth testing works correctly
            Blend SrcAlpha OneMinusSrcAlpha // Enable alpha blending
            Offset 0, -1 // Apply a small depth bias

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
            float4 _Color;
            float _CastShadows; // Add this
            float _ReceiveShadows; // Add this

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normal : NORMAL;
            };

            v2f vert(uint vid : SV_VertexID)
            {
                v2f o;
                int index = _IndexBuffer[vid];

                float3 worldPos = mul(_ObjectToWorld, float4(_MeshVertexPos[index].pos + _MeshVertexPos[index].posMod, 1.0)).xyz;
                o.pos = UnityWorldToClipPos(worldPos);
                o.normal = UnityObjectToWorldNormal(_MeshVertexPos[index].normal);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _Color; // Use the _Color property with alpha
            }
            ENDCG
        }

        // Shadow caster pass
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
            };

            StructuredBuffer<VertexProperties> _VertexProps;
            StructuredBuffer<int> _IndexBuffer;
            float4x4 _ObjectToWorld;

            // Define the appdata struct for the shadow caster pass
            struct appdata
            {
                uint vid : SV_VertexID; // Use vertex ID to fetch data from buffers
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                int index = _IndexBuffer[v.vid];

                float3 worldPos = mul(_ObjectToWorld, float4(_VertexProps[index].pos + _VertexProps[index].posMod, 1.0)).xyz;
                o.pos = UnityWorldToClipPos(worldPos);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}