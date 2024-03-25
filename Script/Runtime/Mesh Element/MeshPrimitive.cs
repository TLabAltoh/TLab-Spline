using UnityEngine;

namespace TLab.MeshEngine
{
    public class MeshPrimitive : MeshElement
    {
        [SerializeField] private Mesh m_mesh;

        public override void GetMeshInfo(out Vector3[] vertices, out Vector2[] uv, out int[] triangles)
        {
            vertices = m_mesh.vertices;
            uv = m_mesh.uv;
            triangles = m_mesh.triangles;
        }

        public override void GetMesh(out Mesh mesh, string name = "")
        {
            mesh = m_mesh;
            mesh.name = name;
        }

        public override void GetBounds(out Bounds bounds)
        {
            bounds = m_mesh.bounds;
        }
    }
}
