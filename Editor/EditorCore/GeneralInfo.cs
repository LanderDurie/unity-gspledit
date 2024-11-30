using UnityEngine;
using UnityEngine.GsplEdit;

namespace UnityEditor.GsplEdit
{
    public class GeneralInfo : Editor
    {
        public static void Draw(DynamicSplat gs)
        {
            GUILayout.Label("General Info", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Selected Splat: {gs.name}");

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

            if (gs.GetSplatData() == null) {
                return;
            }

            EditorGUILayout.LabelField($"Gaussian Count: {gs.GetSplatData().splatCount}");
        }
    }
}
