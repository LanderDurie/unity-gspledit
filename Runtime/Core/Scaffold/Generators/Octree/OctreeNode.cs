using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.GsplEdit {
    public class OctreeNode {
        public Bounds m_Bounds;
        public bool m_IsLeaf;
        public bool m_ContainsPotentialSurface;
        public bool m_ContainsSurface;
        public bool m_OutsideFlag = false;
        public bool m_FinalSplit = false;
        public OctreeNode[] m_Children { get; set; } = new OctreeNode[8];    
        public OctreeNode m_Parent;
        public Vector3 m_VertexPosition;
        public List<int> m_SplatIds;
        public List<OctreeNode> m_FloodNeighbours = new List<OctreeNode>();


        public OctreeNode(OctreeNode parent, Bounds bounds) {
            m_Bounds = bounds;
            m_ContainsPotentialSurface = true;
            m_ContainsSurface = false;
            m_IsLeaf = true;
            m_Parent = parent;
            m_SplatIds = new List<int>();
        }

        public void Draw(bool draw, bool onlyIntersect, bool drawSpheres, bool drawVertices, float vertexSize) {
            if (drawSpheres && m_IsLeaf && m_ContainsSurface) {
                Gizmos.color = Color.green;
                Gizmos.DrawCube(m_Bounds.center, m_Bounds.size / 1.5f);
            } 

            if (draw && onlyIntersect && m_IsLeaf && m_ContainsPotentialSurface) {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(m_Bounds.center, m_Bounds.size);
            } 

            if (draw && !onlyIntersect && m_IsLeaf) {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(m_Bounds.center, m_Bounds.size);
            }

            if (drawVertices && m_ContainsSurface) {
                if (m_VertexPosition.magnitude != 0) {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawSphere(m_VertexPosition, vertexSize);
                }
            }

            // Recursively draw child nodes if not a leaf
            if (!m_IsLeaf) {
                foreach (var child in m_Children) {
                    if (child != null) {
                        child.Draw(draw, onlyIntersect, drawSpheres, drawVertices, vertexSize);
                    }
                }
            }
        }


       public List<OctreeNode> GetNeighbors(int direction)
    {
        // Step 1: Find neighbor of greater or equal size
        OctreeNode neighbor = GetNeighborOfGreaterOrEqualSize(direction);
        
        // Step 2: Find neighbors of smaller size
        List<OctreeNode> neighbors = FindNeighborsOfSmallerSize(neighbor, direction);
        // Debug.Log($"inside count: {neighbors.Count()}");
        return neighbors;
    }

    private OctreeNode GetNeighborOfGreaterOrEqualSize(int direction)
    {
        if (m_Parent == null) // Reached root?
            return null;

        // Create direction vectors for each of the 6 primary directions
        Vector3[] directions = new Vector3[]
        {
            Vector3.right,    // +X (0)
            Vector3.left,     // -X (1)
            Vector3.up,       // +Y (2)
            Vector3.down,     // -Y (3)
            Vector3.forward,  // +Z (4)
            Vector3.back      // -Z (5)
        };

        // Get direction vector
        Vector3 directionVector = directions[direction];
        // Find the face center point of the current node in the specified direction
        Vector3 faceCenter = m_Bounds.center + Vector3.Scale(directionVector, m_Bounds.size / 2);

        // Move slightly beyond the face in the direction
        Vector3 pointOutside = faceCenter + directionVector * (0.01f * m_Bounds.size.x);
        
        // Go up to parent and check if the point is still inside the parent
        if (m_Parent.m_Bounds.Contains(pointOutside))
        {
            // Point is inside parent, so we need a sibling
            for (int i = 0; i < 8; i++)
            {
                if (m_Parent.m_Children[i] != this && 
                    m_Parent.m_Children[i] != null && 
                    m_Parent.m_Children[i].m_Bounds.Contains(pointOutside))
                {
                    return m_Parent.m_Children[i];
                }
            }
        }
        else
        {
            // Point is outside parent, need to go up further
            OctreeNode parentNeighbor = m_Parent.GetNeighborOfGreaterOrEqualSize(direction);
            if (parentNeighbor == null || parentNeighbor.m_IsLeaf)
                return parentNeighbor;
                
            // Find the child of parentNeighbor that contains our point
            for (int i = 0; i < 8; i++)
            {
                if (parentNeighbor.m_Children[i] != null && 
                    parentNeighbor.m_Children[i].m_Bounds.Contains(faceCenter))
                {
                    return parentNeighbor.m_Children[i];
                }
            }
            
            // Try a slightly different approach - find the nearest child
            OctreeNode closestChild = null;
            float minDistance = float.MaxValue;
            
            for (int i = 0; i < 8; i++)
            {
                if (parentNeighbor.m_Children[i] != null)
                {
                    float distance = Vector3.Distance(
                        parentNeighbor.m_Children[i].m_Bounds.center, 
                        faceCenter);
                        
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestChild = parentNeighbor.m_Children[i];
                    }
                }
            }
            
            return closestChild;
        }
        
        return null;
    }

    private List<OctreeNode> FindNeighborsOfSmallerSize(OctreeNode neighbor, int direction)
    {
        var neighbors = new List<OctreeNode>();
        if (neighbor == null)
            return neighbors;
            
        // If the neighbor is a leaf, just return it
        if (neighbor.m_IsLeaf)
        {
            neighbors.Add(neighbor);
            return neighbors;
        }
    
        // BFS to find all relevant leaf nodes
        Queue<OctreeNode> nodesToCheck = new Queue<OctreeNode>();
        nodesToCheck.Enqueue(neighbor);
        
        while (nodesToCheck.Count > 0)
        {
            OctreeNode current = nodesToCheck.Dequeue();
            
            if (current.m_IsLeaf)
            {
                if (AreBoundsTouching(current.m_Bounds)) // Node is in the correct direction from our face
                    {
                        neighbors.Add(current);
                    }
            }
            else
            {
                // It's not a leaf, so check if any children overlap with our face
                for (int i = 0; i < 8; i++)
                {
                    if (current.m_Children[i] != null)
                    {
                        // If child might be adjacent to our face, add it to the queue
                        if (AreBoundsTouching(current.m_Children[i].m_Bounds))
                        {
                            nodesToCheck.Enqueue(current.m_Children[i]);
                        }
                    }
                }
            }
        }
        
        return neighbors;
    }

    public List<OctreeNode> GetFaceNeighbours()
    {
        var allNeighbors = new List<OctreeNode>();
        
        // Face neighbors (6 directions)
        for (int direction = 0; direction < 6; direction++)
        {
            allNeighbors.AddRange(GetNeighbors(direction));
        }
        return allNeighbors;
    }

    // Method to get all neighbors in all directions
    // Method to get all neighbors including face, edge, and corner neighbors
    public List<OctreeNode> GetAllNeighbors()
    {
        var allNeighbors = GetFaceNeighbours();

        // Edge neighbors (12 directions)
        for (int i = 0; i < 4; i++)
        {
            // X-Y plane edges
            allNeighbors.AddRange(GetEdgeNeighbors(i % 2 == 0 ? 0 : 1, i < 2 ? 2 : 3));

            // X-Z plane edges
            allNeighbors.AddRange(GetEdgeNeighbors(i % 2 == 0 ? 0 : 1, i < 2 ? 4 : 5));

            // Y-Z plane edges
            allNeighbors.AddRange(GetEdgeNeighbors(i % 2 == 0 ? 2 : 3, i < 2 ? 4 : 5));
        }

        // Corner neighbors (8 directions)
        for (int x = 0; x <= 1; x++)
        {
            for (int y = 0; y <= 1; y++)
            {
                for (int z = 0; z <= 1; z++)
                {
                    allNeighbors.AddRange(GetCornerNeighbors(x == 0 ? 1 : 0, y == 0 ? 3 : 2, z == 0 ? 5 : 4));
                }
            }
        }
        // Remove duplicates before returning
        return allNeighbors.Distinct().Where(neighbor => AreBoundsTouching(neighbor.m_Bounds)).ToList();
    }

    // Method to find neighbors along an edge (combination of two primary directions)
    public List<OctreeNode> GetEdgeNeighbors(int direction1, int direction2)
    {
        var edgeNeighbors = new List<OctreeNode>();
        
        // Get neighbors in the first direction
        var neighbors1 = GetNeighbors(direction1);
        
        // For each neighbor in direction1, get its neighbors in direction2
        foreach (var neighbor in neighbors1)
        {
            var secondaryNeighbors = neighbor.GetNeighbors(direction2);
            edgeNeighbors.AddRange(secondaryNeighbors);
        }
        
        // Also check neighbors in direction2, then look for direction1
        var neighbors2 = GetNeighbors(direction2);
        
        foreach (var neighbor in neighbors2)
        {
            var secondaryNeighbors = neighbor.GetNeighbors(direction1);
            edgeNeighbors.AddRange(secondaryNeighbors);
        }
        
        return edgeNeighbors;
    }

    // Method to find neighbors at a corner (combination of three primary directions)
    public List<OctreeNode> GetCornerNeighbors(int direction1, int direction2, int direction3)
    {
        var cornerNeighbors = new List<OctreeNode>();
        
        // Get edge neighbors from first two directions
        var edgeNeighbors = GetEdgeNeighbors(direction1, direction2);
        
        // For each edge neighbor, get neighbors in the third direction
        foreach (var neighbor in edgeNeighbors)
        {
            var tertiaryNeighbors = neighbor.GetNeighbors(direction3);
            cornerNeighbors.AddRange(tertiaryNeighbors);
        }
        
        // Check other combinations too (direction1 + direction3, then direction2)
        edgeNeighbors = GetEdgeNeighbors(direction1, direction3);
        foreach (var neighbor in edgeNeighbors)
        {
            var tertiaryNeighbors = neighbor.GetNeighbors(direction2);
            cornerNeighbors.AddRange(tertiaryNeighbors);
        }
        
        // And the last combination (direction2 + direction3, then direction1)
        edgeNeighbors = GetEdgeNeighbors(direction2, direction3);
        foreach (var neighbor in edgeNeighbors)
        {
            var tertiaryNeighbors = neighbor.GetNeighbors(direction1);
            cornerNeighbors.AddRange(tertiaryNeighbors);
        }
        
        return cornerNeighbors;
    }

    public bool AreBoundsTouching(Bounds bounds, float margin = 1e-4f) {
        Bounds expandedA = m_Bounds;
        Bounds expandedB = bounds;

        expandedA.Expand(margin);
        expandedB.Expand(margin);

        return expandedA.Intersects(expandedB);
    }


        public float EvaluateSDF(Vector3 point, float threshold, List<MeshUtils.SplatData> splats)
        {
            float accumulatedOpacity = 0f;
            float minDistanceSq = float.MaxValue;

            for (int i = 0; i < m_SplatIds.Count; i++)
            {
                var splat = splats[m_SplatIds[i]];

                // Offset from splat center
                Vector3 offset = point - splat.center;

                // Rotate offset by inverse of splat rotation
                Vector3 rotated = Quaternion.Inverse(splat.rot) * offset;

                // Normalize by scale
                rotated.x /= splat.scale.x;
                rotated.y /= splat.scale.y;
                rotated.z /= splat.scale.z;

                // Squared distance in transformed space
                float transformedDistanceSq = rotated.sqrMagnitude;

                // Compute opacity contribution
                float opacity = splat.opacity * Mathf.Exp(-transformedDistanceSq);
                accumulatedOpacity += opacity;

                // Untransformed squared distance for fallback return
                float rawDistanceSq = offset.sqrMagnitude;
                if (rawDistanceSq < minDistanceSq)
                    minDistanceSq = rawDistanceSq;

                // Early exit if already opaque enough
                if (accumulatedOpacity > threshold + 0.001f)
                    break;
            }

            return accumulatedOpacity > 0.001f ? accumulatedOpacity - threshold : -Mathf.Sqrt(minDistanceSq);
        }

    }

}