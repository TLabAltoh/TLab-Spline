using UnityEngine;

namespace TLab.Spline
{
    [RequireComponent(typeof(Spline))]
    public class SplineCreator : MonoBehaviour
    {
        public Spline spline;

        public enum HandleType
        {
            POSITION,
            FREE_MOVE
        };

        [Header("Anchor Handle Settings")]
        public Color anchorCol = Color.red;
        public float anchorDiameter = 0.1f;
        public HandleType anchorHandle = HandleType.POSITION;

        [Header("Control Handle Settings")]
        public Color controlCol = Color.white;
        public float controlDiameter = 0.75f;
        public HandleType controlHandle = HandleType.FREE_MOVE;

        [Header("Other Settings")]
        public bool displayControlPoints = true;
        public Color segmentCol = Color.green;

        public void CreatePath()
        {
            spline = GetComponent<Spline>();
            spline.Init(transform.position);
        }

        private void Reset()
        {
            CreatePath();
        }
    }
}
