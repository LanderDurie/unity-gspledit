using UnityEditor;
using System;
using UnityEngine.Animations.Rigging;

namespace UnityEngine.GsplEdit
{
    [CreateAssetMenu(fileName = "Rigging", menuName = "GsplEdit/Modifiers/Rigging")]
    [Serializable]
    public class RiggingModifier : Modifier
    {
        [Header("Animation Rigging")]
        [SerializeField] private Transform rigRoot;
        [SerializeField] private Rig animationRig;
        [SerializeField] private bool autoUpdateWeights = true;
        
        [SerializeField, HideInInspector] private ComputeShader computeShader;
        
        // Buffers for bone data
        private GraphicsBuffer boneMatricesBuffer;
        private GraphicsBuffer boneWeightsBuffer;
        
        private bool weightsGenerated = false;
        private Transform[] bones;
        private Matrix4x4[] boneMatrices;
        
        [Serializable]
        private struct VertexBoneData
        {
            public Vector4 weights;
            public Vector4 indices; // Using Vector4 for indices (will convert to int in compute shader)
        }

        public override void Initialize(Mesh mesh)
        {
            if (rigRoot == null || animationRig == null)
            {
                Debug.LogWarning("RiggingModifier: No Animation Rigging data assigned!");
                return;
            }
            
            // Collect bones from the rig
            CollectBonesFromRig();
            
            // Initialize buffers for bone matrices
            if (boneMatricesBuffer == null || boneMatricesBuffer.count != bones.Length)
            {
                CleanupBuffers();
                
                boneMatrices = new Matrix4x4[bones.Length];
                boneMatricesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, bones.Length, sizeof(float) * 16);
            }
            
            // Generate weights if needed
            if (!weightsGenerated || autoUpdateWeights)
            {
                GenerateWeightsForMesh(mesh);
            }
        }

        public override void Run(ref GraphicsBuffer baseVertices, ref GraphicsBuffer modVertices)
        {
            if (computeShader == null)
            {
                Debug.LogError("RiggingModifier: Missing compute shader!");
                return;
            }
            
            if (bones == null || bones.Length == 0 || boneMatrices == null || !weightsGenerated)
            {
                // No rigging data available, just copy base to mod
                computeShader.SetBuffer(0, "_VertexBasePos", baseVertices);
                computeShader.SetBuffer(0, "_VertexModPos", modVertices);
                
                int threadGroups = Mathf.CeilToInt(baseVertices.count / 256.0f);
                computeShader.Dispatch(0, threadGroups, 1, 1);
                return;
            }
            
            // Update bone matrices
            UpdateBoneMatrices();
            boneMatricesBuffer.SetData(boneMatrices);
            
            // Set compute shader parameters
            computeShader.SetInt("_BoneCount", bones.Length);
            
            computeShader.SetBuffer(1, "_BoneMatrices", boneMatricesBuffer);
            computeShader.SetBuffer(1, "_BoneWeights", boneWeightsBuffer);
            computeShader.SetBuffer(1, "_VertexBasePos", baseVertices);
            computeShader.SetBuffer(1, "_VertexModPos", modVertices);
            
            // Dispatch the compute shader
            int vertexThreadGroups = Mathf.CeilToInt(baseVertices.count / 256.0f);
            computeShader.Dispatch(1, vertexThreadGroups, 1, 1);
        }

        public override void DrawSettings()
        {
            GUILayout.Label("Animation Rigging", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            
            // Rig settings
            EditorGUILayout.Space();
            
            rigRoot = EditorGUILayout.ObjectField("Rig Root", rigRoot, typeof(Transform), true) as Transform;
            animationRig = EditorGUILayout.ObjectField("Animation Rig", animationRig, typeof(Rig), true) as Rig;
            autoUpdateWeights = EditorGUILayout.Toggle("Auto Update Weights", autoUpdateWeights);
            
            // Build button
            EditorGUILayout.Space();
            if (rigRoot != null && animationRig != null)
            {
                if (GUILayout.Button("Build Weights"))
                {
                    CollectBonesFromRig();
                    weightsGenerated = true;
                    EditorUtility.SetDirty(this);
                    Debug.Log("Weights built. Reinitialize to apply.");
                }
                
                if (bones != null)
                {
                    EditorGUILayout.LabelField($"Bones found: {bones.Length}");
                }
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(this);
            }
        }

        private void CollectBonesFromRig()
        {
            if (rigRoot == null)
                return;
                
            // Find all transforms under the rig root
            bones = rigRoot.GetComponentsInChildren<Transform>();
            
            // Initialize bone matrices array
            if (bones != null && bones.Length > 0)
            {
                boneMatrices = new Matrix4x4[bones.Length];
                for (int i = 0; i < bones.Length; i++)
                {
                    boneMatrices[i] = Matrix4x4.identity;
                }
            }
        }

        private void UpdateBoneMatrices()
        {
            if (bones == null || boneMatrices == null)
                return;
                
            // Make sure arrays have the same length
            if (boneMatrices.Length != bones.Length)
            {
                boneMatrices = new Matrix4x4[bones.Length];
            }
                
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] != null)
                {
                    boneMatrices[i] = bones[i].localToWorldMatrix;
                }
                else
                {
                    boneMatrices[i] = Matrix4x4.identity;
                }
            }
        }

        private void GenerateWeightsForMesh(Mesh mesh)
        {
            if (bones == null || bones.Length == 0 || mesh == null)
                return;
                
            int vertexCount = mesh.vertexCount;
            Vector3[] vertices = mesh.vertices;
            
            // Create vertex bone data
            VertexBoneData[] vertexBoneData = new VertexBoneData[vertexCount];
            
            // Simple weight generation based on closest bones
            for (int i = 0; i < vertexCount; i++)
            {
                Vector3 vertexPos = vertices[i];
                // Find 4 closest bones
                int[] closestBones = new int[4] { 0, 0, 0, 0 };
                float[] distances = new float[4] { float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue };
                
                for (int b = 0; b < bones.Length; b++)
                {
                    if (bones[b] == null) continue;
                    
                    float dist = Vector3.Distance(vertexPos, bones[b].position);
                    
                    // Insert into sorted array if closer than any existing
                    for (int j = 0; j < 4; j++)
                    {
                        if (dist < distances[j])
                        {
                            // Shift everything down
                            for (int k = 3; k > j; k--)
                            {
                                distances[k] = distances[k-1];
                                closestBones[k] = closestBones[k-1];
                            }
                            distances[j] = dist;
                            closestBones[j] = b;
                            break;
                        }
                    }
                }
                
                // Calculate weights based on inverse distance
                float totalWeight = 0;
                for (int j = 0; j < 4; j++)
                {
                    if (distances[j] < float.MaxValue)
                    {
                        // Add small epsilon to avoid division by zero
                        totalWeight += 1.0f / (distances[j] + 0.001f);
                    }
                }
                
                // Normalize weights
                Vector4 weights = Vector4.zero;
                for (int j = 0; j < 4; j++)
                {
                    if (distances[j] < float.MaxValue)
                    {
                        weights[j] = (1.0f / (distances[j] + 0.001f)) / totalWeight;
                    }
                }
                
                vertexBoneData[i].weights = weights;
                vertexBoneData[i].indices = new Vector4(
                    closestBones[0],
                    closestBones[1],
                    closestBones[2],
                    closestBones[3]
                );
            }
            
            // Create or update the buffer
            if (boneWeightsBuffer == null || boneWeightsBuffer.count != vertexCount)
            {
                if (boneWeightsBuffer != null)
                    boneWeightsBuffer.Release();
                    
                boneWeightsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, vertexCount, sizeof(float) * 8); // 4 weights + 4 indices
            }
            
            boneWeightsBuffer.SetData(vertexBoneData);
            weightsGenerated = true;
        }

        private void CleanupBuffers()
        {
            if (boneMatricesBuffer != null)
            {
                boneMatricesBuffer.Release();
                boneMatricesBuffer = null;
            }
            
            if (boneWeightsBuffer != null)
            {
                boneWeightsBuffer.Release();
                boneWeightsBuffer = null;
            }
        }

        private void OnDestroy()
        {
            CleanupBuffers();
        }
    }
}