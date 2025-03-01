using System.Collections.Generic;

namespace UnityEngine.GsplEdit
{
    public class ModifierSystem
    {
        public List<SelectionGroup> m_SelectionGroups;
        private EditableMesh m_Mesh;
        private SharedComputeContext m_Context;

        public ModifierSystem(ref SharedComputeContext context)
        {
            m_Context = context;
            m_SelectionGroups = new List<SelectionGroup>();
        }

        public void SetMesh(ref EditableMesh mesh) {
            m_Mesh = mesh;
        }

        public void Insert()
        {
            m_SelectionGroups.Add(new SelectionGroup(ref m_Context, ref m_Mesh));
        }

        public void Remove(uint id)
        {
            if (id >= m_SelectionGroups.Count)
            {
                Debug.LogWarning($"Invalid Id: {id}. No modifier at this position.");
                return;
            }

            m_SelectionGroups.RemoveAt((int)id);
        }

        public void Reorder(uint fromId, uint toId)
        {
            if (fromId >= m_SelectionGroups.Count || toId >= m_SelectionGroups.Count)
            {
                Debug.LogWarning($"Invalid Id(s). FromId: {fromId}, ToId: {toId}. Out of range.");
                return;
            }

            SelectionGroup modifier = m_SelectionGroups[(int)fromId];
            m_SelectionGroups.RemoveAt((int)fromId);

            if (toId > fromId) // Adjust for the shift caused by the removal
                toId--;

            m_SelectionGroups.Insert((int)toId, modifier);
        }

        public void RunAll(bool runStatic = true, bool runDynamic = true)
        {
            // Copy base data
            ResetBuffer();
            for (int i = 0; i < m_SelectionGroups.Count; i++) {
                if (m_SelectionGroups[i].m_Enabled) {
                    m_SelectionGroups[i].RunAll(runStatic, runDynamic);
                }
            }
        }

        public void RunGroup(int groupId, bool runStatic = true, bool runDynamic = true)
        {
            ResetBuffer();
            m_SelectionGroups[groupId].RunAll(runStatic, runDynamic);
        }

        public void RunModifier(int groupId, int modId, bool runStatic = true, bool runDynamic = true)
        {
            ResetBuffer();
            m_SelectionGroups[groupId].RunModifier(modId, runStatic, runDynamic);
        }

        private void ResetBuffer() {
            Graphics.CopyBuffer(m_Mesh.m_VertexBuffer, m_Context.gpuMeshVerts);
        }
    }
}
