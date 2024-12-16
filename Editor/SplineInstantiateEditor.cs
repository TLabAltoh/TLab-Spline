using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace TLab.Spline.Editor
{
    [CustomEditor(typeof(SplineInstantiate))]
    public class SplineInstantiateEditor : UnityEditor.Editor
    {
        private SplineInstantiate m_base;

        private bool m_toggle = false;

        private ReorderableList m_ranges = null;

        protected virtual void DrawActionGUI()
        {
            EditorGUILayout.BeginHorizontal();

            var width = GUILayout.Width(Screen.width / 3);

            if (GUILayout.Button("Update", width))
                m_base.UpdateWithCurrentSpline();

            EditorGUILayout.EndHorizontal();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawProperty("m_" + nameof(m_base.spline));
            DrawProperty(nameof(m_base.autoUpdate));

            DrawProperty("m_" + nameof(m_base.items));
            DrawProperty("m_" + nameof(m_base.anchorAxis));
            DrawProperty("m_" + nameof(m_base.zUp));

            DrawProperty("m_" + nameof(m_base.flipY));
            DrawProperty("m_" + nameof(m_base.flipZ));

            DrawProperty("m_" + nameof(m_base.skip));
            DrawProperty("m_" + nameof(m_base.spacing));
            DrawProperty("m_" + nameof(m_base.slideOffset));

            DrawProperty("m_" + nameof(m_base.scale));
            DrawMinMaxProperty("m_" + nameof(m_base.ranges), "Ranges");

            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();

            DrawActionGUI();
        }

        protected virtual void DrawMinMaxProperty(string name, string dispName)
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
                    m_toggle = true;
            }
            else
            {
                if (GUILayout.Button(EditorGUIUtility.TrIconContent("animationvisibilitytoggleoff"), width))
                    m_toggle = false;
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
                m_ranges.DoLayoutList();
        }

        protected virtual void DrawProperty(string name)
        {
            SerializedProperty prop = serializedObject.FindProperty(name);

            EditorGUILayout.PropertyField(prop);
        }

        protected virtual void OnEnable() => m_base = target as SplineInstantiate;
    }
}
