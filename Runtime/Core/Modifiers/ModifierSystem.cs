using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.GsplEdit {
    public class ModifierSystem {
        public List<SelectionGroup> m_SelectionGroups;
        private SharedComputeContext m_Context;
        private EditableMesh m_Mesh;
        private GraphicsBuffer m_ModifierBuffer;
        private ComputeShader m_CSBufferOps;

        public ModifierSystem(ref SharedComputeContext context, ComputeShader csBufferOps) {
            m_Context = context;
            m_CSBufferOps = csBufferOps;
            m_SelectionGroups = new List<SelectionGroup>(new SelectionGroup[context.modifierData.groups.Count]);

            // Initialize Groups in the correct order
            for (int i = 0; i < context.modifierData.groups.Count; i++) {
                int index = context.modifierData.groups[i].order;
                m_SelectionGroups[index] = new SelectionGroup(ref m_Context, context.modifierData.groups[i], csBufferOps);
            }
        }

        public ModifierSystem(ref SharedComputeContext context, ref EditableMesh mesh, ComputeShader csBufferOps) {
            m_Mesh = mesh;
            m_Context = context;
            m_CSBufferOps = csBufferOps;
            m_SelectionGroups = new List<SelectionGroup>(new SelectionGroup[context.modifierData.groups.Count]);

            // Initialize Groups in the correct order
            for (int i = 0; i < context.modifierData.groups.Count; i++) {
                int index = context.modifierData.groups[i].order;
                m_SelectionGroups[index] = new SelectionGroup(ref m_Context, context.modifierData.groups[i], csBufferOps);
            }
        }

        unsafe public void SetMesh(ref EditableMesh mesh) {
            m_Mesh = mesh;
            m_ModifierBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw | GraphicsBuffer.Target.CopySource, m_Context.scaffoldData.vertexCount, sizeof(Vector3)) { name = "ModifierVertices" };;
            m_ModifierBuffer.SetData(new Vector3[m_Context.scaffoldData.vertexCount]); //Init 0
        }

        public void Destroy() {
            foreach (SelectionGroup group in m_SelectionGroups) {
                group?.Destroy();
            }
            m_ModifierBuffer?.Dispose();
            m_ModifierBuffer = null;
        }

        public SelectionGroup Insert() {
            GroupMeta gm = new();
            gm.enabled = true;
            gm.name = "New Group";
            gm.selection = m_Mesh.m_SelectionGroup.m_SelectedBits;
            gm.order = m_SelectionGroups.Count;
            m_Context.modifierData.groups.Add(gm);

            m_SelectionGroups.Add(
                new SelectionGroup(
                    ref m_Context, 
                    gm,
                    m_CSBufferOps
                )
            );

            return m_SelectionGroups.Last();
        }

        public void Remove(uint id) {
            if (id >= m_SelectionGroups.Count) {
                Debug.LogWarning($"Invalid Id: {id}. No modifier at this position.");
                return;
            }

            m_SelectionGroups.RemoveAt((int)id);
            m_Context.modifierData.groups.RemoveAt((int)id);

            // Update order values
            for (int i = 0; i < m_Context.modifierData.groups.Count; i++) {
                m_SelectionGroups[i].SetOrder(i);
            }
        }

        public void Reorder(uint fromId, uint toId) {
            if (fromId >= m_SelectionGroups.Count || toId >= m_SelectionGroups.Count) {
                Debug.LogWarning($"Invalid Id(s). FromId: {fromId}, ToId: {toId}. Out of range.");
                return;
            }

            SelectionGroup sg = m_SelectionGroups[(int)fromId];
            m_SelectionGroups.RemoveAt((int)fromId);

            m_SelectionGroups.Insert((int)toId, sg);

            for (int i = 0; i < m_Context.modifierData.groups.Count; i++) {
                m_SelectionGroups[i].SetOrder(i);
            }
        }

        public void Run() {

            if (m_Context.scaffoldMesh == null || 
            m_Context.scaffoldBaseVertex == null || 
            m_Context.scaffoldModVertex == null || 
            m_ModifierBuffer == null ||
            m_CSBufferOps == null)
                return;
                
            ModifierUtils.ResetBuffers(m_CSBufferOps, m_Context.scaffoldModVertex, m_ModifierBuffer);
            
            // Only process enabled selection groups
            for (int i = 0; i < m_SelectionGroups.Count; i++)
            {
                if (m_SelectionGroups[i].IsEnabled())
                {
                    m_SelectionGroups[i].Run(ref m_ModifierBuffer);
                }
            }
            
            ModifierUtils.ApplyModifiedBuffer(m_CSBufferOps, m_Context.scaffoldModVertex, m_ModifierBuffer);
            SyncContext();
        }

        private void SyncContext() {
            Vector3[] tempBuffer = new Vector3[m_Context.scaffoldModVertex.count];
            m_Context.scaffoldModVertex.GetData(tempBuffer);
            m_Context.scaffoldMesh.vertices = tempBuffer;
            m_Context.scaffoldMesh.RecalculateBounds();
        }

        public void BakeSnapshot() {
            // TODO
        }

        public void EnableAllGroups() {
            for (int i = 0; i < m_SelectionGroups.Count; i++) {
                m_SelectionGroups[i].SetEnabled(true);
            }
        }

        public void DisableAllGroups() {
            for (int i = 0; i < m_SelectionGroups.Count; i++) {
                m_SelectionGroups[i].SetEnabled(false);
            }
        }
    }
}
