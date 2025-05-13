using System;
using System.Collections.Generic;
using System.Linq;

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
        }

        public Settings m_Settings = new Settings();
        public ComputeShader m_IcosahedronComputeShader;

        private OctreeNode m_Root;
        private Dictionary<EdgeKey, List<OctreeNode>> edgeMap;

        private static readonly int[,] edges = new int[,] {
            {0,1}, {1,2}, {2,3}, {3,0},  // bottom edges
            {4,5}, {5,6}, {6,7}, {7,4},  // top edges
            {0,4}, {1,5}, {2,6}, {3,7}   // vertical edges
        };

        public unsafe override void Generate(SharedComputeContext context, ref Vector3[] vertexList, ref int[] indexList)
        {
            edgeMap = new Dictionary<EdgeKey, List<OctreeNode>>();
            Vector3 size = (context.gsSplatData.boundsMax - context.gsSplatData.boundsMin) * 1.2f;
            Vector3 center = (context.gsSplatData.boundsMax + context.gsSplatData.boundsMin) * 0.5f;
            m_Root = new OctreeNode(null, new Bounds(center, size));

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
            
            
            OctreeBuilder.BuildOctree(splatList, m_Root, m_Settings.maxDepth);
            OctreeHullBuilder.ExtractHull(m_Root);
            IterGen(m_Root, m_Settings.threshold, splatList);

            // Generate the final mesh
            List<Vector3> vertices = new List<Vector3>();
            List<int> indices = new List<int>();
            
            OctreeMeshGen.Gen(vertices, indices, edgeMap);

            vertexList = vertices.ToArray();
            indexList = indices.ToArray();
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

        private void IterGen(OctreeNode currentNode, float threshold, List<MeshUtils.SplatData> splats) {
            if (currentNode == null)
                currentNode = m_Root;

            if (currentNode.m_ContainsSurface && currentNode.m_IsLeaf) {
                GenerateVertexPosition(currentNode, threshold, splats);
            }

            if (!currentNode.m_IsLeaf) {
                foreach(var child in currentNode.m_Children) {
                    if (child != null) {
                        IterGen(child, threshold, splats);
                    }
                }
            }
        }

        public void GenerateVertexPosition(OctreeNode node, float threshold, List<MeshUtils.SplatData> splats) 
        {
            // Compute the cube vertices from node.m_Bounds
            Vector3 min = node.m_Bounds.min;
            Vector3 max = node.m_Bounds.max;

            Vector3 closestCorner = new Vector3(0,0,0);
            float closestCornerValue = float.MaxValue;

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

                float f1 = node.EvaluateSDF(v1, threshold, splats);
                float f2 = node.EvaluateSDF(v2, threshold, splats);

                // Add node to edges
                EdgeKey line;
                if (f1 < f2) 
                    line = new EdgeKey(v1, v2);
                else
                    line = new EdgeKey(v2, v1);

                if (!edgeMap.ContainsKey(line))
                {
                    // If edge doesn't exist, add it
                    edgeMap[line] = new List<OctreeNode>();
                }
                edgeMap[line].Add(node);

                // Vertex position for edge
                if (f1 * f2 < 0) {
                    containsEdge[i] = true;
                    intersection[i] = BinarySearchIntersection(node, v1, v2, threshold, splats);
                    
                }
                else
                {
                    containsEdge[i] = false;
                    if (Math.Abs(f1) < closestCornerValue) {
                        closestCorner = v1;
                        closestCornerValue = Math.Abs(f1);
                    }

                    if (Math.Abs(f2) < closestCornerValue) {
                        closestCorner = v2;
                        closestCornerValue = Math.Abs(f2);
                    }
                }
            }

            int count = 0;
            node.m_VertexPosition = new Vector3(0,0,0);
            for (int i = 0; i < 12; i++) {
                if (containsEdge[i]) {
                    node.m_VertexPosition += intersection[i];
                    count++;
                }
            }
            
            if (count > 0) {
                node.m_VertexPosition /= count;
            } else {
                node.m_VertexPosition = node.m_Bounds.center;
            }
        }

        private Vector3 BinarySearchIntersection(OctreeNode node, Vector3 v1, Vector3 v2, float threshold, List<MeshUtils.SplatData> splats)
        {
            float f1 = node.EvaluateSDF(v1, threshold, splats);
            float f2 = node.EvaluateSDF(v2, threshold, splats);

            Vector3 a = v1;
            Vector3 b = v2;

            for (int i = 0; i < 20; i++)
            {
                Vector3 mid = (a + b) * 0.5f;
                float fmid = node.EvaluateSDF(mid, threshold, splats);

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