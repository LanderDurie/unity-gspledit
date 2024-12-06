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
            GUILayout.Label("Modifier Settings", EditorStyles.boldLabel);
            m.m_Shader = (Shader)EditorGUILayout.ObjectField("Shader", m.m_Shader, typeof(Shader), false);
            m.m_IsAnimation = EditorGUILayout.Toggle("Animation", m.m_IsAnimation);
            if (m.m_IsAnimation) {
                EditorGUI.indentLevel++;
                m.m_AnimationSpeed = EditorGUILayout.FloatField("Animation Speed", Mathf.Clamp(m.m_AnimationSpeed, 0.0f, 1000.0f));           

                m.m_Loop = EditorGUILayout.Toggle("Loop", m.m_Loop);
                if (m.m_Loop) {
                    EditorGUI.indentLevel++;
                    m.m_LoopDelay = EditorGUILayout.FloatField("Restart Delay (s)", Mathf.Clamp(m.m_LoopDelay, 0.0f, 10000.0f));      
                    EditorGUI.indentLevel--;     
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }
    }
}
