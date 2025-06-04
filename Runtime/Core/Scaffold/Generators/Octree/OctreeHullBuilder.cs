using System.Collections.Generic;
using System.Linq;

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

        private static bool EqVec(Vector3 v1, Vector3 v2) {
            return (v1 - v2).magnitude < 0.001;
        }

        public static HashSet<Vector3> ExtractHullVertices(OctreeNode[] surfaceNodes)
        {
            HashSet<Vector3> insideCorners = new HashSet<Vector3>();

            foreach (var node in surfaceNodes)
            {
                OctreeNode p = node.m_Parent;
                List<Vector3> corners = GetCorners(node.m_Bounds);
                List<OctreeNode> neigh = node.GetAllNeighbors();
                foreach (var corner in corners)
                {
                    // bool inactiveFound = false;
                    // foreach (var child in p.m_Children)
                    // {
                    //     if (!child.m_ContainsPotentialSurface && IsPointInsideBoundsWithEpsilon(child.m_Bounds, corner))
                    //     {
                    //         inactiveFound = true;
                    //         break;
                    //     }
                    // }

                    // if (!inactiveFound)
                    // {
                        foreach (var n in neigh)
                        {
                            if (n.m_OutsideFlag && IsPointInsideBoundsWithEpsilon(n.m_Bounds, corner))
                            {
                                insideCorners.Add(corner);
                                break;
                            }
                        }
                    // }
                }
            }

            // The hull consists of corners shared between surface and non-surface nodes
            return insideCorners;
        }

        private static bool IsPointInsideBoundsWithEpsilon(Bounds bounds, Vector3 point, float epsilon = 0.001f)
        {
            return
                point.x >= bounds.min.x - epsilon && point.x <= bounds.max.x + epsilon &&
                point.y >= bounds.min.y - epsilon && point.y <= bounds.max.y + epsilon &&
                point.z >= bounds.min.z - epsilon && point.z <= bounds.max.z + epsilon;
        }


        private static List<Vector3> GetCorners(Bounds bounds)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            return new List<Vector3>
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, max.y, max.z)
            };
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
                        n.m_OutsideFlag = false;
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
                    current.m_OutsideFlag = true;
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
        }
    }
}