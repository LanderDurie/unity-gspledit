using System.Collections.Generic;
using UnityEngine;
using UnityEngine.GsplEdit;

namespace UnityEditor.GsplEdit
{
    public class TabContainer : Editor
    {
        private TabHandle[] m_TabHandles;
        private Tab[] m_Tabs;
        private uint m_CurrentTabId = 0;

        public static TabContainer Create()
        {
            TabContainer instance = CreateInstance<TabContainer>();

            Dictionary<string, string> iconPaths = new()
            {
                { "ObjectOn", "ObjectIconOn.png" },
                { "ObjectOff", "ObjectIconOff.png" },
                { "RiggingOn", "ArmatureOn.png" },
                { "RiggingOff", "ArmatureOff.png" },
                { "Material", "Material.png" },
                { "DownArrow", "SplatIcon.png" },
                { "Renderer", "Renderer.png" },

            };

            // Loop through the dictionary and load icons
            Dictionary<string, Texture2D> m_Icons = new();

            foreach (var iconEntry in iconPaths)
            {
                string iconName = iconEntry.Key;
                string iconFileName = iconEntry.Value;

                // Attempt to load the icon, or create a fallback texture if the icon is missing
                Texture2D icon = Utils.TextureLoader.Load(iconFileName) ?? Utils.TextureLoader.CreateFallbackTexture(Color.black);

                // Add the icon to the dictionary
                m_Icons.Add(iconName, icon);
            }

            instance.m_TabHandles = new TabHandle[]
            {
                TabHandle.Create(0, m_Icons["ObjectOn"], m_Icons["ObjectOff"]),
                TabHandle.Create(1, m_Icons["ObjectOn"], m_Icons["ObjectOff"]),
                TabHandle.Create(2, m_Icons["ObjectOn"], m_Icons["ObjectOff"])
            };

            instance.m_Tabs = new Tab[]
            {
                Tab.Create<ObjectEditorTab>(),
                Tab.Create<ModifierEditorTab>(),
                Tab.Create<MaterialEditorTab>()
            };

            return instance;
        }

        public void Draw(DynamicSplat gs)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(30));
            for (int i = 0; i < m_TabHandles.Length; i++)
            {
                m_CurrentTabId = m_TabHandles[i].Draw(m_CurrentTabId);
            }
            EditorGUILayout.EndVertical();

            m_Tabs[m_CurrentTabId].Draw(gs);
        }
    }
}
