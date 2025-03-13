using System;
using UnityEditor;

namespace UnityEngine.GsplEdit
{
    public class TwistDeform : DeformBase 
    {
        // Twist parameters
        [Header("Twist Parameters")]
        [SerializeField] private float m_TwistAngle = 45.0f;
        [SerializeField] private float m_TwistHeight = 1.0f;
        [SerializeField] private Vector3 m_TwistAxis = Vector3.up;
        [SerializeField] private Vector3 m_TwistCenter = Vector3.zero;
        
        [Header("Falloff Settings")]
        [SerializeField] private bool m_UseFalloff = true;
        [SerializeField] private float m_FalloffStart = 0.0f;
        [SerializeField] private float m_FalloffEnd = 1.0f;
        [SerializeField] private AnimationCurve m_FalloffCurve = AnimationCurve.Linear(0, 1, 1, 0);
        
        [Header("Animation Settings")]
        [SerializeField] private bool m_Animate = false;
        [SerializeField] private float m_AnimationSpeed = 1.0f;
        [SerializeField] private float m_AnimationRange = 90.0f;

        public TwistDeform()
        {
            m_Type = Modifier.Type.Dynamic;
        }

        public override void Initialize(SharedComputeContext context, VertexSelectionGroup selectionGroup)
        {
            base.Initialize(context, selectionGroup);
            
            if (m_ComputeShader == null)
            {
                Debug.LogError("TwistDeform compute shader is not assigned!");
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
            
            // Calculate animated twist angle if animation is enabled
            float currentTwistAngle = m_TwistAngle;
            if (m_Animate)
            {
                currentTwistAngle = m_TwistAngle + Mathf.Sin(time) * m_AnimationRange;
            }
            
            int kernel = m_ComputeShader.FindKernel("CSMain");
            
            // Set buffers
            m_ComputeShader.SetBuffer(kernel, "_VertexBasePos", m_Context.gpuMeshBaseVertex);
            m_ComputeShader.SetBuffer(kernel, "_VertexModPos", m_Context.gpuMeshModVertex);
            m_ComputeShader.SetBuffer(kernel, "_VertexSelectedBits", m_SelectionGroup.m_SelectedVerticesBuffer);
            
            // Set twist parameters
            m_ComputeShader.SetFloat("twistAngle", currentTwistAngle * Mathf.Deg2Rad);
            m_ComputeShader.SetFloat("twistHeight", m_TwistHeight);
            m_ComputeShader.SetVector("twistAxis", m_TwistAxis.normalized);
            m_ComputeShader.SetVector("twistCenter", m_TwistCenter);
            
            // Set falloff parameters
            m_ComputeShader.SetBool("useFalloff", m_UseFalloff);
            m_ComputeShader.SetFloat("falloffStart", m_FalloffStart);
            m_ComputeShader.SetFloat("falloffEnd", m_FalloffEnd);
            
            // Transfer falloff curve to texture if needed
            // This would require additional code to sample the curve and create a texture
            // For simplicity, we're handling falloff in the shader without a texture
            
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
            GUILayout.Label("Twist Deformation", EditorStyles.boldLabel);
            
            // Twist settings
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Twist Parameters", EditorStyles.boldLabel);
            m_TwistAngle = EditorGUILayout.FloatField("Twist Angle", m_TwistAngle);
            m_TwistHeight = EditorGUILayout.FloatField("Twist Height", m_TwistHeight);
            m_TwistAxis = EditorGUILayout.Vector3Field("Twist Axis", m_TwistAxis);
            m_TwistCenter = EditorGUILayout.Vector3Field("Twist Center", m_TwistCenter);
            
            // Falloff settings
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Falloff Settings", EditorStyles.boldLabel);
            m_UseFalloff = EditorGUILayout.Toggle("Use Falloff", m_UseFalloff);
            if (m_UseFalloff)
            {
                m_FalloffStart = EditorGUILayout.FloatField("Falloff Start", m_FalloffStart);
                m_FalloffEnd = EditorGUILayout.FloatField("Falloff End", m_FalloffEnd);
                m_FalloffCurve = EditorGUILayout.CurveField("Falloff Curve", m_FalloffCurve);
            }
            
            // Animation settings
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Animation", EditorStyles.boldLabel);
            m_Animate = EditorGUILayout.Toggle("Animate Twist", m_Animate);
            if (m_Animate)
            {
                m_AnimationSpeed = EditorGUILayout.Slider("Animation Speed", m_AnimationSpeed, 0.1f, 5f);
                m_AnimationRange = EditorGUILayout.FloatField("Animation Range", m_AnimationRange);
            }
            
            // Debug button
            EditorGUILayout.Space();
            if (GUILayout.Button("Force Apply Deformation"))
            {
                Run();
            }
        }
    }
}