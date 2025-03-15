using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class OctreeMeshGen : MonoBehaviour
{
    public class OctreeNode
    {
        public Bounds Bounds;
        public OctreeNode[] Children;

        public OctreeNode(Bounds bounds)
        {
            Bounds = bounds;
            Children = null;
        }

        public bool IsLeaf => Children == null;

        public void Subdivide()
        {
            if (!IsLeaf) return;

            Vector3 center = Bounds.center;
            Vector3 size = Bounds.extents * 0.5f;

            Children = new OctreeNode[8];
            for (int i = 0; i < 8; i++)
            {
                Vector3 offset = new Vector3(
                    ((i & 1) == 0) ? -size.x : size.x,
                    ((i & 2) == 0) ? -size.y : size.y,
                    ((i & 4) == 0) ? -size.z : size.z
                );
                Children[i] = new OctreeNode(new Bounds(center + offset, size * 2));
            }
        }
    }

    public int maxDepth = 3;
    private OctreeNode root;
    private MeshFilter meshFilter;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        GenerateOctree();
        GenerateMesh();
    }

    void GenerateOctree()
    {
        Vector3 center = Vector3.zero;
        Vector3 size = Vector3.one * 10f;
        root = new OctreeNode(new Bounds(center, size));
        SubdivideNode(root, 0);
    }

    void SubdivideNode(OctreeNode node, int depth)
    {
        if (depth < maxDepth)
        {
            node.Subdivide();
            foreach (var child in node.Children)
            {
                SubdivideNode(child, depth + 1);
            }
        }
    }

    void GenerateMesh()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> indices = new List<int>();

        GenerateMeshFromNode(root, vertices, indices);

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = indices.ToArray();
        mesh.RecalculateNormals();
        
        meshFilter.mesh = mesh;
    }

    void GenerateMeshFromNode(OctreeNode node, List<Vector3> vertices, List<int> indices)
    {
        if (node.IsLeaf)
        {
            AddCubeMesh(node.Bounds, vertices, indices);
        }
        else
        {
            foreach (var child in node.Children)
            {
                GenerateMeshFromNode(child, vertices, indices);
            }
        }
    }

    void AddCubeMesh(Bounds bounds, List<Vector3> vertices, List<int> indices)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        int startIndex = vertices.Count;
        vertices.AddRange(new Vector3[]
        {
            new Vector3(min.x, min.y, min.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(max.x, max.y, max.z),
            new Vector3(min.x, max.y, max.z)
        });

        indices.AddRange(new int[]
        {
            startIndex, startIndex + 1, startIndex + 2, startIndex, startIndex + 2, startIndex + 3,
            startIndex + 1, startIndex + 5, startIndex + 6, startIndex + 1, startIndex + 6, startIndex + 2,
            startIndex + 5, startIndex + 4, startIndex + 7, startIndex + 5, startIndex + 7, startIndex + 6,
            startIndex + 4, startIndex, startIndex + 3, startIndex + 4, startIndex + 3, startIndex + 7,
            startIndex + 3, startIndex + 2, startIndex + 6, startIndex + 3, startIndex + 6, startIndex + 7,
            startIndex + 4, startIndex + 5, startIndex + 1, startIndex + 4, startIndex + 1, startIndex
        });
    }
}
