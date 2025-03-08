using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace UnityEngine.GsplEdit
{
    public class OffscreenRendering
    {
        public GameObject m_DebugPlane;
        private SharedComputeContext m_Context;
        public Renderer[] m_Renderers;
        public Material m_Material;
        public GraphicsBuffer m_IndexBuffer;
        public Transform m_GlobalTransform;
        public ComputeBuffer m_ArgsBuffer;
        public bool m_CastShadow;

        // Store original shadow casting modes
        private System.Collections.Generic.Dictionary<Renderer, UnityEngine.Rendering.ShadowCastingMode> originalShadowModes;
        // Command buffer for unified rendering
        private CommandBuffer m_CommandBuffer;

        public OffscreenRendering(ref SharedComputeContext context) {
            m_Context = context;
            // Initialize the command buffer
            m_CommandBuffer = new CommandBuffer();
            m_CommandBuffer.name = "OffscreenRenderingPass";
            
            Recreate(1.0f);
        }

        private void Recreate(float resolutionMultiplier) {
            if (SceneView.lastActiveSceneView == null)
                return;

            // Apply the RenderTexture to the display plane
            if (m_DebugPlane != null)
            {
                Renderer planeRenderer = m_DebugPlane.GetComponent<Renderer>();
                if (planeRenderer != null)
                {
                    // Ensure the material uses the correct shader
                    if (planeRenderer.sharedMaterial == null)
                        planeRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
                    
                    planeRenderer.sharedMaterial.mainTexture = m_Context.offscreenMeshTarget;
                }
            }
        }

        public void OnEnable()
        {
            #if UNITY_EDITOR
            EditorApplication.update += UpdateRendering;
            #endif

            Recreate(1.0f);
        }

        public void OnDisable()
        {
            #if UNITY_EDITOR
            EditorApplication.update -= UpdateRendering;
            #endif

            RestoreShadowModes(); // Restore original shadow modes
            
            // Clean up command buffer
            if (m_CommandBuffer != null)
            {
                m_CommandBuffer.Release();
                m_CommandBuffer = null;
            }
        }

        private void SyncWithSceneViewCamera()
        {
            #if UNITY_EDITOR
            // Get the current Scene View camera
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null || m_Context.offscreenRenderCamera == null)
                return;

            Camera sceneCamera = sceneView.camera;
            if (sceneCamera == null)
                return;

            // Match the Scene View camera's position and rotation
            m_Context.offscreenRenderCamera.transform.position = sceneCamera.transform.position;
            m_Context.offscreenRenderCamera.transform.rotation = sceneCamera.transform.rotation;

            // Match the Scene View camera's projection settings
            m_Context.offscreenRenderCamera.fieldOfView = sceneCamera.fieldOfView;
            m_Context.offscreenRenderCamera.orthographic = sceneCamera.orthographic;
            m_Context.offscreenRenderCamera.orthographicSize = sceneCamera.orthographicSize;
            m_Context.offscreenRenderCamera.nearClipPlane = sceneCamera.nearClipPlane;
            m_Context.offscreenRenderCamera.farClipPlane = sceneCamera.farClipPlane;
            m_Context.offscreenRenderCamera.allowMSAA = false;
    
            #endif
        }

        public void UpdateRendering()
        {
            SyncWithSceneViewCamera();
            RenderOffscreenTexture();

            #if UNITY_EDITOR
            SceneView.RepaintAll();
            #endif
        }

        public void Render()
        {
            if (m_Context.offscreenRenderCamera == null || m_Context.offscreenMeshTarget == null)
            {
                Recreate(1.0f);
            }
            
            SyncWithSceneViewCamera();
            RenderOffscreenTexture();

            #if UNITY_EDITOR
            SceneView.RepaintAll();
            #endif        
        }

        private void RenderOffscreenTexture()
        {
            if (m_Context.offscreenRenderCamera == null || m_Context.offscreenMeshTarget == null)
            {
                Debug.LogWarning("Cannot render offscreen texture: camera or render target is null");
                return;
            }

            // Explicitly ensure the camera is targeting our render texture
            m_Context.offscreenRenderCamera.targetTexture = m_Context.offscreenMeshTarget;
            
            // Set shadow modes for all renderers
            SetShadowModes();
            
            // Clear the command buffer to build a new rendering sequence
            m_CommandBuffer.Clear();
            
            // Clear the render texture
            m_CommandBuffer.SetRenderTarget(m_Context.offscreenMeshTarget);
            m_CommandBuffer.ClearRenderTarget(true, true, m_Context.offscreenRenderCamera.backgroundColor);
            
            // Add the DrawFill commands directly to the command buffer
            PrepareDrawFillCommands();
            m_Context.offscreenRenderCamera.Render();
            
            // Restore shadow modes
            RestoreShadowModes();
            
            // Ensure the debug plane material has the texture
            if (m_DebugPlane != null)
            {
                Renderer planeRenderer = m_DebugPlane.GetComponent<Renderer>();
                if (planeRenderer != null && planeRenderer.sharedMaterial != null)
                {
                    planeRenderer.sharedMaterial.mainTexture = m_Context.offscreenMeshTarget;
                }
            }
        }

        private void PrepareDrawFillCommands()
        {
            // Set up RenderParams
            RenderParams rp = new RenderParams(m_Material);
            rp.worldBounds = new Bounds(m_GlobalTransform.position, Vector3.one * 10f);
            rp.matProps = new MaterialPropertyBlock();
            rp.matProps.SetBuffer("_MeshVertexPos", m_Context.gpuMeshPosData);
            rp.matProps.SetBuffer("_IndexBuffer", m_IndexBuffer);
            rp.matProps.SetMatrix("_ObjectToWorld", m_GlobalTransform.localToWorldMatrix);
            rp.camera = m_Context.offscreenRenderCamera;
            rp.receiveShadows = true;
            rp.shadowCastingMode = ShadowCastingMode.Off; // Dont cast shadows
            rp.layer = 0;

            Graphics.RenderPrimitives(rp, MeshTopology.Triangles, m_IndexBuffer.count, 1);
        }

        void SetShadowModes()
        {
            originalShadowModes = new System.Collections.Generic.Dictionary<Renderer, UnityEngine.Rendering.ShadowCastingMode>();

            // Store the original shadow mode for each renderer
            foreach (Renderer renderer in m_Renderers)
            {
                if (renderer != null)
                {
                    originalShadowModes[renderer] = renderer.shadowCastingMode;
                    
                    // Set to cast shadows only if needed
                    if (m_CastShadow)
                    {
                        // renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                    }
                }
            }
        }

        void RestoreShadowModes()
        {
            // Restore original shadow modes
            if (originalShadowModes != null)
            {
                foreach (var kvp in originalShadowModes)
                {
                    if (kvp.Key != null)
                    {
                        kvp.Key.shadowCastingMode = kvp.Value;
                    }
                }
            }
        }
    }
}