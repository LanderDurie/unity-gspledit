using System;
using System.Runtime.InteropServices;

namespace UnityEngine.GsplEdit
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct Edge
    {
        public uint vertexId1 { get; private set; }
        public uint vertexId2 { get; private set; }

        public Edge(uint v1, uint v2)
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
