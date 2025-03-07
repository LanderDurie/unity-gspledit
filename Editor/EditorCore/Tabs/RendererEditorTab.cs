using UnityEngine;
using UnityEngine.GsplEdit;

namespace UnityEditor.GsplEdit
{
    public class RendererEditorTab : Tab
    {
        public override void Init(DynamicSplat gs)
        {
        }

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
            GUILayout.Label("Wireframe Render Settings", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditableMesh mesh = gs.GetMesh();

            if (mesh == null) {
                EditorGUILayout.HelpBox("No mesh has been created.", MessageType.Warning);
                return;
            }

            Material vertexMaterial = mesh.m_SelectedVertexMaterial;
            Material wireframeMaterial = mesh.m_WireframeMaterial;
            Material fillMaterial = mesh.m_FillMaterial;

            // Ensure the vertexMaterial is assigned
            if (vertexMaterial == null)
            {
                EditorGUILayout.HelpBox("Vertex material is not assigned.", MessageType.Warning);
                return;
            }

            if (vertexMaterial == null)
            {
                EditorGUILayout.HelpBox("Wireframe material is not assigned.", MessageType.Warning);
                return;
            }

            // Point Size
            float pointSize = vertexMaterial.GetFloat("_PointSize");
            pointSize = EditorGUILayout.Slider(
                new GUIContent("Point Size", "Adjust the size of point rendering"),
                pointSize,
                0.01f,
                5.0f
            );
            vertexMaterial.SetFloat("_PointSize", pointSize);

            // Default Color
            Color defaultColor = vertexMaterial.GetColor("_DefaultColor");
            defaultColor = EditorGUILayout.ColorField(
                new GUIContent("Default Color", "The default vertex color"),
                defaultColor
            );
            vertexMaterial.SetColor("_DefaultColor", defaultColor);

            // Selected Color
            Color selectedColor = vertexMaterial.GetColor("_SelectedColor");
            selectedColor = EditorGUILayout.ColorField(
                new GUIContent("Selected Color", "The color for selected vertices"),
                selectedColor
            );
            vertexMaterial.SetColor("_SelectedColor", selectedColor);

            // Wireframe Selected Color
            Color wireframeColor = wireframeMaterial.GetColor("_WireframeColour");
            wireframeColor = EditorGUILayout.ColorField(
                new GUIContent("Wireframe Color", "The color for wireframe"),
                wireframeColor
            );
            wireframeMaterial.SetColor("_WireframeColour", wireframeColor);

            // Wireframe Alias
            float wireframeAlias = wireframeMaterial.GetFloat("_WireframeAliasing");
            wireframeAlias = EditorGUILayout.Slider(
                new GUIContent("Wireframe Alias", ""),
                wireframeAlias,
                0.01f,
                10.0f
            );
            wireframeMaterial.SetFloat("_WireframeAliasing", wireframeAlias);

            // Wireframe Enabled
            float wireframeEnabledFloat = wireframeMaterial.GetFloat("_Enable");
            bool wireframeEnabled = wireframeEnabledFloat > 0.5f;
            bool newWireframeEnabled = EditorGUILayout.Toggle("Wireframe Enabled", wireframeEnabled);
            if (newWireframeEnabled != wireframeEnabled)
            {
                wireframeMaterial.SetFloat("_Enable", newWireframeEnabled ? 1.0f : 0.0f);
                vertexMaterial.SetFloat("_Enable", newWireframeEnabled ? 1.0f : 0.0f);
                EditorUtility.SetDirty(wireframeMaterial);
                EditorUtility.SetDirty(vertexMaterial);
            }

            DrawUtils.Separator();
            GUILayout.Label("Mesh Render Settings", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Mesh Selected Color
            Color meshColor = fillMaterial.GetColor("_Color");
            meshColor = EditorGUILayout.ColorField(
                new GUIContent("Mesh Color", "The color of the Mesh"),
                meshColor
            );
            fillMaterial.SetColor("_Color", meshColor);

            // Shadow Toggles
            mesh.m_CastShadow = EditorGUILayout.Toggle(new GUIContent("Cast Shadows", "Enable shadow casting"), mesh.m_CastShadow);
            bool receiveShadows = fillMaterial.GetFloat("_ReceiveShadows") > 0.5f;

            receiveShadows = EditorGUILayout.Toggle(new GUIContent("Receive Shadows", "Enable shadow reception"), receiveShadows);

            fillMaterial.SetFloat("_CastShadows", mesh.m_CastShadow ? 1f : 0f);
            fillMaterial.SetFloat("_ReceiveShadows", receiveShadows ? 1f : 0f);
        }
    }
}
