Shader "Hidden/GsplEdit/ShadowCaster" {
    Properties {}
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200
        // Render the object as fully transparent
        Pass {
            Name "ForwardBase"
            Tags { "LightMode" = "ForwardBase" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            ByteAddressBuffer _VertexDeletedBits;
            
            struct appdata {
                float4 vertex : POSITION;
                uint vid : SV_VertexID;
            };
            
            struct v2f {
                float4 pos : SV_POSITION;
                float isDeleted : TEXCOORD0;
            };
            
            // Helper function to check if a vertex is deleted
            bool IsVertexDeleted(uint vertexId) {
                uint wordIndex = vertexId >> 5;      // Divide by 32
                uint bitPosition = vertexId & 31;    // Modulo 32
                uint word = _VertexDeletedBits.Load(wordIndex * 4);
                return ((word >> bitPosition) & 1) != 0;
            }
            
            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.isDeleted = IsVertexDeleted(v.vid) ? 1.0 : 0.0;
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target {
                // Discard fragment if the vertex is deleted
                clip(i.isDeleted > 0.5 ? -1 : 1);
                return fixed4(0, 0, 0, 0);
            }
            ENDCG
        }
        
        // Shadow casting pass
        Pass {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"
            
            ByteAddressBuffer _VertexDeletedBits;
            
            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                uint vid : SV_VertexID;
            };
            
            struct v2f {
                V2F_SHADOW_CASTER;
                float isDeleted : TEXCOORD1;
            };
            
            // Helper function to check if a vertex is deleted
            bool IsVertexDeleted(uint vertexId) {
                uint wordIndex = vertexId >> 5;      // Divide by 32
                uint bitPosition = vertexId & 31;    // Modulo 32
                uint word = _VertexDeletedBits.Load(wordIndex * 4);
                return ((word >> bitPosition) & 1) != 0;
            }
            
            v2f vert(appdata v) {
                v2f o;
                
                // Check if vertex is deleted
                o.isDeleted = IsVertexDeleted(v.vid) ? 1.0 : 0.0;
                
                // Standard shadow caster setup
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target {
                // Discard fragment if the vertex is deleted
                clip(i.isDeleted > 0.5 ? -1 : 1);
                
                // Standard shadow caster output
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}