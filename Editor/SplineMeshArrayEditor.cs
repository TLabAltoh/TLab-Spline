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

            var width = GUILayout.Width(Screen.width / 4);

            if (GUILayout.Button("Update", width))
                m_instance.Execute();

            if (GUILayout.Button("Export", width))
                m_instance.Export();

            if (GUILayout.Button("Clear Mesh", width))
                m_instance.ClearMesh();

            EditorGUILayout.EndHorizontal();
        }

        protected override void DrawCustomProp()
        {
            DrawProperty("m_" + nameof(m_instance.element));
            DrawProperty("m_" + nameof(m_instance.container));
            DrawProperty("m_" + nameof(m_instance.collision));
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_instance = target as SplineMeshArray;
        }
    }
}
