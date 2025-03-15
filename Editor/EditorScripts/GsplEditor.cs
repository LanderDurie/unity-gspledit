using System.Collections.Generic;
using UnityEngine;
using UnityEngine.GsplEdit;

namespace UnityEditor.GsplEdit {
    [CustomEditor(typeof(DynamicSplat))]
    public class GaussianSplatEditor : Editor {
        private static Dictionary<int, SplatData> s_PersistentSplatData = new Dictionary<int, SplatData>();

        private void OnEnable() {
            // Store the current SplatData when entering play mode
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
        }

        private void OnDisable() {
            // Clean up event subscription
            EditorApplication.playModeStateChanged -= PlayModeStateChanged;
        }

        private void PlayModeStateChanged(PlayModeStateChange state) {
            if (target == null) return;
            
            DynamicSplat gs = target as DynamicSplat;
            if (gs == null) return;
            
            int instanceID = gs.GetInstanceID();
            
            // Save data before entering play mode
            if (state == PlayModeStateChange.ExitingEditMode) {
                try {
                    SplatData currentData = gs.GetContext().gsSplatData;
                    if (currentData != null) {
                        s_PersistentSplatData[instanceID] = currentData;
                    }
                } catch (System.Exception) {
                    // Safely handle any exceptions when getting data
                }
            } else if (state == PlayModeStateChange.EnteredEditMode) {
                if (s_PersistentSplatData.TryGetValue(instanceID, out SplatData savedData) && savedData != null) {
                    // Ensure we're not in play mode when restoring
                    if (!EditorApplication.isPlaying) {
                        try {
                            gs.LoadGS(savedData);
                        } catch (System.Exception) {
                            // Safely handle any exceptions when loading data
                        }
                    }
                }
            }
        }

        public override void OnInspectorGUI() {
            // Safety check for target
            if (target == null)
                return;
                
            DynamicSplat gs = target as DynamicSplat;
            if (gs == null)
                return;
                
            serializedObject.Update();
            
            SplatData currentData = gs.GetContext().gsSplatData;

            
            EditorGUI.BeginChangeCheck();
            SplatData newData = (SplatData)EditorGUILayout.ObjectField(
                "SplatData",
                currentData,
                typeof(SplatData),
                false
            );
            
            if (EditorGUI.EndChangeCheck() && newData != currentData) {
                Undo.RecordObject(gs, "Change SplatData");
                try {
                    gs.LoadGS(newData);

                    // Also update our persistent cache
                    if (newData != null) {
                        s_PersistentSplatData[gs.GetInstanceID()] = newData;
                    }
                    
                    EditorUtility.SetDirty(gs);
                } catch (System.Exception e) {
                    Debug.LogWarning($"Failed to load SplatData {e}");
                }
            }
            
            EditorGUILayout.Space();
            
            // "Open Editor" button
            if (GUILayout.Button("Open Editor")) {
                try {
                    MainEditorWindow.ShowWindow();
                } catch (System.Exception e) {
                    Debug.LogException(e);
                }
            }
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}