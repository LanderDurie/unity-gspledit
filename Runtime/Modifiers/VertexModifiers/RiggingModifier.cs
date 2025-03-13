// using System;
// using System.Collections.Generic;
// using UnityEditor;
// using UnityEngine;
// using UnityEngine.Animations;
// using UnityEngine.Animations.Rigging;
// using Unity.Collections;
// using System.Linq;

// namespace UnityEngine.GsplEdit
// {
//     [System.Serializable]
//     public struct BoneWeight
//     {
//         public int boneIndex0;
//         public float weight0;
//         public int boneIndex1;
//         public float weight1;
//         public int boneIndex2;
//         public float weight2;
//         public int boneIndex3;
//         public float weight3;
//     }
    
//     public class RiggingModifier : Modifier 
//     {
//         private Transform m_RootBone;
//         private List<Transform> m_BonesList = new List<Transform>();
//         private float m_MaxDistance = 0.1f;
//         private int m_MaxBonesPerVertex = 4;
//         private float m_FalloffExponent = 2.0f;
        
//         // Compute resources
//         public ComputeShader m_ComputeShader;
//         private GraphicsBuffer m_RigWeightsBuffer;
//         private BoneWeight[] m_VertexBoneWeights;
//         private bool m_WeightsCalculated = false;
        
//         // Shader property IDs
//         private static readonly int s_BoneTransformsID = Shader.PropertyToID("_BoneTransforms");
//         private static readonly int s_BoneCountID = Shader.PropertyToID("_BoneCount");
        
//         public RiggingModifier(ref SharedComputeContext context, ref VertexSelectionGroup selectionGroup)
//         {
//             m_Context = context;
//             m_SelectionGroup = selectionGroup;
//         }
        
//         ~RiggingModifier()
//         {
//             ReleaseBuffers();
//         }
        
//         private void ReleaseBuffers()
//         {
//             if (m_RigWeightsBuffer != null)
//             {
//                 m_RigWeightsBuffer.Release();
//                 m_RigWeightsBuffer = null;
//             }
//         }
        
//         public override void Run() 
//         {
//             // if (!m_WeightsCalculated || m_RigWeightsBuffer == null)
//             // {
//             //     Debug.LogWarning("Weights not calculated or buffer not initialized. Call CalculateWeights first.");
//             //     return;
//             // }
            
//             // if (m_Context.scaffold == null)
//             //     throw new InvalidOperationException("GraphicsBuffer is not initialized.");
           
//             // Debug.Log(m_ComputeShader);

//             // if (m_ComputeShader == null)
//             //     throw new InvalidOperationException("Compute Shader is not assigned.");
           
//             // int kernel = m_ComputeShader.FindKernel("ApplyRigWeights");
           
//             // // Set buffers
//             // m_ComputeShader.SetBuffer(kernel, "vertexBuffer", m_Context.scaffold);
//             // m_ComputeShader.SetBuffer(kernel, "_VertexSelectedBits", m_SelectionGroup.m_SelectedVerticesBuffer);
//             // m_ComputeShader.SetBuffer(kernel, "_RigWeights", m_RigWeightsBuffer);
            
//             // // Set bone transforms
//             // if (m_BonesList != null && m_BonesList.Count > 0)
//             // {
//             //     Matrix4x4[] boneTransforms = new Matrix4x4[m_BonesList.Count];
//             //     for (int i = 0; i < m_BonesList.Count; i++)
//             //     {
//             //         boneTransforms[i] = m_BonesList[i].localToWorldMatrix;
//             //     }
//             //     m_ComputeShader.SetMatrixArray(s_BoneTransformsID, boneTransforms);
//             //     m_ComputeShader.SetInt(s_BoneCountID, m_BonesList.Count);
//             // }
           
//             // // Calculate thread groups based on vertex count
//             // int threadGroups = Mathf.CeilToInt(m_Context.vertexCount / 256.0f);
//             // m_ComputeShader.Dispatch(kernel, threadGroups, 1, 1);
            
//             // Debug.Log("Applied rig weights to vertices via compute shader.");
//         }
        
//         public override void DrawSettings() 
//         {
//             // // Object field for the root bone
//             // m_RootBone = EditorGUILayout.ObjectField("Root Bone", m_RootBone, typeof(Transform), true) as Transform;
            
//             // // Display bone collection if root bone is assigned
//             // if (m_RootBone != null)
//             // {
//             //     EditorGUILayout.LabelField("Bones", EditorStyles.boldLabel);
                
//             //     // Collect all bones
//             //     m_BonesList.Clear();
//             //     CollectBones(m_RootBone, m_BonesList);
                
//             //     EditorGUI.indentLevel++;
//             //     foreach (Transform bone in m_BonesList)
//             //     {
//             //         EditorGUILayout.LabelField(bone.name);
//             //     }
//             //     EditorGUI.indentLevel--;
                
//             //     EditorGUILayout.Space();
                
//             //     // Weight calculation parameters
//             //     EditorGUILayout.LabelField("Weight Parameters", EditorStyles.boldLabel);
//             //     m_MaxDistance = EditorGUILayout.Slider("Max Distance", m_MaxDistance, 0.01f, 1.0f);
//             //     m_MaxBonesPerVertex = EditorGUILayout.IntSlider("Max Bones Per Vertex", m_MaxBonesPerVertex, 1, 4);
//             //     m_FalloffExponent = EditorGUILayout.Slider("Falloff Exponent", m_FalloffExponent, 1.0f, 4.0f);
                
//             //     EditorGUILayout.Space();
                
//             //     // Button to calculate weights
//             //     if (GUILayout.Button("Calculate Weights"))
//             //     {
//             //         if (m_Context.vertexCount > 0)
//             //         {
//             //             CalculateWeights();
//             //             m_WeightsCalculated = true;
//             //         }
//             //         else
//             //         {
//             //             EditorUtility.DisplayDialog("Error", "No mesh data available in the context.", "OK");
//             //         }
//             //     }
                
//             //     // Display weight stats if available
//             //     if (m_WeightsCalculated && m_VertexBoneWeights != null)
//             //     {
//             //         EditorGUILayout.Space();
//             //         EditorGUILayout.LabelField($"Weights calculated for {m_VertexBoneWeights.Length} vertices");
                    
//             //         if (GUILayout.Button("Apply Weights"))
//             //         {
//             //             Run();
//             //         }
//             //     }
//             // }
//             // else
//             // {
//             //     EditorGUILayout.HelpBox("Please assign a root bone to continue.", MessageType.Info);
//             // }
//         }
        
//         private void CollectBones(Transform rootBone, List<Transform> bonesList)
//         {
//             if (rootBone == null) return;
            
//             bonesList.Add(rootBone);
            
//             foreach (Transform child in rootBone)
//             {
//                 CollectBones(child, bonesList);
//             }
//         }
        
//         private void CalculateWeights() 
//         {
//             // if (m_Context.vertexCount == 0 || m_BonesList.Count == 0)
//             // {
//             //     Debug.LogError("No vertices or bones to calculate weights.");
//             //     return;
//             // }
            
//             // // Get mesh data from compute context
//             // Vector3[] vertices = new Vector3[m_Context.vertexCount];
//             // m_Context.gpuMeshPosData.GetData(vertices);
            
//             // // Initialize the weight storage array
//             // m_VertexBoneWeights = new BoneWeight[vertices.Length];
            
//             // // Apply automatic weighting based on distance
//             // for (int vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
//             // {
//             //     Vector3 VertexPosition = vertices[vertexIndex].position;
                
//             //     // Calculate distances to each bone
//             //     List<BoneDistance> boneDistances = new List<BoneDistance>();
                
//             //     for (int boneIndex = 0; boneIndex < m_BonesList.Count; boneIndex++)
//             //     {
//             //         // Calculate distance from vertex to bone
//             //         Transform bone = m_BonesList[boneIndex];
//             //         float distance = GetDistanceToBone(VertexPosition, bone);
                    
//             //         if (distance <= m_MaxDistance)
//             //         {
//             //             boneDistances.Add(new BoneDistance
//             //             {
//             //                 Index = boneIndex,
//             //                 Distance = distance
//             //             });
//             //         }
//             //     }
                
//             //     // Sort by distance
//             //     boneDistances.Sort((a, b) => a.Distance.CompareTo(b.Distance));
                
//             //     // Take the closest bones up to max allowed
//             //     int boneCount = Mathf.Min(boneDistances.Count, m_MaxBonesPerVertex);
                
//             //     if (boneCount > 0)
//             //     {
//             //         // Calculate weights based on distance
//             //         float[] weights = new float[boneCount];
//             //         float totalWeight = 0f;
                    
//             //         for (int i = 0; i < boneCount; i++)
//             //         {
//             //             // Use inverse distance weighting with a power falloff
//             //             weights[i] = 1.0f / Mathf.Pow(boneDistances[i].Distance + 0.00001f, m_FalloffExponent);
//             //             totalWeight += weights[i];
//             //         }
                    
//             //         // Normalize weights
//             //         if (totalWeight > 0)
//             //         {
//             //             for (int i = 0; i < boneCount; i++)
//             //             {
//             //                 weights[i] /= totalWeight;
//             //             }
//             //         }
                    
//             //         // Create BoneWeight structure
//             //         BoneWeight boneWeight = new BoneWeight();
                    
//             //         // Assign the bone weights
//             //         if (boneCount > 0) { boneWeight.boneIndex0 = boneDistances[0].Index; boneWeight.weight0 = weights[0]; }
//             //         if (boneCount > 1) { boneWeight.boneIndex1 = boneDistances[1].Index; boneWeight.weight1 = weights[1]; }
//             //         if (boneCount > 2) { boneWeight.boneIndex2 = boneDistances[2].Index; boneWeight.weight2 = weights[2]; }
//             //         if (boneCount > 3) { boneWeight.boneIndex3 = boneDistances[3].Index; boneWeight.weight3 = weights[3]; }
                    
//             //         m_VertexBoneWeights[vertexIndex] = boneWeight;
//             //     }
//             //     else
//             //     {
//             //         // No bones within range, assign to root bone with full weight
//             //         m_VertexBoneWeights[vertexIndex] = new BoneWeight
//             //         {
//             //             boneIndex0 = 0,
//             //             weight0 = 1f,
//             //             boneIndex1 = 0,
//             //             weight1 = 0f,
//             //             boneIndex2 = 0,
//             //             weight2 = 0f,
//             //             boneIndex3 = 0,
//             //             weight3 = 0f
//             //         };
//             //     }
//             // }
            
//             // // Create or update the compute buffer
//             // ReleaseBuffers();
//             // m_RigWeightsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, vertices.Length, System.Runtime.InteropServices.Marshal.SizeOf<BoneWeight>());
//             // m_RigWeightsBuffer.SetData(m_VertexBoneWeights);
            
//             // Debug.Log($"Automatic weights calculated and stored in compute buffer for {m_VertexBoneWeights.Length} vertices.");
//         }
        
//         private float GetDistanceToBone(Vector3 VertexPosition, Transform bone)
//         {
//             // Check if this bone has a child
//             if (bone.childCount > 0)
//             {
//                 // Use line segment distance (bone to first child)
//                 Transform childBone = bone.GetChild(0);
//                 return DistancePointLineSegment(VertexPosition, bone.position, childBone.position);
//             }
//             else
//             {
//                 // Use point distance for end bones
//                 return Vector3.Distance(VertexPosition, bone.position);
//             }
//         }
        
//         private float DistancePointLineSegment(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
//         {
//             Vector3 lineDirection = lineEnd - lineStart;
//             float lineLength = lineDirection.magnitude;
//             lineDirection.Normalize();
            
//             Vector3 pointVector = point - lineStart;
//             float dotProduct = Vector3.Dot(pointVector, lineDirection);
            
//             // Clamp to line segment
//             dotProduct = Mathf.Clamp(dotProduct, 0f, lineLength);
            
//             Vector3 nearestPoint = lineStart + lineDirection * dotProduct;
//             return Vector3.Distance(point, nearestPoint);
//         }
        
//         // Helper struct for bone distance calculation
//         private struct BoneDistance
//         {
//             public int Index;
//             public float Distance;
//         }
//     }
// }