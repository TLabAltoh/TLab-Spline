using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TLab.CurveTool
{
    [RequireComponent(typeof(PathCreator))]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public class CurveTool : MonoBehaviour
    {
        private enum CurveMode
        {
            CURVE,
            ARRAY
        };

        [Header("Update Option")]
        [SerializeField] private bool m_autoUpdate;

        [Header("Curve Mode")]
        [SerializeField] private CurveMode m_curveMode;

        [Header("Curve Settings")]
        [SerializeField] private bool m_zUp = true;
        [SerializeField] private float m_space = 0.5f;

        [Header("Array Settings")]
        [SerializeField] private float m_offset = 1.0f;
        [SerializeField] private Vector3 m_scale = new Vector3(1.0f, 1.0f, 1.0f);
        [SerializeField] private MeshFilter m_element;
        [SerializeField] private bool m_collision = false;

        private Vector3[] m_points;

        public bool autoUpdate { get => m_autoUpdate; set => m_autoUpdate = value; }

        public Vector3[] points { get => m_points; }


        public void UpdateRoad()
        {
            Vector3 boundsSize = m_element.sharedMesh.bounds.size;

            PathCreator creator = GetComponent<PathCreator>();
            Path path = creator.path;

            switch (m_curveMode)
            {
                case CurveMode.CURVE:
                    path.CalculateEvenlySpacedPoints(out m_points, m_space);
                    GetComponent<MeshFilter>().sharedMesh = null;
                    GetComponent<MeshCollider>().sharedMesh = null;
                    break;
                case CurveMode.ARRAY:
                    if (path.CalculateEvenlySpacedPoints(out m_points, boundsSize.z * m_scale.z * m_offset))
                    {
                        Mesh roadMesh = CreateArrayMesh(m_points, path.IsClosed);
                        GetComponent<MeshFilter>().sharedMesh = roadMesh;
                        GetComponent<MeshCollider>().sharedMesh = m_collision ? roadMesh : null;
                        GetComponent<MeshRenderer>().sharedMaterial.mainTextureScale = new Vector2(1, 1);
                    }
                    break;
            }

#if UNITY_EDITOR
            EditorUtility.SetDirty(creator);
#endif
        }

        /// <summary>
        /// Obtain a planar mesh along the path. This planar mesh is used to deform the array object.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="isClosed"></param>
        /// <returns></returns>
        private (Vector3[], Vector2[], int[]) GetQuadMeshInfo(Vector3[] points, bool isClosed)
        {
            // bounds size
            Vector3 boundsSize = m_element.sharedMesh.bounds.size;

            Vector3[] verts = new Vector3[points.Length * 2];
            Vector2[] uvs = new Vector2[verts.Length];

            int numTris = (points.Length - 1) + (isClosed ? 2 : 0);
            int[] tris = new int[2 * numTris * 3];

            int vertIndex = 0;
            int triIndex = 0;

            for (int i = 0; i < points.Length; i++)
            {
                Vector3 forward = Vector3.zero;

                // Neighboring forward
                if (i < points.Length - 1 || isClosed)
                {
                    forward += points[(i + 1) % points.Length] - points[i];
                }

                // Neighboring backward
                if (i > 0 || isClosed)
                {
                    forward += points[i] - points[(i - 1 + points.Length) % points.Length];
                }

                forward.Normalize();

                // Get z-up vector
                Vector3 left = new Vector3(-forward.z, m_zUp ? 0.0f : forward.y, forward.x);

                Vector3 m_offset = left * boundsSize.x * 0.5f * m_scale.x;
                verts[vertIndex + 0] = points[i] + m_offset;
                verts[vertIndex + 1] = points[i] - m_offset;

                /*
                 *    2 -----・----- 3
                 *    
                 *    
                 *    0 -----・----- 1
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

                float completinPercent = i / (float)points.Length;
                float v = 1 - Mathf.Abs(2 * completinPercent - 1);
                uvs[vertIndex + 0] = new Vector2(0, v);
                uvs[vertIndex + 1] = new Vector2(1, v);

                vertIndex += 2;
                triIndex += 6;
            }

            return (verts, uvs, tris);
        }

        public Mesh CreateArrayMesh(Vector3[] points, bool isClosed)
        {
            Vector3[] srcVerts = m_element.sharedMesh.vertices;
            Vector2[] srcUvs = m_element.sharedMesh.uv;
            int[] srcTris = m_element.sharedMesh.triangles;

            // Get bound box
            Vector3 boundsCenter = m_element.sharedMesh.bounds.center;
            Vector3 boundsSize = m_element.sharedMesh.bounds.size;

            float maxTop = boundsCenter.y + boundsSize.y * 0.5f;
            float maxBottom = boundsCenter.y - boundsSize.y * 0.5f;
            float maxRight = boundsCenter.x + boundsSize.x * 0.5f;
            float maxLeft = boundsCenter.x - boundsSize.x * 0.5f;
            float maxForward = boundsCenter.z + boundsSize.z * m_offset * 0.5f;
            float maxBackward = boundsCenter.z - boundsSize.z * m_offset * 0.5f;

            Vector3[] boundsUVs = new Vector3[srcVerts.Length];

            // Get vertex uv
            for (int i = 0; i < srcVerts.Length; i++)
            {
                Vector3 srcVert = srcVerts[i];
                boundsUVs[i].x = (srcVert.x - maxLeft) / (maxRight - maxLeft);
                boundsUVs[i].y = (srcVert.y - maxBottom) / (maxTop - maxBottom);
                boundsUVs[i].z = (srcVert.z - maxBackward) / (maxForward - maxBackward);
            }

            Vector3[] roadPlane = GetQuadMeshInfo(points, isClosed).Item1;

            int arrayNum = isClosed ? points.Length : (points.Length - 1);
            Vector3[] verts = new Vector3[arrayNum * srcVerts.Length];
            Vector2[] uvs = new Vector2[verts.Length];
            int[] tris = new int[arrayNum * srcTris.Length];

            for (int i = 0; i < points.Length; i++)
            {
                if (i > 0 && i < points.Length - 1 || isClosed)
                {
                    /*
                     *     0 -----・-----  1
                     *    
                     *    
                     *    -2 -----・----- -1
                     */

                    Vector3 leftForward = roadPlane[i * 2];
                    Vector3 rightForward = roadPlane[(i * 2 + 1) % roadPlane.Length];
                    Vector3 leftBackward = roadPlane[(i * 2 - 2 + roadPlane.Length) % roadPlane.Length];
                    Vector3 rightBackward = roadPlane[(i * 2 - 1 + roadPlane.Length) % roadPlane.Length];

                    for (int j = 0; j < srcVerts.Length; j++)
                    {
                        Vector3 lerpLeft = leftForward * boundsUVs[j].z + leftBackward * (1 - boundsUVs[j].z);
                        Vector3 lerpRight = rightForward * boundsUVs[j].z + rightBackward * (1 - boundsUVs[j].z);

                        Vector3 posInPlane = lerpLeft * boundsUVs[j].x + lerpRight * (1 - boundsUVs[j].x);
                        Vector3 zOffset = Vector3.Cross((leftForward - rightBackward), (leftBackward - rightBackward)).normalized * srcVerts[j].y;

                        verts[i * srcVerts.Length + j] = posInPlane + zOffset * m_scale.y * (maxTop - maxBottom);
                        uvs[i * srcVerts.Length + j] = srcUvs[j];
                    }
                }
            }

            for (int i = 0; i < points.Length; i++)
            {
                if (i > 0 && i < points.Length - 1 || isClosed)
                {
                    for (int j = 0; j < srcTris.Length; j++)
                    {
                        tris[i * srcTris.Length + j] = i * srcVerts.Length + srcTris[j];
                    }
                }
            }

            Mesh mesh = new Mesh();
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.uv = uvs;
            mesh.RecalculateNormals();

            return mesh;
        }

        public void ArrayMesh(Vector3[] points, bool isClosed)
        {
            // Mesh
            PathCreator creator = GetComponent<PathCreator>();
            Mesh roadMesh = CreateArrayMesh(points, isClosed);
            GetComponent<MeshFilter>().sharedMesh = roadMesh;
            GetComponent<MeshCollider>().sharedMesh = roadMesh;

#if UNITY_EDITOR
            EditorUtility.SetDirty(creator);
#endif
        }
    }
}
