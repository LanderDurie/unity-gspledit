using System.Collections.Generic;

namespace UnityEngine.GsplEdit
{
    public class ModifierSystem
    {
        public List<SelectionGroup> m_SelectionGroups;
        private EditableMesh m_Mesh;

        public ModifierSystem()
        {
            m_SelectionGroups = new List<SelectionGroup>();
        }

        public void SetMesh(ref EditableMesh mesh) {
            m_Mesh = mesh;
        }

        public void Insert()
        {
            m_SelectionGroups.Add(new SelectionGroup(m_Mesh.m_SelectionGroup));
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

        public void PrintSelectionGroups()
        {
            Debug.LogWarning("Current SelectionGroups:");
            for (int i = 0; i < m_SelectionGroups.Count; i++)
            {
                Debug.LogWarning($"SelectionGroup at Id {i}");
            }
        }

        public void RunAll()
        {

        }

        public void Run(uint id)
        {

        }
    }
}
