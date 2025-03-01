
using System;
using System.Collections.Generic;

namespace UnityEngine.GsplEdit
{

    public class SelectionGroup
    {
        public List<Modifier> m_Modifiers;
        public String m_Name = "New Selection Group";
        public bool m_Enabled = true;
        public VertexSelectionGroup m_Selection;
        private SharedComputeContext m_Context;

        public SelectionGroup(ref SharedComputeContext context, ref EditableMesh mesh)
        {
            m_Context = context;
            m_Selection = mesh.m_SelectionGroup.Clone();
            m_Modifiers = new List<Modifier>();
        }

        public void Insert()
        {
            // m_Modifiers.Add(new Modifier(ref m_Context, ref m_Selection));
        }

        public void Remove(uint id)
        {
            if (id >= m_Modifiers.Count)
            {
                Debug.LogWarning($"Invalid Id: {id}. No modifier at this position.");
                return;
            }

            m_Modifiers.RemoveAt((int)id);
        }

        public void Reorder(uint fromId, uint toId)
        {
            if (fromId >= m_Modifiers.Count || toId >= m_Modifiers.Count)
            {
                Debug.LogWarning($"Invalid Id(s). FromId: {fromId}, ToId: {toId}. Out of range.");
                return;
            }

            Modifier modifier = m_Modifiers[(int)fromId];
            m_Modifiers.RemoveAt((int)fromId);

            if (toId > fromId) // Adjust for the shift caused by the removal
                toId--;

            m_Modifiers.Insert((int)toId, modifier);
        }

        public void RunAll(bool runStatic = true, bool runDynamic = true)
        {
            for (int i = 0; i < m_Modifiers.Count; i++)
            {
                // if (m_Modifiers[i].m_Enabled &&
                // (m_Modifiers[i].m_IsAnimation && runDynamic || m_Modifiers[i].m_IsAnimation && runStatic))
                // {
                    // m_Modifiers[i].Run();
                // }
            }
        }

        public void RunModifier(int modId, bool runStatic = true, bool runDynamic = true)
        {
            // if (m_Modifiers[modId].m_IsAnimation && runDynamic || m_Modifiers[modId].m_IsAnimation && runStatic)
            // {
                // m_Modifiers[modId].Run();
            // }
        }
    }
}
