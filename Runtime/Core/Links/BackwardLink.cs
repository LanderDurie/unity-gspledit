using System;
using System.Runtime.InteropServices;
namespace UnityEngine.GsplEdit
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct BackwardLink
    {
        public fixed uint splatIds[32];
        public fixed float splatWeights[32];


        public static BackwardLink Default()
        {
            BackwardLink fl = new();
            for (int i = 0; i < 32; i++)
            {
                fl.splatIds[i] = 0;
                fl.splatWeights[i] = 0;
            }
            return fl;
        }
    }
}