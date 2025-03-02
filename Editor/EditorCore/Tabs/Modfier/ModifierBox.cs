using UnityEditor;
using UnityEngine;
using UnityEngine.GsplEdit;

namespace UnityEditor.GsplEdit
{
    public class ModifierBox : Editor
    {
        private Vector2 scrollPosition;

        public void Init(Modifier m) 
        {
        }

        public void Draw(Modifier m)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            // Start ScrollView
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition); // Adjust height as needed
            m.DrawSettings();
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
        }
    }
}
