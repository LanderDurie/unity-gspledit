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
            }

            GUILayout.Label("Link Generator Options", EditorStyles.boldLabel);

            LinkGen linkGen = gs.GetLinkGen();
            linkGen.m_SelectedType = (LinkGen.GenType)EditorGUILayout.EnumPopup("Link Option", linkGen.m_SelectedType);
            switch (linkGen.m_SelectedType)
            {
                case LinkGen.GenType.Distance:
                    DrawDistanceGenSettings((DistanceGen)linkGen.m_Generators[linkGen.m_SelectedType]);
                    break;
                case LinkGen.GenType.Mahalanobis:
                    DrawMahalanobisSettings((MahalanobisGen)linkGen.m_Generators[linkGen.m_SelectedType]);
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
            meshGen.m_Settings.threshold = EditorGUILayout.Slider(
                new GUIContent("Activation threshold", ""), 
                meshGen.m_Settings.threshold, 
                0.0f, 
                1.0f
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

        private void DrawMahalanobisSettings(MahalanobisGen linkGen)
        {
            linkGen.m_Settings.sigmaSize = EditorGUILayout.Slider(
                new GUIContent("Size Sigma", ""), 
                linkGen.m_Settings.sigmaSize, 
                0.01f, 
                5.0f
            );
        }

        private void DrawDistanceGenSettings(DistanceGen linkGen)
        {

        }
    }
}
