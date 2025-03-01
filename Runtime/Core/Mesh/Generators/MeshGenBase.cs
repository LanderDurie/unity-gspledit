namespace UnityEngine.GsplEdit
{
    public abstract class MeshGenBase : MonoBehaviour
    {
        public abstract void Generate(SharedComputeContext context, ref Vertex[] vertexList, ref int[] indexList);
    }
}
