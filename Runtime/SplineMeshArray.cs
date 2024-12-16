using System.Collections.Generic;
using UnityEngine;
using TLab.MeshEngine;

namespace TLab.Spline
{
    public class SplineMeshArray : SplinePlaneArray
    {
        [Header("Array Mesh")]
        [SerializeField] protected MeshElement m_element;
        [SerializeField] protected MeshContainer m_container;
        [SerializeField] protected bool m_collision = false;

        private string THIS_NAME => "[" + this.GetType() + "] ";

        public MeshElement element
        {
            get => m_element;
            set
            {
                if (m_element != value)
                {
                    m_element = value;

                    RequestAutoUpdate();
                }
            }
        }

        public MeshContainer container
        {
            get => m_container;
            set
            {
                if (m_container != value)
                {
                    m_container = value;

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

        public void ClearMesh()
        {
            if (!m_container)
            {
                Debug.LogError(THIS_NAME + $"{nameof(MeshContainer)} is null !");
                return;
            }

            m_container.Clear();

#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.EditorUtility.SetDirty(m_container);
#endif
        }

        public void Export()
        {
            var go = new GameObject(gameObject.name + " (Export)");
            go.transform.localPosition = transform.localPosition;
            go.transform.localRotation = transform.localRotation;
            go.transform.localScale = transform.localScale;
            go.transform.parent = transform.parent;
            go.AddComponent<MeshFilter>().sharedMesh = m_container.GetComponent<MeshFilter>().sharedMesh;
            go.AddComponent<MeshCollider>().sharedMesh = m_container.GetComponent<MeshCollider>().sharedMesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = m_container.GetComponent<MeshRenderer>().sharedMaterial;
        }

        public override void Execute()
        {
            if (!m_spline)
            {
                Debug.LogError(THIS_NAME + $"{nameof(Spline)} is null !");
                return;
            }

            if (!m_element)
            {
                Debug.LogError(THIS_NAME + $"{nameof(MeshElement)} is null !");
                return;
            }

            if (!m_container)
            {
                Debug.LogError(THIS_NAME + $"{nameof(MeshContainer)} is null !");
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

            m_container.mesh = combinedMesh;
            m_container.collision = m_collision;
            m_container.collider.sharedMesh = m_collision && (combinedMesh != null) ? combinedMesh : null;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.EditorUtility.SetDirty(m_container);
            }
#endif
        }

        private IEnumerator<Mesh> CreateArrayMeshTask()
        {
            if (GeneratePlaneAlongToSpline(m_anchorAxis, m_zUp, m_flipNormal, m_flipUp, m_flipTangent, m_spacing, m_arrayMode, out var splinePoints, out var planeVerts, out var planeUVs, out var planeTris))
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
