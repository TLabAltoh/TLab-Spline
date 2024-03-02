using UnityEngine;

namespace TLab.CurveTool
{
    public class PathCreator : MonoBehaviour
    {
        [SerializeField, HideInInspector] public Path path;

        public enum HandleType
        {
            POSITION,
            FREE_MOVE
        };

        public Color anchorCol = Color.red;
        public Color controlCol = Color.white;
        public Color segmentCol = Color.green;
        public float anchorDiameter = 0.1f;
        public float controlDiameter = 0.75f;
        public HandleType anchorHandle = HandleType.POSITION;
        public HandleType controlHandle = HandleType.FREE_MOVE;
        public bool displayControlPoints = true;
        public bool displayPlane = false;

        public void CreatePath()
        {
            path = new Path(transform.position);
        }

        private void Reset()
        {
            CreatePath();
        }
    }
}
