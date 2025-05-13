using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.GsplEdit {
    public class OctreeNode {
        public Bounds m_Bounds;
        public bool m_IsLeaf;
        public bool m_ContainsPotentialSurface;
        public bool m_ContainsSurface;
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


        public List<OctreeNode> GetNeighbors(int direction) {
            OctreeNode neighbor = GetNeighborOfGreaterOrEqualSize(direction);
            List<OctreeNode> neighbors = FindNeighborsOfSmallerSize(neighbor, direction);
            return neighbors;
        }

        private OctreeNode GetNeighborOfGreaterOrEqualSize(int direction) {
            if (m_Parent == null) // Reached root?
                return null;

            // Create direction vectors for each of the 6 primary directions
            Vector3[] directions = new Vector3[] {
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
            if (m_Parent.m_Bounds.Contains(pointOutside)) {
                // Point is inside parent, so we need a sibling
                for (int i = 0; i < 8; i++) {
                    if (m_Parent.m_Children[i] != this && 
                        m_Parent.m_Children[i] != null && 
                        m_Parent.m_Children[i].m_Bounds.Contains(pointOutside)) {
                        return m_Parent.m_Children[i];
                    }
                }
            } else {
                // Point is outside parent, need to go up further
                OctreeNode parentNeighbor = m_Parent.GetNeighborOfGreaterOrEqualSize(direction);
                if (parentNeighbor == null || parentNeighbor.m_IsLeaf)
                    return parentNeighbor;
                    
                // Find the child of parentNeighbor that contains our point
                for (int i = 0; i < 8; i++) {
                    if (parentNeighbor.m_Children[i] != null && 
                        parentNeighbor.m_Children[i].m_Bounds.Contains(faceCenter)) {
                        return parentNeighbor.m_Children[i];
                    }
                }
                
                // Try a slightly different approach - find the nearest child
                OctreeNode closestChild = null;
                float minDistance = float.MaxValue;
                
                for (int i = 0; i < 8; i++) {
                    if (parentNeighbor.m_Children[i] != null) {
                        float distance = Vector3.Distance(
                            parentNeighbor.m_Children[i].m_Bounds.center, 
                            faceCenter);
                            
                        if (distance < minDistance) {
                            minDistance = distance;
                            closestChild = parentNeighbor.m_Children[i];
                        }
                    }
                }
                
                return closestChild;
            }
            
            return null;
        }

        private List<OctreeNode> FindNeighborsOfSmallerSize(OctreeNode neighbor, int direction) {
            var neighbors = new List<OctreeNode>();
            if (neighbor == null)
                return neighbors;
                
            if (neighbor.m_IsLeaf) {
                neighbors.Add(neighbor);
                return neighbors;
            }
        
            // BFS to find all relevant leaf nodes
            Queue<OctreeNode> nodesToCheck = new Queue<OctreeNode>();
            nodesToCheck.Enqueue(neighbor);
            
            while (nodesToCheck.Count > 0) {
                OctreeNode current = nodesToCheck.Dequeue();
                
                if (current.m_IsLeaf) {
                    if (AreBoundsTouching(current.m_Bounds)) {
                        neighbors.Add(current);
                    }
                }  else {
                    for (int i = 0; i < 8; i++) {
                        if (current.m_Children[i] != null) {
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

        public List<OctreeNode> GetFaceNeighbours() {
            var allNeighbors = new List<OctreeNode>();
            
            // Face neighbors (6 directions)
            for (int direction = 0; direction < 6; direction++) {
                allNeighbors.AddRange(GetNeighbors(direction));
            }
            return allNeighbors;
        }

        // Method to get all neighbors in all directions
        public List<OctreeNode> GetAllNeighbors() {
            var allNeighbors = GetFaceNeighbours();
            
            // Edge neighbors (12 directions)
            for (int i = 0; i < 4; i++) {
                // X-Y plane edges
                allNeighbors.AddRange(GetEdgeNeighbors(i % 2 == 0 ? 0 : 1, i < 2 ? 2 : 3));
                
                // X-Z plane edges
                allNeighbors.AddRange(GetEdgeNeighbors(i % 2 == 0 ? 0 : 1, i < 2 ? 4 : 5));
                
                // Y-Z plane edges
                allNeighbors.AddRange(GetEdgeNeighbors(i % 2 == 0 ? 2 : 3, i < 2 ? 4 : 5));
            }
            
            // Corner neighbors (8 directions)
            for (int x = 0; x <= 1; x++) {
                for (int y = 0; y <= 1; y++) {
                    for (int z = 0; z <= 1; z++) {
                        allNeighbors.AddRange(GetCornerNeighbors(x == 0 ? 1 : 0, y == 0 ? 3 : 2, z == 0 ? 5 : 4));
                    }
                }
            }
            // Remove duplicates
            return allNeighbors.Distinct().Where(neighbor => AreBoundsTouching(neighbor.m_Bounds)).ToList();
        }

        public List<OctreeNode> GetEdgeNeighbors(int direction1, int direction2) {
            var edgeNeighbors = new List<OctreeNode>();
            
            var neighbors1 = GetNeighbors(direction1);
            
            foreach (var neighbor in neighbors1) {
                var secondaryNeighbors = neighbor.GetNeighbors(direction2);
                edgeNeighbors.AddRange(secondaryNeighbors);
            }
            
            var neighbors2 = GetNeighbors(direction2);
            
            foreach (var neighbor in neighbors2) {
                var secondaryNeighbors = neighbor.GetNeighbors(direction1);
                edgeNeighbors.AddRange(secondaryNeighbors);
            }
            
            return edgeNeighbors;
        }

        public List<OctreeNode> GetCornerNeighbors(int direction1, int direction2, int direction3) {
            var cornerNeighbors = new List<OctreeNode>();
            
            var edgeNeighbors = GetEdgeNeighbors(direction1, direction2);
            
            // For each edge neighbor, get neighbors in the third direction
            foreach (var neighbor in edgeNeighbors) {
                var tertiaryNeighbors = neighbor.GetNeighbors(direction3);
                cornerNeighbors.AddRange(tertiaryNeighbors);
            }
            
            // Direction1 + direction3, then direction2
            edgeNeighbors = GetEdgeNeighbors(direction1, direction3);
            foreach (var neighbor in edgeNeighbors) {
                var tertiaryNeighbors = neighbor.GetNeighbors(direction2);
                cornerNeighbors.AddRange(tertiaryNeighbors);
            }
            
            // Direction2 + direction3, then direction1
            edgeNeighbors = GetEdgeNeighbors(direction2, direction3);
            foreach (var neighbor in edgeNeighbors) {
                var tertiaryNeighbors = neighbor.GetNeighbors(direction1);
                cornerNeighbors.AddRange(tertiaryNeighbors);
            }
            
            return cornerNeighbors;
        }

        public bool AreBoundsTouching(Bounds bounds) {
            return m_Bounds.Intersects(bounds);
        }

        public float EvaluateSDF(Vector3 point, float threshold, List<MeshUtils.SplatData> splats)
        {
            float accumulatedOpacity = 0f;
            float minDistance = float.MaxValue;

            foreach (var splatId in m_SplatIds)
            {
                MeshUtils.SplatData splat = splats[splatId];

                // Calculate the combined inverse rotation and scale matrix
                Matrix4x4 invSplatRot_ScaleMat = Matrix4x4.TRS(Vector3.zero, Quaternion.Inverse(splat.rot), Vector3.one)
                                                * Matrix4x4.Scale(new Vector3(1.0f / splat.scale.x, 1.0f / splat.scale.y, 1.0f / splat.scale.z));

                // Apply the transformation to the offset
                Vector3 offset = point - splat.center;
                Vector3 transformedPos = invSplatRot_ScaleMat.MultiplyPoint3x4(offset);

                // Calculate squared distance
                float distanceSquared = transformedPos.sqrMagnitude;

                // Adjust opacity calculation to include the / 2.0f factor
                float opacity = splat.opacity * Mathf.Exp(-distanceSquared / 2.0f);
                accumulatedOpacity += opacity;

                float actualDistance = offset.sqrMagnitude;
                minDistance = Mathf.Min(minDistance, actualDistance);
            }
        
            return accumulatedOpacity > 0.01 ? accumulatedOpacity - threshold : -minDistance;
        }
    }

}