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

        private static Path m_clipboard;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawProperty("m_autoUpdate");
            DrawProperty("m_curveMode");
            DrawProperty("m_arrayMode");

            DrawProperty("m_zUp");
            DrawProperty("m_space");

            DrawProperty("m_skip");
            DrawProperty("m_offset");
            DrawProperty("m_scale");
            DrawProperty("m_element");
            DrawProperty("m_collision");

            DrawProperty("m_length");

            DrawProperty("m_terrains");
            DrawProperty("m_fitRatio");
            DrawProperty("m_terrainFit");

            DrawMinMaxProperty("m_range", "Split Range");

            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.BeginHorizontal();

            var width = GUILayout.Width(Screen.width / 3);

            if (GUILayout.Button("Update", width))
            {
                m_instance.UpdateRoad();
            }

            if (GUILayout.Button("Export", width))
            {
                m_instance.Export();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Copy Path", width))
            {
                var creator = m_instance.GetComponent<PathCreator>();

                m_clipboard = new Path(creator.path);
            }

            if (GUILayout.Button("Paste Path", width))
            {
                m_instance.CopyPath(m_clipboard);

                m_clipboard = null;

                EditorUtility.SetDirty(m_instance);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Expanded", width))
            {
                m_instance.ExpandedByRandomChild();
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

            m_ranges.onAddCallback = (list) =>
            {
                prop.InsertArrayElementAtIndex(list.index);

                SerializedProperty element = prop.GetArrayElementAtIndex(list.index);

                element.vector2Value = new Vector2(0f, 1f);
            };

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
