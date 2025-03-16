using System;
using System.Runtime.InteropServices;

namespace UnityEngine.GsplEdit
{
    public class BackwardDistanceGen : LinkGenBackwardBase
    {
        [System.Serializable]
        public class Settings
        {
            public uint threadsPerGroup = 64;
        }

        public Settings m_Settings = new();
        public ComputeShader m_DistanceBasedLinkageCompute;

        public unsafe override void Generate(SharedComputeContext context) {

            // Set compute shader parameters
            m_DistanceBasedLinkageCompute.SetBuffer(0, "_SplatPos", context.gsPosData);
            m_DistanceBasedLinkageCompute.SetBuffer(0, "_SplatOther", context.gsOtherData);
            m_DistanceBasedLinkageCompute.SetBuffer(0, "_SplatSH", context.gsSHData);
            m_DistanceBasedLinkageCompute.SetBuffer(0, "_SplatChunks", context.gsChunks);
            m_DistanceBasedLinkageCompute.SetInt("_SplatChunkCount", context.gsChunksValid ? context.gsChunks.count : 0);
            m_DistanceBasedLinkageCompute.SetBuffer(0, "_SplatColor", context.gsSHData);
            uint format = (uint)context.gsSplatData.posFormat | ((uint)context.gsSplatData.scaleFormat << 8) | ((uint)context.gsSplatData.shFormat << 16);
            m_DistanceBasedLinkageCompute.SetInt("_SplatFormat", (int)format);
            m_DistanceBasedLinkageCompute.SetTexture(0, "_SplatColor", context.gsColorData);

            m_DistanceBasedLinkageCompute.SetBuffer(0, "_MeshPos", context.scaffoldBaseVertex);
            m_DistanceBasedLinkageCompute.SetBuffer(0, "_SplatLinkBuffer", context.forwardLinks);

            m_DistanceBasedLinkageCompute.SetInt("_SplatCount", context.gsSplatData.splatCount);
            m_DistanceBasedLinkageCompute.SetInt("_VertexCount", context.scaffoldData.vertexCount);

            // Calculate number of thread groups needed
            int numThreadGroups = Mathf.CeilToInt((float)context.gsSplatData.splatCount / m_Settings.threadsPerGroup);

            // Dispatch compute shader
            m_DistanceBasedLinkageCompute.Dispatch(0, numThreadGroups, 1, 1);
        }
    }
}
