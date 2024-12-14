using UnityEngine;
using UnityEditor;

namespace TLab.Spline.Editor
{
    [CustomEditor(typeof(SplineCreator))]
    public class SplineCreatorEditor : UnityEditor.Editor
    {
        private SplineCreator m_instance;

        Spline spline => m_instance.spline;

        private const float SEGMENT_SELECT_DISTANCE_THRESHOLD = 20.0f;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUI.BeginChangeCheck();

            if (GUILayout.Button("Create New"))
            {
                Undo.RecordObject(m_instance, "Create New");
                m_instance.CreatePath();
            }

            bool close = GUILayout.Toggle(spline.close, "Close");
            if (close != spline.close)
            {
                Undo.RecordObject(spline, "Toggle Close");
                spline.close = close;
            }

            if (!(spline.numSegments == 2 && spline.close))
            {
                var editMode = (Spline.EditMode)EditorGUILayout.EnumPopup("Edit Mode", spline.editMode);
                if (editMode != spline.editMode)
                {
                    Undo.RecordObject(spline, "Switch Edit Mode");
                    spline.editMode = editMode;
                }
            }

            if (EditorGUI.EndChangeCheck())
                SceneView.RepaintAll();
        }

        private void OnSceneGUI()
        {
            Input();
            Draw();
        }

        private Vector3 TransformPoint(Vector3 point) => m_instance.transform.TransformPoint(point);

        private Vector3 InverseTransformPoint(Vector3 point) => m_instance.transform.InverseTransformPoint(point);

        private void TransformPoints(System.Span<Vector3> points, System.Span<Vector3> transformedPoints) => m_instance.transform.TransformPoints(points, transformedPoints);

        private void InverseTransformPoints(System.Span<Vector3> points, System.Span<Vector3> transformedPoints) => m_instance.transform.InverseTransformPoints(points, transformedPoints);

        private void Input()
        {
            Event guiEvent = Event.current;

            if (guiEvent.type == EventType.KeyDown)
            {
                // add segment
                if (guiEvent.keyCode == KeyCode.A && !spline.close)
                {
                    var mousePos = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
                    RaycastHit hit;

                    if (Physics.Raycast(mousePos, out hit, 100.0f))
                    {
                        Undo.RecordObject(spline, "Add Segment");
                        spline.AddSegment(hit.point);
                    }
                }

                // delete segment
                if (guiEvent.keyCode == KeyCode.D && !(spline.numSegments == 3 && spline.close))
                {
                    var minDstToAnchor = 20.0f;
                    var closestAnchorIndex = -1;

                    for (int i = 0; i < spline.numPoints; i += 3)
                    {
                        var mousePosOnScene = new Vector2(guiEvent.mousePosition.x, SceneView.lastActiveSceneView.camera.pixelHeight - guiEvent.mousePosition.y);
                        var splineScreenPos = SceneView.lastActiveSceneView.camera.WorldToScreenPoint(TransformPoint(spline[i]));
                        var dst = Vector2.Distance(mousePosOnScene, new Vector2(splineScreenPos.x, splineScreenPos.y));

                        if (dst < minDstToAnchor)
                        {
                            minDstToAnchor = dst;
                            closestAnchorIndex = i;
                        }
                    }

                    if (closestAnchorIndex != -1)
                    {
                        Undo.RecordObject(spline, "Delete Segment");
                        spline.DeleteSegment(closestAnchorIndex);
                    }
                }

                // split segment
                if (guiEvent.keyCode == KeyCode.S)
                {
                    float minDstToSegment = SEGMENT_SELECT_DISTANCE_THRESHOLD;
                    int SelectedSegmentIndex = -1;
                    float lerpValue = -1.0f;

                    Vector2 mousePosOnScene = new Vector2(guiEvent.mousePosition.x, SceneView.lastActiveSceneView.camera.pixelHeight - guiEvent.mousePosition.y);

                    for (int i = 0; i < spline.numSegments; i++)
                    {
                        Vector3[] points = spline.GetPointInSegment(i);

                        for (float j = 0.0f; j < 1.0f; j += 0.01f)
                        {
                            Vector3 bezierPosition = TransformPoint(Bezier.EvaluateCubic(points[0], points[1], points[2], points[3], j));
                            Vector3 bezierPositionOnScene = SceneView.lastActiveSceneView.camera.WorldToScreenPoint(bezierPosition);

                            float dst = Vector2.Distance(mousePosOnScene, new Vector2(bezierPositionOnScene.x, bezierPositionOnScene.y));

                            if (dst < minDstToSegment)
                            {
                                minDstToSegment = dst;
                                SelectedSegmentIndex = i;
                                lerpValue = j;
                            }
                        }
                    }

                    if (SelectedSegmentIndex != -1)
                    {
                        Vector3[] points = spline.GetPointInSegment(SelectedSegmentIndex);
                        Vector3 bezierPosition = Bezier.EvaluateCubic(points[0], points[1], points[2], points[3], lerpValue);
                        Undo.RecordObject(spline, "Split Segment");
                        spline.SplitSegment(bezierPosition, SelectedSegmentIndex);
                    }
                }
            }

            HandleUtility.AddDefaultControl(0);
        }

        private void Draw()
        {
            for (int i = 0; i < spline.numSegments; i++)
            {
                var points = spline.GetPointInSegment(i);

                TransformPoints(points, points);

                if (m_instance.displayControlPoints)
                {
                    Handles.DrawLine(points[1], points[0]);
                    Handles.DrawLine(points[2], points[3]);
                }

                Handles.DrawBezier(points[0], points[3], points[1], points[2], m_instance.segmentColor, null, 2);
            }

            Handles.color = Color.red;

            for (int i = 0; i < spline.numPoints; i++)
            {
                if ((i % 3 == 0) && !m_instance.displayAnchorPoints)
                    continue;

                if ((i % 3 != 0) && !m_instance.displayControlPoints)
                    continue;

                var newPos = spline[i];

                SplineCreator.HandleType handleType;
                float handleSize = 0;

                if (i % 3 == 0)
                {
                    handleType = m_instance.anchor.handleType;
                    handleSize = m_instance.anchor.diameter;
                    Handles.color = m_instance.anchor.color;
                }
                else
                {
                    handleType = m_instance.control.handleType;
                    handleSize = m_instance.control.diameter;
                    Handles.color = m_instance.control.color;
                }

                switch (handleType)
                {
                    case SplineCreator.HandleType.Position:
                        newPos = Handles.PositionHandle(TransformPoint(spline[i]), Quaternion.identity);
                        break;
                    case SplineCreator.HandleType.FreeMove:
                        newPos = Handles.FreeMoveHandle(TransformPoint(spline[i]), handleSize, Vector3.zero, Handles.CylinderHandleCap);
                        break;
                }

                newPos = InverseTransformPoint(newPos);

                if (spline[i] != newPos)
                {
                    Undo.RecordObject(spline, "Move Point");
                    spline.MovePoint(i, newPos);
                }
            }
        }

        private void OnEnable()
        {
            m_instance = (SplineCreator)target;

            if (m_instance.spline == null)
                m_instance.CreatePath();
        }
    }
}
