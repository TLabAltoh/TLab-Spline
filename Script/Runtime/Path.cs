using System.Collections.Generic;
using UnityEngine;

namespace TLab.CurveTool
{

    [System.Serializable]
    public class Path
    {
        [SerializeField, HideInInspector] private List<Vector3> m_points;
        [SerializeField, HideInInspector] private bool m_isClosed;
        [SerializeField, HideInInspector] private bool m_autoSetControlPoints;

        public string THIS_NAME => "[" + this.GetType() + "] ";

        public Vector3 this[int i]
        {
            get
            {
                return m_points[i];
            }
        }

        public int NumPoints
        {
            get
            {
                return m_points.Count;
            }
        }

        public int NumSegments
        {
            get
            {
                return m_points.Count / 3;
            }
        }

        public bool IsClosed
        {
            get
            {
                return m_isClosed;
            }
            set
            {
                if (m_isClosed != value)
                {
                    m_isClosed = value;

                    if (m_isClosed)
                    {
                        // anchor point added at the path end
                        m_points.Add(m_points[m_points.Count - 1] * 2 - m_points[m_points.Count - 2]);

                        // add anchor point at the path beginning
                        m_points.Add(m_points[0] * 2 - m_points[1]);

                        if (m_autoSetControlPoints)
                        {
                            AutoSetAnchorControlPoints(0);
                            AutoSetAnchorControlPoints(m_points.Count - 3);
                        }
                    }
                    else
                    {
                        // remove anchor point at path end and begining.
                        m_points.RemoveRange(m_points.Count - 2, 2);

                        if (m_autoSetControlPoints)
                        {
                            AutoSetStartAndEndControls();
                        }
                    }
                }
            }
        }

        public bool AutoSetControlPoints
        {
            get
            {
                return m_autoSetControlPoints;
            }
            set
            {
                if (m_autoSetControlPoints != value)
                {
                    m_autoSetControlPoints = value;

                    if (m_autoSetControlPoints)
                    {
                        AutoSetAllControlPoints();
                    }
                }
            }
        }

        public Path(Vector3 center)
        {
            m_points = new List<Vector3>
            {
                center + Vector3.left,
                center + (Vector3.left + Vector3.forward) * 0.5f,
                center + (Vector3.right - Vector3.forward) * 0.5f,
                center + Vector3.right
            };
        }

        public Path(List<Vector3> points)
        {
            m_points = points;
        }

        public Path(Path path)
        {
            m_points = new List<Vector3>(path.m_points);
            m_isClosed = path.m_isClosed;
            m_autoSetControlPoints = path.m_autoSetControlPoints;
        }

        public void AddSegment(Vector3 anchorPos)
        {
            /*
             * start, backward;
             * offset = start - backward;
             * forward = start + offset;
             *         = start * (start - backward);
             *         = start * 2 - backward;
             */

            m_points.Add(m_points[m_points.Count - 1] * 2 - m_points[m_points.Count - 2]);
            m_points.Add((m_points[m_points.Count - 1] + anchorPos) * 0.5f);
            m_points.Add(anchorPos);

            if (m_autoSetControlPoints)
            {
                AutoSetAllAffectedControlPoints(m_points.Count - 1);
            }
        }

        public void SplitSegment(Vector3 anchorPos, int segmentIndex)
        {
            m_points.InsertRange(segmentIndex * 3 + 2, new Vector3[] { Vector3.zero, anchorPos, Vector3.zero });

            if (m_autoSetControlPoints)
            {
                AutoSetAllAffectedControlPoints(segmentIndex * 3 + 3);
            }
            else
            {
                AutoSetAnchorControlPoints(segmentIndex * 3 + 3);
            }
        }

        public void DeleteSegment(int anchorIndex)
        {
            if (NumSegments > 2 || !m_isClosed && NumSegments > 1)
            {
                if (anchorIndex == 0)
                {
                    if (m_isClosed)
                    {
                        m_points[m_points.Count - 1] = m_points[2];
                        m_points.RemoveRange(0, 3);
                    }
                }
                else if (anchorIndex == m_points.Count - 1 && !m_isClosed)
                {
                    m_points.RemoveRange(anchorIndex - 2, 3);
                }
                else
                {
                    m_points.RemoveRange(anchorIndex - 1, 3);
                }
            }

            if (m_autoSetControlPoints)
            {
                AutoSetAllAffectedControlPoints(anchorIndex);
            }
        }

        public Vector3[] GetPointInSegment(int i)
        {
            return new Vector3[]
            {
                m_points[i * 3 + 0],
                m_points[i * 3 + 1],
                m_points[i * 3 + 2],
                m_points[LoopIndex(i * 3 + 3)]
            };
        }

        public void MovePoint(int i, Vector3 pos)
        {
            if (i % 3 == 0 || !m_autoSetControlPoints)
            {
                var deltaMove = pos - m_points[i];

                m_points[i] = pos;

                if (m_autoSetControlPoints)
                {
                    AutoSetAllAffectedControlPoints(i);
                }
                else
                {
                    if (i % 3 == 0)
                    {
                        // if is this path point, move anchor m_points with same offset.

                        if (i + 1 < m_points.Count || m_isClosed)
                        {
                            m_points[LoopIndex(i + 1)] += deltaMove;
                        }

                        if (i - 1 > -1 || m_isClosed)
                        {
                            m_points[LoopIndex(i - 1)] += deltaMove;
                        }
                    }
                    else
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

                        if (correspondingControlIndex > -1 && correspondingControlIndex < m_points.Count || m_isClosed)
                        {
                            var dst = (m_points[LoopIndex(anchorIndex)] - m_points[LoopIndex(correspondingControlIndex)]).magnitude;
                            var dir = (m_points[LoopIndex(anchorIndex)] - pos).normalized;
                            m_points[LoopIndex(correspondingControlIndex)] = m_points[LoopIndex(anchorIndex)] + dir * dst;
                        }
                    }
                }
            }
        }

        private int LoopIndex(int i)
        {
            return (i + m_points.Count) % m_points.Count;
        }

        public bool CalculateEvenlySpacedPoints(out Vector3[] spacedPoints, float spacing, float resolution = 1.0f)
        {
            if (m_points.Count <= 0)
            {
                Debug.LogError(THIS_NAME + $"Point's Length is {m_points.Count}");

                spacedPoints = new Vector3[1];

                return false;
            }

            var evenlySpacedPoints = new List<Vector3>();
            evenlySpacedPoints.Add(m_points[0]);
            var previousPoint = m_points[0];
            var dstSinceLastEvenPoint = 0.0f;

            for (int segmentIndex = 0; segmentIndex < NumSegments; segmentIndex++)
            {
                var p = GetPointInSegment(segmentIndex);

                var controlNetLength = Vector3.Distance(p[0], p[1]) + Vector3.Distance(p[1], p[2]) + Vector3.Distance(p[2], p[3]);
                var estimatedCurveLength = Vector3.Distance(p[0], p[3]) + controlNetLength / 2.0f;
                var divisions = Mathf.CeilToInt(estimatedCurveLength * resolution * 10);

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
                        dstSinceLastEvenPoint = overshootDst;
                        previousPoint = newEvenlySpacedPoint;
                    }

                    previousPoint = pointOnCurve;
                }
            }

            spacedPoints = evenlySpacedPoints.ToArray();

            return true;
        }

        private void AutoSetAllAffectedControlPoints(int updateAnchorIndex)
        {
            for (int i = updateAnchorIndex - 3; i < updateAnchorIndex + 4; i += 3)
            {
                if (i > -1 && i < m_points.Count || m_isClosed)
                {
                    AutoSetAnchorControlPoints(LoopIndex(i));
                }
            }
        }

        private void AutoSetAllControlPoints()
        {
            for (int i = 0; i < m_points.Count; i += 3)
            {
                AutoSetAnchorControlPoints(i);
            }
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
            if (anchorIndex - 3 > -1 || m_isClosed)
            {
                var offset = m_points[LoopIndex(anchorIndex - 3)] - anchorPos;
                dir += offset.normalized;
                neighbourDistance[0] = offset.magnitude;
            }

            // if neighbour exist or close enabled
            if (anchorIndex + 3 < m_points.Count || m_isClosed)
            {
                var offset = m_points[LoopIndex(anchorIndex + 3)] - anchorPos;
                dir -= offset.normalized;
                neighbourDistance[1] = -offset.magnitude;
            }

            dir.Normalize();

            for (int i = 0; i < 2; i++)
            {
                var controlIndex = anchorIndex + i * 2 - 1;

                if (controlIndex > -1 && controlIndex < m_points.Count || m_isClosed)
                {
                    m_points[LoopIndex(controlIndex)] = anchorPos + dir * neighbourDistance[i] * 0.5f;
                }
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
             * l = m_points.count
             */

            m_points[1] = (m_points[0] + m_points[3]) * 0.5f;
            m_points[m_points.Count - 2] = (m_points[m_points.Count - 1] + m_points[m_points.Count - 4]) * 0.5f;
        }
    }
}