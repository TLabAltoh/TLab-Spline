using UnityEngine;
using UnityEditor;

namespace TLab.CurveTool.Editor
{
    [CustomEditor(typeof(PathCreator))]
    public class PathEditor : UnityEditor.Editor
    {
        private PathCreator m_instance;

        Path path
        {
            get
            {
                return m_instance.path;
            }
        }

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

            bool isClosed = GUILayout.Toggle(path.IsClosed, "Closed");
            if (isClosed != path.IsClosed)
            {
                Undo.RecordObject(m_instance, "Toggle closed");
                path.IsClosed = isClosed;
            }

            if (!(path.NumSegments == 2 && path.IsClosed))
            {
                bool autoSetControlPoints = GUILayout.Toggle(path.AutoSetControlPoints, "Auto Set Control Points");
                if (autoSetControlPoints != path.AutoSetControlPoints)
                {
                    Undo.RecordObject(m_instance, "Toggle auto set controls");
                    path.AutoSetControlPoints = autoSetControlPoints;
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
                if (guiEvent.keyCode == KeyCode.A && !path.IsClosed)
                {
                    Ray mousePos = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
                    RaycastHit hit;

                    if (Physics.Raycast(mousePos, out hit, 100.0f))
                    {
                        Undo.RecordObject(m_instance, "Add segment");
                        path.AddSegment(hit.point);
                    }
                }

                // delete segment
                if (guiEvent.keyCode == KeyCode.D && !(path.NumSegments == 3 && path.IsClosed))
                {
                    float minDstToAnchor = 20.0f;
                    int closestAnchorIndex = -1;

                    for (int i = 0; i < path.NumPoints; i += 3)
                    {
                        Vector2 mousePosOnScene = new Vector2(guiEvent.mousePosition.x, SceneView.lastActiveSceneView.camera.pixelHeight - guiEvent.mousePosition.y);
                        Vector3 pathScreenPos = SceneView.lastActiveSceneView.camera.WorldToScreenPoint(TransformPoint(path[i]));
                        float dst = Vector2.Distance(mousePosOnScene, new Vector2(pathScreenPos.x, pathScreenPos.y));

                        if (dst < minDstToAnchor)
                        {
                            minDstToAnchor = dst;
                            closestAnchorIndex = i;
                        }
                    }

                    if (closestAnchorIndex != -1)
                    {
                        Undo.RecordObject(m_instance, "Delete Segment");
                        path.DeleteSegment(closestAnchorIndex);
                    }
                }

                // split segment
                if (guiEvent.keyCode == KeyCode.S)
                {
                    float minDstToSegment = SEGMENT_SELECT_DISTANCE_THRESHOLD;
                    int SelectedSegmentIndex = -1;
                    float lerpValue = -1.0f;

                    Vector2 mousePosOnScene = new Vector2(guiEvent.mousePosition.x, SceneView.lastActiveSceneView.camera.pixelHeight - guiEvent.mousePosition.y);

                    for (int i = 0; i < path.NumSegments; i++)
                    {
                        Vector3[] points = path.GetPointInSegment(i);

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
                        Vector3[] points = path.GetPointInSegment(SelectedSegmentIndex);
                        Vector3 bezierPosition = Bezier.EvaluateCubic(points[0], points[1], points[2], points[3], lerpValue);
                        Undo.RecordObject(m_instance, "Split segment");
                        path.SplitSegment(bezierPosition, SelectedSegmentIndex);
                    }
                }
            }

            HandleUtility.AddDefaultControl(0);
        }

        private void Draw()
        {
            for (int i = 0; i < path.NumSegments; i++)
            {
                Vector3[] points = path.GetPointInSegment(i);

                TransformPoints(points, points);

                if (m_instance.displayControlPoints)
                {
                    Handles.DrawLine(points[1], points[0]);
                    Handles.DrawLine(points[2], points[3]);
                }

                Handles.DrawBezier(points[0], points[3], points[1], points[2], m_instance.segmentCol, null, 2);
            }

            Handles.color = Color.red;

            for (int i = 0; i < path.NumPoints; i++)
            {
                if (i % 3 == 0 || m_instance.displayControlPoints)
                {
                    Vector3 newPos = path[i];

                    PathCreator.HandleType handleType;

                    if (i % 3 == 0)
                    {
                        handleType = m_instance.anchorHandle;
                        Handles.color = m_instance.anchorCol;
                    }
                    else
                    {
                        handleType = m_instance.controlHandle;
                        Handles.color = m_instance.controlCol;
                    }

                    switch (handleType)
                    {
                        case PathCreator.HandleType.POSITION:
                            newPos = Handles.PositionHandle(TransformPoint(path[i]), Quaternion.identity);
                            break;
                        case PathCreator.HandleType.FREE_MOVE:
                            newPos = Handles.FreeMoveHandle(TransformPoint(path[i]), m_instance.controlDiameter, Vector3.zero, Handles.CylinderHandleCap);
                            break;
                    }

                    newPos = InverseTransformPoint(newPos);

                    if (path[i] != newPos)
                    {
                        Undo.RecordObject(m_instance, "Move point");
                        path.MovePoint(i, newPos);
                    }
                }
            }
        }

        private void OnEnable()
        {
            m_instance = (PathCreator)target;

            if (m_instance.path == null)
            {
                m_instance.CreatePath();
            }
        }
    }
}
