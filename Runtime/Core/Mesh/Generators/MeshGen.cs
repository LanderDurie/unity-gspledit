using System;
using System.Collections.Generic;

namespace UnityEngine.GsplEdit
{
    public class MeshGen
    {
        public enum GenType {Icosahedron, MarchingCubes};

        public GenType m_SelectedType;
        public Dictionary<GenType, MeshGenBase> m_Generators;
        public SharedComputeContext m_Context;

        public MeshGen(ref SharedComputeContext context) {
            m_Context = context;
            m_SelectedType = GenType.MarchingCubes;

            m_Generators = new Dictionary<GenType, MeshGenBase>
            {
                { GenType.Icosahedron, ScriptableObject.CreateInstance<IcosaehdronGen>() },
                { GenType.MarchingCubes, ScriptableObject.CreateInstance<MarchingCubesGen>() }
            };
        }

        public EditableMesh Generate()
        {

            Vertex[] vertexList = new Vertex[0];
            uint[] indexList = new uint[0];
            Edge[] edgeList = new Edge[0];

            m_Generators[m_SelectedType].Generate(m_Context, ref vertexList, ref indexList);

            MeshGenUtils.OptimizeMesh(ref vertexList, ref indexList);

            MeshGenUtils.ExtractUniqueEdges(indexList, ref edgeList);

            Debug.Log($"Mesh Generated: {vertexList.Length} vertices, {indexList.Length} indices.");

            EditableMesh m = ScriptableObject.CreateInstance<EditableMesh>();
            m.Initialize(ref m_Context, vertexList, indexList, edgeList);

            return m;
        }
    }
}
