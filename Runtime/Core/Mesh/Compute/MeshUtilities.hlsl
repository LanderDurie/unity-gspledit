#ifndef MESH_UTILS_HLSL
#define MESH_UTILS_HLSL

struct TriangleProperties {
    uint vertexId1;
    uint vertexId2;
    uint vertexId3;
};

struct VertexPos {
    float3 pos;
    float3 posMod;     
};

struct Triangle {
    VertexPos v0;
    VertexPos v1;
    VertexPos v2;
};

uint _VertexCount;
uint _IndexCount;
RWStructuredBuffer<VertexPos> _MeshVertexPos;
RWStructuredBuffer<TriangleProperties> _MeshIndices;

TriangleProperties LoadTriangleProps(uint triangleId) {
    return _MeshIndices[triangleId];
}

VertexPos LoadVertexPos1(uint triangleId) {
    return _MeshVertexPos[_MeshIndices[triangleId].vertexId1];
}

VertexPos LoadVertexPos2(uint triangleId) {
    return _MeshVertexPos[_MeshIndices[triangleId].vertexId2];
}

VertexPos LoadVertexPos3(uint triangleId) {
    return _MeshVertexPos[_MeshIndices[triangleId].vertexId3];
}

VertexPos LoadVertexPos(uint vertexId) {
    return _MeshVertexPos[vertexId];
}

Triangle GetTriangle(uint triangleId) {
    Triangle t;
    t.v0 = LoadVertexPos1(triangleId);
    t.v1 = LoadVertexPos2(triangleId);
    t.v2 = LoadVertexPos3(triangleId);
    return t;
}

#endif