using UnityEditor;

namespace UnityEngine.GsplEdit {
    [ExecuteInEditMode]
    public class FPSCounter : MonoBehaviour {
        private float deltaTime = 0.0f;

        void Update() {
            deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
            SceneView.RepaintAll();
        }

        void OnDrawGizmos() {
            Handles.BeginGUI();

            int fps = Mathf.CeilToInt(1.0f / deltaTime);

            GUIStyle style = new GUIStyle();
            style.fontSize = 24;
            style.normal.textColor = Color.white;

            GUI.Label(new Rect(10, 10, 200, 50), $"FPS: {fps}", style);

            Handles.EndGUI();
        }
    }
}