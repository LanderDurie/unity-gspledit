using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityEngine.GsplEdit {
    public class NegativeLight : Modifier  {
        public NegativeLight(ref SharedComputeContext context, ref VertexSelectionGroup selectionGroup) 
        {
            m_Context = context;
            m_SelectionGroup = selectionGroup;
        }

        public override void Run() 
        {
        }

        public override void DrawSettings() 
        {
            GUILayout.Label("Negative Light Modifier", EditorStyles.boldLabel);
        }
    }
}
