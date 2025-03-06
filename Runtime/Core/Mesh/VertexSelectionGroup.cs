using System;
using UnityEngine;

namespace UnityEngine.GsplEdit
{
    public class VertexSelectionGroup
    {
        private VertexPos[] m_SplatVertices;
        public uint[] m_SelectedBits;
        public uint m_SelectedCount = 0;
        public Vector3 m_CenterPos;
        public ComputeBuffer m_SelectedVerticesBuffer;

        public VertexSelectionGroup(ref VertexPos[] vertices)
        {
            m_SplatVertices = vertices;
            int selectionBufferSize = (vertices.Length + 31) / 32;
            m_SelectedBits = new uint[selectionBufferSize];
            m_CenterPos = Vector3.zero;
            if (selectionBufferSize > 0)
            {
                m_SelectedVerticesBuffer = new ComputeBuffer(selectionBufferSize, sizeof(uint));
                m_SelectedVerticesBuffer.SetData(new uint[selectionBufferSize]);
            }

        }

        public VertexSelectionGroup(ref VertexPos[] vertices, uint[] selectedBits, uint count, Vector3 centerPos, ComputeBuffer selectedBuffer)
        {
            m_SplatVertices = vertices;
            m_SelectedBits = selectedBits;
            m_SelectedCount = count;
            m_CenterPos = centerPos;
            int selectionBufferSize = (vertices.Length + 31) / 32;
            if (selectionBufferSize > 0)
            {
                m_SelectedVerticesBuffer = new ComputeBuffer(selectionBufferSize, sizeof(uint));
                CopyBufferData(m_SelectedVerticesBuffer, selectedBuffer, selectionBufferSize);
                            }
        }

        private void CopyBufferData(ComputeBuffer destination, ComputeBuffer source, int elementCount)
{
    if (source == null || destination == null)
    {
        Debug.LogError("Source or destination buffer is null.");
        return;
    }

    if (source.count < elementCount || destination.count < elementCount)
    {
        Debug.LogError("Source or destination buffer is too small for the copy operation.");
        return;
    }

    // Temporary array to hold the data
    uint[] bufferData = new uint[elementCount];

    // Copy data from the source ComputeBuffer to the array
    source.GetData(bufferData);

    // Copy data from the array to the destination ComputeBuffer
    destination.SetData(bufferData);
}


        public VertexSelectionGroup Clone()
        {
            return new VertexSelectionGroup(ref m_SplatVertices, m_SelectedBits, m_SelectedCount, m_CenterPos, m_SelectedVerticesBuffer);
        }

        public uint[] GetBits()
        {
            return m_SelectedBits;
        }

        public uint Count()
        {
            return m_SelectedCount;
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