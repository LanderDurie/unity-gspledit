using UnityEngine;
using UnityEditor;

namespace UnityEditor.GsplEdit
{
    public class TabHandle : Editor
    {
        private Texture2D m_EnableIcon;
        private Texture2D m_DisableIcon;
        private uint m_TabIndex;
        private GUIStyle m_ActiveStyle;
        private GUIStyle m_HoverStyle;
        private GUIStyle m_DefaultStyle;
        private Rect m_ButtonRect;
        private bool m_Hover = false;

        public static TabHandle Create(uint index, Texture2D enabledIcon, Texture2D disabledIcon)
        {
            TabHandle instance = CreateInstance<TabHandle>();
            instance.m_TabIndex = index;
            instance.m_EnableIcon = enabledIcon;
            instance.m_DisableIcon = disabledIcon;
            instance.m_ButtonRect = new Rect(0, index * 30, 25, 25);
            instance.InitializeStyles();
            return instance;
        }

        private void InitializeStyles()
        {
            // Default style: Transparent background
            m_DefaultStyle = new GUIStyle()
            {
                normal = { background = CreateSolidColorTexture(new Color(0, 0, 0, 0)) },
                border = new RectOffset(4, 4, 4, 4),
                padding = new RectOffset(2, 2, 2, 2)
            };

            // Hover style: Light gray background
            m_HoverStyle = new GUIStyle()
            {
                normal = { background = CreateSolidColorTexture(new Color(0, 0, 0, 0.3f)) },
                border = new RectOffset(4, 4, 4, 4),
                padding = new RectOffset(2, 2, 2, 2)
            };

            // Active style (clicked): Darker gray background
            m_ActiveStyle = new GUIStyle()
            {
                normal = { background = CreateSolidColorTexture(new Color(0, 0, 0, 0.7f)) },
                border = new RectOffset(4, 4, 4, 4),
                padding = new RectOffset(2, 2, 2, 2)
            };
        }

        // Utility method to create a texture of a single color
        private Texture2D CreateSolidColorTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        public uint Draw(uint selectedIndex)
        {
            bool isSelected = selectedIndex == m_TabIndex;
            Texture2D icon = isSelected ? m_EnableIcon : m_DisableIcon;

            if (icon == null)
            {
                Debug.LogError($"Icon for tab {m_TabIndex} is null!");
                return selectedIndex;
            }

            bool isHovered = m_ButtonRect.Contains(Event.current.mousePosition);
            bool isPressed = Event.current.type == EventType.MouseDown && isHovered;

            if (isPressed)
            {
                selectedIndex = m_TabIndex;
                EditorWindow.focusedWindow?.Repaint();
            }

            if (isHovered != m_Hover)
            {
                m_Hover = isHovered;
                EditorWindow.focusedWindow?.Repaint();
            }

            if (isSelected)
            {
                GUI.Box(m_ButtonRect, new GUIContent(icon), m_ActiveStyle);
            }
            else if (isHovered)
            {
                GUI.Box(m_ButtonRect, new GUIContent(icon), m_HoverStyle);
            }
            else
            {
                GUI.Box(m_ButtonRect, new GUIContent(icon), m_DefaultStyle);
            }

            // Repaint the window to ensure it reflects the UI changes
            if (Event.current.type == EventType.Repaint)
            {
                EditorWindow.focusedWindow?.Repaint();
            }

            return selectedIndex;
        }

    }
}
