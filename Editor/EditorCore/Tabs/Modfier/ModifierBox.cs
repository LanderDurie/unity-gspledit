using UnityEditor;
using UnityEngine;
using UnityEngine.GsplEdit;

namespace UnityEditor.GsplEdit
{
    public class ModifierBox : Editor
    {
        public void Init(Modifier m) {

        }

        public void Draw(Modifier m)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            m.m_Shader = (Shader)EditorGUILayout.ObjectField("Shader", m.m_Shader, typeof(Shader), false);
            EditorGUILayout.EndVertical();
        }
    }
}
