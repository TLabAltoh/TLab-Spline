using System.Collections.Generic;
using UnityEngine;
using static TLab.Spline.Util.Extention;

namespace TLab.Spline
{
    public class SplineInstantiate : MonoBehaviour
    {
        [SerializeField] protected Spline m_spline;
        public bool autoUpdate;

        [Header("Array Instantiate")]
        [SerializeField] protected List<GameObject> m_items;
        [SerializeField] protected bool m_zUp = true;
        [SerializeField] protected float m_space = 0.5f;
        [SerializeField] protected float m_slideOffset = 0f;
        [SerializeField] protected uint m_skip = 0;
        [SerializeField] protected Vector3 m_scale = new Vector3(1.0f, 1.0f, 1.0f);
        [SerializeField] protected Vector2[] m_range = new Vector2[1] { new Vector2(0, 1) };

        public Spline spline
        {
            get => m_spline;
            set
            {
                if (m_spline != value)
                {
                    m_spline = value;

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

        public List<GameObject> items
        {
            get => m_items;
            set
            {
                if (m_items != value)
                {
                    m_items = value;

                    RequestAutoUpdate();
                }
            }
        }

        public bool zUp
        {
            get => m_zUp;
            set
            {
                if (m_zUp != value)
                {
                    m_zUp = value;

                    RequestAutoUpdate();
                }
            }
        }

        public float space
        {
            get => m_space;
            set
            {
                if (m_space != value)
                {
                    m_space = value;

                    RequestAutoUpdate();
                }
            }
        }

        public Vector3 scale
        {
            get => m_scale;
            set
            {
                if (m_scale != value)
                {
                    m_scale = value;

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

        private string THIS_NAME => "[" + this.GetType() + "] ";

        public virtual void RequestAutoUpdate()
        {
            if (autoUpdate)
            {
                UpdateWithCurrentSpline();
            }
        }

        public virtual void InstantiateAlongToSpline(Vector3[] points, bool isClosed, List<GameObject> items)
        {
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
                    var pos0 = points[i];
                    var pos1 = points[(i + 1) % points.Length];

                    var forward = pos1 - pos0;
                    forward.Normalize();

                    var selected = items.GetRandom();

                    var instance = Instantiate(selected);

                    var left = new Vector3(-forward.z, m_zUp ? 0.0f : forward.y, forward.x);

                    instance.transform.position = (pos0 + pos1) * 0.5f - left * m_slideOffset;
                    instance.transform.rotation = Quaternion.LookRotation(forward, Vector3.Cross(left, forward));
                    instance.transform.localScale = Vector3.Scale(instance.transform.localScale, m_scale);
                    instance.transform.parent = this.transform;

#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(instance);
#endif
                }
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public void ClearChild()
        {
            while (this.transform.childCount > 0)
            {
                DestroyImmediate(this.transform.GetChild(0).gameObject);
            }
        }

        public virtual void UpdateWithCurrentSpline()
        {
            ClearChild();

            if (!m_spline)
            {
                Debug.LogError(THIS_NAME + "spline is null !");
                return;
            }

            if (m_items.Count == 0)
            {
                Debug.LogError(THIS_NAME + "item is empty !");
                return;
            }

            if (m_spline.CalculateEvenlySpacedPoints(out var points, m_space))
            {
                InstantiateAlongToSpline(points, m_spline.isClosed, m_items);
            }
        }
    }
}
