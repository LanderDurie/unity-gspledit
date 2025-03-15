using UnityEngine;
using UnityEngine.GsplEdit;

namespace UnityEditor.GsplEdit {
    public class ObjectEditorTab : Tab {
        public override void Init(DynamicSplat gs) {}

        public override void Draw(DynamicSplat gs) {

            GUILayout.Space(10);

            // Transform fields
            GUILayout.Label("Transform", EditorStyles.boldLabel);

            // Position
            Vector3 position = EditorGUILayout.Vector3Field("Position", gs.transform.position);
            if (position != gs.transform.position) {
                Undo.RecordObject(gs.transform, "Change Position");
                gs.transform.position = position;
            }

            // Rotation
            Vector3 rotation = EditorGUILayout.Vector3Field("Rotation", gs.transform.rotation.eulerAngles);
            if (rotation != gs.transform.rotation.eulerAngles) {
                Undo.RecordObject(gs.transform, "Change Rotation");
                gs.transform.rotation = Quaternion.Euler(rotation);
            }

            // Scale
            Vector3 scale = EditorGUILayout.Vector3Field("Scale", gs.transform.localScale);
            if (scale != gs.transform.localScale) {
                Undo.RecordObject(gs.transform, "Change Scale");
                gs.transform.localScale = scale;
            }
        }
    }
}
