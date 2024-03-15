using System.Runtime.InteropServices;
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
            ARRAY,
            TERRAIN
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
        [SerializeField] private Vector2[] m_range = new Vector2[1];

        [Header("Terrain Group")]
        [SerializeField] private Terrain[] m_terrains;
        [SerializeField] private AnimationCurve m_fitRatio;
        [SerializeField] private ComputeShader m_terrainFit;

        private Material m_mat; // for gl.

        private GraphicsBuffer m_csTerrainBuffer;
        private GraphicsBuffer m_csPlaneBuffer;

        private Vector3[] m_points;

        private static int DISPATCH_GROUP_SIZE = Shader.PropertyToID("_DispatchGroupSize");
        private static int PLANES = Shader.PropertyToID("_Planes");
        private static int TERRAIN_PIXELS = Shader.PropertyToID("_TerrainPixels");

        public bool autoUpdate { get => m_autoUpdate; set => m_autoUpdate = value; }

        public Vector3[] points { get => m_points; }

        public Vector2[] range { get => m_range; }

        struct TerrainPixel
        {
            public Vector3 position;

            public Vector2 uv;
        };

        struct Triangle
        {
            public Vector3 vert0;
            public Vector3 vert1;
            public Vector3 vert2;

            public Vector2 uv0;
            public Vector2 uv1;
            public Vector2 uv2;
        };

        struct Plane
        {
            public Triangle triangle0;
            public Triangle triangle1;
        };

        public void UpdateRoad()
        {
            Vector3 boundsSize = m_element.sharedMesh.bounds.size;

            PathCreator creator = GetComponent<PathCreator>();
            Path path = creator.path;

            switch (m_curveMode)
            {
                case CurveMode.CURVE:
                    if (path.CalculateEvenlySpacedPoints(out m_points, m_space))
                    {
                        GetComponent<MeshFilter>().sharedMesh = null;
                        GetComponent<MeshCollider>().sharedMesh = null;
                    }
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
                case CurveMode.TERRAIN:
                    if (path.CalculateEvenlySpacedPoints(out m_points, m_space))
                    {
                        if (!GetQuadMeshInfo(m_points, path.IsClosed, out Vector3[] verts, out Vector2[] uvs, out int[] tris))
                        {
                            break;
                        }

                        Plane[] planes = new Plane[tris.Length / 6];

                        for (int i = 0; i < planes.Length; i++)
                        {
                            int offset = i * 6;

                            planes[i] = new Plane
                            {
                                triangle0 = new Triangle
                                {
                                    vert0 = transform.TransformPoint(verts[tris[offset + 0]]),
                                    vert1 = transform.TransformPoint(verts[tris[offset + 1]]),
                                    vert2 = transform.TransformPoint(verts[tris[offset + 2]]),

                                    uv0 = uvs[tris[offset + 0]],
                                    uv1 = uvs[tris[offset + 1]],
                                    uv2 = uvs[tris[offset + 2]]
                                },

                                triangle1 = new Triangle
                                {
                                    vert0 = transform.TransformPoint(verts[tris[offset + 3]]),
                                    vert1 = transform.TransformPoint(verts[tris[offset + 4]]),
                                    vert2 = transform.TransformPoint(verts[tris[offset + 5]]),

                                    uv0 = uvs[tris[offset + 3]],
                                    uv1 = uvs[tris[offset + 4]],
                                    uv2 = uvs[tris[offset + 5]]
                                },
                            };
                        }

                        CSUtil.GraphicsBuffer(ref m_csPlaneBuffer, GraphicsBuffer.Target.Structured, planes.Length, Marshal.SizeOf<Plane>());
                        m_csPlaneBuffer.SetData(planes);

                        m_terrainFit.SetBuffer(0, PLANES, m_csPlaneBuffer);

                        for (int i = 0; i < m_terrains.Length; i++)
                        {
                            Terrain terrain = m_terrains[i];

                            TerrainData data = terrain.terrainData;

                            int resolution = data.heightmapResolution;

                            float[,] heights = data.GetHeights(0, 0, resolution, resolution);

                            TerrainPixel[] terrainPixel = new TerrainPixel[resolution * resolution];

                            float xSpace = data.size.x / (resolution - 1);
                            float zSpace = data.size.z / (resolution - 1);

                            for (int row = 0; row < resolution; row++)
                            {
                                for (int col = 0; col < resolution; col++)
                                {
                                    int offset = col * resolution + row;
                                    terrainPixel[offset].position.x = terrain.transform.position.x + xSpace * row;
                                    terrainPixel[offset].position.z = terrain.transform.position.z + zSpace * col;
                                    terrainPixel[offset].position.y = heights[col, row] * data.size.y + terrain.transform.position.y;

                                    // uv is not determined until the compute shader is passed.
                                }
                            }

                            CSUtil.GraphicsBuffer(ref m_csTerrainBuffer, GraphicsBuffer.Target.Structured, terrainPixel.Length, Marshal.SizeOf<TerrainPixel>());
                            m_csTerrainBuffer.SetData(terrainPixel);

                            m_terrainFit.SetBuffer(0, TERRAIN_PIXELS, m_csTerrainBuffer);

                            CSUtil.GetDispatchGroupSize(m_terrainFit, 0,
                                resolution, resolution, 1,
                                out int groupSizeX, out int groupSizeY, out int groupSizeZ);

                            m_terrainFit.SetInts(DISPATCH_GROUP_SIZE, groupSizeX, groupSizeY, groupSizeZ);

                            CSUtil.Dispatch(m_terrainFit, 0, groupSizeX, groupSizeY, groupSizeZ);

                            m_csTerrainBuffer.GetData(terrainPixel);

                            for (int row = 0; row < resolution; row++)
                            {
                                for (int col = 0; col < resolution; col++)
                                {
                                    int offset = col * resolution + row;

                                    float height0 = heights[col, row];
                                    float height1 = terrainPixel[offset].position.y;

                                    float lerpRatio = m_fitRatio.Evaluate(Mathf.Abs(terrainPixel[offset].uv.x % 1f - 0.5f));
                                    float lerpHeight = height1 * lerpRatio + height0 * (1f - lerpRatio);

                                    heights[col, row] = Mathf.Clamp01((lerpHeight - terrain.transform.position.y) / data.size.y);
                                }
                            }

                            data.SetHeights(0, 0, heights);

                            CSUtil.DisposeBuffer(ref m_csTerrainBuffer);
                        }

                        CSUtil.DisposeBuffer(ref m_csPlaneBuffer);
                    }
                    break;
            }

#if UNITY_EDITOR
            EditorUtility.SetDirty(creator);
#endif
        }

        private void OnDrawGizmos()
        {
            if (m_mat == null)
            {
                m_mat = new Material(Shader.Find("Unlit/Color"));
            }

            m_mat.SetColor("_Color", Color.green);

            PathCreator creator = GetComponent<PathCreator>();
            Path path = creator.path;

            if (creator.displayPlane)
            {
                if (path.CalculateEvenlySpacedPoints(out m_points, m_space))
                {
                    if (!GetQuadMeshInfo(m_points, path.IsClosed, out Vector3[] verts, out Vector2[] uvs, out int[] tris))
                    {
                        return;
                    }

                    m_mat.SetPass(0);

                    GL.PushMatrix();

                    for (int i = 0; i < tris.Length; i += 3)
                    {
                        Vector3[] corners = new Vector3[3];
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

        /// <summary>
        /// Export the current mesh as a separate GameObject
        /// </summary>
        public void Export()
        {
            GameObject go = new GameObject();
            go.transform.localPosition = transform.localPosition;
            go.transform.localRotation = transform.localRotation;
            go.transform.localScale = transform.localScale;
            go.transform.parent = transform.parent;
            go.AddComponent<MeshFilter>().sharedMesh = GetComponent<MeshFilter>().sharedMesh;
            go.AddComponent<MeshCollider>().sharedMesh = GetComponent<MeshCollider>().sharedMesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = GetComponent<MeshRenderer>().sharedMaterial;
            go.GetComponent<MeshRenderer>().sharedMaterial.mainTextureScale = new Vector2(1, 1);
        }

        /// <summary>
        /// Obtain a planar mesh along the path. This planar mesh is used to deform the array object.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="isClosed"></param>
        /// <returns></returns>
        private bool GetQuadMeshInfo(
            Vector3[] points, bool isClosed,
            out Vector3[] verts, out Vector2[] uvs, out int[] tris)
        {
            // bounds size
            Vector3 boundsSize = m_element.sharedMesh.bounds.size;

            verts = new Vector3[points.Length * 2];
            uvs = new Vector2[verts.Length];

            int numTris = (points.Length - 1) + (isClosed ? 2 : 0);
            tris = new int[2 * numTris * 3];

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

            return true;
        }

        public Mesh CreateArrayMesh(Vector3[] points, bool isClosed)
        {
            CombineInstance[] combine = new CombineInstance[m_range.Length];

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

            GetQuadMeshInfo(points, isClosed, out Vector3[] planeMesh, out Vector2[] planeUvs, out int[] planeTris);

            for (int r = 0; r < m_range.Length; r++)
            {
                int start = (int)(m_range[r].x * m_points.Length);
                int end = (int)(m_range[r].y * m_points.Length);
                int length = end - start + 1;

                int arrayNum = isClosed ? length : (length - 1);

                Vector3[] verts = new Vector3[arrayNum * srcVerts.Length];
                Vector2[] uvs = new Vector2[verts.Length];
                int[] tris = new int[arrayNum * srcTris.Length];

                for (int p = start, i = 0; (p < end) && (i < length); p++, i++)
                {
                    if ((p > 0 && p < points.Length - 1) && (i > 0 && i < length) || isClosed)
                    {
                        /*
                         *     0 -----・-----  1
                         *    
                         *    
                         *    -2 -----・----- -1
                         */

                        Vector3 leftForward = planeMesh[p * 2];
                        Vector3 rightForward = planeMesh[(p * 2 + 1) % planeMesh.Length];
                        Vector3 leftBackward = planeMesh[(p * 2 - 2 + planeMesh.Length) % planeMesh.Length];
                        Vector3 rightBackward = planeMesh[(p * 2 - 1 + planeMesh.Length) % planeMesh.Length];

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

                for (int i = 0; i < length; i++)
                {
                    if ((i > 0 && i < length - 1) || isClosed)
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

                combine[r].mesh = mesh;
                combine[r].transform = Matrix4x4.identity;
            }

            Mesh combinedMesh = new Mesh();
            combinedMesh.CombineMeshes(combine);

            return combinedMesh;
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
