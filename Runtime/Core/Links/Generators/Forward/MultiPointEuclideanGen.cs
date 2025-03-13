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
            m_MultiPointEuclideanLinkageCompute.SetBuffer(0, "_SplatPos", context.gpuGSPosData);
            m_MultiPointEuclideanLinkageCompute.SetBuffer(0, "_SplatOther", context.gpuGSOtherData);
            m_MultiPointEuclideanLinkageCompute.SetBuffer(0, "_SplatSH", context.gpuGSSHData);
            m_MultiPointEuclideanLinkageCompute.SetBuffer(0, "_SplatChunks", context.gpuGSChunks);
            m_MultiPointEuclideanLinkageCompute.SetInt("_SplatChunkCount", context.gpuGSChunksValid ? context.gpuGSChunks.count : 0);
            m_MultiPointEuclideanLinkageCompute.SetBuffer(0, "_SplatColor", context.gpuGSSHData);
            uint format = (uint)context.splatData.posFormat | ((uint)context.splatData.scaleFormat << 8) | ((uint)context.splatData.shFormat << 16);
            m_MultiPointEuclideanLinkageCompute.SetInt("_SplatFormat", (int)format);
            m_MultiPointEuclideanLinkageCompute.SetTexture(0, "_SplatColor", context.gpuGSColorData);

            m_MultiPointEuclideanLinkageCompute.SetBuffer(0, "_VertexBasePos", context.gpuMeshBaseVertex);
            m_MultiPointEuclideanLinkageCompute.SetBuffer(0, "_SplatLinks", context.gpuForwardLinks);
            m_MultiPointEuclideanLinkageCompute.SetBuffer(0, "_MeshIndices", context.gpuMeshIndices);

            m_MultiPointEuclideanLinkageCompute.SetInt("_SplatCount", context.splatData.splatCount);
            m_MultiPointEuclideanLinkageCompute.SetInt("_VertexCount", context.vertexCount);
            m_MultiPointEuclideanLinkageCompute.SetInt("_IndexCount", context.indexCount);
            m_MultiPointEuclideanLinkageCompute.SetFloat("_GlobalSigma", m_Settings.sigma);


            // Calculate number of thread groups needed
            int numThreadGroups = Mathf.CeilToInt((float)context.splatData.splatCount / m_Settings.threadsPerGroup);

            // Dispatch compute shader
            m_MultiPointEuclideanLinkageCompute.Dispatch(0, numThreadGroups, 1, 1);
        }
    }
}
