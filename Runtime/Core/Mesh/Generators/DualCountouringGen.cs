using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UnityEngine.GsplEdit
{
    [ExecuteInEditMode]
    public class DualContouringGen : MeshGenBase
    {
        [System.Serializable]
        public class Settings
        {
            public float scale = 4.0f;
            public float threshold = 0.5f;
            public int maxDepth = 5;
            public int samplesPerNode = 8;
            public float gradientVarianceThreshold = 0.1f;
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
                return sizeof(float) * (3 + 12 * 3 + 1 + 3 + 3) + sizeof(int) * 60;
            }

            public Bounds GetBounds()
            {
                return new Bounds(center, boundMax - boundMin);
            }
        }

        public Settings m_Settings = new Settings();
        public ComputeShader m_IcosahedronComputeShader;

        private OctreeNode root;
        private System.Random random = new System.Random();

        public class OctreeNode
        {
            public Bounds Bounds;
            public List<SplatData> Icosahedrons;
            public OctreeNode[] Children;
            public int Depth;
            private bool FoundIn;
            private bool FoundOut;

            public OctreeNode(Bounds bounds, int depth)
            {
                Bounds = bounds;
                Icosahedrons = new List<SplatData>();
                Children = null;
                Depth = depth;
            }

            public bool IsLeaf => Children == null;
            public bool ContainsSurface() => FoundIn && FoundOut;

            private Vector3 CalculateGradient(Vector3 point, float threshold, float epsilon = 0.001f)
            {
                float x1 = EvaluateSDF(point + new Vector3(epsilon, 0, 0), threshold);
                float x2 = EvaluateSDF(point - new Vector3(epsilon, 0, 0), threshold);
                float y1 = EvaluateSDF(point + new Vector3(0, epsilon, 0), threshold);
                float y2 = EvaluateSDF(point - new Vector3(0, epsilon, 0), threshold);
                float z1 = EvaluateSDF(point + new Vector3(0, 0, epsilon), threshold);
                float z2 = EvaluateSDF(point - new Vector3(0, 0, epsilon), threshold);

                return new Vector3(
                    (x1 - x2) / (2 * epsilon),
                    (y1 - y2) / (2 * epsilon),
                    (z1 - z2) / (2 * epsilon)
                );
            }

            private float EvaluateSDF(Vector3 point, float threshold)
            {
                float sum = 0;
                float minDistance = float.MaxValue;

                foreach (var splat in Icosahedrons)
                {
                    Vector3 localPoint = Quaternion.Inverse(splat.rot) * (point - splat.center);
                    Vector3 scaledPoint = new Vector3(
                        localPoint.x / splat.scale.x,
                        localPoint.y / splat.scale.y,
                        localPoint.z / splat.scale.z
                    );

                    float squaredDist = scaledPoint.sqrMagnitude;
                    float gaussian = splat.opacity * Mathf.Exp(-squaredDist);
                    sum += gaussian;

                    float actualDistance = Vector3.Distance(point, splat.center);
                    minDistance = Mathf.Min(minDistance, actualDistance);
                }

                return sum > threshold ? sum - threshold : -minDistance;
            }

            private Vector3[] GetJitteredPointsInBounds(Bounds bounds, int sampleCount, System.Random random)
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

            public bool ShouldSplitNode(Settings settings, System.Random random)
            {
                if (Depth >= settings.maxDepth || Icosahedrons.Count == 0)
                    return false;

                Vector3[] samplePoints = GetJitteredPointsInBounds(Bounds, settings.samplesPerNode, random);
                List<Vector3> gradients = new List<Vector3>();

                foreach (Vector3 samplePoint in samplePoints)
                {
                    float value = EvaluateSDF(samplePoint, settings.threshold);
                    if (value <= 0) FoundOut = true;
                    if (value > 0) FoundIn = true;

                    Vector3 gradient = CalculateGradient(samplePoint, settings.threshold);
                    if (gradient.magnitude > 1e-6f)
                        gradients.Add(gradient.normalized);
                }

                if (gradients.Count < settings.samplesPerNode / 2)
                    return false;

                Vector3 meanGradient = Vector3.zero;
                foreach (var gradient in gradients)
                    meanGradient += gradient;
                meanGradient = meanGradient.normalized;

                float variance = 0;
                foreach (var gradient in gradients)
                    variance += 1 - Vector3.Dot(gradient, meanGradient);
                variance /= gradients.Count;

                return variance > settings.gradientVarianceThreshold;
            }

            public void SubdivideNode()
            {
                if (!IsLeaf) return;

                Vector3 size = Bounds.size * 0.5f;
                Vector3 center = Bounds.center;

                Children = new OctreeNode[8];
                for (int i = 0; i < 8; i++)
                {
                    Vector3 offset = new Vector3(
                        ((i & 1) == 0) ? -size.x / 2 : size.x / 2,
                        ((i & 2) == 0) ? -size.y / 2 : size.y / 2,
                        ((i & 4) == 0) ? -size.z / 2 : size.z / 2
                    );
                    Children[i] = new OctreeNode(new Bounds(center + offset, size), Depth + 1);

                    foreach (var icosahedron in Icosahedrons)
                    {
                        if (Children[i].Bounds.Intersects(icosahedron.GetBounds()))
                            Children[i].Icosahedrons.Add(icosahedron);
                    }
                }

                Icosahedrons.Clear();
            }
        }

        private void BuildOctree(OctreeNode node, Settings settings, System.Random random)
        {
            if (node.ShouldSplitNode(settings, random))
            {
                node.SubdivideNode();
                foreach (var child in node.Children)
                    BuildOctree(child, settings, random);
            }
        }

        public unsafe override void Generate(SharedComputeContext context, ref Vertex[] vertexList, ref int[] indexList)
        {
            Vector3 size = context.splatData.boundsMax - context.splatData.boundsMin;
            Vector3 center = (context.splatData.boundsMax + context.splatData.boundsMin) * 0.5f;
            root = new OctreeNode(new Bounds(center, size), 0);

            int splatCount = context.splatData.splatCount;
            int itemsPerDispatch = 65535;

            using (ComputeBuffer IcosahedronBuffer = new ComputeBuffer(splatCount, sizeof(SplatData)))
            {
                SplatData[] splatArray = new SplatData[splatCount];
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

                foreach (var splat in splatArray)
                    root.Icosahedrons.Add(splat);
            }

            BuildOctree(root, m_Settings, random);
            GenerateMeshFromOctree(root, ref vertexList, ref indexList);
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

        private void GenerateMeshFromOctree(OctreeNode node, ref Vertex[] vertexList, ref int[] indexList)
        {
            List<Vertex> vertices = new List<Vertex>();
            List<int> indices = new List<int>();

            GenerateMeshRecursive(node, vertices, indices);

            vertexList = vertices.ToArray();
            indexList = indices.ToArray();
        }

        private void GenerateMeshRecursive(OctreeNode node, List<Vertex> vertices, List<int> indices)
        {
            if (node.IsLeaf)
                AddCubeMesh(node.Bounds, vertices, indices);
            else
            {
                foreach (var child in node.Children)
                    GenerateMeshRecursive(child, vertices, indices);
            }
        }

        private void AddCubeMesh(Bounds bounds, List<Vertex> vertices, List<int> indices)
        {
            Vector3[] cubeVertices = new Vector3[8]
            {
                bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, -bounds.extents.z),
                bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, -bounds.extents.z),
                bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, -bounds.extents.z),
                bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, -bounds.extents.z),
                bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, bounds.extents.z),
                bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, bounds.extents.z),
                bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, bounds.extents.z),
                bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, bounds.extents.z)
            };

            int startIndex = vertices.Count;
            for (int i = 0; i < 8; i++)
                vertices.Add(new Vertex { position = cubeVertices[i] });

            int[] cubeIndices = new int[]
            {
                0, 1, 2, 2, 3, 0,
                1, 5, 6, 6, 2, 1,
                5, 4, 7, 7, 6, 5,
                4, 0, 3, 3, 7, 4,
                3, 2, 6, 6, 7, 3,
                4, 5, 1, 1, 0, 4
            };

            for (int i = 0; i < cubeIndices.Length; i++)
                indices.Add(startIndex + cubeIndices[i]);
        }
    }
}