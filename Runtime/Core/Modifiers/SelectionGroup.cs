
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.GsplEdit {
    [Serializable]
    public class SelectionGroup {
        public List<ModifierHolder> m_Modifiers;
        public VertexSelectionGroup m_Selection;
        private GroupMeta m_Meta;
        private SharedComputeContext m_Context;
        private ComputeShader m_CSBufferOps;
        private GraphicsBuffer m_Buffer;

        unsafe public SelectionGroup(ref SharedComputeContext context, GroupMeta meta, ComputeShader csBufferOps) {
            m_Selection = new VertexSelectionGroup(ref context, meta.selection);
            m_Modifiers = new List<ModifierHolder>(new ModifierHolder[meta.modifiers.Count]);
            m_Meta = meta;
            m_Context = context;
            m_CSBufferOps = csBufferOps;

            // Initialize Modifiers in the correct order
            for (int i = 0; i < m_Meta.modifiers.Count; i++) {
                int index = m_Meta.modifiers[i].order;
                m_Modifiers[index] = new ModifierHolder(m_Meta.modifiers[i].modifier, m_Meta.modifiers[i]);
                m_Modifiers[index].Initialize(ref m_Context);
            }

            m_Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw | GraphicsBuffer.Target.CopySource, m_Context.scaffoldData.vertexCount, sizeof(Vector3)) { name = "SelectionModifierVertices" };;
            ModifierUtils.ClearBuffer(m_Buffer);
        }

        public void Destroy() {
            m_Selection?.Destroy();
            m_Buffer?.Dispose();
            m_Buffer = null;
        }

        public bool IsEnabled() {
            return m_Meta.enabled;
        }

        public string GetName() {
            return m_Meta.name;
        }

        public void SetName(string name) {
            m_Meta.name = name;
        }

        public void SetEnabled(bool state) {
            m_Meta.enabled = state;
        }

        public void SetOrder(int order) {
            m_Meta.order = order;
        }

        public ModifierHolder AddModifier() {
            ModifierMeta mm = new();
            mm.name = "New Modifier";
            mm.order = m_Modifiers.Count;
            mm.enabled = true;
            m_Modifiers.Add(new ModifierHolder(null, mm));
            m_Meta.modifiers.Add(mm);
            return m_Modifiers.Last();
        }

        public void InsertModifier(ModifierHolder mh) {
            while (m_Modifiers.Count <= mh.m_Meta.order) {
                AddModifier();
            }
            m_Modifiers[mh.m_Meta.order] = mh;
        }

        public void RemoveModifier(uint id) {
            if (id >= m_Modifiers.Count) {
                Debug.LogWarning($"Invalid Id: {id}. No modifier at this position.");
                return;
            }

            m_Modifiers.RemoveAt((int)id);
            m_Meta.modifiers.Remove(m_Modifiers[(int)id].m_Meta);

            // Update order values
            for (int i = 0; i < m_Meta.modifiers.Count; i++) {
                m_Meta.modifiers[i].order = i;
            }
        }

        public void Reorder(uint fromId, uint toId) {
            Debug.Log("Reorder");
            if (fromId >= m_Modifiers.Count || toId >= m_Modifiers.Count) {
                Debug.LogWarning($"Invalid Id(s). FromId: {fromId}, ToId: {toId}. Out of range.");
                return;
            }

            ModifierHolder modifier = m_Modifiers[(int)fromId];
            m_Modifiers.RemoveAt((int)fromId);
            m_Modifiers.Insert((int)toId, modifier);

            for (int i = 0; i < m_Meta.modifiers.Count; i++) {
                m_Meta.modifiers[i].order = i;
            }
        }

        public void EnableAllModifiers() {
            for (int i = 0; i < m_Modifiers.Count; i++) {
                m_Modifiers[i].m_Meta.enabled = true;
            }
        }

        public void DisableAllModifiers() {
            for (int i = 0; i < m_Modifiers.Count; i++) {
                m_Modifiers[i].m_Meta.enabled = false;
            }
        }


        public void Run(ref GraphicsBuffer modifierBuffer) {
            ModifierUtils.ClearBuffer(m_Buffer);
            for (int i = 0; i < m_Modifiers.Count; i++) {
                if (m_Modifiers[i].m_Modifier != null) {
                    if (!m_Modifiers[i].m_Initialized && m_Context.scaffoldMesh != null) {
                        m_Modifiers[i].Initialize(ref m_Context);
                    }

                    if (m_Modifiers[i].m_Meta.enabled && m_Context.scaffoldBaseVertex != null && m_Context.scaffoldModVertex != null) {
                        m_Modifiers[i].m_Modifier?.Run(ref m_Context.scaffoldModVertex, ref m_Buffer);
                    }
                }
            }
            ModifierUtils.ApplyMask(m_CSBufferOps, m_Selection.m_SelectedVerticesBuffer, m_Buffer);
            ModifierUtils.ApplyModifiedBuffer(m_CSBufferOps, modifierBuffer, m_Buffer);
        }
    }

    [Serializable]
    public class ModifierHolder {
        public Modifier m_Modifier = null;
        public ModifierMeta m_Meta;
        public bool m_Initialized = false;

        public ModifierHolder(Modifier modifier, ModifierMeta meta) {
            m_Modifier = modifier;
            m_Meta = meta;
        }

        public void Initialize(ref SharedComputeContext context) {
            m_Modifier.Initialize(context.scaffoldMesh);
            m_Initialized = true;
        }

        public void SetModifier(Modifier modifier) {
            m_Modifier = modifier;
            m_Meta.modifier = modifier;
        }
    }
}
