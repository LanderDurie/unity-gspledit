namespace UnityEngine.GsplEdit
{
    public abstract class LinkGenForwardBase : ScriptableObject
    {
        public abstract void Generate(SharedComputeContext context);
    }

    public abstract class LinkGenBackwardBase : ScriptableObject
    {
        public abstract void Generate(SharedComputeContext context);
    }
}
