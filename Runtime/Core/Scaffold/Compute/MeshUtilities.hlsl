#ifndef MESH_UTILS_HLSL
#define MESH_UTILS_HLSL

struct TriangleProperties {
    uint vertexId1;
    uint vertexId2;
    uint vertexId3;
};

struct Vector3 {
    float3 pos;
    float3 posMod; 
    float3 normal;
    float2 uv;    
};

struct Triangle {
    Vector3 v0;
    Vector3 v1;
    Vector3 v2;
};

uint _VertexCount;
uint _IndexCount;
RWStructuredBuffer<Vector3> _MeshVector3;
RWStructuredBuffer<TriangleProperties> _MeshIndices;

TriangleProperties LoadTriangleProps(uint triangleId) {
    return _MeshIndices[triangleId];
}

Vector3 LoadVector31(uint triangleId) {
    return _MeshVector3[_MeshIndices[triangleId].vertexId1];
}

Vector3 LoadVector32(uint triangleId) {
    return _MeshVector3[_MeshIndices[triangleId].vertexId2];
}

Vector3 LoadVector33(uint triangleId) {
    return _MeshVector3[_MeshIndices[triangleId].vertexId3];
}

Vector3 LoadVector3(uint vertexId) {
    return _MeshVector3[vertexId];
}

Triangle GetTriangle(uint triangleId) {
    Triangle t;
    t.v0 = LoadVector31(triangleId);
    t.v1 = LoadVector32(triangleId);
    t.v2 = LoadVector33(triangleId);
    return t;
}

#endif