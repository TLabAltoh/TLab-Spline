using UnityEngine;

namespace TLab.Spline
{
    [RequireComponent(typeof(Spline))]
    public class SplineCreator : MonoBehaviour
    {
        public Spline spline;

        public enum HandleType
        {
            Position,
            FreeMove
        };

        [System.Serializable]
        public class HandleSettings
        {
            public Color col;
            public float diameter;
            public HandleType handleType;

            public HandleSettings(Color col, float diameter, HandleType handleType)
            {
                this.col = col;
                this.diameter = diameter;
                this.handleType = handleType;
            }

            public HandleSettings()
            {

            }
        }

        public HandleSettings anchor = new HandleSettings(Color.red, 1.5f, HandleType.Position);
        public HandleSettings control = new HandleSettings(Color.white, 1.5f, HandleType.FreeMove);

        [Header("Options")]
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
