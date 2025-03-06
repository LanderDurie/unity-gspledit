using UnityEngine;
using UnityEngine.GsplEdit;

namespace UnityEditor.GsplEdit
{
    public class MeshGenEditorTab : Tab
    {
        public override void Init(DynamicSplat gs)
        {
        }

        public override void Draw(DynamicSplat gs)
        {
            GUILayout.Label("Mesh Generator Options", EditorStyles.boldLabel);

            // Dropdown for selecting mesh meshGeneration options
            GUILayout.Label("Select Mesh Option", EditorStyles.label);
            MeshGen meshGen = gs.GetMeshGen();
            meshGen.m_SelectedType = (MeshGen.GenType)EditorGUILayout.EnumPopup("Mesh Option", meshGen.m_SelectedType);
            switch (meshGen.m_SelectedType)
            {
                case MeshGen.GenType.Icosahedron:
                    DrawIcosahedronSettings((IcosaehdronGen)meshGen.m_Generators[meshGen.m_SelectedType]);
                    break;
                case MeshGen.GenType.MarchingCubes:
                    DrawMarchingCubesSettings((MarchingCubesGen)meshGen.m_Generators[meshGen.m_SelectedType]);
                    break;
                case MeshGen.GenType.SurfaceNets:
                    DrawSurfaceNetsSettings((SurfaceNetsGen)meshGen.m_Generators[meshGen.m_SelectedType]);
                    break;
                case MeshGen.GenType.DualContouringGen:
                    DrawDualContourSettings((DualContouringGen)meshGen.m_Generators[meshGen.m_SelectedType]);
                    break;
            }

            GUILayout.Label("Link Generator Options", EditorStyles.boldLabel);

            LinkGen linkGen = gs.GetLinkGen();
            linkGen.m_ForwardSelectedType = (LinkGen.ForwardGenType)EditorGUILayout.EnumPopup("Forward Link Option", linkGen.m_ForwardSelectedType);
            switch (linkGen.m_ForwardSelectedType)
            {
                case LinkGen.ForwardGenType.Euclidean:
                    DrawEuclideanSettings((EuclideanGen)linkGen.m_ForwardGenerators[linkGen.m_ForwardSelectedType]);
                    break;
                case LinkGen.ForwardGenType.Mahalanobis:
                    DrawMahalanobisSettings((MahalanobisGen)linkGen.m_ForwardGenerators[linkGen.m_ForwardSelectedType]);
                    break;
                case LinkGen.ForwardGenType.Interpolate:
                    DrawInterpolateSettings((InterpolateGen)linkGen.m_ForwardGenerators[linkGen.m_ForwardSelectedType]);
                    break;
                case LinkGen.ForwardGenType.PCASmooth:
                    DrawPCASmoothSettings((PCASmoothGen)linkGen.m_ForwardGenerators[linkGen.m_ForwardSelectedType]);
                    break;
            }

            linkGen.m_BackwardSelectedType = (LinkGen.BackwardGenType)EditorGUILayout.EnumPopup("Backward Link Option", linkGen.m_BackwardSelectedType);
            switch (linkGen.m_BackwardSelectedType)
            {
                case LinkGen.BackwardGenType.Euclidean:
                    break;
            }

            if (GUILayout.Button("Bake Mesh"))
            {
                gs.GenerateMesh();
                gs.GenerateLinks();
            }

            if (GUILayout.Button("Recreate Links"))
            {
                gs.GenerateLinks();
            }
        }

        private void DrawIcosahedronSettings(IcosaehdronGen meshGen)
        {
            meshGen.m_Settings.scale = EditorGUILayout.FloatField("Scale", Mathf.Clamp(meshGen.m_Settings.scale, 0.1f, 100.0f));
            meshGen.m_Settings.limit = EditorGUILayout.IntField(
                new GUIContent("Draw Limit", ""), 
                meshGen.m_Settings.limit
            );
        }

        private void DrawMarchingCubesSettings(MarchingCubesGen meshGen)
        {
            meshGen.m_Settings.scale = EditorGUILayout.FloatField("Scale", Mathf.Clamp(meshGen.m_Settings.scale, 0.1f, 100.0f));
            meshGen.m_Settings.threshold = EditorGUILayout.Slider(
                new GUIContent("Activation threshold", ""), 
                meshGen.m_Settings.threshold, 
                0.0f, 
                1.0f
            );
            meshGen.m_Settings.cutoff = EditorGUILayout.Slider(
                new GUIContent("Isosurface Cutoff", ""), 
                meshGen.m_Settings.cutoff, 
                0.0f, 
                1.0f
            );
            meshGen.m_Settings.lod = EditorGUILayout.IntField("Level Of Detail", Mathf.Clamp(meshGen.m_Settings.lod, 4, 1000));
        }

        private void DrawSurfaceNetsSettings(SurfaceNetsGen meshGen)
        {
            meshGen.m_Settings.scale = EditorGUILayout.FloatField("Scale", Mathf.Clamp(meshGen.m_Settings.scale, 0.1f, 100.0f));
            meshGen.m_Settings.threshold = EditorGUILayout.Slider(
                new GUIContent("Activation threshold", ""), 
                meshGen.m_Settings.threshold, 
                0.0f, 
                1.0f
            );
            meshGen.m_Settings.lod = EditorGUILayout.IntField("Level Of Detail", Mathf.Clamp(meshGen.m_Settings.lod, 4, 1000));
        }

        private void DrawDualContourSettings(DualContouringGen meshGen)
        {
            meshGen.m_Settings.scale = EditorGUILayout.FloatField("Scale", Mathf.Clamp(meshGen.m_Settings.scale, 0.1f, 100.0f));
            meshGen.m_Settings.threshold = EditorGUILayout.Slider(
                new GUIContent("Activation threshold", ""), 
                meshGen.m_Settings.threshold, 
                0.0f, 
                1.0f
            );
            meshGen.m_Settings.lod = EditorGUILayout.IntField("Level Of Detail", Mathf.Clamp(meshGen.m_Settings.lod, 4, 1000));
            meshGen.m_Settings.maxDepth = EditorGUILayout.IntField("Max Tree Depth", Mathf.Clamp(meshGen.m_Settings.maxDepth, 1, 100));
        }

        private void DrawMahalanobisSettings(MahalanobisGen linkGen)
        {
            linkGen.m_Settings.sigma = EditorGUILayout.Slider(
                new GUIContent("Distribution Size", ""), 
                linkGen.m_Settings.sigma, 
                0.01f, 
                100.0f
            );
        }

        private void DrawEuclideanSettings(EuclideanGen linkGen)
        {
            linkGen.m_Settings.sigma = EditorGUILayout.Slider(
                new GUIContent("Distribution Size", ""), 
                linkGen.m_Settings.sigma, 
                0.01f, 
                100.0f
            );
        }

        private void DrawInterpolateSettings(InterpolateGen linkGen)
        {
            linkGen.m_Settings.blendFactor = EditorGUILayout.Slider(
                new GUIContent("Blend Factor", ""), 
                linkGen.m_Settings.blendFactor, 
                0.0f, 
                100.0f
            );
        }

        private void DrawPCASmoothSettings(PCASmoothGen linkGen)
        {
            linkGen.m_Settings.startBlend = EditorGUILayout.Slider(
                new GUIContent("Start Blend Distance", ""), 
                linkGen.m_Settings.startBlend, 
                0.0f, 
                50.0f
            );
            linkGen.m_Settings.stopBlend = EditorGUILayout.Slider(
                new GUIContent("Stop Blend Distance", ""), 
                linkGen.m_Settings.stopBlend, 
                0.0f, 
                100.0f
            );
        }
    }
}
