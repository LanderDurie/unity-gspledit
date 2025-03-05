using UnityEngine;
using UnityEditor;

namespace UnityEngine.GsplEdit
{
    [ExecuteInEditMode] // Ensures this script runs in Edit Mode
    public class FPSCounter : MonoBehaviour
    {
        private float deltaTime = 0.0f;

        void Update()
        {
            // Calculate FPS in both Edit Mode and Play Mode
            deltaTime += (Time.deltaTime - deltaTime) * 0.1f;

            // Force the Scene view to repaint so the FPS counter updates
            SceneView.RepaintAll();
        }

        void OnDrawGizmos()
        {
            // Use Handles.BeginGUI to draw GUI elements in the Scene view
            Handles.BeginGUI();

            // Calculate FPS
            int fps = Mathf.CeilToInt(1.0f / deltaTime);

            // Create a GUIStyle for the FPS display
            GUIStyle style = new GUIStyle();
            style.fontSize = 24;
            style.normal.textColor = Color.white;

            // Display the FPS counter on the screen
            GUI.Label(new Rect(10, 10, 200, 50), $"FPS: {fps}", style);

            Handles.EndGUI();
        }
    }
}