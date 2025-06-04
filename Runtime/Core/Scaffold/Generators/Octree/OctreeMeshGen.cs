using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace UnityEngine.GsplEdit
{
    public static class OctreeMeshGen
    {
        public static void Gen(List<Vector3> vertices, List<int> indices, Dictionary<EdgeKey, List<OctreeNode>> edgeMap)
        {
            // Track unique vertices and their indices
            Dictionary<Vector3, int> vertexIndexMap = new Dictionary<Vector3, int>();

            foreach (var edge in edgeMap)
            {
                GenerateTrianglesForEdge(vertices, indices, edge, vertexIndexMap);
            }
        }

        // private static void GatherLeafNodes(OctreeNode current, List<OctreeNode> l)
        // {
        //     if (current.m_IsLeaf && current.m_ContainsSurface)
        //     {
        //         l.Add(current);
        //     }

        //     if (!current.m_IsLeaf)
        //     {
        //         foreach (var child in current.m_Children)
        //         {
        //             if (child != null)
        //             {
        //                 GatherLeafNodes(child, l);
        //             }
        //         }
        //     }
        // }

        private static void GenerateTrianglesForEdge(
            List<Vector3> vertices,
            List<int> triangles,
            KeyValuePair<EdgeKey, List<OctreeNode>> edgeData,
            Dictionary<Vector3, int> vertexIndexMap)
        {
            EdgeKey edge = edgeData.Key;
            List<OctreeNode> nodes = edgeData.Value;

            // Need at least 3 points for meaningful triangulation
            if (nodes.Count < 3)
            {
                return;
            }

            if (nodes.Count > 4)
            {
                Debug.Log(nodes.Count);
            }

            // Extract vertex positions
            List<Vector3> points = nodes.Select(n => n.m_VertexPosition).ToList();
            points = OrderPoints(points);

            // Compute the triangulation
            List<Triangle> delaunayTriangles = DelaunayTriangulation(points, edge);

            // Add triangles to the mesh
            foreach (var triangle in delaunayTriangles)
            {
                AddTriangle(vertices, triangles,
                triangle.V1, triangle.V2, triangle.V3,
                vertexIndexMap);
            }
        }

        // Helper class to represent a triangle
        private class Triangle
        {
            public Vector3 V1 { get; set; }
            public Vector3 V2 { get; set; }
            public Vector3 V3 { get; set; }
        }


        private static List<Triangle> DelaunayTriangulation(List<Vector3> points, EdgeKey edge)
        {
            List<Triangle> resultTriangles = new List<Triangle>();

            // If less than 4 points, no meaningful triangulation
            // if (points.Count < 4) return resultTriangles;

            // Compute the center of the point set
            // Vector3 center = points.Aggregate(Vector3.zero, (acc, p) => acc + p) / points.Count;

            // Vector3 v0 = points[0];    
            // Vector3 v1 = points[1];
            // Vector3 v2 = points[2];
            // Vector3 v3 = points[3];

            // Vector3 center = (v0 + v1 + v2 + v3) / 4;

            // Vector3 n1 = Vector3.Cross(v1 - v0, v2 - v0).normalized;
            // Vector3 n2 = Vector3.Cross(v3 - v2, v0 - v2).normalized;
            // Vector3 normal = (n1 + n2) / 2;
            Vector3 center = new();
            Vector3 normal = new();
            ComputeCenterAndNormal(points, out center, out normal);

            // Basic Delaunay-inspired triangulation
            for (int i = 0; i < points.Count - 2; i++)
            {
                Triangle triangle;
                if (Vector3.Dot(normal, (edge.End - edge.Start).normalized) > 0)
                {
                    triangle = new Triangle
                    {
                        V1 = points[0],
                        V2 = points[i + 1],
                        V3 = points[i + 2]
                    };
                }
                else
                {
                    triangle = new Triangle
                    {
                        V1 = points[0],
                        V2 = points[i + 2],
                        V3 = points[i + 1]
                    };
                }
                resultTriangles.Add(triangle);
            }

            return resultTriangles;
        }

        private static List<Vector3> OrderPoints(List<Vector3> points)
        {
            // Calculate centroid (just in case center isn't exact centroid)
            Vector3 centroid = Vector3.zero;
            foreach (Vector3 p in points) centroid += p;
            centroid /= points.Count;

            // Get normal of the plane (assuming they're roughly coplanar)
            Vector3 normal = Vector3.Cross(points[1] - points[0], points[2] - points[0]).normalized;

            // Sort points by angle around center
            return points.OrderBy(p =>
            {
                Vector3 dir = p - centroid;
                float angle = Mathf.Atan2(
                    Vector3.Dot(normal, Vector3.Cross(points[0] - centroid, dir)),
                    Vector3.Dot(dir, points[0] - centroid)
                );
                return angle;
            }).ToList();
        }

        private static void AddTriangle(
            List<Vector3> vertices,
            List<int> triangles,
            Vector3 v0,
            Vector3 v1,
            Vector3 v2,
            Dictionary<Vector3, int> vertexIndexMap)
        {
            // Get or add vertex indices
            int index0 = GetOrAddVertexIndex(vertices, vertexIndexMap, v0);
            int index1 = GetOrAddVertexIndex(vertices, vertexIndexMap, v1);
            int index2 = GetOrAddVertexIndex(vertices, vertexIndexMap, v2);

            // Add the triangle indices
            triangles.Add(index0);
            triangles.Add(index1);
            triangles.Add(index2);
        }

        private static int GetOrAddVertexIndex(
            List<Vector3> vertices,
            Dictionary<Vector3, int> vertexIndexMap,
            Vector3 vertex)
        {
            // Check if the vertex already exists in the map
            if (vertexIndexMap.TryGetValue(vertex, out int index))
            {
                return index;
            }

            // Add the new vertex to the list and map
            index = vertices.Count;
            vertices.Add(vertex);
            vertexIndexMap[vertex] = index;
            return index;
        }


        public static void ComputeCenterAndNormal(List<Vector3> points, out Vector3 center, out Vector3 normal)
        {
            // --- 1. Compute Center (Average of All Points) ---
            center = Vector3.zero;
            foreach (Vector3 point in points)
            {
                center += point;
            }
            center /= points.Count;

            // --- 2. Compute Normal (Averaged Cross Products) ---
            normal = Vector3.zero;
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 current = points[i];
                Vector3 next = points[(i + 1) % points.Count]; // Wrap around to first point

                // Cross product of two adjacent edges (using center as reference)
                Vector3 edge1 = current - center;
                Vector3 edge2 = next - center;
                normal += Vector3.Cross(edge1, edge2);
            }
            normal = normal.normalized; // Normalize the final result
        }

        public static void CenterTest(List<Vector3> vertices, List<int> indices, OctreeNode node)
        {
            if (!node.m_IsLeaf)
            {
                foreach (var child in node.m_Children)
                {
                    if (child != null)
                    {
                        CenterTest(vertices, indices, child);
                    }
                }
            }
            else
            {
                if (node.m_ContainsSurface)
                {
                    vertices.Add(node.m_VertexPosition);
                    int s = indices.Count() / 3;
                    indices.Add(s);
                    indices.Add(s);
                    indices.Add(s);
                }
            }
        }
        
        public static void BoundsTest(List<Vector3> vertices, List<int> indices, OctreeNode node)
        {
            if (!node.m_IsLeaf)
            {
                foreach (var child in node.m_Children)
                {
                    if (child != null)
                    {
                        BoundsTest(vertices, indices, child);
                    }
                }
            }
            else
            {
                if (node.m_ContainsSurface)
                {
                    // Get the 8 corners of the bounding box
                    Bounds b = node.m_Bounds;
                    Vector3 c = b.center;
                    Vector3 e = b.extents;

                    Vector3[] corners = new Vector3[8];
                    corners[0] = c + new Vector3(-e.x, -e.y, -e.z);
                    corners[1] = c + new Vector3(e.x, -e.y, -e.z);
                    corners[2] = c + new Vector3(e.x, -e.y, e.z);
                    corners[3] = c + new Vector3(-e.x, -e.y, e.z);
                    corners[4] = c + new Vector3(-e.x, e.y, -e.z);
                    corners[5] = c + new Vector3(e.x, e.y, -e.z);
                    corners[6] = c + new Vector3(e.x, e.y, e.z);
                    corners[7] = c + new Vector3(-e.x, e.y, e.z);

                    int baseIndex = vertices.Count;
                    vertices.AddRange(corners);

                    // 12 triangles to draw the cube (2 per face)
                    int[] cubeTris = new int[]
                    {
                        0, 2, 1, 0, 3, 2, // Bottom
                        4, 5, 6, 4, 6, 7, // Top
                        0, 1, 5, 0, 5, 4, // Front
                        1, 2, 6, 1, 6, 5, // Right
                        2, 3, 7, 2, 7, 6, // Back
                        3, 0, 4, 3, 4, 7  // Left
                    };

                    foreach (int i in cubeTris)
                    {
                        indices.Add(baseIndex + i);
                    }
                }
            }
        }

    }
}