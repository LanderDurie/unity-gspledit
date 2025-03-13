// using UnityEngine;
// using UnityEditor;
// using UnityEngine.Rendering;
// using System.Collections.Generic;

// namespace UnityEngine.GsplEdit
// {
//     public class OffscreenRendering
//     {
//         public GameObject m_DebugPlane;
//         private SharedComputeContext m_Context;
//         public Material m_SurfaceMaterial;
//         public Mesh m_SurfaceMesh;
//         public Transform m_GlobalTransform;
//         public bool m_ReceiveShadows;
        
//         // New variables for falloff effect
//         public Material m_FalloffMaterial;
//         private RenderTexture m_TempRenderTexture;

//         // Layer used for our surface mesh
//         private const int SURFACE_LAYER = 31;

//         public OffscreenRendering(ref SharedComputeContext context) 
//         {
//             m_Context = context;
//             Recreate();
//         }

//         // private void CreateFalloffResources()
//         // {
//         //     // Create the falloff material if not already created
//         //     if (m_FalloffMaterial == null)
//         //     {
//         //         // Try to find existing shader first
//         //         Shader falloffShader = Shader.Find("Hidden/MeshFalloff");
                
//         //         // If shader doesn't exist, try to create it at runtime (Editor only)
//         //         if (falloffShader == null)
//         //         {
//         //             #if UNITY_EDITOR
//         //             // Try to create the shader asset in the project
//         //             falloffShader = CreateFalloffShaderAsset();
//         //             #endif
                    
//         //             // If we still don't have a shader, use a simple fallback
//         //             if (falloffShader == null)
//         //             {
//         //                 // Use a simple blit shader as fallback
//         //                 falloffShader = Shader.Find("Hidden/Internal-Colored");
                        
//         //                 if (falloffShader == null)
//         //                 {
//         //                     // Last resort - use a standard shader that we know exists
//         //                     falloffShader = Shader.Find("Standard");
                            
//         //                     // Log warning only if we couldn't find any shader
//         //                     if (falloffShader == null)
//         //                     {
//         //                         Debug.LogWarning("Could not find any shader for falloff effect. Edge falloff will be disabled.");
//         //                         return;
//         //                     }
//         //                 }
//         //             }
//         //         }
                
//         //         // Now we should have some kind of shader to use
//         //         m_FalloffMaterial = new Material(falloffShader);
//         //     }
//         // }

//         private void Recreate() 
//         {
//             if (SceneView.lastActiveSceneView == null)
//                 return;
                
//             // Create falloff resources (needs to be called before creating render textures)
//             // CreateFalloffResources();

//             // Create or recreate temporary render texture for post-processing
//             if (m_Context.offscreenMeshTarget != null)
//             {
//                 if (m_TempRenderTexture == null || 
//                     m_TempRenderTexture.width != m_Context.offscreenMeshTarget.width || 
//                     m_TempRenderTexture.height != m_Context.offscreenMeshTarget.height)
//                 {
//                     if (m_TempRenderTexture != null)
//                     {
//                         m_TempRenderTexture.Release();
//                         Object.DestroyImmediate(m_TempRenderTexture);
//                     }
                    
//                     // In the Recreate() method, modify the RenderTexture creation:
//                     m_TempRenderTexture = new RenderTexture(
//                         m_Context.offscreenMeshTarget.width,
//                         m_Context.offscreenMeshTarget.height,
//                         0,
//                         RenderTextureFormat.ARGB32); // Ensure alpha channel support
//                     m_TempRenderTexture.antiAliasing = 1;
//                     m_TempRenderTexture.useMipMap = false;
//                     m_TempRenderTexture.wrapMode = TextureWrapMode.Clamp;
//                     m_TempRenderTexture.filterMode = FilterMode.Bilinear;
//                     m_TempRenderTexture.Create();

//                     // Make sure your offscreenMeshTarget is also using ARGB32 format
//                 }
//             }

//             // Apply the RenderTexture to the display plane
//             if (m_DebugPlane != null && m_DebugPlane.TryGetComponent(out Renderer planeRenderer)) 
//             {
//                 if (planeRenderer.sharedMaterial == null)
//                     planeRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
                
//                 planeRenderer.sharedMaterial.mainTexture = m_Context.offscreenMeshTarget;
//             }
//         }

//         public void OnEnable() 
//         {
//             Recreate();
//         }

//         private void OnDisable()
//         {
//             // Clean up resources
//             if (m_TempRenderTexture != null)
//             {
//                 m_TempRenderTexture.Release();
//                 Object.DestroyImmediate(m_TempRenderTexture);
//                 m_TempRenderTexture = null;
//             }
            
//             if (m_FalloffMaterial != null)
//             {
//                 Object.DestroyImmediate(m_FalloffMaterial);
//                 m_FalloffMaterial = null;
//             }
//         }

//         private void SyncWithSceneViewCamera() 
//         {
//             SceneView sceneView = SceneView.lastActiveSceneView;
//             if (sceneView == null || m_Context.offscreenRenderCamera == null)
//                 return;

//             Camera sceneCamera = sceneView.camera;
//             if (sceneCamera == null)
//                 return;

//             // Sync the offscreen camera with the Scene View camera
//             m_Context.offscreenRenderCamera.transform.SetPositionAndRotation(
//                 sceneCamera.transform.position,
//                 sceneCamera.transform.rotation
//             );

//             m_Context.offscreenRenderCamera.fieldOfView = sceneCamera.fieldOfView;
//             m_Context.offscreenRenderCamera.orthographic = sceneCamera.orthographic;
//             m_Context.offscreenRenderCamera.orthographicSize = sceneCamera.orthographicSize;
//             m_Context.offscreenRenderCamera.nearClipPlane = sceneCamera.nearClipPlane;
//             m_Context.offscreenRenderCamera.farClipPlane = sceneCamera.farClipPlane;
//             m_Context.offscreenRenderCamera.allowMSAA = false;
//         }

//         public void Render(Renderer[] renderers) 
//         {
//             if (m_Context.offscreenRenderCamera == null || m_Context.offscreenMeshTarget == null) 
//             {
//                 Recreate();
                
//                 // If still null after recreate, skip rendering
//                 if (m_Context.offscreenRenderCamera == null || m_Context.offscreenMeshTarget == null)
//                     return;
//             }

//             SyncWithSceneViewCamera();
//             RenderOffscreenTexture(renderers);
//         }

//         private void RenderOffscreenTexture(Renderer[] renderers) 
//         {
//             if (m_Context.offscreenRenderCamera == null || m_Context.offscreenMeshTarget == null) 
//             {
//                 Debug.LogWarning("Cannot render offscreen texture: camera or render target is null");
//                 return;
//             }

//             // Check if temp render texture exists and has the correct size
//             if (m_TempRenderTexture == null || 
//                 m_TempRenderTexture.width != m_Context.offscreenMeshTarget.width || 
//                 m_TempRenderTexture.height != m_Context.offscreenMeshTarget.height)
//             {
//                 Recreate();
                
//                 // If still null after recreate, skip post-processing
//                 if (m_TempRenderTexture == null)
//                 {
//                     // Render directly to main target without post-processing
//                     RenderWithoutPostProcess(renderers);
//                     return;
//                 }
//             }

//             // Clear both render textures before use
//             RenderTexture.active = m_TempRenderTexture;
//             GL.Clear(true, true, new Color(0, 0, 0, 0));
//             RenderTexture.active = m_Context.offscreenMeshTarget;
//             GL.Clear(true, true, new Color(0, 0, 0, 0));
//             RenderTexture.active = null;
            
//             // Set camera background color to fully transparent
//             Color originalBgColor = m_Context.offscreenRenderCamera.backgroundColor;
//             m_Context.offscreenRenderCamera.backgroundColor = new Color(0, 0, 0, 0);
//             m_Context.offscreenRenderCamera.clearFlags = CameraClearFlags.SolidColor;

//             // Save original camera settings
//             int originalCullingMask = m_Context.offscreenRenderCamera.cullingMask;
//             RenderTexture originalTarget = m_Context.offscreenRenderCamera.targetTexture;
            
//             // Set the render target to our temporary texture first
//             m_Context.offscreenRenderCamera.targetTexture = m_TempRenderTexture;
            
//             // Create a replacement shader for shadow-only rendering
//             Shader replacementShader = null;
//             string replacementTag = null;
            
//             if (m_ReceiveShadows)
//             {
//                 // Only draw our surface mesh normally and everything else as shadows
//                 replacementShader = Shader.Find("Custom/ShadowsOnly");
//                 replacementTag = "RenderType";
                
//                 // If the custom shader doesn't exist, use this fallback approach
//                 if (replacementShader == null)
//                 {
//                     // Save all renderer states
//                     Dictionary<Renderer, ShadowCastingMode> originalShadowModes = new Dictionary<Renderer, ShadowCastingMode>();
                    
//                     // Set all renderers to shadows only mode except our surface
//                     foreach (Renderer renderer in renderers)
//                     {
//                         if (renderer != null)
//                         {
//                             originalShadowModes[renderer] = renderer.shadowCastingMode;
//                             renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
//                         }
//                     }
                    
//                     // Set the camera to see everything
//                     m_Context.offscreenRenderCamera.cullingMask = -1;
//                     DrawSurface();
//                     // Render the scene
//                     m_Context.offscreenRenderCamera.Render();
                    
//                     // Restore all shadow modes
//                     foreach (var kvp in originalShadowModes)
//                     {
//                         if (kvp.Key != null)
//                         {
//                             kvp.Key.shadowCastingMode = kvp.Value;
//                         }
//                     }
//                 }
//                 else
//                 {
//                     // If we have the replacement shader, use it
//                     m_Context.offscreenRenderCamera.cullingMask = -1;
//                     m_Context.offscreenRenderCamera.RenderWithShader(replacementShader, replacementTag);
//                 }
//             }
//             else 
//             {
//                 // When not receiving shadows, only render our surface layer
//                 m_Context.offscreenRenderCamera.cullingMask = 1 << SURFACE_LAYER;
//                 m_Context.offscreenRenderCamera.Render();
//             }
            
//             // Restore camera settings
//             m_Context.offscreenRenderCamera.cullingMask = originalCullingMask;
//             m_Context.offscreenRenderCamera.targetTexture = originalTarget;
//             m_Context.offscreenRenderCamera.backgroundColor = originalBgColor;
            
//             // Apply falloff post-processing effect
//             ApplyFalloffEffect();
            
//             // Update the debug plane texture
//             if (m_DebugPlane != null && m_DebugPlane.TryGetComponent(out Renderer planeRenderer)) 
//             {
//                 planeRenderer.sharedMaterial.mainTexture = m_Context.offscreenMeshTarget;
//             }
//         }

//         // Fallback rendering without post-processing
//         private void RenderWithoutPostProcess(Renderer[] renderers)
//         {
//             // Save original camera settings
//             int originalCullingMask = m_Context.offscreenRenderCamera.cullingMask;
            
//             // Set the render target directly to the final texture
//             m_Context.offscreenRenderCamera.targetTexture = m_Context.offscreenMeshTarget;
            
//             if (m_ReceiveShadows)
//             {
//                 // Save all renderer states
//                 Dictionary<Renderer, ShadowCastingMode> originalShadowModes = new Dictionary<Renderer, ShadowCastingMode>();
                
//                 // Set all renderers to shadows only mode except our surface
//                 foreach (Renderer renderer in renderers)
//                 {
//                     if (renderer != null)
//                     {
//                         originalShadowModes[renderer] = renderer.shadowCastingMode;
//                         renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
//                     }
//                 }
                
//                 // Set the camera to see everything
//                 m_Context.offscreenRenderCamera.cullingMask = -1;
//                 DrawSurface();
//                 // Render the scene
//                 m_Context.offscreenRenderCamera.Render();
                
//                 // Restore all shadow modes
//                 foreach (var kvp in originalShadowModes)
//                 {
//                     if (kvp.Key != null)
//                     {
//                         kvp.Key.shadowCastingMode = kvp.Value;
//                     }
//                 }
//             }
//             else 
//             {
//                 // When not receiving shadows, only render our surface layer
//                 m_Context.offscreenRenderCamera.cullingMask = 1 << SURFACE_LAYER;
//                 m_Context.offscreenRenderCamera.Render();
//             }
            
//             // Restore camera settings
//             m_Context.offscreenRenderCamera.cullingMask = originalCullingMask;
            
//             // Update the debug plane texture
//             if (m_DebugPlane != null && m_DebugPlane.TryGetComponent(out Renderer planeRenderer)) 
//             {
//                 planeRenderer.sharedMaterial.mainTexture = m_Context.offscreenMeshTarget;
//             }
//         }

// private void ApplyFalloffEffect()
// {
//     // Skip if falloff material or textures are missing
//     if (m_FalloffMaterial == null || m_TempRenderTexture == null || m_Context.offscreenMeshTarget == null)
//     {
//         // Fallback to direct copy without edge falloff
//         if (m_TempRenderTexture != null && m_Context.offscreenMeshTarget != null)
//         {
//             // Clear destination first
//             RenderTexture.active = m_Context.offscreenMeshTarget;
//             GL.Clear(true, true, new Color(0, 0, 0, 0));
//             RenderTexture.active = null;
            
//             Graphics.Blit(m_TempRenderTexture, m_Context.offscreenMeshTarget);
//         }
//         return;
//     }

//     try
//     {
//         // Clear the destination texture
//         RenderTexture.active = m_Context.offscreenMeshTarget;
//         GL.Clear(true, true, new Color(0, 0, 0, 0));
//         RenderTexture.active = null;
        
//         // Render with the falloff shader to the final target
//         Graphics.Blit(m_TempRenderTexture, m_Context.offscreenMeshTarget, m_FalloffMaterial);
        
//         // Important: release active render texture
//         RenderTexture.active = null;
//     }
//     catch (System.Exception e)
//     {
//         // If shader is incompatible, just do a straight blit
//         Debug.LogWarning("Error applying falloff effect: " + e.Message);
        
//         // Clear the destination texture
//         RenderTexture.active = m_Context.offscreenMeshTarget;
//         GL.Clear(true, true, new Color(0, 0, 0, 0));
//         RenderTexture.active = null;
        
//         Graphics.Blit(m_TempRenderTexture, m_Context.offscreenMeshTarget);
//         RenderTexture.active = null;
//     }
// }

//         private void DrawSurface() {
//             if (m_SurfaceMesh == null || m_SurfaceMaterial == null) {
//                 Debug.LogWarning("Surface mesh or material is not assigned.");
//                 return;
//             }

//             // Draw the surface mesh
//             Graphics.DrawMesh(
//                 m_SurfaceMesh,
//                 m_GlobalTransform.localToWorldMatrix,
//                 m_SurfaceMaterial,
//                 layer: 0,
//                 camera: m_Context.offscreenRenderCamera,
//                 submeshIndex: 0,
//                 null,
//                 castShadows: false,
//                 receiveShadows: m_ReceiveShadows
//             );
//         }
        
//         // Create a shadows-only shader if it doesn't exist
//         private Shader CreateShadowsOnlyShader()
//         {
//             const string shaderCode = @"
// Shader ""Custom/ShadowsOnly"" {
//     SubShader {
//         Tags { ""RenderType""=""Opaque"" }
//         Pass {
//             ColorMask 0
//         }
//     }
    
//     // Special case for our surface mesh
//     SubShader {
//         Tags { ""RenderType""=""Surface"" }
//         Pass {
//             // Normal rendering for surface
//         }
//     }
// }
// ";
//             // In a real implementation, you would create the shader asset
//             // For now, we'll rely on the manual shadow mode approach
//             return null;
//         }

//         // #if UNITY_EDITOR
//         // // Create falloff shader asset in the project
//         // private Shader CreateFalloffShaderAsset()
//         // {
//         //     try
//         //     {
//         //         // Check if shader already exists in project
//         //         string[] assets = AssetDatabase.FindAssets("t:Shader MeshFalloff");
//         //         if (assets != null && assets.Length > 0)
//         //         {
//         //             string assetPath = AssetDatabase.GUIDToAssetPath(assets[0]);
//         //             return AssetDatabase.LoadAssetAtPath<Shader>(assetPath);
//         //         }
                
//         //         // Create a new shader asset
//         //         string shaderPath = "Assets/Shaders/MeshFalloff.shader";
                
//         //         // Make sure the directory exists
//         //         string directory = System.IO.Path.GetDirectoryName(shaderPath);
//         //         if (!System.IO.Directory.Exists(directory))
//         //         {
//         //             System.IO.Directory.CreateDirectory(directory);
//         //         }
                
//         //         // Write the shader to disk
//         //         System.IO.File.WriteAllText(shaderPath, FALLOFF_SHADER);
//         //         AssetDatabase.Refresh();
                
//         //         // Load and return the created shader
//         //         return AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
//         //     }
//         //     catch (System.Exception e)
//         //     {
//         //         Debug.LogError("Failed to create falloff shader asset: " + e.Message);
//         //         return null;
//         //     }
//         // }
//         // #endif
//     }
// }