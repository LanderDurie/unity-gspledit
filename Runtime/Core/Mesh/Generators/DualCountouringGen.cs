using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;

namespace UnityEngine.GsplEdit
{
    [ExecuteInEditMode]
    public class DualContouringGen : MeshGenBase
    {
        [System.Serializable]
        public class Settings
        {
            public float scale = 2.0f;
            public float threshold = 0.01f;
            public int maxDepth = 5;
            public int lod = 4;
            // public uint samplesPerNode = 8;
            // public float gradientVarianceThreshold = 0.2f;
        }

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


        public Settings m_Settings = new Settings();
        public ComputeShader m_IcosahedronComputeShader;

        private OctreeNode m_Root;
        private System.Random random = new System.Random();

        public class OctreeNode
        {
            public Bounds m_Bounds;
            public List<int> m_SplatIds;
            public OctreeNode[] m_Children;
            public uint m_Depth;
            public bool m_IsLeaf;
            public Vector3? m_VertexPosition;
            public int m_VertexIndex = -1;
            public bool m_ContainsSurface;

            public OctreeNode(Bounds bounds, uint depth)
            {
                m_Bounds = bounds;
                m_SplatIds = new List<int>();
                m_Children = null;
                m_Depth = depth;
                m_ContainsSurface = true;
                m_IsLeaf = true;
                m_VertexPosition = bounds.center;
            }

            private Vector3 CalculateGradient(Vector3 point, float threshold, List<SplatData> splats, float epsilon = 0.001f)
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

            public float EvaluateSDF(Vector3 point, float threshold, List<SplatData> splats)
            {
                float accumulatedOpacity = 0f;

                foreach (var splatId in m_SplatIds)
                {
                    SplatData splat = splats[splatId];

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
                }
            
                return accumulatedOpacity - threshold;
            }

            public float EvaluateGradientSDF(Vector3 point, float threshold, List<SplatData> splats)
            {
                float accumulatedOpacity = 0f;
                float minDistance = float.MaxValue;
                int i = 0;

                foreach (var splatId in m_SplatIds)
                {
                    SplatData splat = splats[splatId];

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
                    i++;
                }
            

                return accumulatedOpacity > threshold ? accumulatedOpacity - threshold : -1;
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

            public int IntersectCount(Vector3 point, List<SplatData> splats)
            {
                int i = 0;

                foreach (var splatId in m_SplatIds)
                {
                    SplatData splat = splats[splatId];

                    if (splat.IsPointInsideIcosahedron(point)) {
                        i++;
                    }
                }

                return i;
            }

            public float TotalOpacity(List<SplatData> splats)
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

            public bool ContainsSurface(Settings settings, List<SplatData> splats) {
                if (m_SplatIds.Count == 0 || TotalOpacity(splats) < settings.threshold) {
                    return false;
                }

                Vector3[] corners = GetCorners();
                bool containsAll = true;
                foreach(var corner in corners) {
                    if (IntersectCount(corner, splats) < m_SplatIds.Count * 0.8) {
                        containsAll = false;
                        break;
                    }
                }
                if (containsAll)
                    return false;

                return true;
            }

            public bool ShouldSplitNode(Settings settings, System.Random random, List<SplatData> splats)
            {
                
                // Base stop conditions
                if (m_Depth >= settings.maxDepth || !m_ContainsSurface || m_SplatIds.Count < 500) {
                    return false;
                }

                return true;
            }


            public void SubdivideNode(Settings settings, List<SplatData> splats)
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

                    m_Children[i].m_ContainsSurface = m_Children[i].ContainsSurface(settings, splats);
                }
                
                m_IsLeaf = false;
                m_SplatIds.Clear();
            }
        }

        private void GenerateMesh(Settings settings, List<SplatData> splats, OctreeNode node, List<Vertex> vertices, List<int> indices)
        {
            if (node == null) return;

            if (node.m_IsLeaf)
            {
                if (node.m_ContainsSurface)
                {
                //     node.m_VertexIndex = vertices.Count;
                //     vertices.Add(new Vertex { position = node.m_VertexPosition.Value });
                    GenerateCube(settings, node, vertices, indices, splats);
                }
            }
             else {
                foreach(var child in node.m_Children) {
                    GenerateMesh(settings, splats, child, vertices, indices);
                }
            }
            // else if (node.m_Children != null)
            // {
                // foreach (var child in node.m_Children)
                // {
                //     if (child != null)
                //     {
                //         GenerateMesh(child, vertices, indices);
                //     }
                // }

                // GenerateFacesBetweenChildren(node, indices);
            // }
        }

        private void GenerateCube(Settings settings, OctreeNode node, List<Vertex> vertices, List<int> indices, List<SplatData> splats) {

            int x = settings.lod;
            int y = settings.lod;
            int z = settings.lod;

            // Isosurface reconstruction
            MarchingCubes.Marching marching = new MarchingCubes.MarchingCubes();
            marching.Surface = 0;

            MarchingCubes.VoxelArray voxels = new MarchingCubes.VoxelArray(x, y, z);

            // Step 2: Map the data into the 3D `Voxels` array
            Vector3 size = new Vector3(node.m_Bounds.size.x / x, node.m_Bounds.size.y / y, node.m_Bounds.size.z / z);
            for (int i = 0; i < x; i++)
            {
                for (int j = 0; j < y; j++)
                {
                    for (int k = 0; k < z; k++)
                    {
                        Vector3 center = node.m_Bounds.min + new Vector3(i * size.x, j * size.y, k * size.z) + size / 2;
                        Vector3 extents = size / 2; // Half the size

                        // Define the 8 cube vertices
                        Vector3[] cubeVertices = new Vector3[]
                        {
                            center + new Vector3(-extents.x, -extents.y, -extents.z), // 0
                            center + new Vector3(extents.x, -extents.y, -extents.z),  // 1
                            center + new Vector3(extents.x, -extents.y, extents.z),   // 2
                            center + new Vector3(-extents.x, -extents.y, extents.z),  // 3
                            center + new Vector3(-extents.x, extents.y, -extents.z),  // 4
                            center + new Vector3(extents.x, extents.y, -extents.z),   // 5
                            center + new Vector3(extents.x, extents.y, extents.z),    // 6
                            center + new Vector3(-extents.x, extents.y, extents.z)    // 7
                        };

                        bool foundIn = false;
                        bool foundOut = false;
                        foreach(var corner in cubeVertices) {
                            float value = node.EvaluateSDF(corner, settings.threshold, splats);
                            if (value <= 0) {
                                foundOut = true;
                            } else {
                                foundIn = true;
                            }
                        }

                        if (foundOut && foundIn) {
                            // // Add vertices to the list
                            // int baseIndex = vertices.Count;
                            // for (int a = 0; a < cubeVertices.Length; a++)
                            // {
                            //     vertices.Add(new Vertex { position = cubeVertices[a] });
                            // }

                            // // Define the 12 triangles (two per face)
                            // int[] cubeIndices = new int[]
                            // {
                            //     0, 1, 2,  2, 3, 0, // Bottom face
                            //     4, 5, 6,  6, 7, 4, // Top face
                            //     0, 4, 7,  7, 3, 0, // Left face
                            //     1, 5, 6,  6, 2, 1, // Right face
                            //     3, 2, 6,  6, 7, 3, // Front face
                            //     0, 1, 5,  5, 4, 0  // Back face
                            // };

                            // // Add indices to the list
                            // for (int a = 0; a < cubeIndices.Length; a++)
                            // {
                            //     indices.Add(baseIndex + cubeIndices[a]);
                            // }
                            GeneratePoint(center, vertices, indices);
                        }
                    }
                }
            }
        }

        private void GeneratePoint(Vector3 center, List<Vertex> vertices, List<int> indices) {

            // Define the 8 cube vertices
            Vector3[] verts = new Vector3[]
            {
                new Vector3(center.x + 0.1f, center.y, center.z), // 0
                new Vector3(center.x, center.y + 0.1f, center.z),  // 1
                new Vector3(center.x, center.y, center.z + 0.1f),   // 2
            };

            // Add vertices to the list
            int baseIndex = vertices.Count;
            for (int i = 0; i < verts.Length; i++)
            {
                vertices.Add(new Vertex { position = verts[i] });
            }

            int[] cubeIndices = new int[]
            {
                0, 1, 2
            };

            // Add indices to the list
            for (int i = 0; i < cubeIndices.Length; i++)
            {
                indices.Add(baseIndex + cubeIndices[i]);
            }
        }



        private void GenerateFacesBetweenChildren(OctreeNode node, List<int> indices)
        {
            if (node.m_Children == null || node.m_Children.Length != 8)
                return;

            // Table for face checking: each entry contains 4 indices representing corners of a face
            int[,] faceTable = new int[,]
            {
                // Faces along X axis (left to right)
                {0, 2, 4, 6}, // left face
                {1, 3, 5, 7}, // right face
                // Faces along Y axis (bottom to top)
                {0, 1, 4, 5}, // bottom face
                {2, 3, 6, 7}, // top face
                // Faces along Z axis (back to front)
                {0, 1, 2, 3}, // back face
                {4, 5, 6, 7}  // front face
            };

            // Check each face
            for (int face = 0; face < 6; face++)
            {
                OctreeNode[] faceNodes = new OctreeNode[4];
                bool validFace = true;

                // Get the 4 nodes that make up this face
                for (int i = 0; i < 4; i++)
                {
                    int nodeIndex = faceTable[face, i];
                    if (nodeIndex >= 0 && nodeIndex < 8 && node.m_Children[nodeIndex] != null)
                    {
                        faceNodes[i] = node.m_Children[nodeIndex];
                    }
                    else
                    {
                        validFace = false;
                        break;
                    }
                }

                if (!validFace) continue;

                // Check if all nodes have valid vertices
                bool allNodesHaveVertices = true;
                for (int i = 0; i < 4; i++)
                {
                    if (faceNodes[i].m_VertexIndex == -1)
                    {
                        allNodesHaveVertices = false;
                        break;
                    }
                }

                if (!allNodesHaveVertices) continue;

                // Generate two triangles for the face
                // First triangle
                indices.Add(faceNodes[0].m_VertexIndex);
                indices.Add(faceNodes[1].m_VertexIndex);
                indices.Add(faceNodes[2].m_VertexIndex);

                // Second triangle
                indices.Add(faceNodes[1].m_VertexIndex);
                indices.Add(faceNodes[3].m_VertexIndex);
                indices.Add(faceNodes[2].m_VertexIndex);
            }
        }

        private void BuildOctree(OctreeNode node, Settings settings, System.Random random, List<SplatData> splats)
        {
            if (node.ShouldSplitNode(settings, random, splats))
            {
                node.SubdivideNode(settings, splats);
                foreach (var child in node.m_Children)
                    BuildOctree(child, settings, random, splats);
            }
        }


        public unsafe override void Generate(SharedComputeContext context, ref Vertex[] vertexList, ref int[] indexList)
        {
            Vector3 size = context.splatData.boundsMax - context.splatData.boundsMin;
            Vector3 center = (context.splatData.boundsMax + context.splatData.boundsMin) * 0.5f;
            m_Root = new OctreeNode(new Bounds(center, size), 0);

            int splatCount = context.splatData.splatCount;
            int itemsPerDispatch = 65535;

            SplatData[] splatArray = new SplatData[splatCount];

            using (ComputeBuffer IcosahedronBuffer = new ComputeBuffer(splatCount, sizeof(SplatData)))
            {
                IcosahedronBuffer.SetData(splatArray);
                SetupComputeShader(context, IcosahedronBuffer);

                for (int i = 0; i < Mathf.CeilToInt((float)splatCount / itemsPerDispatch); i++)
                {
                    int offset = i * itemsPerDispatch;
                    m_IcosahedronComputeShader.SetInt("_Offset", offset);
                    int currentDispatchSize = Mathf.Min(splatCount - offset, itemsPerDispatch);
                    m_IcosahedronComputeShader.Dispatch(0, currentDispatchSize, 1, 1);
                }

                IcosahedronBuffer.GetData(splatArray);
            }

            // Directly populate the list without looping
            m_Root.m_SplatIds = Enumerable.Range(0, splatCount).ToList();

            // Pass the correct type (List<SplatData>) to BuildOctree
            List<SplatData> splatList = new List<SplatData>(splatArray);
            BuildOctree(m_Root, m_Settings, random, splatList);


            // Add QEF solver for all leaf nodes that ContainSurface
            ProcessLeafNodes(m_Root, m_Settings, splatList);

            // Generate the final mesh
            List<Vertex> vertices = new List<Vertex>();
            List<int> indices = new List<int>();
            
            GenerateMesh(m_Settings, splatList, m_Root, vertices, indices);

            vertexList = vertices.ToArray();
            indexList = indices.ToArray();
        }

        private void ProcessLeafNodes(OctreeNode node, Settings settings, List<SplatData> splats)
        {
            if (node == null) return;

            if (node.m_IsLeaf)
            {
                if (node.m_ContainsSurface)
                {
                    // node.GenerateVertexPosition(settings, splats);
                }
            }
            else
            {
                foreach (var child in node.m_Children)
                {
                    ProcessLeafNodes(child, settings, splats);
                }
            }
        }


        private void SetupComputeShader(SharedComputeContext context, ComputeBuffer IcosahedronBuffer)
        {
            m_IcosahedronComputeShader.SetFloat("_GlobalScaleFactor", m_Settings.scale);
            m_IcosahedronComputeShader.SetBuffer(0, "_SplatPos", context.gpuGSPosData);
            m_IcosahedronComputeShader.SetBuffer(0, "_SplatOther", context.gpuGSOtherData);
            m_IcosahedronComputeShader.SetBuffer(0, "_SplatSH", context.gpuGSSHData);
            m_IcosahedronComputeShader.SetBuffer(0, "_SplatChunks", context.gpuGSChunks);
            m_IcosahedronComputeShader.SetInt("_SplatChunkCount", context.gpuGSChunksValid ? context.gpuGSChunks.count : 0);
            uint format = (uint)context.splatData.posFormat | ((uint)context.splatData.scaleFormat << 8) | ((uint)context.splatData.shFormat << 16);
            m_IcosahedronComputeShader.SetInt("_SplatFormat", (int)format);
            m_IcosahedronComputeShader.SetTexture(0, "_SplatColor", context.gpuGSColorData);
            m_IcosahedronComputeShader.SetBuffer(0, "_IcosahedronBuffer", IcosahedronBuffer);
        }
    }
}