using System;
using System.Runtime.InteropServices;

namespace UnityEngine.GsplEdit
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct Triangle
    {
        public int vertexId1;
        public int vertexId2;
        public int vertexId3;

        public Triangle(int v1, int v2, int v3)
        {
            vertexId1 = v1;
            vertexId2 = v2;
            vertexId3 = v3;
        }

        public override bool Equals(object obj)
        {
            if (obj is Triangle other)
            {
                return vertexId1 == other.vertexId1 && vertexId2 == other.vertexId2 && vertexId3 == other.vertexId3;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(vertexId1, vertexId2, vertexId3);
        }
    }
}
