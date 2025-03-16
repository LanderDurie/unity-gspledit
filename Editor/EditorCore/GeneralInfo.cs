using UnityEngine;
using UnityEngine.GsplEdit;

namespace UnityEditor.GsplEdit {
    public class GeneralInfo : Editor {
        public static void Draw(DynamicSplat gs, ref bool isLocked) {
            GUILayout.Label("General Info", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Selected Splat: {gs.name}");
            SharedComputeContext context = gs.GetContext();

            SplatData data = (SplatData)EditorGUILayout.ObjectField(
                "SplatData",
                context.gsSplatData,
                typeof(SplatData),
                true
            );

            if (data != null && data != context.gsSplatData) {
                gs.LoadGS(data);
                EditorUtility.SetDirty(gs);
            }

            if (context.gsSplatData == null) {
                return;
            }

            EditorGUILayout.LabelField($"Gaussian Count: {context.gsSplatData.splatCount}");
            
            if (isLocked) {
                if (GUILayout.Button("Exit Edit Mode")) {
                    isLocked = false;
                }
            } else {
                if (GUILayout.Button("Enter Edit Mode")) {
                    isLocked = true;
                }
            }
        }
    }
}
