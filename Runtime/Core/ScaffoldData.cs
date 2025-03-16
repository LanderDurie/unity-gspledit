using System;
using System.Collections.Generic;

namespace UnityEngine.GsplEdit {
    [Serializable]
    public class ScaffoldData : ScriptableObject {
        public int indexCount;
        public int vertexCount;
        public Vector3[] baseVertices;
        public Vector3[] modVertices;
        public int[] indices;
        public uint[] deletedBits;
        public ForwardLink[] forwardLinks;
        public List<SelectionGroup> groups;
        public List<Modifier> modifiers;

        public ScaffoldData() {
            indexCount = 1;
            vertexCount = 1;
            baseVertices = new Vector3[] { new Vector3(0, 0, 0) };
            modVertices = new Vector3[] { new Vector3(0, 0, 0) };
            deletedBits = new uint[] { 0 };
            indices = new int[] { 0 };
            forwardLinks = new ForwardLink[] { new ForwardLink() };
            groups = new();
            modifiers = new();
        }
    }
}
