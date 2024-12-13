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

        public static Vector3[] Line(Vector3 center, float size = 1f)
        {
            size *= 0.5f;

            var points = new Queue<Vector3>();

            points.Enqueue(center + Vector3.left * size);
            points.Enqueue(center + Vector3.left * size);
            points.Enqueue(center + Vector3.left * size * -1);
            points.Enqueue(center + Vector3.left * size * -1);

            return points.ToArray();
        }

        public static Vector3[] Circle(Vector3 center, int numPoints, float radius = 1.0f)
        {
            var points = new Queue<Vector3>();
            var controlSize = Mathf.Tan(Mathf.PI / (2 * numPoints)) * 4 / 3 * radius;
            var angleOffset = Mathf.PI / 2 + ((numPoints % 2 == 0) ? Mathf.PI / numPoints : 0);
            for (var i = 0; i < numPoints; i++)
            {
                var theta = Mathf.PI * 2 * i / numPoints + angleOffset;
                var cos = Mathf.Cos(theta);
                var sin = Mathf.Sin(theta);

                var anchor = center + new Vector3(cos, 0, sin) * radius * -1;

                points.Enqueue(anchor + new Vector3(sin, 0, -cos) * controlSize * -1);
                points.Enqueue(anchor);
                points.Enqueue(anchor + new Vector3(sin, 0, -cos) * controlSize);
            }
            var head = points.Dequeue();
            points.Enqueue(head);

            return points.ToArray();
        }

        public static Vector3[] Polygon(Vector3 center, int numPoints, float radius = 1f)
        {
            var points = new Queue<Vector3>();
            var angleOffset = Mathf.PI / 2 + ((numPoints % 2 == 0) ? Mathf.PI / numPoints : 0);
            for (var i = 0; i < numPoints; i++)
            {
                var theta = Mathf.PI * 2 * i / numPoints + angleOffset;

                var anchor = center + new Vector3(Mathf.Cos(theta), 0, Mathf.Sin(theta)) * radius;

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
