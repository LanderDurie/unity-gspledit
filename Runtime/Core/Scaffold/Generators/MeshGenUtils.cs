using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace UnityEngine.GsplEdit
{
    public class MeshGenUtils
    {
        public static void OptimizeMesh(ref Vector3[] vertices, ref int[] indices)
        {
            if (vertices == null || vertices.Length == 0 || indices == null || indices.Length == 0)
            {
                Debug.LogWarning("No vertices or indices to optimize.");
                return;
            }

            float tolerance = 0.0001f; // Tolerance for merging vertices
            float inverseTolerance = 1.0f / tolerance; // Precompute for performance

            Dictionary<Vector3Int, List<int>> spatialHash = new Dictionary<Vector3Int, List<int>>();
            List<Vector3> uniqueVertices = new List<Vector3>();
            List<int> optimizedIndices = new List<int>();

            for (int i = 0; i < indices.Length; i++)
            {
                Vector3 vertex = vertices[indices[i]];

                Vector3Int hashKey = new Vector3Int(
                    Mathf.FloorToInt(vertex.x * inverseTolerance),
                    Mathf.FloorToInt(vertex.y * inverseTolerance),
                    Mathf.FloorToInt(vertex.z * inverseTolerance)
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

        private static bool AreVerticesClose(Vector3 v1, Vector3 v2, float tolerance)
        {
            return Vector3.Distance(v1, v2) < tolerance;
        }

        public static Mesh UnwrapMesh(Mesh mesh)
        {
            UnwrapParam param;
            UnwrapParam.SetDefaults(out param);

            param.areaError = 0.0f; // Prevent merging based on angle
            // param.packMargin = 0.01f; // Optional: space between UV islands

            // Generate per-triangle UVs
            Vector2[] unwrappedUVs = Unwrapping.GeneratePerTriangleUV(mesh, param);

            // When vertices need multiple UVs, we need to duplicate vertices at UV seams
            Vector3[] origVertices = mesh.vertices;
            Vector3[] origNormals = mesh.normals;
            int[] origTriangles = mesh.triangles;
            
            // New arrays for the unwrapped mesh
            List<Vector3> newVertices = new List<Vector3>();
            List<Vector3> newNormals = new List<Vector3>();
            List<Vector2> newUVs = new List<Vector2>();
            List<int> newTriangles = new List<int>();
            
            // Process each triangle
            for (int i = 0; i < origTriangles.Length; i += 3)
            {
                // For each triangle (3 vertices)
                for (int j = 0; j < 3; j++)
                {
                    int oldIndex = origTriangles[i + j];
                    int newIndex = newVertices.Count;
                    
                    // Add the vertex with its corresponding normal and UV
                    newVertices.Add(origVertices[oldIndex]);
                    newNormals.Add(origNormals[oldIndex]);
                    newUVs.Add(unwrappedUVs[i + j]);
                    
                    // Add the new vertex index to the triangle
                    newTriangles.Add(newIndex);
                }
            }
            
            // Create new mesh
            Mesh unwrappedMesh = new Mesh();
            unwrappedMesh.vertices = newVertices.ToArray();
            unwrappedMesh.triangles = newTriangles.ToArray();
            unwrappedMesh.uv = newUVs.ToArray();
            unwrappedMesh.normals = newNormals.ToArray();
                    
            return unwrappedMesh;
        }

        public static void MergeOverlappingVertices(ref Vector3[] vertices, ref int[] indices, ref Vector3[] normals, float tolerance = 0.001f)
        {
            Vector3[] oldVertices = vertices;
            int[] oldTriangles = indices;
            Vector3[] oldNormals = normals;

            var newVertices = new List<Vector3>();
            var newNormals = new List<Vector3>();
            var remap = new int[oldVertices.Length];
            var normalSums = new Dictionary<int, Vector3>();
            var normalCounts = new Dictionary<int, int>();

            Dictionary<Vector3Int, List<int>> grid = new Dictionary<Vector3Int, List<int>>();

            // First pass: collect and merge vertices
            for (int i = 0; i < oldVertices.Length; i++)
            {
                Vector3 vertex = oldVertices[i];
                Vector3 vertexNormal = (oldNormals != null && i < oldNormals.Length) ? oldNormals[i] : Vector3.up;
                
                Vector3Int cell = new Vector3Int(
                    Mathf.FloorToInt(vertex.x / tolerance),
                    Mathf.FloorToInt(vertex.y / tolerance),
                    Mathf.FloorToInt(vertex.z / tolerance)
                );

                bool foundMatch = false;
                for (int xOffset = -1; xOffset <= 1; xOffset++)
                {
                    for (int yOffset = -1; yOffset <= 1; yOffset++)
                    {
                        for (int zOffset = -1; zOffset <= 1; zOffset++)
                        {
                            Vector3Int neighborCell = new Vector3Int(
                                cell.x + xOffset,
                                cell.y + yOffset,
                                cell.z + zOffset
                            );

                            if (grid.TryGetValue(neighborCell, out List<int> cellIndices))
                            {
                                foreach (int index in cellIndices)
                                {
                                    if (Vector3.Distance(vertex, oldVertices[index]) < tolerance)
                                    {
                                        int targetIndex = remap[index];
                                        remap[i] = targetIndex;
                                        
                                        // Accumulate normals to average them later
                                        if (!normalSums.ContainsKey(targetIndex))
                                        {
                                            normalSums[targetIndex] = Vector3.zero;
                                            normalCounts[targetIndex] = 0;
                                        }
                                        normalSums[targetIndex] += vertexNormal;
                                        normalCounts[targetIndex]++;
                                        
                                        foundMatch = true;
                                        break;
                                    }
                                }
                            }
                            
                            if (foundMatch) break;
                        }
                        if (foundMatch) break;
                    }
                    if (foundMatch) break;
                }

                if (!foundMatch)
                {
                    int newIndex = newVertices.Count;
                    remap[i] = newIndex;
                    newVertices.Add(vertex);
                    
                    // Initialize normal tracking for this new vertex
                    normalSums[newIndex] = vertexNormal;
                    normalCounts[newIndex] = 1;
                    
                    if (!grid.ContainsKey(cell))
                        grid[cell] = new List<int>();
                    grid[cell].Add(i);
                }
            }

            // Calculate averaged normals
            for (int i = 0; i < newVertices.Count; i++)
            {
                Vector3 avgNormal = normalSums[i] / normalCounts[i];
                newNormals.Add(avgNormal.normalized); // Normalize the averaged normal
            }

            // Remap triangles
            var newTriangles = new int[oldTriangles.Length];
            for (int i = 0; i < oldTriangles.Length; i++)
            {
                newTriangles[i] = remap[oldTriangles[i]];
            }

            // Update the reference parameters
            vertices = newVertices.ToArray();
            indices = newTriangles;
            normals = newNormals.ToArray();            
        }
    }
}