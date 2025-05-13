using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEngine.GsplEdit {
    [ExecuteInEditMode]
    public class FPSCounter : MonoBehaviour {
        private float deltaTime = 0.0f;
        private List<float> frameTimes = new List<float>();
        private const int maxSamples = 2000;

        private float averageFrameTime = 0.0f;
        private float minFPS = float.MaxValue;
        private float maxFPS = 0.0f;

        void Update() {
            deltaTime += (Time.deltaTime - deltaTime) * 0.1f;

            // Store frame time
            float frameTime = Time.deltaTime * 1000f;
            frameTimes.Add(frameTime);
            if (frameTimes.Count > maxSamples)
                frameTimes.RemoveAt(0);

            // Compute stats
            float total = 0.0f;
            float localMinFPS = float.MaxValue;
            float localMaxFPS = 0.0f;
            foreach (float ft in frameTimes) {
                total += ft;
                float fps = 1000f / ft;
                if (fps < localMinFPS) localMinFPS = fps;
                if (fps > localMaxFPS) localMaxFPS = fps;
            }

            averageFrameTime = total / frameTimes.Count;
            minFPS = localMinFPS;
            maxFPS = localMaxFPS;

            SceneView.RepaintAll(); // Ensure the label is redrawn in edit mode
        }

        void OnDrawGizmos() {
            Handles.BeginGUI();

            GUIStyle style = new GUIStyle {
                fontSize = 18,
                normal = { textColor = Color.white }
            };

            int currentFPS = Mathf.CeilToInt(1.0f / deltaTime);
            float currentFrameTime = deltaTime * 1000f;

            GUI.Label(new Rect(10, 10, 300, 100),
                $"Current FPS: {currentFPS} ({currentFrameTime:F1} ms)\n" +
                $"Avg FPS: {(1000f / averageFrameTime):F1} ({averageFrameTime:F1} ms)\n" +
                $"Min FPS: {minFPS:F1}, Max FPS: {maxFPS:F1}",
                style);

            Handles.EndGUI();
        }
    }
}
