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

            if (GUILayout.Button("Create new"))
            {
                Undo.RecordObject(m_instance, "Create new");
                m_instance.CreatePath();
            }

            bool isClosed = GUILayout.Toggle(spline.isClosed, "Closed");
            if (isClosed != spline.isClosed)
            {
                Undo.RecordObject(spline, "Toggle closed");
                spline.isClosed = isClosed;
            }

            if (!(spline.numSegments == 2 && spline.isClosed))
            {
                bool autoSetControlPoints = GUILayout.Toggle(spline.autoSetControlPoints, "Auto set control points");
                if (autoSetControlPoints != spline.autoSetControlPoints)
                {
                    Undo.RecordObject(spline, "Toggle auto set controls");
                    spline.autoSetControlPoints = autoSetControlPoints;
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }
        }

        private void OnSceneGUI()
        {
            Input();
            Draw();
        }

        private Vector3 TransformPoint(Vector3 point)
        {
            return m_instance.transform.TransformPoint(point);
        }

        private Vector3 InverseTransformPoint(Vector3 point)
        {
            return m_instance.transform.InverseTransformPoint(point);
        }

        private void TransformPoints(System.Span<Vector3> points, System.Span<Vector3> transformedPoints)
        {
            m_instance.transform.TransformPoints(points, transformedPoints);
        }

        private void InverseTransformPoints(System.Span<Vector3> points, System.Span<Vector3> transformedPoints)
        {
            m_instance.transform.InverseTransformPoints(points, transformedPoints);
        }

        private void Input()
        {
            Event guiEvent = Event.current;

            if (guiEvent.type == EventType.KeyDown)
            {
                // add segment
                if (guiEvent.keyCode == KeyCode.A && !spline.isClosed)
                {
                    var mousePos = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
                    RaycastHit hit;

                    if (Physics.Raycast(mousePos, out hit, 100.0f))
                    {
                        Undo.RecordObject(spline, "Add segment");
                        spline.AddSegment(hit.point);
                    }
                }

                // delete segment
                if (guiEvent.keyCode == KeyCode.D && !(spline.numSegments == 3 && spline.isClosed))
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
                        Undo.RecordObject(spline, "Split segment");
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

                Handles.DrawBezier(points[0], points[3], points[1], points[2], m_instance.segmentCol, null, 2);
            }

            Handles.color = Color.red;

            for (int i = 0; i < spline.numPoints; i++)
            {
                if (i % 3 == 0 || m_instance.displayControlPoints)
                {
                    var newPos = spline[i];

                    SplineCreator.HandleType handleType;
                    float handleSize = 0;

                    if (i % 3 == 0)
                    {
                        handleType = m_instance.anchorHandle;
                        handleSize = m_instance.anchorDiameter;
                        Handles.color = m_instance.anchorCol;
                    }
                    else
                    {
                        handleType = m_instance.controlHandle;
                        handleSize = m_instance.controlDiameter;
                        Handles.color = m_instance.controlCol;
                    }

                    switch (handleType)
                    {
                        case SplineCreator.HandleType.POSITION:
                            newPos = Handles.PositionHandle(TransformPoint(spline[i]), Quaternion.identity);
                            break;
                        case SplineCreator.HandleType.FREE_MOVE:
                            newPos = Handles.FreeMoveHandle(TransformPoint(spline[i]), handleSize, Vector3.zero, Handles.CylinderHandleCap);
                            break;
                    }

                    newPos = InverseTransformPoint(newPos);

                    if (spline[i] != newPos)
                    {
                        Undo.RecordObject(spline, "Move point");
                        spline.MovePoint(i, newPos);
                    }
                }
            }
        }

        private void OnEnable()
        {
            m_instance = (SplineCreator)target;

            if (m_instance.spline == null)
            {
                m_instance.CreatePath();
            }
        }
    }
}
