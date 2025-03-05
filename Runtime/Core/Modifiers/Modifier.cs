using System;
using UnityEditor;

namespace UnityEngine.GsplEdit{
    
    public abstract class Modifier : MonoBehaviour {

        public enum Type {
            Static,
            Dynamic
        };

        public bool m_Enabled = true;
        protected VertexSelectionGroup m_SelectionGroup;
        protected SharedComputeContext m_Context;
        protected Type m_Type;
        public String m_Name = "New Modifier";

        public new Type GetType() {
            return m_Type;
        }

        public abstract void DrawSettings();
        public abstract void Run();
    }
}
