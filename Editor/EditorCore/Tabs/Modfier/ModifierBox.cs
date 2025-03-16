using UnityEngine;
using UnityEngine.GsplEdit;
using System;
using System.Linq;

namespace UnityEditor.GsplEdit {
    public class ModifierBox : Editor {
        private Vector2 scrollPosition;
        private ModifierHolder m_ModifierHolder;
        private string[] m_ModifierTypeNames;
        private Type[] m_ModifierTypes;
        private int m_SelectedModifierTypeIndex = -1;

        public void Init(ModifierHolder modifierHolder) {
            m_ModifierHolder = modifierHolder;
            
            // Find all Modifier types in the assemblies
            FindModifierTypes();
            
            // Find the current modifier type in the list
            UpdateSelectedModifierTypeIndex();
        }

        private void FindModifierTypes() {
            // Get all types that derive from Modifier
            m_ModifierTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsClass && !type.IsAbstract && typeof(Modifier).IsAssignableFrom(type))
                .ToArray();

            // Create display names for the dropdown
            m_ModifierTypeNames = m_ModifierTypes
                .Select(type => type.Name)
                .ToArray();
        }

        private void UpdateSelectedModifierTypeIndex() {
            m_SelectedModifierTypeIndex = -1;
            
            if (m_ModifierHolder.m_Modifier != null) {
                Type currentType = m_ModifierHolder.m_Modifier.GetType();
                for (int i = 0; i < m_ModifierTypes.Length; i++) {
                    if (m_ModifierTypes[i] == currentType) {
                        m_SelectedModifierTypeIndex = i;
                        break;
                    }
                }
            }
        }

        public void Draw(ModifierHolder modifierHolder) {
            if (modifierHolder == null) return;
            
            // Update the modifier holder reference (in case it changed)
            m_ModifierHolder = modifierHolder;
            
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            // Modifier Type Selection
            EditorGUILayout.LabelField("Modifier Type", EditorStyles.boldLabel);
            
            // Show dropdown for modifier types
            EditorGUI.BeginChangeCheck();
            int newSelectedIndex = EditorGUILayout.Popup(
                "Type",
                m_SelectedModifierTypeIndex,
                m_ModifierTypeNames
            );
            
            if (EditorGUI.EndChangeCheck() && newSelectedIndex != m_SelectedModifierTypeIndex) {
                // Create a new modifier of the selected type
                m_SelectedModifierTypeIndex = newSelectedIndex;
                CreateNewModifier(m_ModifierTypes[m_SelectedModifierTypeIndex]);
            }
            
            EditorGUILayout.Space();
            
            // Modifier Settings
            if (m_ModifierHolder.m_Modifier != null) {
                EditorGUILayout.LabelField("Modifier Settings", EditorStyles.boldLabel);
                
                // Start ScrollView for settings
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                
                // Draw settings in the ScrollView
                if (m_ModifierHolder.m_Modifier != null) {
                    // Check if the modifier is an Asset or needs to be created
                    if (AssetDatabase.Contains(m_ModifierHolder.m_Modifier)) {
                        EditorGUILayout.ObjectField("Modifier Asset", m_ModifierHolder.m_Modifier, typeof(Modifier), false);
                    } else {
                        EditorGUILayout.HelpBox("This modifier is not saved as an asset. Click 'Save as Asset' to create a reusable modifier.", MessageType.Info);
                        if (GUILayout.Button("Save as Asset")) {
                            SaveModifierAsAsset();
                        }
                    }
                    
                    EditorGUILayout.Space();
                    
                    // Call the modifier's DrawSettings method
                    m_ModifierHolder.m_Modifier.DrawSettings();
                }
                
                EditorGUILayout.EndScrollView();
            } else {
                EditorGUILayout.HelpBox("No modifier selected. Choose a modifier type from the dropdown above.", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }

        private void CreateNewModifier(Type modifierType) {
            // Create a new instance of the selected modifier type
            Modifier newModifier = (Modifier)ScriptableObject.CreateInstance(modifierType);
            newModifier.name = modifierType.Name;
            
            m_ModifierHolder.SetModifier(newModifier);
            
            // Mark the modifier as dirty for saving
            EditorUtility.SetDirty(newModifier);
        }

        private void SaveModifierAsAsset() {
            if (m_ModifierHolder.m_Modifier == null) return;
            
            // Create a path for the asset
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Modifier as Asset",
                m_ModifierHolder.m_Modifier.name + ".asset",
                "asset",
                "Please enter a filename to save the modifier"
            );
            
            if (string.IsNullOrEmpty(path)) return;
            
            // Create the asset
            AssetDatabase.CreateAsset(m_ModifierHolder.m_Modifier, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Update the reference in the ModifierHolder
            m_ModifierHolder.m_Modifier = AssetDatabase.LoadAssetAtPath<Modifier>(path);
            
            // Select the new asset
            Selection.activeObject = m_ModifierHolder.m_Modifier;
        }
    }
}