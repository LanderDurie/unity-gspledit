using System;
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.GsplEdit {
    public class ModifierSystem {
        public List<SelectionGroup> m_SelectionGroups;
        private EditableMesh m_Mesh;
        private SharedComputeContext m_Context;
        public enum Type {
            Deform,
            Rigging,
            Texture
        }
        public static Dictionary<Type, Func<SharedComputeContext, VertexSelectionGroup, Modifier>> m_Generators = new Dictionary<Type, Func<SharedComputeContext, VertexSelectionGroup, Modifier>> {
            { Type.Deform, (context, selectionGroup) => new DeformModifier(ref context, ref selectionGroup) },
            // { Type.Rigging, (context, selectionGroup) => new RiggingModifier(ref context, ref selectionGroup) },
        };

        private static readonly Dictionary<Type, string> m_ModifierDisplayNames = new Dictionary<Type, string> {
            { Type.Deform, "Deformation" },
            { Type.Rigging, "Rigging" }
        };
        private static string[] m_ModifierNames;
        private static Type[] m_ModifierTypes;

        public ModifierSystem(ref SharedComputeContext context) {
            m_Context = context;
            m_SelectionGroups = new List<SelectionGroup>();
            
            m_ModifierNames = new string[m_Generators.Count];
            m_ModifierTypes = new Type[m_Generators.Count];
            int index = 0;
            foreach (var kvp in m_Generators) {
                m_ModifierNames[index] = m_ModifierDisplayNames[kvp.Key];
                m_ModifierTypes[index] = kvp.Key; // Store the type
                index++;
            }
        }

        public void SetMesh(ref EditableMesh mesh) {
            m_Mesh = mesh;
        }

        public void Insert() {
            m_SelectionGroups.Add(new SelectionGroup(ref m_Mesh));
        }

        public void Remove(uint id) {
            if (id >= m_SelectionGroups.Count) {
                Debug.LogWarning($"Invalid Id: {id}. No modifier at this position.");
                return;
            }

            m_SelectionGroups.RemoveAt((int)id);
        }

        public void Reorder(uint fromId, uint toId) {
            if (fromId >= m_SelectionGroups.Count || toId >= m_SelectionGroups.Count) {
                Debug.LogWarning($"Invalid Id(s). FromId: {fromId}, ToId: {toId}. Out of range.");
                return;
            }

            SelectionGroup modifier = m_SelectionGroups[(int)fromId];
            m_SelectionGroups.RemoveAt((int)fromId);

            if (toId > fromId) // Adjust for the shift caused by the removal
                toId--;

            m_SelectionGroups.Insert((int)toId, modifier);
        }

        public void RunAll() {
            for (int i = 0; i < m_SelectionGroups.Count; i++) {
                if (m_SelectionGroups[i].m_Enabled) {
                    m_SelectionGroups[i].RunAll();
                }
            }
            UpdateMesh();
        }

        public void RunGroup(int groupId) {
            m_SelectionGroups[groupId].RunAll();
            UpdateMesh();
        }

        public void RunModifier(int groupId, int modId) {
            m_SelectionGroups[groupId].RunModifier(modId);
            UpdateMesh();
        }

        private void UpdateMesh() {
            Vector3[] vertices = new Vector3[m_Context.gpuMeshModVertex.count];
            m_Context.gpuMeshModVertex.GetData(vertices);
            m_Context.scaffoldMesh.vertices = vertices;
            m_Context.scaffoldMesh.RecalculateNormals();
            m_Context.scaffoldMesh.RecalculateBounds();
        }

        public void ShowModifierDropdown(Action<Modifier> onModifierSelected, SelectionGroup selectionGroup) {
            // Calculate the width of the dropdown button
            float buttonWidth = GUI.skin.button.CalcSize(new GUIContent("Add Modifier")).x;

            // Custom dropdown implementation
            if (EditorGUILayout.DropdownButton(new GUIContent("Add Modifier"), FocusType.Passive)) {
                // Create a GenericMenu to display the modifier options
                GenericMenu menu = new GenericMenu();

                // Add modifier options to the menu
                for (int i = 0; i < m_ModifierNames.Length; i++) {
                    int index = i; // Capture the index for the closure
                    menu.AddItem(new GUIContent(m_ModifierNames[index]), false, () => {
                        // Create the selected modifier and invoke the callback
                        Modifier modifier = m_Generators[m_ModifierTypes[index]](m_Context, selectionGroup.m_Selection);
                        onModifierSelected?.Invoke(modifier);
                    });
                }

                // Show the dropdown menu with the same width as the button
                menu.DropDown(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, buttonWidth, 0));
            }
        }
    }
}
