using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class MeshContainer : MonoBehaviour
{
    private MeshFilter m_filter;
    private MeshCollider m_collider;

    public bool collision
    {
        get => m_collider.enabled;
        set => m_collider.enabled = value;
    }

    public Mesh mesh
    {
        get => m_filter.sharedMesh;
        set => m_filter.sharedMesh = value;
    }

    public Mesh colliderMesh
    {
        get => m_collider.sharedMesh;
        set => m_collider.sharedMesh = value;
    }

    public void Awake()
    {
        m_filter = GetComponent<MeshFilter>();
        m_collider = GetComponent<MeshCollider>();
    }
}
