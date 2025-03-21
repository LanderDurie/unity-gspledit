using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class UVUnwrapperWithDebug : MonoBehaviour
{
    public Material debugMaterial; // Material to which the debug texture will be assigned
    public int textureSize = 1024; // Size of the debug texture

    private Texture2D debugTexture; // Texture to draw the UV layout on

    public void UnwrapMesh()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogError("No mesh found on this GameObject!");
            return;
        }

        // Get the mesh
        Mesh mesh = meshFilter.sharedMesh;

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

        // Assign the new mesh to the MeshFilter
        meshFilter.mesh = newMesh;

        // Create a debug texture and draw the UV layout
        CreateDebugTexture(newUVs, newTriangles);

        Debug.Log("Per-triangle UV unwrapping and debug texture creation completed!");
    }

private void CreateDebugTexture(Vector2[] uvs, int[] triangles)
{
    // Create a new texture
    debugTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);

    // Fill the texture with a transparent background
    Color[] pixels = new Color[textureSize * textureSize];
    for (int i = 0; i < pixels.Length; i++)
    {
        pixels[i] = new Color(1, 1, 1, 0); // Transparent white
    }
    debugTexture.SetPixels(pixels);

    // Draw each triangle with a unique color
    for (int i = 0; i < triangles.Length; i += 3)
    {
        // Get the UV coordinates for the triangle
        Vector2 uv1 = uvs[triangles[i]];
        Vector2 uv2 = uvs[triangles[i + 1]];
        Vector2 uv3 = uvs[triangles[i + 2]];

        // Convert UV coordinates to texture space
        int x1 = (int)(uv1.x * textureSize);
        int y1 = (int)(uv1.y * textureSize);
        int x2 = (int)(uv2.x * textureSize);
        int y2 = (int)(uv2.y * textureSize);
        int x3 = (int)(uv3.x * textureSize);
        int y3 = (int)(uv3.y * textureSize);

        // Generate a random color for the triangle
        Color triangleColor = new Color(
            Random.Range(0.2f, 0.8f),
            Random.Range(0.2f, 0.8f),
            Random.Range(0.2f, 0.8f),
            0.7f // Semi-transparent
        );

        // Fill the triangle with the random color
        DrawFilledTriangle(x1, y1, x2, y2, x3, y3, triangleColor);

        // Draw the edges of the triangle in black
        DrawLine(x1, y1, x2, y2, Color.black);
        DrawLine(x2, y2, x3, y3, Color.black);
        DrawLine(x3, y3, x1, y1, Color.black);
    }

    // Apply changes to the texture
    debugTexture.Apply();

    // Assign the texture to the debug material
    if (debugMaterial != null)
    {
        debugMaterial.mainTexture = debugTexture;
    }
    else
    {
        Debug.LogWarning("No debug material assigned!");
    }
}

// Helper function to fill a triangle with a color
private void DrawFilledTriangle(int x1, int y1, int x2, int y2, int x3, int y3, Color color)
{
    // Sort the vertices by y-coordinate (y1 <= y2 <= y3)
    if (y1 > y2)
    {
        int tempX = x1; x1 = x2; x2 = tempX;
        int tempY = y1; y1 = y2; y2 = tempY;
    }
    if (y2 > y3)
    {
        int tempX = x2; x2 = x3; x3 = tempX;
        int tempY = y2; y2 = y3; y3 = tempY;
    }
    if (y1 > y2)
    {
        int tempX = x1; x1 = x2; x2 = tempX;
        int tempY = y1; y1 = y2; y2 = tempY;
    }

    // Calculate the total height of the triangle
    int totalHeight = y3 - y1;

    // Fill the first half of the triangle
    for (int y = y1; y <= y2; y++)
    {
        if (y < 0 || y >= textureSize) continue;

        int segmentHeight = y2 - y1 + 1;
        float alpha = (float)(y - y1) / totalHeight;
        float beta = (float)(y - y1) / segmentHeight;

        int xA = x1 + (int)((x3 - x1) * alpha);
        int xB = x1 + (int)((x2 - x1) * beta);

        // Ensure xA is to the left of xB
        if (xA > xB)
        {
            int temp = xA;
            xA = xB;
            xB = temp;
        }

        // Draw a horizontal line between xA and xB
        for (int x = xA; x <= xB; x++)
        {
            SetPixelSafe(x, y, color);
        }
    }

    // Fill the second half of the triangle
    for (int y = y2 + 1; y <= y3; y++)
    {
        if (y < 0 || y >= textureSize) continue;

        int segmentHeight = y3 - y2 + 1;
        float alpha = (float)(y - y1) / totalHeight;
        float beta = (float)(y - y2) / segmentHeight;

        int xA = x1 + (int)((x3 - x1) * alpha);
        int xB = x2 + (int)((x3 - x2) * beta);

        // Ensure xA is to the left of xB
        if (xA > xB)
        {
            int temp = xA;
            xA = xB;
            xB = temp;
        }

        // Draw a horizontal line between xA and xB
        for (int x = xA; x <= xB; x++)
        {
            SetPixelSafe(x, y, color);
        }
    }
}
    // Bresenham's line algorithm to draw lines on the texture
    private void DrawLine(int x0, int y0, int x1, int y1, Color color)
    {
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            SetPixelSafe(x0, y0, color);

            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    // Helper function to set a pixel safely (within texture bounds)
    private void SetPixelSafe(int x, int y, Color color)
    {
        if (x >= 0 && x < textureSize && y >= 0 && y < textureSize)
        {
            debugTexture.SetPixel(x, y, color);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(UVUnwrapperWithDebug))]
public class UVUnwrapperWithDebugEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        UVUnwrapperWithDebug unwrapper = (UVUnwrapperWithDebug)target;

        if (GUILayout.Button("Unwrap UVs and Generate Debug Texture"))
        {
            unwrapper.UnwrapMesh();
        }
    }
}
#endif