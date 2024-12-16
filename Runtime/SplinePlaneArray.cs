using UnityEngine;

namespace TLab.Spline
{
    public class SplinePlaneArray : MonoBehaviour
    {
        [SerializeField] protected Spline m_spline;
        public bool autoUpdate;

        public enum ArrayMode
        {
            Default,
            NoSpace,
        };

        [Header("Array")]
        [SerializeField] protected ArrayMode m_arrayMode;
        [SerializeField] protected Spline.AnchorAxis m_anchorAxis;
        [SerializeField] protected bool m_zUp = true;
        [SerializeField] protected uint m_skip = 0;
        [SerializeField] protected bool m_flipNormal = false;
        [SerializeField] protected bool m_flipUp = false;
        [SerializeField] protected bool m_flipTangent = false;
        [SerializeField, Min(0.5f)] protected float m_spacing = 0.5f;
        [SerializeField] protected float m_slideOffset = 0f;
        [SerializeField] protected Vector3 m_size = new Vector3(1.0f, 1.0f, 1.0f);
        [SerializeField] protected Vector2[] m_ranges = new Vector2[1] { new Vector2(0, 1) };

        [SerializeField] public GizmoSetting gizmoSetting = new GizmoSetting();

        [System.Serializable]
        public class GizmoSetting
        {
            public bool draw = false;
            public Color color = Color.green;

            public GizmoSetting() { }

            public GizmoSetting(bool draw, Color color)
            {
                this.draw = draw;
                this.color = color;
            }
        }

        internal static readonly int PROP_COLOR = Shader.PropertyToID("_Color");

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

        public Spline.AnchorAxis anchorAxis
        {
            get => m_anchorAxis;
            set
            {
                if (m_anchorAxis != value)
                {
                    m_anchorAxis = value;

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

        public uint skip
        {
            get => m_skip;
            set
            {
                if (m_skip != value)
                {
                    m_skip = value;

                    RequestAutoUpdate();
                }
            }
        }

        public bool flipNormal
        {
            get => m_flipNormal;
            set
            {
                if (m_flipNormal != value)
                {
                    m_flipNormal = value;

                    RequestAutoUpdate();
                }
            }
        }

        public bool flipUp
        {
            get => m_flipUp;
            set
            {
                if (m_flipUp != value)
                {
                    m_flipUp = value;

                    RequestAutoUpdate();
                }
            }
        }

        public bool flipTangent
        {
            get => m_flipTangent;
            set
            {
                if (m_flipTangent != value)
                {
                    m_flipTangent = value;

                    RequestAutoUpdate();
                }
            }
        }

        public float spacing
        {
            get => m_spacing;
            set
            {
                if (m_spacing != value)
                {
                    m_spacing = Mathf.Clamp(value, 1, float.MaxValue);

                    RequestAutoUpdate();
                }
            }
        }

        public float slideOffset
        {
            get => m_slideOffset;
            set
            {
                if (m_slideOffset != value)
                {
                    m_slideOffset = value;

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

        public Vector2[] ranges
        {
            get => m_ranges;
            set
            {
                if (m_ranges != value)
                {
                    m_ranges = value;

                    RequestAutoUpdate();
                }
            }
        }

        private string THIS_NAME => "[" + this.GetType() + "] ";

        protected virtual bool GeneratePlaneAlongToSpline(Spline.AnchorAxis anchorAxis, bool zUp, bool flipNormal, bool flipUp, bool flipTangent, float spacing, ArrayMode arrayMode, out Spline.Point[] splinePoints, out Vector3[] verts, out Vector2[] uvs, out int[] tris)
        {
            verts = null;
            uvs = null;
            tris = null;

            if (m_spline.GetSplinePoints(out splinePoints, anchorAxis, zUp, spacing))
            {
                if (flipNormal || flipUp || flipTangent)
                {
                    var flipX = m_flipNormal ? -1 : 1;
                    var flipY = m_flipUp ? -1 : 1;
                    var flipZ = m_flipTangent ? -1 : 1;
                    for (int i = 0; i < splinePoints.Length; i++)
                    {
                        var point = splinePoints[i];
                        point.normal *= flipX;
                        point.up *= flipY;
                        point.tangent *= flipZ;
                        splinePoints[i] = point;
                    }
                }

                switch (arrayMode)
                {
                    case ArrayMode.NoSpace:
                        {
                            verts = new Vector3[splinePoints.Length * 2];
                            uvs = new Vector2[verts.Length];

                            var numTris = (splinePoints.Length - 1) + (m_spline.close ? 1 : 0);
                            tris = new int[2 * numTris * 3];

                            var vertIndex = 0;
                            var triIndex = 0;

                            for (int i = 0; i < splinePoints.Length; i++)
                            {
                                var left = splinePoints[i].normal;

                                var offset = left * m_size.x;
                                verts[vertIndex + 0] = splinePoints[i].position + offset;
                                verts[vertIndex + 1] = splinePoints[i].position - offset;

                                /*
                                 *    2 -----E----- 3
                                 *    
                                 *    
                                 *    0 -----E----- 1
                                 */

                                if (i < splinePoints.Length - 1 || m_spline.close)
                                {
                                    tris[triIndex + 0] = (vertIndex + 1) % verts.Length;
                                    tris[triIndex + 1] = (vertIndex + 2) % verts.Length;
                                    tris[triIndex + 2] = (vertIndex + 0) % verts.Length;

                                    tris[triIndex + 3] = (vertIndex + 1) % verts.Length;
                                    tris[triIndex + 4] = (vertIndex + 3) % verts.Length;
                                    tris[triIndex + 5] = (vertIndex + 2) % verts.Length;
                                }

                                var completinPercent = i / (float)splinePoints.Length;
                                var v = 1 - Mathf.Abs(2 * completinPercent - 1);
                                uvs[vertIndex + 0] = new Vector2(0, v);
                                uvs[vertIndex + 1] = new Vector2(1, v);

                                vertIndex += 2;
                                triIndex += 6;
                            }
                        }
                        return true;
                    default:
                        {
                            var numArray = splinePoints.Length + (m_spline.close ? 0 : -1);
                            verts = new Vector3[numArray * 4];
                            uvs = new Vector2[verts.Length];
                            tris = new int[2 * numArray * 3];

                            var vertIndex = 0;
                            var triIndex = 0;

                            for (int i = 0; i < numArray; i++)
                            {
                                var left = splinePoints[i].normal;

                                var offset = left * m_size.x;
                                verts[vertIndex + 0] = splinePoints[i].position + offset;
                                verts[vertIndex + 1] = splinePoints[i].position - offset;
                                verts[vertIndex + 2] = splinePoints[(i + 1) % splinePoints.Length].position + offset;
                                verts[vertIndex + 3] = splinePoints[(i + 1) % splinePoints.Length].position - offset;

                                if (i < splinePoints.Length - 1 || m_spline.close)
                                {
                                    tris[triIndex + 0] = (vertIndex + 1) % verts.Length;
                                    tris[triIndex + 1] = (vertIndex + 2) % verts.Length;
                                    tris[triIndex + 2] = (vertIndex + 0) % verts.Length;

                                    tris[triIndex + 3] = (vertIndex + 1) % verts.Length;
                                    tris[triIndex + 4] = (vertIndex + 3) % verts.Length;
                                    tris[triIndex + 5] = (vertIndex + 2) % verts.Length;
                                }

                                var completinPercent = i / (float)splinePoints.Length;
                                var v = 1 - Mathf.Abs(2 * completinPercent - 1);
                                uvs[vertIndex + 0] = new Vector2(0, v);
                                uvs[vertIndex + 1] = new Vector2(1, v);

                                completinPercent = (i + 1) % splinePoints.Length / (float)splinePoints.Length;
                                v = 1 - Mathf.Abs(2 * completinPercent - 1);
                                uvs[vertIndex + 0] = new Vector2(0, v);
                                uvs[vertIndex + 1] = new Vector2(1, v);

                                vertIndex += 4;
                                triIndex += 6;
                            }
                        }
                        return true;
                }
            }

            return false;
        }

        public virtual void RequestAutoUpdate()
        {
            if (autoUpdate)
                Execute();
        }

        public virtual void Execute() { }

#if UNITY_EDITOR
        protected void OnDrawGizmos()
        {
            if (!gizmoSetting.draw || !m_spline)
                return;

            if (m_gizmoMat == null)
                m_gizmoMat = new Material(Shader.Find("Unlit/Color"));

            m_gizmoMat.SetColor(PROP_COLOR, gizmoSetting.color);

            if (GeneratePlaneAlongToSpline(m_anchorAxis, m_zUp, m_flipNormal, m_flipUp, m_flipTangent, m_spacing, m_arrayMode, out var splinePoints, out var verts, out var uvs, out var tris))
            {
                m_gizmoMat.SetPass(0);

                GL.PushMatrix();

                foreach (var range in m_ranges)
                {
                    var offset = 6;

                    for (int i = (int)(range.x * tris.Length / offset); i < (int)(range.y * tris.Length / offset); i += (1 + (int)skip))
                    {
                        var corners = new Vector3[3];
                        corners[0] = verts[tris[i * offset + 0]];
                        corners[1] = verts[tris[i * offset + 1]];
                        corners[2] = verts[tris[i * offset + 2]];

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

                        corners[0] = verts[tris[i * offset + 3]];
                        corners[1] = verts[tris[i * offset + 4]];
                        corners[2] = verts[tris[i * offset + 5]];

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
                }

                GL.PopMatrix();
            }
        }
#endif
    }
}
