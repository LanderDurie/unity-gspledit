using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;

namespace UnityEngine.GsplEdit
{
    public class MeshUtils
    {
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct SplatData
        {
            public Vector3 center;
            public fixed float vertices[12 * 3];
            public fixed int indices[60];
            public float opacity;
            public Vector3 boundMin;
            public Vector3 boundMax;
            public Quaternion rot;
            public Vector3 scale;

            public static int GetSize()
            {
                return sizeof(float) * (3 + 12 * 3 + 1 + 3 + 3 + 4 + 3);
            }

            public Bounds GetBounds()
            {
                return new Bounds(center, boundMax - boundMin);
            }

            public bool IsPointInsideIcosahedron(Vector3 point)
            {
                // Convert the point to local space
                Quaternion inverseRotation = Quaternion.Inverse(rot);
                Vector3 localPoint = inverseRotation * (point - center);
                localPoint = new Vector3(
                    localPoint.x / scale.x,
                    localPoint.y / scale.y,
                    localPoint.z / scale.z
                );
                int intersectionCount = 0;

                // Iterate over each triangle in the icosahedron
                for (int j = 0; j < 60; j += 3)
                {
                    Vector3 v0 = new Vector3(vertices[j * 3], vertices[j * 3 + 1], vertices[j * 3 + 2]);
                    Vector3 v1 = new Vector3(vertices[(j+1) * 3], vertices[(j+1) * 3 + 1], vertices[(j+1) * 3 + 2]);
                    Vector3 v2 = new Vector3(vertices[(j+2) * 3], vertices[(j+2) * 3 + 1], vertices[(j+2) * 3 + 2]);

                    // Check if a ray from the point in a fixed direction (e.g., +X) intersects the triangle
                    if (RayIntersectsTriangle(localPoint, Vector3.right, v0, v1, v2))
                    {
                        intersectionCount++;
                    }
                }

                // If the number of intersections is odd, the point is inside
                return (intersectionCount % 2) == 1;
            }

            private bool RayIntersectsTriangle(Vector3 rayOrigin, Vector3 rayDir, Vector3 v0, Vector3 v1, Vector3 v2)
            {
                const float EPSILON = 1e-6f;
                Vector3 edge1 = v1 - v0;
                Vector3 edge2 = v2 - v0;
                Vector3 h = Vector3.Cross(rayDir, edge2);
                float a = Vector3.Dot(edge1, h);

                if (a > -EPSILON && a < EPSILON)
                    return false; // Parallel ray

                float f = 1.0f / a;
                Vector3 s = rayOrigin - v0;
                float u = f * Vector3.Dot(s, h);

                if (u < 0.0f || u > 1.0f)
                    return false;

                Vector3 q = Vector3.Cross(s, edge1);
                float v = f * Vector3.Dot(rayDir, q);

                if (v < 0.0f || u + v > 1.0f)
                    return false;

                float t = f * Vector3.Dot(edge2, q);
                return t > EPSILON;
            }
        }
    }
}