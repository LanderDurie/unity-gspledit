using System;
using UnityEngine;

namespace UnityEngine.GsplEdit {
public class VertexSelectionGroup
{
    private Vertex[] m_SplatVertices;
    private uint[] m_SelectedBits;
    public uint m_SelectedCount = 0;
    public Vector3 m_CenterPos;

    public VertexSelectionGroup(Vertex[] vertices, EditableMesh editableMeshHandle, ComputeShader editableMeshComputeShader)
    {
        m_SplatVertices = vertices;
        int selectionBufferSize = (vertices.Length + 31) / 32;
        m_SelectedBits = new uint[selectionBufferSize];
        m_CenterPos = Vector3.zero;
    }

    public bool IsSelected(int id)
    {
        int bitIndex = id / 32;
        int bitOffset = id % 32;
        return (m_SelectedBits[bitIndex] & (1u << bitOffset)) != 0;
    }

    public void SetBit(int id, bool value)
    {
        int bitIndex = id / 32;
        int bitOffset = id % 32;

        if (value)
        {
            m_SelectedBits[bitIndex] |= (1u << bitOffset);
        }
        else
        {
            m_SelectedBits[bitIndex] &= ~(1u << bitOffset);
        }
    }

    public void AddVertex(int id)
    {
        if (!IsSelected(id)) // Avoid duplicate additions
        {
            m_SelectedCount++;
            SetBit(id, true);
            // Incrementally update center position
            m_CenterPos = ((m_CenterPos * (m_SelectedCount - 1)) + m_SplatVertices[id].position) / m_SelectedCount;

            // Update the opacity selection and color selection
            // m_OpacitySelection.AddVertex(id);
        }
    }

    public void RemoveVertex(int id)
    {
        if (IsSelected(id) && m_SelectedCount > 0) // Avoid removing unselected or decrementing below zero
        {
            m_SelectedCount--;
            SetBit(id, false);

            // Adjust center position by recalculating only if there are selected vertices left
            if (m_SelectedCount > 0)
            {
                m_CenterPos = ((m_CenterPos * (m_SelectedCount + 1)) - m_SplatVertices[id].position) / m_SelectedCount;
            }
            else
            {
                m_CenterPos = Vector3.zero;
            }

            // Update the opacity selection and color selection
            // m_OpacitySelection.RemoveVertex(id);
            // m_ColorSelection.RemoveVertex(id);
        }
    }
}
}