using UnityEditor;
using System;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEngine.GsplEdit
{
    [CreateAssetMenu(fileName = "WindDeform", menuName = "GsplEdit/Modifiers/Wind")]
    [Serializable]
    public class WindDeform : Modifier
    {
        [Header("Wind Parameters")]
        [SerializeField] private Vector3 windDirection = new Vector3(1f, 0f, 0f);
        [SerializeField] private float windStrength = 0.5f;
        [SerializeField] private float turbulence = 0.2f;
        [SerializeField] private float noiseScale = 1.0f;

        [Header("Vertex Influence")]
        [SerializeField] private bool useVertexColor = false;
        [SerializeField] private bool heightBasedInfluence = true;
        [SerializeField] private float minHeight = 0.0f;
        [SerializeField] private float maxHeight = 1.0f;

        [Header("Anchor Points")]
        [SerializeField] private List<int> anchorIndices = new List<int>();

        [Header("Animation Settings")]
        [SerializeField] private float gustFrequency = 0.2f;
        [SerializeField] private float gustStrength = 0.3f;
        [SerializeField] private bool randomizeDirection = true;
        [SerializeField] private float directionChangeSpeed = 0.05f;

        [SerializeField, HideInInspector] private ComputeShader computeShader;

        private GraphicsBuffer anchorBuffer;
        private Vector3 currentWindDirection;
        private Vector3 targetWindDirection;
        private float lastDirectionChangeTime;

        public override void Initialize(Mesh mesh)
        {
            currentWindDirection = windDirection.normalized;
            targetWindDirection = currentWindDirection;
            lastDirectionChangeTime = 0f;

            // Create buffer for anchor points
            if (anchorBuffer != null) anchorBuffer.Release();
            anchorBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, anchorIndices.Count, sizeof(int));
            anchorBuffer.SetData(anchorIndices);
        }

        public override void Run(ref GraphicsBuffer baseVertices, ref GraphicsBuffer modVertices)
        {
            if (computeShader == null)
            {
                Debug.LogError("WindDeform: Missing compute shader!");
                return;
            }

            float time = (Application.isPlaying) 
                ? Time.time
                : (float)UnityEditor.EditorApplication.timeSinceStartup;

            if (randomizeDirection)
            {
                if (time - lastDirectionChangeTime > directionChangeSpeed)
                {
                    lastDirectionChangeTime = time;
                    Vector3 randomOffset = new Vector3(
                        UnityEngine.Random.Range(-0.3f, 0.3f),
                        UnityEngine.Random.Range(-0.2f, 0.2f),
                        UnityEngine.Random.Range(-0.3f, 0.3f)
                    );
                    targetWindDirection = (windDirection.normalized + randomOffset).normalized;
                }
                currentWindDirection = Vector3.Lerp(currentWindDirection, targetWindDirection, Time.deltaTime * 2.0f);
            }
            else
            {
                currentWindDirection = windDirection.normalized;
            }

            float gustFactor = 1.0f + Mathf.Sin(time * gustFrequency * Mathf.PI * 2) * gustStrength;

            computeShader.SetVector("windDirection", currentWindDirection);
            computeShader.SetFloat("windStrength", windStrength * gustFactor);
            computeShader.SetFloat("turbulence", turbulence);
            computeShader.SetFloat("noiseScale", noiseScale);
            computeShader.SetFloat("time", time);
            computeShader.SetBool("heightBasedInfluence", heightBasedInfluence);
            computeShader.SetFloat("minHeight", minHeight);
            computeShader.SetFloat("maxHeight", maxHeight);
            computeShader.SetBool("useVertexColor", useVertexColor);

            computeShader.SetBuffer(0, "_VertexBasePos", baseVertices);
            computeShader.SetBuffer(0, "_VertexModPos", modVertices);
            computeShader.SetBuffer(0, "_AnchorIndices", anchorBuffer);
            computeShader.SetInt("_AnchorCount", anchorIndices.Count);

            int threadGroups = Mathf.CeilToInt(baseVertices.count / 256.0f);
            computeShader.Dispatch(0, threadGroups, 1, 1);
        }

        private void OnDestroy()
        {
            if (anchorBuffer != null) anchorBuffer.Release();
        }

                public override void DrawSettings()
        {
            GUILayout.Label("Wind Deformation", EditorStyles.boldLabel);
            
            // Wind settings
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Wind Parameters", EditorStyles.boldLabel);
            windDirection = EditorGUILayout.Vector3Field("Wind Direction", windDirection);
            windStrength = EditorGUILayout.Slider("Wind Strength", windStrength, 0f, 2f);
            turbulence = EditorGUILayout.Slider("Turbulence", turbulence, 0f, 1f);
            noiseScale = EditorGUILayout.FloatField("Noise Scale", noiseScale);
            
            // Vertex influence
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Vertex Influence", EditorStyles.boldLabel);
            useVertexColor = EditorGUILayout.Toggle("Use Vertex Color", useVertexColor);
            heightBasedInfluence = EditorGUILayout.Toggle("Height Based", heightBasedInfluence);
            
            if (heightBasedInfluence)
            {
                EditorGUI.indentLevel++;
                minHeight = EditorGUILayout.FloatField("Min Height", minHeight);
                maxHeight = EditorGUILayout.FloatField("Max Height", maxHeight);
                EditorGUI.indentLevel--;
            }
            
            // Gust and animation settings
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Gust Settings", EditorStyles.boldLabel);
            gustFrequency = EditorGUILayout.Slider("Gust Frequency", gustFrequency, 0.01f, 1f);
            gustStrength = EditorGUILayout.Slider("Gust Strength", gustStrength, 0f, 1f);
            randomizeDirection = EditorGUILayout.Toggle("Randomize Direction", randomizeDirection);
            
            if (randomizeDirection)
            {
                EditorGUI.indentLevel++;
                directionChangeSpeed = EditorGUILayout.Slider("Direction Change Speed", directionChangeSpeed, 0.01f, 1f);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.LabelField("Anchor Points", EditorStyles.boldLabel);
            for (int i = 0; i < anchorIndices.Count; i++)
            {
                anchorIndices[i] = EditorGUILayout.IntField($"Anchor {i}", anchorIndices[i]);
            }
            if (GUILayout.Button("Add Anchor"))
                anchorIndices.Add(0);
            if (GUILayout.Button("Remove Last Anchor") && anchorIndices.Count > 0)
                anchorIndices.RemoveAt(anchorIndices.Count - 1);
        }
    }
}
