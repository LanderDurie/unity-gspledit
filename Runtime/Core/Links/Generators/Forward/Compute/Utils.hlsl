#ifndef FORWARD_LINK_UTILS_HLSL
#define FORWARD_LINK_UTILS_HLSL

// Helper function to convert a 3x3 matrix to a 4x4 matrix
float4x4 Convert3x3To4x4(float3x3 mat3x3) {
    return float4x4(
        float4(mat3x3[0], 0.0f),
        float4(mat3x3[1], 0.0f),
        float4(mat3x3[2], 0.0f),
        float4(0.0f, 0.0f, 0.0f, 1.0f)
    );
}

// Function to convert a quaternion to a 4x4 rotation matrix
float4x4 QuatToMatrix(float4 quat) {
    float3x3 rotationMat = float3x3(
        1.0f - 2.0f * (quat.y * quat.y + quat.z * quat.z),  2.0f * (quat.x * quat.y - quat.w * quat.z),  2.0f * (quat.x * quat.z + quat.w * quat.y),
        2.0f * (quat.x * quat.y + quat.w * quat.z),  1.0f - 2.0f * (quat.x * quat.x + quat.z * quat.z),  2.0f * (quat.y * quat.z - quat.w * quat.x),
        2.0f * (quat.x * quat.z - quat.w * quat.y),  2.0f * (quat.y * quat.z + quat.w * quat.x),  1.0f - 2.0f * (quat.x * quat.x + quat.y * quat.y)
    );
    return Convert3x3To4x4(rotationMat);
}

float3 ClosestPoint(float3 extPos, float3 p1, float3 p2, float3 p3) {
    // Compute vectors relative to p1
    float3 v0 = p2 - p1;
    float3 v1 = p3 - p1;
    float3 v2 = extPos - p1;

    // Compute dot products
    float d00 = dot(v0, v0);
    float d01 = dot(v0, v1);
    float d11 = dot(v1, v1);
    float d20 = dot(v2, v0);
    float d21 = dot(v2, v1);

    // Compute the denominator of the barycentric coordinates
    float denom = d00 * d11 - d01 * d01;

    // Compute barycentric coordinates with improved stability
    float v = (d11 * d20 - d01 * d21) / max(denom, 1e-6f);
    float w = (d00 * d21 - d01 * d20) / max(denom, 1e-6f);
    float u = 1.0f - v - w;

    // Clamp barycentric coordinates
    v = max(0.0f, v);
    w = max(0.0f, w);
    u = max(0.0f, u);

    float sum = u + v + w;
    u /= sum;
    v /= sum;
    w /= sum;

    // Compute the closest point in the triangle
    return u * p1 + v * p2 + w * p3;
}

float3 MahalanobisTransform(float3 pos, SplatData splat) {
    // Translate to local space and apply rotation and scaling
    pos -= splat.pos;
    float4x4 rotationMatrix = QuatToMatrix(splat.rot);
    float4x4 inverseRotation = transpose(rotationMatrix);
    pos = mul(inverseRotation, float4(pos, 0.0)).xyz;
    pos /= splat.scale;
    return pos;
}

float3 InverseMahalanobisTransform(float3 pos, SplatData splat) {
    // Apply scaling, rotation, and translation in reverse order
    pos *= splat.scale;
    float4x4 rotationMatrix = QuatToMatrix(splat.rot);
    pos = mul(rotationMatrix, float4(pos, 0.0)).xyz;
    pos += splat.pos;
    return pos;
}


#endif