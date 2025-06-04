using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace UnityEngine.GsplEdit
{
    public class OffscreenRendering
    {
        public GameObject m_DebugPlane;
        private SharedComputeContext m_Context;
        public Material m_SurfaceMaterial;
        public Mesh m_SurfaceMesh;
        public Texture2D m_SurfaceColorTex;
        public Transform m_GlobalTransform;
        public bool m_ReceiveShadows;
        public EditableMesh.ModifierMode m_RenderMode;
        
        // New variables for falloff effect
        public Material m_FalloffMaterial;
        private RenderTexture m_TempRenderTexture;

        // Layer used for our surface mesh
        private const int SURFACE_LAYER = 31;

        public OffscreenRendering(ref SharedComputeContext context) 
        {
            m_Context = context;
            Recreate();
        }

        private void Recreate() 
        {
            if (SceneView.lastActiveSceneView == null)
                return;
                
            // Create falloff resources (needs to be called before creating render textures)
            // CreateFalloffResources();

            // Create or recreate temporary render texture for post-processing
            if (m_Context.offscreenBuffer != null)
            {
                if (m_TempRenderTexture == null || 
                    m_TempRenderTexture.width != m_Context.offscreenBuffer.width || 
                    m_TempRenderTexture.height != m_Context.offscreenBuffer.height)
                {
                    if (m_TempRenderTexture != null)
                    {
                        m_TempRenderTexture.Release();
                        Object.DestroyImmediate(m_TempRenderTexture);
                    }
                    
                    // In the Recreate() method, modify the RenderTexture creation:
                    m_TempRenderTexture = new RenderTexture(
                        m_Context.offscreenBuffer.width,
                        m_Context.offscreenBuffer.height,
                        0,
                        RenderTextureFormat.ARGB32); // Ensure alpha channel support
                    m_TempRenderTexture.antiAliasing = 1;
                    m_TempRenderTexture.useMipMap = false;
                    m_TempRenderTexture.wrapMode = TextureWrapMode.Clamp;
                    m_TempRenderTexture.filterMode = FilterMode.Bilinear;
                    m_TempRenderTexture.Create();

                    // Make sure your offscreenMeshTarget is also using ARGB32 format
                }
            }

            // Apply the RenderTexture to the display plane
            if (m_DebugPlane != null && m_DebugPlane.TryGetComponent(out Renderer planeRenderer)) 
            {
                if (planeRenderer.sharedMaterial == null)
                    planeRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
                
                planeRenderer.sharedMaterial.mainTexture = m_Context.offscreenBuffer;
            }
        }

        public void OnEnable() 
        {
            Recreate();
        }

        private void OnDisable()
        {
            // Clean up resources
            if (m_TempRenderTexture != null)
            {
                m_TempRenderTexture.Release();
                Object.DestroyImmediate(m_TempRenderTexture);
                m_TempRenderTexture = null;
            }
            
            if (m_FalloffMaterial != null)
            {
                Object.DestroyImmediate(m_FalloffMaterial);
                m_FalloffMaterial = null;
            }
        }

        private void SyncWithSceneViewCamera(Camera cam) 
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null || m_Context.offscreenCam == null)
                return;

            if (cam == null)
                return;

            // Sync the offscreen camera with the Scene View camera
            m_Context.offscreenCam.transform.SetPositionAndRotation(
                cam.transform.position,
                cam.transform.rotation
            );

            m_Context.offscreenCam.fieldOfView = cam.fieldOfView;
            m_Context.offscreenCam.orthographic = cam.orthographic;
            m_Context.offscreenCam.orthographicSize = cam.orthographicSize;
            m_Context.offscreenCam.nearClipPlane = cam.nearClipPlane;
            m_Context.offscreenCam.farClipPlane = cam.farClipPlane;
        }

        public void Render(Renderer[] renderers, Camera cam) 
        {
            if (m_Context.offscreenCam == null || m_Context.offscreenBuffer == null) 
            {
                Recreate();
                
                // If still null after recreate, skip rendering
                if (m_Context.offscreenCam == null || m_Context.offscreenBuffer == null)
                    return;
            }

            SyncWithSceneViewCamera(cam);
            RenderOffscreenTexture(renderers);
        }

        private void RenderOffscreenTexture(Renderer[] renderers) 
        {
            if (m_Context.offscreenCam == null || m_Context.offscreenBuffer == null) 
            {
                Debug.LogWarning("Cannot render offscreen texture: camera or render target is null");
                return;
            }

            // Check if temp render texture exists and has the correct size
            if (m_TempRenderTexture == null || 
                m_TempRenderTexture.width != m_Context.offscreenBuffer.width || 
                m_TempRenderTexture.height != m_Context.offscreenBuffer.height)
            {
                Recreate();
                
                // If still null after recreate, skip post-processing
                if (m_TempRenderTexture == null)
                {
                    // Render directly to main target without post-processing
                    RenderWithoutPostProcess(renderers);
                    return;
                }
            }

            // Clear both render textures before use
            RenderTexture.active = m_TempRenderTexture;
            GL.Clear(true, true, new Color(1, 1, 1, 1));
            RenderTexture.active = m_Context.offscreenBuffer;
            GL.Clear(true, true, new Color(1, 1, 1, 1));
            RenderTexture.active = null;
            
            // Set camera background color to fully transparent
            Color originalBgColor = m_Context.offscreenCam.backgroundColor;
            m_Context.offscreenCam.backgroundColor = new Color(1, 1, 1, 1);
            m_Context.offscreenCam.clearFlags = CameraClearFlags.SolidColor;

            // Save original camera settings
            int originalCullingMask = m_Context.offscreenCam.cullingMask;
            RenderTexture originalTarget = m_Context.offscreenCam.targetTexture;
            
            // Set the render target to our temporary texture first
            m_Context.offscreenCam.targetTexture = m_TempRenderTexture;
            
            // Create a replacement shader for shadow-only rendering
            Shader replacementShader = null;
            string replacementTag = null;
            
            if (m_ReceiveShadows)
            {
                // Only draw our surface mesh normally and everything else as shadows
                replacementShader = Shader.Find("Custom/ShadowsOnly");
                replacementTag = "RenderType";
                
                // If the custom shader doesn't exist, use this fallback approach
                if (replacementShader == null)
                {
                    // Save all renderer states
                    Dictionary<Renderer, ShadowCastingMode> originalShadowModes = new Dictionary<Renderer, ShadowCastingMode>();
                    
                    // Set all renderers to shadows only mode except our surface
                    foreach (Renderer renderer in renderers)
                    {
                        if (renderer != null)
                        {
                            originalShadowModes[renderer] = renderer.shadowCastingMode;
                            renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                        }
                    }
                    
                    // Set the camera to see everything
                    m_Context.offscreenCam.cullingMask = -1;
                    DrawSurface();
                    // Render the scene
                    m_Context.offscreenCam.Render();
                    
                    // Restore all shadow modes
                    foreach (var kvp in originalShadowModes)
                    {
                        if (kvp.Key != null)
                        {
                            kvp.Key.shadowCastingMode = kvp.Value;
                        }
                    }
                }
                else
                {
                    // If we have the replacement shader, use it
                    m_Context.offscreenCam.cullingMask = -1;
                    m_Context.offscreenCam.RenderWithShader(replacementShader, replacementTag);
                }
            }
            else 
            {
                // When not receiving shadows, only render our surface layer
                m_Context.offscreenCam.cullingMask = 1 << SURFACE_LAYER;
                m_Context.offscreenCam.Render();
            }
            
            // Restore camera settings
            m_Context.offscreenCam.cullingMask = originalCullingMask;
            m_Context.offscreenCam.targetTexture = originalTarget;
            m_Context.offscreenCam.backgroundColor = originalBgColor;
            
            // Apply falloff post-processing effect
            ApplyFalloffEffect();
            
            // Update the debug plane texture
            if (m_DebugPlane != null && m_DebugPlane.TryGetComponent(out Renderer planeRenderer)) 
            {
                planeRenderer.sharedMaterial.mainTexture = m_Context.offscreenBuffer;
            }
        }

        // Fallback rendering without post-processing
        private void RenderWithoutPostProcess(Renderer[] renderers)
        {
            // Save original camera settings
            int originalCullingMask = m_Context.offscreenCam.cullingMask;
            
            // Set the render target directly to the final texture
            m_Context.offscreenCam.targetTexture = m_Context.offscreenBuffer;
            
            if (m_ReceiveShadows)
            {
                // Save all renderer states
                Dictionary<Renderer, ShadowCastingMode> originalShadowModes = new Dictionary<Renderer, ShadowCastingMode>();
                
                // Set all renderers to shadows only mode except our surface
                foreach (Renderer renderer in renderers)
                {
                    if (renderer != null)
                    {
                        originalShadowModes[renderer] = renderer.shadowCastingMode;
                        renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                    }
                }
                
                // Set the camera to see everything
                m_Context.offscreenCam.cullingMask = -1;
                DrawSurface();
                m_Context.offscreenCam.Render();
                
                // Restore all shadow modes
                foreach (var kvp in originalShadowModes)
                {
                    if (kvp.Key != null)
                    {
                        kvp.Key.shadowCastingMode = kvp.Value;
                    }
                }
            }
            else 
            {
                // When not receiving shadows, only render our surface layer
                m_Context.offscreenCam.cullingMask = 1 << SURFACE_LAYER;
                m_Context.offscreenCam.Render();
            }
            
            // Restore camera settings
            m_Context.offscreenCam.cullingMask = originalCullingMask;
            
            // Update the debug plane texture
            if (m_DebugPlane != null && m_DebugPlane.TryGetComponent(out Renderer planeRenderer)) 
            {
                planeRenderer.sharedMaterial.mainTexture = m_Context.offscreenBuffer;
            }
        }

        private void ApplyFalloffEffect()
        {
            // Skip if falloff material or textures are missing
            if (m_FalloffMaterial == null || m_TempRenderTexture == null || m_Context.offscreenBuffer == null)
            {
                // Fallback to direct copy without edge falloff
                if (m_TempRenderTexture != null && m_Context.offscreenBuffer != null)
                {
                    // Clear destination first
                    RenderTexture.active = m_Context.offscreenBuffer;
                    GL.Clear(true, true, new Color(0, 0, 0, 0));
                    RenderTexture.active = null;
                    
                    Graphics.Blit(m_TempRenderTexture, m_Context.offscreenBuffer);
                }
                return;
            }

            try
            {
                // Clear the destination texture
                RenderTexture.active = m_Context.offscreenBuffer;
                GL.Clear(true, true, new Color(0, 0, 0, 0));
                RenderTexture.active = null;
                
                // Render with the falloff shader to the final target
                Graphics.Blit(m_TempRenderTexture, m_Context.offscreenBuffer, m_FalloffMaterial);
                
                // Important: release active render texture
                RenderTexture.active = null;
            }
            catch (System.Exception e)
            {
                // If shader is incompatible, just do a straight blit
                Debug.LogWarning("Error applying falloff effect: " + e.Message);
                
                // Clear the destination texture
                RenderTexture.active = m_Context.offscreenBuffer;
                GL.Clear(true, true, new Color(0, 0, 0, 0));
                RenderTexture.active = null;
                
                Graphics.Blit(m_TempRenderTexture, m_Context.offscreenBuffer);
                RenderTexture.active = null;
            }
        }

        private void DrawSurface() {
            if (m_SurfaceMesh == null || m_SurfaceMaterial == null) {
                Debug.LogWarning("Surface mesh or material is not assigned.");
                return;
            }

            if (m_Context.backwardNormalTex != null) {
                m_SurfaceMaterial.SetTexture("_NormalTex", m_Context.backwardNormalTex);
            }

            if (m_Context.backwardDepthTex != null) {
                m_SurfaceMaterial.SetTexture("_DepthTex", m_Context.backwardDepthTex);
            }

            m_SurfaceMaterial.SetInt("_RenderMode", (int)m_RenderMode);

            // Draw the surface mesh
            Graphics.DrawMesh(
                m_SurfaceMesh,
                m_GlobalTransform.localToWorldMatrix,
                m_SurfaceMaterial,
                layer: 0,
                camera: m_Context.offscreenCam,
                submeshIndex: 0,
                null,
                castShadows: false,
                receiveShadows: m_ReceiveShadows
            );
        }
    }
}