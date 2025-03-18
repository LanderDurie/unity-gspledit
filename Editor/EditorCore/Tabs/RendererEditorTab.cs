using UnityEngine;
using UnityEngine.GsplEdit;

namespace UnityEditor.GsplEdit {
    public class RendererEditorTab : Tab {
        public override void Init(DynamicSplat gs) {}

        public override void Draw(DynamicSplat gs) {
            GUILayout.Label("Splat Render Settings", EditorStyles.boldLabel);
            GUILayout.Space(10);
            DrawSplatSettins(gs);

            DrawUtils.Separator();
            GUILayout.Label("Scaffold Render Settings", EditorStyles.boldLabel);
            GUILayout.Space(10);
            DrawScaffoldSettings(gs);  
            
            DrawUtils.Separator();
            GUILayout.Label("Surface Render Settings", EditorStyles.boldLabel);
            GUILayout.Space(10);
            DrawSurfaceSettings(gs);
        }

        private void DrawSplatSettins(DynamicSplat gs) {
            // Retrieve the GSRenderer instance
            GSRenderer gsr = gs.GetSplatRenderer();
            if (gsr == null) {
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
            if (GUI.changed) {
                EditorUtility.SetDirty(gs);
            }
        }

        private void DrawScaffoldSettings(DynamicSplat gs) {
            EditableMesh mesh = gs.GetMesh();

            if (mesh == null) {
                EditorGUILayout.HelpBox("No mesh has been created.", MessageType.Warning);
                return;
            }

            Material scaffoldMaterial = mesh.m_ScaffoldMaterial;

            // Ensure the vertexMaterial is assigned
            if (scaffoldMaterial == null) {
                EditorGUILayout.HelpBox("Scaffold material is not assigned.", MessageType.Warning);
                return;
            }

            // Point Size
            float pointSize = scaffoldMaterial.GetFloat("_PointSize");
            pointSize = EditorGUILayout.Slider(
                new GUIContent("Point Size", "Adjust the size of point rendering"),
                pointSize,
                0.01f,
                5.0f
            );
            scaffoldMaterial.SetFloat("_PointSize", pointSize);

            // Default Color
            Color defaultColor = scaffoldMaterial.GetColor("_DefaultColor");
            defaultColor = EditorGUILayout.ColorField(
                new GUIContent("Default Color", "The default vertex color"),
                defaultColor
            );
            scaffoldMaterial.SetColor("_DefaultColor", defaultColor);

            // Selected Color
            Color selectedColor = scaffoldMaterial.GetColor("_SelectedColor");
            selectedColor = EditorGUILayout.ColorField(
                new GUIContent("Selected Color", "The color for selected vertices"),
                selectedColor
            );
            scaffoldMaterial.SetColor("_SelectedColor", selectedColor);

            // Wireframe Selected Color
            Color wireframeColor = scaffoldMaterial.GetColor("_WireframeColour");
            wireframeColor = EditorGUILayout.ColorField(
                new GUIContent("Wireframe Color", "The color for wireframe"),
                wireframeColor
            );
            scaffoldMaterial.SetColor("_WireframeColour", wireframeColor);

            // Wireframe Alias
            float wireframeAlias = scaffoldMaterial.GetFloat("_WireframeAliasing");
            wireframeAlias = EditorGUILayout.Slider(
                new GUIContent("Wireframe Alias", ""),
                wireframeAlias,
                0.01f,
                10.0f
            );
            scaffoldMaterial.SetFloat("_WireframeAliasing", wireframeAlias);

            // Wireframe Enabled
            mesh.m_DrawScaffoldMesh = EditorGUILayout.Toggle("Wireframe Enabled", mesh.m_DrawScaffoldMesh);
            
            if (GUI.changed) {
                EditorUtility.SetDirty(gs);
            }        
        }

        private void DrawSurfaceSettings(DynamicSplat gs) {
            EditableMesh mesh = gs.GetMesh();

            if (mesh == null) {
                EditorGUILayout.HelpBox("No mesh has been created.", MessageType.Warning);
                return;
            }

            Material surfaceMaterial = mesh.m_SurfaceMaterial;

            // Shadow Toggles
            mesh.m_CastShadows = EditorGUILayout.Toggle(new GUIContent("Cast Shadows"), mesh.m_CastShadows);
            mesh.m_ReceiveShadows = EditorGUILayout.Toggle(new GUIContent("Receive Lighting"), mesh.m_ReceiveShadows);
            
            if (surfaceMaterial.HasProperty("_DiffuseComponent")) {
                float diffuseComponent = surfaceMaterial.GetFloat("_DiffuseComponent");
                diffuseComponent = EditorGUILayout.Slider(new GUIContent("Diffuse Component"), diffuseComponent, 0f, 1f);
                surfaceMaterial.SetFloat("_DiffuseComponent", diffuseComponent);
            }

            if (surfaceMaterial.HasProperty("_ShadowStrength")) {
                float shadowStrength = surfaceMaterial.GetFloat("_ShadowStrength");
                shadowStrength = EditorGUILayout.Slider(new GUIContent("Light Factor"), shadowStrength, 0f, 1f);
                surfaceMaterial.SetFloat("_ShadowStrength", shadowStrength);
            }

            if (surfaceMaterial.HasProperty("_AmbientLight")) {
                float ambientLight = surfaceMaterial.GetFloat("_AmbientLight");
                ambientLight = EditorGUILayout.Slider(new GUIContent("Ambient Component"), ambientLight, 0f, 1f);
                surfaceMaterial.SetFloat("_AmbientLight", ambientLight);
            }
            

            if (GUI.changed) {
                EditorUtility.SetDirty(gs);
            }   
        }
    }
}
