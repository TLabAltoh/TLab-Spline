using System.Runtime.InteropServices;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using TLab.MeshEngine;

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
        [SerializeField] private uint m_skip = 0;
        [SerializeField] private float m_offset = 1.0f;
        [SerializeField] private Vector3 m_scale = new Vector3(1.0f, 1.0f, 1.0f);
        [SerializeField] private MeshElement m_element;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        public void CopyPath(Path path)
        {
            var creator = GetComponent<PathCreator>();
            creator.path = path;
        }

        /// <summary>
        /// 
        /// </summary>
        public void UpdateRoad()
        {
            m_element.GetBounds(out var bounds);
            var boundsSize = bounds.size;

            var creator = GetComponent<PathCreator>();
            var path = creator.path;

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
                        var mesh = CreateArrayMesh(m_points, path.IsClosed);
                        GetComponent<MeshFilter>().sharedMesh = mesh;
                        GetComponent<MeshCollider>().sharedMesh = m_collision ? mesh : null;
                    }
                    break;
                case CurveMode.TERRAIN:
                    if (path.CalculateEvenlySpacedPoints(out m_points, m_space))
                    {
                        if (!GetQuadMeshInfo(m_points, path.IsClosed, out Vector3[] verts, out Vector2[] uvs, out int[] tris))
                        {
                            break;
                        }

                        var planes = new Plane[tris.Length / 6];

                        for (int i = 0; i < planes.Length; i++)
                        {
                            var offset = i * 6;

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
                            var terrain = m_terrains[i];

                            var data = terrain.terrainData;

                            var resolution = data.heightmapResolution;

                            var heights = data.GetHeights(0, 0, resolution, resolution);

                            var terrainPixel = new TerrainPixel[resolution * resolution];

                            var xSpace = data.size.x / (resolution - 1);
                            var zSpace = data.size.z / (resolution - 1);

                            for (int row = 0; row < resolution; row++)
                            {
                                for (int col = 0; col < resolution; col++)
                                {
                                    var offset = col * resolution + row;
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
                                    var offset = col * resolution + row;

                                    var height0 = heights[col, row];
                                    var height1 = terrainPixel[offset].position.y;

                                    var lerpRatio = m_fitRatio.Evaluate(Mathf.Abs(terrainPixel[offset].uv.x % 1f - 0.5f));
                                    var lerpHeight = height1 * lerpRatio + height0 * (1f - lerpRatio);

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

            var creator = GetComponent<PathCreator>();
            var path = creator.path;

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

        /// <summary>
        /// Export the current mesh as a separate GameObject
        /// </summary>
        public void Export()
        {
            var go = new GameObject();
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
            m_element.GetBounds(out var bounds);
            var boundsSize = bounds.size;

            verts = new Vector3[points.Length * 2];
            uvs = new Vector2[verts.Length];

            int numTris = (points.Length - 1) + (isClosed ? 2 : 0);
            tris = new int[2 * numTris * 3];

            int vertIndex = 0;
            int triIndex = 0;

            for (int i = 0; i < points.Length; i++)
            {
                Vector3 forward = Vector3.zero;

                if (i < points.Length - 1 || isClosed)  // Neighboring forward
                {
                    forward += points[(i + 1) % points.Length] - points[i];
                }

                if (i > 0 || isClosed)  // Neighboring backward
                {
                    forward += points[i] - points[(i - 1 + points.Length) % points.Length];
                }

                forward.Normalize();

                // Get z-up vector
                var left = new Vector3(-forward.z, m_zUp ? 0.0f : forward.y, forward.x);

                var m_offset = left * boundsSize.x * 0.5f * m_scale.x;
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

                var completinPercent = i / (float)points.Length;
                var v = 1 - Mathf.Abs(2 * completinPercent - 1);
                uvs[vertIndex + 0] = new Vector2(0, v);
                uvs[vertIndex + 1] = new Vector2(1, v);

                vertIndex += 2;
                triIndex += 6;
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="points"></param>
        /// <param name="isClosed"></param>
        /// <returns></returns>
        public Mesh CreateArrayMesh(Vector3[] points, bool isClosed)
        {
            var combine = new CombineInstance[m_range.Length];

            m_element.GetMesh(out var srcMesh);
            m_element.GetBounds(out var bounds);

            var srcVerts = srcMesh.vertices;
            var srcUvs = srcMesh.uv;
            var srcTris = srcMesh.triangles;

            var maxYP = bounds.max.y;
            var maxYN = bounds.min.y;
            var maxXP = bounds.max.x;
            var maxXN = bounds.min.x;
            var maxZP = bounds.max.z * m_offset;
            var maxZN = bounds.min.z * m_offset;

            var boundsUVs = new Vector3[srcVerts.Length];

            for (int i = 0; i < srcVerts.Length; i++)   // Get vertex uv
            {
                Vector3 srcVert = srcVerts[i];
                boundsUVs[i].x = (srcVert.x - maxXN) / (maxXP - maxXN);
                boundsUVs[i].y = (srcVert.y - maxYN) / (maxYP - maxYN);
                boundsUVs[i].z = (srcVert.z - maxZN) / (maxZP - maxZN);
            }

            GetQuadMeshInfo(points, isClosed, out var planeMesh, out var planeUvs, out var planeTris);

            for (int r = 0; r < m_range.Length; r++)
            {
                var start = (int)(m_range[r].x * (m_points.Length - 1));
                var end = (int)(m_range[r].y * (m_points.Length - 1));
                var length = end - start;

                if (m_skip > 0)
                {
                    length /= (int)m_skip;
                }

                if (isClosed)
                {
                    length++;
                }

                var verts = new Vector3[length * srcVerts.Length];
                var uvs = new Vector2[verts.Length];
                var tris = new int[length * srcTris.Length];

                for (int i = 0, p = start; i < length; p += (1 + (int)m_skip), i++)
                {
                    /*
                     *    2 -----・----- 3
                     *    
                     *    
                     *    0 -----・----- 1
                     */

                    var LF = planeMesh[(p * 2 + 0) % planeMesh.Length];
                    var RF = planeMesh[(p * 2 + 1) % planeMesh.Length];
                    var LB = planeMesh[(p * 2 + 2) % planeMesh.Length];
                    var RB = planeMesh[(p * 2 + 3) % planeMesh.Length];

                    for (int j = 0; j < srcVerts.Length; j++)
                    {
                        var lerpL = LF * boundsUVs[j].z + LB * (1 - boundsUVs[j].z);
                        var lerpR = RF * boundsUVs[j].z + RB * (1 - boundsUVs[j].z);

                        var posInPlane = lerpL * boundsUVs[j].x + lerpR * (1 - boundsUVs[j].x);
                        var zOffset = Vector3.Cross((LF - RB), (LB - RB)).normalized * srcVerts[j].y;

                        verts[i * srcVerts.Length + j] = posInPlane + zOffset * m_scale.y;
                        uvs[i * srcVerts.Length + j] = srcUvs[j];
                    }
                }

                for (int i = 0; i < length; i++)
                {
                    for (int j = 0; j < srcTris.Length; j++)
                    {
                        tris[i * srcTris.Length + j] = i * srcVerts.Length + srcTris[j];
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

            var combinedMesh = new Mesh();
            combinedMesh.CombineMeshes(combine);

            return combinedMesh;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="points"></param>
        /// <param name="isClosed"></param>
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
