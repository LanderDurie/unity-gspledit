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
            m_MahalanobisBasedLinkageCompute.SetBuffer(0, "_SplatPos", context.gsPosData);
            m_MahalanobisBasedLinkageCompute.SetBuffer(0, "_SplatOther", context.gsOtherData);
            m_MahalanobisBasedLinkageCompute.SetBuffer(0, "_SplatSH", context.gsSHData);
            m_MahalanobisBasedLinkageCompute.SetBuffer(0, "_SplatChunks", context.gsChunks);
            m_MahalanobisBasedLinkageCompute.SetInt("_SplatChunkCount", context.gsChunksValid ? context.gsChunks.count : 0);
            m_MahalanobisBasedLinkageCompute.SetBuffer(0, "_SplatColor", context.gsSHData);
            uint format = (uint)context.gsSplatData.posFormat | ((uint)context.gsSplatData.scaleFormat << 8) | ((uint)context.gsSplatData.shFormat << 16);
            m_MahalanobisBasedLinkageCompute.SetInt("_SplatFormat", (int)format);
            m_MahalanobisBasedLinkageCompute.SetTexture(0, "_SplatColor", context.gsColorData);

            m_MahalanobisBasedLinkageCompute.SetBuffer(0, "_VertexBasePos", context.scaffoldBaseVertex);
            m_MahalanobisBasedLinkageCompute.SetBuffer(0, "_SplatLinks", context.forwardLinks);
            m_MahalanobisBasedLinkageCompute.SetBuffer(0, "_MeshIndices", context.scaffoldIndices);

            m_MahalanobisBasedLinkageCompute.SetInt("_SplatCount", context.gsSplatData.splatCount);
            m_MahalanobisBasedLinkageCompute.SetInt("_VertexCount", context.scaffoldData.vertexCount);
            m_MahalanobisBasedLinkageCompute.SetInt("_IndexCount", context.scaffoldData.indexCount);
            m_MahalanobisBasedLinkageCompute.SetFloat("_GlobalSigma", m_Settings.sigma);

            // Calculate number of thread groups needed
            int numThreadGroups = Mathf.CeilToInt((float)context.gsSplatData.splatCount / m_Settings.threadsPerGroup);

            // Dispatch compute shader
            m_MahalanobisBasedLinkageCompute.Dispatch(0, numThreadGroups, 1, 1);

        }
    }
}
