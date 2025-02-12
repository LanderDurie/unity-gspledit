
using System;
using UnityEditor;

namespace UnityEngine.GsplEdit{
    
    public class Modifier {
        public ComputeShader m_ComputeShader;
        public String m_Name = "New Modifier";
        public bool m_IsAnimation = false;
        public float m_AnimationSpeed = 1.0f;
        public bool m_Loop = false;
        public float m_LoopDelay = 0.0f;
        public bool m_Enabled = true;
        private SharedComputeContext m_Context;
        private Material m_Material;
        private VertexSelectionGroup m_SelectionGroup;

        public Modifier(ref SharedComputeContext context, ref VertexSelectionGroup selectionGroup) {
            m_Context = context;
            m_SelectionGroup = selectionGroup;
        }

        public void Run() {
            if (m_Context.gpuMeshVerts == null)
                throw new InvalidOperationException("GraphicsBuffer is not initialized.");

            float time = (Application.isPlaying) ? Time.time : (float)EditorApplication.timeSinceStartup;
            int kernel = m_ComputeShader.FindKernel("CSMain");

            // Set the single buffer for read-write access
            m_ComputeShader.SetBuffer(kernel, "vertexBuffer", m_Context.gpuMeshVerts);
            m_ComputeShader.SetBuffer(kernel, "_VertexSelectedBits", m_SelectionGroup.m_SelectedVerticesBuffer);

            // Set time parameter
            m_ComputeShader.SetFloat("time", m_AnimationSpeed * time);

            // Dispatch compute shader
            int threadGroups = Mathf.CeilToInt(m_Context.vertexCount / 256.0f);
            m_ComputeShader.Dispatch(kernel, threadGroups, 1, 1);
        }

        public void Bake() {

        }
    }
}
