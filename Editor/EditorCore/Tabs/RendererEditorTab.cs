using UnityEngine;
using UnityEngine.GsplEdit;

namespace UnityEditor.GsplEdit
{
    public class RendererEditorTab : Tab
    {
        public override void Draw(DynamicSplat gs)
        {
            GUILayout.Label("Splat Render Settings", EditorStyles.boldLabel);
            GUILayout.Space(10);
            // Retrieve the GSRenderer instance
            GSRenderer gsr = gs.GetSplatRenderer();
            if (gsr == null)
            {
                EditorGUILayout.HelpBox("GSRenderer is not assigned.", MessageType.Warning);
                return;
            }

            // Draw properties with appropriate fields
            gsr.m_SplatScale = EditorGUILayout.Slider(
                new GUIContent("Splat Scale", "Additional scaling factor for the splats"), 
                gsr.m_SplatScale, 
                0.1f, 
                2.0f
            );

            gsr.m_OpacityScale = EditorGUILayout.Slider(
                new GUIContent("Opacity Scale", "Additional scaling factor for opacity"), 
                gsr.m_OpacityScale, 
                0.05f, 
                20.0f
            );

            gsr.m_SHOrder = EditorGUILayout.IntSlider(
                new GUIContent("SH Order", "Spherical Harmonics order to use"), 
                gsr.m_SHOrder, 
                0, 
                3
            );

            gsr.m_SHOnly = EditorGUILayout.Toggle(
                new GUIContent("SH Only", "Show only Spherical Harmonics contribution, using gray color"), 
                gsr.m_SHOnly
            );

            gsr.m_SortNthFrame = EditorGUILayout.IntSlider(
                new GUIContent("Sort Nth Frame", "Sort splats only every N frames"), 
                gsr.m_SortNthFrame, 
                1, 
                30
            );

            gsr.m_RenderMode = (GSRenderer.RenderMode)EditorGUILayout.EnumPopup(
                new GUIContent("Render Mode", "Choose the rendering mode"), 
                gsr.m_RenderMode
            );

            gsr.m_PointDisplaySize = EditorGUILayout.Slider(
                new GUIContent("Point Display Size", "Adjust the size of point rendering"), 
                gsr.m_PointDisplaySize, 
                1.0f, 
                15.0f
            );

            // Mark as dirty if changes were made
            if (GUI.changed)
            {
                EditorUtility.SetDirty(gsr);
            }

            DrawUtils.Separator();
            GUILayout.Label("Mesh Render Settings", EditorStyles.boldLabel);
            GUILayout.Space(10);
        }
    }
}
