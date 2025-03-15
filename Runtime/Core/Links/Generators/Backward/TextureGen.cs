namespace UnityEngine.GsplEdit
{
    public class TextureGen : LinkGenBackwardBase
    {
        [System.Serializable]
        public class Settings
        {
            public uint threadsPerGroup = 64;
            public Vector2 resolution = new Vector2(1000, 1000);
        }

        public Settings m_Settings = new();
        public ComputeShader m_TextureLinkageCompute;

        public unsafe override void Generate(SharedComputeContext context) {

            // Set compute shader parameters
            m_TextureLinkageCompute.SetBuffer(0, "_SplatPos", context.gsPosData);
            m_TextureLinkageCompute.SetBuffer(0, "_SplatOther", context.gsOtherData);
            m_TextureLinkageCompute.SetBuffer(0, "_SplatSH", context.gsSHData);
            m_TextureLinkageCompute.SetBuffer(0, "_SplatChunks", context.gsChunks);
            m_TextureLinkageCompute.SetInt("_SplatChunkCount", context.gsChunksValid ? context.gsChunks.count : 0);
            m_TextureLinkageCompute.SetBuffer(0, "_SplatColor", context.gsSHData);
            uint format = (uint)context.gsSplatData.posFormat | ((uint)context.gsSplatData.scaleFormat << 8) | ((uint)context.gsSplatData.shFormat << 16);
            m_TextureLinkageCompute.SetInt("_SplatFormat", (int)format);
            m_TextureLinkageCompute.SetTexture(0, "_SplatColor", context.gsColorData);

            m_TextureLinkageCompute.SetBuffer(0, "_VertexBasePos", context.scaffoldBaseVertex);
            m_TextureLinkageCompute.SetBuffer(0, "_SplatLinks", context.forwardLinks);
            m_TextureLinkageCompute.SetBuffer(0, "_MeshIndices", context.scaffoldIndices);

            m_TextureLinkageCompute.SetInt("_SplatCount", context.gsSplatData.splatCount);
            m_TextureLinkageCompute.SetInt("_VertexCount", context.scaffoldData.vertexCount);
            m_TextureLinkageCompute.SetInt("_IndexCount", context.scaffoldData.indexCount);

            // Calculate number of thread groups needed
            int numThreadGroups = Mathf.CeilToInt((float)context.gsSplatData.splatCount / m_Settings.threadsPerGroup);

            // Dispatch compute shader
            m_TextureLinkageCompute.Dispatch(0, numThreadGroups, 1, 1);
        }
    }
}
