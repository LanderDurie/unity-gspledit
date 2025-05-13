using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.GsplEdit
{
    public static class OctreeUtils
    {
        public static List<OctreeNode> ExtractHull(OctreeNode root)
        {
            List<OctreeNode> leafNodes = new List<OctreeNode>();
            CollectLeafNodes(root, leafNodes);

            Dictionary<OctreeNode, int> neighborCount = new Dictionary<OctreeNode, int>();

            // Determine adjacency using a HashSet for efficient lookup
            HashSet<OctreeNode> nodeSet = new HashSet<OctreeNode>(leafNodes);

            foreach (var node in leafNodes)
            {
                int count = CountNeighbors(node, nodeSet);
                neighborCount[node] = count;
            }

            // Nodes with 25 or fewer neighbors are part of the hull
            return neighborCount.Where(kvp => kvp.Value <= 25).Select(kvp => kvp.Key).ToList();
        }

        private static void CollectLeafNodes(OctreeNode node, List<OctreeNode> leafNodes)
        {
            if (node == null) return;

            if (node.m_IsLeaf)
            {
                leafNodes.Add(node);
            }
            else
            {
                foreach (var child in node.m_Children)
                {
                    CollectLeafNodes(child, leafNodes);
                }
            }
        }

        private static int CountNeighbors(OctreeNode node, HashSet<OctreeNode> nodeSet)
        {
            Debug.Log(node.GetAllNeighbors().Count);
            return node.GetAllNeighbors().Count; 
        }

        private static bool IsNeighbor(OctreeNode a, OctreeNode b)
        {
            Bounds boundsA = a.m_Bounds;
            Bounds boundsB = b.m_Bounds;

            // Check if bounds are touching (face, edge, or corner)
            return AreBoundsTouching(boundsA, boundsB);
        }

        private static bool AreBoundsTouching(Bounds a, Bounds b)
        {
            // Allow small floating-point errors
            float epsilon = 0.0001f;

            Vector3 minA = a.min, maxA = a.max;
            Vector3 minB = b.min, maxB = b.max;

            bool touchingX = Mathf.Abs(minA.x - maxB.x) < epsilon || Mathf.Abs(maxA.x - minB.x) < epsilon;
            bool touchingY = Mathf.Abs(minA.y - maxB.y) < epsilon || Mathf.Abs(maxA.y - minB.y) < epsilon;
            bool touchingZ = Mathf.Abs(minA.z - maxB.z) < epsilon || Mathf.Abs(maxA.z - minB.z) < epsilon;

            // They must at least be overlapping in the other two dimensions
            bool overlappingX = minA.x <= maxB.x && maxA.x >= minB.x;
            bool overlappingY = minA.y <= maxB.y && maxA.y >= minB.y;
            bool overlappingZ = minA.z <= maxB.z && maxA.z >= minB.z;

            return (touchingX && overlappingY && overlappingZ) ||
                   (touchingY && overlappingX && overlappingZ) ||
                   (touchingZ && overlappingX && overlappingY);
        }

        public static void GenerateCube(OctreeNode node, List<Vector3> vertices, List<int> indices) {

            Vector3 center = node.m_Bounds.center;
            Vector3 extents = node.m_Bounds.extents;
            Vector3[] cubeVertices = new Vector3[]
            {
                center + new Vector3(-extents.x, -extents.y, -extents.z), // 0
                center + new Vector3(extents.x, -extents.y, -extents.z),  // 1
                center + new Vector3(extents.x, -extents.y, extents.z),   // 2
                center + new Vector3(-extents.x, -extents.y, extents.z),  // 3
                center + new Vector3(-extents.x, extents.y, -extents.z),  // 4
                center + new Vector3(extents.x, extents.y, -extents.z),   // 5
                center + new Vector3(extents.x, extents.y, extents.z),    // 6
                center + new Vector3(-extents.x, extents.y, extents.z)    // 7
            };


            int baseIndex = vertices.Count;
            for (int a = 0; a < cubeVertices.Length; a++)
            {
                vertices.Add(cubeVertices[a]);
            }

            // Define the 12 triangles (two per face)
            int[] cubeIndices = new int[]
            {
                0, 1, 2,  2, 3, 0, // Bottom face
                4, 5, 6,  6, 7, 4, // Top face
                0, 4, 7,  7, 3, 0, // Left face
                1, 5, 6,  6, 2, 1, // Right face
                3, 2, 6,  6, 7, 3, // Front face
                0, 1, 5,  5, 4, 0  // Back face
            };

            // Add indices to the list
            for (int a = 0; a < cubeIndices.Length; a++)
            {
                indices.Add(baseIndex + cubeIndices[a]);
            }
            
            if (node.m_VertexPosition != null) {
                vertices.Add((Vector3)node.m_VertexPosition);
                indices.Add(vertices.Count-1);
                indices.Add(vertices.Count-1);
                indices.Add(vertices.Count-1);
            }
        }
    }
}
