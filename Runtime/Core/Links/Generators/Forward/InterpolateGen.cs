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
            m_InterpolateLinkageCompute.SetBuffer(0, "_SplatPos", context.gsPosData);
            m_InterpolateLinkageCompute.SetBuffer(0, "_SplatOther", context.gsOtherData);
            m_InterpolateLinkageCompute.SetBuffer(0, "_SplatSH", context.gsSHData);
            m_InterpolateLinkageCompute.SetBuffer(0, "_SplatChunks", context.gsChunks);
            m_InterpolateLinkageCompute.SetInt("_SplatChunkCount", context.gsChunksValid ? context.gsChunks.count : 0);
            m_InterpolateLinkageCompute.SetBuffer(0, "_SplatColor", context.gsSHData);
            uint format = (uint)context.gsSplatData.posFormat | ((uint)context.gsSplatData.scaleFormat << 8) | ((uint)context.gsSplatData.shFormat << 16);
            m_InterpolateLinkageCompute.SetInt("_SplatFormat", (int)format);
            m_InterpolateLinkageCompute.SetTexture(0, "_SplatColor", context.gsColorData);

            m_InterpolateLinkageCompute.SetBuffer(0, "_VertexBasePos", context.scaffoldBaseVertex);
            m_InterpolateLinkageCompute.SetBuffer(0, "_SplatLinks", context.forwardLinks);
            m_InterpolateLinkageCompute.SetBuffer(0, "_MeshIndices", context.scaffoldIndices);

            m_InterpolateLinkageCompute.SetInt("_SplatCount", context.gsSplatData.splatCount);
            m_InterpolateLinkageCompute.SetInt("_VertexCount", context.scaffoldData.vertexCount);
            m_InterpolateLinkageCompute.SetInt("_IndexCount", context.scaffoldData.indexCount);
            m_InterpolateLinkageCompute.SetFloat("_BlendFactor", m_Settings.blendFactor);

            // Calculate number of thread groups needed
            int numThreadGroups = Mathf.CeilToInt((float)context.gsSplatData.splatCount / m_Settings.threadsPerGroup);

            // Dispatch compute shader
            m_InterpolateLinkageCompute.Dispatch(0, numThreadGroups, 1, 1);
        }
    }
}
