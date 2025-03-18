#ifndef OFFSCREEN_UTILITIES_HLSL
#define OFFSCREEN_UTILITIES_HLSL

Texture2D _OffscreenMeshTexture;
SamplerState sampler_OffscreenMeshTexture; // Correct sampler name format
float4 _VecScreenParams;

#ifndef VP
#define VP
float4x4 _MatrixVP;
#endif

// Sample texture using screen-space coordinates (x, y in pixels)
half4 SampleTextureScreenSpace(float2 screenPos) {
    // Ensure screenPos is in pixel coordinates before division
    float2 uv = screenPos / _VecScreenParams.xy;
    // Invert Y axis to match Unity's UV coordinate system
    uv.y = 1.0 - uv.y;
    return _OffscreenMeshTexture.SampleLevel(sampler_OffscreenMeshTexture, uv, 0.0);
}


// Sample texture using world-space coordinates (xyz)
half4 SampleTextureWorldSpace(float3 worldPos)
{
    float4 screenPos = mul(_MatrixVP, float4(worldPos, 1.0));

    // Add a safety check for zero or negative w
    if (screenPos.w <= 0.0001f) {
        return float4(0, 0, 0, 0);
    }

    screenPos.xy /= screenPos.w; // Perspective divide
    screenPos.xy = (screenPos.xy + 1.0) * 0.5; // Convert to UV space [0,1]

    // Invert Y axis to match Unity's UV coordinate system
    screenPos.y = 1.0 - screenPos.y;

    // Clamp UV coordinates to valid range [0,1]
    screenPos.xy = saturate(screenPos.xy);

    // Sample the texture directly with the clamped UVs
    return _OffscreenMeshTexture.SampleLevel(sampler_OffscreenMeshTexture, screenPos.xy, 0.0);
}





#endif