Shader "Hidden/MeshExternalFalloff" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _FalloffStrength ("Falloff Strength", Range(0, 1)) = 0.5
        _FalloffDistance ("Falloff Distance", Range(0, 5)) = 2.0
    }
    
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _MainTex;
            float _FalloffStrength;
            float _FalloffDistance;
            float4 _MainTex_TexelSize;
            
            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            float sampleAlpha(float2 uv) {
                // Sample the texture and return the alpha value
                // Handle out-of-bounds UVs by returning 0 (transparent)
                if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1)
                    return 0;
                    
                return tex2D(_MainTex, uv).a;
            }
            
            float4 frag (v2f i) : SV_Target {
                float4 col = tex2D(_MainTex, i.uv);
                
                // If pixel is inside the mesh (opaque), return it unchanged
                if (col.a > 0.99)
                    return col;
                
                // Pixel step size based on falloff distance
                float2 pixelStep = _MainTex_TexelSize.xy * max(1.0, _FalloffDistance);
                
                // Check in multiple directions to find the nearest edge
                const int directions = 16; // More directions for smoother falloff
                const float maxSamples = 15.0; // Maximum samples in each direction
                
                float closestEdgeDist = maxSamples;
                
                // Loop through each direction
                for (int dir = 0; dir < directions; dir++) {
                    // Calculate the direction vector
                    float angle = dir * (3.14159265 * 2.0 / directions);
                    float2 dirVec = float2(cos(angle), sin(angle)) * pixelStep;
                    
                    // Start from a small step to catch very close edges
                    for (float step = 1.0; step <= maxSamples; step += 1.0) {
                        // Sample point
                        float2 sampleUV = i.uv + dirVec * step;
                        float sampleA = sampleAlpha(sampleUV);
                        
                        // If we found an opaque pixel, we've hit the edge of the mesh
                        if (sampleA > 0.99) {
                            closestEdgeDist = min(closestEdgeDist, step);
                            break; // Found an edge in this direction, move to next
                        }
                    }
                }
                
                // Calculate the falloff factor based on the distance to the edge
                float falloffFactor = smoothstep(0.0, 1.0, closestEdgeDist / maxSamples);
                
                // Apply the falloff to the alpha channel ONLY in transparent regions
                // The falloff is stronger as we move away from the edge
                col.a *= lerp(1.0, 1.0 - _FalloffStrength, falloffFactor);
                
                return col;
            }
            ENDCG
        }
    }
}