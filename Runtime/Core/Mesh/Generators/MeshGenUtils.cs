using System.Collections.Generic;
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

        public static void AutoUVUnwrap(ref Mesh mesh) {
            if (mesh == null) {
                Debug.LogError("Cannot unwrap UVs: Mesh is null");
                return;
            }
            
            Vector2[] uvs = new Vector2[mesh.vertexCount];
            
            // Create unwrapping parameters with default settings
            UnwrapParam unwrapParams = new UnwrapParam {
                angleError = 0.08f,       // Lower values = better quality but more charts
                areaError = 0.15f,        // Lower values = better quality but more charts
                hardAngle = 60f,          // Angle at which to cut mesh (degrees)
                packMargin = 0.005f,      // Margin between islands (in UV space 0-1)
            };
            
            // Generate UV coordinates
            Unwrapping.GenerateSecondaryUVSet(mesh, unwrapParams);
            
            Debug.Log("Successfully unwrapped UVs for mesh: " + mesh.name);
        }
    }
}