using System;
using UnityEditor;
using UnityEngine.Rendering;

namespace UnityEngine.GsplEdit
{
    public class EditableMesh : ScriptableObject
    {

        private Vertex[] m_Vertices;
        private uint[] m_Indices;
        private Edge[] m_Edges;
        
        public ComputeBuffer m_TriangleBuffer;
        public ComputeBuffer m_SelectedVerticesBuffer;
        private ComputeBuffer m_ArgsBuffer;
        private static readonly int ARGS_STRIDE = sizeof(int) * 4;
        public ComputeShader m_CSVertexUtilities;
        public Material m_WireframeMaterial;
        public Material m_SelectedVertexMaterial;
        private CommandBuffer cmd;
        public bool isActive = false;

        public Vector3 m_LocalPos;
        public Vector3 m_LocalScale;
        public Quaternion m_LocalRot;
        public Transform m_GlobalTransform;

        public VertexSelectionGroup m_SelectionGroup;
        public SharedComputeContext m_SharedContext;

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

        public void Initialize(ref SharedComputeContext context, Vertex[] vertices, uint[] indices, Edge[] edges)
        {
            m_Indices = indices;
            m_Vertices = vertices;
            m_Edges = edges;
            m_SharedContext = context;

            m_SharedContext.vertexCount = m_Vertices.Length;
            m_SharedContext.edgeCount = m_Edges.Length;

            m_LocalPos = new Vector3();
            m_LocalRot = new Quaternion(0, 0, 0, 1);
            m_LocalScale = new Vector3();

            CreateBuffers();

            m_SelectionGroup = new VertexSelectionGroup(m_Vertices, this, m_CSVertexUtilities);
        }

        [System.Obsolete]
        private void OnEnable()
        {
                           cmd = new CommandBuffer
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
            Draw(sceneView.camera);
        }

        public void Update()
        {
            Draw(Camera.main);
        }

        private bool AreBuffersValid()
        {
            if (m_SharedContext != null && m_SharedContext.gpuMeshVerts == null || m_TriangleBuffer == null || m_SelectedVerticesBuffer == null)
                return false;

            return true;
        }

        private unsafe void CreateBuffers()
        {
            try
            {
                // Create vertex buffer
                if (m_SharedContext.vertexCount > 0)
                {
                    m_SharedContext.gpuMeshVerts = new ComputeBuffer(m_SharedContext.vertexCount, sizeof(Vertex));
                    m_SharedContext.gpuMeshVerts.SetData(m_Vertices);
                }

                // Create triangle buffer
                if (m_Indices.Length > 0)
                {
                    m_TriangleBuffer = new ComputeBuffer(m_Indices.Length, sizeof(int));
                    m_TriangleBuffer.SetData(m_Indices);
                }

                m_SharedContext.gpuMeshEdges = new ComputeBuffer(m_SharedContext.edgeCount, sizeof(Edge));
                m_SharedContext.gpuMeshEdges.SetData(m_Edges);

                // Create selection buffer (using uint for bit operations, 32 vertices per uint)
                int selectionBufferSize = (m_SharedContext.vertexCount + 31) / 32;
                if (selectionBufferSize > 0)
                {
                    m_SelectedVerticesBuffer = new ComputeBuffer(selectionBufferSize, sizeof(uint));
                    uint[] clearData = new uint[selectionBufferSize];
                    m_SelectedVerticesBuffer.SetData(clearData);
                }

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
            if (m_SharedContext == null)
                return;
                
            if (m_SharedContext.gpuMeshVerts != null)
            {
                m_SharedContext.gpuMeshVerts.Release();
                m_SharedContext.gpuMeshVerts = null;
            }

            if (m_TriangleBuffer != null)
            {
                m_TriangleBuffer.Release();
                m_TriangleBuffer = null;
            }

            if (m_SelectedVerticesBuffer != null)
            {
                m_SelectedVerticesBuffer.Release();
                m_SelectedVerticesBuffer = null;
            }

            m_SharedContext.vertexCount = 0;
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
            cmb.SetComputeBufferParam(m_CSVertexUtilities, kernelIndex, "_VertexProps", m_SharedContext.gpuMeshVerts);
            cmb.SetComputeBufferParam(m_CSVertexUtilities, kernelIndex, Props.VertexSelectedBits, m_SelectedVerticesBuffer);
            cmb.SetComputeIntParam(m_CSVertexUtilities, Props.VertexCount, m_SharedContext.vertexCount);
            cmb.SetComputeVectorParam(m_CSVertexUtilities, "_SelectionRect", new Vector4(rectMin.x, rectMax.y, rectMax.x, rectMin.y));

            cmb.SetComputeMatrixParam(m_CSVertexUtilities, Props.MatrixObjectToWorld, matO2W);
            cmb.SetComputeMatrixParam(m_CSVertexUtilities, Props.MatrixVP, matProj * matView);
            cmb.SetComputeVectorParam(m_CSVertexUtilities, Props.VecScreenParams, screenPar);
            DispatchUtilsAndExecute(cmb, KernelIndices.SelectionUpdate, m_SharedContext.vertexCount);

            SetSelection();
        }

        public void DeselectAll()
        {
            int selectionBufferSize = (m_Vertices.Length + 31) / 32;
            if (selectionBufferSize > 0)
            {
                uint[] clearData = new uint[selectionBufferSize];
                m_SelectedVerticesBuffer.SetData(clearData);
            }
        }

        public void SelectAll()
        {
            int selectionBufferSize = (m_Vertices.Length + 31) / 32;
            if (selectionBufferSize > 0)
            {
                uint[] clearData = new uint[selectionBufferSize];
                Array.Fill(clearData, uint.MaxValue);
                m_SelectedVerticesBuffer.SetData(clearData);
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
            cmb.SetComputeBufferParam(m_CSVertexUtilities, kernelIndex, "_VertexProps", m_SharedContext.gpuMeshVerts);
            cmb.SetComputeBufferParam(m_CSVertexUtilities, kernelIndex, Props.VertexSelectedBits, m_SelectedVerticesBuffer);
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
            uint[] selectedBits = new uint[(m_SharedContext.vertexCount + 31) / 32];
            m_SelectedVerticesBuffer.GetData(selectedBits);

            for (int i = 0; i < m_SharedContext.vertexCount; i++)
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

        public void DrawSelectedVertices()
        {
            if (m_SharedContext == null || m_SharedContext.gpuMeshVerts == null || m_SelectedVerticesBuffer == null || !m_SelectedVertexMaterial)
                return;

            // Set up material properties
            m_SelectedVertexMaterial.SetBuffer("_VertexProps", m_SharedContext.gpuMeshVerts);
            m_SelectedVertexMaterial.SetBuffer("_VertexSelectedBits", m_SelectedVerticesBuffer);
            m_SelectedVertexMaterial.SetMatrix("_ObjectToWorld", m_GlobalTransform.localToWorldMatrix);

            // Set up the draw command
            cmd.DrawProcedural(Matrix4x4.identity, m_SelectedVertexMaterial, 0, MeshTopology.Points, m_Vertices.Length);

        }

        public void DrawWireframe()
        {
            if (!AreBuffersValid() || m_SharedContext.gpuMeshVerts == null || !m_WireframeMaterial)
                return;

            // Set up material properties
            m_WireframeMaterial.SetBuffer("_VertexProps", m_SharedContext.gpuMeshVerts);
            m_WireframeMaterial.SetBuffer("_IndexBuffer", m_TriangleBuffer);
            m_WireframeMaterial.SetMatrix("_ObjectToWorld", m_GlobalTransform.localToWorldMatrix);

            // Draw back faces
            m_WireframeMaterial.SetPass(0);
            cmd.DrawProceduralIndirect(
                Matrix4x4.identity,
                m_WireframeMaterial,
                0,
                MeshTopology.Triangles,
                m_ArgsBuffer,
                0
            );

            // Draw front faces
            m_WireframeMaterial.SetPass(1);
            cmd.DrawProceduralIndirect(
                Matrix4x4.identity,
                m_WireframeMaterial,
                1,
                MeshTopology.Triangles,
                m_ArgsBuffer,
                0
            );
        }

        private void Draw(Camera camera)
        {
            cmd.Clear();
            DrawWireframe();
            DrawSelectedVertices();
            Graphics.ExecuteCommandBuffer(cmd);
        }
    }
}