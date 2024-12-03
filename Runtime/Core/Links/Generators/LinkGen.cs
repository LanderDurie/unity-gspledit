using System;
using System.Collections.Generic;

namespace UnityEngine.GsplEdit
{
    public class LinkGen
    {
        public enum GenType {Distance, Mahalanobis};

        public GenType m_SelectedType;
        public Dictionary<GenType, LinkGenBase> m_Generators;
        public SharedComputeContext m_Context;

        public LinkGen(ref SharedComputeContext context) {
            m_Context = context;
            m_SelectedType = GenType.Distance;

            m_Generators = new Dictionary<GenType, LinkGenBase>
            {
                { GenType.Distance, ScriptableObject.CreateInstance<DistanceGen>() },
                { GenType.Mahalanobis, ScriptableObject.CreateInstance<MahalanobisGen>() }
            };
        }

        public void Generate()
        {
            m_Generators[m_SelectedType].Generate(m_Context);
        }
    }
}
