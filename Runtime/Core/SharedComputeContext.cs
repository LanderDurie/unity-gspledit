namespace UnityEngine.GsplEdit
{
    public class SharedComputeContext
    {
        public SplatData splatData;
        public GraphicsBuffer gpuGSPosData;
        public GraphicsBuffer gpuGSOtherData;
        public GraphicsBuffer gpuGSSHData;
        public GraphicsBuffer gpuGSChunks;
        public Texture gpuGSColorData;

        public GraphicsBuffer gpuMeshPosData;
        public ComputeBuffer gpuMeshIndexData;
        
        public ComputeBuffer gpuForwardLinks; // Links from splats to vertices
        public ComputeBuffer gpuBackwardLinks; // Links from vertices to splats
        public RenderTexture offscreenMeshTarget;
        public Camera offscreenRenderCamera;

        public int vertexCount;
        public int triangleCount;
        public int splatCount;

        public bool gpuGSChunksValid;

        public bool IsValid()
        {
            return splatData != null &&
                    splatCount > 0 &&
                    splatData.formatVersion == SplatData.kCurrentVersion &&
                    splatData.posData != null &&
                    splatData.otherData != null &&
                    splatData.shData != null &&
                    splatData.colorData != null;
        }

    }
}