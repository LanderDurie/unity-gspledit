using System;
using System.Runtime.InteropServices;

namespace UnityEngine.GsplEdit
{
    public class IcosaehdronGen : MeshGenBase
    {
        [System.Serializable]
        public class Settings
        {
            public float scale = 1;
            public float threshold = 0;
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct Icosahedron
        {
            public Vector3 center;
            public fixed float vertices[12 * 3];
            public fixed uint indices[60];
            public float opacity;
            public Vector3 boundMin;
            public Vector3 boundMax;

            public static int GetSize() {
                return sizeof(float) * (3 + 12*3 + 1 + 3 + 3) + sizeof(uint) * 60;
            }
        }

        public Settings m_Settings = new();
        public ComputeShader m_IcosahedronComputeShader;

        public unsafe override void Generate(SharedComputeContext context, ref Vertex[] vertexList, ref uint[] indexList) {

            int splatCount = context.splatData.splatCount;
            int itemsPerDispatch = 65535;

            ComputeBuffer IcosahedronBuffer = new(splatCount,  Icosahedron.GetSize());
            IcosahedronBuffer.SetData(new Icosahedron[splatCount]);

            m_IcosahedronComputeShader.SetFloat("_GlobalScaleFactor", m_Settings.scale);

            m_IcosahedronComputeShader.SetBuffer(0, "_SplatPos", context.gpuGSPosData);
            m_IcosahedronComputeShader.SetBuffer(0, "_SplatOther", context.gpuGSOtherData);
            m_IcosahedronComputeShader.SetBuffer(0, "_SplatSH", context.gpuGSSHData);
            m_IcosahedronComputeShader.SetBuffer(0, "_SplatChunks", context.gpuGSChunks);
            m_IcosahedronComputeShader.SetInt("_SplatChunkCount", context.gpuGSChunksValid ? context.gpuGSChunks.count : 0);
            m_IcosahedronComputeShader.SetBuffer(0, "_SplatColor", context.gpuGSSHData);
            uint format = (uint)context.splatData.posFormat | ((uint)context.splatData.scaleFormat << 8) | ((uint)context.splatData.shFormat << 16);
            m_IcosahedronComputeShader.SetInt("_SplatFormat", (int)format);
            m_IcosahedronComputeShader.SetTexture(0, "_SplatColor", context.gpuGSColorData);
            m_IcosahedronComputeShader.SetBuffer(0, "_IcosahedronBuffer", IcosahedronBuffer);
            for (int i = 0; i < Mathf.CeilToInt((float)splatCount / itemsPerDispatch); i++)
            {
                int offset = i * itemsPerDispatch;
                m_IcosahedronComputeShader.SetInt("_Offset", offset);
                int currentDispatchSize = Mathf.Min(splatCount - offset, itemsPerDispatch);
                m_IcosahedronComputeShader.Dispatch(0, currentDispatchSize, 1, 1);
            }

            // Retrieve the data from the IcosahedronBuffer
            Icosahedron[] icosahedrons = new Icosahedron[splatCount];
            IcosahedronBuffer.GetData(icosahedrons);
            int totalVertices = splatCount * 12;
            int totalIndices = splatCount * 60;

            // Resize the arrays to fit the total vertices and indices
            Array.Resize(ref vertexList, totalVertices);
            Array.Resize(ref indexList, totalIndices);

            uint vertexIndex = 0;
            uint indexIndex = 0;
            foreach (var icosahedron in icosahedrons)
            {
                // Add vertices
                for (int i = 0; i < 12; i++)
                {
                    Vector3 vertexPosition = new Vector3(icosahedron.vertices[i * 3], icosahedron.vertices[i * 3 + 1], icosahedron.vertices[i * 3 + 2]);
                    vertexList[vertexIndex] = Vertex.Default();
                    vertexList[vertexIndex].position = vertexPosition;
                    vertexIndex++;
                }

                // Add indices
                for (int j = 0; j < 60; j++)
                {
                    indexList[indexIndex] = (uint)icosahedron.indices[j];
                    indexIndex++;
                }
            }
        }
    }
}
