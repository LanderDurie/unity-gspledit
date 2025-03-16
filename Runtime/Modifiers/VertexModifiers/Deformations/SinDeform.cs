using UnityEditor;
using System;

namespace UnityEngine.GsplEdit
{
    [CreateAssetMenu(fileName = "SinDeform", menuName = "GsplEdit/Modifiers/Sin")]
    [Serializable]
    public class SinDeform : Modifier
    {
        [Header("Sine Wave Parameters")]
        [SerializeField] private float amplitudeX = 0.1f;
        [SerializeField] private float amplitudeY = 0.1f;
        [SerializeField] private float amplitudeZ = 0.1f;

        [SerializeField] private float frequencyX = 1.0f;
        [SerializeField] private float frequencyY = 1.0f;
        [SerializeField] private float frequencyZ = 1.0f;

        [SerializeField] private float phaseX = 0.0f;
        [SerializeField] private float phaseY = 0.0f;
        [SerializeField] private float phaseZ = 0.0f;

        [Header("Animation Settings")]
        [SerializeField] private bool animateX = true;
        [SerializeField] private bool animateY = true;
        [SerializeField] private bool animateZ = true;
        [SerializeField] private float animationSpeed = 1.0f;

        [SerializeField, HideInInspector] private ComputeShader computeShader;

        public override void Initialize(Mesh mesh)
        {
            // Don't do anything in Initialize
            // We'll handle everything in Run
        }

        public override void Run(ref GraphicsBuffer baseVertices, ref GraphicsBuffer modVertices) {
            if (computeShader == null) {
                Debug.LogError("SinModifier: Missing compute shader!");
                return;
            }

            // Set compute shader parameters
            float time = (Application.isPlaying) 
                ? Time.time * animationSpeed 
                : (float)UnityEditor.EditorApplication.timeSinceStartup * animationSpeed;

            computeShader.SetFloat("time", time);
            computeShader.SetVector("amplitude", new Vector3(amplitudeX, amplitudeY, amplitudeZ));
            computeShader.SetVector("frequency", new Vector3(frequencyX, frequencyY, frequencyZ));
            computeShader.SetVector("phase", new Vector3(phaseX, phaseY, phaseZ));
            computeShader.SetInts("animateFlags", animateX ? 1 : 0, animateY ? 1 : 0, animateZ ? 1 : 0);

            computeShader.SetBuffer(0, "_VertexBasePos", baseVertices);
            computeShader.SetBuffer(0, "_VertexModPos", modVertices);

            // Dispatch the compute shader
            int threadGroups = Mathf.CeilToInt(baseVertices.count / 256.0f);
            computeShader.Dispatch(0, threadGroups, 1, 1);
        }

        public override void DrawSettings()
        {
            GUILayout.Label("Sine Wave Deformation", EditorStyles.boldLabel);

            // Amplitude settings
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Amplitude", EditorStyles.boldLabel);
            amplitudeX = EditorGUILayout.FloatField("X Amplitude", amplitudeX);
            amplitudeY = EditorGUILayout.FloatField("Y Amplitude", amplitudeY);
            amplitudeZ = EditorGUILayout.FloatField("Z Amplitude", amplitudeZ);

            // Frequency settings
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Frequency", EditorStyles.boldLabel);
            frequencyX = EditorGUILayout.FloatField("X Frequency", frequencyX);
            frequencyY = EditorGUILayout.FloatField("Y Frequency", frequencyY);
            frequencyZ = EditorGUILayout.FloatField("Z Frequency", frequencyZ);

            // Phase settings
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Phase", EditorStyles.boldLabel);
            phaseX = EditorGUILayout.FloatField("X Phase", phaseX);
            phaseY = EditorGUILayout.FloatField("Y Phase", phaseY);
            phaseZ = EditorGUILayout.FloatField("Z Phase", phaseZ);

            // Animation settings
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Animation", EditorStyles.boldLabel);
            animateX = EditorGUILayout.Toggle("Animate X", animateX);
            animateY = EditorGUILayout.Toggle("Animate Y", animateY);
            animateZ = EditorGUILayout.Toggle("Animate Z", animateZ);
            animationSpeed = EditorGUILayout.FloatField("Animation Speed", animationSpeed);
        }
    }
}