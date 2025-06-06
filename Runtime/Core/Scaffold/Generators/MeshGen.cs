using System.Collections.Generic;
using System.Linq;
using UnityEditor;

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
            m_SelectedType = GenType.SurfaceNets;

            GameObject generatorHolder = new GameObject("MeshGenerators");
            generatorHolder.hideFlags = HideFlags.HideAndDontSave; // Hide in hierarchy and don't save

            m_Generators = new Dictionary<GenType, MeshGenBase>
            {
                { GenType.Icosahedron, generatorHolder.AddComponent<IcosaehdronGen>() },
                { GenType.MarchingCubes, generatorHolder.AddComponent<MarchingCubesGen>() },
                { GenType.DualContouringGen, generatorHolder.AddComponent<DualContouringGen>() },
                { GenType.SurfaceNets, generatorHolder.AddComponent<SurfaceNetGen>() }
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

        public void Generate()
        {
            Vector3[] vertexList = new Vector3[0];
            int[] indexList = new int[0];

            // Generate vertex and index data
            m_Generators[m_SelectedType].Generate(m_Context, ref vertexList, ref indexList);

            // Merge overlapping vertices and generate vertex normals
            Vector3[] mergedNormals = new Vector3[vertexList.Length];
            for (int i = 0; i < mergedNormals.Length; i++) {
                mergedNormals[i] = Vector3.up;
            }
            MeshGenUtils.MergeOverlappingVertices(ref vertexList, ref indexList, ref mergedNormals);

            Mesh baseMesh = new Mesh();
            baseMesh.vertices = vertexList;
            baseMesh.triangles = indexList;
            baseMesh.RecalculateNormals();
            baseMesh.RecalculateBounds();
            baseMesh = MeshGenUtils.UnwrapMesh(baseMesh);

            Debug.Log($"Mesh generated with {baseMesh.vertices.Count()} vertices and {baseMesh.triangles.Count()} indices");

            // Create and initialize the EditableMesh
            m_Context.scaffoldMesh = baseMesh;
            m_Context.scaffoldData.vertexCount = baseMesh.vertices.Length;
            m_Context.scaffoldData.baseVertices = baseMesh.vertices;
            m_Context.scaffoldData.modVertices = baseMesh.vertices;
            m_Context.scaffoldData.indices = baseMesh.triangles;
            m_Context.scaffoldData.indexCount = baseMesh.triangles.Length;
            m_Context.scaffoldData.deletedBits = new uint[(baseMesh.vertices.Length + 31) / 32];
        }
    }
}
