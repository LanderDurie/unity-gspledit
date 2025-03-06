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

            EditableMesh m = ScriptableObject.CreateInstance<EditableMesh>();
            m.Initialize(ref m_Context, ref modSystem, vertexList, indexList, edgeList, triangleList);

            return m;
        }
    }
}
