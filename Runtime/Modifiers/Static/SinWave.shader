Shader "Custom/SinWaveShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Amplitude ("Wave Amplitude", Float) = 0.1
        _Frequency ("Wave Frequency", Float) = 1.0
        _Speed ("Wave Speed", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            sampler2D _MainTex;
            float _Amplitude;
            float _Frequency;
            float _Speed;
            float _T;

            v2f vert (appdata v)
            {
                v2f o;
                
                // Modify vertex position with a sine wave
                float wave = sin(v.vertex.x * _Frequency + _T * _Speed) * _Amplitude;
                v.vertex.y += wave;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}
