using UnityEngine;
using UnityEngine.GsplEdit;

namespace UnityEditor.GsplEdit
{
    public class MeshGenEditorTab : Tab
    {
        public override void Draw(DynamicSplat gs)
        {
            GUILayout.Label("MeshGen", EditorStyles.boldLabel);
        }
    }
}
