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
            m_PCASmoothLinkageCompute.SetBuffer(0, "_SplatPos", context.gsPosData);
            m_PCASmoothLinkageCompute.SetBuffer(0, "_SplatOther", context.gsOtherData);
            m_PCASmoothLinkageCompute.SetBuffer(0, "_SplatSH", context.gsSHData);
            m_PCASmoothLinkageCompute.SetBuffer(0, "_SplatChunks", context.gsChunks);
            m_PCASmoothLinkageCompute.SetInt("_SplatChunkCount", context.gsChunksValid ? context.gsChunks.count : 0);
            m_PCASmoothLinkageCompute.SetBuffer(0, "_SplatColor", context.gsSHData);
            uint format = (uint)context.gsSplatData.posFormat | ((uint)context.gsSplatData.scaleFormat << 8) | ((uint)context.gsSplatData.shFormat << 16);
            m_PCASmoothLinkageCompute.SetInt("_SplatFormat", (int)format);
            m_PCASmoothLinkageCompute.SetTexture(0, "_SplatColor", context.gsColorData);

            m_PCASmoothLinkageCompute.SetBuffer(0, "_VertexBasePos", context.scaffoldBaseVertex);
            m_PCASmoothLinkageCompute.SetBuffer(0, "_SplatLinks", context.forwardLinks);
            m_PCASmoothLinkageCompute.SetBuffer(0, "_MeshIndices", context.scaffoldIndices);

            m_PCASmoothLinkageCompute.SetInt("_SplatCount", context.gsSplatData.splatCount);
            m_PCASmoothLinkageCompute.SetInt("_VertexCount", context.scaffoldData.vertexCount);
            m_PCASmoothLinkageCompute.SetInt("_IndexCount", context.scaffoldData.indexCount);
            m_PCASmoothLinkageCompute.SetFloat("_StartBlend", m_Settings.startBlend);
            m_PCASmoothLinkageCompute.SetFloat("_StopBlend", m_Settings.stopBlend);

            // Calculate number of thread groups needed
            int numThreadGroups = Mathf.CeilToInt((float)context.gsSplatData.splatCount / m_Settings.threadsPerGroup);

            // Dispatch compute shader
            m_PCASmoothLinkageCompute.Dispatch(0, numThreadGroups, 1, 1);
        }
    }
}
