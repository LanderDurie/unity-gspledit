using System.Runtime.InteropServices;

namespace UnityEngine.GsplEdit
{
    [System.Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
        public fixed uint colorIds[4];
        public Vector3 positionMod;
        public Vector4 rotMod;
        public Vector3 scaleMod;
        public fixed uint colorMods[4];

        public Vertex(Vector3 position, Vector3 normal, uint[] colorIds, Vector3 positionMod, Vector4 rotMod, Vector3 scaleMod, uint[] colorMods)
        {
            this.position = position;
            this.normal = normal;

            for (uint i = 0; i < 4; i++) {
                this.colorIds[i] = colorIds != null && colorIds.Length > i ? colorIds[i] : 0;
            }

            this.positionMod = positionMod;
            this.rotMod = rotMod;
            this.scaleMod = scaleMod;

            for (uint i = 0; i < 4; i++) {
                this.colorMods[i] = colorMods != null && colorMods.Length > i ? colorMods[i] : 0;
            }
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
    }
}