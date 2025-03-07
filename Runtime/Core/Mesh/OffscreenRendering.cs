using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace UnityEngine.GsplEdit
{
    public class OffscreenRendering
    {
        private GameObject m_DebugPlane;
        private SharedComputeContext m_Context;
        public Renderer[] m_Renderers;
        public Material m_Material;
        public ComputeBuffer m_IndexBuffer;
        public Transform m_GlobalTransform;
        public ComputeBuffer m_ArgsBuffer;
        public bool m_CastShadow;

        // Store original shadow casting modes
        private System.Collections.Generic.Dictionary<Renderer, UnityEngine.Rendering.ShadowCastingMode> originalShadowModes;

        public OffscreenRendering(ref SharedComputeContext context, GameObject debugPlane) {
            m_Context = context;
            m_DebugPlane = debugPlane;
            
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

            CleanupResources();
            RestoreShadowModes(); // Restore original shadow modes
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
            
            // Clear the render texture first
            RenderTexture.active = m_Context.offscreenMeshTarget;
            GL.Clear(true, true, m_Context.offscreenRenderCamera.backgroundColor);
            RenderTexture.active = null;
            
            SetShadowModes();
            DrawFill();
            m_Context.offscreenRenderCamera.Render();
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

        private void DrawFill()
        {
            if (m_Context == null || m_Context.gpuMeshPosData == null || m_Material == null || m_IndexBuffer == null || m_ArgsBuffer == null)
            {
                Debug.LogWarning("Cannot draw fill: one or more required resources are null");
                return;
            }

            // Set up material properties
            m_Material.SetBuffer("_MeshVertexPos", m_Context.gpuMeshPosData);
            m_Material.SetBuffer("_IndexBuffer", m_IndexBuffer);
            m_Material.SetMatrix("_ObjectToWorld", m_GlobalTransform != null ? m_GlobalTransform.localToWorldMatrix : Matrix4x4.identity);

            // Define bounds for the procedural drawing - make it large enough to ensure visibility
            Bounds bounds = new Bounds(
                m_GlobalTransform != null ? m_GlobalTransform.position : Vector3.zero, 
                Vector3.one * 100f
            );
            
            // Draw the procedural mesh directly to the render texture
            Graphics.DrawProceduralIndirect(
                m_Material,
                bounds,
                MeshTopology.Triangles,
                m_ArgsBuffer,
                argsOffset: 0,
                camera: m_Context.offscreenRenderCamera,  // Explicitly specify the camera
                properties: null,
                castShadows: m_CastShadow ? ShadowCastingMode.On : ShadowCastingMode.Off,
                receiveShadows: true,  // Allow shadows to be received
                layer: 0
            );
        }

    void SetShadowModes()
    {
        originalShadowModes = new System.Collections.Generic.Dictionary<Renderer, UnityEngine.Rendering.ShadowCastingMode>();

        // Find all renderers in the scene
        foreach (Renderer renderer in m_Renderers)
        {
            // Store the original shadow mode
            originalShadowModes[renderer] = renderer.shadowCastingMode;

            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            
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

        void CleanupResources()
        {
            // if (m_Context.offscreenRenderCamera != null)
            // {
            //     Object.DestroyImmediate(m_Context.offscreenRenderCamera.gameObject);
            //     tempRenderCamera = null;
            // }

            // if (m_Context != null && m_Context.offscreenMeshTarget != null)
            // {
            //     m_Context.offscreenMeshTarget.Release();
            //     Object.DestroyImmediate(m_Context.offscreenMeshTarget);
            //     m_Context.offscreenMeshTarget = null;
            // }
        }
    }
}