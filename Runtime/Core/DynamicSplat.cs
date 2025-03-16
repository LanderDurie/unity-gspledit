using System;
using UnityEditor;
using UnityEngine.Rendering;

namespace UnityEngine.GsplEdit {
    [ExecuteInEditMode, Serializable]
    public class DynamicSplat : MonoBehaviour {
        // Serialized Fields
        // [SerializeField] private GameObject m_DebugPlane;
        [HideInInspector, SerializeField] public Shader m_ScaffoldShader;
        [HideInInspector, SerializeField] public Shader m_SurfaceShader;
        [HideInInspector, SerializeField] public Shader m_ShadowCasterShader;
        [HideInInspector, SerializeField] private float m_ProjectTextureResolutionMultiplier = 1.0f;
        [HideInInspector, SerializeField] private ComputeShader m_CSEditMesh;
        [HideInInspector, SerializeField] public Shader m_ShaderSplats;
        [HideInInspector, SerializeField] public Shader m_ShaderComposite;
        [HideInInspector, SerializeField] public Shader m_ShaderDebugPoints;
        [HideInInspector, SerializeField] public Shader m_ShaderDebugBoxes;
        [HideInInspector, SerializeField] public ComputeShader m_CSSplatUtilities;
        [HideInInspector, SerializeField] public ComputeShader m_CSBufferOps;

        // Serialized CPU data (Keep persistence when switching between editor and run mode)
        [HideInInspector, SerializeField] private SplatData m_SplatData;
        [HideInInspector, SerializeField] private ScaffoldData m_ScaffoldData;
        [HideInInspector, SerializeField] private ModifierData m_ModifierData;

        private SharedComputeContext m_Context;
        private EditableMesh m_EditableMesh;
        private GSRenderer m_GSRenderer;
        private MeshGen m_MeshGenerator;
        private LinkGen m_LinkGenerator;
        private ModifierSystem m_ModifierSystem;
        private Rect m_ScreenSize;

        public void OnEnable() {
            m_Context = new();

#if UNITY_EDITOR
            m_MeshGenerator = new(ref m_Context);
            m_LinkGenerator = new(ref m_Context);
#endif
            if (m_ScaffoldData == null) {
                m_ScaffoldData = ScriptableObject.CreateInstance<ScaffoldData>();
            }

            if (m_ModifierData == null) {
                m_ModifierData = ScriptableObject.CreateInstance<ModifierData>();
            }

            if (m_SplatData != null) {
                m_Context.gsSplatData = m_SplatData;
            }

            m_Context.scaffoldData = m_ScaffoldData;
            m_Context.modifierData = m_ModifierData;

            m_ModifierSystem = new(ref m_Context, m_CSBufferOps);

            CreateBuffers();

            if (m_ScaffoldData != null && m_ScaffoldData.indexCount > 1) {
                m_EditableMesh = new EditableMesh(
                    ref m_Context, 
                    ref m_ModifierSystem,
                    m_ScaffoldShader,
                    m_SurfaceShader,
                    m_ShadowCasterShader,
                    m_CSEditMesh
                );
                m_ModifierSystem.SetMesh(ref m_EditableMesh);
            }

            // Add to camera pass
            if (GraphicsSettings.currentRenderPipeline == null) {
                Camera.onPreCull += OnPreCullCamera;
            }
        }

        public void OnDisable()
        {
            // Remove from camera pass
            Camera.onPreCull -= OnPreCullCamera;
            Destroy();
        }

        public unsafe void CreateBuffers() {
            if (m_Context == null || m_SplatData == null || m_ScaffoldData == null) {
                return;
            }

            // Create splat buffers
            m_Context.gsSplatCount = m_SplatData.splatCount;
            m_Context.gsPosData = new GraphicsBuffer(GraphicsBuffer.Target.Raw | GraphicsBuffer.Target.CopySource, (int)(m_SplatData.posData.dataSize / 4), 4) { name = "GaussianPosData" };
            m_Context.gsPosData.SetData(m_SplatData.posData.GetData<uint>());
            m_Context.gsOtherData = new GraphicsBuffer(GraphicsBuffer.Target.Raw | GraphicsBuffer.Target.CopySource, (int)(m_SplatData.otherData.dataSize / 4), 4) { name = "GaussianOtherData" };
            m_Context.gsOtherData.SetData(m_SplatData.otherData.GetData<uint>());
            m_Context.gsSHData = new GraphicsBuffer(GraphicsBuffer.Target.Raw, (int)(m_SplatData.shData.dataSize / 4), 4) { name = "GaussianSHData" };
            m_Context.gsSHData.SetData(m_SplatData.shData.GetData<uint>());

            // Create scaffold buffers
            m_Context.scaffoldModVertex = new GraphicsBuffer(GraphicsBuffer.Target.Raw | GraphicsBuffer.Target.CopySource, m_Context.scaffoldData.vertexCount, sizeof(Vector3)) { name = "MeshBaseVertices" };
            m_Context.scaffoldModVertex.SetData(m_ScaffoldData.modVertices);
            m_Context.scaffoldBaseVertex = new GraphicsBuffer(GraphicsBuffer.Target.Raw | GraphicsBuffer.Target.CopySource, m_Context.scaffoldData.vertexCount, sizeof(Vector3)) { name = "MeshModVertices" };
            m_Context.scaffoldBaseVertex.SetData(m_ScaffoldData.baseVertices);
            m_Context.scaffoldIndices = new GraphicsBuffer(GraphicsBuffer.Target.Raw | GraphicsBuffer.Target.CopySource, m_Context.scaffoldData.indexCount, sizeof(int)) { name = "MeshIndices" };
            m_Context.scaffoldIndices.SetData(m_ScaffoldData.indices);
            m_Context.scaffoldDeletedBits = new ComputeBuffer((m_Context.scaffoldData.vertexCount + 31) / 32, sizeof(uint));
            m_Context.scaffoldDeletedBits.SetData(m_ScaffoldData.deletedBits);

            // Create textures
            m_Context.splatColorMap = new Texture2D(1000, 1000, TextureFormat.RGBA32, false);
            m_Context.splatNormalMap = new Texture2D(1000, 1000, TextureFormat.RGBA32, false);

            // Create link buffers
            m_Context.forwardLinks = new ComputeBuffer(m_Context.gsSplatData.splatCount, sizeof(ForwardLink));
            m_Context.forwardLinks.SetData(m_ScaffoldData.forwardLinks);

            // Create offscreen targets
            GameObject offscreenCameraObject = new GameObject("offscreenCameraObject");
            offscreenCameraObject.hideFlags = HideFlags.HideAndDontSave;
            m_Context.offscreenCam = offscreenCameraObject.AddComponent<Camera>();
            m_Context.offscreenCam.enabled = false;
            m_Context.offscreenCam.renderingPath = RenderingPath.DeferredShading;
            m_Context.offscreenCam.clearFlags = CameraClearFlags.SolidColor;
            m_Context.offscreenCam.backgroundColor = new Color(0, 0, 0, 0);
            m_Context.offscreenCam.allowMSAA = false;

            CreateOffscreenTexture();
            if (m_Context.offscreenBuffer != null) {
                m_Context.offscreenCam.targetTexture = m_Context.offscreenBuffer;
            }

            if (Camera.current != null) {
                m_ScreenSize = Camera.current.pixelRect;
            }

            m_GSRenderer = GSRenderer.Create(
                transform, 
                isActiveAndEnabled, 
                ref m_Context, 
                m_ShaderSplats,
                m_ShaderComposite,
                m_ShaderDebugPoints,
                m_ShaderDebugBoxes, 
                m_CSSplatUtilities
            );
        }

        public void DestroyBuffers() {
            if (m_Context == null) {
                return;
            }

            // Dispose of GPU buffers
            m_Context.gsPosData?.Dispose();
            m_Context.gsOtherData?.Dispose();
            m_Context.gsSHData?.Dispose();
            m_Context.scaffoldModVertex?.Dispose();
            m_Context.scaffoldBaseVertex?.Dispose();
            m_Context.scaffoldIndices?.Dispose();
            m_Context.forwardLinks?.Dispose();
            m_Context.scaffoldDeletedBits?.Dispose();

            // Clean up offscreen camera
            if (m_Context.offscreenCam != null) {
                DestroyImmediate(m_Context.offscreenCam.gameObject);
                m_Context.offscreenCam = null;
            }

            // Clean up offscreen texture
            DestroyOffscreenTexture();
        }

        public void CreateOffscreenTexture() {
            int width = 1;
            int height = 1;

            if (m_ScreenSize != null && m_ScreenSize.width != 0 && m_ScreenSize.height != 0) {
                width = (int)(m_ScreenSize.width * m_ProjectTextureResolutionMultiplier);
                height = (int)(m_ScreenSize.height * m_ProjectTextureResolutionMultiplier);
            }

            m_Context.offscreenBuffer = new RenderTexture(
                width,
                height,
                24,
                RenderTextureFormat.DefaultHDR
            );
            m_Context.offscreenBuffer.antiAliasing = 1;
            m_Context.offscreenBuffer.Create();

            if (m_Context.offscreenCam != null) {
                m_Context.offscreenCam.targetTexture = m_Context.offscreenBuffer;
            }
        }

        private void DestroyOffscreenTexture() {
            if (m_Context != null && m_Context.offscreenBuffer != null) {
                m_Context.offscreenBuffer.Release();
                DestroyImmediate(m_Context.offscreenBuffer);
                m_Context.offscreenBuffer = null;
            }
        }

        public void LoadGS(SplatData data) {
            Destroy();
            m_SplatData = data; // Update serialized CPU data
            m_Context.gsSplatData = data; // Update context
            CreateBuffers();
            // Recreate modifier system
            m_ModifierSystem.Destroy();
            m_ModifierSystem = new(ref m_Context, m_CSBufferOps);
        }

        public void Destroy() {
            m_GSRenderer?.Destroy();
            m_EditableMesh?.Destroy();
            m_ModifierSystem?.Destroy();
            m_EditableMesh = null;
            m_GSRenderer = null;
            DestroyBuffers();
        }

        public ref SharedComputeContext GetContext() {
            return ref m_Context;
        }

        public GSRenderer GetSplatRenderer() {
            return m_GSRenderer;
        }

        public EditableMesh GetMesh() {
            return m_EditableMesh;
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

#if UNITY_EDITOR
        public void GenerateMesh() {            
            if (m_MeshGenerator != null) {
                m_EditableMesh?.DestroyBuffers();
                // Create mesh on m_Context passed in constructor
                m_MeshGenerator.Generate();
                
                m_EditableMesh = new EditableMesh(
                    ref m_Context, 
                    ref m_ModifierSystem,
                    m_ScaffoldShader,
                    m_SurfaceShader,
                    m_ShadowCasterShader,
                    m_CSEditMesh
                );

                // m_Mesh.m_DebugPlane = m_DebugPlane;
                m_ModifierSystem.SetMesh(ref m_EditableMesh);
                GenerateLinks();

                // Sync GPU Buffers to Serializable struct
                m_ScaffoldData.vertexCount = m_Context.scaffoldData.vertexCount;
                m_ScaffoldData.indexCount = m_Context.scaffoldData.indexCount;

                m_ScaffoldData.modVertices = m_Context.scaffoldData.modVertices;
                m_ScaffoldData.baseVertices = m_Context.scaffoldData.baseVertices;
                m_ScaffoldData.indices = m_Context.scaffoldData.indices;
                m_ScaffoldData.deletedBits = m_Context.scaffoldData.deletedBits;
            }
        }

        public void GenerateLinks() {
            m_LinkGenerator.GenerateForward();
            // m_LinkGenerator.GenerateBackward(); // TODO

            // Sync GPU Buffers to Serializable struct
            m_ScaffoldData.forwardLinks = new ForwardLink[m_Context.forwardLinks.count];
            m_Context.forwardLinks.GetData(m_ScaffoldData.forwardLinks);
        }
#endif

        public void Update() {
            if (m_Context != null && m_GSRenderer != null) {
                m_GSRenderer.m_Transform = transform;
                m_GSRenderer.m_IsActiveAndEnabled = isActiveAndEnabled;
                m_GSRenderer.Update();
            }

            if (m_Context != null && m_EditableMesh != null && m_GSRenderer != null) {

                if (m_Context.offscreenBuffer == null) {
                    CreateOffscreenTexture();
                }

#if UNITY_EDITOR
                // Sync GPU Buffer modifiers to CPU every 100 frames to avoid delay
                if (m_Context.scaffoldModVertex != null) {
                    m_ScaffoldData.modVertices = new Vector3[m_Context.scaffoldModVertex.count];
                    m_Context.scaffoldModVertex.GetData(m_ScaffoldData.modVertices);
                }
                m_ModifierData = m_Context.modifierData;
#endif
            }
        }

        public void OnPreCullCamera(Camera cam) {
            if (Application.isPlaying && cam != SceneView.lastActiveSceneView.camera && cam.allowMSAA) {
                // TODO: fix MSAA bug
                Debug.LogWarning("MSAA currently not supported, disable on camera!");
                return;
            }

            if (m_EditableMesh != null) {
                // Resize offscreen target on window resize
                if (m_ScreenSize != cam.pixelRect) {
                    m_ScreenSize = cam.pixelRect;
                    DestroyOffscreenTexture();
                    CreateOffscreenTexture();
                }

                m_EditableMesh.m_GlobalTransform = transform;
                m_EditableMesh.Draw(FindObjectsOfType<Renderer>(), cam);
            }
        }
    }
}