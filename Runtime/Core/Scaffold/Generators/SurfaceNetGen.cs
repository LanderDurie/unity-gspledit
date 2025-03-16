// using System;
// using System.Collections.Generic;
// using System.Runtime.InteropServices;
// using UnityEngine;

// namespace UnityEngine.GsplEdit
// {
//     public class SurfaceNetsGen : MeshGenBase
//     {
//         [System.Serializable]
//         public class Settings
//         {
//             public float threshold = 0.25f;
//             public float scale = 4f;
//             public int lod = 64;
//         }

//         [StructLayout(LayoutKind.Sequential)]
//         public unsafe struct Icosahedron
//         {
//             public Vector3 center;
//             public fixed float vertices[12 * 3];
//             public fixed uint indices[60];
//             public float opacity;
//             public Vector3 boundMin;
//             public Vector3 boundMax;

//             public static int GetSize()
//             {
//                 return sizeof(float) * (3 + 12 * 3 + 1 + 3 + 3) + sizeof(uint) * 60;
//             }
//         }

//         public Settings m_Settings = new Settings();

//         public ComputeShader m_VoxelizeIcosahedron;
//         public ComputeShader m_IcosahedronComputeShader;


//         public unsafe override void Generate(SharedComputeContext context, ref Vector3[] vertexList, ref int[] indexList)
//         {
//             int splatCount = context.gsSplatData.gsSplatCount;

//             // Calculate size and scale
//             float padding = 0.1f;
//             Vector3 size = context.gsSplatData.boundsMax - context.gsSplatData.boundsMin;
//             float maxSize = Mathf.Max(size.x, size.y, size.z);
//             float paddingValue = maxSize * padding;
//             maxSize += paddingValue * 2;

//             int x = m_Settings.lod;
//             int y = m_Settings.lod;
//             int z = m_Settings.lod;
//             uint maxGridDimension = (uint)Mathf.Max(x, y, z);
//             float voxelScale = maxSize / (maxGridDimension - 1);
//             Vector3 startPos = new Vector3(
//                 context.gsSplatData.boundsMin.x - voxelScale / 2,
//                 context.gsSplatData.boundsMin.y - voxelScale / 2,
//                 context.gsSplatData.boundsMin.z - voxelScale / 2
//             );
//             int itemsPerDispatch = 65535;

//             ComputeBuffer IcosahedronBuffer = new(splatCount, sizeof(Icosahedron));
//             IcosahedronBuffer.SetData(new Icosahedron[splatCount]);

//             m_IcosahedronComputeShader.SetFloat("_GlobalScaleFactor", m_Settings.scale);
//             m_IcosahedronComputeShader.SetBuffer(0, "_SplatPos", context.gsPosData);
//             m_IcosahedronComputeShader.SetBuffer(0, "_SplatOther", context.gsOtherData);
//             m_IcosahedronComputeShader.SetBuffer(0, "_SplatSH", context.gsSHData);
//             m_IcosahedronComputeShader.SetBuffer(0, "_SplatChunks", context.gsChunks);
//             m_IcosahedronComputeShader.SetInt("_SplatChunkCount", context.gsChunksValid ? context.gsChunks.count : 0);
//             uint format = (uint)context.gsSplatData.posFormat | ((uint)context.gsSplatData.scaleFormat << 8) | ((uint)context.gsSplatData.shFormat << 16);
//             m_IcosahedronComputeShader.SetInt("_SplatFormat", (int)format);
//             m_IcosahedronComputeShader.SetTexture(0, "_SplatColor", context.gsColorData);
//             m_IcosahedronComputeShader.SetBuffer(0, "_IcosahedronBuffer", IcosahedronBuffer);
//             for (int i = 0; i < Mathf.CeilToInt((float)splatCount / itemsPerDispatch); i++)
//             {
//                 int offset = i * itemsPerDispatch;
//                 m_IcosahedronComputeShader.SetInt("_Offset", offset);
//                 int currentDispatchSize = Mathf.Min(splatCount - offset, itemsPerDispatch);
//                 m_IcosahedronComputeShader.Dispatch(0, currentDispatchSize, 1, 1);
//             }


//             Vector3Int voxelDims = new(x, y, z);
//             int[] voxelDimsInt = { x, y, z };
//             int voxelCount = x * y * z;
//             ComputeBuffer voxelBuffer = new(voxelCount, sizeof(float));
//             float[] d = new float[voxelCount];
//             voxelBuffer.SetData(d);
//             m_VoxelizeIcosahedron.SetBuffer(0, "_SplatChunks", context.gsChunks);
//             m_VoxelizeIcosahedron.SetInt("_SplatChunkCount", context.gsChunksValid ? context.gsChunks.count : 0);
//             m_VoxelizeIcosahedron.SetBuffer(0, "_SplatPos", context.gsPosData);
//             m_VoxelizeIcosahedron.SetBuffer(0, "_SplatOther", context.gsOtherData);
//             m_VoxelizeIcosahedron.SetBuffer(0, "_SplatSH", context.gsSHData);
//             m_VoxelizeIcosahedron.SetInt("_SplatFormat", (int)format);
//             m_VoxelizeIcosahedron.SetTexture(0, "_SplatColor", context.gsColorData);
//             m_VoxelizeIcosahedron.SetBuffer(0, "_IcosahedronBuffer", IcosahedronBuffer);
//             m_VoxelizeIcosahedron.SetBuffer(0, "VoxelGrid", voxelBuffer);
//             m_VoxelizeIcosahedron.SetInt("_SplatCount", splatCount);
//             m_VoxelizeIcosahedron.SetInts("_Dims", voxelDimsInt);
//             m_VoxelizeIcosahedron.SetFloat("_Scale", voxelScale);
//             m_VoxelizeIcosahedron.SetFloats("_GridOffset", startPos.x, startPos.y, startPos.z);
//             int dispatchX = Mathf.CeilToInt((float)voxelDims.x / 4);
//             int dispatchY = Mathf.CeilToInt((float)voxelDims.y / 4);
//             int dispatchZ = Mathf.CeilToInt((float)voxelDims.z / 4);

//             m_VoxelizeIcosahedron.Dispatch(0, dispatchX, dispatchY, dispatchZ);

//             MarchingCubes.VoxelArray voxels = new MarchingCubes.VoxelArray(x, y, z);

//             // Step 1: Create a 1D array to store the data from the buffer
//             float[] voxelData = new float[voxelCount];
//             // Retrieve voxel data
//             voxelBuffer.GetData(voxelData);

//             // Surface Nets mesh generation
//             List<Vector3> vertices = new List<Vector3>();
//             List<int> indices = new List<int>();
//             GenerateSurfaceNetsMesh(voxelData, x, y, z, voxelScale, startPos, vertices, indices);



//             Array.Resize(ref vertexList, vertices.Count);
//             Array.Resize(ref indexList, indices.Count);

//             for (int i = 0; i < vertices.Count; i++)
//             {
//                 vertexList[i] = Vector3.Default();
//                 vertexList[i].position = vertices[i];
//             }

//             for (int i = 0; i < indices.Count; i++)
//             {
//                 indexList[i] = indices[i];
//             }
//             voxelBuffer.Dispose();
//         }

//         private static readonly int[] cubeEdges = {
//             0, 1,  // Bottom face edge 0
//             1, 2,  // Bottom face edge 1
//             2, 3,  // Bottom face edge 2
//             3, 0,  // Bottom face edge 3
//             4, 5,  // Top face edge 4
//             5, 6,  // Top face edge 5
//             6, 7,  // Top face edge 6
//             7, 4,  // Top face edge 7
//             0, 4,  // Vertical edge connecting v0 -> v4
//             1, 5,  // Vertical edge connecting v1 -> v5
//             2, 6,  // Vertical edge connecting v2 -> v6
//             3, 7   // Vertical edge connecting v3 -> v7
//         };




//         private void GenerateSurfaceNetsMesh(
//             float[] voxelData,
//             int width,
//             int height,
//             int depth,
//             float voxelScale,
//             Vector3 gridOffset,
//             List<Vector3> vertices,
//             List<int> indices)
//         {
//             // Buffers for processing
//             int[,,] vertexIndices = new int[width, height, depth];
//             for (int i = 0; i < width; i++)
//                 for (int j = 0; j < height; j++)
//                     for (int k = 0; k < depth; k++)
//                         vertexIndices[i, j, k] = -1;

//             // Iterators and corner grid
//             float[] grid = new float[8];
//             int[] pos = new int[3];

//             for (pos[2] = 0; pos[2] < depth - 1; pos[2]++)
//             {
//                 for (pos[1] = 0; pos[1] < height - 1; pos[1]++)
//                 {
//                     for (pos[0] = 0; pos[0] < width - 1; pos[0]++)
//                     {
//                         // Compute voxel index and sample the 8 corners
//                         int cornerMask = 0;

//                         for (int i = 0; i < 8; i++)
//                         {
//                             int cx = pos[0] + (i & 1);
//                             int cy = pos[1] + ((i >> 1) & 1);
//                             int cz = pos[2] + ((i >> 2) & 1);

//                             grid[i] = voxelData[cz * width * height + cy * width + cx];

//                             if (grid[i] > m_Settings.threshold)
//                                 cornerMask |= (1 << i);
//                         }

//                         // Skip cubes entirely inside or outside the isosurface
//                         if (cornerMask == 0 || cornerMask == 255)
//                             continue;

//                         // Compute vertex position
//                         Vector3 vertex = Vector3.zero;
//                         int edgeCount = 0;

//                         for (int i = 0; i < 12; i++)
//                         {
//                             int v0 = cubeEdges[i * 2];
//                             int v1 = cubeEdges[i * 2 + 1];

//                             if (((cornerMask >> v0) & 1) != ((cornerMask >> v1) & 1))
//                             {
//                                 float t = (m_Settings.threshold - grid[v0]) / (grid[v1] - grid[v0]);
//                                 t = Mathf.Clamp01(t);

//                                 Vector3 interpolatedVertex = Vector3.zero;
//                                 for (int j = 0; j < 3; j++)
//                                 {
//                                     int c0 = (v0 >> j) & 1;
//                                     int c1 = (v1 >> j) & 1;
//                                     interpolatedVertex[j] = Mathf.Lerp(c0, c1, t) + pos[j];
//                                 }

//                                 vertex += interpolatedVertex;
//                                 edgeCount++;
//                             }
//                         }

//                         // Average vertex position
//                         if (edgeCount > 0)
//                         {
//                             vertex /= edgeCount;
//                             vertex = gridOffset + vertex * voxelScale;

//                             // Store vertex index
//                             vertexIndices[pos[0], pos[1], pos[2]] = vertices.Count;
//                             vertices.Add(vertex);
//                         }
//                     }
//                 }
//             }

//             // Generate faces
//             for (pos[2] = 0; pos[2] < depth - 1; pos[2]++)
//             {
//                 for (pos[1] = 0; pos[1] < height - 1; pos[1]++)
//                 {
//                     for (pos[0] = 0; pos[0] < width - 1; pos[0]++)
//                     {
//                         // Check if a vertex was generated for this cell
//                         int cellVertexIndex = vertexIndices[pos[0], pos[1], pos[2]];
//                         if (cellVertexIndex == -1)
//                             continue;

//                         // Check neighboring vertices
//                         int[] neighborIndices = new int[8];
//                         bool hasNeighbors = false;

//                         for (int i = 0; i < 8; i++)
//                         {
//                             int nx = pos[0] + (i & 1);
//                             int ny = pos[1] + ((i >> 1) & 1);
//                             int nz = pos[2] + ((i >> 2) & 1);

//                             if (nx < width - 1 && ny < height - 1 && nz < depth - 1)
//                             {
//                                 neighborIndices[i] = vertexIndices[nx, ny, nz];
//                                 if (neighborIndices[i] != -1)
//                                     hasNeighbors = true;
//                             }
//                             else
//                             {
//                                 neighborIndices[i] = -1;
//                             }
//                         }

//                         if (!hasNeighbors)
//                             continue;

//                         // Generate faces for the cube
//                         int[][] faceIndices = new int[][] {
//                     new int[] {0, 1, 3, 2},  // Bottom face
//                     new int[] {4, 5, 7, 6},  // Top face
//                     new int[] {0, 4, 1, 5},  // Front face
//                     new int[] {2, 6, 3, 7},  // Back face
//                     new int[] {1, 5, 3, 7},  // Right face
//                     new int[] {0, 2, 4, 6}   // Left face
//                 };

//                         foreach (int[] face in faceIndices)
//                         {
//                             int[] faceVertices = new int[4];
//                             int validVertices = 0;

//                             for (int i = 0; i < 4; i++)
//                             {
//                                 int idx = neighborIndices[face[i]];
//                                 if (idx != -1)
//                                 {
//                                     faceVertices[validVertices++] = idx;
//                                 }
//                             }

//                             // Triangulate the face
//                             if (validVertices == 4)
//                             {
//                                 indices.Add(faceVertices[0]);
//                                 indices.Add(faceVertices[1]);
//                                 indices.Add(faceVertices[2]);

//                                 indices.Add(faceVertices[0]);
//                                 indices.Add(faceVertices[2]);
//                                 indices.Add(faceVertices[3]);
//                             }
//                         }
//                     }
//                 }
//             }
//         }
//     }
// }