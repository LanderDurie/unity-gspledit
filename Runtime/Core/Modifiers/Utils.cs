namespace UnityEngine.GsplEdit {

    public static class ModifierUtils {
        enum KernelIndices{AddBuffers, ResetBuffers, VertexMask}

        public static void ResetBuffers(ComputeShader cs, GraphicsBuffer target, GraphicsBuffer modifier) {
            int kernelIndex = (int)KernelIndices.ResetBuffers;
            cs.SetBuffer(kernelIndex, "_TargetBuffer", target);
            cs.SetBuffer(kernelIndex, "_ModifierBuffer",modifier);
            int threadGroupCount = Mathf.CeilToInt((float)target.count / 64);
            cs.Dispatch(kernelIndex, threadGroupCount, 1, 1);
        }

        public static void ApplyModifiedBuffer(ComputeShader cs, GraphicsBuffer target, GraphicsBuffer modifier) {
            int kernelIndex = (int)KernelIndices.AddBuffers;
            cs.SetBuffer(kernelIndex, "_TargetBuffer", target);
            cs.SetBuffer(kernelIndex, "_ModifierBuffer", modifier);
            int threadGroupCount = Mathf.CeilToInt((float)target.count / 64);
            cs.Dispatch(kernelIndex, threadGroupCount, 1, 1);
        }

        public static void ApplyMask(ComputeShader cs, ComputeBuffer mask, GraphicsBuffer modifier) {
            int kernelIndex = (int)KernelIndices.VertexMask;
            cs.SetBuffer(kernelIndex, "_VertexMask", mask);
            cs.SetBuffer(kernelIndex, "_ModifierBuffer", modifier);
            int threadGroupCount = Mathf.CeilToInt((float)modifier.count / 64);
            cs.Dispatch(kernelIndex, threadGroupCount, 1, 1);
        }

        public static void ClearBuffer(GraphicsBuffer buffer) {
            Vector3[] zeroData = new Vector3[buffer.count];
            buffer.SetData(zeroData);
        }

    }
}
