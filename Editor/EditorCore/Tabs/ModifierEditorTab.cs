using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.GsplEdit;

namespace UnityEditor.GsplEdit
{
    public class ModifierEditorTab : Tab
    {
        private static List<SelectionGroupBox> m_SelectionGroups = new List<SelectionGroupBox>();
        private static  ReorderableList m_ReorderableList;
        private static Vector2 m_ScrollPosition;
        private int m_SelectedIndex = -1;

        public override void Init(DynamicSplat gs) {

            if (gs == null) {
                m_SelectionGroups = null;
                m_ScrollPosition = new Vector2(0, 0);
                m_SelectedIndex = -1;
                return;
            }

            ModifierSystem ms = gs.GetModifierSystem();

            m_SelectionGroups = new List<SelectionGroupBox>();

            foreach (SelectionGroup group in ms.m_SelectionGroups) {
                m_SelectionGroups.Add(CreateInstance<SelectionGroupBox>());
                m_SelectionGroups[m_SelectionGroups.Count-1].Init(ms.m_SelectionGroups[m_SelectionGroups.Count-1]);
            }

            // Initialize ReorderableList
            m_ReorderableList = new ReorderableList(m_SelectionGroups, typeof(SelectionGroupBox),
                true, false, false, false);

            m_ReorderableList.elementHeight = 24f;

            m_ReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                // Ensure index is valid before accessing
                if (index < 0 || index >= m_SelectionGroups.Count)
                    return;

                var element = m_SelectionGroups[index];
                
                // Highlight selected item
                if (index == m_SelectedIndex)
                {
                    EditorGUI.DrawRect(rect, new Color(0.3f, 0.5f, 0.8f, 0.2f));
                }

                // Name field with reduced width and height
                ms.m_SelectionGroups[index].m_Name = EditorGUI.TextField(
                    new Rect(rect.x + 40, rect.y + 4, rect.width - 100, 18),
                    ms.m_SelectionGroups[index].m_Name,
                    EditorStyles.textField
                );

                
                // Endabled toggle
                ms.m_SelectionGroups[index].m_Enabled = EditorGUI.Toggle(
                    new Rect(rect.x + rect.width - 40 - 4, rect.y + 4, 18, 18), 
                    ms.m_SelectionGroups[index].m_Enabled
                );


                // Remove button
                if (GUI.Button(
                    new Rect(rect.x + rect.width - 18 - 4, rect.y + 4, 18, 18),
                    "x"))
                {
                    if (index >= 0 && index < m_SelectionGroups.Count)
                    {
                        m_SelectionGroups.RemoveAt(index);
                        ms.m_SelectionGroups.RemoveAt(index);
                        // Reset selected index if it no longer exists
                        if (m_SelectedIndex >= m_SelectionGroups.Count)
                        {
                            m_SelectedIndex = -1;
                        }
                    }
                }
            };

            // Reorder handle callback
            m_ReorderableList.drawElementBackgroundCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                if (index < 0 || index >= m_SelectionGroups.Count)
                    return;

                // Draw reorder handle
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 20, rect.height), new Color(0.5f, 0.5f, 0.5f, 0.2f));
            };

            // Track selected index
            m_ReorderableList.onSelectCallback = (ReorderableList list) =>
            {
                m_SelectedIndex = list.index;
                gs.SetVertexGroup(ms.m_SelectionGroups[m_SelectedIndex].m_Selection);
            };
        }

        public override void Draw(DynamicSplat gs)
        {
            GUILayout.Label("Modifiers", EditorStyles.boldLabel);

            ModifierSystem ms = gs.GetModifierSystem();

            if (m_ReorderableList == null || ms == null || m_SelectionGroups == null)
                return;

            // if (GUILayout.Button("Run All"))
            // {
            //     ms.RunAll();
            // }

            // Add new modifier button
            if (GUILayout.Button("Add Group"))
            {
                m_SelectionGroups.Add(CreateInstance<SelectionGroupBox>());
                ms.Insert();
                m_SelectionGroups[m_SelectionGroups.Count - 1].Init(ms.m_SelectionGroups[m_SelectionGroups.Count - 1]);
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
            if (m_SelectedIndex >= 0 && m_SelectedIndex < m_SelectionGroups.Count)
            {
                m_SelectionGroups[m_SelectedIndex].Draw(ms.m_SelectionGroups[m_SelectedIndex]);
            }
            else
            {
                EditorGUILayout.LabelField("No Group Selected");
            }
        }
    }
}