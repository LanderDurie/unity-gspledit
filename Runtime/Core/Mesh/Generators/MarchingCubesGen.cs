using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnityEngine.GsplEdit
{
    public class MarchingCubesGen : MeshGenBase
    {
        [System.Serializable]
        public class Settings
        {
            public float threshold = 0f;
            public float scale = 4f;
            public float cutoff = .999f;
            public int lod = 64;
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct Icosahedron
        {
            public Vector3 center;
            public fixed float vertices[12 * 3];
            public fixed int indices[60];
            public float opacity;
            public Vector3 boundMin;
            public Vector3 boundMax;

            public static int GetSize() {
                return sizeof(float) * (3 + 12*3 + 1 + 3 + 3) + sizeof(int) * 60;
            }
        }

        public Settings m_Settings = new Settings();

        public ComputeShader m_IcosahedronComputeShader;
        public ComputeShader m_VoxelizeIcosahedron;


        public unsafe override void Generate(SharedComputeContext context, ref Vertex[] vertexList, ref int[] indexList) {
            int splatCount = context.splatData.splatCount;
            int itemsPerDispatch = 65535;

            ComputeBuffer IcosahedronBuffer = new(splatCount, sizeof(Icosahedron));
            IcosahedronBuffer.SetData(new Icosahedron[splatCount]);

            m_IcosahedronComputeShader.SetFloat("_GlobalScaleFactor", m_Settings.scale);
            m_IcosahedronComputeShader.SetBuffer(0, "_SplatPos", context.gpuGSPosData);
            m_IcosahedronComputeShader.SetBuffer(0, "_SplatOther", context.gpuGSOtherData);
            m_IcosahedronComputeShader.SetBuffer(0, "_SplatSH", context.gpuGSSHData);
            m_IcosahedronComputeShader.SetBuffer(0, "_SplatChunks", context.gpuGSChunks);
            m_IcosahedronComputeShader.SetInt("_SplatChunkCount", context.gpuGSChunksValid ? context.gpuGSChunks.count : 0);
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

            // Calculate size and scale
            float padding = 0.1f;
            Vector3 size = context.splatData.boundsMax - context.splatData.boundsMin;
            float maxSize = Mathf.Max(size.x, size.y, size.z);
            float paddingValue = maxSize * padding;
            maxSize += paddingValue * 2;

            int x = m_Settings.lod;
            int y = m_Settings.lod;
            int z = m_Settings.lod;
            uint maxGridDimension = (uint)Mathf.Max(x, y, z);
            float scale = maxSize / (maxGridDimension - 1);
            float[] startPos = { context.splatData.boundsMin.x - scale/2, context.splatData.boundsMin.y - scale/2, context.splatData.boundsMin.z - scale/2};

            Vector3Int voxelDims = new(x, y, z);
            int[] voxelDimsInt = {x, y, z};
            int voxelCount = x * y * z;
            ComputeBuffer voxelBuffer = new(voxelCount, sizeof(float));
                        float[] d = new float[voxelCount];
            voxelBuffer.SetData(d);
            m_VoxelizeIcosahedron.SetBuffer(0, "_SplatChunks", context.gpuGSChunks);
            m_VoxelizeIcosahedron.SetInt("_SplatChunkCount", context.gpuGSChunksValid ? context.gpuGSChunks.count : 0);
            m_VoxelizeIcosahedron.SetBuffer(0, "_SplatPos", context.gpuGSPosData);
            m_VoxelizeIcosahedron.SetBuffer(0, "_SplatOther", context.gpuGSOtherData);
            m_VoxelizeIcosahedron.SetBuffer(0, "_SplatSH", context.gpuGSSHData);
            m_VoxelizeIcosahedron.SetInt("_SplatFormat", (int)format);
            m_VoxelizeIcosahedron.SetTexture(0, "_SplatColor", context.gpuGSColorData);
            m_VoxelizeIcosahedron.SetBuffer(0, "_IcosahedronBuffer", IcosahedronBuffer);
            m_VoxelizeIcosahedron.SetBuffer(0, "VoxelGrid", voxelBuffer);
            m_VoxelizeIcosahedron.SetInt("_SplatCount", splatCount);
            m_VoxelizeIcosahedron.SetInts("_Dims", voxelDimsInt);
            m_VoxelizeIcosahedron.SetFloat("_Scale", scale);
            m_VoxelizeIcosahedron.SetFloats("_GridOffset", startPos);
            int dispatchX = Mathf.CeilToInt((float)voxelDims.x / 4);
            int dispatchY = Mathf.CeilToInt((float)voxelDims.y / 4);
            int dispatchZ = Mathf.CeilToInt((float)voxelDims.z / 4);

            m_VoxelizeIcosahedron.Dispatch(0, dispatchX, dispatchY, dispatchZ);

            // Isosurface reconstruction
            MarchingCubes.Marching marching = new MarchingCubes.MarchingCubes();
            marching.Surface = m_Settings.cutoff;

            MarchingCubes.VoxelArray voxels = new MarchingCubes.VoxelArray(x, y, z);

            // Step 1: Create a 1D array to store the data from the buffer
            float[] flatData = new float[voxelCount];
            voxelBuffer.GetData(flatData);

            float max = 0;
            // Step 2: Map the data into the 3D `Voxels` array
            for (int i = 0; i < x; i++)
            {
                for (int j = 0; j < y; j++)
                {
                    for (int k = 0; k < z; k++)
                    {
                        int index = i * y * z + j * z + k;
                        voxels[k, j, i] = flatData[index];
                        if (flatData[index] > max) {
                            max = flatData[index];
                        }
                    }
                }
            }

            for (int i = 0; i < x; i++)
            {
                for (int j = 0; j < y; j++)
                {
                    for (int k = 0; k < z; k++)
                    {
                        voxels[i, j, k] = 1 - (voxels[i, j, k] / max);
                    }
                }
            }

            List<Vector3> verts = new List<Vector3>();
            List<int> indices = new List<int>();
            marching.Generate(voxels.Voxels, verts, indices);


            System.Array.Resize(ref vertexList, verts.Count);
            System.Array.Resize(ref indexList, indices.Count);
            for(int i = 0; i < verts.Count; i++) {
                vertexList[i] = Vertex.Default();
                vertexList[i].position = verts[i] * scale + context.splatData.boundsMin - new Vector3(scale, scale, scale) / 2;
            }

            for(int i = 0; i < indices.Count; i++) {
                indexList[i] = indices[i];
            }

            // Cleanup
            voxelBuffer.Dispose();

        }
    }
}
