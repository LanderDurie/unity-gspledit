#ifndef TANC_HLSL
#define TANC_HLSL

#ifndef EPSILON
#define EPSILON 1e-6f
#endif

struct TANC {
    float coord1;    // First coordinate (barycentric u, or edge t)
    float coord2;    // Second coordinate (barycentric v, or distance)
    float coord3;    // Third coordinate (barycentric w, or projectDistance)
    int type;        // Type indicator: -4 for barycentric, 0-2 for edge, 3-5 for corners
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


TANC WorldToTanc(float3 p, float3 v1, float3 v2, float3 v3)
{
    TANC result;
    result.type = 0;
    result.coord1 = 0;
    result.coord2 = 0;
    result.coord3 = 0;

    float3 edge1 = v2 - v1;
    float3 edge2 = v3 - v1;
    float3 normal = normalize(cross(edge1, edge2));
    
    // Check for degenerate triangle
    if (dot(normal, normal) < EPSILON)
    {
        result.type = -1;
        return result;
    }
    
    // Project point onto triangle plane
    float projectDistance = dot(normal, p - v1);
    float3 projectedPoint = p - projectDistance * normal;
    
    // Calculate barycentric coordinates
    float area = length(cross(edge1, edge2)) * 0.5f;
    float3 v1p = projectedPoint - v1;
    float3 v2p = projectedPoint - v2;
    float3 v3p = projectedPoint - v3;
    
    float area1 = length(cross(v2p, v3p)) * 0.5f;
    float area2 = length(cross(v3p, v1p)) * 0.5f;
    float area3 = length(cross(v1p, v2p)) * 0.5f;
    
    float u = area1 / area;
    float v = area2 / area;
    float w = area3 / area;
    
    // Check if point is inside triangle
    if (u >= -EPSILON && v >= -EPSILON && w >= -EPSILON && abs(u + v + w - 1.0f) < EPSILON) {
        result.coord1 = u;
        result.coord2 = v;
        result.coord3 = projectDistance;
        result.type = 0;
        return result;
    }
    
    // Outside triangle - find closest feature
    float3 vertices[3] = { v1, v2, v3 };
    float3 edges[6] = { v1, v2, v2, v3, v3, v1 };
    float minDistance = 3.402823466e+38f; // float.MaxValue
    
    // Check corners first
    for (int i = 0; i < 3; i++) {
        float d = distance(projectedPoint, vertices[i]);
        if (d < minDistance)
        {
            minDistance = d;
            result.type = i + 1; // 1-3 for corners
            
            float3 edgeA;
            float3 edgeB;
            
            switch (i)
            {
                case 0: // Corner v1
                    edgeA = v1 - v2;
                    edgeB = v1 - v3;
                    break;
                case 1: // Corner v2
                    edgeA = v2 - v3;
                    edgeB = v2 - v1;
                    break;
                case 2: // Corner v3
                    edgeA = v3 - v1;
                    edgeB = v3 - v2;
                    break;
                default:
                    edgeA = float3(0, 0, 0);
                    edgeB = float3(0, 0, 0);
                    break;
            }
            
            // Calculate outward-facing normals (perpendicular to edges in the plane)
            float3 edgeANormal = normalize(cross(edgeA, normal));
            float3 edgeBNormal = normalize(cross(normal, edgeB));
            
            float3 toPoint = normalize(vertices[i] - projectedPoint);

            // Calculate angle between edge normals (full range)
            float angleBetweenNormals = degrees(acos(dot(edgeANormal, edgeBNormal)));
            float angleFromANormal = degrees(acos(dot(edgeANormal, toPoint)));
            float normalizedAngle = angleFromANormal / angleBetweenNormals;

            // Calculate perpendicular distance (distance to corner)
            float perpDist = d;
            
            result.coord1 = normalizedAngle; // Angle between edge normals (0-1)
            result.coord2 = perpDist;        // Distance from corner
            result.coord3 = projectDistance; // Signed distance to surface
        }
    }
    
    // Check edges
    for (int i = 0; i < 3; i++)
    {
        float3 a = edges[i * 2];
        float3 b = edges[i * 2 + 1];
        float3 ab = b - a;
        float3 ap = projectedPoint - a;
        
        float projection = dot(ap, ab) / dot(ab, ab);
        projection = clamp(projection, 0.0f, 1.0f);
        
        float3 closestPoint = a + projection * ab;
        float d = distance(projectedPoint, closestPoint);
        
        if (d < minDistance)
        {
            minDistance = d;
            result.type = i + 4; // 4-6 for edges
            
            // Calculate position along edge (0-1)
            float edgePos = projection;
            
            // Calculate perpendicular distance to edge
            float perpDist = d;
            
            // Calculate signed distance to surface
            float signedDist = projectDistance;
            
            result.coord1 = edgePos;
            result.coord2 = perpDist;
            result.coord3 = signedDist;
        }
    }
    
    return result;
}

float3 TancToWorld(TANC coords, float3 v1, float3 v2, float3 v3)
{
    float3 vertices[3] = { v1, v2, v3 };
    float3 edges[6] = { v1, v2, v2, v3, v3, v1 };
    float3 edge1 = v2 - v1;
    float3 edge2 = v3 - v1;
    float3 normal = normalize(cross(edge1, edge2));
    
    // Handle barycentric case (type 0)
    if (coords.type == 0)
    {
        return v1 * coords.coord1 + v2 * coords.coord2 + v3 * (1 - coords.coord1 - coords.coord2) + 
               normal * coords.coord3;
    }
    
    // Handle corner cases (types 1-3)
    if (coords.type >= 1 && coords.type <= 3)
    {
        int cornerIndex = coords.type - 1;
        float3 corner = vertices[cornerIndex];
        float3 edgeA, edgeB;
        
        // Determine edges based on corner index
        if (cornerIndex == 0) // v1
        {
            edgeA = v1 - v2;
            edgeB = v1 - v3;
        }
        else if (cornerIndex == 1) // v2
        {
            edgeA = v2 - v3;
            edgeB = v2 - v1;
        }
        else // v3
        {
            edgeA = v3 - v1;
            edgeB = v3 - v2;
        }
        
        // Calculate outward-facing normals
        float3 edgeANormal = normalize(cross(normal, edgeA));
        float3 edgeBNormal = normalize(cross(edgeB, normal));
        
        // Implement spherical interpolation (slerp)
        float dotProduct = dot(edgeANormal, edgeBNormal);
        dotProduct = clamp(dotProduct, -1.0f, 1.0f);
        float theta = acos(dotProduct) * coords.coord1;
        float3 relativeVec = normalize(edgeBNormal - edgeANormal * dotProduct);
        float3 interpolatedDir = edgeANormal * cos(theta) + relativeVec * sin(theta);
        
        return corner + interpolatedDir * coords.coord2 + normal * coords.coord3;
    }
    
    // Handle edge cases (types 4-6)
    if (coords.type >= 4 && coords.type <= 6)
    {
        int edgeIndex = coords.type - 4;
        float3 edgeStart = edges[edgeIndex * 2];
        float3 edgeEnd = edges[edgeIndex * 2 + 1];
        float3 edgeDir = normalize(edgeStart - edgeEnd);
        
        // Calculate point along edge
        float3 edgePoint = edgeStart + (edgeEnd - edgeStart) * coords.coord1;
        
        // Calculate perpendicular direction
        float3 perpDir = normalize(cross(normal, edgeDir));
        
        // Calculate final position
        return edgePoint + perpDir * coords.coord2 + normal * coords.coord3;
    }
    
    // Default case
    return float3(0, 0, 0);
}

#endif