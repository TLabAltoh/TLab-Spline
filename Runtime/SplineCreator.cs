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

        [System.Serializable]
        public class ArcHandleSettings
        {
            public Color color;
            public float radius;

            public ArcHandleSettings() { }

            public ArcHandleSettings(Color color, float radius)
            {
                this.color = color;
                this.radius = radius;
            }

            public ArcHandleSettings(Color color, float alpha, float radius)
            {
                this.color = new Color(color.r, color.g, color.b, alpha);
                this.radius = radius;
            }
        }

        [System.Serializable]
        public class DisplaySetting
        {
            public Color segmentColor = Color.green;
            public bool anchor = true;
            public bool control = true;
            public bool angle = true;

            public DisplaySetting() { }

            public DisplaySetting(Color segmentColor, bool anchor, bool control, bool angle)
            {
                this.segmentColor = segmentColor;
                this.anchor = anchor;
                this.control = control;
                this.angle = angle;
            }
        }

        public HandleSettings anchor = new HandleSettings(Color.red, 1.5f, HandleType.Position);
        public HandleSettings control = new HandleSettings(Color.white, 1.5f, HandleType.FreeMove);
        public ArcHandleSettings angle = new ArcHandleSettings(Color.yellow, 0.1f, 1.5f);

        public DisplaySetting displaySetting = new DisplaySetting();
        public Spline.InitOption initOption = new Spline.InitOption();

        public void CreatePath()
        {
            spline = GetComponent<Spline>();
            spline.Init(initOption);
        }

        private void Reset()
        {
            CreatePath();
        }
    }
}
