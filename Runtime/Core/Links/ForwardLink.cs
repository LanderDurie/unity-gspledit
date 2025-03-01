using System;
using System.Runtime.InteropServices;

namespace UnityEngine.GsplEdit
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ForwardLink
    {
        public fixed int triangleIds[8];
        public fixed float triangleWeights[8];
        public fixed float triangleX[8]; // Barycentric x coord
        public fixed float triangleY[8]; // Barycentric y coord

        public static ForwardLink Default()
        {
            ForwardLink fl = new ForwardLink();

            for (int i = 0; i < 8; i++) {
                fl.triangleIds[i] = -1;
                fl.triangleIds[i] = 0;
                fl.triangleX[i] = 0;
                fl.triangleY[i] = 0;

            }
            return fl;
        }
    }
}
