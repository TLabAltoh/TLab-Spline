using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TLab.Spline
{
    public class Spline : MonoBehaviour
    {
        [System.Serializable]
        public class InitOption
        {
            public Primitive.PrimitiveType primitive;
            [Min(0)] public float size = 1f;
            [Min(2)] public int numSegments = 5;

            public InitOption() { }

            public InitOption(Primitive.PrimitiveType primitive, float size, int numSegments)
            {
                this.primitive = primitive;
                this.size = size;
                this.numSegments = numSegments;
            }
        }

        [SerializeField, HideInInspector] private List<Vector3> m_points;
        [SerializeField, HideInInspector] private List<float> m_angles;

        [SerializeField, HideInInspector] private bool m_close;
        [SerializeField, HideInInspector] private EditMode m_editMode = EditMode.Free;

        public enum EditMode
        {
            Free,
            Tangent,
            AutoSetControlPoints,
        }

        public enum AnchorAxis
        {
            Default,
            Transform,
        };

        public string THIS_NAME => "[" + this.GetType() + "] ";

        public int numPoints => m_points.Count;

        public int numAngles => m_angles.Count;

        public int numSegments => numPoints / 3;

        public int numAnglesFoAllocation => (numPoints + (m_close ? 0 : 2)) / 3;

        public bool close
        {
            get => m_close;
            set
            {
                if (m_close != value)
                {
                    m_close = value;

                    if (m_close)
                    {
                        if (numPoints % 3 != 0)
                        {
                            // anchor point added at the path end
                            m_points.Add(m_points[numPoints - 1] * 2 - m_points[numPoints - 2]);

                            // add anchor point at the path beginning
                            m_points.Add(m_points[0] * 2 - m_points[1]);
                        }

                        if (m_editMode == EditMode.AutoSetControlPoints)
                        {
                            AutoSetAnchorControlPoints(0);
                            AutoSetAnchorControlPoints(numPoints - 3);
                        }
                    }
                    else
                    {
                        if (numPoints % 3 == 0)
                        {
                            // remove anchor point at path end and begining.
                            m_points.RemoveRange(numPoints - 2, 2);
                        }

                        if (m_editMode == EditMode.AutoSetControlPoints)
                            AutoSetStartAndEndControls();
                    }
                }
            }
        }

        public EditMode editMode
        {
            get => m_editMode;
            set
            {
                if (m_editMode != value)
                {
                    m_editMode = value;

                    if (m_editMode == EditMode.AutoSetControlPoints)
                        AutoSetAllControlPoints();
                }
            }
        }

        public void Init(InitOption init)
        {
            switch (init.primitive)
            {
                case Primitive.PrimitiveType.Line:
                    m_close = false;
                    m_points = Primitive.Line(init.numSegments, init.size).ToList();
                    break;
                case Primitive.PrimitiveType.Circle:
                    m_close = true;
                    m_points = Primitive.Circle(init.numSegments, init.size * 0.5f).ToList();
                    break;
                case Primitive.PrimitiveType.Polygon:
                    m_close = true;
                    m_points = Primitive.Polygon(init.numSegments, init.size * 0.5f).ToList();
                    break;
            }

            m_angles = new List<float>(new float[numAnglesFoAllocation]);
        }

        public void Init(List<Vector3> points, bool close)
        {
            m_points = new List<Vector3>(points);
            m_angles = new List<float>(new float[numAnglesFoAllocation]);

            m_close = close;
        }

        public void Init(List<Vector3> points, List<float> angles, bool close)
        {
            m_points = new List<Vector3>(points);
            m_angles = new List<float>(angles);

            this.close = close;
        }

        public void Init(Spline path)
        {
            m_points = new List<Vector3>(path.m_points);
            m_angles = new List<float>(path.m_angles);
            m_editMode = path.m_editMode;

            this.close = path.m_close;
        }

        private int LoopIndexPoint(int i) => (i + numPoints) % numPoints;
        private int LoopIndexAngle(int i) => (i + numAngles) % numAngles;

        private Vector3 Local2World(Vector3 localPos)
        {
            var lossyScale = this.transform.lossyScale;
            var scaledLocalPos = localPos;
            scaledLocalPos.x *= lossyScale.x;
            scaledLocalPos.y *= lossyScale.y;
            scaledLocalPos.z *= lossyScale.z;
            return this.transform.TransformPoint(scaledLocalPos);
        }

        private Vector3 Local2WorldScale(Vector3 localPos)
        {
            var lossyScale = this.transform.lossyScale;
            var scaledLocalPos = localPos;
            scaledLocalPos.x *= lossyScale.x;
            scaledLocalPos.y *= lossyScale.y;
            scaledLocalPos.z *= lossyScale.z;
            return scaledLocalPos;
        }

        private Vector3 World2Local(Vector3 worldPos)
        {
            var lossyScale = this.transform.lossyScale;
            var scaledLocalPos = this.transform.InverseTransformPoint(worldPos);
            scaledLocalPos.x /= lossyScale.x;
            scaledLocalPos.y /= lossyScale.y;
            scaledLocalPos.z /= lossyScale.z;
            var localPos = scaledLocalPos;
            return localPos;
        }

        public float GetAngle(int i) => m_angles[i];

        public Vector3 GetPoint(int i) => Local2World(m_points[i]);

        public Vector3[] GetPointInSegment(int i)
        {
            return new Vector3[]
            {
                GetPoint(i * 3 + 0),
                GetPoint(i * 3 + 1),
                GetPoint(i * 3 + 2),
                GetPoint(LoopIndexPoint(i * 3 + 3))
            };
        }

        public void AddSegment(Vector3 newAnchorPos)
        {
            /*
             * start, backward;
             * offset = start - backward;
             * forward = start + offset;
             *         = start * (start - backward);
             *         = start * 2 - backward;
             */

            newAnchorPos = World2Local(newAnchorPos);

            var point0 = m_points[numPoints - 1];
            var point1 = m_points[numPoints - 2];
            var control0 = point0 * 2 - point1;
            var control1 = 0.5f * (control0 + newAnchorPos);

            m_points.Add(control0);
            m_points.Add(control1);
            m_points.Add(newAnchorPos);

            var angle0 = m_angles[numAngles - 1];
            var angle1 = m_angles[numAngles - 2];
            var angle2 = angle0 * 2 - angle1;

            m_angles.Add(angle2);

            if (m_editMode == EditMode.AutoSetControlPoints)
                AutoSetAllAffectedControlPoints(numPoints - 1);
        }

        public void SplitSegment(Vector3 newAnchorPos, int segmentIndex)
        {
            m_points.InsertRange(segmentIndex * 3 + 2, new Vector3[] { Vector3.zero, World2Local(newAnchorPos), Vector3.zero });

            m_angles.Insert(segmentIndex + 1, m_angles[segmentIndex] * 2 - m_angles[segmentIndex + 1]);

            if (m_editMode == EditMode.AutoSetControlPoints)
                AutoSetAllAffectedControlPoints(segmentIndex * 3 + 3);
            else
                AutoSetAnchorControlPoints(segmentIndex * 3 + 3);
        }

        public void DeleteSegment(int anchorIndex)
        {
            if (numSegments > 2 || !m_close && numSegments > 1)
            {
                if (anchorIndex == 0)
                {
                    if (m_close)
                    {
                        m_points[numPoints - 1] = m_points[2];
                        m_points.RemoveRange(0, 3);
                    }
                }
                else if (anchorIndex == numPoints - 1 && !m_close)
                    m_points.RemoveRange(anchorIndex - 2, 3);
                else
                    m_points.RemoveRange(anchorIndex - 1, 3);
            }

            if (m_editMode == EditMode.AutoSetControlPoints)
                AutoSetAllAffectedControlPoints(anchorIndex);
        }

        public void RotateAngle(int i, float angle)
        {
            m_angles[i] = angle;
        }

        public void MovePoint(int i, Vector3 pos)
        {
            pos = World2Local(pos);

            if (i % 3 == 0 || !(m_editMode == EditMode.AutoSetControlPoints))
            {
                var deltaMove = pos - m_points[i];

                m_points[i] = pos;

                if (m_editMode == EditMode.AutoSetControlPoints)
                    AutoSetAllAffectedControlPoints(i);
                else
                {
                    if (i % 3 == 0)
                    {
                        // if is this path point, move anchor m_points with same offset.

                        if (i + 1 < numPoints || m_close)
                            m_points[LoopIndexPoint(i + 1)] += deltaMove;

                        if (i - 1 > -1 || m_close)
                            m_points[LoopIndexPoint(i - 1)] += deltaMove;
                    }
                    else
                    {
                        if (m_editMode == EditMode.Tangent)
                        {
                            /*      
                             *     control
                             *        |
                             *        v
                             *  acnhor 
                             *    |   
                             *    V   1          2
                             * 
                             *    0                   3
                             *   
                             * -1                        4  
                             */

                            var nextPointIsAnchor = (i + 1) % 3 == 0;
                            var correspondingControlIndex = nextPointIsAnchor ? i + 2 : i - 2;
                            var anchorIndex = nextPointIsAnchor ? i + 1 : i - 1;

                            if (correspondingControlIndex > -1 && correspondingControlIndex < numPoints || m_close)
                            {
                                var dst = (m_points[LoopIndexPoint(anchorIndex)] - m_points[LoopIndexPoint(correspondingControlIndex)]).magnitude;
                                var dir = (m_points[LoopIndexPoint(anchorIndex)] - pos).normalized;
                                m_points[LoopIndexPoint(correspondingControlIndex)] = m_points[LoopIndexPoint(anchorIndex)] + dir * dst;
                            }
                        }
                    }
                }
            }
        }

        public bool CalculateEvenlySpacedPointsAndAngles(out Vector3[] spacedPoints, out float[] spacedAngles, float spacing, float resolution = 1.0f)
        {
            if (numPoints <= 0)
            {
                Debug.LogWarning(THIS_NAME + $"Point's Length is {numPoints}");

                spacedPoints = new Vector3[1];
                spacedAngles = new float[1];

                return false;
            }

            var previousPoint = Local2World(m_points[0]);
            var previousAngle = m_angles[0];

            var evenlySpacedPoints = new List<Vector3>() { previousPoint };
            var evenlySpacedAngles = new List<float> { previousAngle };

            var dstSinceLastEvenPoint = 0.0f;

            for (int segmentIndex = 0; segmentIndex < numSegments; segmentIndex++)
            {
                var p = GetPointInSegment(segmentIndex);

                var previousPointInSegment = p[0];

                var controlNetLength = Vector3.Distance(p[0], p[1]) + Vector3.Distance(p[1], p[2]) + Vector3.Distance(p[2], p[3]);
                var estimatedCurveLength = Vector3.Distance(p[0], p[3]) + controlNetLength / 2.0f;
                var divisions = Mathf.CeilToInt(estimatedCurveLength * resolution * 10);

                var pointStartIndexInSegment = evenlySpacedPoints.Count - 1;
                var segmentLineLength = 0f;

                var t = 0.0f;

                while (t <= 1.0f)
                {
                    t += 1.0f / divisions;
                    var pointOnCurve = Bezier.EvaluateCubic(p[0], p[1], p[2], p[3], t);
                    dstSinceLastEvenPoint += Vector3.Distance(previousPoint, pointOnCurve);

                    while (dstSinceLastEvenPoint >= spacing)
                    {
                        var overshootDst = dstSinceLastEvenPoint - spacing;
                        var newEvenlySpacedPoint = pointOnCurve + (previousPoint - pointOnCurve).normalized * overshootDst;

                        evenlySpacedPoints.Add(newEvenlySpacedPoint);
                        segmentLineLength += Vector3.Distance(newEvenlySpacedPoint, previousPointInSegment);

                        dstSinceLastEvenPoint = overshootDst;
                        previousPoint = newEvenlySpacedPoint;
                        previousPointInSegment = newEvenlySpacedPoint;
                    }

                    previousPoint = pointOnCurve;
                }

                segmentLineLength += Vector3.Distance(previousPointInSegment, p[3]);

                var dstAngle = GetAngle(LoopIndexAngle(segmentIndex + 1));
                var deltaAngle = (dstAngle - previousAngle);

                var newEvenlySpacedAngle = previousAngle;

                previousPointInSegment = p[0];

                for (int i = pointStartIndexInSegment + 1; i < evenlySpacedPoints.Count; i++)
                {
                    newEvenlySpacedAngle += deltaAngle * (Vector3.Distance(previousPointInSegment, evenlySpacedPoints[i]) / segmentLineLength);

                    previousPointInSegment = evenlySpacedPoints[i];

                    evenlySpacedAngles.Add(newEvenlySpacedAngle);
                }

                previousAngle = dstAngle;
            }

            spacedPoints = evenlySpacedPoints.ToArray();
            spacedAngles = evenlySpacedAngles.ToArray();

            return true;
        }

        public struct Point
        {
            public Vector3 tangent;
            public Vector3 up;
            public Vector3 normal;
            public Vector3 position;
        }

        public bool GetSplinePoints(out Point[] splinePoints, AnchorAxis anchorAxis, bool zUp, float spacing, float resolution = 1.0f)
        {
            var splinePointList = new List<Point>();
            splinePoints = null;

            if (CalculateEvenlySpacedPointsAndAngles(out Vector3[] spacedPoints, out float[] spacedAngles, spacing, resolution))
            {
                var prevRotationAxis = anchorAxis == AnchorAxis.Default ? Vector3.up : transform.up;

                var tangent = (m_close ? (spacedPoints[1] - spacedPoints[0]) + (spacedPoints[0] - spacedPoints.Last()) : spacedPoints[1] - spacedPoints[0]).normalized;
                var normal = Vector3.Cross(prevRotationAxis, tangent).normalized;

                var prevTangent = tangent;

                normal = Quaternion.AngleAxis(spacedAngles[0], tangent) * normal;

                var up = Vector3.Cross(tangent, normal);

                splinePointList.Add(new Point
                {
                    tangent = tangent,
                    up = up,
                    normal = normal,
                    position = spacedPoints[0],
                });

                for (int i = 1; i < spacedPoints.Length; i++)
                {
                    var pos0 = spacedPoints[(i - 1 + spacedPoints.Length) % spacedPoints.Length];
                    var pos1 = spacedPoints[i];
                    var pos2 = spacedPoints[(i + 1) % spacedPoints.Length];

                    var forward = pos1 - pos0; // Neighboring backward

                    if (i < spacedPoints.Length - 1 || m_close)  // Neighboring forward
                        forward += pos2 - pos1;

                    tangent = forward.normalized;

                    normal = Vector3.Cross(Vector3.up, tangent).normalized;

                    if (!zUp)
                    {
                        var offset = pos2 - pos1;
                        var sqrDst = offset.sqrMagnitude;

                        var r = prevRotationAxis - offset * 2 / sqrDst * Vector3.Dot(offset, prevRotationAxis);
                        var t = prevTangent - offset * 2 / sqrDst * Vector3.Dot(offset, prevTangent);
                        var v2 = tangent - t;
                        var c2 = Vector3.Dot(v2, v2);

                        var rotationAxis = r - v2 * 2 / c2 * Vector3.Dot(v2, r);
                        prevRotationAxis = rotationAxis;

                        normal = Vector3.Cross(rotationAxis, tangent).normalized;
                    }

                    prevTangent = tangent;

                    normal = Quaternion.AngleAxis(spacedAngles[i], tangent) * normal;

                    up = Vector3.Cross(tangent, normal);

                    splinePointList.Add(new Point
                    {
                        tangent = tangent,
                        up = up,
                        normal = normal,
                        position = pos1,
                    });
                }

                splinePoints = splinePointList.ToArray();

                if (m_close)
                {
                    var upAngleErrorAcrossjoin = Vector3.SignedAngle(splinePoints.Last().up, splinePoints[0].up, splinePoints[0].tangent);
                    if (Mathf.Abs(upAngleErrorAcrossjoin) > 0.01f)
                    {
                        for (int i = 1; i < splinePoints.Length; i++)
                        {
                            var t = (float)i / splinePoints.Length;
                            var angle = upAngleErrorAcrossjoin * t;
                            var rot = Quaternion.AngleAxis(angle, splinePoints[i].tangent);

                            splinePoints[i].tangent = splinePointList[i].tangent;
                            splinePoints[i].up = rot * splinePointList[i].up;
                            splinePoints[i].normal = rot * splinePointList[i].normal;
                            splinePoints[i].position = splinePointList[i].position;
                        }
                    }
                }

                return true;
            }

            return false;
        }

        private void AutoSetAllAffectedControlPoints(int updateAnchorIndex)
        {
            for (int i = updateAnchorIndex - 3; i < updateAnchorIndex + 4; i += 3)
            {
                if (i > -1 && i < numPoints || m_close)
                    AutoSetAnchorControlPoints(LoopIndexPoint(i));
            }
        }

        private void AutoSetAllControlPoints()
        {
            for (int i = 0; i < numPoints; i += 3)
                AutoSetAnchorControlPoints(i);
        }

        private void AutoSetAnchorControlPoints(int anchorIndex)
        {
            /*      
             * target     
             *   |   +-1          +-2
             *   v
             *   
             *   0                   +-3
             */

            var anchorPos = m_points[anchorIndex];
            var dir = Vector3.zero;
            var neighbourDistance = new float[2];

            // if neighbour exist or close enabled
            if (anchorIndex - 3 > -1 || m_close)
            {
                var offset = m_points[LoopIndexPoint(anchorIndex - 3)] - anchorPos;
                dir += offset.normalized;
                neighbourDistance[0] = offset.magnitude;
            }

            // if neighbour exist or close enabled
            if (anchorIndex + 3 < numPoints || m_close)
            {
                var offset = m_points[LoopIndexPoint(anchorIndex + 3)] - anchorPos;
                dir -= offset.normalized;
                neighbourDistance[1] = -offset.magnitude;
            }

            dir.Normalize();

            for (int i = 0; i < 2; i++)
            {
                var controlIndex = anchorIndex + i * 2 - 1;

                if (controlIndex > -1 && controlIndex < numPoints || m_close)
                    m_points[LoopIndexPoint(controlIndex)] = anchorPos + dir * neighbourDistance[i] * 0.5f;
            }
        }

        private void AutoSetStartAndEndControls()
        {
            /*      
             * start control
             *      |
             *      v
             *      
             *      1          2
             *
             *  0                   3
             */

            /*              end control
             *                   |
             *                   v
             *      
             *     l-3          l-2
             *     
             * l-4                   l-1
             * 
             * l = numPoints
             */

            m_points[1] = (m_points[0] + m_points[3]) * 0.5f;
            m_points[numPoints - 2] = (m_points[numPoints - 1] + m_points[numPoints - 4]) * 0.5f;
        }

        private void OnEnable()
        {
            if (m_angles.Count != numAnglesFoAllocation)
                m_angles = new List<float>(new float[numAnglesFoAllocation]);
        }
    }
}