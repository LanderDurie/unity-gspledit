using UnityEditorInternal;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.GsplEdit;

namespace UnityEditor.GsplEdit
{
    public class SelectionGroupBox : Editor
    {
        private List<ModifierBox> m_Modifiers = new List<ModifierBox>();
        private ReorderableList m_ReorderableList;
        private Vector2 m_ScrollPosition;
        private int m_SelectedIndex = -1;

        public void Init(SelectionGroup group) {

            if (group == null) {
                m_Modifiers = null;
                m_ScrollPosition = new Vector2(0, 0);
                m_SelectedIndex = -1;
                return;
            }
            
            m_Modifiers = new List<ModifierBox>();

            Debug.Log(group.m_Modifiers);

            foreach (Modifier m in group.m_Modifiers) {
                m_Modifiers.Add(CreateInstance<ModifierBox>());
                m_Modifiers[m_Modifiers.Count - 1].Init(group.m_Modifiers[m_Modifiers.Count - 1]);
            }

            // Initialize ReorderableList
            m_ReorderableList = new ReorderableList(m_Modifiers, typeof(Modifier),
                true, false, false, false);

            m_ReorderableList.elementHeight = 24f;

            m_ReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                // Ensure index is valid before accessing
                if (index < 0 || index >= m_Modifiers.Count)
                    return;

                var element = m_Modifiers[index];
                
                // Highlight selected item
                if (index == m_SelectedIndex)
                {
                    EditorGUI.DrawRect(rect, new Color(0.3f, 0.5f, 0.8f, 0.2f));
                }

                // Name field with reduced width and height
                group.m_Modifiers[index].m_Name = EditorGUI.TextField(
                    new Rect(rect.x + 40, rect.y + 4, rect.width - 100, 18),
                    group.m_Modifiers[index].m_Name,
                    EditorStyles.textField
                );

                // Endabled toggle
                group.m_Modifiers[index].m_Enabled = EditorGUI.Toggle(
                    new Rect(rect.x + rect.width - 40 - 4, rect.y + 4, 18, 18), 
                    group.m_Modifiers[index].m_Enabled
                );

                // Remove button
                if (GUI.Button(
                    new Rect(rect.x + rect.width - 18 - 4, rect.y + 4, 18, 18),
                    "x"))
                {
                    if (index >= 0 && index < m_Modifiers.Count)
                    {
                        m_Modifiers.RemoveAt(index);
                        group.m_Modifiers.RemoveAt(index);
                        // Reset selected index if it no longer exists
                        if (m_SelectedIndex >= m_Modifiers.Count)
                        {
                            m_SelectedIndex = -1;
                        }
                    }
                }
            };

            // Reorder handle callback
            m_ReorderableList.drawElementBackgroundCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                if (index < 0 || index >= m_Modifiers.Count)
                    return;

                // Draw reorder handle
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 20, rect.height), new Color(0.5f, 0.5f, 0.5f, 0.2f));
            };

            // Track selected index
            m_ReorderableList.onSelectCallback = (ReorderableList list) =>
            {
                m_SelectedIndex = list.index;
            };
        }

        public void Draw(SelectionGroup group)
        {
            // // Add new modifier button
            if (GUILayout.Button("Add Modifier to group"))
            {
                m_Modifiers.Add(CreateInstance<ModifierBox>());
                group.Insert();
                m_Modifiers[m_Modifiers.Count - 1].Init(group.m_Modifiers[m_Modifiers.Count - 1]);
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
            if (m_SelectedIndex >= 0 && m_SelectedIndex < m_Modifiers.Count)
            {
                m_Modifiers[m_SelectedIndex].Draw(group.m_Modifiers[m_SelectedIndex]);
            }
            else
            {
                EditorGUILayout.LabelField("No Modifier Selected");
            }
        }
    }
}
