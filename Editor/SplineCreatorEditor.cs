using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace TLab.Spline.Editor
{
    [CustomEditor(typeof(SplineCreator))]
    public class SplineCreatorEditor : UnityEditor.Editor
    {
        private SplineCreator m_instance;

        Spline spline => m_instance.spline;

        ArcHandle anchorAngleHandle = new ArcHandle();

        private const float SEGMENT_SELECT_DISTANCE_THRESHOLD = 20.0f;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUI.BeginChangeCheck();

            if (GUILayout.Button("Create New"))
            {
                var spline = m_instance.GetComponent<Spline>();
                Undo.RecordObject(spline, "Create New");
                m_instance.CreatePath();
            }

            var close = GUILayout.Toggle(spline.close, "Close");
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
            {
                SceneView.RepaintAll();
            }
        }

        private void OnSceneGUI()
        {
            Input();
            Draw();
        }

        private void Input()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.A && !spline.close)
                {
                    // Add Segment

                    var mousePos = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                    RaycastHit hit;

                    if (Physics.Raycast(mousePos, out hit, 100.0f))
                    {
                        Undo.RecordObject(spline, "Add Segment");
                        spline.AddSegment(hit.point);
                    }
                }

                if (Event.current.keyCode == KeyCode.D && !(spline.numSegments == 3 && spline.close))
                {
                    // Delete Segment

                    var minDstToAnchor = 20.0f;
                    var closestAnchorIndex = -1;

                    for (int i = 0; i < spline.numPoints; i += 3)
                    {
                        var camera = SceneView.lastActiveSceneView.camera;
                        var mousePos = Event.current.mousePosition;

                        var mousePosOnScene = new Vector2(mousePos.x, camera.pixelHeight - mousePos.y);
                        var splineScreenPos = camera.WorldToScreenPoint(spline.GetPoint(i));
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

                if (Event.current.keyCode == KeyCode.S)
                {
                    // Split Segment

                    var camera = SceneView.lastActiveSceneView.camera;
                    var mousePos = Event.current.mousePosition;

                    var minDstToSegment = SEGMENT_SELECT_DISTANCE_THRESHOLD;
                    var selectedSegmentIndex = -1;

                    var lerpValue = -1.0f;

                    var mousePosOnScene = new Vector2(mousePos.x, camera.pixelHeight - mousePos.y);

                    for (int i = 0; i < spline.numSegments; i++)
                    {
                        var points = spline.GetPointInSegment(i);

                        for (float j = 0.0f; j < 1.0f; j += 0.01f)
                        {
                            var bezierPos = Bezier.EvaluateCubic(points[0], points[1], points[2], points[3], j);
                            var bezierPosOnScene = camera.WorldToScreenPoint(bezierPos);

                            var dst = Vector2.Distance(mousePosOnScene, new Vector2(bezierPosOnScene.x, bezierPosOnScene.y));

                            if (dst < minDstToSegment)
                            {
                                minDstToSegment = dst;
                                selectedSegmentIndex = i;
                                lerpValue = j;
                            }
                        }
                    }

                    if (selectedSegmentIndex != -1)
                    {
                        var points = spline.GetPointInSegment(selectedSegmentIndex);
                        var bezierPos = Bezier.EvaluateCubic(points[0], points[1], points[2], points[3], lerpValue);
                        Undo.RecordObject(spline, "Split Segment");
                        spline.SplitSegment(bezierPos, selectedSegmentIndex);
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

                if (m_instance.displaySetting.control)
                {
                    Handles.DrawLine(points[1], points[0]);
                    Handles.DrawLine(points[2], points[3]);
                }

                Handles.DrawBezier(points[0], points[3], points[1], points[2], m_instance.displaySetting.segmentColor, null, 2);
            }

            Handles.color = Color.red;

            for (int i = 0; i < spline.numPoints; i++)
            {
                var showAnchorHandle = (i % 3 == 0) && m_instance.displaySetting.anchor;
                var showControlHandle = (i % 3 != 0) && m_instance.displaySetting.control;

                if (showAnchorHandle || showControlHandle)
                {
                    var newPos = spline.GetPoint(i);

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
                            newPos = Handles.PositionHandle(spline.GetPoint(i), Quaternion.identity);
                            break;
                        case SplineCreator.HandleType.FreeMove:
                            newPos = Handles.FreeMoveHandle(spline.GetPoint(i), handleSize, Vector3.zero, Handles.CylinderHandleCap);
                            break;
                    }

                    if (spline.GetPoint(i) != newPos)
                    {
                        Undo.RecordObject(spline, "Move Point");
                        spline.MovePoint(i, newPos);
                    }
                }

                var showAngleHandle = (i % 3 == 0) && m_instance.displaySetting.angle;

                if (showAngleHandle)
                {
                    anchorAngleHandle.radius = m_instance.angle.radius * 3;
                    anchorAngleHandle.angle = spline.GetAngle(i / 3);
                    anchorAngleHandle.fillColor = m_instance.angle.color;

                    var handlePosition = spline.GetPoint(i);

                    var dir = (spline.GetPoint((i == spline.numPoints - 1) ? i - 1 : i + 1) - handlePosition).normalized;
                    if (dir.magnitude == 0)
                    {
                        if ((i != 0) && (i != spline.numPoints - 1))
                        {
                            dir = (handlePosition - spline.GetPoint(i - 2)).normalized;
                            dir = ((spline.GetPoint(i + 2) - handlePosition).normalized + dir).normalized;
                        }
                        else
                            dir = (spline.GetPoint((i == spline.numPoints - 1) ? i - 2 : i + 2) - handlePosition).normalized;
                    }

                    var handleRotOffset = 0;

                    var handleDirection = Vector3.Cross(dir, Vector3.up);
                    var handleMatrix = Matrix4x4.TRS(handlePosition, Quaternion.LookRotation(handleDirection, dir), Vector3.one);

                    using (new Handles.DrawingScope(handleMatrix))
                    {
                        EditorGUI.BeginChangeCheck();
                        anchorAngleHandle.DrawHandle();
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(spline, "Set Angle");
                            spline.RotateAngle(i / 3, anchorAngleHandle.angle - handleRotOffset);
                        }
                    }
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
