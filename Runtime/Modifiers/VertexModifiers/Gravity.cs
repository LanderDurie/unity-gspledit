using UnityEditor;
using System;

namespace UnityEngine.GsplEdit
{
    [CreateAssetMenu(fileName = "Gravity", menuName = "GsplEdit/Modifiers/Gravity")]
    [Serializable]
    public class Gravity : Modifier
    {
        [Header("Gravity Parameters")]
        [SerializeField] private Vector3 gravityCenter = Vector3.zero;
        [SerializeField] private float gravityStrength = 1.0f;
        [SerializeField] private float falloff = 2.0f; // Quadratic falloff by default
        [SerializeField] private float maxDisplacement = 1.0f;
        
        [Header("Animation Settings")]
        [SerializeField] private bool animate = true;
        [SerializeField] private float animationSpeed = 1.0f;
        [SerializeField] private float pulseFrequency = 0.5f;
        [SerializeField] private bool oscillate = false;
        
        [SerializeField, HideInInspector] private ComputeShader computeShader;
        
        public override void Initialize(Mesh mesh)
        {
            // Initialization happens in Run
        }
        
        public override void Run(ref GraphicsBuffer baseVertices, ref GraphicsBuffer modVertices)
        {
            if (computeShader == null)
            {
                Debug.LogError("Gravity: Missing compute shader!");
                return;
            }
            
            // Calculate time for animation
            float time = (Application.isPlaying) 
                ? Time.time * animationSpeed 
                : (float)UnityEditor.EditorApplication.timeSinceStartup * animationSpeed;
            
            float gravityMod = 1.0f;
            if (animate)
            {
                gravityMod = oscillate 
                    ? Mathf.Sin(time * pulseFrequency * Mathf.PI * 2) * 0.5f + 0.5f 
                    : (Mathf.Sin(time * pulseFrequency * Mathf.PI * 2) * 0.5f + 0.5f) + 0.5f;
            }
            
            // Set shader parameters
            computeShader.SetVector("gravityCenter", gravityCenter);
            computeShader.SetFloat("gravityStrength", gravityStrength * gravityMod);
            computeShader.SetFloat("falloff", falloff);
            computeShader.SetFloat("maxDisplacement", maxDisplacement);
            
            computeShader.SetBuffer(0, "_VertexBasePos", baseVertices);
            computeShader.SetBuffer(0, "_VertexModPos", modVertices);
            
            // Dispatch the compute shader
            int threadGroups = Mathf.CeilToInt(baseVertices.count / 256.0f);
            computeShader.Dispatch(0, threadGroups, 1, 1);
        }
        
        public override void DrawSettings()
        {
            GUILayout.Label("Gravity Deformation", EditorStyles.boldLabel);
            
            // Gravity settings
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Gravity Parameters", EditorStyles.boldLabel);
            gravityCenter = EditorGUILayout.Vector3Field("Gravity Center", gravityCenter);
            gravityStrength = EditorGUILayout.FloatField("Gravity Strength", gravityStrength);
            falloff = EditorGUILayout.Slider("Distance Falloff", falloff, 0.1f, 5.0f);
            maxDisplacement = EditorGUILayout.FloatField("Max Displacement", maxDisplacement);
            
            // Animation settings
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Animation", EditorStyles.boldLabel);
            animate = EditorGUILayout.Toggle("Animate Gravity", animate);
            
            if (animate)
            {
                EditorGUI.indentLevel++;
                animationSpeed = EditorGUILayout.FloatField("Animation Speed", animationSpeed);
                pulseFrequency = EditorGUILayout.FloatField("Pulse Frequency", pulseFrequency);
                oscillate = EditorGUILayout.Toggle("Oscillate", oscillate);
                EditorGUI.indentLevel--;
            }
        }
    }
}