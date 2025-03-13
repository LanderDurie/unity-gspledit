using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.GsplEdit {
    [ExecuteAlways]
    public class DynamicSplat : MonoBehaviour {

        public SplatData m_SplatData;
        public Material m_ScaffoldMaterial;
        public Material m_SurfaceMaterial;
        public Material m_ShadowCasterMaterial;
        public Material m_PostProcessMaterial;
        public float m_ProjectTextureResolutionMultiplier = 1.0f;
        public Vector3 m_SplatTextureResolution = new Vector2(1000, 1000);
        public ComputeShader m_CSEditMesh;
        public GameObject m_DebugPlane;

        // Public fields hidden in the Inspector but visible in the Project window
        private EditableMesh m_Mesh;
        private GSRenderer m_GSRenderer;
        private SharedComputeContext m_Context;
        private MeshGen m_MeshGenerator;
        private LinkGen m_LinkGenerator;
        private ModifierSystem m_ModifierSystem;
        private Rect m_ScreenSize;

        public void OnEnable() {
            m_Context = new();
            m_MeshGenerator = new(ref m_Context);
            m_LinkGenerator = new(ref m_Context);
            m_ModifierSystem = new(ref m_Context);
            // Update offscreen in PreCull to avoid lag behind splat
            if (GraphicsSettings.currentRenderPipeline == null)
                Camera.onPreCull += OnPreCullCamera;
        }

        public void OnDisable() {
            Destroy();
            if (GraphicsSettings.currentRenderPipeline == null)
                Camera.onPreCull -= OnPreCullCamera;
        }

        private void OnDestroy() {
            Destroy();
        }

        public unsafe void CreateBuffers() {
            if (m_Context.splatData != null && m_Context.splatData.splatCount > 0) {
                m_GSRenderer = GSRenderer.Create(transform, isActiveAndEnabled, ref m_Context);

                // Create splat buffers
                m_Context.splatCount = m_Context.splatData.splatCount;
                m_Context.gpuGSPosData = new GraphicsBuffer(GraphicsBuffer.Target.Raw | GraphicsBuffer.Target.CopySource, (int)(m_Context.splatData.posData.dataSize / 4), 4) { name = "GaussianPosData" };
                m_Context.gpuGSPosData.SetData(m_Context.splatData.posData.GetData<uint>());
                m_Context.gpuGSOtherData = new GraphicsBuffer(GraphicsBuffer.Target.Raw | GraphicsBuffer.Target.CopySource, (int)(m_Context.splatData.otherData.dataSize / 4), 4) { name = "GaussianOtherData" };
                m_Context.gpuGSOtherData.SetData(m_Context.splatData.otherData.GetData<uint>());
                m_Context.gpuGSSHData = new GraphicsBuffer(GraphicsBuffer.Target.Raw, (int)(m_Context.splatData.shData.dataSize / 4), 4) { name = "GaussianSHData" };
                m_Context.gpuGSSHData.SetData(m_Context.splatData.shData.GetData<uint>());

                // Create mesh buffers
                m_Context.indexCount = 1;
                m_Context.vertexCount = 1;
                m_Context.gpuMeshModVertex = new GraphicsBuffer(GraphicsBuffer.Target.Raw | GraphicsBuffer.Target.CopySource, m_Context.vertexCount, sizeof(Vector3)) { name = "MeshBaseVertices" };
                m_Context.gpuMeshModVertex.SetData(Enumerable.Repeat(Vector3.zero, m_Context.vertexCount).ToArray());
                
                m_Context.gpuMeshBaseVertex = new GraphicsBuffer(GraphicsBuffer.Target.Raw | GraphicsBuffer.Target.CopySource, m_Context.vertexCount, sizeof(Vector3)) { name = "MeshModVertices" };
                m_Context.gpuMeshBaseVertex.SetData(Enumerable.Repeat(Vector3.zero, m_Context.vertexCount).ToArray());

                m_Context.gpuMeshIndices = new GraphicsBuffer(GraphicsBuffer.Target.Raw | GraphicsBuffer.Target.CopySource, m_Context.indexCount, sizeof(int)) { name = "MeshIndices" };
                m_Context.gpuMeshIndices.SetData(Enumerable.Repeat(0, m_Context.indexCount).ToArray());
                
                m_Context.splatColorMap = new Texture2D(1000, 1000, TextureFormat.RGBA32, false);
                m_Context.splatNormalMap = new Texture2D(1000, 1000, TextureFormat.RGBA32, false);

                // Create link buffers
                m_Context.gpuForwardLinks = new ComputeBuffer(m_Context.splatData.splatCount, sizeof(ForwardLink));
                m_Context.gpuForwardLinks.SetData(Enumerable.Repeat(ForwardLink.Default(), m_Context.splatData.splatCount).ToArray());
                // m_Context.gpuBackwardLinks = new ComputeBuffer(1, sizeof(BackwardLink));
                // m_Context.gpuBackwardLinks.SetData(Enumerable.Repeat(BackwardLink.Default(), 1).ToArray());

                // Create offscreen camera
                GameObject offscreenCameraObject = new GameObject("offscreenCameraObject");
                offscreenCameraObject.hideFlags = HideFlags.HideAndDontSave;
                m_Context.offscreenRenderCamera = offscreenCameraObject.AddComponent<Camera>();
                m_Context.offscreenRenderCamera.enabled = false;
                m_Context.offscreenRenderCamera.renderingPath = RenderingPath.DeferredShading;
                m_Context.offscreenRenderCamera.clearFlags = CameraClearFlags.SolidColor;
                m_Context.offscreenRenderCamera.backgroundColor = new Color(0,0,0,0);
                CreateOffscreenTexture();
                if (Camera.current != null) {
                    m_ScreenSize = Camera.current.pixelRect;
                }
            }
        }

        public void CreateOffscreenTexture() {
            int width = 1;
            int height = 1;
            if (m_ScreenSize != null && m_ScreenSize.width != 0 & m_ScreenSize.height != 0) {
                width = (int)(m_ScreenSize.width * m_ProjectTextureResolutionMultiplier);
                height = (int)(m_ScreenSize.height * m_ProjectTextureResolutionMultiplier);
            }

            m_Context.offscreenMeshTarget = new RenderTexture(
                width, 
                height, 
                24,
                RenderTextureFormat.DefaultHDR
            );
            m_Context.offscreenMeshTarget.Create();
            m_Context.offscreenRenderCamera.targetTexture = m_Context.offscreenMeshTarget;
        }

        public void Destroy() {
            m_GSRenderer?.Destroy();
            m_Mesh?.Destroy();
            m_Mesh = null;
            m_GSRenderer = null;
            DestroyBuffers();
        }

        private void DestroyBuffers() {
            m_Context.gpuGSPosData?.Dispose();
            m_Context.gpuGSPosData = null;
            m_Context.gpuGSOtherData?.Dispose();
            m_Context.gpuGSOtherData = null;
            m_Context.gpuGSSHData?.Dispose();
            m_Context.gpuGSSHData = null;
            m_Context.gpuMeshModVertex?.Dispose();
            m_Context.gpuMeshModVertex = null;
            m_Context.gpuMeshBaseVertex?.Dispose();
            m_Context.gpuMeshBaseVertex = null;
            m_Context.gpuForwardLinks?.Dispose();
            m_Context.gpuForwardLinks = null;
            // m_Context.gpuBackwardLinks?.Dispose();
            // m_Context.gpuBackwardLinks = null;
            m_Context.splatCount = 0;
            m_Context.vertexCount = 0;
            m_Context.indexCount = 0;

            if (m_Context.offscreenRenderCamera != null) {
                DestroyImmediate(m_Context.offscreenRenderCamera.gameObject);
                m_Context.offscreenRenderCamera = null;
            }
            DestroyOffscreenTexture();
        }

        private void DestroyOffscreenTexture() {
            if (m_Context != null && m_Context.offscreenMeshTarget != null) {
                m_Context.offscreenMeshTarget.Release();
                DestroyImmediate(m_Context.offscreenMeshTarget);
                m_Context.offscreenMeshTarget = null;
            }
        }

        public void LoadGS(SplatData data) {
            Destroy();
            m_Context.splatData = data;
            CreateBuffers();
        }

        public SplatData GetSplatData() {
            return m_Context.splatData;
        }

        public GSRenderer GetSplatRenderer() {
            return m_GSRenderer;
        }

        public void SetVertexGroup(VertexSelectionGroup group) {
            m_Mesh.m_SelectionGroup = group.Clone();
        }

        public EditableMesh GetMesh() {
            return m_Mesh;
        }

        public MeshGen GetMeshGen() {
            return m_MeshGenerator;
        }

        public LinkGen GetLinkGen() {
            return m_LinkGenerator;
        }

        public ModifierSystem GetModifierSystem() {
            return m_ModifierSystem;
        }

        public void GenerateMesh() {
            if (m_MeshGenerator != null) {
                m_Mesh?.DestroyBuffers();
                m_Mesh = m_MeshGenerator.Generate(ref m_ModifierSystem);
                
                m_Mesh.m_ScaffoldMaterial = m_ScaffoldMaterial;
                m_Mesh.m_SurfaceMaterial = m_SurfaceMaterial;
                m_Mesh.m_ShadowCasterMaterial = m_ShadowCasterMaterial;
                m_Mesh.m_PostProcessMaterial = m_PostProcessMaterial;
                m_Mesh.m_DebugPlane = m_DebugPlane;
                m_Mesh.m_CSEditMesh = m_CSEditMesh;

                m_ModifierSystem.SetMesh(ref m_Mesh);
                GenerateLinks();
            }
        }

        public void GenerateLinks() {
            m_LinkGenerator.GenerateForward();
            m_LinkGenerator.GenerateBackward();
        }

        public void Update() {
            if (m_Context != null && m_GSRenderer != null) {
                m_GSRenderer.m_Transform = transform;
                m_GSRenderer.m_IsActiveAndEnabled = isActiveAndEnabled;
                m_GSRenderer.Update();
            }

            if (m_Context != null && m_Mesh != null && m_GSRenderer != null) {
                if (m_Context.offscreenMeshTarget == null) {
                    CreateOffscreenTexture();
                }

                if (Camera.current != null && m_ScreenSize != Camera.current.pixelRect) {
                    m_ScreenSize = Camera.current.pixelRect;
                    DestroyOffscreenTexture();
                    CreateOffscreenTexture();
                }
            }
        }

        public void OnPreCullCamera(Camera cam){
            if (m_Mesh != null) {
                m_Mesh.m_GlobalTransform = transform;
                m_Mesh.Draw(FindObjectsOfType<Renderer>());
            }
        }
    }
}