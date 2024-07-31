using System.Collections.Generic;
using UnityEngine;
using TLab.MeshEngine;

namespace TLab.Spline
{
    public class SplineMeshArray : SplinePlaneArray
    {
        [Header("Array Mesh")]
        [SerializeField] private MeshElement m_meshElement;
        [SerializeField] protected GameObject m_meshHolder;
        [SerializeField] protected bool m_collision = false;
        [SerializeField] private float m_slideOffset = 0f;
        [SerializeField] private uint m_skip = 0;
        [SerializeField] private Vector2[] m_range = new Vector2[1] { new Vector2(0, 1) };

        private string THIS_NAME => "[" + this.GetType() + "] ";

        public MeshElement meshElement
        {
            get => m_meshElement;
            set
            {
                if (m_meshElement != value)
                {
                    m_meshElement = value;

                    RequestAutoUpdate();
                }
            }
        }

        public GameObject meshHolder
        {
            get => m_meshHolder;
            set
            {
                if (m_meshHolder != value)
                {
                    m_meshHolder = value;

                    RequestAutoUpdate();
                }
            }
        }

        public bool collision
        {
            get => m_collision;
            set
            {
                if (m_collision != value)
                {
                    m_collision = value;

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

        public Vector2[] range
        {
            get => m_range;
            set
            {
                if (m_range != value)
                {
                    m_range = value;

                    RequestAutoUpdate();
                }
            }
        }

        public void Export()
        {
            var go = new GameObject(m_meshHolder.name + " (Export)");
            go.transform.localPosition = transform.localPosition;
            go.transform.localRotation = transform.localRotation;
            go.transform.localScale = transform.localScale;
            go.transform.parent = transform.parent;
            go.AddComponent<MeshFilter>().sharedMesh = m_meshHolder.GetComponent<MeshFilter>().sharedMesh;
            go.AddComponent<MeshCollider>().sharedMesh = m_meshHolder.GetComponent<MeshCollider>().sharedMesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = m_meshHolder.GetComponent<MeshRenderer>().sharedMaterial;
            go.GetComponent<MeshRenderer>().sharedMaterial.mainTextureScale = new Vector2(1, 1);
        }

        public override void UpdateWithCurrentSpline()
        {
            if (!m_spline)
            {
                Debug.LogError(THIS_NAME + "spline is null !");
                return;
            }

            if (!m_meshElement)
            {
                Debug.LogError(THIS_NAME + "mesh element is null !");
                return;
            }

            if (m_spline.CalculateEvenlySpacedPoints(out var points, m_space))
            {
                var arrayMeshTask = CreateArrayMeshTask(points, m_spline.isClosed);

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

                var meshFilter = m_meshHolder.GetComponent<MeshFilter>();
                if (meshFilter)
                    meshFilter.sharedMesh = combinedMesh;

                var meshCollider = m_meshHolder.GetComponent<MeshCollider>();
                if (meshCollider)
                    meshCollider.sharedMesh = m_collision && (combinedMesh != null) ? combinedMesh : null;
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(m_meshHolder);
#endif
        }

        private IEnumerator<Mesh> CreateArrayMeshTask(Vector3[] points, bool isClosed)
        {
            GeneratePlaneAlongToSpline(points, isClosed, m_arrayMode, out var planeVerts, out var planeUVs, out var planeTris);

            m_meshElement.GetMesh(out var srcMesh);
            m_meshElement.GetBounds(out var bounds);

            var srcVerts = srcMesh.vertices;
            var srcUvs = srcMesh.uv;
            var srcTris = srcMesh.triangles;

            var maxYP = bounds.max.y;
            var maxYN = bounds.min.y;
            var maxXP = bounds.max.x;
            var maxXN = bounds.min.x;
            var maxZP = bounds.max.z;
            var maxZN = bounds.min.z;

            int offset;

            switch (m_arrayMode)
            {
                case ArrayMode.NO_SPACE:
                    offset = 2;
                    break;
                default:
                    offset = 4;
                    break;
            }

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
                var start = (int)(m_range[r].x * (points.Length - 1));
                var end = (int)(m_range[r].y * (points.Length - 1));

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
                     *    2 -----E----- 3
                     *    
                     *    
                     *    0 -----E----- 1
                     */

                    var LF = planeVerts[(i * offset + 0) % planeVerts.Length];
                    var RF = planeVerts[(i * offset + 1) % planeVerts.Length];
                    var LB = planeVerts[(i * offset + 2) % planeVerts.Length];
                    var RB = planeVerts[(i * offset + 3) % planeVerts.Length];

                    for (int j = 0; j < srcVerts.Length; j++)
                    {
                        var lerpL = LF * boundsUVs[j].z + LB * (1 - boundsUVs[j].z);
                        var lerpR = RF * boundsUVs[j].z + RB * (1 - boundsUVs[j].z);

                        var posInPlane = lerpL * boundsUVs[j].x + lerpR * (1 - boundsUVs[j].x);
                        var zOffset = Vector3.Cross((LF - RB), (LB - RB)).normalized * srcVerts[j].y;

                        verts[j] = posInPlane;
                        verts[j] += zOffset * m_size.y;
                        verts[j] += m_slideOffset * (boundsUVs[j].z * (LF - RF) + (1 - boundsUVs[j].z) * (LB - RB));

                        uvs[j] = srcUvs[j];
                    }

                    for (int j = 0; j < srcTris.Length; j++)
                    {
                        tris[j] = srcTris[j];
                    }

                    var mesh = new Mesh();
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
