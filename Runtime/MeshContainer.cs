using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class MeshContainer : MonoBehaviour
{
    private MeshFilter m_filter;
    private MeshRenderer m_renderer;
    private MeshCollider m_collider;

    public Material[] materials
    {
        get => m_renderer.sharedMaterials;
        set
        {
            if (m_renderer.sharedMaterials != value)
            {
                m_renderer.sharedMaterials = value;
            }
        }
    }

    public Mesh mesh
    {
        get => m_filter.sharedMesh;
        set
        {
            if (m_filter.sharedMesh != value)
            {
                m_filter.sharedMesh = value;
            }
        }
    }

    public bool collision
    {
        get => m_collider.enabled;
        set
        {
            if (m_collider.enabled != value)
            {
                m_collider.enabled = value;
            }
        }
    }

    public MeshFilter filter => m_filter;

    public new MeshCollider collider => m_collider;

    public void Clear()
    {
        mesh = null;
        collision = false;
        collider.sharedMesh = null;
    }

    private void OnEnable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            m_filter = GetComponent<MeshFilter>();
            m_renderer = GetComponent<MeshRenderer>();
            m_collider = GetComponent<MeshCollider>();
        }
#endif
    }

    public void Awake()
    {
        m_filter = GetComponent<MeshFilter>();
        m_renderer = GetComponent<MeshRenderer>();
        m_collider = GetComponent<MeshCollider>();
    }
}
