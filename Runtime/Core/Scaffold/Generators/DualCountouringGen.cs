using System.Collections.Generic;
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

        public Settings m_Settings = new Settings();
        public ComputeShader m_IcosahedronComputeShader;

        private OctreeNode m_Root;
        private System.Random random = new System.Random();

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
    }
}