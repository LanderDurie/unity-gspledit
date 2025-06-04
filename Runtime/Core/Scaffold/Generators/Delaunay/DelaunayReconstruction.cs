// Vertex class implementing IVertex for MIConvexHull
using System.Collections.Generic;
using System.Linq;
using MIConvexHull;
using UnityEngine;

public class Vertex3 : IVertex
{
    public readonly double[] position;

    public Vertex3(Vector3 v)
    {
        position = new double[] { v.x, v.y, v.z };
    }

    // Explicit interface implementation to avoid conflict
    double[] IVertex.Position => position;

    public Vector3 ToVector3()
    {
        return new Vector3((float)position[0], (float)position[1], (float)position[2]);
    }

    // Optional for dictionary keys
    public override int GetHashCode() => position[0].GetHashCode() ^ position[1].GetHashCode() ^ position[2].GetHashCode();

    public override bool Equals(object obj)
    {
        if (obj is Vertex3 other)
            return position[0] == other.position[0] &&
                    position[1] == other.position[1] &&
                    position[2] == other.position[2];
        return false;
    }
}

public static class Delaunay3D
{
    public static Mesh CreateSurfaceMesh(Vector3[] points)
    {
        var vertices = points.Select(p => new Vertex3(p)).ToList();

        var delaunay = DelaunayTriangulation<Vertex3, DefaultTriangulationCell<Vertex3>>.Create(vertices, 1e-10);

        var unityVertices = new List<Vector3>();
        var vertexMap = new Dictionary<Vertex3, int>();

        // Dictionary to count how many times each face appears
        var faceCount = new Dictionary<string, int>();

        // We'll store faces along with info needed to fix winding later
        var faceList = new List<(int[] tri, Vertex3 excludedVertex, DefaultTriangulationCell<Vertex3> cell)>();

        foreach (var cell in delaunay.Cells)
        {
            var v = cell.Vertices;

            // Faces: each excluding one vertex
            int[][] faceIndices = new int[][]
            {
                new int[] { 0, 1, 2 }, // exclude vertex 3
                new int[] { 0, 1, 3 }, // exclude vertex 2
                new int[] { 0, 2, 3 }, // exclude vertex 1
                new int[] { 1, 2, 3 }  // exclude vertex 0
            };

            for (int faceIdx = 0; faceIdx < 4; faceIdx++)
            {
                var face = faceIndices[faceIdx];
                int[] tri = new int[3];
                for (int i = 0; i < 3; i++)
                {
                    var vert = v[face[i]];
                    if (!vertexMap.TryGetValue(vert, out int idx))
                    {
                        var pos = vert.position;
                        idx = unityVertices.Count;
                        unityVertices.Add(new Vector3((float)pos[0], (float)pos[1], (float)pos[2]));
                        vertexMap[vert] = idx;
                    }
                    tri[i] = idx;
                }

                // Sort for counting duplicates
                var key = string.Join(",", tri.OrderBy(x => x));

                if (faceCount.ContainsKey(key))
                    faceCount[key]++;
                else
                    faceCount[key] = 1;

                // Store the face and the excluded vertex (the one opposite the face in the tetrahedron)
                var excludedVertex = v[3 - faceIdx]; // or v of the excluded index

                faceList.Add((tri, excludedVertex, cell));
            }
        }

        var boundaryTriangles = new List<int>();

        foreach (var (tri, excludedVertex, cell) in faceList)
        {
            var key = string.Join(",", tri.OrderBy(x => x));
            if (faceCount[key] == 1) // Boundary face
            {
                // Fix winding:

                // Positions of the triangle vertices
                Vector3 p0 = unityVertices[tri[0]];
                Vector3 p1 = unityVertices[tri[1]];
                Vector3 p2 = unityVertices[tri[2]];
                Vector3 excludedPos = new Vector3((float)excludedVertex.position[0], (float)excludedVertex.position[1], (float)excludedVertex.position[2]);

                // Calculate face normal (using current winding)
                Vector3 normal = Vector3.Cross(p1 - p0, p2 - p0);

                // Vector from a triangle vertex to the excluded vertex
                Vector3 toExcluded = excludedPos - p0;

                // If normal points towards excluded vertex, flip winding
                if (Vector3.Dot(normal, toExcluded) > 0)
                {
                    // Flip winding: swap tri[1] and tri[2]
                    boundaryTriangles.Add(tri[0]);
                    boundaryTriangles.Add(tri[2]);
                    boundaryTriangles.Add(tri[1]);
                }
                else
                {
                    boundaryTriangles.AddRange(tri);
                }
            }
        }

        Mesh unityMesh = new Mesh();
        unityMesh.SetVertices(unityVertices);
        unityMesh.SetTriangles(boundaryTriangles, 0);
        unityMesh.RecalculateNormals();
        unityMesh.RecalculateBounds();
        return unityMesh;

        // return boundaryTriangles.ToArray();
    }
}