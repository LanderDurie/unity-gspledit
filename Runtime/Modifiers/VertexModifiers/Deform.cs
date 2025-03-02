using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityEngine.GsplEdit
{
    public class DeformModifier : Modifier 
    {
        public enum GenType { Sine, Wave, Twist, Bend }

        private Dictionary<GenType, DeformBase> m_Generators = new Dictionary<GenType, DeformBase>();
        public GenType m_SelectedType;

        private GameObject _deformContainer;

        public DeformModifier(ref SharedComputeContext context, ref VertexSelectionGroup selectionGroup) 
        {
            m_Context = context;
            m_SelectionGroup = selectionGroup;
            
            // Create a container GameObject to hold deformers
            _deformContainer = new GameObject("Deformers");
            _deformContainer.hideFlags = HideFlags.HideAndDontSave; // Prevent clutter in the scene

            // Setup deformers as MonoBehaviours
            SetupDeformers();
        }
        
        private void SetupDeformers()
        {
            // Instantiate deformers as components of the container GameObject
            m_Generators[GenType.Sine] = _deformContainer.AddComponent<SinDeform>();
            m_Generators[GenType.Wave] = _deformContainer.AddComponent<WaveDeform>();
            m_Generators[GenType.Twist] = _deformContainer.AddComponent<TwistDeform>();
            m_Generators[GenType.Bend] = _deformContainer.AddComponent<BendDeform>();

            foreach (var deform in m_Generators.Values)
            {
                deform.Initialize(m_Context, m_SelectionGroup);
            }

            // Set default if none selected
            if (!m_Generators.ContainsKey(m_SelectedType))
            {
                m_SelectedType = GenType.Sine;
            }
        }

        public override void Run() 
        {
            if (m_Generators.TryGetValue(m_SelectedType, out DeformBase generator))
            {
                generator.Run();
            }
            else
            {
                Debug.LogWarning($"Selected generator type {m_SelectedType} not found!");
            }
        }

        public override void DrawSettings() 
        {
            GUILayout.Label("Deform Modifier", EditorStyles.boldLabel);

            // Create a dropdown to select the deformation type
            EditorGUI.BeginChangeCheck();
            GenType previousType = m_SelectedType;
            m_SelectedType = (GenType)EditorGUILayout.EnumPopup("Deformation Type", m_SelectedType);

            if (EditorGUI.EndChangeCheck() && previousType != m_SelectedType)
            {
                Debug.Log($"Switched to {m_SelectedType} deformer");
            }

            // Draw settings for the selected generator
            if (m_Generators.TryGetValue(m_SelectedType, out DeformBase generator))
            {
                generator.DrawSettings();
            }
        }
    }
}
