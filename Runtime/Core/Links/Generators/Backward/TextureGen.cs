using System.Linq;
using TreeEditor;
using UnityEditor;

namespace UnityEngine.GsplEdit
{
    public class TextureGen : LinkGenBackwardBase
    {
        [System.Serializable]
        public class Settings
        {
            public uint threadsPerGroup = 64;
            public Vector2 resolution = new Vector2(1000, 1000);
            public float cameraDistance = 0.1f;
            public int textureSize = 1024;
        }

        public Settings m_Settings = new();
        public ComputeShader m_TextureLinkageCompute;

        public unsafe override void Generate(SharedComputeContext context) {
            Mesh mesh = context.scaffoldMesh;
            
            mesh = UnwrapMesh(mesh);

            // Create target texture
            Texture2D generatedTexture = new Texture2D(m_Settings.textureSize, m_Settings.textureSize, TextureFormat.RGBA32, false);
            Color[] clearPixels = Enumerable.Repeat(Color.clear, m_Settings.textureSize * m_Settings.textureSize).ToArray();
            generatedTexture.SetPixels(clearPixels);
            generatedTexture.Apply();

            // Create a single RenderTexture to reuse
            RenderTexture renderTexture = new RenderTexture(m_Settings.textureSize, m_Settings.textureSize, 24);
            renderTexture.antiAliasing = 1;
            renderTexture.Create();

            // Create camera only once
            GameObject cameraObj = new GameObject("FaceCamera");
            Camera faceCamera = cameraObj.AddComponent<Camera>();
            faceCamera.orthographic = true;
            faceCamera.clearFlags = CameraClearFlags.SolidColor;
            faceCamera.backgroundColor = new Color(0, 0, 0, 0);
            faceCamera.nearClipPlane = 0.001f;
            faceCamera.farClipPlane = m_Settings.cameraDistance * 2;
            faceCamera.enabled = false;
            faceCamera.allowMSAA = false;
            faceCamera.targetTexture = renderTexture;

            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            Vector2[] uvs = mesh.uv;

            // Create a temporary texture for reading render result
            Texture2D tempTexture = new Texture2D(m_Settings.textureSize, m_Settings.textureSize, TextureFormat.RGBA32, false);
            
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int vertA = triangles[i];
                int vertB = triangles[i + 1];
                int vertC = triangles[i + 2];

                // Get world positions
                Vector3 worldA = vertices[vertA];
                Vector3 worldB = vertices[vertB];
                Vector3 worldC = vertices[vertC];

                // Setup camera for this face
                Vector3 cameraPos, faceNormal;
                float orthoSize;
                SetCamera(faceCamera, worldA, worldB, worldC, out cameraPos, out faceNormal, out orthoSize);
                
                // Render the scene without the mesh itself
                // meshRenderer.enabled = false;
                faceCamera.Render();
                // meshRenderer.enabled = true;

                // Read pixels from the render texture
                RenderTexture.active = renderTexture;
                tempTexture.ReadPixels(new Rect(0, 0, m_Settings.textureSize, m_Settings.textureSize), 0, 0);
                tempTexture.Apply();
                RenderTexture.active = null;
                
                // Get UVs for the triangle
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

                // Precompute inverse denominator for barycentric coordinates
                float invDenom = 1.0f / ((pixelB.y - pixelC.y) * (pixelA.x - pixelC.x) + (pixelC.x - pixelB.x) * (pixelA.y - pixelC.y));
                if (float.IsInfinity(invDenom) || float.IsNaN(invDenom))
                    continue;

                // Get camera view matrix for transforming points
                Matrix4x4 worldToCameraMatrix = faceCamera.worldToCameraMatrix;
                Matrix4x4 projectionMatrix = faceCamera.projectionMatrix;
                
                // Iterate through the bounding box
                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        // Compute barycentric coordinates
                        float wA = ((pixelB.y - pixelC.y) * (x - pixelC.x) + (pixelC.x - pixelB.x) * (y - pixelC.y)) * invDenom;
                        float wB = ((pixelC.y - pixelA.y) * (x - pixelC.x) + (pixelA.x - pixelC.x) * (y - pixelC.y)) * invDenom;
                        float wC = 1.0f - wA - wB;

                        // If inside triangle
                        if (wA >= 0 && wB >= 0 && wC >= 0)
                        {
                            // Interpolate the world position using barycentric coordinates
                            Vector3 worldPos = wA * worldA + wB * worldB + wC * worldC;
                            
                            // Transform world position to camera space
                            Vector3 viewPos = worldToCameraMatrix.MultiplyPoint(worldPos);
                            Vector4 clipPos = projectionMatrix.MultiplyPoint(viewPos);
                            
                            // Convert to normalized device coordinates (NDC)
                            Vector2 ndcPos = new Vector2(clipPos.x, clipPos.y);
                            
                            // Convert NDC to texture coordinates [0,1]
                            Vector2 texCoord = new Vector2(ndcPos.x * 0.5f + 0.5f, ndcPos.y * 0.5f + 0.5f);
                            
                            // Sample the rendered texture
                            Color sampledColor;
                            if (texCoord.x >= 0 && texCoord.x <= 1 && texCoord.y >= 0 && texCoord.y <= 1)
                            {
                                sampledColor = tempTexture.GetPixelBilinear(texCoord.x, texCoord.y);
                            }
                            else
                            {
                                sampledColor = Color.black;
                            }
                            
                            // Apply to our output texture
                            generatedTexture.SetPixel(x, y, sampledColor);
                        }
                    }
                }

                // Draw triangle edges if enabled
                // if (drawFaceEdges)
                // {
                //     DrawLine(pixelA, pixelB, edgeColor, generatedTexture);
                //     DrawLine(pixelB, pixelC, edgeColor, generatedTexture);
                //     DrawLine(pixelC, pixelA, edgeColor, generatedTexture);
                // }
            }

            // Apply all pixel changes at once
            generatedTexture.Apply();

            // Cleanup resources
            DestroyImmediate(tempTexture);
            renderTexture.Release();
            DestroyImmediate(renderTexture);
            DestroyImmediate(cameraObj);

            // Apply the texture to materials
            context.backwardColorTex = generatedTexture;
            context.scaffoldMesh = mesh;

            Debug.Log("Texture generated successfully!");
        }

        private void SetCamera(Camera cam, Vector3 v0, Vector3 v1, Vector3 v2, out Vector3 cameraPos, out Vector3 normal, out float orthoSize)
        {
            // Calculate face center (centroid)
            Vector3 faceCenter = (v0 + v1 + v2) / 3f;

            // Calculate face normal
            Vector3 edge1 = v1 - v0;
            Vector3 edge2 = v2 - v0;
            normal = Vector3.Cross(edge1, edge2).normalized;

            // Position the camera facing the face
            cameraPos = faceCenter + normal * m_Settings.cameraDistance;
            cam.transform.position = cameraPos;
            cam.transform.LookAt(faceCenter, Vector3.up);

            // Calculate the orthographic size to fit the face
            orthoSize = CalculateOrthographicSize(cam, v0, v1, v2);
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

            // Adjust the orthographic size based on the camera aspect ratio
            float orthoSizeHeight = maxDistance;
            float orthoSizeWidth = maxDistance / cam.aspect;

            // Ensure we take the larger size to fit everything
            return Mathf.Max(orthoSizeHeight, orthoSizeWidth);
        }

        public Mesh UnwrapMesh(Mesh mesh)
        {
            // Generate per-triangle UVs
            Vector2[] unwrappedUVs = Unwrapping.GeneratePerTriangleUV(mesh);

            // Duplicate vertices and adjust triangle indices
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            Vector3[] newVertices = new Vector3[triangles.Length];
            Vector2[] newUVs = new Vector2[triangles.Length];
            int[] newTriangles = new int[triangles.Length];

            for (int i = 0; i < triangles.Length; i++)
            {
                // Duplicate vertices
                newVertices[i] = vertices[triangles[i]];
                newUVs[i] = unwrappedUVs[i];
                newTriangles[i] = i; // Each triangle now uses its own unique vertices
            }

            // Create a new mesh with the duplicated vertices and UVs
            Mesh newMesh = new Mesh
            {
                vertices = newVertices,
                uv = newUVs,
                triangles = newTriangles
            };

            // Recalculate normals and bounds for the new mesh
            newMesh.RecalculateNormals();
            newMesh.RecalculateBounds();

            return newMesh;
        }
    }
}