using UnityEngine;
using UnityEditor;
using UnityEngine.GsplEdit;

namespace UnityEditor.GsplEdit
{
    public class MainEditorWindow : EditorWindow
    {
        private static TabContainer m_TabContainer;
        private static DynamicSplat m_SelectedGS;
        private static EditorContainer m_Editor;
        private static bool m_IsLocked = false; // Flag to lock focus
        private static SelectorTool m_EditorTool;

        [MenuItem("Window/GsplEdit/Editor Window")]
        public static void ShowWindow()
        {
            GetWindow<MainEditorWindow>("GsplEdit Editor");
        }

        private void OnEnable()
        {
            if (m_TabContainer == null)
            {
                m_TabContainer = TabContainer.Create();
                m_TabContainer.hideFlags = HideFlags.HideAndDontSave;
                m_Editor = EditorContainer.Create();
                m_Editor.hideFlags = HideFlags.HideAndDontSave;
                m_EditorTool = CreateInstance<SelectorTool>();
                m_EditorTool.hideFlags = HideFlags.HideAndDontSave;
            }

            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.update += EnforceSelection;

            UpdateSelectedSplat();
        }

        static MainEditorWindow()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }



        private void OnDisable()
        {
            // Unsubscribe from selection changes
            Selection.selectionChanged -= OnSelectionChanged;

            // Unsubscribe from the update loop
            EditorApplication.update -= EnforceSelection;
            SceneView.duringSceneGui -= OnSceneGUI;

            // Clean up resources
            if (m_TabContainer != null)
            {
                DestroyImmediate(m_TabContainer);
                m_TabContainer = null;
                DestroyImmediate(m_Editor);
                m_Editor = null;
            }

            // Unlock focus
            m_IsLocked = false;
        }

        private void OnGUI()
        {
            if (m_SelectedGS == null)
            {
                EditorGUILayout.LabelField(
                    "No DynamicSplat selected.",
                    new GUIStyle(EditorStyles.label)
                    {
                        alignment = TextAnchor.MiddleCenter
                    },
                    GUILayout.ExpandWidth(true)
                );
                return;
            }

            GeneralInfo.Draw(m_SelectedGS, ref m_IsLocked);

            // Draw the default inspector for DynamicSplat
            EditorGUILayout.Space();
            SerializedObject serializedSplat = new SerializedObject(m_SelectedGS);
            SerializedProperty iterator = serializedSplat.GetIterator();
            iterator.NextVisible(true);
            while (iterator.NextVisible(false))
            {
                EditorGUILayout.PropertyField(iterator, true);
            }
            serializedSplat.ApplyModifiedProperties();

            // Draw the tab container for the selected DynamicSplat
            DrawUtils.Separator();
            if (!m_IsLocked)
            {
                GUI.enabled = false;
            }
            m_Editor.Draw(m_SelectedGS);
            GUI.enabled = true;
        }

        private void OnSelectionChanged()
        {
            if (!m_IsLocked || m_SelectedGS == null)
            {
                UpdateSelectedSplat();
            }

            Repaint(); // Refresh the window when the selection changes
        }

        private void UpdateSelectedSplat()
        {
            // Update the selected DynamicSplat if it's part of the current selection
            if (Selection.activeGameObject != null)
            {
                m_SelectedGS = Selection.activeGameObject.GetComponent<DynamicSplat>();
            }
            else
            {
                m_SelectedGS = null;
                m_IsLocked = false;
            }
            
            if (m_Editor != null) {
                m_Editor.Init(m_SelectedGS);
            }
        }

        public static void OnSceneGUI(SceneView sceneView)
        {
            if (m_SelectedGS != null && m_IsLocked)
            {
                // Hide default handle
                Tools.hidden = true;
                SelectorTool.Draw(m_SelectedGS, sceneView);
            }
            else
            {
                Tools.hidden = false;
            }
        }

        private void EnforceSelection()
        {
            if (m_IsLocked && m_SelectedGS != null && Selection.activeGameObject != m_SelectedGS.gameObject)
            {
                if (Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<DynamicSplat>() != null) {
                    m_SelectedGS = Selection.activeGameObject.GetComponent<DynamicSplat>();
                    m_Editor.Init(m_SelectedGS);
                } else {
                // Force the selection back to the selected object
                Selection.activeGameObject = m_SelectedGS.gameObject;
                }
            }
        }
    }
}
