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
            public Color color;
            public float diameter;
            public HandleType handleType;

            public HandleSettings() { }

            public HandleSettings(Color color, float diameter, HandleType handleType)
            {
                this.color = color;
                this.diameter = diameter;
                this.handleType = handleType;
            }
        }

        public HandleSettings anchor = new HandleSettings(Color.red, 1.5f, HandleType.Position);
        public HandleSettings control = new HandleSettings(Color.white, 1.5f, HandleType.FreeMove);

        [Header("Options")]
        public bool displayAnchorPoints = true;
        public bool displayControlPoints = true;
        public Color segmentColor = Color.green;

        [Header("Create New Options")]
        public Primitive.PrimitiveType initPrimitiveType;
        [Min(0)] public float initSize = 1f;
        [Min(2)] public int initSegmentNum = 5;

        public void CreatePath()
        {
            spline = GetComponent<Spline>();
            spline.Init(initPrimitiveType, initSegmentNum, initSize);
        }

        private void Reset()
        {
            CreatePath();
        }
    }
}
