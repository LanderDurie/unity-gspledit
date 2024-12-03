using System;
using System.Runtime.InteropServices;

namespace UnityEngine.GsplEdit
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct ForwardLink
    {
        public fixed uint edgeIds[8];
        public fixed float edgeWeights[8];
        public fixed float edgeInterp[8];

        public static ForwardLink Default()
        {
            ForwardLink fl = new ForwardLink();
            for (int i = 0; i < 8; i++)
            {
                fl.SetEdgeId(i, 0);
                fl.SetEdgeWeight(i, 1.0f / 8.0f);
                fl.SetEdgeInterp(i, 0.5f);
            }
            return fl;
        }

        public void SetEdgeId(int index, uint value)
        {
            if (index < 0 || index >= 8)
                throw new IndexOutOfRangeException($"Index {index} is out of bounds for edgeIds.");

            fixed (uint* ids = edgeIds)
            {
                ids[index] = value;
            }
        }

        public void SetEdgeWeight(int index, float value)
        {
            if (index < 0 || index >= 8)
                throw new IndexOutOfRangeException($"Index {index} is out of bounds for edgeWeights.");

            fixed (float* weights = edgeWeights)
            {
                weights[index] = value;
            }
        }

        public void SetEdgeInterp(int index, float value)
        {
            if (index < 0 || index >= 8)
                throw new IndexOutOfRangeException($"Index {index} is out of bounds for edgeInterp.");

            fixed (float* interp = edgeInterp)
            {
                interp[index] = value;
            }
        }

        // Explicit size calculation (96 bytes)
        public static uint StructSize()
        {
            return 8 * sizeof(uint) +   // edgeIds (8 uints)
                   8 * sizeof(float) + // edgeWeights (8 floats)
                   8 * sizeof(float);  // edgeInterp (8 floats)
        }
    }
}
