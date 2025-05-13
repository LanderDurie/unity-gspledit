using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace UnityEngine.GsplEdit
{
    public class TextureGenRayCast : LinkGenBackwardBase
    {
        [System.Serializable]
        public class Settings
        {
            public int textureSize = 4096;
            public int triangleTextureSize = 512;
        }

        public Settings m_Settings = new();
        public ComputeShader m_RayCastShader;

        struct RayPayload
        {
            public Vector3 origin;
            public Vector3 direction;
            public int pixelX;
            public int pixelY;
        }

        public override unsafe void Generate(SharedComputeContext context)
        {
            Mesh mesh = context.scaffoldMesh;

            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            Vector2[] uvs = mesh.uv;
            Vector3[] normals = mesh.normals;

            List<RayPayload> raysToTrace = new();

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                Vector3 worldA = vertices[a], worldB = vertices[b], worldC = vertices[c];
                Vector3 normalA = normals[a], normalB = normals[b], normalC = normals[c];
                Vector2 uvA = uvs[a], uvB = uvs[b], uvC = uvs[c];

                Vector2Int pA = ToPixel(uvA), pB = ToPixel(uvB), pC = ToPixel(uvC);
                var (minX, maxX, minY, maxY) = BoundingBox(pA, pB, pC);
                float invDenom = 1.0f / ((pB.y - pC.y) * (pA.x - pC.x) + (pC.x - pB.x) * (pA.y - pC.y));

                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        float wA = ((pB.y - pC.y) * (x - pC.x) + (pC.x - pB.x) * (y - pC.y)) * invDenom;
                        float wB = ((pC.y - pA.y) * (x - pC.x) + (pA.x - pC.x) * (y - pC.y)) * invDenom;
                        float wC = 1f - wA - wB;

                        if (wA >= 0 && wB >= 0 && wC >= 0)
                        {
                            Vector3 pos = wA * worldA + wB * worldB + wC * worldC;
                            Vector3 nrm = (wA * normalA + wB * normalB + wC * normalC).normalized;
                            raysToTrace.Add(new RayPayload { origin = pos, direction = nrm, pixelX = x, pixelY = y });
                        }
                    }
                }
            }

            int kernel = m_RayCastShader.FindKernel("CSMain");

            ComputeBuffer rayBuffer = new ComputeBuffer(raysToTrace.Count, sizeof(float) * 6 + sizeof(int) * 2);
            rayBuffer.SetData(raysToTrace);

            RenderTexture outputTex = new RenderTexture(m_Settings.textureSize, m_Settings.textureSize, 0, RenderTextureFormat.ARGBFloat)
            {
                enableRandomWrite = true
            };
            outputTex.Create();

            m_RayCastShader.SetBuffer(kernel, "rays", rayBuffer);
            m_RayCastShader.SetTexture(kernel, "Result", outputTex);
            m_RayCastShader.SetInt("rayCount", raysToTrace.Count);
            m_RayCastShader.SetInt("textureSize", m_Settings.textureSize);

            int threadGroups = Mathf.CeilToInt(raysToTrace.Count / 64.0f);
            m_RayCastShader.Dispatch(kernel, threadGroups, 1, 1);

            RenderTexture.active = outputTex;
            Texture2D tex = new Texture2D(m_Settings.textureSize, m_Settings.textureSize, TextureFormat.RGBAFloat, false);
            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            tex.Apply();
            
            context.backwardDepthTex = tex;
            context.backwardNormalTex = DepthToNormalMap(tex, 10);

            rayBuffer.Release();
        }

        Vector2Int ToPixel(Vector2 uv)
        {
            return new Vector2Int(
                Mathf.Clamp(Mathf.FloorToInt(uv.x * m_Settings.textureSize), 0, m_Settings.textureSize - 1),
                Mathf.Clamp(Mathf.FloorToInt(uv.y * m_Settings.textureSize), 0, m_Settings.textureSize - 1)
            );
        }

        (int minX, int maxX, int minY, int maxY) BoundingBox(Vector2Int a, Vector2Int b, Vector2Int c)
        {
            int minX = Mathf.Max(0, Mathf.Min(a.x, Mathf.Min(b.x, c.x)));
            int minY = Mathf.Max(0, Mathf.Min(a.y, Mathf.Min(b.y, c.y)));
            int maxX = Mathf.Min(m_Settings.textureSize - 1, Mathf.Max(a.x, Mathf.Max(b.x, c.x)));
            int maxY = Mathf.Min(m_Settings.textureSize - 1, Mathf.Max(a.y, Mathf.Max(b.y, c.y)));
            return (minX, maxX, minY, maxY);
        }
  
        public Texture2D DepthToNormalMap(Texture2D depthMap, float strength = 1.0f)
        {
            int width = depthMap.width;
            int height = depthMap.height;
            
            // Create output normal map texture (RGB format is sufficient for normals)
            Texture2D normalMap = new Texture2D(width, height, TextureFormat.RGB24, false);
            
            // Get depth data
            Color[] depthPixels = depthMap.GetPixels();
            Color[] normalPixels = new Color[width * height];
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Sample neighboring depth values
                    float depthLeft = GetDepthSafe(depthPixels, x - 1, y, width, height);
                    float depthRight = GetDepthSafe(depthPixels, x + 1, y, width, height);
                    float depthUp = GetDepthSafe(depthPixels, x, y + 1, width, height);
                    float depthDown = GetDepthSafe(depthPixels, x, y - 1, width, height);
                    
                    // Calculate surface normal using Sobel operator
                    // The normal is the cross product of the horizontal and vertical tangent vectors
                    Vector3 normal = new Vector3(
                        (depthLeft - depthRight) * strength,
                        (depthDown - depthUp) * strength,
                        1.0f
                    );
                    
                    // Normalize the vector
                    normal.Normalize();
                    
                    // Convert from [-1,1] range to [0,1] range for color storage
                    Color normalColor = new Color(
                        normal.x * 0.5f + 0.5f,
                        normal.y * 0.5f + 0.5f,
                        normal.z * 0.5f + 0.5f
                    );
                    
                    // Store in output array
                    normalPixels[y * width + x] = normalColor;
                }
            }
            
            // Apply the pixels to the texture and return
            normalMap.SetPixels(normalPixels);
            normalMap.Apply();
            
            return normalMap;
        }

        private float GetDepthSafe(Color[] depthPixels, int x, int y, int width, int height)
        {
            // Clamp coordinates to texture boundaries
            x = Mathf.Clamp(x, 0, width - 1);
            y = Mathf.Clamp(y, 0, height - 1);
            
            // Use the red channel as the depth value
            // Assuming depth is stored in the red channel of the color
            return depthPixels[y * width + x].r;
        }

    }
}