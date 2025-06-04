#ifndef SPLAT_MODIFIERS_HLSL
#define SPLAT_MODIFIERS_HLSL

#include "../../Scaffold/Compute/OffscreenUtilities.hlsl"
#include "../../Links/Compute/LinkUtilities.hlsl"
#include "./SSSamplePoints.hlsl"
#include "./SamplePoints.hlsl"
#include "ColorUtils.hlsl"
#include "tanc.hlsl"

#ifndef QUATERNION_IDENTITY
#define QUATERNION_IDENTITY float4(0, 0, 0, 1)
#endif

#ifndef PI
#define PI 3.14159265359f
#endif 

#ifndef EPSILON
#define EPSILON 1e-6f
#endif

static const int MAX_ITERATIONS = 10;


struct ModSplat {
    float4 pos;     // 16 bytes
    float4 rot;     // 16 bytes
    float4 scale;   // 16 bytes (use .xyz)
    float4 color;   // 16 bytes
}; // 64 bytes total, fully aligned


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
StructuredBuffer<float3> _TriangleProj; // 6 float3s: v1, v2, v3, n1, n2, n3

float4x4 _MatrixObjectToWorld;
float4x4 _MatrixWorldToObject;
float4x4 _MatrixMV;
float4x4 _MatrixP;
float4 _VecWorldSpaceCameraPos;
int _SelectionMode;

#ifndef VP
#define VP
float4x4 _MatrixVP;
#endif

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
    // bool anyLinks = false;
    // [unroll]
    // for (int i = 0; i < 8; i++) {
    //     // Check if valid triangle
    //     int triangleId = currentSplat.triangleIds[i];
    //     if (triangleId == -1) break; // Weights are sorted -> stop on first invalid
        
    //     // Fetch triangle data once
    //     Triangle t = _MeshIndices[triangleId];
    //     if (abs(length(_VertexModPos[t.v0] - _VertexBasePos[t.v0])) > 0.01 ||
    //         abs(length(_VertexModPos[t.v1] - _VertexBasePos[t.v1])) > 0.01 ||
    //         abs(length(_VertexModPos[t.v2] - _VertexBasePos[t.v2])) > 0.01) {
    //         return false;
    //     }

    //     // anyLinks = true;
    //     return false;
    // }
    // return true;
    return false;
}

bool ShouldRemoveSplat(SplatLink currentSplat, float threshold = 0.9999) {
    // float totalWeight = 0;
    bool anyLinks = false;
    [unroll]
    for (int i = 0; i < 8; i++) {
        // Check if valid triangle
        int triangleId = currentSplat.triangleIds[i];
        if (triangleId == -1) break; // Weights are sorted -> stop on first invalid

        anyLinks = true;
        Triangle t = _MeshIndices[triangleId];
        if (IsVertexDeleted(t.v0) || IsVertexDeleted(t.v1) || IsVertexDeleted(t.v2)) return true;

    }
    return !anyLinks;
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

ModSplat fitSteinerCircumellipsoid(Tetrahedron t)
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
    scale = sqrt(3 * scale) / 4;

    // Convert eigenvectors to quaternion
    float4 rotation = quaternionFromMatrix(V);

    ModSplat result;
    result.pos = float4(center, 1);
    result.scale = float4(scale, 1);
    result.rot = rotation;
    return result;
}

// Calculate the distance from a point to a line segment
float DistanceToEdge(float3 p, float3 edgeStart, float3 edgeEnd) {
    float3 edge = edgeEnd - edgeStart;
    float3 pointToStart = p - edgeStart;

    // Project the point onto the edge
    float t = dot(pointToStart, edge) / dot(edge, edge);
    t = clamp(t, 0.0, 1.0);

    // Find the closest point on the edge
    float3 closestPoint = edgeStart + t * edge;

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
    bool valid[8];

    int validCount = 0;
    float totalWeight = 0.0;

    float4 avgColor = float4(0,0,0,0);
    float3 finalPoint = float3(0,0,0);

    // First pass to gather triangle data and determine weights
    [unroll]
    for (int i = 0; i < 8; i++) {
        weights[i] = -1;
        valid[i] = false;

        int triangleId = currentSplat.triangleIds[i];
        
        // Skip invalid triangles early
        if (triangleId == -1) continue;
        
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
        
        // Compute perpendicular distance once
        float3 toPoint = p - tv.v0;         // tv.v0 is a point on the plane
        float distance = abs(dot(toPoint, normal)); // normal is already normalized

        if (distance >= 0) {
            totalWeight += distance;
            weights[i] = distance;
            valid[i] = true;
            validCount++;
        }      
    }

    // Handle case where no valid weights were found
    if (validCount == 0) {
        TranslateOut to;
        to.pos = p;
        to.col = float4(0,0,0,0);
        return to;   
    } 

    // Normalise
    float sum = 0;
    [unroll]
    for (int i = 0; i < 8; i++) {
        weights[i] = log(2 - (weights[i] / totalWeight));
        if (valid[i]) sum += weights[i];
    }

    [unroll]
    for (int i = 0; i < 8; i++) {
        weights[i] = weights[i] / sum;
    }

    
    // Second pass: transform point using cached data
    [unroll]
    for (int j = 0; j < 8; j++) {
        if (!valid[j]) continue;

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

        TANC tc = WorldToTanc(p, tv.v0, tv.v1, tv.v2);
        float3 tancTransformed = TancToWorld(tc, tvm.v0, tvm.v1, tvm.v2);

        finalPoint += tancTransformed * weights[j];
    }

    [unroll]
    for (int j = 0; j < 8; j++) {
        if (!valid[j]) continue;

        int triangleId = currentSplat.triangleIds[j];

        Triangle t = _MeshIndices[triangleId];
        TriangleVerts tvm;
        tvm.v0 = _VertexModPos[t.v0];
        tvm.v1 = _VertexModPos[t.v1];
        tvm.v2 = _VertexModPos[t.v2];
        float3 projectedPoint = ClosestPointOnTriangle(finalPoint, tvm.v0, tvm.v1, tvm.v2);

        // Update Color
        avgColor += SampleTextureWorldSpace(projectedPoint) * weights[j];
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

float3 LinearToSRGB(float3 linearColor) {
    float3 srgbColor;
    [unroll]
    for (int i = 0; i < 3; i++) {
        if (linearColor[i] <= 0.0031308)
            srgbColor[i] = linearColor[i] * 12.92;
        else
            srgbColor[i] = 1.055 * pow(linearColor[i], 1.0 / 2.4) - 0.055;
    }
    return srgbColor;
}



half4 getModColor(int splatIndex, ModSplat splat) {
    const int NUM_SAMPLES = 12;
    half4 weightedColor = half4(0, 0, 0, 0);
    half totalWeight = 0;
    SplatLink currentSplat = LoadSplatLink(splatIndex);
    half3 colorsLab[NUM_SAMPLES];
    float2 samplePoints[NUM_SAMPLES];
    
    float2 innerPoints[6];
    getEdgePoints6(splat.pos, splat.scale.xyz * 0.5, splat.rot, 2.0, innerPoints);
    
    // Get outer ring points
    float2 outerPoints[6];
    getEdgePoints6(splat.pos, splat.scale.xyz, splat.rot, 3.0, outerPoints);
    
    // Combine points into one array
    for (int i = 0; i < 6; i++) {
        samplePoints[i] = innerPoints[i];
    }
    for (int i = 0; i < 6; i++) {
        samplePoints[i + 6] = outerPoints[i];
    }
    
    // Calculate splat center in screen space
    float4 centerClipPos = mul(_MatrixVP, splat.pos);
    float2 centerNDC = centerClipPos.xy / centerClipPos.w;
    float2 centerScreen = (centerNDC * 0.5 + 0.5) * float2(_VecScreenParams.x, _VecScreenParams.y);
    
    // Calculate splat standard deviation based on scale
    float sigma = length(splat.scale.xy) * 0.5;
    
    // Sample colors with Gaussian weighting
    for (int i = 0; i < NUM_SAMPLES; i++) {
        // Sample color at this point
        colorsLab[i] = SampleTextureScreenSpace(samplePoints[i]);
        
        // Calculate distance from the center
        float distance = length(samplePoints[i] - centerScreen) / (_VecScreenParams.x * 0.5);
        
        // Apply Gaussian weight
        // Inner points (first 4) get higher base weight
        half baseWeight = (i < 4) ? 1.5 : 1.0;
        half weight = baseWeight * exp(-distance * distance / (2.0 * sigma * sigma));
        
        // Accumulate weighted color
        weightedColor.rgb += colorsLab[i] * weight;
        totalWeight += weight;
    }
    
    // Normalize by total weight
    if (totalWeight > 0.001) {
        weightedColor.rgb /= totalWeight;
    }
    
    // Compute perceptual similarity with distance weighting
    half similarity = 0;
    half similarityWeight = 0;
    
    for (int i = 0; i < NUM_SAMPLES; i++) {
        half sampleSimilarity = 1.0 - CIEDE2000Similarity(colorsLab[i], weightedColor.rgb);
        
        // Calculate distance from the center for this sample
        float distance = length(samplePoints[i] - centerScreen) / (_VecScreenParams.x * 0.5);
        
        // Weight the similarity based on the Gaussian importance
        // Inner points contribute more to similarity assessment
        half weight = (i < 4) ? 1.5 : 1.0;
        weight *= exp(-distance * distance / (2.0 * sigma * sigma));
        
        similarity += sampleSimilarity * weight;
        similarityWeight += weight;
    }
    
    if (similarityWeight > 0.001) {
        similarity /= similarityWeight;
    }
    similarity *= similarity * similarity;

    // Apply cubic response for more contrast in similarity
    // similarity = smoothstep(0.01, 0.99, similarity);
    
    // Store final alpha
    weightedColor.a = max(0.5, similarity);
    weightedColor.xyz = LinearToSRGB(weightedColor.xyz);
    
    return weightedColor;
}





ModSplat calcModSplat(int splatIndex, SplatData splat) {
    
    SplatLink currentSplat = LoadSplatLink(splatIndex);

    bool ignore = CanIgnoreModifiers(currentSplat);

    float4 avgColor = float4(0,0,0,0);
    ModSplat m;
    m.pos = float4(splat.pos, 1);
    m.scale = float4(splat.scale, 0);
    m.rot = splat.rot;

    if (!ignore) {
        // Ensure numerical stability
        float maxScale = max(splat.scale.x, max(splat.scale.y, splat.scale.z));
        splat.scale.xyz = max(splat.scale.xyz, maxScale / 50.0);

        Tetrahedron baseTetra = computeTetrahedronFromEllipsoid(splat.pos, splat.scale.xyz, splat.rot, 4);
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

        m = fitSteinerCircumellipsoid(transformTetra);
    }
    m.color = getModColor(splatIndex, m);

    return m;
}

float3 warpProject(float3 p, float3 v1, float3 v2, float3 v3, float3 n1, float3 n2, float3 n3, inout bool valid)
{
    float3 normal = normalize(cross(v2 - v1, v3 - v1));
    float d = dot(p - v1, normal);

    float3 v1p = v1 + n1 * (d / dot(normal, n1));
    float3 v2p = v2 + n2 * (d / dot(normal, n2));
    float3 v3p = v3 + n3 * (d / dot(normal, n3));

    float3 u = v2p - v1p;
    float3 v = v3p - v1p;
    float3 w = p - v1p;

    float uu = dot(u, u);
    float uv = dot(u, v);
    float vv = dot(v, v);
    float wu = dot(w, u);
    float wv = dot(w, v);

    float denom = uv * uv - uu * vv;
    float s = (uv * wv - vv * wu) / denom;
    float t = (uv * wu - uu * wv) / denom;

    // false if outside triangle
    valid = !(s < 0.0 || t < 0.0 || s + t > 1.0);

    float3 proj = (1.0 - s - t) * v1 + s * v2 + t * v3;

    float sign = (dot(normal, p - proj) >= 0) ? 1.0f : -1.0f;
    return proj + normal * sign * length(p - proj);
}



ModSplat baryWarp(ModSplat ms) {

    Tetrahedron baseTetra = computeTetrahedronFromEllipsoid(ms.pos, ms.scale.xyz, ms.rot, 4);

    float3 wp0 = baseTetra.v0;
    float3 wp1 = baseTetra.v1;
    float3 wp2 = baseTetra.v2;
    float3 wp3 = baseTetra.v3;

    float3 v1 = _TriangleProj[0];
    float3 v2 = _TriangleProj[1];
    float3 v3 = _TriangleProj[2];
    float3 n1 = _TriangleProj[3];
    float3 n2 = _TriangleProj[4];
    float3 n3 = _TriangleProj[5];

    bool valid = false;
    Tetrahedron warped;
    bool v = true;
    warped.v0 = warpProject(wp0, v1, v2, v3, n1, n2, n3, v);  
    if (v) valid = true;
    warped.v1 = warpProject(wp1, v1, v2, v3, n1, n2, n3, v);  
    if (v) valid = true;
    warped.v2 = warpProject(wp2, v1, v2, v3, n1, n2, n3, v);  
    if (v) valid = true;
    warped.v3 = warpProject(wp3, v1, v2, v3, n1, n2, n3, v);  
    if (v) valid = true;

    ModSplat m;

    if (valid) {
        m = fitSteinerCircumellipsoid(warped);
    } else {
        m = ms;
    }
    float3 normal = normalize(cross(v2 - v1, v3 - v1));
    
    float dist = dot(m.pos - v1, normal);
    dist += 0.3;
    dist /= 0.37;

    if(dist < 0 || dist > 1) {
        m.color = half4(0, 0, 0, 0);
    } else {
        m.color = half4(dist, dist, dist, 1);
    }

    return m;
}

#endif