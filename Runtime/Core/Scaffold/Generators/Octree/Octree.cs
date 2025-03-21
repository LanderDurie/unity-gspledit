using System.Collections.Generic;

namespace UnityEngine.GsplEdit
{
    public class OctreeNode
    {
        public class Settings
        {
            public float threshold = 0.01f;
            public int maxDepth = 5;
        }

        public Bounds m_Bounds;
        public List<int> m_SplatIds;
        public OctreeNode[] m_Children;
        public uint m_Depth;
        public bool m_IsLeaf;
        public Vector3? m_VertexPosition;
        public int m_VertexIndex = -1;
        public bool m_ContainsPotentialSurface;

        public OctreeNode(Bounds bounds, uint depth)
        {
            m_Bounds = bounds;
            m_SplatIds = new List<int>();
            m_Children = null;
            m_Depth = depth;
            m_ContainsPotentialSurface = true;
            m_IsLeaf = true;
            m_VertexPosition = bounds.center;
        }

        private Vector3 CalculateGradient(Vector3 point, float threshold, List<MeshUtils.SplatData> splats, float epsilon = 0.001f)
        {
            Vector3 gradient = Vector3.zero;

            System.Threading.Tasks.Parallel.For(0, 3, i =>
            {
                Vector3 delta = Vector3.zero;
                delta[i] = epsilon;

                float x1 = EvaluateSDF(point + delta, threshold, splats);
                float x2 = EvaluateSDF(point - delta, threshold, splats);

                gradient[i] = (x1 - x2) / (2 * epsilon);
            });

            return gradient;
        }

        public float EvaluateSDF(Vector3 point, float threshold, List<MeshUtils.SplatData> splats)
        {
            float accumulatedOpacity = 0f;
            float minDistance = float.MaxValue;

            foreach (var splatId in m_SplatIds)
            {
                MeshUtils.SplatData splat = splats[splatId];

                // Calculate the combined inverse rotation and scale matrix
                Matrix4x4 invSplatRot_ScaleMat = Matrix4x4.TRS(Vector3.zero, Quaternion.Inverse(splat.rot), Vector3.one)
                                                * Matrix4x4.Scale(new Vector3(1.0f / splat.scale.x, 1.0f / splat.scale.y, 1.0f / splat.scale.z));

                // Apply the transformation to the offset
                Vector3 offset = point - splat.center;
                Vector3 transformedPos = invSplatRot_ScaleMat.MultiplyPoint3x4(offset);

                // Calculate squared distance
                float distanceSquared = transformedPos.sqrMagnitude;

                // Adjust opacity calculation to include the / 2.0f factor
                float opacity = splat.opacity * Mathf.Exp(-distanceSquared / 2.0f);
                accumulatedOpacity += opacity;

                float actualDistance = offset.sqrMagnitude;
                minDistance = Mathf.Min(minDistance, actualDistance);
            }
        
            return accumulatedOpacity > 0.01 ? accumulatedOpacity - threshold : -minDistance;
        }

        private Vector3[] GetCorners()
        {
            Vector3 min = m_Bounds.min;
            Vector3 max = m_Bounds.max;
            
            return new Vector3[]
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, max.y, max.z)
            };
        }

        public int IntersectCount(Vector3 point, List<MeshUtils.SplatData> splats)
        {
            int i = 0;

            foreach (var splatId in m_SplatIds)
            {
                MeshUtils.SplatData splat = splats[splatId];

                if (splat.IsPointInsideIcosahedron(point)) {
                    i++;
                }
            }

            return i;
        }

        public float TotalOpacity(List<MeshUtils.SplatData> splats)
        {
            float total = 0;

            foreach (var splatId in m_SplatIds)
            {
                total += splats[splatId].opacity;
            }

            return total;
        }


        private Vector3[] GetJitteredPointsInBounds(Bounds bounds, uint sampleCount, System.Random random)
        {
            Vector3[] points = new Vector3[sampleCount];
            int gridSize = Mathf.CeilToInt(Mathf.Pow(sampleCount, 1.0f / 3.0f));
            Vector3 cellSize = bounds.size / gridSize;

            int index = 0;
            for (int x = 0; x < gridSize && index < sampleCount; x++)
            {
                for (int y = 0; y < gridSize && index < sampleCount; y++)
                {
                    for (int z = 0; z < gridSize && index < sampleCount; z++)
                    {
                        Vector3 basePos = bounds.min + new Vector3(
                            (x + 0.5f) * cellSize.x,
                            (y + 0.5f) * cellSize.y,
                            (z + 0.5f) * cellSize.z
                        );

                        Vector3 jitter = new Vector3(
                            ((float)random.NextDouble() - 0.5f) * cellSize.x,
                            ((float)random.NextDouble() - 0.5f) * cellSize.y,
                            ((float)random.NextDouble() - 0.5f) * cellSize.z
                        );

                        points[index++] = basePos + jitter * 0.5f;
                    }
                }
            }

            return points;
        }

        public bool ContainsPotentialSurface(Settings settings, List<MeshUtils.SplatData> splats) {
            if (m_SplatIds.Count == 0 || TotalOpacity(splats) < settings.threshold) {
                return false;
            }

            Vector3[] corners = GetCorners();
            // If all conrners contain 80% of all splats in the node, stop subdividing
            bool containsAll = true;
            foreach(var corner in corners) {
                if (IntersectCount(corner, splats) < m_SplatIds.Count * 0.8) {
                    containsAll = false;
                    break;
                }
            }
            if (containsAll)
                return false;

            // float pointer = 0;
            // float totalMagnitude = 0;

            // foreach (var corner in corners) {
            //     Vector3 gradient = CalculateGradient(corner, settings.threshold, splats);
                
            //     // Compute dot product with position vector
            //     float dotProduct = Vector3.Dot(gradient, corner);
                
            //     // Accumulate the weighted sum
            //     pointer += dotProduct;
            //     totalMagnitude += gradient.magnitude;
            // }

            // // Normalize to range [-1, 1]
            // pointer = (totalMagnitude > 0) ? (pointer / totalMagnitude) : 0;

            // if (m_Depth >= settings.maxDepth || m_SplatIds.Count > 0) {
            //     bool noIntersections = true;
            //     bool allIntersections = true;
            //     foreach(var corner in corners) {
            //         if (EvaluateSDF(corner, settings.threshold, splats) < 0) {
            //             allIntersections = false;
            //         } else {
            //             noIntersections = false;
            //         }
            //     }
            //     if (allIntersections || noIntersections) {
            //         Debug.Log("Ignoooore");
            //         return false;
            //     }
            // }

            return true;
        }

        public bool ShouldSplitNode(Settings settings, List<MeshUtils.SplatData> splats)
        {
            // Base stop conditions
            if (m_Depth >= settings.maxDepth || !m_ContainsPotentialSurface || m_SplatIds.Count < 100) {
                return false;
            }

            return true;
        }


        public void SubdivideNode(Settings settings, List<MeshUtils.SplatData> splats)
        {
            if (!m_IsLeaf) return;

            Vector3 size = m_Bounds.size * 0.5f;
            Vector3 center = m_Bounds.center;

            m_Children = new OctreeNode[8];
            for (int i = 0; i < 8; i++)
            {
                Vector3 offset = new Vector3(
                    ((i & 1) == 0) ? -size.x / 2 : size.x / 2,
                    ((i & 2) == 0) ? -size.y / 2 : size.y / 2,
                    ((i & 4) == 0) ? -size.z / 2 : size.z / 2
                );
                m_Children[i] = new OctreeNode(new Bounds(center + offset, size), m_Depth + 1);

                foreach (var id in m_SplatIds)
                {
                    if (m_Children[i].m_Bounds.Intersects(splats[id].GetBounds()))
                        m_Children[i].m_SplatIds.Add(id);
                }

                m_Children[i].m_ContainsPotentialSurface = m_Children[i].ContainsPotentialSurface(settings, splats);
            }
            
            m_IsLeaf = false;
            m_SplatIds.Clear();
        }

        public static void GenerateMesh(Settings settings, List<MeshUtils.SplatData> splats, OctreeNode node, List<Vector3> vertices, List<int> indices)
        {
            if (node == null) return;

            if (node.m_IsLeaf)
            {
                if (node.m_ContainsPotentialSurface)
                {
                    GenerateCube(settings, node, vertices, indices, splats);
                }
            }
             else {
                foreach(var child in node.m_Children) {
                    GenerateMesh(settings, splats, child, vertices, indices);
                }
            }
        }
        
        private static void GenerateCube(Settings settings, OctreeNode node, List<Vector3> vertices, List<int> indices, List<MeshUtils.SplatData> splats) {

            // Vector3 center = node.m_Bounds.center;
            // Vector3 extents = node.m_Bounds.extents;
            // Vector3[] cubeVertices = new Vector3[]
            // {
            //     center + new Vector3(-extents.x, -extents.y, -extents.z), // 0
            //     center + new Vector3(extents.x, -extents.y, -extents.z),  // 1
            //     center + new Vector3(extents.x, -extents.y, extents.z),   // 2
            //     center + new Vector3(-extents.x, -extents.y, extents.z),  // 3
            //     center + new Vector3(-extents.x, extents.y, -extents.z),  // 4
            //     center + new Vector3(extents.x, extents.y, -extents.z),   // 5
            //     center + new Vector3(extents.x, extents.y, extents.z),    // 6
            //     center + new Vector3(-extents.x, extents.y, extents.z)    // 7
            // };

            // bool foundIn = false;
            // bool foundOut = false;
            // foreach(var corner in cubeVertices) {
            //     float value = node.EvaluateSDF(corner, settings.threshold, splats);
            //     if (value <= 0) {
            //         foundOut = true;
            //     } else {
            //         foundIn = true;
            //     }
            // }

            // if (foundOut && foundIn) {
            //     // Add vertices to the list
            //     int baseIndex = vertices.Count;
            //     for (int a = 0; a < cubeVertices.Length; a++)
            //     {
            //         vertices.Add(cubeVertices[a]);
            //     }

            //     // Define the 12 triangles (two per face)
            //     int[] cubeIndices = new int[]
            //     {
            //         0, 1, 2,  2, 3, 0, // Bottom face
            //         4, 5, 6,  6, 7, 4, // Top face
            //         0, 4, 7,  7, 3, 0, // Left face
            //         1, 5, 6,  6, 2, 1, // Right face
            //         3, 2, 6,  6, 7, 3, // Front face
            //         0, 1, 5,  5, 4, 0  // Back face
            //     };

            //     // Add indices to the list
            //     for (int a = 0; a < cubeIndices.Length; a++)
            //     {
            //         indices.Add(baseIndex + cubeIndices[a]);
            //     }
            // }
            
            if (node.m_VertexPosition != null) {
                vertices.Add((Vector3)node.m_VertexPosition);
                indices.Add(vertices.Count-1);
                indices.Add(vertices.Count-1);
                indices.Add(vertices.Count-1);
            }
        }

        public static void BuildOctree(OctreeNode node, Settings settings, System.Random random, List<MeshUtils.SplatData> splats)
        {
            if (node.ShouldSplitNode(settings, splats))
            {
                node.SubdivideNode(settings, splats);
                foreach (var child in node.m_Children)
                    BuildOctree(child, settings, random, splats);
            }
        }
    }
}