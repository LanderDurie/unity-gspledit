using UnityEngine;

namespace UnityEditor.GsplEdit.Utils
{
    public static class TextureLoader
    {
        public static Texture2D Load(string iconName)
        {
            const string iconFolder = "gspledit/Content/Icons/";
            string[] possiblePaths =
            {
                $"Assets/{iconFolder}{iconName}",
                $"Packages/{iconFolder}{iconName}",
                $"{Application.dataPath}/../{iconFolder}{iconName}"
            };

            foreach (string path in possiblePaths)
            {
                Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (icon != null)
                {
                    return icon;
                }
            }

            Debug.LogWarning($"[TextureLoader] Failed to load icon: {iconName}");
            return null;
        }

        public static Texture2D CreateFallbackTexture(Color color)
        {
            Texture2D fallbackTexture = new(64, 64);
            for (int x = 0; x < fallbackTexture.width; x++)
            {
                for (int y = 0; y < fallbackTexture.height; y++)
                {
                    fallbackTexture.SetPixel(x, y, color);
                }
            }
            fallbackTexture.Apply();
            return fallbackTexture;
        }
    }
}
