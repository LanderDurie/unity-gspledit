using System;
using System.Collections.Generic;

namespace UnityEngine.GsplEdit {
    [Serializable]
    public class ModifierData : ScriptableObject {
        public List<GroupMeta> groups;

        public ModifierData() {
            groups = new();
        }
    }

    [Serializable]
    public class GroupMeta {
        public uint[] selection;
        public List<ModifierMeta> modifiers;
        public bool enabled;
        public string name;
        public int order;

        public GroupMeta() {
            modifiers = new();
        }
    }

    [Serializable]
    public class ModifierMeta {
        public Modifier modifier;
        public bool enabled;
        public string name;
        public int order;
    }
}
