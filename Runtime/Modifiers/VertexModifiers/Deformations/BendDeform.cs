using System;
using UnityEditor;

namespace UnityEngine.GsplEdit
{
    public class BendDeform : DeformBase 
    {
        // Bend parameters
        [Header("Bend Parameters")]
        [SerializeField] private float m_BendAngle = 90.0f;
        [SerializeField] private float m_BendRadius = 1.0f;
        [SerializeField] private Vector3 m_BendAxis = Vector3.forward;
        [SerializeField] private Vector3 m_BendDirection = Vector3.up;
        [SerializeField] private Vector3 m_BendCenter = Vector3.zero;
        
        [Header("Bounds Settings")]
        [SerializeField] private float m_BendStart = -0.5f;
        [SerializeField] private float m_BendEnd = 0.5f;
        [SerializeField] private bool m_ClampBounds = true;
        
        [Header("Animation Settings")]
        [SerializeField] private bool m_Animate = false;
        [SerializeField] private float m_AnimationSpeed = 1.0f;
        [SerializeField] private float m_AnimationRange = 45.0f;

        public BendDeform()
        {
            m_Type = Modifier.Type.Dynamic;
        }

        public override void Initialize(SharedComputeContext context, VertexSelectionGroup selectionGroup)
        {
            base.Initialize(context, selectionGroup);
            
            if (m_ComputeShader == null)
            {
                Debug.LogError("BendDeform compute shader is not assigned!");
            }
        }

        public override void Run()
        {
            if (m_Context.scaffoldBaseVertex == null || m_Context.scaffoldModVertex == null)
                throw new InvalidOperationException("GraphicsBuffer is not initialized.");
            
            if (m_ComputeShader == null)
                throw new InvalidOperationException("Compute Shader is not assigned.");
            
            // Get current time for animation
            float time = (Application.isPlaying) ? Time.time : (float)EditorApplication.timeSinceStartup;
            time *= m_AnimationSpeed;
            
            // Calculate animated bend angle if animation is enabled
            float currentBendAngle = m_BendAngle;
            if (m_Animate)
            {
                currentBendAngle = m_BendAngle + Mathf.Sin(time) * m_AnimationRange;
            }
            
            int kernel = m_ComputeShader.FindKernel("CSMain");
            
            // Set buffers
            m_ComputeShader.SetBuffer(kernel, "_VertexBasePos", m_Context.scaffoldBaseVertex);
            m_ComputeShader.SetBuffer(kernel, "_VertexModPos", m_Context.scaffoldModVertex);
            m_ComputeShader.SetBuffer(kernel, "_VertexSelectedBits", m_SelectionGroup.m_SelectedVerticesBuffer);
            
            // Set bend parameters
            m_ComputeShader.SetFloat("bendAngle", currentBendAngle * Mathf.Deg2Rad);
            m_ComputeShader.SetFloat("bendRadius", m_BendRadius);
            m_ComputeShader.SetVector("bendAxis", m_BendAxis.normalized);
            m_ComputeShader.SetVector("bendDirection", m_BendDirection.normalized);
            m_ComputeShader.SetVector("bendCenter", m_BendCenter);
            
            // Set bounds parameters
            m_ComputeShader.SetFloat("bendStart", m_BendStart);
            m_ComputeShader.SetFloat("bendEnd", m_BendEnd);
            m_ComputeShader.SetBool("clampBounds", m_ClampBounds);
            
            // Calculate thread groups based on vertex count
            int threadGroups = Mathf.CeilToInt(m_Context.scaffoldData.vertexCount / 256.0f);
            m_ComputeShader.Dispatch(kernel, threadGroups, 1, 1);
            
            // Force mesh update to see changes
            if (!Application.isPlaying)
            {
                SceneView.RepaintAll();
            }
        }

        public override void DrawSettings()
        {
            GUILayout.Label("Bend Deformation", EditorStyles.boldLabel);
            
            // Bend settings
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Bend Parameters", EditorStyles.boldLabel);
            m_BendAngle = EditorGUILayout.FloatField("Bend Angle", m_BendAngle);
            m_BendRadius = EditorGUILayout.FloatField("Bend Radius", m_BendRadius);
            m_BendAxis = EditorGUILayout.Vector3Field("Bend Axis", m_BendAxis);
            m_BendDirection = EditorGUILayout.Vector3Field("Bend Direction", m_BendDirection);
            m_BendCenter = EditorGUILayout.Vector3Field("Bend Center", m_BendCenter);
            
            // Bounds settings
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Bounds Settings", EditorStyles.boldLabel);
            m_BendStart = EditorGUILayout.FloatField("Bend Start", m_BendStart);
            m_BendEnd = EditorGUILayout.FloatField("Bend End", m_BendEnd);
            m_ClampBounds = EditorGUILayout.Toggle("Clamp Bounds", m_ClampBounds);
            
            // Animation settings
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Animation", EditorStyles.boldLabel);
            m_Animate = EditorGUILayout.Toggle("Animate Bend", m_Animate);
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