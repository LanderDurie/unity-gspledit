using System.Collections.Generic;
using UnityEngine;
using UnityEditorInternal;
using UnityEngine.GsplEdit;

namespace UnityEditor.GsplEdit
{
    public class ModifierEditorTab : Tab
    {
        private List<SelectionGroupBox> m_SelectionGroups = new List<SelectionGroupBox>();
        private ReorderableList m_ReorderableList;
        private Vector2 m_ScrollPosition;
        private int m_SelectedIndex = -1;
        private ModifierSystem m_ModifierSystem;
        private DynamicSplat m_Splat;

        public override void Init(DynamicSplat gs) {
            m_Splat = gs;
            if (gs == null) {
                m_SelectionGroups = new List<SelectionGroupBox>();
                m_ScrollPosition = new Vector2(0, 0);
                m_SelectedIndex = -1;
                m_ModifierSystem = null;
                return;
            }

            m_ModifierSystem = gs.GetModifierSystem();
            RefreshSelectionGroups();
            InitializeList(gs);
        }

        private void RefreshSelectionGroups() {
            // Clear existing groups
            foreach (var group in m_SelectionGroups) {
                if (group != null) {
                    ScriptableObject.DestroyImmediate(group);
                }
            }
            m_SelectionGroups.Clear();

            // Create new groups based on ModifierSystem
            if (m_ModifierSystem != null) {
                for (int i = 0; i < m_ModifierSystem.m_SelectionGroups.Count; i++) {
                    SelectionGroupBox groupBox = CreateInstance<SelectionGroupBox>();
                    groupBox.Init(m_ModifierSystem.m_SelectionGroups[i]);
                    m_SelectionGroups.Add(groupBox);
                }
            }
        }

        private void InitializeList(DynamicSplat gs) {
            // Initialize ReorderableList
            m_ReorderableList = new ReorderableList(m_SelectionGroups, typeof(SelectionGroupBox),
                true, false, true, true);

            m_ReorderableList.elementHeight = 24f;

            m_ReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                // Ensure index is valid before accessing
                if (index < 0 || index >= m_SelectionGroups.Count)
                    return;

                var element = m_SelectionGroups[index];
                
                // Highlight selected item
                if (index == m_SelectedIndex) {
                    EditorGUI.DrawRect(rect, new Color(0.3f, 0.5f, 0.8f, 0.2f));
                }

                // Name field with reduced width and height
                EditorGUI.BeginChangeCheck();
                string newName = EditorGUI.TextField(
                    new Rect(rect.x + 40, rect.y + 4, rect.width - 100, 18),
                    m_ModifierSystem.m_SelectionGroups[index].GetName(),
                    EditorStyles.textField
                );
                if (EditorGUI.EndChangeCheck()) {
                    m_ModifierSystem.m_SelectionGroups[index].SetName(newName);
                }
                
                // Enabled toggle
                EditorGUI.BeginChangeCheck();
                bool newEnabled = EditorGUI.Toggle(
                    new Rect(rect.x + rect.width - 40 - 4, rect.y + 4, 18, 18), 
                    m_ModifierSystem.m_SelectionGroups[index].IsEnabled()
                );
                if (EditorGUI.EndChangeCheck()) {
                    m_ModifierSystem.m_SelectionGroups[index].SetEnabled(newEnabled);
                }

                // Remove button
                if (GUI.Button(
                    new Rect(rect.x + rect.width - 18 - 4, rect.y + 4, 18, 18),
                    "x"))
                {
                    if (index >= 0 && index < m_SelectionGroups.Count) {
                        // Remove using ModifierSystem's method
                        m_ModifierSystem.Remove((uint)index);
                        
                        // Refresh the UI
                        RefreshSelectionGroups();
                        
                        // Reset selected index if it no longer exists
                        if (m_SelectedIndex >= m_SelectionGroups.Count) {
                            m_SelectedIndex = -1;
                        }
                    }
                }
            };

            // Reorder handle callback
            m_ReorderableList.drawElementBackgroundCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                if (index < 0 || index >= m_SelectionGroups.Count)
                    return;

                // Draw reorder handle
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 20, rect.height), new Color(0.5f, 0.5f, 0.5f, 0.2f));
            };

            // Track selected index
            m_ReorderableList.onSelectCallback = (ReorderableList list) => {
                m_SelectedIndex = list.index;
                if (m_SelectedIndex >= 0 && m_SelectedIndex < m_ModifierSystem.m_SelectionGroups.Count) {
                    m_Splat.GetMesh().SetVertexGroup(m_ModifierSystem.m_SelectionGroups[m_SelectedIndex].m_Selection);
                }
            };

            // Reorder Callback
            m_ReorderableList.onReorderCallbackWithDetails = (ReorderableList list, int oldIndex, int newIndex) => {
                if (oldIndex != newIndex) {
                    gs.GetModifierSystem().Reorder((uint)oldIndex, (uint)newIndex);
                }

                // Refresh the UI
                RefreshSelectionGroups();
            };

            // Add callback
            m_ReorderableList.onAddCallback = (ReorderableList list) => {
                SelectionGroup newGroup = m_ModifierSystem.Insert();
                RefreshSelectionGroups();
                m_SelectedIndex = m_SelectionGroups.Count - 1;
            };

            // Remove callback
            m_ReorderableList.onRemoveCallback = (ReorderableList list) => {
                if (list.index >= 0 && list.index < m_SelectionGroups.Count) {
                    m_ModifierSystem.Remove((uint)list.index);
                    RefreshSelectionGroups();
                    if (m_SelectedIndex >= m_SelectionGroups.Count) {
                        m_SelectedIndex = -1;
                    }
                }
            };
        }

        public override void Draw(DynamicSplat gs)
        {
            GUILayout.Label("Modifiers", EditorStyles.boldLabel);

            if (gs == null) {
                EditorGUILayout.HelpBox("No DynamicSplat selected", MessageType.Warning);
                return;
            }

            if (gs.GetMesh() == null) {
                EditorGUILayout.HelpBox("No mesh has been created.", MessageType.Warning);
                return;
            }

            ModifierSystem ms = gs.GetModifierSystem();
            
            // Check if ModifierSystem has changed
            if (ms != m_ModifierSystem) {
                m_ModifierSystem = ms;
                RefreshSelectionGroups();
                InitializeList(gs);
            }
            
            // Also check if the ModifierSystem has a different count of groups
            if (m_ModifierSystem != null && m_SelectionGroups.Count != m_ModifierSystem.m_SelectionGroups.Count) {
                RefreshSelectionGroups();
                InitializeList(gs);
            }

            if (m_ReorderableList == null || ms == null) {
                EditorGUILayout.HelpBox("Missing ModifierSystem", MessageType.Warning);
                return;
            }

            GUILayout.Space(10); // Top spacing

            // First row: Full-width buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Bake Snapshot", GUILayout.ExpandWidth(true))) {
                m_ModifierSystem.BakeSnapshot();
            }
            if (GUILayout.Button("Enable All", GUILayout.ExpandWidth(true))) {
                m_ModifierSystem.EnableAllGroups();
            }
            if (GUILayout.Button("Disable All", GUILayout.ExpandWidth(true))) {
                m_ModifierSystem.DisableAllGroups();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5); // Spacing between rows

            if (GUILayout.Button("Add Group", GUILayout.ExpandWidth(true))) {
                m_ModifierSystem.Insert();
                RefreshSelectionGroups();
                m_SelectedIndex = m_SelectionGroups.Count - 1;
            }

            // Scroll view for ReorderableList with forced scrollbar
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition,
                GUILayout.Height(120),
                GUILayout.ExpandWidth(true));

            // Ensure the list takes up full width and adjusts height dynamically
            Rect listRect = EditorGUILayout.GetControlRect(false,
                m_ReorderableList.GetHeight(),
                GUILayout.ExpandWidth(true));
           
            // Draw the ReorderableList
            m_ReorderableList.DoList(listRect);

            EditorGUILayout.EndScrollView();

            // Display selected item name
            if (m_SelectedIndex >= 0 && m_SelectedIndex < m_SelectionGroups.Count) {
                m_SelectionGroups[m_SelectedIndex].Draw(gs, ms.m_SelectionGroups[m_SelectedIndex]);
            } else {
                EditorGUILayout.LabelField("No Group Selected");
            }
        }
    }
}