using UnityEngine;
using UnityEditor;

namespace TLab.CurveTool.Editor
{
    [CustomEditor(typeof(CurveTool))]
    public class CurveToolEditor : UnityEditor.Editor
    {
        private CurveTool m_instance;

        public override void OnInspectorGUI()
        {
            base.DrawDefaultInspector();

            if (GUILayout.Button("Update"))
            {
                m_instance.UpdateRoad();
            }
        }

        void OnSceneGUI()
        {
            if (m_instance.autoUpdate && Event.current.type == EventType.Repaint)
            {
                m_instance.UpdateRoad();
            }
        }

        void OnEnable()
        {
            m_instance = (CurveTool)target;
        }
    }
}
