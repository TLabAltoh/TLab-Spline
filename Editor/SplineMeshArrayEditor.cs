using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TLab.Spline.Editor
{
    [CustomEditor(typeof(SplineMeshArray))]
    public class SplineMeshArrayEditor : SplinePlaneArrayEditor
    {
        private SplineMeshArray m_instance;

        protected override void DrawActionGUI()
        {
            EditorGUILayout.BeginHorizontal();

            var width = GUILayout.Width(Screen.width / 3);

            if (GUILayout.Button("Update", width))
            {
                m_instance.UpdateWithCurrentSpline();
            }

            if (GUILayout.Button("Export", width))
            {
                m_instance.Export();
            }

            EditorGUILayout.EndHorizontal();
        }

        protected override void DrawCustomProp()
        {
            DrawProperty("m_" + nameof(m_instance.meshElement));
            DrawProperty("m_" + nameof(m_instance.meshHolder));
            DrawProperty("m_" + nameof(m_instance.slideOffset));
            DrawProperty("m_" + nameof(m_instance.skip));
            DrawProperty("m_" + nameof(m_instance.collision));
            DrawMinMaxProperty("m_" + nameof(m_instance.range), "Range");
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_instance = target as SplineMeshArray;
        }
    }
}
