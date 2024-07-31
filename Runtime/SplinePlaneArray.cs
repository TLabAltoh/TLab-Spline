using UnityEngine;

namespace TLab.Spline
{
    public class SplinePlaneArray : MonoBehaviour
    {
        [SerializeField] protected Spline m_spline;
        public bool autoUpdate;

        public enum ArrayMode
        {
            DEFAULT,
            NO_SPACE
        };

        [Header("Array")]
        [SerializeField] protected ArrayMode m_arrayMode;
        [SerializeField] protected bool m_zUp = true;
        [SerializeField] protected float m_space = 0.5f;
        [SerializeField] protected Vector3 m_size = new Vector3(1.0f, 1.0f, 1.0f);

        [Header("Gizmo")]
        [SerializeField] public bool drawGizmo = false;

        protected Material m_gizmoMat;

        public Spline spline
        {
            get => m_spline;
            set
            {
                if (m_spline != value)
                {
                    m_spline = value;

                    RequestAutoUpdate();
                }
            }
        }

        public ArrayMode arrayMode
        {
            get => m_arrayMode;
            set
            {
                if (m_arrayMode != value)
                {
                    m_arrayMode = value;

                    RequestAutoUpdate();
                }
            }
        }

        public bool zUp
        {
            get => m_zUp;
            set
            {
                if (m_zUp != value)
                {
                    m_zUp = value;

                    RequestAutoUpdate();
                }
            }
        }

        public float space
        {
            get => m_space;
            set
            {
                if (m_space != value)
                {
                    m_space = value;

                    RequestAutoUpdate();
                }
            }
        }

        public Vector3 size
        {
            get => m_size;
            set
            {
                if (m_size != value)
                {
                    m_size = value;

                    RequestAutoUpdate();
                }
            }
        }

        private string THIS_NAME => "[" + this.GetType() + "] ";

        protected virtual void GeneratePlaneAlongToSpline(Vector3[] points, bool isClosed, ArrayMode arrayMode, out Vector3[] verts, out Vector2[] uvs, out int[] tris)
        {
            switch (arrayMode)
            {
                case ArrayMode.NO_SPACE:
                    {
                        verts = new Vector3[points.Length * 2];
                        uvs = new Vector2[verts.Length];

                        var numTris = (points.Length - 1) + (isClosed ? 2 : 0);
                        tris = new int[2 * numTris * 3];

                        var vertIndex = 0;
                        var triIndex = 0;

                        for (int i = 0; i < points.Length; i++)
                        {
                            var forward = Vector3.zero;

                            if (i < points.Length - 1 || isClosed)  // Neighboring forward
                                forward += points[(i + 1) % points.Length] - points[i];

                            if (i > 0 || isClosed)  // Neighboring backward
                                forward += points[i] - points[(i - 1 + points.Length) % points.Length];

                            forward.Normalize();

                            // Get z-up vector
                            var left = new Vector3(-forward.z, m_zUp ? 0.0f : forward.y, forward.x);

                            var m_offset = left * m_size.x;
                            verts[vertIndex + 0] = points[i] + m_offset;
                            verts[vertIndex + 1] = points[i] - m_offset;

                            /*
                             *    2 -----ÅE----- 3
                             *    
                             *    
                             *    0 -----ÅE----- 1
                             */

                            if (i < points.Length - 1 || isClosed)
                            {
                                tris[triIndex + 0] = (vertIndex + 0) % verts.Length;
                                tris[triIndex + 1] = (vertIndex + 2) % verts.Length;
                                tris[triIndex + 2] = (vertIndex + 1) % verts.Length;

                                tris[triIndex + 3] = (vertIndex + 1) % verts.Length;
                                tris[triIndex + 4] = (vertIndex + 2) % verts.Length;
                                tris[triIndex + 5] = (vertIndex + 3) % verts.Length;
                            }

                            var completinPercent = i / (float)points.Length;
                            var v = 1 - Mathf.Abs(2 * completinPercent - 1);
                            uvs[vertIndex + 0] = new Vector2(0, v);
                            uvs[vertIndex + 1] = new Vector2(1, v);

                            vertIndex += 2;
                            triIndex += 6;
                        }
                    }
                    break;
                default:
                    {
                        var numArray = (points.Length + (isClosed ? 0 : -1));
                        verts = new Vector3[numArray * 4];
                        uvs = new Vector2[verts.Length];
                        tris = new int[2 * numArray * 3];

                        var vertIndex = 0;
                        var triIndex = 0;

                        for (int i = 0; i < numArray; i++)
                        {
                            var forward = points[(i + 1) % points.Length] - points[i];
                            forward.Normalize();

                            var left = new Vector3(-forward.z, m_zUp ? 0.0f : forward.y, forward.x);

                            var offset = left * m_size.x;
                            verts[vertIndex + 0] = points[i] + offset;
                            verts[vertIndex + 1] = points[i] - offset;
                            verts[vertIndex + 2] = points[(i + 1) % points.Length] + offset;
                            verts[vertIndex + 3] = points[(i + 1) % points.Length] - offset;

                            if (i < points.Length - 1 || isClosed)
                            {
                                tris[triIndex + 0] = (vertIndex + 0) % verts.Length;
                                tris[triIndex + 1] = (vertIndex + 2) % verts.Length;
                                tris[triIndex + 2] = (vertIndex + 1) % verts.Length;

                                tris[triIndex + 3] = (vertIndex + 1) % verts.Length;
                                tris[triIndex + 4] = (vertIndex + 2) % verts.Length;
                                tris[triIndex + 5] = (vertIndex + 3) % verts.Length;
                            }

                            var completinPercent = i / (float)points.Length;
                            var v = 1 - Mathf.Abs(2 * completinPercent - 1);
                            uvs[vertIndex + 0] = new Vector2(0, v);
                            uvs[vertIndex + 1] = new Vector2(1, v);

                            completinPercent = (i + 1) % numArray / (float)points.Length;
                            v = 1 - Mathf.Abs(2 * completinPercent - 1);
                            uvs[vertIndex + 0] = new Vector2(0, v);
                            uvs[vertIndex + 1] = new Vector2(1, v);

                            vertIndex += 4;
                            triIndex += 6;
                        }
                    }
                    break;
            }
        }

        protected virtual Mesh GeneratePlaneMeshAlongToSpline(Vector3[] points, bool isClosed, ArrayMode arrayMode)
        {
            GeneratePlaneAlongToSpline(points, isClosed, arrayMode, out var verts, out var uvs, out var tris);
            var mesh = new Mesh();
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;

            mesh.RecalculateNormals();

            return mesh;
        }

        public virtual void RequestAutoUpdate()
        {
            if (autoUpdate)
            {
                UpdateWithCurrentSpline();
            }
        }

        public virtual void UpdateWithCurrentSpline()
        {

        }

        protected void OnDrawGizmos()
        {
            if (!drawGizmo || !m_spline)
                return;

            if (m_gizmoMat == null)
                m_gizmoMat = new Material(Shader.Find("Unlit/Color"));

            m_gizmoMat.SetColor("_Color", Color.green);

            if (spline.CalculateEvenlySpacedPoints(out var points, m_space))
            {
                GeneratePlaneAlongToSpline(points, m_spline.isClosed, m_arrayMode, out var verts, out var uvs, out var tris);

                m_gizmoMat.SetPass(0);

                GL.PushMatrix();

                for (int i = 0; i < tris.Length; i += 3)
                {
                    var corners = new Vector3[3];
                    corners[0] = verts[tris[i + 0]];
                    corners[1] = verts[tris[i + 1]];
                    corners[2] = verts[tris[i + 2]];

                    transform.TransformPoints(corners, corners);

                    GL.Begin(GL.LINES);
                    GL.Vertex3(corners[0].x, corners[0].y, corners[0].z);
                    GL.Vertex3(corners[1].x, corners[1].y, corners[1].z);
                    GL.End();

                    GL.Begin(GL.LINES);
                    GL.Vertex3(corners[1].x, corners[1].y, corners[1].z);
                    GL.Vertex3(corners[2].x, corners[2].y, corners[2].z);
                    GL.End();

                    GL.Begin(GL.LINES);
                    GL.Vertex3(corners[2].x, corners[2].y, corners[2].z);
                    GL.Vertex3(corners[0].x, corners[0].y, corners[0].z);
                    GL.End();
                }

                GL.PopMatrix();
            }
        }
    }
}
