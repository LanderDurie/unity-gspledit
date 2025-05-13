using System.Linq;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.IO;

namespace UnityEngine.GsplEdit
{
    public class TextureGen : LinkGenBackwardBase
    {
        [System.Serializable]
        public class Settings
        {
            // public float cameraDistance = 1.0f;
            public int textureSize = 4096;
            public int triangleTextureSize = 256;
        }

        public Settings m_Settings = new();
        public ComputeShader m_TextureLinkageCompute;

        public unsafe override void Generate(SharedComputeContext context)
        {
            // // Modified CPU-side code
            // // Create render texture instead of Texture2D for UAV support
            // RenderTexture distances = new RenderTexture(context.scaffoldData.indexCount/3/10, 100, 0, RenderTextureFormat.ARGBFloat);
            // distances.enableRandomWrite = true;  // Critical for UAV access
            // distances.Create();
            // // Clear to black (0)
            // RenderTexture.active = distances;
            // GL.Clear(true, true, Color.black);
            // RenderTexture.active = null;

            // // Need to create a buffer to store average triangle size
            // ComputeBuffer avgTriangleSizeBuffer = new ComputeBuffer(1, sizeof(float));
            // float[] avgSizeData = new float[1] { 0.0f };
            // avgTriangleSizeBuffer.SetData(avgSizeData);

            // // Set compute shader parameters
            // m_TextureLinkageCompute.SetTexture(0, "_Distances", distances);
            // m_TextureLinkageCompute.SetBuffer(0, "_SplatPos", context.gsPosData);
            // m_TextureLinkageCompute.SetBuffer(0, "_SplatOther", context.gsOtherData);
            // m_TextureLinkageCompute.SetBuffer(0, "_SplatSH", context.gsSHData);
            // m_TextureLinkageCompute.SetBuffer(0, "_SplatChunks", context.gsChunks);
            // m_TextureLinkageCompute.SetInt("_SplatChunkCount", context.gsChunksValid ? context.gsChunks.count : 0);
            // m_TextureLinkageCompute.SetBuffer(0, "_SplatColor", context.gsSHData);
            // uint format = (uint)context.gsSplatData.posFormat | ((uint)context.gsSplatData.scaleFormat << 8) | ((uint)context.gsSplatData.shFormat << 16);
            // m_TextureLinkageCompute.SetInt("_SplatFormat", (int)format);
            // m_TextureLinkageCompute.SetTexture(0, "_SplatColor", context.gsColorData);
            // m_TextureLinkageCompute.SetBuffer(0, "_VertexBasePos", context.scaffoldBaseVertex);
            // m_TextureLinkageCompute.SetBuffer(0, "_MeshIndices", context.scaffoldIndices);
            // m_TextureLinkageCompute.SetInt("_SplatCount", context.gsSplatData.splatCount);
            // m_TextureLinkageCompute.SetInt("_VertexCount", context.scaffoldData.vertexCount);
            // m_TextureLinkageCompute.SetInt("_IndexCount", context.scaffoldData.indexCount/10);
            // m_TextureLinkageCompute.SetBuffer(0, "_AvgTriangleSizeBuffer", avgTriangleSizeBuffer);

            // // Calculate number of thread groups needed
            // int numThreadGroups = Mathf.CeilToInt((float)context.gsSplatData.splatCount / m_Settings.threadsPerGroup);

            // // Dispatch compute shader
            // m_TextureLinkageCompute.Dispatch(0, numThreadGroups, 1, 1);

            // // Read back average triangle size
            // avgTriangleSizeBuffer.GetData(avgSizeData);
            // float avgTriangleSize = avgSizeData[0];
            // avgTriangleSizeBuffer.Release(); // Important: release the buffer when done

            // // Read back results from texture
            // Texture2D readbackTex = new Texture2D(distances.width, distances.height, TextureFormat.RGBAFloat, false);
            // RenderTexture.active = distances;
            // readbackTex.ReadPixels(new Rect(0, 0, distances.width, distances.height), 0, 0);
            // RenderTexture.active = null;
            // readbackTex.Apply();

            // // Now avgTriangleSize contains the computed average triangle size
            // // You can use it as needed
            // Debug.Log("Average Triangle Size: " + avgTriangleSize);

            // // Don't forget to clean up
            // distances.Release();

            // WriteDistancesToJson(readbackTex, avgTriangleSize, context);


            Mesh mesh = context.scaffoldMesh;

            // Create target texture
            Texture2D generatedTexture = new Texture2D(m_Settings.textureSize, m_Settings.textureSize, TextureFormat.RGBA32, false);
            Color[] clearPixels = Enumerable.Repeat(Color.clear, m_Settings.textureSize * m_Settings.textureSize).ToArray();
            generatedTexture.SetPixels(clearPixels);
            generatedTexture.Apply();

            // Create a single RenderTexture to reuse
            RenderTexture renderTexture = new RenderTexture(m_Settings.triangleTextureSize, m_Settings.triangleTextureSize, 0, RenderTextureFormat.ARGBFloat);
            renderTexture.antiAliasing = 1;
            renderTexture.Create();

            // Create a camera GameObject
            GameObject cameraObj = new GameObject("FaceCamera");
            Camera faceCamera = cameraObj.AddComponent<Camera>();

            // Configure the camera
            faceCamera.orthographic = true;
            faceCamera.orthographicSize = 1.0f; // Set an appropriate orthographic size
            faceCamera.clearFlags = CameraClearFlags.SolidColor;
            faceCamera.backgroundColor = new Color(0, 0, 0, 0); // Transparent background
            faceCamera.nearClipPlane = 1 - 0.07f;
            faceCamera.farClipPlane = 1 + 0.3f;// + 0.3f; // Ensure this is a valid value
            faceCamera.enabled = false; // Disable the camera since we're rendering manually
            faceCamera.allowMSAA = false; // Disable MSAA for better control
            faceCamera.targetTexture = renderTexture; // Assign the render texture

            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            Vector2[] uvs = mesh.uv;
            Vector3[] normals = mesh.normals;

            // Create a temporary texture for reading render result
            Texture2D tempTexture = new Texture2D(m_Settings.triangleTextureSize, m_Settings.triangleTextureSize, TextureFormat.RGBAFloat, false);

            // Timing variables
            // Stopwatch stopwatch1 = new Stopwatch();
            // Stopwatch stopwatch2 = new Stopwatch();
            // long totalTriangleTime = 0;
            // int count = 0;

            GSRenderSystem.instance.m_ForceDepth = true;

            for (int i = 0; i < triangles.Length; i += 3)
            {   
                // count++;
                // stopwatch1.Restart();

                int vertA = triangles[i];
                int vertB = triangles[i + 1];
                int vertC = triangles[i + 2];

                // Get world positions
                Vector3 worldA = vertices[vertA];
                Vector3 worldB = vertices[vertB];
                Vector3 worldC = vertices[vertC];

                GSRenderSystem.instance.m_TriangleProj[0] = worldA;
                GSRenderSystem.instance.m_TriangleProj[1] = worldB;
                GSRenderSystem.instance.m_TriangleProj[2] = worldC;
                GSRenderSystem.instance.m_TriangleProj[3] = normals[i];
                GSRenderSystem.instance.m_TriangleProj[4] = normals[i+1];
                GSRenderSystem.instance.m_TriangleProj[5] = normals[i+2];

                // Setup camera
                // Vector3 cameraPos, faceNormal;
                // float orthoSize;
                SetCamera(faceCamera, worldA, worldB, worldC);

                CaptureGaussianSplatOnly(worldA, worldB, worldC, renderTexture, faceCamera);

                RenderTexture.active = renderTexture;
                tempTexture.ReadPixels(new Rect(0, 0, m_Settings.textureSize, m_Settings.textureSize), 0, 0);
                tempTexture.Apply();
                RenderTexture.active = null;

                // Get UVs
                Vector2 uvA = uvs[vertA];
                Vector2 uvB = uvs[vertB];
                Vector2 uvC = uvs[vertC];

                // Convert UVs to texture space
                Vector2Int pixelA = new Vector2Int(Mathf.FloorToInt(uvA.x * m_Settings.textureSize), Mathf.FloorToInt(uvA.y * m_Settings.textureSize));
                Vector2Int pixelB = new Vector2Int(Mathf.FloorToInt(uvB.x * m_Settings.textureSize), Mathf.FloorToInt(uvB.y * m_Settings.textureSize));
                Vector2Int pixelC = new Vector2Int(Mathf.FloorToInt(uvC.x * m_Settings.textureSize), Mathf.FloorToInt(uvC.y * m_Settings.textureSize));

                // Compute bounding box
                int minX = Mathf.Max(0, Mathf.Min(pixelA.x, Mathf.Min(pixelB.x, pixelC.x)));
                int minY = Mathf.Max(0, Mathf.Min(pixelA.y, Mathf.Min(pixelB.y, pixelC.y)));
                int maxX = Mathf.Min(m_Settings.textureSize - 1, Mathf.Max(pixelA.x, Mathf.Max(pixelB.x, pixelC.x)));
                int maxY = Mathf.Min(m_Settings.textureSize - 1, Mathf.Max(pixelA.y, Mathf.Max(pixelB.y, pixelC.y)));

                // Compute inverse denominator for barycentric coordinates
                float invDenom = 1.0f / ((pixelB.y - pixelC.y) * (pixelA.x - pixelC.x) + (pixelC.x - pixelB.x) * (pixelA.y - pixelC.y));
                if (float.IsInfinity(invDenom) || float.IsNaN(invDenom))
                    continue;

                // Get camera matrices
                Matrix4x4 worldToCameraMatrix = faceCamera.worldToCameraMatrix;
                Matrix4x4 projectionMatrix = faceCamera.projectionMatrix;

                // Time the pixel loop separately
                // stopwatch2.Restart();

                for (int y = minY - 5; y <= maxY + 5; y++)
                {
                    for (int x = minX - 5; x <= maxX + 5; x++)
                    {
                        // Compute barycentric coordinates
                        float wA = ((pixelB.y - pixelC.y) * (x - pixelC.x) + (pixelC.x - pixelB.x) * (y - pixelC.y)) * invDenom;
                        float wB = ((pixelC.y - pixelA.y) * (x - pixelC.x) + (pixelA.x - pixelC.x) * (y - pixelC.y)) * invDenom;
                        float wC = 1.0f - wA - wB;

                        // Interpolate world position
                        Vector3 worldPos = wA * worldA + wB * worldB + wC * worldC;
                        Vector3 viewPos = worldToCameraMatrix.MultiplyPoint(worldPos);
                        Vector4 clipPos = projectionMatrix.MultiplyPoint(viewPos);
                        Vector2 ndcPos = new Vector2(clipPos.x, clipPos.y);
                        Vector2 texCoord = new Vector2(ndcPos.x * 0.5f + 0.5f, ndcPos.y * 0.5f + 0.5f);
                        Color currentColor = generatedTexture.GetPixel(x, y);

                        if (wA >= 0 && wB >= 0 && wC >= 0 || currentColor == Color.clear)
                        {
                            Color sampledColor = tempTexture.GetPixel((int)(texCoord.x * m_Settings.triangleTextureSize), (int)(texCoord.y * m_Settings.triangleTextureSize));
                            generatedTexture.SetPixel(x, y, sampledColor);
                        }
                    }
                }

                // totalTriangleTime += stopwatch1.ElapsedTicks;
            }

            GSRenderSystem.instance.m_ForceDepth = false;

            // Apply changes
            generatedTexture.Apply();

            // Cleanup
            DestroyImmediate(tempTexture);
            renderTexture.Release();
            DestroyImmediate(renderTexture);
            DestroyImmediate(cameraObj);

            // Compute and log timing results
            // double averageTriangleTimeMs = (double)totalTriangleTime / count / Stopwatch.Frequency * 1000.0;

            // Debug.Log($"Average time per triangle: {averageTriangleTimeMs:F4} ms");

            DestroyImmediate(context.backwardDepthTex);
            DestroyImmediate(context.backwardNormalTex);

            context.backwardDepthTex = (generatedTexture);
            context.backwardNormalTex = DepthToNormalMap(generatedTexture, 10);

        }

        public void CaptureGaussianSplatOnly(Vector3 vertA, Vector3 vertB, Vector3 vertC, RenderTexture outputRT, Camera renderCamera) {
            // Save the original culling mask and clear flags
            int originalCullingMask = renderCamera.cullingMask;
            CameraClearFlags originalClearFlags = renderCamera.clearFlags;
            Color originalBackgroundColor = renderCamera.backgroundColor;
            
            try {
                // Setup the camera to view the triangle
                // Vector3 cameraPos, faceNormal;
                // float orthoSize;
                SetCamera(renderCamera, vertA, vertB, vertC);
                
                // Make sure the camera renders to our render texture
                renderCamera.targetTexture = outputRT;
                
                // Set the culling mask to only include the layer your Gaussian Splat objects are on
                // Assuming your splats are on layer 8 (adjust as needed)
                renderCamera.cullingMask = 1 << 8; // Only the layer with Gaussian Splats
                
                // Clear to transparent color
                renderCamera.clearFlags = CameraClearFlags.SolidColor;
                renderCamera.backgroundColor = new Color(0, 0, 0, 0);
                
                // Force the camera to render
                renderCamera.Render();
            }
            finally {
                // Restore camera settings
                renderCamera.cullingMask = originalCullingMask;
                renderCamera.clearFlags = originalClearFlags;
                renderCamera.backgroundColor = originalBackgroundColor;
            }
        }

// public void WriteDistancesToJson(Texture2D tex, float avgSize, SharedComputeContext context)
// {
//     // Get dimensions of the distances texture
//     int triangleCount = context.scaffoldData.indexCount / 3;
//     int distanceBins = 100;
    
//     // Create a data structure to hold the distance information
//     var triangleData = new List<TrianglesData>();
    
//     for (int i = 0; i < triangleCount; i++) {
//         var distancesData = new List<DistanceEntry>();
        
//         for (int j = 0; j < distanceBins; j++) {
//             // Calculate the x value (actual distance value)
//             float distanceValue = -0.5f + j / (float)(distanceBins - 1) * 1f;

//             distancesData.Add(new DistanceEntry {
//                 x = distanceValue,
//                 signedDistance = tex.GetPixel(i, j).r
//             });
//         }
        
//         triangleData.Add(new TrianglesData {
//             distances = distancesData.ToArray(),
//             avgSize = avgSize
//         });
//     }
   
//     var wrapper = new DistanceDataWrapper {
//         triangles = triangleData.ToArray()
//     };
   
//     // Convert to JSON
//     string jsonData = JsonUtility.ToJson(wrapper, true); // true for pretty print
   
//     // Create a unique filename with timestamp
//     string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
//     string filename = $"SplatDistances_{timestamp}.json";
//     string path = Path.Combine(Application.persistentDataPath, filename);
   
//     // Write to file
//     File.WriteAllText(path, jsonData);
   
//     Debug.Log($"Distances written to: {path}");
// }

//         [System.Serializable]
// public class DistanceEntry
// {
//     public float x;
//     public float signedDistance;
// }

// [System.Serializable]
// public class TrianglesData
// {
//     public DistanceEntry[] distances;
//     public float avgSize;
// }

// [System.Serializable]
// public class DistanceDataWrapper
// {
//     public TrianglesData[] triangles;
// }


        private void SetCamera(Camera cam, Vector3 v0, Vector3 v1, Vector3 v2)
        {
            // Calculate face center (centroid)
            Vector3 faceCenter = (v0 + v1 + v2) / 3f;

            // Calculate face normal
            Vector3 edge1 = v1 - v0;
            Vector3 edge2 = v2 - v0;
            Vector3 normal = Vector3.Cross(edge1, edge2).normalized;

            // Position the camera facing the face
            Vector3 cameraPos = faceCenter + normal;
            cam.transform.position = cameraPos;
            cam.transform.LookAt(faceCenter, Vector3.up);

            // Calculate the orthographic size to fit the face
            float orthoSize = CalculateOrthographicSize(cam, v0, v1, v2);
            cam.orthographicSize = orthoSize;
        }

        private float CalculateOrthographicSize(Camera cam, Vector3 v0, Vector3 v1, Vector3 v2)
        {
            // Compute the centroid (center of the triangle)
            Vector3 center = (v0 + v1 + v2) / 3f;

            // Compute the maximum distance from the center to any of the points
            float maxDistance = Mathf.Max(
                Vector3.Distance(center, v0),
                Vector3.Distance(center, v1),
                Vector3.Distance(center, v2)
            );

            // Adjust the orthographic size based on the camera aspect ratio + small border
            float orthoSizeHeight = maxDistance * 1.1f;
            float orthoSizeWidth = maxDistance / cam.aspect;

            // Ensure we take the larger size to fit everything
            return Mathf.Max(orthoSizeHeight, orthoSizeWidth);
        }

        public Texture2D DepthToNormalMap(Texture2D depthMap, float strength = 1.0f)
        {
            int width = depthMap.width;
            int height = depthMap.height;
            
            // Create output normal map texture (RGBA32 for better precision)
            Texture2D normalMap = new Texture2D(width, height, TextureFormat.RGBA32, false);
            
            // Get depth data
            Color[] depthPixels = depthMap.GetPixels();
            Color[] normalPixels = new Color[width * height];
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Sample neighboring depth values
                    // Use the red channel as the depth value (assuming grayscale depth map)
                    float depthL = GetDepthSafe(depthPixels, x - 1, y, width, height).r;
                    float depthR = GetDepthSafe(depthPixels, x + 1, y, width, height).r;
                    float depthT = GetDepthSafe(depthPixels, x, y + 1, width, height).r;
                    float depthB = GetDepthSafe(depthPixels, x, y - 1, width, height).r;
                    
                    // Calculate partial derivatives dz/dx and dz/dy
                    // Note: We inverse the x direction to match Unity's normal map convention
                    float dzdx = (depthL - depthR) * strength;
                    float dzdy = (depthB - depthT) * strength;
                    
                    // In normal map calculation, we need to create a normal from the gradient
                    // The normal is perpendicular to the tangent vectors (1,0,dzdx) and (0,1,dzdy)
                    // Cross product of these tangent vectors gives us the normal
                    Vector3 normal = Vector3.Cross(new Vector3(2.0f, 0, dzdx), new Vector3(0, 2.0f, dzdy)).normalized;
                    
                    // Make sure the normal is pointing outward (positive Z)
                    if (normal.z < 0) normal = -normal;
                    
                    // Re-normalize to ensure unit length
                    normal.Normalize();
                    
                    // Convert from [-1,1] range to [0,1] range for texture storage
                    // This is the standard format for normal maps in Unity
                    Color normalColor = new Color(
                        normal.x * 0.5f + 0.5f,
                        normal.y * 0.5f + 0.5f,
                        normal.z * 0.5f + 0.5f,
                        1.0f  // Alpha channel should be 1
                    );
                    
                    // Store in output array
                    normalPixels[y * width + x] = normalColor;
                }
            }
            
            // Apply the pixels to the texture
            normalMap.SetPixels(normalPixels);
            normalMap.Apply();
            
            // Set appropriate texture settings for normal maps
            normalMap.wrapMode = TextureWrapMode.Clamp;
            normalMap.filterMode = FilterMode.Bilinear;
            
            return normalMap;
        }

        // Helper function to safely get depth values with edge handling
        private Color GetDepthSafe(Color[] pixels, int x, int y, int width, int height)
        {
            // Clamp coordinates to ensure they're within bounds
            x = Mathf.Clamp(x, 0, width - 1);
            y = Mathf.Clamp(y, 0, height - 1);
            
            return pixels[y * width + x];
        }
    }
}