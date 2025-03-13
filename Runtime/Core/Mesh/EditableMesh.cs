using System;
using UnityEditor;
using UnityEngine.Rendering;

namespace UnityEngine.GsplEdit
{
    public class EditableMesh {
        public Material m_ScaffoldMaterial;
        public Material m_SurfaceMaterial;
        public Material m_ShadowCasterMaterial;
        public Material m_PostProcessMaterial;
        public GameObject m_DebugPlane;
        public Vector3 m_SelectedPos;
        public Vector3 m_SelectedScale;
        public Quaternion m_SelectedRot;
        public Transform m_GlobalTransform;
        public bool m_CastShadows = true;
        public bool m_ReceiveShadows = true;
        public bool m_DrawScaffoldMesh = false;
        public ComputeShader m_CSEditMesh;

        private OffscreenRendering m_OffscreenRenderer;
        public VertexSelectionGroup m_SelectionGroup;
        private SharedComputeContext m_Context;
        private ModifierSystem m_ModifierSystem;

        internal static class Props {
            public static readonly int VertexModPos = Shader.PropertyToID("_VertexModPos");
            public static readonly int VertexSelectedBits = Shader.PropertyToID("_VertexSelectedBits");
            public static readonly int VertexCount = Shader.PropertyToID("_VertexCount");
            public static readonly int MatrixVP = Shader.PropertyToID("_MatrixVP");
            public static readonly int MatrixObjectToWorld = Shader.PropertyToID("_MatrixObjectToWorld");
            public static readonly int VecScreenParams = Shader.PropertyToID("_VecScreenParams");
            public static readonly int SelectionRect = Shader.PropertyToID("_SelectionRect");
        }

        enum KernelIndices{SelectionUpdate, VertexTransform}

        public void Initialize(ref SharedComputeContext context, ref ModifierSystem modSystem, Mesh scaffoldMesh) {
            m_Context = context;
            m_Context.scaffoldMesh = scaffoldMesh;
            m_ModifierSystem = modSystem;

            m_SelectedPos = Vector3.zero;
            m_SelectedScale = Vector3.one;
            m_SelectedRot = Quaternion.identity;

            CreateBuffers();

            m_SelectionGroup = new VertexSelectionGroup(ref m_Context.scaffoldMesh);
            m_OffscreenRenderer = new OffscreenRendering(ref m_Context);
        }

        public void Destroy() {
            DestroyBuffers();

            // Clean up the offscreen renderer
            if (m_OffscreenRenderer != null) {
                (m_OffscreenRenderer as IDisposable)?.Dispose();
                m_OffscreenRenderer = null;
            }

            // Clean up the selection group
            m_SelectionGroup?.Destroy();
            m_SelectionGroup = null;

            // Clean up the scaffold mesh
            if (m_Context.scaffoldMesh != null) {
                UnityEngine.Object.DestroyImmediate(m_Context.scaffoldMesh);
                m_Context.scaffoldMesh = null;
            }
        }

        private bool IsValid() {
            return !(m_Context == null || m_Context.scaffoldMesh == null || !m_Context.IsValid());
        }

        private unsafe void CreateBuffers() {
            try {
                // Create vertex buffer
                if (m_Context.scaffoldMesh.vertices.Length > 0 && m_Context.scaffoldMesh.triangles.Length > 0) {
                    m_Context.vertexCount = m_Context.scaffoldMesh.vertices.Length;
                    m_Context.indexCount = m_Context.scaffoldMesh.triangles.Length;
                    m_Context.gpuMeshModVertex = new GraphicsBuffer(GraphicsBuffer.Target.Raw | GraphicsBuffer.Target.CopySource, m_Context.scaffoldMesh.vertices.Length, sizeof(Vector3)) { name = "MeshBaseVertices" };
                    m_Context.gpuMeshModVertex.SetData(m_Context.scaffoldMesh.vertices);
                    
                    m_Context.gpuMeshBaseVertex = new GraphicsBuffer(GraphicsBuffer.Target.Raw | GraphicsBuffer.Target.CopySource, m_Context.scaffoldMesh.vertices.Length, sizeof(Vector3)) { name = "MeshModVertices" };
                    m_Context.gpuMeshBaseVertex.SetData(m_Context.scaffoldMesh.vertices);

                    m_Context.gpuMeshIndices = new GraphicsBuffer(GraphicsBuffer.Target.Raw | GraphicsBuffer.Target.CopySource, m_Context.scaffoldMesh.triangles.Length, sizeof(int)) { name = "MeshIndices" };
                    m_Context.gpuMeshIndices.SetData(m_Context.scaffoldMesh.triangles);
                }
            }
            catch (System.Exception e) {
                Debug.LogError($"Failed to create buffers: {e.Message}");
                DestroyBuffers();
            }
        }

        public void DestroyBuffers() {
            if (m_Context != null) {
                m_Context.gpuMeshModVertex?.Dispose();
                m_Context.gpuMeshModVertex = null;
                m_Context.gpuMeshBaseVertex?.Dispose();
                m_Context.gpuMeshBaseVertex = null;
                m_Context.vertexCount = 0;
                m_Context.indexCount = 0;
            }
        }

        public void EditUpdateSelection(Vector2 rectMin, Vector2 rectMax, Camera cam) {
            if (!IsValid()) {
                Debug.LogWarning("Cannot update selection: buffers not IsValid or compute shader missing");
                return;
            }

            Matrix4x4 matView = cam.worldToCameraMatrix;
            Matrix4x4 matProj = GL.GetGPUProjectionMatrix(cam.projectionMatrix, true);
            Matrix4x4 matO2W = Matrix4x4.identity;
            int screenW = cam.pixelWidth, screenH = cam.pixelHeight;
            Vector4 screenPar = new Vector4(screenW, screenH, 0, 0);

            try {
                using var cmb = new CommandBuffer { name = "VertexSelectionUpdate" };
                int kernelIndex = (int)KernelIndices.SelectionUpdate;
                cmb.SetComputeBufferParam(m_CSEditMesh, kernelIndex, Props.VertexModPos, m_Context.gpuMeshModVertex);
                cmb.SetComputeBufferParam(m_CSEditMesh, kernelIndex, Props.VertexSelectedBits, m_SelectionGroup.m_SelectedVerticesBuffer);

                cmb.SetComputeIntParam(m_CSEditMesh, Props.VertexCount, m_Context.vertexCount);
                cmb.SetComputeVectorParam(m_CSEditMesh, Props.SelectionRect, new Vector4(rectMin.x, rectMax.y, rectMax.x, rectMin.y));

                cmb.SetComputeMatrixParam(m_CSEditMesh, Props.MatrixObjectToWorld, matO2W);
                cmb.SetComputeMatrixParam(m_CSEditMesh, Props.MatrixVP, matProj * matView);
                cmb.SetComputeVectorParam(m_CSEditMesh, Props.VecScreenParams, screenPar);
                DispatchUtilsAndExecute(cmb, KernelIndices.SelectionUpdate, m_Context.vertexCount);
                SetSelection();
            } catch (Exception e) {
                Debug.LogError($"Failed to update Selection: {e.Message}");
                DestroyBuffers();
            }
        }

        public void DeselectAll() {
            int selectionBufferSize = (m_Context.scaffoldMesh.vertices.Length + 31) / 32;
            if (selectionBufferSize > 0) {
                uint[] clearData = new uint[selectionBufferSize];
                m_SelectionGroup.m_SelectedVerticesBuffer.SetData(clearData);
            }
        }

        public void SelectAll() {
            int selectionBufferSize = (m_Context.scaffoldMesh.vertices.Length + 31) / 32;
            if (selectionBufferSize > 0) {
                uint[] clearData = new uint[selectionBufferSize];
                Array.Fill(clearData, uint.MaxValue);
                m_SelectionGroup.m_SelectedVerticesBuffer.SetData(clearData);
            }
        }

        public void EditVertexTransformation(Vector3 positionDiff, Vector4 rotationDiff, Vector3 scaleDiff) {
            // IsValidate buffers before proceeding
            if (!IsValid() || m_CSEditMesh == null) {
                Debug.LogWarning("Cannot update selection: buffers not IsValid or compute shader missing");
                return;
            }
            using var cmb = new CommandBuffer { name = "VertexSelectionUpdate" };
            int kernelIndex = (int)KernelIndices.VertexTransform;
            cmb.SetComputeBufferParam(m_CSEditMesh, kernelIndex, Props.VertexModPos, m_Context.gpuMeshModVertex);
            cmb.SetComputeBufferParam(m_CSEditMesh, kernelIndex, Props.VertexSelectedBits, m_SelectionGroup.m_SelectedVerticesBuffer);
            cmb.SetComputeIntParam(m_CSEditMesh, Props.VertexCount, m_Context.vertexCount);
            cmb.SetComputeVectorParam(m_CSEditMesh, "_PositionDiff", positionDiff);
            cmb.SetComputeVectorParam(m_CSEditMesh, "_RotationDiff", rotationDiff);
            cmb.SetComputeVectorParam(m_CSEditMesh, "_ScaleDiff", scaleDiff);
            cmb.SetComputeVectorParam(m_CSEditMesh, "_PivotPoint", m_SelectedPos);

            DispatchUtilsAndExecute(cmb, KernelIndices.VertexTransform, m_Context.vertexCount);

            // Update mesh with modified positions

            // Read modified vertex data back from GPU
            Vector3[] modifiedVertices = new Vector3[m_Context.vertexCount];
            m_Context.gpuMeshModVertex.GetData(modifiedVertices);

            // Update the mesh vertices
            m_Context.scaffoldMesh.vertices = modifiedVertices;

            // Recalculate normals and bounds
            m_Context.scaffoldMesh.RecalculateNormals();
            m_Context.scaffoldMesh.RecalculateBounds();
        }

        void DispatchUtilsAndExecute(CommandBuffer cmb, KernelIndices kernel, int count) {
            m_CSEditMesh.GetKernelThreadGroupSizes((int)kernel, out uint gsX, out _, out _);
            cmb.DispatchCompute(m_CSEditMesh, (int)kernel, (int)((count + gsX - 1) / gsX), 1, 1);
            Graphics.ExecuteCommandBuffer(cmb);
        }

        public void SetSelection() {
            if (!IsValid())
            {
                Debug.LogWarning("Buffers are not IsValid or no base mesh found.");
                return;
            }

            // Retrieve selected vertices data from the buffer
            uint[] selectedBits = new uint[(m_Context.vertexCount + 31) / 32];
            m_SelectionGroup.m_SelectedVerticesBuffer.GetData(selectedBits);

            for (int i = 0; i < m_Context.vertexCount; i++)
            {
                int bitIndex = i / 32;
                int bitOffset = i % 32;

                if ((selectedBits[bitIndex] & (1u << bitOffset)) != 0)
                {
                    if (!m_SelectionGroup.IsSelected(i))
                    {
                        m_SelectionGroup.AddVertex(i);
                    }
                }
                else
                {
                    if (m_SelectionGroup.IsSelected(i))
                    {
                        m_SelectionGroup.RemoveVertex(i);
                    }
                }
            }

            m_SelectedPos = m_SelectionGroup.m_CenterPos;
        }

        public void SetSelectionBuffer() {
            if (!IsValid()) {
                Debug.LogWarning("Buffers are not IsValid or no base mesh found.");
                return;
            }

            m_SelectionGroup.m_SelectedVerticesBuffer.SetData(m_SelectionGroup.m_SelectedBits);
        }

        private void DrawScaffold() {
            if (m_Context.scaffoldMesh == null) {
                Debug.LogWarning("Scaffold mesh is not assigned.");
                return;
            }

            if (m_ScaffoldMaterial == null) {
                Debug.LogError("Material is not assigned.");
                return;
            }

            m_ScaffoldMaterial.SetBuffer("_VertexSelectedBits", m_SelectionGroup.m_SelectedVerticesBuffer);

            Graphics.DrawMesh(
                m_Context.scaffoldMesh,
                m_GlobalTransform.localToWorldMatrix,
                m_ScaffoldMaterial,
                layer: 0,
                camera: SceneView.lastActiveSceneView.camera,
                submeshIndex: 0,
                null
            );
        }

        private void DrawShadowCaster() {
            if (m_Context.scaffoldMesh == null) {
                Debug.LogWarning("Scaffold mesh is not assigned.");
                return;
            }

            if (m_ShadowCasterMaterial == null) {
                Debug.LogError("Material is not assigned.");
                return;
            }

            // Draw the mesh using Graphics.DrawMesh
            Graphics.DrawMesh(
                m_Context.scaffoldMesh,
                m_GlobalTransform.localToWorldMatrix,
                m_ShadowCasterMaterial,
                layer: 0,
                camera: null,
                submeshIndex: 0,
                null
            );
        }

        public void Draw(Renderer[] renderers) {
            if (m_Context.scaffoldMesh == null || m_Context.scaffoldMesh.triangles.Length == 0 || m_Context.scaffoldMesh.vertices.Length == 0)
                return;

            // Apply Modifier System
            m_ModifierSystem.RunAll();

            if (m_DrawScaffoldMesh) {
                DrawScaffold();
            }

            if (m_CastShadows) {
                DrawShadowCaster();
            }

            // m_SurfaceMaterial.SetTexture("_MainTex", m_Context.splatColorMap);
            // m_SurfaceMaterial.SetTexture("_BumpMap", m_Context.splatNormalMap);
            // m_SurfaceMaterial.EnableKeyword("_NORMALMAP");

            m_OffscreenRenderer.m_DebugPlane = m_DebugPlane;
            m_OffscreenRenderer.m_SurfaceMesh = m_Context.scaffoldMesh;
            m_OffscreenRenderer.m_SurfaceMaterial = m_SurfaceMaterial;
            m_OffscreenRenderer.m_GlobalTransform = m_GlobalTransform;
            m_OffscreenRenderer.m_ReceiveShadows = m_ReceiveShadows;
            // m_OffscreenRenderer.m_PostProcessMaterial = m_PostProcessMaterial;
            m_OffscreenRenderer.Render(renderers);
        }
    }
}