using System.Runtime.InteropServices;

namespace UnityEngine.GsplEdit
{
    [System.Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct VertexPos
    {
        public Vector3 position;
        public Vector3 positionMod;
        public Vector3 normal;
        public Vector2 uv;

        public VertexPos(Vector3 position, Vector3 positionMod, Vector3 normal, Vector2 uv)
        {
            this.position = position;
            this.positionMod = positionMod;
            this.normal = normal;
            this.uv = uv;
        }

        public static VertexPos Default()
        {
            return new VertexPos(
                Vector3.zero,
                Vector3.zero,
                Vector3.up,
                Vector2.zero
            );
        }
    }
}