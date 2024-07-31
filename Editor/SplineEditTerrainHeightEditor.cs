using UnityEditor;
using UnityEngine;

namespace TLab.Spline.Editor
{
    [CustomEditor(typeof(SplineEditTerrainHeight))]
    public class SplineEditTerrainHeightEditor : SplinePlaneArrayEditor
    {
        private SplineEditTerrainHeight m_instance;

        protected override void DrawCustomProp()
        {
            DrawProperty(nameof(m_instance.terrains));
            DrawProperty(nameof(m_instance.brushStrength));
            DrawProperty(nameof(m_instance.csEditTerrainHeight));
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_instance = target as SplineEditTerrainHeight;
        }
    }
}
