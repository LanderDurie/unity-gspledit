using System;
using System.Runtime.InteropServices;
namespace UnityEngine.GsplEdit
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BackwardLink
    {
        public uint splatId1;
        public uint splatId2;
        public uint splatId3;
        public uint splatId4;
        public uint splatId5;
        public uint splatId6;
        public uint splatId7;
        public uint splatId8;
        public uint splatId9;
        public uint splatId10;
        public uint splatId11;
        public uint splatId12;
        public uint splatId13;
        public uint splatId14;
        public uint splatId15;
        public uint splatId16;
        public uint splatId17;
        public uint splatId18;
        public uint splatId19;
        public uint splatId20;
        public uint splatId21;
        public uint splatId22;
        public uint splatId23;
        public uint splatId24;
        public uint splatId25;
        public uint splatId26;
        public uint splatId27;
        public uint splatId28;
        public uint splatId29;
        public uint splatId30;
        public uint splatId31;
        public uint splatId32;

        public float splatWeight1;
        public float splatWeight2;
        public float splatWeight3;
        public float splatWeight4;
        public float splatWeight5;
        public float splatWeight6;
        public float splatWeight7;
        public float splatWeight8;
        public float splatWeight9;
        public float splatWeight10;
        public float splatWeight11;
        public float splatWeight12;
        public float splatWeight13;
        public float splatWeight14;
        public float splatWeight15;
        public float splatWeight16;
        public float splatWeight17;
        public float splatWeight18;
        public float splatWeight19;
        public float splatWeight20;
        public float splatWeight21;
        public float splatWeight22;
        public float splatWeight23;
        public float splatWeight24;
        public float splatWeight25;
        public float splatWeight26;
        public float splatWeight27;
        public float splatWeight28;
        public float splatWeight29;
        public float splatWeight30;
        public float splatWeight31;
        public float splatWeight32;

        public static BackwardLink Default()
        {
            BackwardLink fl = new();
            for (int i = 0; i < 32; i++)
            {
                fl.SetSplatId(i, 0);
                fl.SetSplatWeight(i, 1.0f / 32.0f);
            }
            return fl;
        }

        private void SetSplatId(int index, uint value)
        {
            switch (index)
            {
                case 0: splatId1 = value; break;
                case 1: splatId2 = value; break;
                case 2: splatId3 = value; break;
                case 3: splatId4 = value; break;
                case 4: splatId5 = value; break;
                case 5: splatId6 = value; break;
                case 6: splatId7 = value; break;
                case 7: splatId8 = value; break;
                case 8: splatId9 = value; break;
                case 9: splatId10 = value; break;
                case 10: splatId11 = value; break;
                case 11: splatId12 = value; break;
                case 12: splatId13 = value; break;
                case 13: splatId14 = value; break;
                case 14: splatId15 = value; break;
                case 15: splatId16 = value; break;
                case 16: splatId17 = value; break;
                case 17: splatId18 = value; break;
                case 18: splatId19 = value; break;
                case 19: splatId20 = value; break;
                case 20: splatId21 = value; break;
                case 21: splatId22 = value; break;
                case 22: splatId23 = value; break;
                case 23: splatId24 = value; break;
                case 24: splatId25 = value; break;
                case 25: splatId26 = value; break;
                case 26: splatId27 = value; break;
                case 27: splatId28 = value; break;
                case 28: splatId29 = value; break;
                case 29: splatId30 = value; break;
                case 30: splatId31 = value; break;
                case 31: splatId32 = value; break;
            }
        }

        private void SetSplatWeight(int index, float value)
        {
            switch (index)
            {
                case 0: splatWeight1 = value; break;
                case 1: splatWeight2 = value; break;
                case 2: splatWeight3 = value; break;
                case 3: splatWeight4 = value; break;
                case 4: splatWeight5 = value; break;
                case 5: splatWeight6 = value; break;
                case 6: splatWeight7 = value; break;
                case 7: splatWeight8 = value; break;
                case 8: splatWeight9 = value; break;
                case 9: splatWeight10 = value; break;
                case 10: splatWeight11 = value; break;
                case 11: splatWeight12 = value; break;
                case 12: splatWeight13 = value; break;
                case 13: splatWeight14 = value; break;
                case 14: splatWeight15 = value; break;
                case 15: splatWeight16 = value; break;
                case 16: splatWeight17 = value; break;
                case 17: splatWeight18 = value; break;
                case 18: splatWeight19 = value; break;
                case 19: splatWeight20 = value; break;
                case 20: splatWeight21 = value; break;
                case 21: splatWeight22 = value; break;
                case 22: splatWeight23 = value; break;
                case 23: splatWeight24 = value; break;
                case 24: splatWeight25 = value; break;
                case 25: splatWeight26 = value; break;
                case 26: splatWeight27 = value; break;
                case 27: splatWeight28 = value; break;
                case 28: splatWeight29 = value; break;
                case 29: splatWeight30 = value; break;
                case 30: splatWeight31 = value; break;
                case 31: splatWeight32 = value; break;
            }
        }

        // Explicit size calculation (256 bytes)
        public static uint StructSize() {
            return 32 * 4 +   // splatIds (32 uints)
                                      32 * 4;    // splatWeights (32 floats)
        }
    }
}