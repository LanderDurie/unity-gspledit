using System;
using System.Runtime.InteropServices;

namespace UnityEngine.GsplEdit
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Edge
    {
        public int vertex1 { get; private set; }
        public int vertex2 { get; private set; }

        public Edge(int v1, int v2)
        {
            if (v1 < v2)
            {
                vertex1 = v1;
                vertex2 = v2;
            }
            else
            {
                vertex1 = v2;
                vertex2 = v1;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is Edge other)
            {
                return vertex1 == other.vertex1 && vertex2 == other.vertex2;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(vertex1, vertex2);
        }

        public static uint Size()
        {
            return 8; // 4 bytes per vertex
        }
    }
}
