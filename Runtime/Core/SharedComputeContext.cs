using System;

namespace UnityEngine.GsplEdit {
    [Serializable]
    public class SharedComputeContext {
        // Serializable fields
        public SplatData gsSplatData;
        public ScaffoldData scaffoldData;
        public ModifierData modifierData;
        public int gsSplatCount;
        public bool gsChunksValid;

        // Non-serialized fields
        [NonSerialized] public GraphicsBuffer gsPosData;
        [NonSerialized] public GraphicsBuffer gsOtherData;
        [NonSerialized] public GraphicsBuffer gsSHData;
        [NonSerialized] public GraphicsBuffer gsChunks;
        [NonSerialized] public Texture2D splatColorMap;
        [NonSerialized] public Texture2D splatNormalMap;
        [NonSerialized] public Texture gsColorData;
        [NonSerialized] public GraphicsBuffer scaffoldBaseVertex;
        [NonSerialized] public GraphicsBuffer scaffoldModVertex;
        [NonSerialized] public GraphicsBuffer scaffoldIndices;
        [NonSerialized] public ComputeBuffer scaffoldDeletedBits;
        [NonSerialized] public Texture2D backwardColorTex;

        [NonSerialized] public ComputeBuffer forwardLinks;
        [NonSerialized] public RenderTexture offscreenBuffer;
        [NonSerialized] public Camera offscreenCam;
        [NonSerialized] public Mesh scaffoldMesh;

        public bool AllValid() {
            return SplatDataValid() && SplatBuffersValid() && MeshValid();
        }

        public bool SplatDataValid()
        {
            return gsSplatData != null &&
                   gsSplatCount > 0 &&
                   gsSplatData.formatVersion == SplatData.kCurrentVersion &&
                   gsSplatData.posData != null &&
                   gsSplatData.otherData != null &&
                   gsSplatData.shData != null &&
                   gsSplatData.colorData != null &&
                   gsPosData != null;
        }

        public bool SplatBuffersValid()
        {
            return gsPosData != null && 
                   gsOtherData != null && 
                   gsSHData != null && 
                   gsChunks != null;
        }

        public bool MeshValid()
        {
            return scaffoldMesh != null &&
                   scaffoldBaseVertex != null &&
                   scaffoldModVertex != null &&
                   scaffoldIndices != null;
        }
    }
}