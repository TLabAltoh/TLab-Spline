using System.Collections.Generic;
using UnityEngine;

namespace TLab.Spline
{
    public static class Primitive
    {
        public enum PrimitiveType
        {
            Line,
            Circle,
            Polygon,
        }

        public static Vector3[] Line(int numSegments, float size = 1f)
        {
            size *= 0.5f;

            var points = new Queue<Vector3>();

            var delta = size / numSegments;

            for (int i = 0; i <= numSegments; i++)
            {
                var anchor = Vector3.left * (size * -0.5f + delta * i);
                points.Enqueue(anchor);
                points.Enqueue(anchor);

                if ((i > 0) && (i < numSegments))
                    points.Enqueue(anchor);
            }

            return points.ToArray();
        }

        public static Vector3[] Circle(int numSegments, float radius = 1.0f)
        {
            var points = new Queue<Vector3>();
            var controlSize = Mathf.Tan(Mathf.PI / (2 * numSegments)) * 4 / 3 * radius;
            var angleOffset = Mathf.PI / 2 + ((numSegments % 2 == 0) ? Mathf.PI / numSegments : 0);
            for (var i = 0; i < numSegments; i++)
            {
                var theta = Mathf.PI * 2 * i / numSegments + angleOffset;
                var cos = Mathf.Cos(theta);
                var sin = Mathf.Sin(theta);

                var anchor = new Vector3(cos, 0, sin) * radius * -1;

                points.Enqueue(anchor + new Vector3(sin, 0, -cos) * controlSize * -1);
                points.Enqueue(anchor);
                points.Enqueue(anchor + new Vector3(sin, 0, -cos) * controlSize);
            }
            var head = points.Dequeue();
            points.Enqueue(head);

            return points.ToArray();
        }

        public static Vector3[] Polygon(int numSegments, float radius = 1f)
        {
            var points = new Queue<Vector3>();
            var angleOffset = Mathf.PI / 2 + ((numSegments % 2 == 0) ? Mathf.PI / numSegments : 0);
            for (var i = 0; i < numSegments; i++)
            {
                var theta = Mathf.PI * 2 * i / numSegments + angleOffset;

                var anchor = new Vector3(Mathf.Cos(theta), 0, Mathf.Sin(theta)) * radius;

                points.Enqueue(anchor);
                points.Enqueue(anchor);
                points.Enqueue(anchor);
            }

            var head = points.Dequeue();
            points.Enqueue(head);

            return points.ToArray();
        }
    }
}
