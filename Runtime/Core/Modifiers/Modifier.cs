
using System;

namespace UnityEngine.GsplEdit{
    
    public class Modifier {
        private Material m_ShaderMaterial;
        public Shader m_Shader;
        public String m_Name = "New Modifier";
        public bool m_IsAnimation = false;
        public float m_AnimationSpeed = 1.0f;
        public bool m_Loop = false;
        public float m_LoopDelay = 0.0f;
        public bool m_Enabled = true;
    }
}
