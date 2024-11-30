using System.Numerics;
using System.Runtime.InteropServices;

namespace UnityEngine.GsplEdit
{
    [System.Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
        public uint colorId1;
        public uint colorId2;
        public uint colorId3;
        public uint colorId4;
        public Vector3 positionMod;
        public Vector4 rotMod;
        public Vector3 scaleMod;
        public uint colorMod1;
        public uint colorMod2;
        public uint colorMod3;
        public uint colorMod4;

        public Vertex(Vector3 position, Vector3 normal, uint[] colorIds, Vector3 positionMod, Vector4 rotMod, Vector3 scaleMod, uint[] colorMods)
        {
            this.position = position;
            this.normal = normal;

            this.colorId1 = colorIds != null && colorIds.Length > 0 ? colorIds[0] : 0;
            this.colorId2 = colorIds != null && colorIds.Length > 1 ? colorIds[1] : 0;
            this.colorId3 = colorIds != null && colorIds.Length > 2 ? colorIds[2] : 0;
            this.colorId4 = colorIds != null && colorIds.Length > 3 ? colorIds[3] : 0;

            this.positionMod = positionMod;
            this.rotMod = rotMod;
            this.scaleMod = scaleMod;

            this.colorMod1 = colorMods != null && colorMods.Length > 0 ? colorMods[0] : 0;
            this.colorMod2 = colorMods != null && colorMods.Length > 1 ? colorMods[1] : 0;
            this.colorMod3 = colorMods != null && colorMods.Length > 2 ? colorMods[2] : 0;
            this.colorMod4 = colorMods != null && colorMods.Length > 3 ? colorMods[3] : 0;
        }

        public static Vertex Default()
        {
            return new Vertex(
                Vector3.zero,
                Vector3.up,
                new uint[4],
                Vector3.zero,
                new Vector4(0, 0, 0, 1),
                Vector3.zero,
                new uint[4]
            );
        }

        // Explicit size calculation (52 bytes)
        public static uint StructSize()
        {
            return 3 * 4 +   // position (Vector3)
                    3 * 4 +   // normal (Vector3)
                    4 * 4 +   // colorIds (4 uints)
                    3 * 4 +   // positionMod (Vector3)
                    4 * 4 +   // rotMod (Vector4)
                    3 * 4 +   // scaleMod (Vector3)
                    4 * 4;    // colorMods (4 uints)
        }
    }
}