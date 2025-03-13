using System;
using UnityEditor;

namespace UnityEngine.GsplEdit
{
    public class SinDeform : DeformBase 
    {
        // Sine wave parameters
        [Header("Sine Wave Parameters")]
        [SerializeField] private float m_AmplitudeX = 0.0f;
        [SerializeField] private float m_AmplitudeY = 0.0f;
        [SerializeField] private float m_AmplitudeZ = 0.0f;
        
        [SerializeField] private float m_FrequencyX = 1.0f;
        [SerializeField] private float m_FrequencyY = 1.0f;
        [SerializeField] private float m_FrequencyZ = 1.0f;
        
        [SerializeField] private float m_PhaseX = 0.0f;
        [SerializeField] private float m_PhaseY = 0.0f;
        [SerializeField] private float m_PhaseZ = 0.0f;
        
        [Header("Animation Settings")]
        [SerializeField] private bool m_AnimateX = false;
        [SerializeField] private bool m_AnimateY = false;
        [SerializeField] private bool m_AnimateZ = false;
        [SerializeField] private float m_AnimationSpeed = 1.0f;

        public SinDeform()
        {
            m_Type = Modifier.Type.Dynamic;
        }

        public override void Initialize(SharedComputeContext context, VertexSelectionGroup selectionGroup)
        {
            base.Initialize(context, selectionGroup);
            
            if (m_ComputeShader == null)
            {
                Debug.LogError("SinDeform compute shader is not assigned!");
            }
        }

        public override void Run()
        {
            if (m_Context.gpuMeshBaseVertex == null || m_Context.gpuMeshModVertex == null)
                throw new InvalidOperationException("GraphicsBuffer is not initialized.");
            
            if (m_ComputeShader == null)
                throw new InvalidOperationException("Compute Shader is not assigned.");
            
            // Get current time for animation
            float time = (Application.isPlaying) ? Time.time : (float)EditorApplication.timeSinceStartup;
            time *= m_AnimationSpeed;
            
            int kernel = m_ComputeShader.FindKernel("CSMain");
            
            // Set buffers
            m_ComputeShader.SetBuffer(kernel, "_VertexBasePos", m_Context.gpuMeshBaseVertex);
            m_ComputeShader.SetBuffer(kernel, "_VertexModPos", m_Context.gpuMeshModVertex);
            m_ComputeShader.SetBuffer(kernel, "_VertexSelectedBits", m_SelectionGroup.m_SelectedVerticesBuffer);
            
            // Set sine wave parameters
            m_ComputeShader.SetFloat("time", time);
            m_ComputeShader.SetFloat("amplitudeX", m_AmplitudeX);
            m_ComputeShader.SetFloat("amplitudeY", m_AmplitudeY);
            m_ComputeShader.SetFloat("amplitudeZ", m_AmplitudeZ);
            m_ComputeShader.SetFloat("frequencyX", m_FrequencyX);
            m_ComputeShader.SetFloat("frequencyY", m_FrequencyY);
            m_ComputeShader.SetFloat("frequencyZ", m_FrequencyZ);
            m_ComputeShader.SetFloat("phaseX", m_PhaseX);
            m_ComputeShader.SetFloat("phaseY", m_PhaseY);
            m_ComputeShader.SetFloat("phaseZ", m_PhaseZ);
            
            // Convert animation flags to vector
            Vector3 animateFlags = new Vector3(
                m_AnimateX ? 1.0f : 0.0f,
                m_AnimateY ? 1.0f : 0.0f,
                m_AnimateZ ? 1.0f : 0.0f
            );
            m_ComputeShader.SetVector("animateFlags", animateFlags);
            
            // Calculate thread groups based on vertex count
            int threadGroups = Mathf.CeilToInt(m_Context.vertexCount / 256.0f);
            m_ComputeShader.Dispatch(kernel, threadGroups, 1, 1);
            
            // Force mesh update to see changes
            if (!Application.isPlaying)
            {
                SceneView.RepaintAll();
            }
        }

        public override void DrawSettings()
        {
            GUILayout.Label("Sine Wave Deformation", EditorStyles.boldLabel);
            
            // Amplitude settings
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Amplitude", EditorStyles.boldLabel);
            m_AmplitudeX = EditorGUILayout.FloatField("X Amplitude", m_AmplitudeX);
            m_AmplitudeY = EditorGUILayout.FloatField("Y Amplitude", m_AmplitudeY);
            m_AmplitudeZ = EditorGUILayout.FloatField("Z Amplitude", m_AmplitudeZ);
            
            // Frequency settings
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Frequency", EditorStyles.boldLabel);
            m_FrequencyX = EditorGUILayout.Slider("X Frequency", m_FrequencyX, 0.1f, 5f);
            m_FrequencyY = EditorGUILayout.Slider("Y Frequency", m_FrequencyY, 0.1f, 5f);
            m_FrequencyZ = EditorGUILayout.Slider("Z Frequency", m_FrequencyZ, 0.1f, 5f);
            
            // Phase settings
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Phase", EditorStyles.boldLabel);
            m_PhaseX = EditorGUILayout.Slider("X Phase", m_PhaseX, 0f, 2f * Mathf.PI);
            m_PhaseY = EditorGUILayout.Slider("Y Phase", m_PhaseY, 0f, 2f * Mathf.PI);
            m_PhaseZ = EditorGUILayout.Slider("Z Phase", m_PhaseZ, 0f, 2f * Mathf.PI);
            
            // Animation settings
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Animation", EditorStyles.boldLabel);
            m_AnimateX = EditorGUILayout.Toggle("Animate X", m_AnimateX);
            m_AnimateY = EditorGUILayout.Toggle("Animate Y", m_AnimateY);
            m_AnimateZ = EditorGUILayout.Toggle("Animate Z", m_AnimateZ);
            m_AnimationSpeed = EditorGUILayout.Slider("Animation Speed", m_AnimationSpeed, 0.1f, 5f);
            
            // Add debug button to help troubleshoot visibility issues
            EditorGUILayout.Space();
            if (GUILayout.Button("Force Apply Deformation"))
            {
                Run();
            }
        }
    }
}