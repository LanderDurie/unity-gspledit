using System.Collections.Generic;

namespace UnityEngine.GsplEdit
{
    public class LinkGen
    {
        public enum ForwardGenType {Euclidean, Mahalanobis, Interpolate, PCASmooth};
        public enum BackwardGenType {Euclidean};

        public ForwardGenType m_ForwardSelectedType;
        public BackwardGenType m_BackwardSelectedType;

        public Dictionary<ForwardGenType, LinkGenForwardBase> m_ForwardGenerators;
        public Dictionary<BackwardGenType, LinkGenBackwardBase> m_BackwardGenerators;

        public SharedComputeContext m_Context;

        public LinkGen(ref SharedComputeContext context) {
            m_Context = context;
            m_ForwardSelectedType = ForwardGenType.Euclidean;
            

            m_ForwardGenerators = new Dictionary<ForwardGenType, LinkGenForwardBase>
            {
                { ForwardGenType.Euclidean, ScriptableObject.CreateInstance<EuclideanGen>() },
                { ForwardGenType.Mahalanobis, ScriptableObject.CreateInstance<MahalanobisGen>() },
                { ForwardGenType.Interpolate, ScriptableObject.CreateInstance<InterpolateGen>() },
                { ForwardGenType.PCASmooth, ScriptableObject.CreateInstance<PCASmoothGen>() }
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
