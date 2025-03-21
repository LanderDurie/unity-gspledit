using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Codice.Client.Common.TreeGrouper;

namespace UnityEngine.GsplEdit
{
    public class SurfaceNetGen : MeshGenBase
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

        public Settings m_Settings = new Settings();
        public ComputeShader m_IcosahedronComputeShader;

        private OctreeNode m_Root;
        private System.Random random = new System.Random();

        private static readonly int[,] edges = new int[,] {
            {0,1}, {1,2}, {2,3}, {3,0},  // bottom edges
            {4,5}, {5,6}, {6,7}, {7,4},  // top edges
            {0,4}, {1,5}, {2,6}, {3,7}   // vertical edges
        };

        public unsafe override void Generate(SharedComputeContext context, ref Vector3[] vertexList, ref int[] indexList)
        {
            Vector3 size = context.gsSplatData.boundsMax - context.gsSplatData.boundsMin;
            Vector3 center = (context.gsSplatData.boundsMax + context.gsSplatData.boundsMin) * 0.5f;
            m_Root = new OctreeNode(new Bounds(center, size), 0);

            int splatCount = context.gsSplatData.splatCount;
            int itemsPerDispatch = 65535;

            MeshUtils.SplatData[] splatArray = new MeshUtils.SplatData[splatCount];

            using (ComputeBuffer IcosahedronBuffer = new ComputeBuffer(splatCount, sizeof(MeshUtils.SplatData)))
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

            // Pass the correct type (List<MeshUtils.SplatData>) to BuildOctree
            List<MeshUtils.SplatData> splatList = new List<MeshUtils.SplatData>(splatArray);

            OctreeNode.Settings s = new();
            s.maxDepth = m_Settings.maxDepth;
            s.threshold = m_Settings.threshold;

            OctreeNode.BuildOctree(m_Root, s, random, splatList);


            // Add QEF solver for all leaf nodes that ContainSurface
            ProcessLeafNodes(m_Root, m_Settings, splatList);

            // Generate the final mesh
            List<Vector3> vertices = new List<Vector3>();
            List<int> indices = new List<int>();
            
            OctreeNode.GenerateMesh(s, splatList, m_Root, vertices, indices);

            vertexList = vertices.ToArray();
            indexList = indices.ToArray();
        }

        private void ProcessLeafNodes(OctreeNode node, Settings settings, List<MeshUtils.SplatData> splats)
        {
            if (node == null) return;

            if (node.m_IsLeaf)
            {
                if (node.m_ContainsPotentialSurface)
                {
                    GenerateVertexPosition(node, settings, splats);
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
            m_IcosahedronComputeShader.SetBuffer(0, "_SplatPos", context.gsPosData);
            m_IcosahedronComputeShader.SetBuffer(0, "_SplatOther", context.gsOtherData);
            m_IcosahedronComputeShader.SetBuffer(0, "_SplatSH", context.gsSHData);
            m_IcosahedronComputeShader.SetBuffer(0, "_SplatChunks", context.gsChunks);
            m_IcosahedronComputeShader.SetInt("_SplatChunkCount", context.gsChunksValid ? context.gsChunks.count : 0);
            uint format = (uint)context.gsSplatData.posFormat | ((uint)context.gsSplatData.scaleFormat << 8) | ((uint)context.gsSplatData.shFormat << 16);
            m_IcosahedronComputeShader.SetInt("_SplatFormat", (int)format);
            m_IcosahedronComputeShader.SetTexture(0, "_SplatColor", context.gsColorData);
            m_IcosahedronComputeShader.SetBuffer(0, "_IcosahedronBuffer", IcosahedronBuffer);
        }


        private void GenerateVertexPosition(OctreeNode node, Settings settings, List<MeshUtils.SplatData> splatData) 
        {
            // Compute the cube vertices from node.m_Bounds
            Vector3 min = node.m_Bounds.min;
            Vector3 max = node.m_Bounds.max;

            Vector3[] cubeVertices = new Vector3[8]
            {
                new Vector3(min.x, min.y, min.z), // 0
                new Vector3(max.x, min.y, min.z), // 1
                new Vector3(max.x, min.y, max.z), // 2
                new Vector3(min.x, min.y, max.z), // 3
                new Vector3(min.x, max.y, min.z), // 4
                new Vector3(max.x, max.y, min.z), // 5
                new Vector3(max.x, max.y, max.z), // 6
                new Vector3(min.x, max.y, max.z)  // 7
            };

            // Ensure array is correctly initialized
            bool[] containsEdge = new bool[12];
            Vector3[] intersection = new Vector3[12];

            // Iterate over edges and determine intersections
            for (int i = 0; i < 12; i++)
            {
                Vector3 v1 = cubeVertices[edges[i, 0]];
                Vector3 v2 = cubeVertices[edges[i, 1]];

                float f1 = node.EvaluateSDF(v1, settings.threshold, splatData);
                float f2 = node.EvaluateSDF(v2, settings.threshold, splatData);
                
                Debug.Log($"ScalarField values: {f1}, {f2}");

                if (f1 * f2 < 0) // Check for sign change (zero crossing)
                {
                    intersection[i] = BinarySearchIntersection(node, v1, v2, settings, splatData);
                    // m_EdgeGradients[i] = CalculateGradient(m_EdgeIntersections[i]);
                    containsEdge[i] = true;
                }
                else
                {
                    containsEdge[i] = false;
                }
            }

            node.m_VertexPosition = new Vector3(0,0,0);
            int count = 0;
            for (int i = 0; i < 12; i++) {
                if (containsEdge[i]) {
                    node.m_VertexPosition += intersection[i];
                    count++;
                }
            }
            if (count > 0) {
                node.m_VertexPosition /= count;
            }
        }

        private Vector3 BinarySearchIntersection(OctreeNode node, Vector3 v1, Vector3 v2, Settings settings, List<MeshUtils.SplatData> splatData)
        {
            float f1 = node.EvaluateSDF(v1, settings.threshold, splatData);
            float f2 = node.EvaluateSDF(v2, settings.threshold, splatData);

            Vector3 a = v1;
            Vector3 b = v2;

            for (int i = 0; i < 10; i++)
            {
                Vector3 mid = (a + b) * 0.5f;
                float fmid = node.EvaluateSDF(mid, settings.threshold, splatData);

                if (Mathf.Abs(fmid) < 1e-6f)
                    return mid;

                if (fmid * f1 < 0)
                {
                    b = mid;
                    f2 = fmid;
                }
                else
                {
                    a = mid;
                    f1 = fmid;
                }
            }

            return (a + b) * 0.5f;
        }
    }
}