// using System;
// using UnityEditor;

// namespace UnityEngine.GsplEdit
// {
//     public class WaveDeform : DeformBase 
//     {
//         [Header("Wave Parameters")]
//         [SerializeField] private float m_Amplitude = 0.5f;
//         [SerializeField] private float m_Wavelength = 1.0f;
//         [SerializeField] private float m_Speed = 1.0f;
//         [SerializeField] private Vector3 m_Direction = new Vector3(1f, 0f, 0f);
//         [SerializeField] private AnimationCurve m_WaveShape = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
//         [SerializeField] private float m_Offset = 0f;
//         [SerializeField] private bool m_Animate = true;

//         public WaveDeform()
//         {
//             m_Type = Modifier.Type.Dynamic;
//         }

//         public override void Initialize(SharedComputeContext context, VertexSelectionGroup selectionGroup)
//         {
//             base.Initialize(context, selectionGroup);
            
//             if (m_ComputeShader == null)
//             {
//                 Debug.LogError("WaveDeform compute shader is not assigned!");
//             }
//         }

//         public override void Run()
//         {
//             if (m_Context.scaffoldBaseVertex == null || m_Context.scaffoldModVertex == null)
//                 throw new InvalidOperationException("GraphicsBuffer is not initialized.");
            
//             if (m_ComputeShader == null)
//                 throw new InvalidOperationException("Compute Shader is not assigned.");

//             // Sample animation curve to compute buffer
//             const int curveResolution = 32;
//             float[] curveData = new float[curveResolution];
//             for (int i = 0; i < curveResolution; i++)
//             {
//                 float t = i / (float)(curveResolution - 1);
//                 curveData[i] = m_WaveShape.Evaluate(t);
//             }
            
//             ComputeBuffer curveBuffer = new ComputeBuffer(curveResolution, sizeof(float));
//             curveBuffer.SetData(curveData);
            
//             // Get current time for animation
//             float time = (Application.isPlaying) ? Time.time : (float)EditorApplication.timeSinceStartup;
//             float animatedTime = m_Animate ? time * m_Speed : 0f;
            
//             int kernel = m_ComputeShader.FindKernel("CSMain");
            
//             // Set buffers
//             m_ComputeShader.SetBuffer(kernel, "_VertexBasePos", m_Context.scaffoldBaseVertex);
//             m_ComputeShader.SetBuffer(kernel, "_VertexModPos", m_Context.scaffoldModVertex);
//             m_ComputeShader.SetBuffer(kernel, "_VertexSelectedBits", m_SelectionGroup.m_SelectedVerticesBuffer);
//             m_ComputeShader.SetBuffer(kernel, "waveShapeCurve", curveBuffer);
            
//             // Set parameters
//             m_ComputeShader.SetFloat("time", animatedTime);
//             m_ComputeShader.SetFloat("amplitude", m_Amplitude);
//             m_ComputeShader.SetFloat("wavelength", m_Wavelength);
//             m_ComputeShader.SetFloat("offset", m_Offset);
//             m_ComputeShader.SetInt("curveResolution", curveResolution);
            
//             // Normalize direction vector
//             Vector3 dir = m_Direction.normalized;
//             m_ComputeShader.SetVector("direction", dir);
            
//             // Calculate thread groups based on vertex count
//             int threadGroups = Mathf.CeilToInt(m_Context.scaffoldVertexCount / 256.0f);
//             m_ComputeShader.Dispatch(kernel, threadGroups, 1, 1);
            
//             // Release the curve buffer
//             curveBuffer.Release();
            
//             // Force mesh update to see changes
//             if (!Application.isPlaying)
//             {
//                 SceneView.RepaintAll();
//             }
//         }

//         public override void DrawSettings()
//         {            
//             GUILayout.Label("Wave Deformation", EditorStyles.boldLabel);
            
//             // Wave parameters
//             m_Amplitude = EditorGUILayout.Slider("Amplitude", m_Amplitude, 0f, 2f);
//             m_Wavelength = EditorGUILayout.Slider("Wavelength", m_Wavelength, 0.1f, 10f);
//             m_Direction = EditorGUILayout.Vector3Field("Direction", m_Direction);
            
//             EditorGUILayout.Space();
//             m_WaveShape = EditorGUILayout.CurveField("Wave Shape", m_WaveShape);
//             m_Offset = EditorGUILayout.Slider("Offset", m_Offset, 0f, 2f * Mathf.PI);
            
//             // Animation settings
//             EditorGUILayout.Space();
//             EditorGUILayout.LabelField("Animation", EditorStyles.boldLabel);
//             m_Animate = EditorGUILayout.Toggle("Animate", m_Animate);
//             if (m_Animate)
//             {
//                 m_Speed = EditorGUILayout.Slider("Speed", m_Speed, 0.1f, 5f);
//             }
            
//             // Add debug button to help troubleshoot visibility issues
//             EditorGUILayout.Space();
//             if (GUILayout.Button("Force Apply Deformation"))
//             {
//                 Run();
//             }
//         }
//     }
// }