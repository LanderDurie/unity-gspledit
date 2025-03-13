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
            m_TextureLinkageCompute.SetBuffer(0, "_SplatPos", context.gpuGSPosData);
            m_TextureLinkageCompute.SetBuffer(0, "_SplatOther", context.gpuGSOtherData);
            m_TextureLinkageCompute.SetBuffer(0, "_SplatSH", context.gpuGSSHData);
            m_TextureLinkageCompute.SetBuffer(0, "_SplatChunks", context.gpuGSChunks);
            m_TextureLinkageCompute.SetInt("_SplatChunkCount", context.gpuGSChunksValid ? context.gpuGSChunks.count : 0);
            m_TextureLinkageCompute.SetBuffer(0, "_SplatColor", context.gpuGSSHData);
            uint format = (uint)context.splatData.posFormat | ((uint)context.splatData.scaleFormat << 8) | ((uint)context.splatData.shFormat << 16);
            m_TextureLinkageCompute.SetInt("_SplatFormat", (int)format);
            m_TextureLinkageCompute.SetTexture(0, "_SplatColor", context.gpuGSColorData);

            m_TextureLinkageCompute.SetBuffer(0, "_VertexBasePos", context.gpuMeshBaseVertex);
            m_TextureLinkageCompute.SetBuffer(0, "_SplatLinks", context.gpuForwardLinks);
            m_TextureLinkageCompute.SetBuffer(0, "_MeshIndices", context.gpuMeshIndices);

            m_TextureLinkageCompute.SetInt("_SplatCount", context.splatData.splatCount);
            m_TextureLinkageCompute.SetInt("_VertexCount", context.vertexCount);
            m_TextureLinkageCompute.SetInt("_IndexCount", context.indexCount);

            // Calculate number of thread groups needed
            int numThreadGroups = Mathf.CeilToInt((float)context.splatData.splatCount / m_Settings.threadsPerGroup);

            // Dispatch compute shader
            m_TextureLinkageCompute.Dispatch(0, numThreadGroups, 1, 1);
        }
    }
}
