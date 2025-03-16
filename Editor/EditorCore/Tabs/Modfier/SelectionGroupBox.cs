using UnityEditorInternal;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.GsplEdit;

namespace UnityEditor.GsplEdit {
    public class SelectionGroupBox : Editor {
        private List<ModifierBox> m_Modifiers = new List<ModifierBox>();
        private ReorderableList m_ReorderableList;
        private Vector2 m_ScrollPosition;
        private int m_SelectedIndex = -1;
        private SelectionGroup m_Group;

        public void Init(SelectionGroup group) {
            m_Group = group;

            if (group == null) {
                m_Modifiers = new List<ModifierBox>();
                m_ScrollPosition = new Vector2(0, 0);
                m_SelectedIndex = -1;
                return;
            }
            
            RefreshModifiers();
            InitializeList(group);
        }

        private void RefreshModifiers() {
            // Clear existing modifiers
            foreach (var modifier in m_Modifiers) {
                if (modifier != null) {
                    ScriptableObject.DestroyImmediate(modifier);
                }
            }
            m_Modifiers.Clear();

            // Create new modifiers based on SelectionGroup
            if (m_Group != null) {
                for (int i = 0; i < m_Group.m_Modifiers.Count; i++) {
                    ModifierBox modifierBox = CreateInstance<ModifierBox>();
                    modifierBox.Init(m_Group.m_Modifiers[i]);
                    m_Modifiers.Add(modifierBox);
                }
            }
        }

        private void InitializeList(SelectionGroup group) {
            // Initialize ReorderableList
            m_ReorderableList = new ReorderableList(m_Modifiers, typeof(ModifierBox),
                true, false, true, true);

            m_ReorderableList.elementHeight = 24f;

            m_ReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                // Ensure index is valid before accessing
                if (index < 0 || index >= m_Modifiers.Count)
                    return;

                var element = m_Modifiers[index];
                
                // Highlight selected item
                if (index == m_SelectedIndex) {
                    EditorGUI.DrawRect(rect, new Color(0.3f, 0.5f, 0.8f, 0.2f));
                }

                // Name field with reduced width and height
                EditorGUI.BeginChangeCheck();
                string newName = EditorGUI.TextField(
                    new Rect(rect.x + 40, rect.y + 4, rect.width - 100, 18),
                    group.m_Modifiers[index].m_Meta.name,
                    EditorStyles.textField
                );
                if (EditorGUI.EndChangeCheck()) {
                    group.m_Modifiers[index].m_Meta.name = newName;
                }

                // Enabled toggle
                EditorGUI.BeginChangeCheck();
                bool newEnabled = EditorGUI.Toggle(
                    new Rect(rect.x + rect.width - 40 - 4, rect.y + 4, 18, 18), 
                    group.m_Modifiers[index].m_Meta.enabled
                );
                if (EditorGUI.EndChangeCheck()) {
                    group.m_Modifiers[index].m_Meta.enabled = newEnabled;
                }

                // Remove button
                if (GUI.Button(
                    new Rect(rect.x + rect.width - 18 - 4, rect.y + 4, 18, 18),
                    "x")) {
                    if (index >= 0 && index < m_Modifiers.Count) {
                        group.m_Modifiers.RemoveAt(index);
                        
                        // Refresh the UI
                        RefreshModifiers();
                        
                        // Reset selected index if it no longer exists
                        if (m_SelectedIndex >= m_Modifiers.Count) {
                            m_SelectedIndex = -1;
                        }
                    }
                }
            };

            // Reorder handle callback
            m_ReorderableList.drawElementBackgroundCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                if (index < 0 || index >= m_Modifiers.Count)
                    return;

                // Draw reorder handle
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 20, rect.height), new Color(0.5f, 0.5f, 0.5f, 0.2f));
            };

            // Track selected index
            m_ReorderableList.onSelectCallback = (ReorderableList list) => {
                m_SelectedIndex = list.index;
            };

            // Reorder Callback
            m_ReorderableList.onReorderCallbackWithDetails = (ReorderableList list, int oldIndex, int newIndex) => {
                if (oldIndex != newIndex) {
                    group.Reorder((uint)oldIndex, (uint)newIndex);
                }

                // Refresh the UI
                RefreshModifiers();
            };

            // Add callback
            m_ReorderableList.onAddCallback = (ReorderableList list) => {
                ModifierHolder mh = group.AddModifier();
                RefreshModifiers();
                m_SelectedIndex = m_Modifiers.Count - 1;
            };

            // Remove callback
            m_ReorderableList.onRemoveCallback = (ReorderableList list) => {
                if (list.index >= 0 && list.index < m_Modifiers.Count) {
                    group.m_Modifiers.RemoveAt(list.index);
                    RefreshModifiers();
                    if (m_SelectedIndex >= m_Modifiers.Count) {
                        m_SelectedIndex = -1;
                    }
                }
            };
        }

        public void Draw(DynamicSplat gs, SelectionGroup group) {
            // Check if SelectionGroup has changed
            if (group != m_Group) {
                m_Group = group;
                RefreshModifiers();
                InitializeList(group);
            }
            
            // Also check if the SelectionGroup has a different count of modifiers
            if (m_Group != null && m_Modifiers.Count != m_Group.m_Modifiers.Count) {
                RefreshModifiers();
                InitializeList(group);
            }

                        GUILayout.Space(10); // Top spacing

            // First row: Full-width buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Enable All", GUILayout.ExpandWidth(true))) {
                group.EnableAllModifiers();
            }
            if (GUILayout.Button("Disable All", GUILayout.ExpandWidth(true))) {
                group.DisableAllModifiers();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5); // Spacing between rows

            if (GUILayout.Button("Add Modifier")) {
                ModifierHolder mh = group.AddModifier();
                RefreshModifiers();
                m_SelectedIndex = m_Modifiers.Count - 1;
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
            if (m_SelectedIndex >= 0 && m_SelectedIndex < m_Modifiers.Count) {
                m_Modifiers[m_SelectedIndex].Draw(group.m_Modifiers[m_SelectedIndex]);
            } else {
                EditorGUILayout.LabelField("No Modifier Selected");
            }
        }
    }
}