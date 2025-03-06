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
            m_DistanceBasedLinkageCompute.SetBuffer(0, "_SplatPos", context.gpuGSPosData);
            m_DistanceBasedLinkageCompute.SetBuffer(0, "_SplatOther", context.gpuGSOtherData);
            m_DistanceBasedLinkageCompute.SetBuffer(0, "_SplatSH", context.gpuGSSHData);
            m_DistanceBasedLinkageCompute.SetBuffer(0, "_SplatChunks", context.gpuGSChunks);
            m_DistanceBasedLinkageCompute.SetInt("_SplatChunkCount", context.gpuGSChunksValid ? context.gpuGSChunks.count : 0);
            m_DistanceBasedLinkageCompute.SetBuffer(0, "_SplatColor", context.gpuGSSHData);
            uint format = (uint)context.splatData.posFormat | ((uint)context.splatData.scaleFormat << 8) | ((uint)context.splatData.shFormat << 16);
            m_DistanceBasedLinkageCompute.SetInt("_SplatFormat", (int)format);
            m_DistanceBasedLinkageCompute.SetTexture(0, "_SplatColor", context.gpuGSColorData);

            m_DistanceBasedLinkageCompute.SetBuffer(0, "_MeshVertexPos", context.gpuMeshPosData);
            m_DistanceBasedLinkageCompute.SetBuffer(0, "_SplatLinkBuffer", context.gpuForwardLinks);

            m_DistanceBasedLinkageCompute.SetInt("_SplatCount", context.splatData.splatCount);
            m_DistanceBasedLinkageCompute.SetInt("_VertexCount", context.vertexCount);

            // Calculate number of thread groups needed
            int numThreadGroups = Mathf.CeilToInt((float)context.splatData.splatCount / m_Settings.threadsPerGroup);

            // Dispatch compute shader
            m_DistanceBasedLinkageCompute.Dispatch(0, numThreadGroups, 1, 1);
        }
    }
}
