using System.Linq;

namespace UnityEngine.GsplEdit
{

    public class SharedComputeContext
    {
        public SplatData splatData;
        public GraphicsBuffer gpuGSPosData;
        public GraphicsBuffer gpuGSOtherData;
        public GraphicsBuffer gpuGSSHData;
        public GraphicsBuffer gpuMeshVerts;
        public GraphicsBuffer gpuMeshEdges;
        public GraphicsBuffer gpuForwardLinks; // Links from splats to vertices
        public GraphicsBuffer gpuBackwardLinks; // Links from vertices to splats

        public int vertexCount;
        public int edgeCount;
        public int splatCount;

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