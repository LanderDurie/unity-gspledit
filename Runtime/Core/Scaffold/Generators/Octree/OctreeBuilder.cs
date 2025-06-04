using System.Collections.Generic;
using System.Linq;
using Codice.Client.Common.TreeGrouper;

namespace UnityEngine.GsplEdit {
    public static class OctreeBuilder {

        public struct OctreeSettings {
            public uint maxDepth;

            public OctreeSettings(uint maxDepth)
            {
                this.maxDepth = maxDepth;
            }
        }

        public static void BuildOctree(List<MeshUtils.SplatData> context, OctreeNode root, float maxDepth, float threshold)
        {

            List<OctreeNode> currentLayer = new() { root };
            uint currentDepth = 0;
            while (currentDepth < maxDepth && currentLayer.Count > 0)
            {
                currentLayer = ProcessLayer(context, currentLayer, threshold);
                currentDepth++;
            }

            // Additional splat such that each leaf node can contain 8 points (avoid flat surfaces)
            // FinalLayer(context, currentLayer, threshold);
        }

        private static void FinalLayer(List<MeshUtils.SplatData> context, List<OctreeNode> nodes, float threshold)
        {
            foreach (var node in nodes)
            {
                if (node.m_ContainsPotentialSurface) continue;
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
                    foreach (int id in node.m_SplatIds)
                    {
                        if (node.m_Children[i].m_Bounds.Intersects(context[id].GetBounds()))
                            node.m_Children[i].m_SplatIds.Add(id);
                    }

                    if (!ShouldSplitNode(node.m_Children[i], context, threshold))
                    {
                        // Not valid
                        node.m_Children[i].m_ContainsPotentialSurface = false;
                    }
                    node.m_Children[i].m_SplatIds = node.m_SplatIds;
                }
                node.m_IsLeaf = false;
                node.m_FinalSplit = true;
            }
        }

        private static List<OctreeNode> ProcessLayer(List<MeshUtils.SplatData> context, List<OctreeNode> nodes, float threshold)
        {

            List<OctreeNode> newNodes = new();

            foreach (OctreeNode node in nodes)
            {
                newNodes.AddRange(SubdivideNode(context, node, threshold));
            }

            return newNodes;
        }

        private static bool ShouldSplitNode(OctreeNode node, List<MeshUtils.SplatData> context, float threshold)
        {
            // Base stop conditions
            if (!node.m_ContainsPotentialSurface) {
                return false;
            }

            if (node.m_SplatIds.Count() < 10)
            {
                return false;
            }

            float total = 0;
            foreach (var id in node.m_SplatIds)
            {
                total += context[id].opacity;
            }

            if (total < threshold)
            {
                return false;
            }

            return true;
        }

        private static List<OctreeNode> SubdivideNode(List<MeshUtils.SplatData> context, OctreeNode node, float threshold)
        {
            if (!node.m_ContainsPotentialSurface) return new();
            // if (!ShouldSplitNode(node, context, threshold))
            // {
            //     node.m_ContainsPotentialSurface = false;
            //     node.m_IsLeaf = true;
            //     return new List<OctreeNode>();
            // }


            // node.m_IsLeaf = false;
            // node.m_ContainsPotentialSurface = true; // Node contains threshold crossing, so it has potential surface
        
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

            bool validChild = false;
            for (int i = 0; i < 8; i++)
            {
                Vector3 childCenter = center + offsets[i];
                node.m_Children[i] = new OctreeNode(node, new Bounds(childCenter, childSize));

                foreach (var id in node.m_SplatIds)
                {
                    if (node.m_Children[i].m_Bounds.Intersects(context[id].GetBounds()))
                        node.m_Children[i].m_SplatIds.Add(id);
                }

                // check if the node is valid
                if (!ShouldSplitNode(node.m_Children[i], context, threshold))
                {
                    // Not valid
                    node.m_Children[i].m_ContainsPotentialSurface = false;
                }
                else
                {
                    validChild = true;
                }
            }

            if (validChild)
            {
                node.m_IsLeaf = false;
                node.m_SplatIds.Clear();
            }

            return node.m_Children.ToList();
        }
    }
}
