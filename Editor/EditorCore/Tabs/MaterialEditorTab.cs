using UnityEngine;
using UnityEngine.GsplEdit;

namespace UnityEditor.GsplEdit
{
    public class MaterialEditorTab : Tab
    {
        public override void Draw(DynamicSplat gs)
        {
            GUILayout.Label("Material", EditorStyles.boldLabel);
        }
    }
}
