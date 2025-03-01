
using System;
using UnityEditor;

namespace UnityEngine.GsplEdit{
    
    public class RiggingModifier : Modifier {

        public RiggingModifier(ref SharedComputeContext context, ref VertexSelectionGroup selectionGroup) {
            m_Context = context;
            m_SelectionGroup = selectionGroup;
            m_Name = "Rigging";
        }
    }
}
