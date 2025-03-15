#ifndef OFFSCREEN_UTILITIES_HLSL
#define OFFSCREEN_UTILITIES_HLSL

Texture2D _OffscreenMeshTexture;
SamplerState sampler_OffscreenMeshTexture; // Correct sampler name format
float4 _VecScreenParams;
float4x4 _MatrixVP;

// Sample texture using screen-space coordinates (x, y in pixels)
half4 SampleTextureScreenSpace(float2 screenPos)
{
    float2 uv = screenPos / _VecScreenParams.xy;
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

half4 SampleTextureWorldSpaceMaxNeighbor(float3 worldPos) 
{
    float4 screenPos = mul(_MatrixVP, float4(worldPos, 1.0));

    // Safety check for zero or negative w
    if (screenPos.w <= 0.0001f) {
        return float4(0, 0, 0, 0);
    }

    screenPos.xy /= screenPos.w; // Perspective divide
    screenPos.xy = (screenPos.xy + 1.0) * 0.5; // Convert to UV space [0,1]

    // Invert Y axis to match Unity's UV coordinate system
    screenPos.y = 1.0 - screenPos.y;

    // Clamp UV coordinates to valid range [0,1]
    screenPos.xy = saturate(screenPos.xy);

    // Texture size in pixels
    float2 texelSize = 1.0 / _VecScreenParams.xy;

    // Neighboring UVs
    float2 uvTL = screenPos.xy + float2(-texelSize.x, texelSize.y); // Top-left
    float2 uvTR = screenPos.xy + float2(texelSize.x, texelSize.y);  // Top-right
    float2 uvBL = screenPos.xy + float2(-texelSize.x, -texelSize.y); // Bottom-left
    float2 uvBR = screenPos.xy + float2(texelSize.x, -texelSize.y);  // Bottom-right

    // Clamp UVs to avoid out-of-bounds sampling
    uvTL = saturate(uvTL);
    uvTR = saturate(uvTR);
    uvBL = saturate(uvBL);
    uvBR = saturate(uvBR);

    // Sample the four neighboring pixels
    half4 colTL = _OffscreenMeshTexture.SampleLevel(sampler_OffscreenMeshTexture, uvTL, 0.0);
    half4 colTR = _OffscreenMeshTexture.SampleLevel(sampler_OffscreenMeshTexture, uvTR, 0.0);
    half4 colBL = _OffscreenMeshTexture.SampleLevel(sampler_OffscreenMeshTexture, uvBL, 0.0);
    half4 colBR = _OffscreenMeshTexture.SampleLevel(sampler_OffscreenMeshTexture, uvBR, 0.0);

    // Find the color with the highest alpha (opacity)
    half4 maxColor = colTL;
    if (colTR.a > maxColor.a) maxColor = colTR;
    if (colBL.a > maxColor.a) maxColor = colBL;
    if (colBR.a > maxColor.a) maxColor = colBR;

    return maxColor;
}





#endif