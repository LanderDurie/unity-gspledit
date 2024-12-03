using UnityEngine;
using UnityEngine.GsplEdit;

namespace UnityEditor.GsplEdit
{
    public class EditorContainer : Editor
    {
        private string[] m_TabNames = { "Object", "Renderer", "Generator", "Modifier", "Material" };
        private uint m_CurrentTabId = 0;
        private Tab[] m_Tabs;

        public static EditorContainer Create()
        {
            EditorContainer instance = CreateInstance<EditorContainer>();

            instance.m_Tabs = new Tab[]
            {
                Tab.Create<ObjectEditorTab>(),
                Tab.Create<RendererEditorTab>(),
                Tab.Create<MeshGenEditorTab>(),
                Tab.Create<ModifierEditorTab>(),
                Tab.Create<MaterialEditorTab>()
            };

            return instance;
        }


        public void Draw(DynamicSplat gs)
        {
            if (gs.GetSplatData() == null)
            {
                EditorGUILayout.LabelField(
                    "Select an asset to use edit mode",
                    new GUIStyle(EditorStyles.label)
                    {
                        alignment = TextAnchor.MiddleCenter
                    },
                    GUILayout.ExpandWidth(true)
                );
                GUILayout.Space(16); 
                GUI.enabled = false;
            }

            m_CurrentTabId = (uint)GUILayout.SelectionGrid((int)m_CurrentTabId, m_TabNames, 4);
            DrawUtils.Separator();
            m_Tabs[m_CurrentTabId].Draw(gs);
        }
    }
}