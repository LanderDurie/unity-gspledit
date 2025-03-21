// using UnityEngine;
// using System.Linq;
// using UnityEditor;
// using System.Collections.Generic;

// [RequireComponent(typeof(MeshFilter))]
// public class FaceTextureGenerator : MonoBehaviour
// {
//     [Header("Settings")]
//     [SerializeField] private int textureSize = 1024;
//     [SerializeField] private LayerMask layersToRender;
//     [SerializeField] private float cameraDistance = 1f;
//     [SerializeField] private ComputeShader triangleRasterizationShader;

//     [Header("Debug")]
//     [SerializeField] private Material previewMaterial;
//     [SerializeField] private Color edgeColor = Color.red;
//     [SerializeField] private int edgeThickness = 2;
//     [SerializeField] private bool drawFaceEdges = true;


//     public void GenerateTexture()
//     {
//         if (triangleRasterizationShader == null)
//         {
//             Debug.LogError("Compute shader is missing. Please assign it in the inspector.");
//             return;
//         }
        
//         MeshFilter meshFilter = GetComponent<MeshFilter>();
//         MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

//         if (meshFilter == null || meshFilter.sharedMesh == null)
//         {
//             Debug.LogError("No mesh found on this GameObject!");
//             return;
//         }

//         Mesh mesh = meshFilter.sharedMesh;
//         mesh = UnwrapMesh(mesh);

//         // Create target texture
//         RenderTexture generatedTexture = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGB32);
//         generatedTexture.enableRandomWrite = true;
//         generatedTexture.Create();
        
//         // Clear the texture
//         int clearKernel = triangleRasterizationShader.FindKernel("ClearTexture");
//         triangleRasterizationShader.SetTexture(clearKernel, "_OutputTexture", generatedTexture);
//         triangleRasterizationShader.SetInt("_TextureSize", textureSize);
//         triangleRasterizationShader.Dispatch(clearKernel, Mathf.CeilToInt(textureSize / 8f), Mathf.CeilToInt(textureSize / 8f), 1);

//         // Create a single RenderTexture to reuse
//         RenderTexture renderTexture = new RenderTexture(textureSize, textureSize, 24);
//         renderTexture.antiAliasing = 1;
//         renderTexture.Create();

//         // Create camera only once
//         GameObject cameraObj = new GameObject("FaceCamera");
//         Camera faceCamera = cameraObj.AddComponent<Camera>();
//         faceCamera.orthographic = true;
//         faceCamera.clearFlags = CameraClearFlags.SolidColor;
//         faceCamera.backgroundColor = new Color(0, 0, 0, 0);
//         faceCamera.cullingMask = layersToRender;
//         faceCamera.nearClipPlane = 0.001f;
//         faceCamera.farClipPlane = cameraDistance * 2;
//         faceCamera.enabled = false;
//         faceCamera.allowMSAA = false;
//         faceCamera.targetTexture = renderTexture;

//         Vector3[] vertices = mesh.vertices;
//         int[] triangles = mesh.triangles;
//         Vector2[] uvs = mesh.uv;

//         // Create a temporary texture for reading render result
//         Texture2D tempTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        
//         // Get kernel handle for the compute shader
//         int rasterizeKernel = triangleRasterizationShader.FindKernel("RasterizeTriangle");
        
//         // Find the edge drawing kernel if needed
//         int edgeKernel = -1;
//         if (drawFaceEdges)
//         {
//             edgeKernel = triangleRasterizationShader.FindKernel("DrawEdges");
//         }
        
//         // Create a compute buffer for triangle data
//         // Structure: worldA, worldB, worldC, uvA, uvB, uvC, cameraPos, normal, projectionMatrix (4x4), viewMatrix (4x4)
//         ComputeBuffer triangleDataBuffer = new ComputeBuffer(1, 
//             (3 * 3 + 3 * 2 + 3 + 3 + 16 + 16) * sizeof(float)); // 3 vertices * 3 floats + 3 UVs * 2 floats + cameraPos(3) + normal(3) + 2 matrices (16 each)
        
//         // Create a temporary texture on the GPU
//         RenderTexture tempRenderTexture = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGB32);
//         tempRenderTexture.enableRandomWrite = true;
//         tempRenderTexture.Create();
        
//         for (int i = 0; i < triangles.Length; i += 3)
//         {
//             int vertA = triangles[i];
//             int vertB = triangles[i + 1];
//             int vertC = triangles[i + 2];

//             // Get world positions
//             Vector3 worldA = transform.TransformPoint(vertices[vertA]);
//             Vector3 worldB = transform.TransformPoint(vertices[vertB]);
//             Vector3 worldC = transform.TransformPoint(vertices[vertC]);

//             // Setup camera for this face
//             Vector3 cameraPos, faceNormal;
//             float orthoSize;
//             SetCamera(faceCamera, worldA, worldB, worldC, out cameraPos, out faceNormal, out orthoSize);
            
//             // Render the scene without the mesh itself
//             meshRenderer.enabled = false;
//             faceCamera.Render();
//             meshRenderer.enabled = true;

//             // Read pixels from the render texture
//             RenderTexture.active = renderTexture;
//             tempTexture.ReadPixels(new Rect(0, 0, textureSize, textureSize), 0, 0);
//             tempTexture.Apply();
//             RenderTexture.active = null;
            
//             // Get UVs for the triangle
//             Vector2 uvA = uvs[vertA];
//             Vector2 uvB = uvs[vertB];
//             Vector2 uvC = uvs[vertC];

//             // Convert UVs to texture space
//             Vector2Int pixelA = new Vector2Int(Mathf.FloorToInt(uvA.x * textureSize), Mathf.FloorToInt(uvA.y * textureSize));
//             Vector2Int pixelB = new Vector2Int(Mathf.FloorToInt(uvB.x * textureSize), Mathf.FloorToInt(uvB.y * textureSize));
//             Vector2Int pixelC = new Vector2Int(Mathf.FloorToInt(uvC.x * textureSize), Mathf.FloorToInt(uvC.y * textureSize));

//             // Pack triangle data for the compute shader
//             float[] triangleData = new float[(3 * 3 + 3 * 2 + 3 + 3 + 16 + 16)];
//             int index = 0;
            
//             // World positions
//             triangleData[index++] = worldA.x; triangleData[index++] = worldA.y; triangleData[index++] = worldA.z;
//             triangleData[index++] = worldB.x; triangleData[index++] = worldB.y; triangleData[index++] = worldB.z;
//             triangleData[index++] = worldC.x; triangleData[index++] = worldC.y; triangleData[index++] = worldC.z;
            
//             // UVs
//             triangleData[index++] = uvA.x; triangleData[index++] = uvA.y;
//             triangleData[index++] = uvB.x; triangleData[index++] = uvB.y;
//             triangleData[index++] = uvC.x; triangleData[index++] = uvC.y;
            
//             // Camera position
//             triangleData[index++] = cameraPos.x; triangleData[index++] = cameraPos.y; triangleData[index++] = cameraPos.z;
            
//             // Face normal
//             triangleData[index++] = faceNormal.x; triangleData[index++] = faceNormal.y; triangleData[index++] = faceNormal.z;
            
//             // Projection matrix
//             Matrix4x4 projectionMatrix = faceCamera.projectionMatrix;
//             for (int row = 0; row < 4; row++)
//             {
//                 for (int col = 0; col < 4; col++)
//                 {
//                     triangleData[index++] = projectionMatrix[row, col];
//                 }
//             }
            
//             // View matrix (world to camera)
//             Matrix4x4 viewMatrix = faceCamera.worldToCameraMatrix;
//             for (int row = 0; row < 4; row++)
//             {
//                 for (int col = 0; col < 4; col++)
//                 {
//                     triangleData[index++] = viewMatrix[row, col];
//                 }
//             }
            
//             // Set the data to the compute buffer
//             triangleDataBuffer.SetData(triangleData);

//             // First, copy the tempTexture to a render texture for GPU access
//             Graphics.Blit(tempTexture, tempRenderTexture);
            
//             // Set shader parameters
//             triangleRasterizationShader.SetBuffer(rasterizeKernel, "_TriangleData", triangleDataBuffer);
//             triangleRasterizationShader.SetTexture(rasterizeKernel, "_InputTexture", tempRenderTexture);
//             triangleRasterizationShader.SetTexture(rasterizeKernel, "_InputTexture", generatedTexture);
//             triangleRasterizationShader.SetInt("_TextureSize", textureSize);
            
//             // Compute bounding box for better performance (avoid processing entire texture)
//             int minX = Mathf.Max(0, Mathf.Min(pixelA.x, Mathf.Min(pixelB.x, pixelC.x)));
//             int minY = Mathf.Max(0, Mathf.Min(pixelA.y, Mathf.Min(pixelB.y, pixelC.y)));
//             int maxX = Mathf.Min(textureSize - 1, Mathf.Max(pixelA.x, Mathf.Max(pixelB.x, pixelC.x)));
//             int maxY = Mathf.Min(textureSize - 1, Mathf.Max(pixelA.y, Mathf.Max(pixelB.y, pixelC.y)));
            
//             // Calculate dispatch sizes
//             int dispatchWidth = Mathf.CeilToInt((maxX - minX + 1) / 8f);
//             int dispatchHeight = Mathf.CeilToInt((maxY - minY + 1) / 8f);
            
//             // Ensure at least one thread group
//             dispatchWidth = Mathf.Max(1, dispatchWidth);
//             dispatchHeight = Mathf.Max(1, dispatchHeight);
            
//             // Dispatch the compute shader
//             triangleRasterizationShader.Dispatch(rasterizeKernel, dispatchWidth, dispatchHeight, 1);
            
//             // Draw triangle edges if enabled
//             if (drawFaceEdges && edgeKernel != -1)
//             {
//                 triangleRasterizationShader.SetBuffer(edgeKernel, "_TriangleData", triangleDataBuffer);
//                 triangleRasterizationShader.SetTexture(edgeKernel, "_OutputTexture", generatedTexture);
//                 triangleRasterizationShader.SetInt("_TextureSize", textureSize);
//                 triangleRasterizationShader.SetVector("_EdgeColor", edgeColor);
//                 triangleRasterizationShader.SetInt("_EdgeThickness", edgeThickness);
//                 triangleRasterizationShader.SetBool("_DrawEdges", true);
//                 triangleRasterizationShader.Dispatch(edgeKernel, 1, 1, 1);
//             }
//         }

//         // Cleanup resources
//         triangleDataBuffer.Dispose();
//         tempRenderTexture.Release();
//         DestroyImmediate(tempRenderTexture);
//         DestroyImmediate(tempTexture);
//         renderTexture.Release();
//         DestroyImmediate(renderTexture);
//         DestroyImmediate(cameraObj);

//         // Convert RenderTexture to Texture2D for materials
//         Texture2D finalTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
//         RenderTexture.active = generatedTexture;
//         finalTexture.ReadPixels(new Rect(0, 0, textureSize, textureSize), 0, 0);
//         finalTexture.Apply();
//         RenderTexture.active = null;
        
//         // Apply the texture to materials
//         previewMaterial.mainTexture = finalTexture;
//         meshRenderer.sharedMaterial.mainTexture = finalTexture;
//         meshFilter.mesh = mesh;
        
//         // Release the generated render texture
//         generatedTexture.Release();
//         DestroyImmediate(generatedTexture);

//         Debug.Log("Texture generated successfully!");
//     }

//     private void SetCamera(Camera cam, Vector3 v0, Vector3 v1, Vector3 v2, out Vector3 cameraPos, out Vector3 normal, out float orthoSize)
//     {
//         // Calculate face center (centroid)
//         Vector3 faceCenter = (v0 + v1 + v2) / 3f;

//         // Calculate face normal
//         Vector3 edge1 = v1 - v0;
//         Vector3 edge2 = v2 - v0;
//         normal = Vector3.Cross(edge1, edge2).normalized;

//         // Position the camera facing the face
//         cameraPos = faceCenter + normal * cameraDistance;
//         cam.transform.position = cameraPos;
//         cam.transform.LookAt(faceCenter, Vector3.up);

//         // Calculate the orthographic size to fit the face
//         orthoSize = CalculateOrthographicSize(cam, v0, v1, v2);
//         cam.orthographicSize = orthoSize;
//     }

//     private float CalculateOrthographicSize(Camera cam, Vector3 v0, Vector3 v1, Vector3 v2)
//     {
//         // Compute the centroid (center of the triangle)
//         Vector3 center = (v0 + v1 + v2) / 3f;

//         // Compute the maximum distance from the center to any of the points
//         float maxDistance = Mathf.Max(
//             Vector3.Distance(center, v0),
//             Vector3.Distance(center, v1),
//             Vector3.Distance(center, v2)
//         );

//         // Adjust the orthographic size based on the camera aspect ratio
//         float orthoSizeHeight = maxDistance;
//         float orthoSizeWidth = maxDistance / cam.aspect;

//         // Ensure we take the larger size to fit everything
//         return Mathf.Max(orthoSizeHeight, orthoSizeWidth);
//     }

//     public Mesh UnwrapMesh(Mesh mesh)
//     {
//         // Generate per-triangle UVs
//         Vector2[] unwrappedUVs = Unwrapping.GeneratePerTriangleUV(mesh);

//         // Duplicate vertices and adjust triangle indices
//         Vector3[] vertices = mesh.vertices;
//         int[] triangles = mesh.triangles;
//         Vector3[] newVertices = new Vector3[triangles.Length];
//         Vector2[] newUVs = new Vector2[triangles.Length];
//         int[] newTriangles = new int[triangles.Length];

//         for (int i = 0; i < triangles.Length; i++)
//         {
//             // Duplicate vertices
//             newVertices[i] = vertices[triangles[i]];
//             newUVs[i] = unwrappedUVs[i];
//             newTriangles[i] = i; // Each triangle now uses its own unique vertices
//         }

//         // Create a new mesh with the duplicated vertices and UVs
//         Mesh newMesh = new Mesh
//         {
//             vertices = newVertices,
//             uv = newUVs,
//             triangles = newTriangles
//         };

//         // Recalculate normals and bounds for the new mesh
//         newMesh.RecalculateNormals();
//         newMesh.RecalculateBounds();

//         return newMesh;
//     }
// }

// #if UNITY_EDITOR
// [CustomEditor(typeof(FaceTextureGenerator))]
// public class FaceTextureGeneratorEditor : Editor
// {
//     public override void OnInspectorGUI()
//     {
//         DrawDefaultInspector();

//         FaceTextureGenerator generator = (FaceTextureGenerator)target;

//         if (GUILayout.Button("Generate Face Texture"))
//         {
//             generator.GenerateTexture();
//         }
//     }
// }
// #endif