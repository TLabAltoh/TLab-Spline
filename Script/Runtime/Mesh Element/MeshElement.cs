using UnityEngine;

namespace TLab.MeshEngine
{
    public class MeshElement : MonoBehaviour
    {
        [SerializeField] private Vector2 m_boundOffsetX;
        [SerializeField] private Vector2 m_boundOffsetY;
        [SerializeField] private Vector2 m_boundOffsetZ;

#if UNITY_EDITOR
        [Header("Editor Property")]

        [HideInInspector] public Mesh cash;

        public bool draw = false;
#endif

        public virtual void GetMeshInfo(
            out Vector3[] vertices, out Vector2[] uv, out int[] triangles)
        {
            vertices = new Vector3[4];
            uv = new Vector2[vertices.Length];
            triangles = new int[(vertices.Length - 2) * 3];

            vertices[0] = new Vector3(-0.5f, 0.0f, -0.5f);
            vertices[1] = new Vector3(-0.5f, 0.0f, 0.5f);
            vertices[2] = new Vector3(0.5f, 0.0f, 0.5f);
            vertices[3] = new Vector3(0.5f, 0.0f, -0.5f);

            uv[0] = new Vector2(0.0f, 0.0f);
            uv[1] = new Vector2(0.0f, 1.0f);
            uv[2] = new Vector2(1.0f, 1.0f);
            uv[3] = new Vector2(1.0f, 0.0f);

            triangles[0] = 0;
            triangles[1] = 1;
            triangles[2] = 2;

            triangles[3] = 0;
            triangles[4] = 2;
            triangles[5] = 3;
        }

        public virtual void GetMesh(out Mesh mesh, string name = "")
        {
            GetMeshInfo(out var vertices, out var uv, out var triangles);

            mesh = new Mesh();
            mesh.name = name;

            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;

            mesh.RecalculateNormals();
        }

        public virtual void GetBounds(out Bounds bounds)
        {
            GetMesh(out var mesh);

            var dummy = mesh.bounds;

            dummy.max = dummy.max + Vector3.right * m_boundOffsetX.y;
            dummy.max = dummy.max + Vector3.up * m_boundOffsetY.y;
            dummy.max = dummy.max + Vector3.forward * m_boundOffsetZ.y;

            dummy.min = dummy.min - Vector3.right * m_boundOffsetX.x;
            dummy.min = dummy.min - Vector3.up * m_boundOffsetY.x;
            dummy.min = dummy.min - Vector3.forward * m_boundOffsetZ.x;

            bounds = dummy;
        }

        public virtual GameObject Instantiate(
            Vector3 position, Quaternion rotation,
            bool collision = false, string name = "", Transform parent = null)
        {
            GetMesh(out var mesh);

            var go = new GameObject(name);

            go.transform.parent = parent;

            var filter = go.AddComponent<MeshFilter>();

            filter.sharedMesh = mesh;

            var renderer = go.AddComponent<MeshRenderer>();

            if (collision)
            {
                var collider = go.AddComponent<MeshCollider>();

                collider.sharedMesh = mesh;
            }

            go.transform.position = position;
            go.transform.rotation = rotation;

            return go;
        }

#if UNITY_EDITOR

        public virtual void Cash()
        {
            GetMesh(out cash);
        }

        public virtual void OnDrawGizmosSelected()
        {
            if (draw && cash != null)
            {
                Gizmos.color = Color.green;

                var cache = Gizmos.matrix;

                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.DrawWireCube(cash.bounds.center, cash.bounds.size);

                Gizmos.matrix = cache;
            }
        }
#endif
    }
}
