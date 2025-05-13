using System.Collections.Generic;

namespace UnityEngine.GsplEdit {
    public static class OctreeHullBuilder
    {
        public static void ExtractHull(OctreeNode root)
        {
            if (root == null) return;

            // Find a corner node to start the flood-fill
            OctreeNode startNode = FindCornerNode(root);
            if (startNode == null) return;

            // Flood-fill from the start node
            FloodFillSurface(startNode, root);
        }

        private static OctreeNode FindCornerNode(OctreeNode node)
        {
            // Traverse the octree to find a corner node (e.g., minX, minY, minZ)
            if (node.m_IsLeaf)
            {
                return node;
            }

            // Prefer the first child (bottom-south-west corner)
            return FindCornerNode(node.m_Children[0]);
        }

    private static void FloodFillSurface(OctreeNode startNode, OctreeNode root)
    {
        if (startNode == null) return;

        // Create a queue for flood-filling
        Queue<OctreeNode> nodesToCheck = new Queue<OctreeNode>();
        nodesToCheck.Enqueue(startNode);

        // Track processed nodes to avoid redundant checks
        HashSet<OctreeNode> processedNodes = new HashSet<OctreeNode>();
        HashSet<OctreeNode> surfaceNodes = new HashSet<OctreeNode>();

        while (nodesToCheck.Count > 0)
        {
            OctreeNode current = nodesToCheck.Dequeue();

            // Skip if already processed
            if (processedNodes.Contains(current)) continue;

            processedNodes.Add(current);

            if (current.m_ContainsPotentialSurface)
            {
                OctreeNode n = current;
                while(n != null && !n.m_ContainsSurface) {
                    n.m_ContainsSurface = true;
                    n = n.m_Parent;
                }
                surfaceNodes.Add(current);
                
                // Get flood neighbors for surface nodes
                List<OctreeNode> floodNeighbors = new List<OctreeNode>();
                List<OctreeNode> allNeighbors = current.GetAllNeighbors();
                
                foreach (var neighbor in allNeighbors)
                {
                    if (neighbor != null && neighbor.m_ContainsPotentialSurface)
                    {
                        floodNeighbors.Add(neighbor);
                        if (!processedNodes.Contains(neighbor))
                        {
                            nodesToCheck.Enqueue(neighbor);
                        }
                    }
                }
                
                // Store the flood neighbors
                current.m_FloodNeighbours = floodNeighbors;
            }
            else
            {
                // For non-surface nodes, just continue propagation
                List<OctreeNode> neighbors = current.GetAllNeighbors();
                foreach (var neighbor in neighbors)
                {
                    if (neighbor != null && !processedNodes.Contains(neighbor))
                    {
                        nodesToCheck.Enqueue(neighbor);
                    }
                }
            }
        }

        // Set non-filled nodes on edge
        FillEdges(root, root.m_Bounds, processedNodes);
    }

    private static void FillEdges(OctreeNode node, Bounds bounds, HashSet<OctreeNode> processedNodes) 
    {
        if (node.m_IsLeaf) 
        {
            if (!processedNodes.Contains(node) && TouchingBounds(node, bounds)) 
            {
                OctreeNode n = node;
                while(n != null && !n.m_ContainsSurface) {
                    n.m_ContainsSurface = true;
                    n = n.m_Parent;
                }
                
                // Initialize and populate flood neighbors for edge nodes
                node.m_FloodNeighbours = new List<OctreeNode>();
                List<OctreeNode> allNeighbors = node.GetAllNeighbors();
                
                foreach (var neighbor in allNeighbors)
                {
                    if (neighbor != null && neighbor.m_ContainsSurface)
                    {
                        node.m_FloodNeighbours.Add(neighbor);
                        
                        // Ensure bidirectional connection
                        if (neighbor.m_FloodNeighbours == null)
                            neighbor.m_FloodNeighbours = new List<OctreeNode>();
                            
                        if (!neighbor.m_FloodNeighbours.Contains(node))
                            neighbor.m_FloodNeighbours.Add(node);
                    }
                }
            }
        } 
        else 
        {
            foreach (var child in node.m_Children)
            {
                if (child != null)
                    FillEdges(child, bounds, processedNodes);
            }
        }
    }
        public static bool TouchingBounds(OctreeNode node, Bounds bounds)
        {
            if (node == null) return false;

            Bounds nodeBounds = node.m_Bounds;
            Bounds octreeBounds = bounds;

            // Check if any face of the node's bounds is touching a face of the octree's bounds
            bool touchingXMin = Mathf.Approximately(nodeBounds.min.x, octreeBounds.min.x);
            bool touchingXMax = Mathf.Approximately(nodeBounds.max.x, octreeBounds.max.x);
            bool touchingYMin = Mathf.Approximately(nodeBounds.min.y, octreeBounds.min.y);
            bool touchingYMax = Mathf.Approximately(nodeBounds.max.y, octreeBounds.max.y);
            bool touchingZMin = Mathf.Approximately(nodeBounds.min.z, octreeBounds.min.z);
            bool touchingZMax = Mathf.Approximately(nodeBounds.max.z, octreeBounds.max.z);

            // Return true if any face is touching
            return touchingXMin || touchingXMax || touchingYMin || touchingYMax || touchingZMin || touchingZMax;
        }
    }
}