using System;
using System.Collections.Generic;

namespace UnityEngine.GsplEdit
{
    public class LinkGen
    {
        public enum ForwardGenType {Distance, Mahalanobis};
        public enum BackwardGenType {Distance};

        public ForwardGenType m_ForwardSelectedType;
        public BackwardGenType m_BackwardSelectedType;

        public Dictionary<ForwardGenType, LinkGenForwardBase> m_ForwardGenerators;
        public Dictionary<BackwardGenType, LinkGenBackwardBase> m_BackwardGenerators;

        public SharedComputeContext m_Context;

        public LinkGen(ref SharedComputeContext context) {
            m_Context = context;
            m_ForwardSelectedType = ForwardGenType.Distance;
            

            m_ForwardGenerators = new Dictionary<ForwardGenType, LinkGenForwardBase>
            {
                { ForwardGenType.Distance, ScriptableObject.CreateInstance<DistanceGen>() },
                { ForwardGenType.Mahalanobis, ScriptableObject.CreateInstance<MahalanobisGen>() }
            };

            m_BackwardGenerators = new Dictionary<BackwardGenType, LinkGenBackwardBase>
            {
            };
        }

        public void GenerateForward()
        {
            m_ForwardGenerators[m_ForwardSelectedType].Generate(m_Context);
        }

        public void GenerateBackward()
        {
            // m_BackwardGenerators[m_BackwardSelectedType].Generate(m_Context);
        }
    }
}
