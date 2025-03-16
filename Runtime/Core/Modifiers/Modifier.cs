namespace UnityEngine.GsplEdit {

    public abstract class Modifier : ScriptableObject {

        public enum ModifierType { Static, Loop, OnTrigger }

        public ModifierType m_Type;

        public abstract void Initialize(Mesh mesh);
        public abstract void DrawSettings();
        public abstract void Run(ref GraphicsBuffer baseVertices, ref GraphicsBuffer modVertices);
    }

    public abstract class StaticModifier : Modifier {

        public StaticModifier() {
            m_Type = ModifierType.Static;
        }
    }

    public abstract class LoopModifier : Modifier {

        public LoopModifier() {
            m_Type = ModifierType.Loop;
        }

        public void Start() {
            // Start logic
        }

        public void Stop() {
            // Stop logic
        }
    }

    public abstract class TriggerModifier : Modifier {

        public TriggerModifier() {
            m_Type = ModifierType.OnTrigger;
        }

        public void Trigger() {
            // Trigger logic
        }
    }
}
