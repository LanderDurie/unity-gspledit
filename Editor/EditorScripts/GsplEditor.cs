using System.Collections.Generic;
using UnityEngine;
using UnityEngine.GsplEdit;

namespace UnityEditor.GsplEdit
{
        [CustomEditor(typeof(DynamicSplat))]
        public class GaussianSplatEditor : Editor
        {
                public override void OnInspectorGUI()
                {
                        DynamicSplat gs = target as DynamicSplat;
                        if (!gs)
                                return;

                        serializedObject.Update();

                        GUILayout.Label("Object", EditorStyles.boldLabel);
                        SplatData data = (SplatData)EditorGUILayout.ObjectField(
                            "SplatData",
                            gs.GetSplatData(),
                            typeof(SplatData),
                            true
                        );

                        if (data != gs.GetSplatData())
                        {
                                gs.LoadGS(data);
                                EditorUtility.SetDirty(gs);
                        }

                        if (GUILayout.Button("Open Editor"))
                        {
                                MainEditorWindow.ShowWindow();
                        }

                        serializedObject.ApplyModifiedProperties();
                }
        }
}
