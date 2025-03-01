
using System;
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.GsplEdit{

    public class ModifierFactory
    {
        public enum Type
        {
            Deform,
            Rigging,
            Texture
        }

        private static readonly Dictionary<Type, string> m_ModifierDisplayNames = new Dictionary<Type, string>
        {
            { Type.Deform, "Deformation" },
            { Type.Rigging, "Rigging" }
        };

        public static Dictionary<Type, Func<SharedComputeContext, VertexSelectionGroup, Modifier>> m_Generators = new Dictionary<Type, Func<SharedComputeContext, VertexSelectionGroup, Modifier>>
        {
            { Type.Deform, (context, selectionGroup) => new DeformModifier(ref context, ref selectionGroup) },
            { Type.Rigging, (context, selectionGroup) => new RiggingModifier(ref context, ref selectionGroup) },
            // Add other modifiers here
        };

        private static string[] m_ModifierNames;
        private static Type[] m_ModifierTypes;

        static ModifierFactory()
        {
            m_ModifierNames = new string[m_Generators.Count];
            m_ModifierTypes = new Type[m_Generators.Count];
            int index = 0;
            foreach (var kvp in m_Generators)
            {
                m_ModifierNames[index] = m_ModifierDisplayNames[kvp.Key];
                m_ModifierTypes[index] = kvp.Key; // Store the type
                index++;
            }
        }

        public static void ShowModifierDropdown(Action<Modifier> onModifierSelected, SharedComputeContext context, VertexSelectionGroup selectionGroup)
        {
            // Calculate the width of the dropdown button
            float buttonWidth = GUI.skin.button.CalcSize(new GUIContent("Add Modifier")).x;

            // Custom dropdown implementation
            if (EditorGUILayout.DropdownButton(new GUIContent("Add Modifier"), FocusType.Passive))
            {
                // Create a GenericMenu to display the modifier options
                GenericMenu menu = new GenericMenu();

                // Add modifier options to the menu
                for (int i = 0; i < m_ModifierNames.Length; i++)
                {
                    int index = i; // Capture the index for the closure
                    menu.AddItem(new GUIContent(m_ModifierNames[index]), false, () =>
                    {
                        // Create the selected modifier and invoke the callback
                        Modifier modifier = m_Generators[m_ModifierTypes[index]](context, selectionGroup);
                        onModifierSelected?.Invoke(modifier);
                    });
                }

                // Show the dropdown menu with the same width as the button
                menu.DropDown(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, buttonWidth, 0));
            }
        }
    }
}
