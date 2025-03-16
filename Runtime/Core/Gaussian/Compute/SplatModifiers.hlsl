#ifndef SPLAT_MODIFIERS_HLSL
#define SPLAT_MODIFIERS_HLSL

#include "../../Scaffold/Compute/OffscreenUtilities.hlsl"
#include "../../Links/Compute/LinkUtilities.hlsl"
#include "./SamplePoints.hlsl"

#ifndef QUATERNION_IDENTITY
#define QUATERNION_IDENTITY float4(0, 0, 0, 1)
#endif

#ifndef PI
#define PI 3.14159265359f
#endif 

static const float EPSILON = 1e-10;
static const int MAX_ITERATIONS = 10;


struct ModSplat {
    float3 pos;
    float4 rot;
    float3 scale;
    half4 color;
};

struct Triangle {
    int v0;
    int v1;
    int v2;
};

struct TriangleVerts {
    float3 v0;
    float3 v1;
    float3 v2;
};

StructuredBuffer<float3> _VertexBasePos;
StructuredBuffer<float3> _VertexModPos;
StructuredBuffer<Triangle> _MeshIndices;
RWByteAddressBuffer _VertexDeletedBits;
uint _IndexCount;

float4x4 _MatrixObjectToWorld;
float4x4 _MatrixWorldToObject;
float4x4 _MatrixMV;
float4x4 _MatrixP;
float4 _VecWorldSpaceCameraPos;
int _SelectionMode;

RWStructuredBuffer<uint> _SplatSortDistances;
RWStructuredBuffer<uint> _SplatSortKeys;

uint _SplatCount;

bool IsVertexDeleted(int vertexId) {
    uint wordIndex = vertexId / 32;
    uint bitPosition = vertexId & 31;
    uint word = _VertexDeletedBits.Load(wordIndex * 4);
    return ((word >> bitPosition) & 1) != 0;
}

bool CanIgnoreModifiers(SplatLink currentSplat) {
    // bool ignore = true;
    // [unroll]
    // for (int i = 0; i < 8; i++) {
    //     // Check if valid triangle
    //     int triangleId = currentSplat.triangleIds[i];
    //     float triangleWeight = currentSplat.triangleWeights[i];
    //     if (triangleId == -1 || triangleWeight <= 0) break; // Weights are sorted -> stop on first invalid

    //     TriangleVerts tv;
    //     Triangle t = _MeshIndices[triangleId];
    //     tv.v0 = _VertexModPos[t.v0] - _VertexBasePos[t.v0];
    //     tv.v1 = _VertexModPos[t.v1] - _VertexBasePos[t.v1];
    //     tv.v2 = _VertexModPos[t.v2] - _VertexBasePos[t.v2];

    //     if (length(tv.v0) > 0.00001 ||
    //         length(tv.v1) > 0.00001 ||
    //         length(tv.v2) > 0.00001) {
    //         ignore = false;
    //     }
    // }
    // return ignore;
    bool anyLinks = false;
    [unroll]
    for (int i = 0; i < 8; i++) {
        // Check if valid triangle
        int triangleId = currentSplat.triangleIds[i];
        float triangleWeight = currentSplat.triangleWeights[i];
        if (triangleId == -1 || triangleWeight <= 0) break; // Weights are sorted -> stop on first invalid
        anyLinks = true;
    }
    return !anyLinks;
}

bool ShouldRemoveSplat(SplatLink currentSplat, float threshold = 0.5) {
    float totalWeight = 0;
    bool anyLinks = false;
    [unroll]
    for (int i = 0; i < 8; i++) {
        // Check if valid triangle
        int triangleId = currentSplat.triangleIds[i];
        float triangleWeight = currentSplat.triangleWeights[i];
        if (triangleId == -1 || triangleWeight <= 0) break; // Weights are sorted -> stop on first invalid

        anyLinks = true;
        Triangle t = _MeshIndices[triangleId];
        if (IsVertexDeleted(t.v0) || IsVertexDeleted(t.v1) || IsVertexDeleted(t.v2)) continue;
        totalWeight += triangleWeight;

    }
    return anyLinks && totalWeight < threshold;
}


float det3x3(
    float m11, float m12, float m13,
    float m21, float m22, float m23,
    float m31, float m32, float m33)
{
    return m11 * (m22 * m33 - m23 * m32) -
           m12 * (m21 * m33 - m23 * m31) +
           m13 * (m21 * m32 - m22 * m31);
}

float4 calculateTetraBarycentricCoordinates(float3 p, float3 v0, float3 v1, float3 v2, float3 v3)
{
    // Compute vectors relative to d
    float3 vd = p - v3;
    float3 va = v0 - v3;
    float3 vb = v1 - v3;
    float3 vc = v2 - v3;

    // Compute determinant of the full system
    float d = det3x3(va.x, vb.x, vc.x,
                     va.y, vb.y, vc.y,
                     va.z, vb.z, vc.z);

    // Compute determinants for each coordinate
    float d1 = det3x3(vd.x, vb.x, vc.x,
                      vd.y, vb.y, vc.y,
                      vd.z, vb.z, vc.z);

    float d2 = det3x3(va.x, vd.x, vc.x,
                      va.y, vd.y, vc.y,
                      va.z, vd.z, vc.z);

    float d3 = det3x3(va.x, vb.x, vd.x,
                      va.y, vb.y, vd.y,
                      va.z, vb.z, vd.z);

    // Compute barycentric coordinates
    float invD = 1.0 / d;
    float4 bary;
    bary.x = d1 * invD;
    bary.y = d2 * invD;
    bary.z = d3 * invD;
    bary.w = 1.0 - bary.x - bary.y - bary.z;

    return bary;
}

float4 quaternionFromMatrix(float3x3 V)
{
    // Ensure right-handed coordinate system
    float3 v2 = cross(
        float3(V[0][0], V[1][0], V[2][0]),
        float3(V[0][1], V[1][1], V[2][1])
    );
    V[0][2] = v2.x;
    V[1][2] = v2.y;
    V[2][2] = v2.z;

    float trace = V[0][0] + V[1][1] + V[2][2];
    float4 q;

    if (trace > 0)
    {
        float s = sqrt(trace + 1.0f) * 2;
        q = float4(
            (V[2][1] - V[1][2]) / s,
            (V[0][2] - V[2][0]) / s,
            (V[1][0] - V[0][1]) / s,
            0.25f * s
        );
    }
    else if (V[0][0] > V[1][1] && V[0][0] > V[2][2])
    {
        float s = sqrt(1.0f + V[0][0] - V[1][1] - V[2][2]) * 2;
        q = float4(
            0.25f * s,
            (V[0][1] + V[1][0]) / s,
            (V[0][2] + V[2][0]) / s,
            (V[2][1] - V[1][2]) / s
        );
    }
    else if (V[1][1] > V[2][2])
    {
        float s = sqrt(1.0f + V[1][1] - V[0][0] - V[2][2]) * 2;
        q = float4(
            (V[0][1] + V[1][0]) / s,
            0.25f * s,
            (V[1][2] + V[2][1]) / s,
            (V[0][2] - V[2][0]) / s
        );
    }
    else
    {
        float s = sqrt(1.0f + V[2][2] - V[0][0] - V[1][1]) * 2;
        q = float4(
            (V[0][2] + V[2][0]) / s,
            (V[1][2] + V[2][1]) / s,
            0.25f * s,
            (V[1][0] - V[0][1]) / s
        );
    }

    // Normalize quaternion
    float len = sqrt(dot(q, q));
    return q / len;
}

void JacobiEigenvalueDecomposition(float3x3 A, out float3 eigenvalues, out float3x3 eigenvectors)
{
    // Initialize eigenvectors to identity matrix
    eigenvectors = float3x3(1, 0, 0, 0, 1, 0, 0, 0, 1);
    
    // Copy input matrix (we'll modify it during iterations)
    float3x3 D = A;
    
    const int MAX_ITERATIONS = 50;
    const float EPSILON = 1e-16;
    
    for (int iter = 0; iter < MAX_ITERATIONS; iter++)
    {
        // Find largest off-diagonal element
        float maxOffDiag = 0.0;
        int p = 0, q = 1;
        
        for (int i = 0; i < 3; i++)
        {
            for (int j = i + 1; j < 3; j++)
            {
                float absValue = abs(D[i][j]);
                if (absValue > maxOffDiag)
                {
                    maxOffDiag = absValue;
                    p = i;
                    q = j;
                }
            }
        }
        
        // Check for convergence
        if (maxOffDiag < EPSILON)
            break;
        
        // Compute Jacobi rotation
        float theta = 0.5 * atan2(2.0 * D[p][q], D[p][p] - D[q][q]);
        float c = cos(theta);
        float s = sin(theta);
        
        // Apply rotation to D
        float3x3 J = float3x3(1, 0, 0, 0, 1, 0, 0, 0, 1);
        J[p][p] = c;
        J[p][q] = -s;
        J[q][p] = s;
        J[q][q] = c;
        
        float3x3 temp = mul(transpose(J), mul(D, J));
        D = temp;
        
        // Update eigenvectors
        eigenvectors = mul(eigenvectors, J);
    }
    
    // Extract eigenvalues from diagonal of D
    eigenvalues.x = D[0][0];
    eigenvalues.y = D[1][1];
    eigenvalues.z = D[2][2];
}

bool fitSteinerCircumellipsoid(Tetrahedron t, inout ModSplat result)
{
    // Compute centroid
    float3 center = (t.v0 + t.v1 + t.v2 + t.v3) / 4;

    // Translate points to origin
    float3 c0 = t.v0 - center;
    float3 c1 = t.v1 - center;
    float3 c2 = t.v2 - center;
    float3 c3 = t.v3 - center;

    // Compute covariance matrix
    float3x3 covMatrix = 0;
    covMatrix[0][0] = (c0.x * c0.x + c1.x * c1.x + c2.x * c2.x + c3.x * c3.x) / 4;
    covMatrix[0][1] = (c0.x * c0.y + c1.x * c1.y + c2.x * c2.y + c3.x * c3.y) / 4;
    covMatrix[0][2] = (c0.x * c0.z + c1.x * c1.z + c2.x * c2.z + c3.x * c3.z) / 4;
    covMatrix[1][1] = (c0.y * c0.y + c1.y * c1.y + c2.y * c2.y + c3.y * c3.y) / 4;
    covMatrix[1][2] = (c0.y * c0.z + c1.y * c1.z + c2.y * c2.z + c3.y * c3.z) / 4;
    covMatrix[2][2] = (c0.z * c0.z + c1.z * c1.z + c2.z * c2.z + c3.z * c3.z) / 4;

    covMatrix[1][0] = covMatrix[0][1];
    covMatrix[2][0] = covMatrix[0][2];
    covMatrix[2][1] = covMatrix[1][2];

    // Compute eigenvectors using power iteration
    float3x3 V;
    float3 scale;

    JacobiEigenvalueDecomposition(covMatrix, scale, V);

    // Scale factor for Steiner circumellipsoid
    scale = sqrt(3 * scale) / 4.0;

    // Convert eigenvectors to quaternion
    float4 rotation = quaternionFromMatrix(V);

    result.pos = center;
    result.scale = scale;
    result.rot = rotation;
    return true;
}


struct TANCCoords {
    float t;
    int edgeIndex;
    float signedDistance;
    float projectDistance;
    int closestCorner;
    bool isCorner;
};

bool isPointInsideTriangle(float3 p, float3 v1, float3 v2, float3 v3)
{
    // Compute normal of the triangle
    float3 edge1 = v2 - v1;
    float3 edge2 = v3 - v1;
    float3 normal = normalize(cross(edge1, edge2));

    // Compute perpendicular projection of point onto the triangle plane
    float projectDistance = dot(normal, p - v1);
    float3 projectedPoint = p - projectDistance * normal;

    // Compute vectors        
    float3 v0 = v2 - v1;
    float3 v1ToV3 = v3 - v1;
    float3 v2ToP = projectedPoint - v1;
    
    // Compute dot products
    float dot00 = dot(v0, v0);
    float dot01 = dot(v0, v1ToV3);
    float dot02 = dot(v0, v2ToP);
    float dot11 = dot(v1ToV3, v1ToV3);
    float dot12 = dot(v1ToV3, v2ToP);

    // Compute barycentric coordinates
    float denom = dot00 * dot11 - dot01 * dot01;
    float u = (dot11 * dot02 - dot01 * dot12) / denom;
    float v = (dot00 * dot12 - dot01 * dot02) / denom;
    float w = 1.0 - u - v;

    // Check if inside the triangle
    return (u >= 0) && (v >= 0) && (w >= 0);
}


float3 planeLineIntersection(float3 p0, float3 p1, float3 v2, float3 l1, float3 l2) {
    float3 v1 = p1 - p0;
    float3 planeNormal = normalize(cross(v1, v2));
    float3 lineDir = l2 - l1;
    float denom = dot(planeNormal, lineDir);

    if (abs(denom) < 1e-6)
        return float3(0, 0, 0); // Parallel case, undefined behavior

    float t = dot(planeNormal, p0 - l1) / denom;
    return l1 + t * lineDir;
}


TANCCoords triangleAlignedNormalizedCoordinates(float3 p, float3 v1, float3 v2, float3 v3) {
    float3 edge1 = v2 - v1;
    float3 edge2 = v3 - v1;
    float3 normal = normalize(cross(edge1, edge2));

    if (dot(normal, normal) < 1e-6) {
        TANCCoords result;
        result.t = 0;
        result.edgeIndex = 0;
        result.signedDistance = 0;
        result.projectDistance = 0;
        result.closestCorner = 0;
        result.isCorner = false;
        return result;
    }
        
    float projectDistance = dot(normal, p - v1);
    float3 projectedPoint = p - projectDistance * normal;

    float3 edges[6] = { v1, v2, v2, v3, v3, v1 };
    float minDistance = 3.402823466e+30; // float max
    float t = 0;
    uint edgeIndex = 0;
    bool isCorner = false;
    uint opposingCorner = 0;

    for (uint i = 0; i < 3; i++) {
        float3 a = edges[i * 2];
        float3 b = edges[i * 2 + 1];
        float3 ab = b - a;
        float3 ap = projectedPoint - a;
        float projection = dot(ap, ab) / dot(ab, ab);
        
        if (projection > 1 || projection < 0) {
            int opposingEdgeIndex = (i + 2) % 3;
            float3 potentialOpposingCorner = edges[(i * 2 + 2) % 6];
            float cornerDistance = length(projectedPoint - potentialOpposingCorner);

            if (cornerDistance < minDistance) {
                minDistance = cornerDistance;
                isCorner = true;
                edgeIndex = opposingEdgeIndex;
                opposingCorner = (i + 1) % 3;

                float3 startEdge = edges[edgeIndex * 2];
                float3 endEdge = edges[edgeIndex * 2 + 1];
                
                float3 cornerMax = normalize(cross(normal, potentialOpposingCorner - startEdge));
                float3 cornerMin = normalize(cross(potentialOpposingCorner - endEdge, normal));
                float3 cornerT = normalize(projectedPoint - potentialOpposingCorner);
                float3 intersect = planeLineIntersection(cornerMin, cornerMax, normal, float3(0,0,0), cornerT);
                t = length(cornerMin - intersect) / length(cornerMin - cornerMax);
            }
        } else {
            float distance = length(projectedPoint - (a + projection * ab));
            if (distance < minDistance) {
                minDistance = distance;
                isCorner = false;
                edgeIndex = i;
                t = projection;
            }
        }
    }

    if (isCorner) {
        TANCCoords result;
        result.t = t;
        result.edgeIndex = edgeIndex;
        result.signedDistance = minDistance;
        result.projectDistance = projectDistance;
        result.closestCorner = opposingCorner;
        result.isCorner = true;
        return result;
    }

    float3 edgeStart = edges[edgeIndex * 2];
    float3 edgeEnd = edges[edgeIndex * 2 + 1];
    float3 edgeDir = normalize(edgeEnd - edgeStart);
    float3 edgeNormal = normalize(cross(edgeDir, normal));
    float signedDistance = dot(p - edgeStart, edgeNormal);

    TANCCoords result;
    result.t = t;
    result.edgeIndex = edgeIndex;
    result.signedDistance = signedDistance;
    result.projectDistance = projectDistance;
    result.closestCorner = 0;
    result.isCorner = false;
    return result;
}

float3 TANCToWorld(TANCCoords tanc, float3 v1, float3 v2, float3 v3) {
    float t = tanc.t;
    int edgeIndex = tanc.edgeIndex;
    float signedDistance = tanc.signedDistance;
    float projectDistance = tanc.projectDistance;

    float3 a, b;
    switch (edgeIndex) {
        case 0: a = v1; b = v2; break;
        case 1: a = v2; b = v3; break;
        case 2: a = v3; b = v1; break;
        default: return float3(0,0,0);
    }

    float3 edge1 = v2 - v1;
    float3 edge2 = v3 - v1;
    float3 triangleNormal = normalize(cross(edge1, edge2));

    if (tanc.isCorner) {
        float3 corner;
        switch (tanc.closestCorner) {
            case 0: corner = v1; break;
            case 1: corner = v2; break;
            case 2: corner = v3; break;
            default: return float3(0,0,0);
        }

        float3 startEdge = a;
        float3 endEdge = b;
        float3 cornerMax = normalize(cross(triangleNormal, corner - startEdge));
        float3 cornerMin = normalize(cross(corner - endEdge, triangleNormal));
        float3 dir = normalize(cornerMin * (1 - t) + cornerMax * t);
        return corner + dir * signedDistance + triangleNormal * projectDistance;
    } else {
        float3 edgeDir = normalize(b - a);
        float3 edgeNormal = normalize(cross(edgeDir, triangleNormal));
        float3 edgePoint = a * (1 - t) + b * t;
        return edgePoint + edgeNormal * signedDistance + triangleNormal * projectDistance;
    }
}



// Calculate the distance from a point to a line segment
float DistanceToEdge(float3 p, float3 edgeStart, float3 edgeEnd) {
    float3 edge = edgeEnd - edgeStart;
    float3 pointToStart = p - edgeStart;

    // Project the point onto the edge
    float t = dot(pointToStart, edge) / dot(edge, edge);

    // Clamp the projection to the segment
    t = clamp(t, 0.0, 1.0);

    // Find the closest point on the edge
    float3 closestPoint = edgeStart + t * edge;

    // Return the distance from the point to the closest point on the edge
    return length(p - closestPoint);
}

// Calculate the barycentric coordinates of a point relative to a triangle
float3 BarycentricCoordinates(float3 p, float3 a, float3 b, float3 c) {
    float3 v0 = b - a;
    float3 v1 = c - a;
    float3 v2 = p - a;

    float d00 = dot(v0, v0);
    float d01 = dot(v0, v1);
    float d11 = dot(v1, v1);
    float d20 = dot(v2, v0);
    float d21 = dot(v2, v1);

    float denom = d00 * d11 - d01 * d01;
    float v = (d11 * d20 - d01 * d21) / denom;
    float w = (d00 * d21 - d01 * d20) / denom;
    float u = 1.0 - v - w;

    return float3(u, v, w);
}

// Calculate the minimum distance from a point to the surface, edges, or vertices of a triangle
float DistanceToTriangle(TriangleVerts t, float3 p) {
    // Calculate the triangle's normal
    float3 edge1 = t.v1 - t.v0;
    float3 edge2 = t.v2 - t.v0;
    float3 normal = normalize(cross(edge1, edge2));

    // Calculate the signed distance from the point to the triangle's plane
    float planeDistance = dot(p - t.v0, normal);

    // Project the point onto the triangle's plane
    float3 projectedPoint = p - planeDistance * normal;

    // Calculate barycentric coordinates of the projected point
    float3 barycentric = BarycentricCoordinates(projectedPoint, t.v0, t.v1, t.v2);

    // Check if the projected point is inside the triangle
    bool isInside = (barycentric.x >= 0.0) && (barycentric.y >= 0.0) && (barycentric.z >= 0.0);

    if (isInside) {
        // The closest point is on the surface of the triangle
        return abs(planeDistance); // Perpendicular distance to the plane
    } else {
        // The closest point is on one of the edges or vertices
        float distAB = DistanceToEdge(p, t.v0, t.v1);
        float distBC = DistanceToEdge(p, t.v1, t.v2);
        float distCA = DistanceToEdge(p, t.v2, t.v0);

        float distA = length(p - t.v0);
        float distB = length(p - t.v1);
        float distC = length(p - t.v2);

        return min(min(min(distAB, distBC), distCA), min(min(distA, distB), distC));
    }
}

float3 ClosestPointOnTriangle(float3 p, float3 v0, float3 v1, float3 v2) {
    float3 edge1 = v1 - v0;
    float3 edge2 = v2 - v0;
    float3 normal = normalize(cross(edge1, edge2));

    // Project point onto the plane of the triangle
    float3 toPoint = p - v0;
    float perpDistance = dot(toPoint, normal);
    float3 projPoint = p - perpDistance * normal;

    // Compute barycentric coordinates
    float3 toProj = projPoint - v0;
    float dot11 = dot(edge1, edge1);
    float dot12 = dot(edge1, edge2);
    float dot22 = dot(edge2, edge2);
    float dot1p = dot(edge1, toProj);
    float dot2p = dot(edge2, toProj);
    float denom = dot11 * dot22 - dot12 * dot12;

    float u = (dot22 * dot1p - dot12 * dot2p) / denom;
    float v = (dot11 * dot2p - dot12 * dot1p) / denom;
    float w = 1.0f - u - v;

    // Clamp barycentric coordinates to the triangle
    if (u < 0.0f) {
        u = 0.0f;
        v = clamp(v, 0.0f, 1.0f);
        w = 1.0f - v;
    } else if (v < 0.0f) {
        v = 0.0f;
        u = clamp(u, 0.0f, 1.0f);
        w = 1.0f - u;
    } else if (w < 0.0f) {
        w = 0.0f;
        u = clamp(u, 0.0f, 1.0f);
        v = 1.0f - u;
    }

    // Compute the closest point
    return u * v1 + v * v2 + w * v0;
}

struct TranslateOut {
    float3 pos;
    float4 col;
};

TranslateOut translatePoint(int splatIndex, float3 p) {
    // Cache splat links data once
    SplatLink currentSplat = LoadSplatLink(splatIndex);

    
    // First pass: gather valid triangles and calculate initial values
    float weights[8];
    int validCount = 0;
    float totalWeight = 0.0;
    int closestId = -1;
    float closestDist = 3.402823466e+30;
    float4 avgColor = float4(0,0,0,0);
    
    // First pass to gather triangle data and determine weights
    [unroll]
    for (int i = 0; i < 8; i++) {
        weights[i] = 0;

        int triangleId = currentSplat.triangleIds[i];
        float triangleWeight = currentSplat.triangleWeights[i];
        
        // Skip invalid triangles early
        if (triangleId == -1 || triangleWeight <= 0) continue;
        
        // Fetch triangle data once
        Triangle t = _MeshIndices[triangleId];
        TriangleVerts tv;
        tv.v0 = _VertexBasePos[t.v0];
        tv.v1 = _VertexBasePos[t.v1];
        tv.v2 = _VertexBasePos[t.v2];

        // Calculate triangle properties once
        float3 edge1 = tv.v1 - tv.v0;
        float3 edge2 = tv.v2 - tv.v0;
        float3 crossProduct = cross(edge1, edge2);
        float surfaceArea = 0.5 * length(cross(edge1, edge2));
        if (surfaceArea <= 0.0) continue;
        
        // Cache triangle data for reuse in second pass
        float3 normal = normalize(crossProduct);
        float3 center = (tv.v0 + tv.v1 + tv.v2) / 3.0;
        
        // Compute perpendicular distance once
        float3 toPoint = p - tv.v0;
        float perpDistance = dot(toPoint, normal);
        
        // Project point onto triangle plane once
        float3 projPoint = p - perpDistance * normal;
        
        // Check if point is inside triangle and cache result
        
        // Compute weight based on distance and perpendicular distance
        float distanceToCentroid = length(projPoint - center);
        // float weight = 1 / (exp(DistanceToTriangle(tv, p)) + 1e-5);
        float weight = triangleWeight * surfaceArea / (exp(distanceToCentroid) * sqrt(abs(perpDistance) + 1e-5));

        if (weight > 0) {
            totalWeight += weight;
            weights[i] = weight;
            validCount++;
        } else {
            weights[i] = 0;
            // Find closest triangle for fallback
            float dist = length(p - center);
            if (dist < closestDist) {
                closestDist = dist;
                closestId = i;
            }
        }        
    }

    // Handle case where no valid weights were found
    if (validCount == 0) {
        if (closestId == -1) {
            TranslateOut to;
            to.pos = float3(0,0,0);
            to.col = float4(0,0,0,0);
            return to;
        }
        weights[closestId] = 1.0;
        totalWeight = 1.0;
    } 
    
    if (totalWeight == 0) {
        TranslateOut to;
        to.pos = p;
        to.col = float4(0,0,0,0);
        return to;    
    }

    
    // Second pass: transform point using cached data
    float3 finalPoint = float3(0,0,0);
    [unroll]
    for (int j = 0; j < 8; j++) {
        float normalizedWeight = weights[j] / totalWeight;

        if (normalizedWeight <= 0) continue;
        
        int triangleId = currentSplat.triangleIds[j];

        Triangle t = _MeshIndices[triangleId];
        TriangleVerts tv;
        tv.v0 = _VertexBasePos[t.v0];
        tv.v1 = _VertexBasePos[t.v1];
        tv.v2 = _VertexBasePos[t.v2];

        TriangleVerts tvm;
        tvm.v0 = _VertexModPos[t.v0];
        tvm.v1 = _VertexModPos[t.v1];
        tvm.v2 = _VertexModPos[t.v2];

        bool isInside = isPointInsideTriangle(p, tv.v0, tv.v1, tv.v2);
        if (isInside) {
            // Pre-calculated triangle data
            float3 center = (tv.v0 + tv.v1 + tv.v2) / 3.0;
            float3 edge1 = tv.v1 - tv.v0;
            float3 edge2 = tv.v2 - tv.v0;
            float3 normal = normalize(cross(edge1, edge2));
            float3 autoVert = center + normal;
            
            // Modified triangle data
            float3 modCenter = (tvm.v0 + tvm.v1 + tvm.v2) / 3.0;
            
            // Avoid recalculating cross product
            float3 modEdge1 = tvm.v1 - tvm.v0;
            float3 modEdge2 = tvm.v2 - tvm.v0;
            float3 modNormal = normalize(cross(modEdge1, modEdge2));
            float3 modAutoVert = modCenter + modNormal;
            
            float4 barycentricCoords = calculateTetraBarycentricCoordinates(p, tv.v0, tv.v1, tv.v2, autoVert);
            
            float3 baryTransformed = barycentricCoords.x * tvm.v0 + 
                                     barycentricCoords.y * tvm.v1 + 
                                     barycentricCoords.z * tvm.v2 + 
                                     barycentricCoords.w * modAutoVert;
            
            finalPoint += baryTransformed * normalizedWeight;
        } else {
            // Use pre-computed TANC coordinates
            TANCCoords tancCoords = triangleAlignedNormalizedCoordinates(p, tv.v0, tv.v1, tv.v2);
            float3 tancTransformed = TANCToWorld(tancCoords, tvm.v0, tvm.v1, tvm.v2);
            
            finalPoint += tancTransformed * normalizedWeight;
        }
    }

    [unroll]
    for (int j = 0; j < 8; j++) {
        float normalizedWeight = weights[j] / totalWeight;

        if (normalizedWeight <= 0) continue;
        int triangleId = currentSplat.triangleIds[j];

        Triangle t = _MeshIndices[triangleId];
        TriangleVerts tvm;
        tvm.v0 = _VertexModPos[t.v0];
        tvm.v1 = _VertexModPos[t.v1];
        tvm.v2 = _VertexModPos[t.v2];
        float3 projectedPoint = ClosestPointOnTriangle(finalPoint, tvm.v0, tvm.v1, tvm.v2);

        // Update Color
        avgColor += SampleTextureWorldSpace(projectedPoint) * normalizedWeight;
    }

    if (abs(totalWeight - 1) < 0.00001) {
        finalPoint = p;
    }
    
    TranslateOut to;
    to.pos = finalPoint;
    to.col = avgColor;
    return to;
}



half calculateColorVariance(half4 colors[6], int count) {
    half4 meanColor = half4(0, 0, 0, 0);

    // Calculate the mean color
    for (int i = 0; i < count; i++) {
        meanColor += colors[i];
    }
    meanColor /= count;

    // Calculate the variance
    half variance = 0;
    for (int i = 0; i < count; i++) {
        half4 diff = colors[i] - meanColor;
        variance += dot(diff, diff); // Sum of squared differences
    }
    variance /= count; // Average variance

    // Normalize variance to [0, 1] range
    variance = saturate(variance);

    return variance;
}

half4 getModColor(int splatIndex, SplatData splat) {
    half4 avgColor = half4(0, 0, 0, 0);
    half totalWeight = 0;
    half similarity = 0;

    SplatLink currentSplat = LoadSplatLink(splatIndex);
    Tetrahedron hex = computeTetrahedronFromEllipsoid(splat.pos, splat.scale, splat.rot);
    TranslateOut to;

    half3 colorsLab[4];

    // Process each vertex of the hexahedron
    to = translatePoint(splatIndex, hex.v0);
    colorsLab[0] = (to.col.rgb);
    avgColor.rgb += colorsLab[0] / 4;

    to = translatePoint(splatIndex, hex.v1);
    colorsLab[1] = (to.col.rgb);
    avgColor.rgb += colorsLab[1] / 4;

    to = translatePoint(splatIndex, hex.v2);
    colorsLab[2] = (to.col.rgb);
    avgColor.rgb += colorsLab[2] / 4;

    to = translatePoint(splatIndex, hex.v3);
    colorsLab[3] = (to.col.rgb);
    avgColor.rgb += colorsLab[3] / 4;

    // Compute perceptual similarity using Delta-E 2000
    for (int i = 0; i < 4; i++) {
        similarity += 1.0 - DeltaE2000(colorsLab[i], avgColor.rgb) / 100.0;
    }
    similarity /= 4.0;
    similarity = similarity * similarity * similarity; // Cubic response
    avgColor.a *= similarity; // Store similarity in alpha

    return avgColor;
}




ModSplat calcModSplat(int splatIndex, SplatData splat) {
    
    SplatLink currentSplat = LoadSplatLink(splatIndex);

    bool ignore = CanIgnoreModifiers(currentSplat);

    float4 avgColor = float4(0,0,0,0);
    ModSplat m;
    if (ignore) {
        m.pos = splat.pos;
        m.scale = splat.scale;
        m.rot = splat.rot;
    } else {
        // Ensure numerical stability
        float maxScale = max(splat.scale.x, max(splat.scale.y, splat.scale.z));
        splat.scale = max(splat.scale, maxScale / 50.0);

        Tetrahedron baseTetra = computeTetrahedronFromEllipsoid(splat.pos, splat.scale, splat.rot);
        Tetrahedron transformTetra;
        TranslateOut to;
        to = translatePoint(splatIndex, baseTetra.v0);
        transformTetra.v0 = to.pos;
        to = translatePoint(splatIndex, baseTetra.v1);
        transformTetra.v1 = to.pos;
        to = translatePoint(splatIndex, baseTetra.v2);
        transformTetra.v2 = to.pos;
        to = translatePoint(splatIndex, baseTetra.v3);
        transformTetra.v3 = to.pos;

        fitSteinerCircumellipsoid(transformTetra, m);
    }
    m.color = getModColor(splatIndex, splat);

    return m;
}

#endif