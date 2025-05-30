using UnityEngine;
using UnityEngine.GsplEdit;

namespace UnityEditor.GsplEdit {
    public class EditorContainer : Editor {
        private string[] m_TabNames = { "Object", "Renderer", "Generator", "Modifier", "Material" };
        private uint m_CurrentTabId = 0;
        private Tab[] m_Tabs;
        private Vector2 m_ScrollPosition;


        public static EditorContainer Create() {
            EditorContainer instance = CreateInstance<EditorContainer>();
            instance.m_Tabs = new Tab[] {
                Tab.Create<ObjectEditorTab>(),
                Tab.Create<RendererEditorTab>(),
                Tab.Create<MeshGenEditorTab>(),
                Tab.Create<ModifierEditorTab>(),
            };

            return instance;
        }

        public void Init(DynamicSplat gs) {
            foreach(Tab tab in m_Tabs) {
                tab.Init(gs);
            }
        }


        public void Draw(DynamicSplat gs) {
            if (gs.GetContext().gsSplatData == null) {
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

            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);
            m_Tabs[m_CurrentTabId].Draw(gs);
            EditorGUILayout.EndScrollView();
        }
    }
}