using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor.AssetImporters;

namespace UnityEngine.GsplEdit
{
    public class LinkGen
    {
        public enum ForwardGenType {Euclidean, Mahalanobis, Interpolate, PCASmooth, MultiPointEuclidean};
        public enum BackwardGenType {Texture, RayCastTexture};

        public ForwardGenType m_ForwardSelectedType;
        public BackwardGenType m_BackwardSelectedType;

        public Dictionary<ForwardGenType, LinkGenForwardBase> m_ForwardGenerators;
        public Dictionary<BackwardGenType, LinkGenBackwardBase> m_BackwardGenerators;

        public SharedComputeContext m_Context;

        public LinkGen(ref SharedComputeContext context) {
            m_Context = context;
            m_ForwardSelectedType = ForwardGenType.Euclidean;
            m_BackwardSelectedType = BackwardGenType.Texture;
            

            m_ForwardGenerators = new Dictionary<ForwardGenType, LinkGenForwardBase>
            {
                { ForwardGenType.Euclidean, ScriptableObject.CreateInstance<EuclideanGen>() },
                { ForwardGenType.Mahalanobis, ScriptableObject.CreateInstance<MahalanobisGen>() },
                { ForwardGenType.Interpolate, ScriptableObject.CreateInstance<InterpolateGen>() },
                { ForwardGenType.PCASmooth, ScriptableObject.CreateInstance<PCASmoothGen>() },
                { ForwardGenType.MultiPointEuclidean, ScriptableObject.CreateInstance<MultiPointEuclideanGen>() }
            };

            m_BackwardGenerators = new Dictionary<BackwardGenType, LinkGenBackwardBase>
            {
                { BackwardGenType.Texture, ScriptableObject.CreateInstance<TextureGen>() },
                { BackwardGenType.RayCastTexture, ScriptableObject.CreateInstance<TextureGenRayCast>() }
            };
        }

        public void GenerateForward()
        {
            m_ForwardGenerators[m_ForwardSelectedType].Generate(m_Context);
        }

        public void GenerateBackward()
        {
            m_BackwardGenerators[m_BackwardSelectedType].Generate(m_Context);
        }
    }
}
