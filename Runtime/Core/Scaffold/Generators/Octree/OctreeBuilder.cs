using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.GsplEdit {
    public static class OctreeBuilder {

        public struct OctreeSettings {
            public uint maxDepth;

            public OctreeSettings(uint maxDepth)
            {
                this.maxDepth = maxDepth;
            }
        }

        public static void BuildOctree(List<MeshUtils.SplatData> context, OctreeNode root, float maxDepth) {
            
            List<OctreeNode> currentLayer = new() {root};
            uint currentDepth = 0;
            while(currentDepth < maxDepth && currentLayer.Count > 0) {
                currentLayer = ProcessLayer(context, currentLayer);
                currentDepth++;
            }
        }

        private static List<OctreeNode> ProcessLayer(List<MeshUtils.SplatData> context, List<OctreeNode> nodes) {
            
            List<OctreeNode> newNodes = new();
            
            foreach(OctreeNode node in nodes) {
                newNodes.AddRange(SubdivideNode(context, node));
            }
            
            return newNodes;
        }

        private static bool ShouldSplitNode(OctreeNode node)
        {
            // Base stop conditions
            if (!node.m_ContainsPotentialSurface || node.m_SplatIds.Count < 1) {
                return false;
            }

            return true;
        }

        private static List<OctreeNode> SubdivideNode(List<MeshUtils.SplatData> context, OctreeNode node)
        {
            if (!node.m_ContainsPotentialSurface) return new();

            Vector3 center = node.m_Bounds.center;
            Vector3 quarterSize = node.m_Bounds.size * 0.25f;

            Vector3[] offsets = new Vector3[8]
            {
                new Vector3(-quarterSize.x, -quarterSize.y, -quarterSize.z), // 0: Bottom South West (---)
                new Vector3(quarterSize.x, -quarterSize.y, -quarterSize.z),  // 1: Bottom South East (+--) 
                new Vector3(-quarterSize.x, quarterSize.y, -quarterSize.z),  // 2: Bottom North West (-+-)
                new Vector3(quarterSize.x, quarterSize.y, -quarterSize.z),   // 3: Bottom North East (++-)
                new Vector3(-quarterSize.x, -quarterSize.y, quarterSize.z),  // 4: Top South West (--+)
                new Vector3(quarterSize.x, -quarterSize.y, quarterSize.z),   // 5: Top South East (+-+)
                new Vector3(-quarterSize.x, quarterSize.y, quarterSize.z),   // 6: Top North West (-++)
                new Vector3(quarterSize.x, quarterSize.y, quarterSize.z)     // 7: Top North East (+++)
            };
            
            Vector3 childSize = node.m_Bounds.size * 0.5f;
            
            for (int i = 0; i < 8; i++)
            {
                Vector3 childCenter = center + offsets[i];
                node.m_Children[i] = new OctreeNode(node, new Bounds(childCenter, childSize));

                foreach (var id in node.m_SplatIds)
                {
                    if (node.m_Children[i].m_Bounds.Intersects(context[id].GetBounds()))
                        node.m_Children[i].m_SplatIds.Add(id);
                }

                if (!ShouldSplitNode(node.m_Children[i])) {
                    node.m_Children[i].m_ContainsPotentialSurface = false;
                }
            }
            
            node.m_IsLeaf = false;
            node.m_SplatIds.Clear();

            return node.m_Children.ToList();
        }
    }
}
