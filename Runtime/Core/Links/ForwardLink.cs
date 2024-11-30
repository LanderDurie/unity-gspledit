using System;
using System.Runtime.InteropServices;
namespace UnityEngine.GsplEdit
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ForwardLink
    {
        public uint edgeId1;
        public uint edgeId2;
        public uint edgeId3;
        public uint edgeId4;
        public uint edgeId5;
        public uint edgeId6;
        public uint edgeId7;
        public uint edgeId8;
        
        public float edgeWeight1;
        public float edgeWeight2;
        public float edgeWeight3;
        public float edgeWeight4;
        public float edgeWeight5;
        public float edgeWeight6;
        public float edgeWeight7;
        public float edgeWeight8;
        
        public float edgeInterp1;
        public float edgeInterp2;
        public float edgeInterp3;
        public float edgeInterp4;
        public float edgeInterp5;
        public float edgeInterp6;
        public float edgeInterp7;
        public float edgeInterp8;

        public static ForwardLink Default()
        {
            ForwardLink fl = new();
            for (int i = 0; i < 8; i++)
            {
                fl.SetEdgeId(i, 0);
                fl.SetEdgeWeight(i, 1.0f / 32.0f);
                fl.SetEdgeInterp(i, 0.5f);
            }
            return fl;
        }

        private void SetEdgeId(int index, uint value)
        {
            switch (index)
            {
                case 0: edgeId1 = value; break;
                case 1: edgeId2 = value; break;
                case 2: edgeId3 = value; break;
                case 3: edgeId4 = value; break;
                case 4: edgeId5 = value; break;
                case 5: edgeId6 = value; break;
                case 6: edgeId7 = value; break;
                case 7: edgeId8 = value; break;
            }
        }

        private void SetEdgeWeight(int index, float value)
        {
            switch (index)
            {
                case 0: edgeWeight1 = value; break;
                case 1: edgeWeight2 = value; break;
                case 2: edgeWeight3 = value; break;
                case 3: edgeWeight4 = value; break;
                case 4: edgeWeight5 = value; break;
                case 5: edgeWeight6 = value; break;
                case 6: edgeWeight7 = value; break;
                case 7: edgeWeight8 = value; break;
            }
        }

        private void SetEdgeInterp(int index, float value)
        {
            switch (index)
            {
                case 0: edgeInterp1 = value; break;
                case 1: edgeInterp2 = value; break;
                case 2: edgeInterp3 = value; break;
                case 3: edgeInterp4 = value; break;
                case 4: edgeInterp5 = value; break;
                case 5: edgeInterp6 = value; break;
                case 6: edgeInterp7 = value; break;
                case 7: edgeInterp8 = value; break;
            }
        }

        // Explicit size calculation (96 bytes)
        public static uint StructSize() {
            return 8 * 4 +   // edgeIds (8 uints)
                                      8 * 4 +   // edgeWeights (8 floats)
                                      8 * 4;    // edgeInterps (8 floats)
        }
    }
}