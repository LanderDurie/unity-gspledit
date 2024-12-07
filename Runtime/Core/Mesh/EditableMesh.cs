using System;
using UnityEditor;
using UnityEditor.SearchService;
using UnityEngine.Rendering;

namespace UnityEngine.GsplEdit
{
    public class EditableMesh : ScriptableObject
    {

        private Vertex[] m_Vertices;
        private uint[] m_Indices;
        private Edge[] m_Edges;
        
        public ComputeBuffer m_IndexBuffer;
        public GraphicsBuffer m_VertexBuffer; // Stores base vertices before running modifier system
        private ComputeBuffer m_ArgsBuffer;
        private static readonly int ARGS_STRIDE = sizeof(int) * 4;
        public ComputeShader m_CSVertexUtilities;
        public Material m_WireframeMaterial;
        public Material m_SelectedVertexMaterial;
        private CommandBuffer m_Cmd;
        public bool m_StaticModifierPass = true;
        public bool m_DynamicModifierPass = true;

        public Vector3 m_LocalPos;
        public Vector3 m_LocalScale;
        public Quaternion m_LocalRot;
        public Transform m_GlobalTransform;

        public VertexSelectionGroup m_SelectionGroup;
        public SharedComputeContext m_Context;
        public ModifierSystem m_ModifierSystem;

        internal static class Props
        {
            public static readonly int VertexPos = Shader.PropertyToID("_VertexPos");
            public static readonly int VertexSelectedBits = Shader.PropertyToID("_VertexSelectedBits");
            public static readonly int VertexCount = Shader.PropertyToID("_VertexCount");
            public static readonly int MatrixVP = Shader.PropertyToID("_MatrixVP");
            public static readonly int MatrixMV = Shader.PropertyToID("_MatrixMV");
            public static readonly int MatrixP = Shader.PropertyToID("_MatrixP");
            public static readonly int MatrixObjectToWorld = Shader.PropertyToID("_MatrixObjectToWorld");
            public static readonly int MatrixWorldToObject = Shader.PropertyToID("_MatrixWorldToObject");
            public static readonly int VecScreenParams = Shader.PropertyToID("_VecScreenParams");
            public static readonly int VecWorldSpaceCameraPos = Shader.PropertyToID("_VecWorldSpaceCameraPos");
        }

        enum KernelIndices
        {
            SelectionUpdate,
            VertexTransform
        }

        public void Initialize(ref SharedComputeContext context, ref ModifierSystem modSystem, Vertex[] vertices, uint[] indices, Edge[] edges)
        {
            m_Indices = indices;
            m_Vertices = vertices;
            m_Edges = edges;
            m_Context = context;
            m_ModifierSystem = modSystem;

            m_Context.vertexCount = m_Vertices.Length;
            m_Context.edgeCount = m_Edges.Length;

            m_LocalPos = new Vector3();
            m_LocalRot = new Quaternion(0, 0, 0, 1);
            m_LocalScale = new Vector3();

            CreateBuffers();

            m_SelectionGroup = new VertexSelectionGroup(ref m_Vertices);
        }

        [System.Obsolete]
        private void OnEnable()
        {
                           m_Cmd = new CommandBuffer
                    {
                        name = "Vertex Drawing"
                    };
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
            SceneView.onSceneGUIDelegate += OnSceneGUI;
        }

        public void Destroy()
        {
            DestroyBuffers();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            Draw();
        }

        public void UpdateDraw()
        {
            Draw();
        }

        private bool AreBuffersValid()
        {
            if (m_Context != null && m_VertexBuffer == null || m_Context == null || m_Context.gpuMeshVerts == null || m_IndexBuffer == null || m_SelectionGroup.m_SelectedVerticesBuffer == null)
                return false;

            return true;
        }

        private unsafe void CreateBuffers()
        {
            try
            {
                // Create vertex buffer
                if (m_Context.vertexCount > 0)
                {
                    m_VertexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw | GraphicsBuffer.Target.CopySource, m_Context.vertexCount, sizeof(Vertex)) { name = "vertices" };
                    m_VertexBuffer.SetData(m_Vertices);
                    m_Context.gpuMeshVerts = new GraphicsBuffer(GraphicsBuffer.Target.Raw | GraphicsBuffer.Target.CopyDestination, m_Context.vertexCount, sizeof(Vertex)) { name = "modifiedVertices" };
                }

                // Create triangle buffer
                if (m_Indices.Length > 0)
                {
                    m_IndexBuffer = new ComputeBuffer(m_Indices.Length, sizeof(int));
                    m_IndexBuffer.SetData(m_Indices);
                }

                m_Context.gpuMeshEdges = new ComputeBuffer(m_Context.edgeCount, sizeof(Edge));
                m_Context.gpuMeshEdges.SetData(m_Edges);

                // Create arguments buffer
                if (m_ArgsBuffer != null)
                    m_ArgsBuffer.Release();

                // Arguments for DrawProceduralIndirect
                uint[] args = new uint[4]
                {
            (uint)m_Indices.Length,    // Index count per instance
            1,                           // Instance count
            0,                           // Start index location
            0                            // Base vertex location
                };

                m_ArgsBuffer = new ComputeBuffer(1, ARGS_STRIDE, ComputeBufferType.IndirectArguments);
                m_ArgsBuffer.SetData(args);

            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create buffers: {e.Message}");
                DestroyBuffers();
            }
        }

        public void DestroyBuffers()
        {
            if (m_Context == null)
                return;
                
            if (m_VertexBuffer != null)
            {
                m_VertexBuffer.Release();
                m_VertexBuffer = null;
                m_Context.gpuMeshVerts.Release();
                m_Context.gpuMeshVerts = null;
            }

            if (m_IndexBuffer != null)
            {
                m_IndexBuffer.Release();
                m_IndexBuffer = null;
            }

            if (m_SelectionGroup.m_SelectedVerticesBuffer != null)
            {
                m_SelectionGroup.m_SelectedVerticesBuffer.Release();
                m_SelectionGroup.m_SelectedVerticesBuffer = null;
            }

            m_Context.vertexCount = 0;
        }

        public void EditUpdateSelection(Vector2 rectMin, Vector2 rectMax, Camera cam)
        {
            // Validate buffers before proceeding
            if (!AreBuffersValid())
            {
                Debug.LogWarning("Cannot update selection: buffers not valid or compute shader missing");
                return;
            }

            Matrix4x4 matView = cam.worldToCameraMatrix;
            Matrix4x4 matProj = GL.GetGPUProjectionMatrix(cam.projectionMatrix, true);
            Matrix4x4 matO2W = Matrix4x4.identity;
            int screenW = cam.pixelWidth, screenH = cam.pixelHeight;
            Vector4 screenPar = new Vector4(screenW, screenH, 0, 0);

            using var cmb = new CommandBuffer { name = "VertexSelectionUpdate" };
            int kernelIndex = (int)KernelIndices.SelectionUpdate;
            cmb.SetComputeBufferParam(m_CSVertexUtilities, kernelIndex, "_VertexProps", m_VertexBuffer);
            cmb.SetComputeBufferParam(m_CSVertexUtilities, kernelIndex, Props.VertexSelectedBits, m_SelectionGroup.m_SelectedVerticesBuffer);
            cmb.SetComputeIntParam(m_CSVertexUtilities, Props.VertexCount, m_Context.vertexCount);
            cmb.SetComputeVectorParam(m_CSVertexUtilities, "_SelectionRect", new Vector4(rectMin.x, rectMax.y, rectMax.x, rectMin.y));

            cmb.SetComputeMatrixParam(m_CSVertexUtilities, Props.MatrixObjectToWorld, matO2W);
            cmb.SetComputeMatrixParam(m_CSVertexUtilities, Props.MatrixVP, matProj * matView);
            cmb.SetComputeVectorParam(m_CSVertexUtilities, Props.VecScreenParams, screenPar);
            DispatchUtilsAndExecute(cmb, KernelIndices.SelectionUpdate, m_Context.vertexCount);

            SetSelection();
        }

        public void DeselectAll()
        {
            int selectionBufferSize = (m_Vertices.Length + 31) / 32;
            if (selectionBufferSize > 0)
            {
                uint[] clearData = new uint[selectionBufferSize];
                m_SelectionGroup.m_SelectedVerticesBuffer.SetData(clearData);
            }
        }

        public void SelectAll()
        {
            int selectionBufferSize = (m_Vertices.Length + 31) / 32;
            if (selectionBufferSize > 0)
            {
                uint[] clearData = new uint[selectionBufferSize];
                Array.Fill(clearData, uint.MaxValue);
                m_SelectionGroup.m_SelectedVerticesBuffer.SetData(clearData);
            }
        }

        public void EditVertexTransformation(Vector3 positionDiff, Vector4 rotationDiff, Vector3 scaleDiff)
        {
            // Validate buffers before proceeding
            if (!AreBuffersValid() || m_CSVertexUtilities == null)
            {
                Debug.LogWarning("Cannot update selection: buffers not valid or compute shader missing");
                return;
            }

            using var cmb = new CommandBuffer { name = "VertexSelectionUpdate" };
            int kernelIndex = (int)KernelIndices.VertexTransform;
            cmb.SetComputeBufferParam(m_CSVertexUtilities, kernelIndex, "_VertexProps", m_VertexBuffer);
            cmb.SetComputeBufferParam(m_CSVertexUtilities, kernelIndex, Props.VertexSelectedBits, m_SelectionGroup.m_SelectedVerticesBuffer);
            cmb.SetComputeIntParam(m_CSVertexUtilities, Props.VertexCount, m_Vertices.Length);
            cmb.SetComputeVectorParam(m_CSVertexUtilities, "_PositionDiff", positionDiff);
            cmb.SetComputeVectorParam(m_CSVertexUtilities, "_RotationDiff", rotationDiff);
            cmb.SetComputeVectorParam(m_CSVertexUtilities, "_ScaleDiff", scaleDiff);
            cmb.SetComputeVectorParam(m_CSVertexUtilities, "_PivotPoint", m_LocalPos);

            DispatchUtilsAndExecute(cmb, KernelIndices.VertexTransform, m_Vertices.Length);
        }

        void DispatchUtilsAndExecute(CommandBuffer cmb, KernelIndices kernel, int count)
        {
            m_CSVertexUtilities.GetKernelThreadGroupSizes((int)kernel, out uint gsX, out _, out _);
            cmb.DispatchCompute(m_CSVertexUtilities, (int)kernel, (int)((count + gsX - 1) / gsX), 1, 1);
            Graphics.ExecuteCommandBuffer(cmb);
        }



        public void SetSelection()
        {
            if (!AreBuffersValid())
            {
                Debug.LogWarning("Buffers are not valid or no base mesh found.");
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

            m_LocalPos = m_SelectionGroup.m_CenterPos;
            EditorUtility.SetDirty(this);
        }

        public void SetSelectionBuffer() {
            if (!AreBuffersValid())
            {
                Debug.LogWarning("Buffers are not valid or no base mesh found.");
                return;
            }

            Debug.Log(m_SelectionGroup.m_SelectedCount);

            m_SelectionGroup.m_SelectedVerticesBuffer.SetData(m_SelectionGroup.m_SelectedBits);
        }

        public void DrawSelectedVertices()
        {
            if (m_Context == null || m_Context.gpuMeshVerts == null || m_SelectionGroup.m_SelectedVerticesBuffer == null || !m_SelectedVertexMaterial)
                return;

            // Set up material properties
            m_SelectedVertexMaterial.SetBuffer("_VertexProps", m_Context.gpuMeshVerts);
            m_SelectedVertexMaterial.SetBuffer("_VertexSelectedBits", m_SelectionGroup.m_SelectedVerticesBuffer);
            m_SelectedVertexMaterial.SetMatrix("_ObjectToWorld", m_GlobalTransform.localToWorldMatrix);

            // Set up the draw command
            m_Cmd.DrawProcedural(Matrix4x4.identity, m_SelectedVertexMaterial, 0, MeshTopology.Points, m_Vertices.Length);

        }

        public void DrawWireframe()
        {
            if (!AreBuffersValid() || m_Context.gpuMeshVerts == null || !m_WireframeMaterial)
                return;

            // Set up material properties
            m_WireframeMaterial.SetBuffer("_VertexProps", m_Context.gpuMeshVerts);
            m_WireframeMaterial.SetBuffer("_IndexBuffer", m_IndexBuffer);
            m_WireframeMaterial.SetMatrix("_ObjectToWorld", m_GlobalTransform.localToWorldMatrix);

            // Draw back faces
            m_WireframeMaterial.SetPass(0);
            m_Cmd.DrawProceduralIndirect(
                Matrix4x4.identity,
                m_WireframeMaterial,
                0,
                MeshTopology.Triangles,
                m_ArgsBuffer,
                0
            );

            // Draw front faces
            m_WireframeMaterial.SetPass(1);
            m_Cmd.DrawProceduralIndirect(
                Matrix4x4.identity,
                m_WireframeMaterial,
                1,
                MeshTopology.Triangles,
                m_ArgsBuffer,
                0
            );
        }

        private void Draw()
        {
            m_Cmd.Clear();

            if (!AreBuffersValid())
                return;

            // Apply Modifier System
            m_ModifierSystem.RunAll(m_StaticModifierPass, m_DynamicModifierPass);

            DrawWireframe();
            DrawSelectedVertices();
            Graphics.ExecuteCommandBuffer(m_Cmd);
        }
    }
}