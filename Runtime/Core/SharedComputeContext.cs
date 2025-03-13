namespace UnityEngine.GsplEdit
{
    public class SharedComputeContext
    {
        public SplatData splatData;
        public GraphicsBuffer gpuGSPosData;
        public GraphicsBuffer gpuGSOtherData;
        public GraphicsBuffer gpuGSSHData;
        public GraphicsBuffer gpuGSChunks;
        public Texture2D splatColorMap;
        public Texture2D splatNormalMap;

        public int splatCount;

        public Texture gpuGSColorData;
        public GraphicsBuffer gpuMeshBaseVertex;
        public GraphicsBuffer gpuMeshModVertex;
        public GraphicsBuffer gpuMeshIndices;
        public int indexCount;
        public int vertexCount;

        public ComputeBuffer gpuForwardLinks; // Links from splats to vertices
        // public ComputeBuffer gpuBackwardLinks; // Links from vertices to splats
        public RenderTexture offscreenMeshTarget;
        public Camera offscreenRenderCamera;
        public Mesh scaffoldMesh;

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

        public void ValidateFields()
        {
            if (splatData == null)
                Debug.LogWarning("splatData is not assigned.");
            if (splatCount <= 0)
                Debug.LogWarning($"splatCount is invalid: {splatCount}");
            if (splatData != null && splatData.formatVersion != SplatData.kCurrentVersion)
                Debug.LogWarning($"splatData.formatVersion is invalid: {splatData.formatVersion} (expected {SplatData.kCurrentVersion})");
            if (splatData != null && splatData.posData == null)
                Debug.LogWarning("splatData.posData is not assigned.");
            if (splatData != null && splatData.otherData == null)
                Debug.LogWarning("splatData.otherData is not assigned.");
            if (splatData != null && splatData.shData == null)
                Debug.LogWarning("splatData.shData is not assigned.");
            if (splatData != null && splatData.colorData == null)
                Debug.LogWarning("splatData.colorData is not assigned.");

            if (gpuGSPosData == null)
                Debug.LogWarning("gpuGSPosData is not assigned.");
            if (gpuGSOtherData == null)
                Debug.LogWarning("gpuGSOtherData is not assigned.");
            if (gpuGSSHData == null)
                Debug.LogWarning("gpuGSSHData is not assigned.");
            if (gpuGSChunks == null)
                Debug.LogWarning("gpuGSChunks is not assigned.");
            if (gpuGSColorData == null)
                Debug.LogWarning("gpuGSColorData is not assigned.");
            if (gpuMeshBaseVertex == null)
                Debug.LogWarning("gpuMeshBaseVertex is not assigned.");
            if (gpuMeshModVertex == null)
                Debug.LogWarning("gpuMeshModVertex is not assigned.");
            if (gpuMeshIndices == null)
                Debug.LogWarning("gpuMeshIndices is not assigned.");
            if (indexCount <= 0)
                Debug.LogWarning($"indexCount is invalid: {indexCount}");
            if (vertexCount <= 0)
                Debug.LogWarning($"vertexCount is invalid: {vertexCount}");
            if (gpuForwardLinks == null)
                Debug.LogWarning("gpuForwardLinks is not assigned.");
            // if (gpuBackwardLinks == null)
                // Debug.LogWarning("gpuBackwardLinks is not assigned.");
            if (offscreenMeshTarget == null)
                Debug.LogWarning("offscreenMeshTarget is not assigned.");
            if (offscreenRenderCamera == null)
                Debug.LogWarning("offscreenRenderCamera is not assigned.");
            if (!gpuGSChunksValid)
                Debug.LogWarning("gpuGSChunksValid is false.");
        }
    }
}