using System;
using System.Runtime.InteropServices;

namespace UnityEngine.GsplEdit
{
    public class MahalanobisGen : LinkGenForwardBase
    {
        [System.Serializable]
        public class Settings
        {
            public uint threadsPerGroup = 64;
            public float sigma = 1.0f;
        }

        public Settings m_Settings = new();
        public ComputeShader m_MahalanobisBasedLinkageCompute;

        public unsafe override void Generate(SharedComputeContext context) {

            // Set compute shader parameters
            m_MahalanobisBasedLinkageCompute.SetBuffer(0, "_SplatPos", context.gpuGSPosData);
            m_MahalanobisBasedLinkageCompute.SetBuffer(0, "_SplatOther", context.gpuGSOtherData);
            m_MahalanobisBasedLinkageCompute.SetBuffer(0, "_SplatSH", context.gpuGSSHData);
            m_MahalanobisBasedLinkageCompute.SetBuffer(0, "_SplatChunks", context.gpuGSChunks);
            m_MahalanobisBasedLinkageCompute.SetInt("_SplatChunkCount", context.gpuGSChunksValid ? context.gpuGSChunks.count : 0);
            m_MahalanobisBasedLinkageCompute.SetBuffer(0, "_SplatColor", context.gpuGSSHData);
            uint format = (uint)context.splatData.posFormat | ((uint)context.splatData.scaleFormat << 8) | ((uint)context.splatData.shFormat << 16);
            m_MahalanobisBasedLinkageCompute.SetInt("_SplatFormat", (int)format);
            m_MahalanobisBasedLinkageCompute.SetTexture(0, "_SplatColor", context.gpuGSColorData);

            m_MahalanobisBasedLinkageCompute.SetBuffer(0, "_VertexProps", context.gpuMeshVerts);
            m_MahalanobisBasedLinkageCompute.SetBuffer(0, "_SplatLinkBuffer", context.gpuForwardLinks);
            m_MahalanobisBasedLinkageCompute.SetBuffer(0, "_TriangleProps", context.gpuMeshTriangles);

            m_MahalanobisBasedLinkageCompute.SetInt("_SplatCount", context.splatData.splatCount);
            m_MahalanobisBasedLinkageCompute.SetInt("_VertexCount", context.vertexCount);
            m_MahalanobisBasedLinkageCompute.SetInt("_TriangleCount", context.triangleCount);
            m_MahalanobisBasedLinkageCompute.SetFloat("_GlobalSigma", m_Settings.sigma);

            // Calculate number of thread groups needed
            int numThreadGroups = Mathf.CeilToInt((float)context.splatData.splatCount / m_Settings.threadsPerGroup);

            // Dispatch compute shader
            m_MahalanobisBasedLinkageCompute.Dispatch(0, numThreadGroups, 1, 1);

        }
    }
}
