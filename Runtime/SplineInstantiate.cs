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
        [SerializeField] protected Spline.AnchorAxis m_anchorAxis;
        [SerializeField] protected bool m_zUp = true;
        [SerializeField] protected uint m_skip = 0;
        [SerializeField, Min(0)] protected float m_spacing = 0.5f;
        [SerializeField] protected float m_slideOffset = 0f;
        [SerializeField] protected Vector3 m_scale = new Vector3(1.0f, 1.0f, 1.0f);
        [SerializeField] protected Vector2[] m_ranges = new Vector2[1] { new Vector2(0, 1) };

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

        public float spacing
        {
            get => m_spacing;
            set
            {
                if (m_spacing != value)
                {
                    m_spacing = value;

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

        public Vector2[] ranges
        {
            get => m_ranges;
            set
            {
                if (m_ranges != value)
                {
                    m_ranges = value;

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

        public struct SplinePoint
        {
            public Vector3 forward;
            public Vector3 up;
            public Vector3 position;
        }

        public virtual void InstantiateAlongToSpline(Spline.AnchorAxis anchorAxis, bool zUp, uint skip, Vector2[] ranges, float spacing, List<GameObject> items)
        {
            if (m_spline.GetSplinePoints(out var splinePoints, anchorAxis, zUp, spacing))
            {
                foreach (var range in ranges)
                {
                    for (int i = (int)(range.x * splinePoints.Length); i < (int)(range.y * splinePoints.Length); i += (1 + (int)skip))
                    {
                        var left0 = splinePoints[i].normal;
                        left0.Normalize();

                        var left1 = splinePoints[(i + 1) % splinePoints.Length].normal;
                        left1.Normalize();

                        var left = (left0 + left1) * 0.5f;

                        var forward = (splinePoints[(i + 1) % splinePoints.Length].tangent + splinePoints[i].tangent) * 0.5f;
                        var up = (splinePoints[(i + 1) % splinePoints.Length].up + splinePoints[i].up) * 0.5f;
                        var position = (splinePoints[(i + 1) % splinePoints.Length].position + splinePoints[i].position) * 0.5f;

                        var instance = Instantiate(items.GetRandom());

                        instance.transform.position = position - left * m_slideOffset;
                        instance.transform.rotation = Quaternion.LookRotation(forward, up);
                        instance.transform.localScale = Vector3.Scale(instance.transform.localScale, m_scale);
                        instance.transform.parent = this.transform;

#if UNITY_EDITOR
                        UnityEditor.EditorUtility.SetDirty(instance);
#endif
                    }
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

            InstantiateAlongToSpline(m_anchorAxis, m_zUp, m_skip, m_ranges, m_spacing, m_items);
        }
    }
}
