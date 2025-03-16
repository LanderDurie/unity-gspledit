namespace UnityEngine.GsplEdit {
    public class VertexSelectionGroup {
        private SharedComputeContext m_Context;
        public uint[] m_SelectedBits;
        public uint m_SelectedCount = 0;
        public Vector3 m_CenterPos;
        public ComputeBuffer m_SelectedVerticesBuffer;

        public VertexSelectionGroup(ref SharedComputeContext context) {
            m_Context = context;
            int selectionBufferSize = (m_Context.scaffoldData.vertexCount + 31) / 32;
            m_SelectedBits = new uint[selectionBufferSize];
            m_CenterPos = Vector3.zero;
            if (selectionBufferSize > 0) {
                m_SelectedVerticesBuffer = new ComputeBuffer(selectionBufferSize, sizeof(uint));
                m_SelectedVerticesBuffer.SetData(new uint[selectionBufferSize]);
            }
        }

        public VertexSelectionGroup(ref SharedComputeContext context, uint[] selectedBits) {
            m_Context = context;
            m_SelectedBits = selectedBits;
            int selectionBufferSize = (m_Context.scaffoldData.vertexCount + 31) / 32;
            if (selectionBufferSize > 0) {
                m_SelectedVerticesBuffer = new ComputeBuffer(selectionBufferSize, sizeof(uint));
                m_SelectedVerticesBuffer.SetData(selectedBits);
            }
        }

        public VertexSelectionGroup(ref SharedComputeContext context, uint[] selectedBits, uint count, Vector3 centerPos, ComputeBuffer selectedBuffer) {
            m_Context = context;
            m_SelectedBits = selectedBits;
            m_SelectedCount = count;
            m_CenterPos = centerPos;
            int selectionBufferSize = (m_Context.scaffoldData.vertexCount + 31) / 32;
            if (selectionBufferSize > 0) {
                m_SelectedVerticesBuffer = new ComputeBuffer(selectionBufferSize, sizeof(uint));
                CopyBufferData(m_SelectedVerticesBuffer, selectedBuffer, selectionBufferSize);
            }
        }

        public void Destroy() {
            m_SelectedVerticesBuffer?.Dispose();
            m_SelectedVerticesBuffer = null;
        }

        private void CopyBufferData(ComputeBuffer destination, ComputeBuffer source, int elementCount) {
            if (source == null || destination == null) {
                Debug.LogError("Source or destination buffer is null.");
                return;
            }

            if (source.count < elementCount || destination.count < elementCount) {
                Debug.LogError("Source or destination buffer is too small for the copy operation.");
                return;
            }

            // Temporary array to hold the data
            uint[] bufferData = new uint[elementCount];
            source.GetData(bufferData);
            destination.SetData(bufferData);
        }


        public VertexSelectionGroup Clone() {
            return new VertexSelectionGroup(ref m_Context, m_SelectedBits, m_SelectedCount, m_CenterPos, m_SelectedVerticesBuffer);
        }

        public uint[] GetBits() {
            return m_SelectedBits;
        }

        public uint Count() {
            return m_SelectedCount;
        }

        public bool IsSelected(int id) {
            int bitIndex = id / 32;
            int bitOffset = id % 32;
            return (m_SelectedBits[bitIndex] & (1u << bitOffset)) != 0;
        }

        public void SetBit(int id, bool value) {
            int bitIndex = id / 32;
            int bitOffset = id % 32;

            if (value) {
                m_SelectedBits[bitIndex] |= (1u << bitOffset);
            } else {
                m_SelectedBits[bitIndex] &= ~(1u << bitOffset);
            }
        }

        public void AddVertex(int id) {
            if (!IsSelected(id)) {
                m_SelectedCount++;
                SetBit(id, true);

                // Update center position
                if (m_SelectedCount == 1) {
                    m_CenterPos = m_Context.scaffoldData.modVertices[id];
                } else {
                    // Incrementally update center position
                    m_CenterPos = ((m_CenterPos * (m_SelectedCount - 1)) + m_Context.scaffoldData.modVertices[id]) / m_SelectedCount;
                }
            }
        }

        public void RemoveVertex(int id) {
            if (IsSelected(id) && m_SelectedCount > 0) {
                m_SelectedCount--;
                SetBit(id, false);

                // Update center position
                if (m_SelectedCount == 0) {
                    m_CenterPos = Vector3.zero;
                } else {
                    // Incrementally update center position
                    m_CenterPos = ((m_CenterPos * (m_SelectedCount + 1)) - m_Context.scaffoldData.modVertices[id]) / m_SelectedCount;
                }
            }
        }
    }
}