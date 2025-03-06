namespace UnityEngine.GsplEdit
{
    public class InterpolateGen : LinkGenForwardBase
    {
        [System.Serializable]
        public class Settings
        {
            public uint threadsPerGroup = 64;
            public float blendFactor = 1.0f;
        }

        public Settings m_Settings = new();
        public ComputeShader m_InterpolateLinkageCompute;

        public unsafe override void Generate(SharedComputeContext context) {

            // Set compute shader parameters
            m_InterpolateLinkageCompute.SetBuffer(0, "_SplatPos", context.gpuGSPosData);
            m_InterpolateLinkageCompute.SetBuffer(0, "_SplatOther", context.gpuGSOtherData);
            m_InterpolateLinkageCompute.SetBuffer(0, "_SplatSH", context.gpuGSSHData);
            m_InterpolateLinkageCompute.SetBuffer(0, "_SplatChunks", context.gpuGSChunks);
            m_InterpolateLinkageCompute.SetInt("_SplatChunkCount", context.gpuGSChunksValid ? context.gpuGSChunks.count : 0);
            m_InterpolateLinkageCompute.SetBuffer(0, "_SplatColor", context.gpuGSSHData);
            uint format = (uint)context.splatData.posFormat | ((uint)context.splatData.scaleFormat << 8) | ((uint)context.splatData.shFormat << 16);
            m_InterpolateLinkageCompute.SetInt("_SplatFormat", (int)format);
            m_InterpolateLinkageCompute.SetTexture(0, "_SplatColor", context.gpuGSColorData);

            m_InterpolateLinkageCompute.SetBuffer(0, "_MeshVertexPos", context.gpuMeshPosData);
            m_InterpolateLinkageCompute.SetBuffer(0, "_SplatLinks", context.gpuForwardLinks);
            m_InterpolateLinkageCompute.SetBuffer(0, "_MeshIndices", context.gpuMeshIndexData);

            m_InterpolateLinkageCompute.SetInt("_SplatCount", context.splatData.splatCount);
            m_InterpolateLinkageCompute.SetInt("_VertexCount", context.vertexCount);
            m_InterpolateLinkageCompute.SetInt("_IndexCount", context.triangleCount);
            m_InterpolateLinkageCompute.SetFloat("_BlendFactor", m_Settings.blendFactor);

            // Calculate number of thread groups needed
            int numThreadGroups = Mathf.CeilToInt((float)context.splatData.splatCount / m_Settings.threadsPerGroup);

            // Dispatch compute shader
            m_InterpolateLinkageCompute.Dispatch(0, numThreadGroups, 1, 1);
        }
    }
}
