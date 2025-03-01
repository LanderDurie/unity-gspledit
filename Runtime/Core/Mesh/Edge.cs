using System;
using System.Runtime.InteropServices;

namespace UnityEngine.GsplEdit
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct Edge
    {
        public int vertexId1;
        public int vertexId2;

        public Edge(int v1, int v2)
        {
            if (v1 < v2)
            {
                vertexId1 = v1;
                vertexId2 = v2;
            }
            else
            {
                vertexId1 = v2;
                vertexId2 = v1;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is Edge other)
            {
                return vertexId1 == other.vertexId1 && vertexId2 == other.vertexId2;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(vertexId1, vertexId2);
        }
    }
}
