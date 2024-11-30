using UnityEngine;
using UnityEngine.GsplEdit;

namespace UnityEditor.GsplEdit
{
    public class ModifierEditorTab : Tab
    {
        public override void Draw(DynamicSplat gs)
        {
            GUILayout.Label("Modifiers", EditorStyles.boldLabel);
        }
    }
}
