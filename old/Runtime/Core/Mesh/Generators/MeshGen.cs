using System;
using System.Collections.Generic;
using Codice.CM.SEIDInfo;

namespace UnityEngine.GsplEdit
{
    public class MeshGen
    {
        public enum GenType {Icosahedron, MarchingCubes, SurfaceNets};

        public GenType m_SelectedType;
        public Dictionary<GenType, MeshGenBase> m_Generators;
        public SharedComputeContext m_Context;

        public MeshGen(ref SharedComputeContext context) {
            m_Context = context;
            m_SelectedType = GenType.MarchingCubes;

            m_Generators = new Dictionary<GenType, MeshGenBase>
            {
                { GenType.Icosahedron, ScriptableObject.CreateInstance<IcosaehdronGen>() },
                { GenType.MarchingCubes, ScriptableObject.CreateInstance<MarchingCubesGen>() },
                { GenType.SurfaceNets, ScriptableObject.CreateInstance<SurfaceNetsGen>() }
            };
        }

        public EditableMesh Generate(ref ModifierSystem modSystem)
        {

            Vertex[] vertexList = new Vertex[0];
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
