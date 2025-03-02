using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.GsplEdit
{
    public class MeshGenUtils
    {
        public static void OptimizeMesh(ref Vertex[] vertices, ref int[] indices)
        {
            if (vertices == null || vertices.Length == 0 || indices == null || indices.Length == 0)
            {
                Debug.LogWarning("No vertices or indices to optimize.");
                return;
            }

            float tolerance = 0.0001f; // Tolerance for merging vertices
            float inverseTolerance = 1.0f / tolerance; // Precompute for performance

            Dictionary<Vector3Int, List<int>> spatialHash = new Dictionary<Vector3Int, List<int>>();
            List<Vertex> uniqueVertices = new List<Vertex>();
            List<int> optimizedIndices = new List<int>();

            for (int i = 0; i < indices.Length; i++)
            {
                Vertex vertex = vertices[indices[i]];

                Vector3Int hashKey = new Vector3Int(
                    Mathf.FloorToInt(vertex.position.x * inverseTolerance),
                    Mathf.FloorToInt(vertex.position.y * inverseTolerance),
                    Mathf.FloorToInt(vertex.position.z * inverseTolerance)
                );

                // Check if a close vertex already exists in the hash bucket
                if (!spatialHash.TryGetValue(hashKey, out var bucket))
                {
                    bucket = new List<int>();
                    spatialHash[hashKey] = bucket;
                }

                bool found = false;
                foreach (int existingIndex in bucket)
                {
                    if (AreVerticesClose(vertex, uniqueVertices[(int)existingIndex], tolerance))
                    {
                        optimizedIndices.Add(existingIndex);
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    // Add new unique vertex
                    bucket.Add(uniqueVertices.Count);
                    uniqueVertices.Add(vertex);
                    optimizedIndices.Add(uniqueVertices.Count - 1);
                }
            }

            vertices = uniqueVertices.ToArray();
            indices = optimizedIndices.ToArray();
        }

        public static void ExtractUniqueEdges(int[] indexList, ref Edge[] edgeList)
        {
            // Use a HashSet to avoid duplicate edges
            HashSet<Edge> edgeSet = new HashSet<Edge>();

            for (int i = 0; i < indexList.Length; i += 3)
            {
                int v1 = indexList[i];
                int v2 = indexList[i + 1];
                int v3 = indexList[i + 2];

                edgeSet.Add(new Edge(v1, v2));
                edgeSet.Add(new Edge(v2, v3));
                edgeSet.Add(new Edge(v3, v1));
            }

            edgeList = edgeSet.ToArray();
        }

        public static void ExtractUniqueTriangles(int[] indexList, ref Triangle[] triangleList)
        {
            // Use a HashSet to avoid duplicate edges
            HashSet<Triangle> triangleSet = new HashSet<Triangle>();

            for (int i = 0; i < indexList.Length; i += 3)
            {
                int v1 = indexList[i];
                int v2 = indexList[i + 1];
                int v3 = indexList[i + 2];

                triangleSet.Add(new Triangle(v1, v2, v3));
            }

            triangleList = triangleSet.ToArray();
        }

        private static bool AreVerticesClose(Vertex v1, Vertex v2, float tolerance)
        {
            return Vector3.Distance(v1.position, v2.position) < tolerance;
        }
    }
}