using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace TLab.CurveTool.Editor
{
    [CustomEditor(typeof(CurveTool))]
    public class CurveToolEditor : UnityEditor.Editor
    {
        private CurveTool m_instance;

        private bool m_toggle = false;

        private ReorderableList m_ranges = null;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawProperty("m_autoUpdate");
            DrawProperty("m_curveMode");

            DrawProperty("m_zUp");
            DrawProperty("m_space");

            DrawProperty("m_offset");
            DrawProperty("m_scale");
            DrawProperty("m_element");
            DrawProperty("m_collision");

            DrawProperty("m_terrains");
            DrawProperty("m_fitCurve");
            DrawProperty("m_fitRatio");
            DrawProperty("m_terrainFit");

            DrawMinMaxProperty("m_range", "Range");

            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Update"))
            {
                m_instance.UpdateRoad();
            }

            if (GUILayout.Button("Export"))
            {
                m_instance.Export();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawMinMaxProperty(string name, string dispName)
        {
            SerializedProperty prop = serializedObject.FindProperty(name);

            if (m_ranges == null)
            {
                m_ranges = new ReorderableList(serializedObject, prop);
            }

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField($"{dispName}");

            GUILayout.FlexibleSpace();

            GUILayoutOption width = GUILayout.Width(50);

            if (!m_toggle)
            {
                if (GUILayout.Button(EditorGUIUtility.TrIconContent("animationvisibilitytoggleon"), width))
                {
                    m_toggle = true;
                }
            }
            else
            {
                if (GUILayout.Button(EditorGUIUtility.TrIconContent("animationvisibilitytoggleoff"), width))
                {
                    m_toggle = false;
                }
            }

            EditorGUILayout.EndHorizontal();

            m_ranges.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                SerializedProperty element = prop.GetArrayElementAtIndex(index);

                Vector2 current = element.vector2Value;

                float rectangleWidth = rect.width;
                float rectangleXMin = rect.xMin;
                float minValueWidth = rect.width * 0.15f;
                float margin = minValueWidth * 0.1f;

                rect.width = minValueWidth - margin;
                rect.xMin = rectangleXMin;

                current.x = EditorGUI.FloatField(rect, current.x);

                rect.xMin = rectangleXMin + rect.width + margin;
                rect.width = rectangleWidth - minValueWidth * 2;

                EditorGUI.MinMaxSlider(rect, ref current.x, ref current.y, 0f, 1f);

                rect.xMin = rect.xMin + rect.width + margin;
                rect.width = minValueWidth - margin;

                current.y = EditorGUI.FloatField(rect, current.y);

                element.vector2Value = current;
            };

            m_ranges.headerHeight = 0f;

            if (m_toggle)
            {
                m_ranges.DoLayoutList();
            }
        }

        private void DrawProperty(string name)
        {
            SerializedProperty prop = serializedObject.FindProperty(name);

            EditorGUILayout.PropertyField(prop);
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
