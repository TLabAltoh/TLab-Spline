using System.Collections.Generic;
using System.Collections;
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

        private enum ArrayMode
        {
            MONO,
            SEPARATE
        }

        [Header("Update Option")]
        [SerializeField] private bool m_autoUpdate;

        [Header("Curve Mode")]
        [SerializeField] private CurveMode m_curveMode;
        [SerializeField] private ArrayMode m_arrayMode;

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

        [Header("Random Settings")]
        [SerializeField] private float m_length = 1.0f;

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
        /// <param name="mesh"></param>
        /// <param name="parent"></param>
        public void AddChild(Mesh mesh)
        {
            var go = new GameObject("Element");

            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>();

            if (m_collision)
            {
                go.AddComponent<MeshCollider>().sharedMesh = mesh;
            }

            go.transform.parent = this.transform;
        }

        /// <summary>
        /// 
        /// </summary>
        public void ClearChild()
        {
            while (this.transform.childCount > 0)
            {
                DestroyImmediate(this.transform.GetChild(0).gameObject);
            }

            m_tailQuad = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mesh"></param>
        public void UpdateRootMesh(Mesh mesh)
        {
            GetComponent<MeshFilter>().sharedMesh = mesh;
            GetComponent<MeshCollider>().sharedMesh = m_collision && (mesh != null) ? mesh : null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        public void CopyPath(Path path)
        {
            var creator = GetComponent<PathCreator>();
            creator.path = path;
        }

        private class TailQuad
        {
            public Vector3 vert0;
            public Vector3 vert1;
            public Vector3 vert2;
            public Vector3 vert3;
        }

        private TailQuad m_tailQuad = null;

        /// <summary>
        /// 
        /// </summary>
        public void ExpandedByRandomChild()
        {
            var creator = GetComponent<PathCreator>();

            var path = creator.path;

            if (path.IsClosed)
            {
                return;
            }

            // If TailQuad is null, quadVerts will have the quad mesh info of the parent mesh.

            var vert0 = m_quadVerts[m_quadVerts.Length - 1];
            var vert1 = m_quadVerts[m_quadVerts.Length - 2];
            var vert2 = m_quadVerts[m_quadVerts.Length - 3];
            var vert3 = m_quadVerts[m_quadVerts.Length - 4];

            if (m_tailQuad != null)
            {
                vert0 = m_tailQuad.vert0;
                vert1 = m_tailQuad.vert1;
                vert2 = m_tailQuad.vert2;
                vert3 = m_tailQuad.vert3;
            }

            var middle0 = (vert0 + vert1) * 0.5f;
            var middle1 = (vert2 + vert3) * 0.5f;

            var upper = Vector3.Cross((vert0 - vert1).normalized, (vert0 - vert2).normalized);
            var right = (vert0 - vert1).normalized;
            var normal = Vector3.Cross(upper, right).normalized;

            m_element.GetBounds(out var bounds);
            var boundsSize = bounds.size;

            //
            // Create random anchor point and set control point from random point.
            //

            var newTheta = Random.Range(-Mathf.PI * 0.5f, Mathf.PI * 0.5f) * Mathf.Rad2Deg;
            var newAnchor = middle0 + Quaternion.AngleAxis(newTheta, upper) * normal * (boundsSize.z * m_scale.z * m_offset) * m_length;
            var newControl0 = middle0 + normal * (middle0 - newAnchor).magnitude * 0.5f;
            var newControl1 = (middle0 + newAnchor) * 0.5f;

            var newPath = new Path(new List<Vector3>()
            {
                middle0,
                newControl0,
                newControl1,
                newAnchor
            });

            //
            // Create mesh from random points
            //

            if (newPath.CalculateEvenlySpacedPoints(out var points, boundsSize.z * m_scale.z * m_offset))
            {
                var quadMeshTask = UpdateQuadMesh(points, false);

                while (quadMeshTask.MoveNext()) ;

                m_quadVerts[0] = vert1; // Fixing misalignments to make it seamless.
                m_quadVerts[1] = vert0;

                m_tailQuad = new TailQuad
                {
                    vert0 = m_quadVerts[m_quadVerts.Length - 1],
                    vert1 = m_quadVerts[m_quadVerts.Length - 2],
                    vert2 = m_quadVerts[m_quadVerts.Length - 3],
                    vert3 = m_quadVerts[m_quadVerts.Length - 4],
                };

                var arrayMeshTask = CreateArrayMesh(false);

                var combines = new List<CombineInstance>();

                while (arrayMeshTask.MoveNext())
                {
                    var combine = new CombineInstance();
                    combine.mesh = arrayMeshTask.Current;
                    combine.transform = Matrix4x4.identity;

                    combines.Add(combine);
                }

                var combinedMesh = new Mesh();
                combinedMesh.CombineMeshes(combines.ToArray());

                AddChild(combinedMesh);
            }
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
                        UpdateRootMesh(null);
                    }
                    break;
                case CurveMode.ARRAY:
                    if (path.CalculateEvenlySpacedPoints(out m_points, boundsSize.z * m_scale.z * m_offset))
                    {
                        ClearChild();

                        var quadMeshTask = UpdateQuadMesh(points, path.IsClosed);

                        while (quadMeshTask.MoveNext()) ;

                        var arrayMeshTask = CreateArrayMesh(path.IsClosed);

                        switch (m_arrayMode)
                        {
                            case ArrayMode.MONO:

                                var combines = new List<CombineInstance>();

                                while (arrayMeshTask.MoveNext())
                                {
                                    var combine = new CombineInstance();
                                    combine.mesh = arrayMeshTask.Current;
                                    combine.transform = Matrix4x4.identity;

                                    combines.Add(combine);
                                }

                                var combinedMesh = new Mesh();
                                combinedMesh.CombineMeshes(combines.ToArray());

                                UpdateRootMesh(combinedMesh);

                                break;
                            case ArrayMode.SEPARATE:

                                UpdateRootMesh(null);

                                while (arrayMeshTask.MoveNext())
                                {
                                    AddChild(arrayMeshTask.Current);
                                }

                                break;
                        }
                    }
                    break;
                case CurveMode.TERRAIN:
                    if (path.CalculateEvenlySpacedPoints(out m_points, m_space))
                    {
                        var quadMeshTask = UpdateQuadMesh(points, path.IsClosed);

                        while (quadMeshTask.MoveNext()) ;

                        var planes = new Plane[m_quadTris.Length / 6];

                        for (int i = 0; i < planes.Length; i++)
                        {
                            var offset = i * 6;

                            planes[i] = new Plane
                            {
                                triangle0 = new Triangle
                                {
                                    vert0 = transform.TransformPoint(m_quadVerts[m_quadTris[offset + 0]]),
                                    vert1 = transform.TransformPoint(m_quadVerts[m_quadTris[offset + 1]]),
                                    vert2 = transform.TransformPoint(m_quadVerts[m_quadTris[offset + 2]]),

                                    uv0 = m_quadUVs[m_quadTris[offset + 0]],
                                    uv1 = m_quadUVs[m_quadTris[offset + 1]],
                                    uv2 = m_quadUVs[m_quadTris[offset + 2]]
                                },

                                triangle1 = new Triangle
                                {
                                    vert0 = transform.TransformPoint(m_quadVerts[m_quadTris[offset + 3]]),
                                    vert1 = transform.TransformPoint(m_quadVerts[m_quadTris[offset + 4]]),
                                    vert2 = transform.TransformPoint(m_quadVerts[m_quadTris[offset + 5]]),

                                    uv0 = m_quadUVs[m_quadTris[offset + 3]],
                                    uv1 = m_quadUVs[m_quadTris[offset + 4]],
                                    uv2 = m_quadUVs[m_quadTris[offset + 5]]
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
                    var quadMeshTask = UpdateQuadMesh(points, path.IsClosed);

                    while (quadMeshTask.MoveNext()) ;

                    m_mat.SetPass(0);

                    GL.PushMatrix();

                    for (int i = 0; i < m_quadTris.Length; i += 3)
                    {
                        var corners = new Vector3[3];
                        corners[0] = m_quadVerts[m_quadTris[i + 0]];
                        corners[1] = m_quadVerts[m_quadTris[i + 1]];
                        corners[2] = m_quadVerts[m_quadTris[i + 2]];

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

        private Vector3[] m_quadVerts;
        private Vector2[] m_quadUVs;
        private int[] m_quadTris;

        /// <summary>
        /// Obtain a planar mesh along the path. This planar mesh is used to deform the array object.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="isClosed"></param>
        /// <returns></returns>
        private IEnumerator UpdateQuadMesh(Vector3[] points, bool isClosed)
        {
            m_element.GetBounds(out var bounds);
            var boundsSize = bounds.size;

            m_quadVerts = new Vector3[points.Length * 2];
            m_quadUVs = new Vector2[m_quadVerts.Length];

            int numTris = (points.Length - 1) + (isClosed ? 2 : 0);
            m_quadTris = new int[2 * numTris * 3];

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
                m_quadVerts[vertIndex + 0] = points[i] + m_offset;
                m_quadVerts[vertIndex + 1] = points[i] - m_offset;

                /*
                 *    2 -----・----- 3
                 *    
                 *    
                 *    0 -----・----- 1
                 */

                if (i < points.Length - 1 || isClosed)
                {
                    m_quadTris[triIndex + 0] = (vertIndex + 0) % m_quadVerts.Length;
                    m_quadTris[triIndex + 1] = (vertIndex + 2) % m_quadVerts.Length;
                    m_quadTris[triIndex + 2] = (vertIndex + 1) % m_quadVerts.Length;

                    m_quadTris[triIndex + 3] = (vertIndex + 1) % m_quadVerts.Length;
                    m_quadTris[triIndex + 4] = (vertIndex + 2) % m_quadVerts.Length;
                    m_quadTris[triIndex + 5] = (vertIndex + 3) % m_quadVerts.Length;
                }

                var completinPercent = i / (float)points.Length;
                var v = 1 - Mathf.Abs(2 * completinPercent - 1);
                m_quadUVs[vertIndex + 0] = new Vector2(0, v);
                m_quadUVs[vertIndex + 1] = new Vector2(1, v);

                vertIndex += 2;
                triIndex += 6;

                yield return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isClosed"></param>
        /// <returns></returns>
        public IEnumerator<Mesh> CreateArrayMesh(bool isClosed)
        {
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

            for (int r = 0; r < m_range.Length; r++)
            {
                var start = (int)(m_range[r].x * (m_points.Length - 1));
                var end = (int)(m_range[r].y * (m_points.Length - 1));

                if (isClosed)
                {
                    end++;
                }

                for (int i = start; i < end; i += (1 + (int)m_skip))
                {
                    var verts = new Vector3[srcVerts.Length];
                    var uvs = new Vector2[verts.Length];
                    var tris = new int[srcTris.Length];

                    /*
                     *    2 -----・----- 3
                     *    
                     *    
                     *    0 -----・----- 1
                     */

                    var LF = m_quadVerts[(i * 2 + 0) % m_quadVerts.Length];
                    var RF = m_quadVerts[(i * 2 + 1) % m_quadVerts.Length];
                    var LB = m_quadVerts[(i * 2 + 2) % m_quadVerts.Length];
                    var RB = m_quadVerts[(i * 2 + 3) % m_quadVerts.Length];

                    for (int j = 0; j < srcVerts.Length; j++)
                    {
                        var lerpL = LF * boundsUVs[j].z + LB * (1 - boundsUVs[j].z);
                        var lerpR = RF * boundsUVs[j].z + RB * (1 - boundsUVs[j].z);

                        var posInPlane = lerpL * boundsUVs[j].x + lerpR * (1 - boundsUVs[j].x);
                        var zOffset = Vector3.Cross((LF - RB), (LB - RB)).normalized * srcVerts[j].y;

                        verts[j] = posInPlane + zOffset * m_scale.y;
                        uvs[j] = srcUvs[j];
                    }

                    for (int j = 0; j < srcTris.Length; j++)
                    {
                        tris[j] = srcTris[j];
                    }

                    Mesh mesh = new Mesh();
                    mesh.vertices = verts;
                    mesh.triangles = tris;
                    mesh.uv = uvs;
                    mesh.RecalculateNormals();

                    yield return mesh;
                }
            }
        }
    }
}
