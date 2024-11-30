using UnityEngine;
using UnityEngine.GsplEdit;

namespace UnityEditor.GsplEdit
{
    public class MainEditorWindow : EditorWindow
    {
        private TabContainer m_TabContainer;
        private DynamicSplat m_SelectedGS; // Track the selected DynamicSplat
        private EditorContainer m_Editor;

        [MenuItem("Window/GsplEdit/Editor Window")]
        public static void ShowWindow()
        {
            GetWindow<MainEditorWindow>("GsplEdit Editor");
        }

        private void OnEnable()
        {
            // Create TabContainer if it doesn't exist
            if (m_TabContainer == null)
            {
                m_TabContainer = TabContainer.Create();
                m_TabContainer.hideFlags = HideFlags.HideAndDontSave;
                m_Editor = EditorContainer.Create();
                m_Editor.hideFlags = HideFlags.HideAndDontSave;            
            }

            // Subscribe to selection changes
            Selection.selectionChanged += OnSelectionChanged;
            UpdateSelectedSplat(); // Initial update
        }

        private void OnDisable()
        {
            // Unsubscribe from selection changes
            Selection.selectionChanged -= OnSelectionChanged;

            // Clean up resources
            if (m_TabContainer != null)
            {
                DestroyImmediate(m_TabContainer);
                m_TabContainer = null;
                DestroyImmediate(m_Editor);
                m_Editor = null;
            }
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

            GeneralInfo.Draw(m_SelectedGS);

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
            // EditorGUILayout.BeginVertical(GUILayout.Width(25));
            // m_TabContainer.Draw(m_SelectedGS);
            // EditorGUILayout.EndVertical();
            DrawUtils.Separator();
            m_Editor.Draw(m_SelectedGS);
        }

        private void OnSelectionChanged()
        {
            UpdateSelectedSplat();
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
            }
        }
    }
}
