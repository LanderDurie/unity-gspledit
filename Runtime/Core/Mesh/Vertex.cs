using System.Runtime.InteropServices;

namespace UnityEngine.GsplEdit
{
    [System.Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct VertexPos
    {
        public Vector3 position;
        public Vector3 positionMod;

        public VertexPos(Vector3 position, Vector3 positionMod)
        {
            this.position = position;
            this.positionMod = positionMod;
        }

        public static VertexPos Default()
        {
            return new VertexPos(
                Vector3.zero,
                Vector3.zero
            );
        }
    }
}