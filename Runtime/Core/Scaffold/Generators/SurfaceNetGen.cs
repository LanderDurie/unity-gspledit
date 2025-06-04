using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Codice.CM.Common;
using Unity.Mathematics;

namespace UnityEngine.GsplEdit
{
    public class SurfaceNetGen : MeshGenBase
    {
        [System.Serializable]
        public class Settings
        {
            public float scale = 1.0f;
            public float threshold = 0.01f;
            public int maxDepth = 6;
        }

        public Settings m_Settings = new Settings();
        public ComputeShader m_IcosahedronComputeShader;

        private OctreeNode m_Root;
        private Dictionary<EdgeKey, List<OctreeNode>> edgeMap;

        private unsafe float getMaxElement(Vector3 v3){
            return Mathf.Max(Mathf.Max(v3.x, v3.y), v3.z);
        }

        public unsafe override void Generate(SharedComputeContext context, ref Vector3[] vertexList, ref int[] indexList)
        {
            edgeMap = new Dictionary<EdgeKey, List<OctreeNode>>();
            float maxSize = getMaxElement(context.gsSplatData.boundsMax - context.gsSplatData.boundsMin) * 1.5f;
            Vector3 size = new Vector3(maxSize, maxSize, maxSize); //(context.gsSplatData.boundsMax - context.gsSplatData.boundsMin) * 1.2f;
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

        Stopwatch stopwatch = new Stopwatch();

            // 1. Build Octree
            stopwatch.Restart();
            OctreeBuilder.BuildOctree(splatList, m_Root, m_Settings.maxDepth, m_Settings.threshold);
            stopwatch.Stop();
            UnityEngine.Debug.Log($"BuildOctree: {stopwatch.ElapsedMilliseconds} ms");

            // 2. Extract Hull
            stopwatch.Restart();
            OctreeHullBuilder.ExtractHull(m_Root);
            stopwatch.Stop();
            UnityEngine.Debug.Log($"ExtractHull: {stopwatch.ElapsedMilliseconds} ms");

            // 3. Gather Surface Nodes
            stopwatch.Restart();
            List<OctreeNode> nodes = new List<OctreeNode>();
            GatherSurfaceNodes(m_Root, nodes);
            stopwatch.Stop();
            UnityEngine.Debug.Log($"GatherSurfaceNodes: {stopwatch.ElapsedMilliseconds} ms");

            // 4. Extract Hull Vertices
            stopwatch.Restart();
            Vector3[] hullVertices = OctreeHullBuilder.ExtractHullVertices(nodes.ToArray()).ToArray();
            stopwatch.Stop();
            UnityEngine.Debug.Log($"ExtractHullVertices: {stopwatch.ElapsedMilliseconds} ms");

            // 5. IterGen
            stopwatch.Restart();
            IterGen(m_Root, m_Settings.threshold, splatList, hullVertices);
            stopwatch.Stop();
            UnityEngine.Debug.Log($"IterGen: {stopwatch.ElapsedMilliseconds} ms");

            // 6. Generate Mesh
            stopwatch.Restart();
            List<Vector3> vertices = new List<Vector3>();
            List<int> indices = new List<int>();
            OctreeMeshGen.Gen(vertices, indices, edgeMap);
            stopwatch.Stop();
            UnityEngine.Debug.Log($"GenMesh: {stopwatch.ElapsedMilliseconds} ms");

            // Assign results
            vertexList = vertices.ToArray();
            indexList = indices.ToArray();

        }

        private void GatherSurfaceNodes(OctreeNode current, List<OctreeNode> l) {
            if (current.m_ContainsSurface && current.m_IsLeaf) {
                l.Add(current);
            }

            if (!current.m_IsLeaf) {
                foreach(var child in current.m_Children) {
                    if (child != null) {
                        GatherSurfaceNodes(child, l);
                    }
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

        private void IterGen(OctreeNode currentNode, float threshold, List<MeshUtils.SplatData> splats, Vector3[] hullVertices) {
            if (currentNode == null)
                currentNode = m_Root;

            if (currentNode.m_ContainsSurface && currentNode.m_IsLeaf)
            {
                GetVPos(currentNode, threshold, splats);
                if (currentNode.m_ContainsSurface)
                { 
                    GenEdge(currentNode, hullVertices);
                }
            }

            if (!currentNode.m_IsLeaf) {
                foreach(var child in currentNode.m_Children) {
                    if (child != null) {
                        IterGen(child, threshold, splats, hullVertices);
                    }
                }
            }
        }
        
        private static int CompareVector3(Vector3 a, Vector3 b, float epsilon = 1e-6f)
        {
            if (Mathf.Abs(a.x - b.x) > epsilon)
                return a.x < b.x ? -1 : 1;
            if (Mathf.Abs(a.y - b.y) > epsilon)
                return a.y < b.y ? -1 : 1;
            if (Mathf.Abs(a.z - b.z) > epsilon)
                return a.z < b.z ? -1 : 1;
            return 0; // Equal within epsilon
        }

        private static readonly int[,] edges = new int[,] {
            {0,1}, {1,3}, {3,2}, {2,0},  // bottom face edges
            {4,5}, {5,7}, {7,6}, {6,4},  // top face edges
            {0,4}, {1,5}, {2,6}, {3,7}   // vertical edges
        };

        public void GenEdge(OctreeNode node, Vector3[] hullVertices)
        {
            // Compute the cube vertices from node.m_Bounds
            Vector3 min = node.m_Bounds.min;
            Vector3 max = node.m_Bounds.max;

            Vector3[] cubeVertices = new Vector3[8]
            {
                new Vector3(min.x, min.y, min.z), // 0
                new Vector3(max.x, min.y, min.z), // 1
                new Vector3(min.x, min.y, max.z), // 2
                new Vector3(max.x, min.y, max.z), // 3
                new Vector3(min.x, max.y, min.z), // 4
                new Vector3(max.x, max.y, min.z), // 5
                new Vector3(min.x, max.y, max.z), // 6
                new Vector3(max.x, max.y, max.z)  // 7
            };
            // Iterate over edges and determine intersections
            for (int i = 0; i < 12; i++)
            {
                Vector3 v1 = cubeVertices[edges[i, 0]];
                Vector3 v2 = cubeVertices[edges[i, 1]];

                // // Add node to edges
                // EdgeKey line;
                // if (CompareVector3(v1, v2) > 0)
                // {
                //     line = new EdgeKey(v1, v2);
                // }
                // else
                // {
                //     line = new EdgeKey(v2, v1);
                // }

                if (hullVertices.Contains(v1) || hullVertices.Contains(v2))
                {
                    EdgeKey line = new EdgeKey(v2, v1);
                    if (!edgeMap.ContainsKey(line))
                    {
                        // If edge doesn't exist, add it
                        edgeMap[line] = new List<OctreeNode>();
                    }
                    edgeMap[line].Add(node);
                }
                else if (hullVertices.Contains(v2) && !hullVertices.Contains(v1))
                { 
                    EdgeKey line = new EdgeKey(v1, v2);
                    if (!edgeMap.ContainsKey(line))
                    {
                        // If edge doesn't exist, add it
                        edgeMap[line] = new List<OctreeNode>();
                    }
                    edgeMap[line].Add(node);
                }
            }
        }

        public void GetVPos(OctreeNode node, float threshold, List<MeshUtils.SplatData> splats)
        {
            Vector3 min = node.m_Bounds.min;
            Vector3 max = node.m_Bounds.max;

            Vector3[] cubeVertices = new Vector3[8]
            {
                new Vector3(min.x, min.y, min.z), // 0
                new Vector3(max.x, min.y, min.z), // 1
                new Vector3(min.x, min.y, max.z), // 2
                new Vector3(max.x, min.y, max.z), // 3
                new Vector3(min.x, max.y, min.z), // 4
                new Vector3(max.x, max.y, min.z), // 5
                new Vector3(min.x, max.y, max.z), // 6
                new Vector3(max.x, max.y, max.z)  // 7
            };

            Vector3 vertexSum = Vector3.zero;
            int count = 0;

            const float isosurface = 0f;
            const int numSteps = 8;
            const float tolerance = 0.05f;  // Adjust tolerance as needed

            for (int i = 0; i < 12; i++)
            {
                Vector3 v1 = cubeVertices[edges[i, 0]];
                Vector3 v2 = cubeVertices[edges[i, 1]];

                float bestDiff = float.MaxValue;
                Vector3 bestPoint = v1;

                // Sample points along edge
                for (int step = 0; step <= numSteps; step++)
                {
                    float t = step / (float)numSteps;
                    Vector3 pos = Vector3.Lerp(v1, v2, t);
                    float val = node.EvaluateSDF(pos, threshold, splats);

                    float diff = Mathf.Abs(val - isosurface);
                    if (diff < bestDiff)
                    {
                        bestDiff = diff;
                        bestPoint = pos;
                    }
                }

                // Accept only if close enough to isosurface
                if (bestDiff < tolerance)
                {
                    vertexSum += bestPoint;
                    count++;
                }
            }

            if (count > 0)
            {
                node.m_VertexPosition = vertexSum / count;
            }
            else
            {
                Vector3 mean = Vector3.zero;
                for (int i = 0; i < 10; i++)
                {
                    mean += splats[node.m_SplatIds[i]].center;
                }
                    // int counter = 0;

            // int maxSamples = 10;
            // int totalAvailable = node.m_SplatIds.Count;

            // if (totalAvailable == 0)
            // {
            //     node.m_ContainsSurface = false;
            //     node.m_VertexPosition = Vector3.zero;
            //     return;
            // }

            // HashSet<int> visited = new HashSet<int>();
            // System.Random rng = new System.Random();

            // while (visited.Count < totalAvailable && counter < maxSamples)
            // {
            //     int randIndex = rng.Next(totalAvailable);
            //     int splatId = node.m_SplatIds[randIndex];

            //     if (!visited.Add(randIndex)) continue; // already seen

            //     if (node.m_Bounds.Contains(splats[splatId].center))
            //     {
            //         counter++;
            //         mean += splats[splatId].center;
            //     }
            // }

            // if (counter == 0)
            // {
            // node.m_ContainsSurface = false;
            // node.m_VertexPosition = node.m_Bounds.center;
            // }
            // else
            // {
                    node.m_VertexPosition = mean / 10;
                    // }
                }
            }
        


        // private Vector3 BinarySearchIntersection(OctreeNode node, Vector3 v1, Vector3 v2, float threshold, List<MeshUtils.SplatData> splats)
        // {
        //     float f1 = node.EvaluateSDF(v1, threshold, splats);
        //     float f2 = node.EvaluateSDF(v2, threshold, splats);

        //     Vector3 a = v1;
        //     Vector3 b = v2;

        //     for (int i = 0; i < 8; i++)
        //     {
        //         Vector3 mid = (a + b) * 0.5f;
        //         float fmid = node.EvaluateSDF(mid, threshold, splats);

        //         if (Mathf.Abs(fmid) < 1e-6f)
        //             return mid;

        //         if (fmid * f1 < 0)
        //         {
        //             b = mid;
        //             f2 = fmid;
        //         }
        //         else
        //         {
        //             a = mid;
        //             f1 = fmid;
        //         }
        //     }

        //     return (a + b) * 0.5f;
        // }
    }
}