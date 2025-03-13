namespace UnityEngine.GsplEdit
{
    public abstract class MeshGenBase : MonoBehaviour
    {
        public abstract void Generate(SharedComputeContext context, ref Vector3[] vertexList, ref int[] indexList);
    }
}
