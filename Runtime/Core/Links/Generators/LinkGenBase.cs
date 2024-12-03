namespace UnityEngine.GsplEdit
{
    public abstract class LinkGenBase : ScriptableObject
    {
        public abstract void Generate(SharedComputeContext context);
    }
}
