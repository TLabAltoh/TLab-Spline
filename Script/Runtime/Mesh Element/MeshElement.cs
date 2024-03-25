using UnityEngine;

namespace TLab.MeshEngine
{
    public class MeshElement : MonoBehaviour
    {
        [SerializeField] private Vector2 m_boundOffsetX;
        [SerializeField] private Vector2 m_boundOffsetY;
        [SerializeField] private Vector2 m_boundOffsetZ;

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

            var dummy = new Bounds();

            dummy.max = mesh.bounds.max + Vector3.right * m_boundOffsetX.y;
            dummy.max = mesh.bounds.max + Vector3.up * m_boundOffsetY.y;
            dummy.max = mesh.bounds.max + Vector3.forward * m_boundOffsetZ.y;

            dummy.min = mesh.bounds.min - Vector3.right * m_boundOffsetX.x;
            dummy.min = mesh.bounds.min - Vector3.up * m_boundOffsetY.x;
            dummy.min = mesh.bounds.min - Vector3.forward * m_boundOffsetZ.x;

            bounds = dummy;
        }
    }
}
