using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.GsplEdit
{
    public class MeshGen
    {
        public enum GenType {Icosahedron, MarchingCubes, SurfaceNets, DualContouringGen};

        public GenType m_SelectedType;
        public Dictionary<GenType, MeshGenBase> m_Generators;
        public SharedComputeContext m_Context;

        public MeshGen(ref SharedComputeContext context) {
            m_Context = context;
            m_SelectedType = GenType.MarchingCubes;

            GameObject generatorHolder = new GameObject("MeshGenerators");
            generatorHolder.hideFlags = HideFlags.HideAndDontSave; // Hide in hierarchy and don't save

            m_Generators = new Dictionary<GenType, MeshGenBase>
            {
                { GenType.Icosahedron, generatorHolder.AddComponent<IcosaehdronGen>() },
                { GenType.MarchingCubes, generatorHolder.AddComponent<MarchingCubesGen>() },
                { GenType.SurfaceNets, generatorHolder.AddComponent<SurfaceNetsGen>() },
                { GenType.DualContouringGen, generatorHolder.AddComponent<DualContouringGen>() }
            };
        }

        ~MeshGen() {
            if (m_Generators != null && m_Generators.Count > 0) {
                var firstGen = m_Generators.Values.First();
                if (firstGen != null) {
                    GameObject.DestroyImmediate(firstGen.gameObject);
                }
            }
        }

        public EditableMesh Generate(ref ModifierSystem modSystem)
        {

            VertexPos[] vertexList = new VertexPos[0];
            int[] indexList = new int[0];
            Edge[] edgeList = new Edge[0];
            Triangle[] triangleList = new Triangle[0];

            m_Generators[m_SelectedType].Generate(m_Context, ref vertexList, ref indexList);

            MeshGenUtils.OptimizeMesh(ref vertexList, ref indexList);

            MeshGenUtils.ExtractUniqueEdges(indexList, ref edgeList);
            MeshGenUtils.ExtractUniqueTriangles(indexList, ref triangleList);

            Debug.Log($"Mesh Generated: {vertexList.Length} vertices, {indexList.Length} indices.");

            SetNormals(vertexList, indexList);
            SetUV(vertexList, indexList);

            EditableMesh m = ScriptableObject.CreateInstance<EditableMesh>();
            m.Initialize(ref m_Context, ref modSystem, vertexList, indexList, edgeList, triangleList);

            return m;
        }

        private void SetNormals(VertexPos[] vertexList, int[] indexList)
        {
            // Initialize all vertex normals to zero
            for (int i = 0; i < vertexList.Length; i++) 
            {
                vertexList[i].normal = Vector3.zero;
            }
            
            // Calculate the normal for each triangle and add it to each vertex
            for (int i = 0; i < indexList.Length; i += 3) 
            {
                int idx1 = indexList[i];
                int idx2 = indexList[i + 1];
                int idx3 = indexList[i + 2];
                
                // Get the three vertices of this triangle
                Vector3 v1 = vertexList[idx1].position;
                Vector3 v2 = vertexList[idx2].position;
                Vector3 v3 = vertexList[idx3].position;
                
                // Calculate the triangle's normal using the cross product
                Vector3 edge1 = v2 - v1;
                Vector3 edge2 = v3 - v1;
                Vector3 triangleNormal = Vector3.Cross(edge1, edge2).normalized;
                
                // Add this normal to each vertex
                vertexList[idx1].normal += triangleNormal;
                vertexList[idx2].normal += triangleNormal;
                vertexList[idx3].normal += triangleNormal;
            }
            
            // Normalize all vertex normals
            for (int i = 0; i < vertexList.Length; i++) 
            {
                vertexList[i].normal.Normalize();
            }
        }

        private void SetUV(VertexPos[] vertexList, int[] indexList)
        {
            // Calculate bounds to normalize UVs
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            
            foreach (var vertex in vertexList)
            {
                min.x = Mathf.Min(min.x, vertex.position.x);
                min.y = Mathf.Min(min.y, vertex.position.y);
                min.z = Mathf.Min(min.z, vertex.position.z);
                
                max.x = Mathf.Max(max.x, vertex.position.x);
                max.y = Mathf.Max(max.y, vertex.position.y);
                max.z = Mathf.Max(max.z, vertex.position.z);
            }
            
            Vector3 size = max - min;
            
            // Assign UV coordinates based on XZ projection (common for terrain-like meshes)
            // You can modify this to use different projections based on your specific needs
            for (int i = 0; i < vertexList.Length; i++)
            {
                // Normalize the position to 0-1 range
                float u = (vertexList[i].position.x - min.x) / (size.x == 0 ? 1 : size.x);
                float v = (vertexList[i].position.z - min.z) / (size.z == 0 ? 1 : size.z);
                
                // Assign the UV coordinates to the vertex
                vertexList[i].uv = new Vector2(u, v);
            }
            
            Debug.Log($"UV coordinates assigned to {vertexList.Length} vertices.");
        }
    }
}
