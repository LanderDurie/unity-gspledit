#ifndef SSSAMPLE_POINTS_HLSL
#define SSSAMPLE_POINTS_HLSL

#ifndef SUB_STRUCTS
#define SUB_STRUCTS
struct Tetrahedron {
    float3 v0;
    float3 v1;
    float3 v2;
    float3 v3;
};

struct Hexahedron {
    float3 v0;
    float3 v1;
    float3 v2;
    float3 v3;
    float3 v4;
    float3 v5;
};

struct Line {
    float3 v0;
    float3 v1;
};
#endif


#ifndef VP
#define VP
float4x4 _MatrixVP;
#endif

// Rotates a point using a quaternion
float3 RotateByQuaternion(float3 v, float4 q) {
    float3 t = 2.0 * cross(q.xyz, v);
    return v + q.w * t + cross(q.xyz, t);
}

void getEdgePoints4(float3 center, float3 scale, float4 rotationQuat, float distanceFactor,
    out float2 edgePoints[4]) {
// Compute transformed axes of the ellipsoid
float3 right = RotateByQuaternion(float3(scale.x, 0, 0), rotationQuat);
float3 up = RotateByQuaternion(float3(0, scale.y, 0), rotationQuat);

// Get viewport dimensions
float2 viewportSize = float2(_VecScreenParams.x, _VecScreenParams.y);

// Sample points along the edge
for (int i = 0; i < 4; i++) {
float angle = (i / 4.0) * 6.283185; // Full circle (2 * PI)

// Apply distance factor to scale the ellipse
float3 worldPos = center + (cos(angle) * right + sin(angle) * up) * distanceFactor;

// Project to clip space using VP matrix
float4 clipPos = mul(_MatrixVP, float4(worldPos, 1.0));

// Convert to NDC space
float2 ndcPos = clipPos.xy / clipPos.w;

// Convert NDC to screen coordinates
edgePoints[i] = (ndcPos * 0.5 + 0.5) * viewportSize;
}
}

void getEdgePoints6(float3 center, float3 scale, float4 rotationQuat, float distanceFactor,
    out float2 edgePoints[6]) {
// Compute transformed axes of the ellipsoid
float3 right = RotateByQuaternion(float3(scale.x, 0, 0), rotationQuat);
float3 up = RotateByQuaternion(float3(0, scale.y, 0), rotationQuat);

// Get viewport dimensions
float2 viewportSize = float2(_VecScreenParams.x, _VecScreenParams.y);

// Sample points along the edge
for (int i = 0; i < 6; i++) {
float angle = (i / 6.0) * 6.283185; // Full circle (2 * PI)

// Apply distance factor to scale the ellipse
float3 worldPos = center + (cos(angle) * right + sin(angle) * up) * distanceFactor;

// Project to clip space using VP matrix
float4 clipPos = mul(_MatrixVP, float4(worldPos, 1.0));

// Convert to NDC space
float2 ndcPos = clipPos.xy / clipPos.w;

// Convert NDC to screen coordinates
edgePoints[i] = (ndcPos * 0.5 + 0.5) * viewportSize;
}
}

void getEdgePoints10(float3 center, float3 scale, float4 rotationQuat, float distanceFactor,
    out float2 edgePoints[10]) {
// Compute transformed axes of the ellipsoid
float3 right = RotateByQuaternion(float3(scale.x, 0, 0), rotationQuat);
float3 up = RotateByQuaternion(float3(0, scale.y, 0), rotationQuat);

// Get viewport dimensions
float2 viewportSize = float2(_VecScreenParams.x, _VecScreenParams.y);

// Sample points along the edge
for (int i = 0; i < 10; i++) {
float angle = (i / 10.0) * 6.283185; // Full circle (2 * PI)

// Apply distance factor to scale the ellipse
float3 worldPos = center + (cos(angle) * right + sin(angle) * up) * distanceFactor;

// Project to clip space using VP matrix
float4 clipPos = mul(_MatrixVP, float4(worldPos, 1.0));

// Convert to NDC space
float2 ndcPos = clipPos.xy / clipPos.w;

// Convert NDC to screen coordinates
edgePoints[i] = (ndcPos * 0.5 + 0.5) * viewportSize;
}
}

#endif