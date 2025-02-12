using UnityEngine;
using UnityEngine.GsplEdit;

namespace UnityEditor.GsplEdit
{
    public class MenuItems
    {
        const int k_Priority = 9;

        [MenuItem("GameObject/GsplEdit/DynamicSplat", false, k_Priority)]
        static void CreateDynamicSplat()
        {
            GameObject obj = new("Splat");
            obj.AddComponent<DynamicSplat>();
            Undo.RegisterCreatedObjectUndo(obj.gameObject, $"Create dynamic splat");
            Selection.activeGameObject = obj;
        }
    }
}