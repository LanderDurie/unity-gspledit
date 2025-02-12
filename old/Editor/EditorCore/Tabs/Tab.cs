using UnityEngine.GsplEdit;

namespace UnityEditor.GsplEdit
{
    public abstract class Tab : Editor
    {
        public abstract void Init(DynamicSplat gs);

        public abstract void Draw(DynamicSplat gs);

        public static T Create<T>() where T : Tab, new()
        {
            T instance = CreateInstance<T>();
            return instance;
        }
    }
}
