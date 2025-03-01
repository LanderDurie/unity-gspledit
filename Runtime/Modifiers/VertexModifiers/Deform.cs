
using System;
using UnityEditor;

namespace UnityEngine.GsplEdit{
    
    public class DeformModifier : Modifier {
        
        public DeformModifier(ref SharedComputeContext context, ref VertexSelectionGroup selectionGroup) {
            m_Context = context;
            m_SelectionGroup = selectionGroup;
            m_Name = "Rigging";
        }
    }
}
