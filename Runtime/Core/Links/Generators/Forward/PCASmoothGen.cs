namespace UnityEngine.GsplEdit
{
    public class PCASmoothGen : LinkGenForwardBase
    {
        [System.Serializable]
        public class Settings
        {
            public uint threadsPerGroup = 64;
            public float startBlend = 3.0f;
            public float stopBlend = 5.0f;
        }

        public Settings m_Settings = new();
        public ComputeShader m_PCASmoothLinkageCompute;

        public unsafe override void Generate(SharedComputeContext context) {

            // Set compute shader parameters
            m_PCASmoothLinkageCompute.SetBuffer(0, "_SplatPos", context.gpuGSPosData);
            m_PCASmoothLinkageCompute.SetBuffer(0, "_SplatOther", context.gpuGSOtherData);
            m_PCASmoothLinkageCompute.SetBuffer(0, "_SplatSH", context.gpuGSSHData);
            m_PCASmoothLinkageCompute.SetBuffer(0, "_SplatChunks", context.gpuGSChunks);
            m_PCASmoothLinkageCompute.SetInt("_SplatChunkCount", context.gpuGSChunksValid ? context.gpuGSChunks.count : 0);
            m_PCASmoothLinkageCompute.SetBuffer(0, "_SplatColor", context.gpuGSSHData);
            uint format = (uint)context.splatData.posFormat | ((uint)context.splatData.scaleFormat << 8) | ((uint)context.splatData.shFormat << 16);
            m_PCASmoothLinkageCompute.SetInt("_SplatFormat", (int)format);
            m_PCASmoothLinkageCompute.SetTexture(0, "_SplatColor", context.gpuGSColorData);

            m_PCASmoothLinkageCompute.SetBuffer(0, "_MeshVertexPos", context.gpuMeshPosData);
            m_PCASmoothLinkageCompute.SetBuffer(0, "_SplatLinks", context.gpuForwardLinks);
            m_PCASmoothLinkageCompute.SetBuffer(0, "_MeshIndices", context.gpuMeshIndexData);

            m_PCASmoothLinkageCompute.SetInt("_SplatCount", context.splatData.splatCount);
            m_PCASmoothLinkageCompute.SetInt("_VertexCount", context.vertexCount);
            m_PCASmoothLinkageCompute.SetInt("_IndexCount", context.triangleCount);
            m_PCASmoothLinkageCompute.SetFloat("_StartBlend", m_Settings.startBlend);
            m_PCASmoothLinkageCompute.SetFloat("_StopBlend", m_Settings.stopBlend);

            // Calculate number of thread groups needed
            int numThreadGroups = Mathf.CeilToInt((float)context.splatData.splatCount / m_Settings.threadsPerGroup);

            // Dispatch compute shader
            m_PCASmoothLinkageCompute.Dispatch(0, numThreadGroups, 1, 1);
        }
    }
}
