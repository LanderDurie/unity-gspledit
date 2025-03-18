#ifndef SAMPLE_POINTS_HLSL
#define SAMPLE_POINTS_HLSL

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

Tetrahedron computeTetrahedronFromEllipsoid(float3 center, float3 scale, float4 rotationQuat, float distanceFactor = 4.0)
{
    rotationQuat = normalize(rotationQuat);

    float xx = rotationQuat.x * rotationQuat.x;
    float yy = rotationQuat.y * rotationQuat.y;
    float zz = rotationQuat.z * rotationQuat.z;
    float xy = rotationQuat.x * rotationQuat.y;
    float xz = rotationQuat.x * rotationQuat.z;
    float yz = rotationQuat.y * rotationQuat.z;
    float xw = rotationQuat.x * rotationQuat.w;
    float yw = rotationQuat.y * rotationQuat.w;
    float zw = rotationQuat.z * rotationQuat.w;

    float3x3 rotationMatrix = float3x3(
        1 - 2 * (yy + zz), 2 * (xy - zw), 2 * (xz + yw),
        2 * (xy + zw), 1 - 2 * (xx + zz), 2 * (yz - xw),
        2 * (xz - yw), 2 * (yz + xw), 1 - 2 * (xx + yy)
    );

    float3 dir0 = normalize(float3(1, 1, 1)) * scale   * distanceFactor;
    float3 dir1 = normalize(float3(-1, -1, 1)) * scale * distanceFactor;
    float3 dir2 = normalize(float3(-1, 1, -1)) * scale * distanceFactor;
    float3 dir3 = normalize(float3(1, -1, -1)) * scale * distanceFactor;

    Tetrahedron t;
    t.v0 = mul(rotationMatrix, dir0) + center;
    t.v1 = mul(rotationMatrix, dir1) + center;
    t.v2 = mul(rotationMatrix, dir2) + center;
    t.v3 = mul(rotationMatrix, dir3) + center;

    return t;
}

Hexahedron computeHexahedronFromEllipsoid(float3 center, float3 scale, float4 rotationQuat, float distanceFactor = 3.0)
{
    rotationQuat = normalize(rotationQuat);

    float xx = rotationQuat.x * rotationQuat.x;
    float yy = rotationQuat.y * rotationQuat.y;
    float zz = rotationQuat.z * rotationQuat.z;
    float xy = rotationQuat.x * rotationQuat.y;
    float xz = rotationQuat.x * rotationQuat.z;
    float yz = rotationQuat.y * rotationQuat.z;
    float xw = rotationQuat.x * rotationQuat.w;
    float yw = rotationQuat.y * rotationQuat.w;
    float zw = rotationQuat.z * rotationQuat.w;

    float3x3 rotationMatrix = float3x3(
        1 - 2 * (yy + zz), 2 * (xy - zw), 2 * (xz + yw),
        2 * (xy + zw), 1 - 2 * (xx + zz), 2 * (yz - xw),
        2 * (xz - yw), 2 * (yz + xw), 1 - 2 * (xx + yy)
    );

    // Define the 6 points (2 per axis) in local space
    float3 dir0 = float3( 1,  0,  0) * scale * distanceFactor; // +X axis
    float3 dir1 = float3(-1,  0,  0) * scale * distanceFactor; // -X axis
    float3 dir2 = float3( 0,  1,  0) * scale * distanceFactor; // +Y axis
    float3 dir3 = float3( 0, -1,  0) * scale * distanceFactor; // -Y axis
    float3 dir4 = float3( 0,  0,  1) * scale * distanceFactor; // +Z axis
    float3 dir5 = float3( 0,  0, -1) * scale * distanceFactor; // -Z axis

    Hexahedron h;
    h.v0 = mul(rotationMatrix, dir0) + center; // +X
    h.v1 = mul(rotationMatrix, dir1) + center; // -X
    h.v2 = mul(rotationMatrix, dir2) + center; // +Y
    h.v3 = mul(rotationMatrix, dir3) + center; // -Y
    h.v4 = mul(rotationMatrix, dir4) + center; // +Z
    h.v5 = mul(rotationMatrix, dir5) + center; // -Z

    return h;
}

Line computeLineFromEllipsoid(float3 center, float3 scale, float4 rotationQuat)
{
    rotationQuat = normalize(rotationQuat);

    float xx = rotationQuat.x * rotationQuat.x;
    float yy = rotationQuat.y * rotationQuat.y;
    float zz = rotationQuat.z * rotationQuat.z;
    float xy = rotationQuat.x * rotationQuat.y;
    float xz = rotationQuat.x * rotationQuat.z;
    float yz = rotationQuat.y * rotationQuat.z;
    float xw = rotationQuat.x * rotationQuat.w;
    float yw = rotationQuat.y * rotationQuat.w;
    float zw = rotationQuat.z * rotationQuat.w;

    float3x3 rotationMatrix = float3x3(
        1 - 2 * (yy + zz), 2 * (xy - zw), 2 * (xz + yw),
        2 * (xy + zw), 1 - 2 * (xx + zz), 2 * (yz - xw),
        2 * (xz - yw), 2 * (yz + xw), 1 - 2 * (xx + yy)
    );

    // Determine the major principal component axis (largest scale component)
    float3 majorAxis = float3(0, 0, 0);
    if (scale.x >= scale.y && scale.x >= scale.z) {
        majorAxis = float3(1, 0, 0); // X-axis is major
    } else if (scale.y >= scale.x && scale.y >= scale.z) {
        majorAxis = float3(0, 1, 0); // Y-axis is major
    } else {
        majorAxis = float3(0, 0, 1); // Z-axis is major
    }

    // Define the 2 points on the major axis
    float3 dir0 = majorAxis * scale / 2; // Positive end
    float3 dir1 = -majorAxis * scale / 2; // Negative end

    Line l;
    l.v0 = mul(rotationMatrix, dir0) + center; // Positive end
    l.v1 = mul(rotationMatrix, dir1) + center; // Negative end

    return l;
}

#endif