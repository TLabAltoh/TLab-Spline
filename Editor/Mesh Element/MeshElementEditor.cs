using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TLab.MeshEngine.Editor
{
    [CustomEditor(typeof(MeshElement))]
    public class MeshElementEditor : UnityEditor.Editor
    {
        private MeshElement m_instance;

        private void OnEnable()
        {
            m_instance = target as MeshElement;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var width = GUILayout.Width(Screen.width / 3);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Instantiate", width))
            {
                m_instance.Instantiate(
                    m_instance.transform.position,
                    m_instance.transform.rotation, false, nameof(MeshElement));
            }

            if (GUILayout.Button("Cash", width))
            {
                m_instance.Cash();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
