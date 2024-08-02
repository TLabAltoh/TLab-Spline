using System.Collections.Generic;
using UnityEngine;
using TLab.MeshEngine;

namespace TLab.Spline
{
    public class SplineMeshArray : SplinePlaneArray
    {
        [Header("Array Mesh")]
        [SerializeField] protected MeshElement m_meshElement;
        [SerializeField] protected GameObject m_meshHolder;
        [SerializeField] protected bool m_collision = false;

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

        public override void Execute()
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

            var arrayMeshTask = CreateArrayMeshTask();

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

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(m_meshHolder);
#endif
        }

        private IEnumerator<Mesh> CreateArrayMeshTask()
        {
            if (GeneratePlaneAlongToSpline(m_zUp, m_spacing, m_arrayMode, out var splinePoints, out var planeVerts, out var planeUVs, out var planeTris))
            {
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

                var boundsUVs = new Vector3[srcVerts.Length];

                for (int i = 0; i < srcVerts.Length; i++)   // Get vertex uv
                {
                    Vector3 srcVert = srcVerts[i];
                    boundsUVs[i].x = (srcVert.x - maxXN) / (maxXP - maxXN);
                    boundsUVs[i].y = (srcVert.y - maxYN) / (maxYP - maxYN);
                    boundsUVs[i].z = (srcVert.z - maxZN) / (maxZP - maxZN);
                }

                var offset = 6;

                foreach (var range in m_ranges)
                {
                    for (int i = (int)(range.x * (planeTris.Length / offset)); i < (int)(range.y * (planeTris.Length / offset)); i += (1 + (int)skip))
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

                        // tris: 120, 132
                        //     : 012, 345

                        var LF = planeVerts[planeTris[i * offset + 1]];
                        var RF = planeVerts[planeTris[i * offset + 4]];
                        var LB = planeVerts[planeTris[i * offset + 2]];
                        var RB = planeVerts[planeTris[i * offset + 0]];

                        for (int j = 0; j < srcVerts.Length; j++)
                        {
                            var lerpL = LF * boundsUVs[j].z + LB * (1 - boundsUVs[j].z);
                            var lerpR = RF * boundsUVs[j].z + RB * (1 - boundsUVs[j].z);

                            var up = (splinePoints[(i + 1) % splinePoints.Length].up * boundsUVs[j].z + splinePoints[i % splinePoints.Length].up * (1f - boundsUVs[j].z)).normalized;

                            verts[j] = lerpL * boundsUVs[j].x + lerpR * (1 - boundsUVs[j].x);
                            verts[j] += up * srcVerts[j].y * m_size.y;
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
}
