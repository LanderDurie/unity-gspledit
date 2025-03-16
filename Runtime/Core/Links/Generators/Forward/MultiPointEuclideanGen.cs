namespace UnityEngine.GsplEdit
{
    public class MultiPointEuclideanGen : LinkGenForwardBase
    {
        [System.Serializable]
        public class Settings
        {
            public uint threadsPerGroup = 64;
            public float sigma = 1.0f;
        }

        public Settings m_Settings = new();
        public ComputeShader m_MultiPointEuclideanLinkageCompute;

        public unsafe override void Generate(SharedComputeContext context) {

            // Set compute shader parameters
            m_MultiPointEuclideanLinkageCompute.SetBuffer(0, "_SplatPos", context.gsPosData);
            m_MultiPointEuclideanLinkageCompute.SetBuffer(0, "_SplatOther", context.gsOtherData);
            m_MultiPointEuclideanLinkageCompute.SetBuffer(0, "_SplatSH", context.gsSHData);
            m_MultiPointEuclideanLinkageCompute.SetBuffer(0, "_SplatChunks", context.gsChunks);
            m_MultiPointEuclideanLinkageCompute.SetInt("_SplatChunkCount", context.gsChunksValid ? context.gsChunks.count : 0);
            m_MultiPointEuclideanLinkageCompute.SetBuffer(0, "_SplatColor", context.gsSHData);
            uint format = (uint)context.gsSplatData.posFormat | ((uint)context.gsSplatData.scaleFormat << 8) | ((uint)context.gsSplatData.shFormat << 16);
            m_MultiPointEuclideanLinkageCompute.SetInt("_SplatFormat", (int)format);
            m_MultiPointEuclideanLinkageCompute.SetTexture(0, "_SplatColor", context.gsColorData);

            m_MultiPointEuclideanLinkageCompute.SetBuffer(0, "_VertexBasePos", context.scaffoldBaseVertex);
            m_MultiPointEuclideanLinkageCompute.SetBuffer(0, "_SplatLinks", context.forwardLinks);
            m_MultiPointEuclideanLinkageCompute.SetBuffer(0, "_MeshIndices", context.scaffoldIndices);

            m_MultiPointEuclideanLinkageCompute.SetInt("_SplatCount", context.gsSplatData.splatCount);
            m_MultiPointEuclideanLinkageCompute.SetInt("_VertexCount", context.scaffoldData.vertexCount);
            m_MultiPointEuclideanLinkageCompute.SetInt("_IndexCount", context.scaffoldData.indexCount);
            m_MultiPointEuclideanLinkageCompute.SetFloat("_GlobalSigma", m_Settings.sigma);


            // Calculate number of thread groups needed
            int numThreadGroups = Mathf.CeilToInt((float)context.gsSplatData.splatCount / m_Settings.threadsPerGroup);

            // Dispatch compute shader
            m_MultiPointEuclideanLinkageCompute.Dispatch(0, numThreadGroups, 1, 1);
        }
    }
}
