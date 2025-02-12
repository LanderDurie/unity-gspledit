using UnityEngine;
using UnityEngine.GsplEdit;

namespace UnityEditor.GsplEdit
{
    public class SelectorTool : Editor
    {
        private static Vector2 m_StartPos;
        public enum EditorMode { DISABLED, CLICK, DRAG }
        public enum EditorTool { MOVE, ROTATE, SCALE }
        public static EditorMode m_CurrentMode = EditorMode.DISABLED;
        public static EditorTool m_CurrentTool = EditorTool.MOVE;
        private const float POINT_SELECTION_RADIUS = 7f;


        public static void Draw(DynamicSplat gs, SceneView sceneView)
        {

            Event evt = Event.current;

            EditableMesh mesh = gs.GetMesh();
            if (mesh == null)
                return;

            switch (Tools.current)
            {
                case Tool.Move:
                    m_CurrentTool = EditorTool.MOVE;
                    break;
                case Tool.Rotate:
                    m_CurrentTool = EditorTool.ROTATE;
                    break;
                case Tool.Scale:
                    m_CurrentTool = EditorTool.SCALE;
                    break;
            }
                        
            Handles.BeginGUI();
            // Draw selection rectangle
            Rect a = FromToRect(m_StartPos, evt.mousePosition);
            EditorGUI.DrawRect(a, new Color(0, 0.5f, 1f, 0.1f));
            EditorGUI.DrawRect(a, new Color(0, 0.5f, 1f, 0.6f));
            Handles.EndGUI();
            SceneView.RepaintAll();

            Vector3 newPos = mesh.m_LocalPos;
            Quaternion newRot = mesh.m_LocalRot;
            Vector3 newScale = mesh.m_LocalScale;

            // Draw tool handle
            if (mesh.m_SelectionGroup.m_SelectedCount > 0) {
                switch (m_CurrentTool)
                {
                    case EditorTool.MOVE:
                        {
                            newPos = Handles.PositionHandle(mesh.m_LocalPos, Quaternion.identity);
                            break;
                        }
                    case EditorTool.ROTATE:
                        {
                            newRot = Handles.RotationHandle(mesh.m_LocalRot, newPos);
                            break;
                        }
                    case EditorTool.SCALE:
                        {
                            newScale = Handles.ScaleHandle(mesh.m_LocalScale, newPos, Quaternion.identity);
                            break;
                        }
                }
            }

            // Start selection (on MouseDown)
            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                m_CurrentMode = EditorMode.CLICK;
                m_StartPos = evt.mousePosition;

                // If not holding shift, clear previous selection
                if (!evt.shift)
                {
                    mesh.DeselectAll();
                    SceneView.RepaintAll();
                }
                evt.Use();

            }

            // Handle dragging logic
            if (Event.current.type == EventType.Used && GUI.changed)
            {
                switch (m_CurrentTool)
                {
                    case EditorTool.MOVE:
                        {
                            if (mesh.m_LocalPos != newPos)
                            {
                                mesh.EditVertexTransformation(newPos - mesh.m_LocalPos, new Vector4(0, 0, 0, 1), new Vector3());
                                mesh.m_LocalPos = newPos;
                            }
                            break;
                        }
                    case EditorTool.ROTATE:
                        {
                            if (mesh.m_LocalRot != newRot)
                            {
                                Quaternion rotationDiff = Quaternion.Inverse(mesh.m_LocalRot) * newRot;
                                mesh.EditVertexTransformation(new Vector3(), new Vector4(rotationDiff.x, rotationDiff.y, rotationDiff.z, rotationDiff.w), new Vector3());
                                mesh.m_LocalRot = newRot;
                            }
                            break;
                        }
                    case EditorTool.SCALE:
                        {
                            if (mesh.m_LocalScale != newScale)
                            {
                                mesh.EditVertexTransformation(new Vector3(), new Vector4(0, 0, 0, 1), newScale - mesh.m_LocalScale);
                                mesh.m_LocalScale = newScale;
                            }
                            break;
                        }
                }
                m_StartPos = evt.mousePosition;
            }
            else
            {
                if (evt.type == EventType.MouseDrag && evt.button == 0)
                {
                    m_CurrentMode = EditorMode.DRAG;
                }
            }

            // End selection (on MouseUp)
            if (evt.type == EventType.MouseUp && evt.button == 0)
            {
                if (m_CurrentMode == EditorMode.CLICK)
                {
                    Vector2 rectMin = HandleUtility.GUIPointToScreenPixelCoordinate(evt.mousePosition - new Vector2(POINT_SELECTION_RADIUS, POINT_SELECTION_RADIUS));
                    Vector2 rectMax = HandleUtility.GUIPointToScreenPixelCoordinate(evt.mousePosition + new Vector2(POINT_SELECTION_RADIUS, POINT_SELECTION_RADIUS));
                    mesh.EditUpdateSelection(rectMin, rectMax, sceneView.camera);
                }
                else if (m_CurrentMode == EditorMode.DRAG)
                {
                    Rect selectionRect = FromToRect(m_StartPos, evt.mousePosition);
                    Vector2 rectMin = HandleUtility.GUIPointToScreenPixelCoordinate(selectionRect.min);
                    Vector2 rectMax = HandleUtility.GUIPointToScreenPixelCoordinate(selectionRect.max);
                    mesh.EditUpdateSelection(rectMin, rectMax, sceneView.camera);
                }

                m_CurrentMode = EditorMode.DISABLED;
            }

            if (m_CurrentMode == EditorMode.DISABLED) {
                m_StartPos = evt.mousePosition;
            }
        }

            static Rect FromToRect(Vector2 from, Vector2 to)
    {
        if (from.x > to.x) (from.x, to.x) = (to.x, from.x);
        if (from.y > to.y) (from.y, to.y) = (to.y, from.y);
        return new Rect(from.x, from.y, to.x - from.x, to.y - from.y);
    }
    }
}
