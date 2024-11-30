using System.Linq;

namespace UnityEngine.GsplEdit
{
    [ExecuteInEditMode]
    public class DynamicSplat : MonoBehaviour
    {
        private EditableMesh m_Mesh;
        private GSRenderer m_GSRenderer;
        private SharedComputeContext m_Context;

        public void OnEnable()
        {
            m_Context = new();
        }

        public void OnDisable() {
            Destroy();
        }


        public void CreateBuffers()
        {
            if (m_Context.splatData != null && m_Context.splatData.splatCount > 0)
            {
                m_GSRenderer = GSRenderer.Create(transform, isActiveAndEnabled, ref m_Context);
                m_Mesh = new();

                // Create splat buffers
                m_Context.splatCount = m_Context.splatData.splatCount;
                m_Context.gpuGSPosData = new GraphicsBuffer(GraphicsBuffer.Target.Raw | GraphicsBuffer.Target.CopySource, (int)(m_Context.splatData.posData.dataSize / 4), 4) { name = "GaussianPosData" };
                m_Context.gpuGSPosData.SetData(m_Context.splatData.posData.GetData<uint>());
                m_Context.gpuGSOtherData = new GraphicsBuffer(GraphicsBuffer.Target.Raw | GraphicsBuffer.Target.CopySource, (int)(m_Context.splatData.otherData.dataSize / 4), 4) { name = "GaussianOtherData" };
                m_Context.gpuGSOtherData.SetData(m_Context.splatData.otherData.GetData<uint>());
                m_Context.gpuGSSHData = new GraphicsBuffer(GraphicsBuffer.Target.Raw, (int)(m_Context.splatData.shData.dataSize / 4), 4) { name = "GaussianSHData" };
                m_Context.gpuGSSHData.SetData(m_Context.splatData.shData.GetData<uint>());

                // Create mesh buffers
                m_Context.vertexCount = 1;
                m_Context.edgeCount = 1;
                m_Context.gpuMeshVerts = new GraphicsBuffer(GraphicsBuffer.Target.Raw | GraphicsBuffer.Target.CopySource, m_Context.vertexCount, (int)Vertex.StructSize()) { name = "MeshVertices" };
                m_Context.gpuMeshVerts.SetData(Enumerable.Repeat(Vertex.Default(), m_Context.vertexCount).ToArray());
                m_Context.gpuMeshEdges = new GraphicsBuffer(GraphicsBuffer.Target.Raw | GraphicsBuffer.Target.CopySource, m_Context.edgeCount, (int)Edge.Size()) { name = "MeshEdges" };
                m_Context.gpuMeshEdges.SetData(Enumerable.Repeat(new Edge(0, 0), m_Context.edgeCount).ToArray());

                // Create link buffers
                m_Context.gpuForwardLinks = new GraphicsBuffer(GraphicsBuffer.Target.Raw | GraphicsBuffer.Target.CopySource, m_Context.splatData.splatCount, (int)ForwardLink.StructSize()) { name = "ForwardLinks" };
                m_Context.gpuForwardLinks.SetData(Enumerable.Repeat(ForwardLink.Default(), m_Context.splatData.splatCount).ToArray());
                m_Context.gpuBackwardLinks = new GraphicsBuffer(GraphicsBuffer.Target.Raw | GraphicsBuffer.Target.CopySource, 1, (int)BackwardLink.StructSize()) { name = "BackwardLinks" };
                m_Context.gpuBackwardLinks.SetData(Enumerable.Repeat(BackwardLink.Default(), 1).ToArray());
            }
        }

        public void Destroy() {
            m_GSRenderer?.Destroy();
            DestroyBuffers();
        }

        private void DestroyBuffers()
        {
            m_Context.gpuGSPosData?.Dispose();
            m_Context.gpuGSPosData = null;
            m_Context.gpuGSOtherData?.Dispose();
            m_Context.gpuGSOtherData = null;
            m_Context.gpuGSSHData?.Dispose();
            m_Context.gpuGSSHData = null;
            m_Context.gpuMeshVerts?.Dispose();
            m_Context.gpuMeshVerts = null;
            m_Context.gpuMeshEdges?.Dispose();
            m_Context.gpuMeshEdges = null;
            m_Context.gpuForwardLinks?.Dispose();
            m_Context.gpuForwardLinks = null;
            m_Context.gpuBackwardLinks?.Dispose();
            m_Context.gpuBackwardLinks = null;
            m_Context.splatCount = 0;
            m_Context.vertexCount = 0;
            m_Context.edgeCount = 0;
        }

        public void LoadGS(SplatData data)
        {
            Destroy();
            m_Context.splatData = data;
            CreateBuffers();
        }

        public SplatData GetSplatData()
        {
            return m_Context.splatData;
        }
        public GSRenderer GetSplatRenderer() {
            return m_GSRenderer;
        }

        public EditableMesh GetMesh() {
            return m_Mesh;
        }

        public void Update()
        {

            if (m_GSRenderer != null)
            {
                m_GSRenderer.m_Transform = transform;
                m_GSRenderer.m_IsActiveAndEnabled = isActiveAndEnabled;
                m_GSRenderer.Update();
            }
        }

        public void ActivateCamera(int index) {
            if (m_GSRenderer != null) {
                                Debug.Log("update");

                m_GSRenderer.ActivateCamera(index);
            }
        }
    }
}