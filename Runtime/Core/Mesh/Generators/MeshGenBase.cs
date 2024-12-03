namespace UnityEngine.GsplEdit
{
    public abstract class MeshGenBase : ScriptableObject
    {
        public abstract void Generate(SharedComputeContext context, ref Vertex[] vertexList, ref uint[] indexList);
    }
}
