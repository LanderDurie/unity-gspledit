#ifndef LINK_UTILS_HLSL
#define LINK_UTILS_HLSL

static const uint LINK_COUNT = 8;

struct SplatLink {
    int triangleIds[LINK_COUNT];
    float triangleWeights[LINK_COUNT];
    float triangleX[LINK_COUNT];
    float triangleY[LINK_COUNT];
};

RWStructuredBuffer<SplatLink> _SplatLinks;

SplatLink LoadSplatLink(uint splatIndex) {
    return _SplatLinks[splatIndex];
}

void SetSplatLink(uint splatIndex, SplatLink sl) {
    _SplatLinks[splatIndex] = sl;
}

#endif
