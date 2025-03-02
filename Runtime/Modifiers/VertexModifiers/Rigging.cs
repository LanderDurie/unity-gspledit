using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Animations;

namespace UnityEngine.GsplEdit
{
    public class RiggingModifier : Modifier {

        public RiggingModifier(ref SharedComputeContext context, ref VertexSelectionGroup selectionGroup) 
        {
            m_Context = context;
            m_SelectionGroup = selectionGroup;
        }

            public override void Run() {

            }

            public override void DrawSettings() {

            }


    }
}

            