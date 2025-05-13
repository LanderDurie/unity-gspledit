// SPDX-License-Identifier: MIT
Shader "Hidden/GsplEdit/RenderSplats"
{
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }

        Pass
        {
            ZWrite Off
            Blend OneMinusDstAlpha One
            Cull Off
            
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma require compute
#pragma use_dxc

#include "GaussianSplatting.hlsl"

StructuredBuffer<uint> _OrderBuffer;

struct v2f
{
    half4 col : COLOR0;
    float2 pos : TEXCOORD0;
    float4 vertex : SV_POSITION;
    float depth: TEXCOORD1;
};

StructuredBuffer<SplatViewData> _SplatViewData;
ByteAddressBuffer _SplatSelectedBits;
uint _SplatBitsValid;

v2f vert (uint vtxID : SV_VertexID, uint instID : SV_InstanceID)
{
    v2f o = (v2f)0;
    instID = _OrderBuffer[instID];
	SplatViewData view = _SplatViewData[instID];
	float4 centerClipPos = view.pos;
	bool behindCam = centerClipPos.w <= 0;
	if (behindCam)
	{
		o.vertex = asfloat(0x7fc00000); // NaN discards the primitive
	}
	else
	{
		o.col.r = f16tof32(view.color.x >> 16);
		o.col.g = f16tof32(view.color.x);
		o.col.b = f16tof32(view.color.y >> 16);
		o.col.a = f16tof32(view.color.y);

		uint idx = vtxID;
		float2 quadPos = float2(idx&1, (idx>>1)&1) * 2.0 - 1.0;
		quadPos *= 2;

		o.pos = quadPos;

		float2 deltaScreenPos = (quadPos.x * view.axis1 + quadPos.y * view.axis2) * 2 / _ScreenParams.xy;
		o.vertex = centerClipPos;
		o.vertex.xy += deltaScreenPos * centerClipPos.w;

        
        // Depth render
        uint splatIndex = instID;
        float3 splatPos = LoadSplatPos(splatIndex);
        // Calculate distance to the orthographic camera plane
        float3 cameraForward = -UNITY_MATRIX_V[2].xyz; // Camera's forward direction in world space
        float3 cameraToSplat = mul(unity_ObjectToWorld, float4(splatPos, 1.0)) - _WorldSpaceCameraPos;
        float distanceToPlane = dot(cameraToSplat, cameraForward); // Project onto camera forward

		float nearClipPlane = 0.07;
		float farClipPlane = 0.3;
		float range = farClipPlane + nearClipPlane;
		
		distanceToPlane = distanceToPlane - 1 + nearClipPlane;
		distanceToPlane /= range;
		distanceToPlane = 1 - distanceToPlane;

		if (distanceToPlane < 0) {
			distanceToPlane = 0;
		} else if (distanceToPlane > 1) {
			distanceToPlane = 1;
		}

        // Normalize the distance for visualization (adjust the range as needed)
        o.depth = distanceToPlane;
	}
    return o;
}

half4 frag (v2f i) : SV_Target
{
	float power = -dot(i.pos, i.pos);
	half alpha = exp(power);

	alpha = saturate(alpha * i.col.a);

    if (alpha < 1.0/255.0)
        discard;

    half4 res = half4(half3(i.depth, i.depth, i.depth) * alpha, alpha);
    return res;
}
ENDCG
        }
    }
}