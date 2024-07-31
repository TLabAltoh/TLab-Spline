using System.Runtime.InteropServices;
using UnityEngine;
using TLab.Spline.Util;

namespace TLab.Spline
{
    public class SplineEditTerrainHeight : SplinePlaneArray
    {
        [Header("Edit Terrain Height")]
        public Terrain[] terrains;
        public AnimationCurve brushStrength;
        public ComputeShader csEditTerrainHeight;

        private string THIS_NAME => "[" + this.GetType() + "] ";

        internal static int PROP_DISPATCH_GROUP_SIZE = Shader.PropertyToID("_DispatchGroupSize");
        internal static int PROP_PLANES = Shader.PropertyToID("_Planes");
        internal static int PROP_TERRAIN_PIXELS = Shader.PropertyToID("_TerrainPixels");

        private struct TerrainPixel
        {
            public Vector3 position;
            public Vector2 uv;
        };

        private struct Triangle
        {
            public Vector3 vert0;
            public Vector3 vert1;
            public Vector3 vert2;

            public Vector2 uv0;
            public Vector2 uv1;
            public Vector2 uv2;
        };

        private struct Plane
        {
            public Triangle triangle0;
            public Triangle triangle1;
        };

        public override void UpdateWithCurrentSpline()
        {
            if (!m_spline)
            {
                Debug.LogError(THIS_NAME + "spline is null !");
                return;
            }

            if (!csEditTerrainHeight)
            {
                Debug.LogError(THIS_NAME + "terrain fit is null !");
                return;
            }

            if (m_spline.CalculateEvenlySpacedPoints(out var points, m_space))
            {
                GeneratePlaneAlongToSpline(points, m_spline.isClosed, m_arrayMode, out var verts, out var uvs, out var tris);

                var planes = new Plane[tris.Length / 6];

                for (int i = 0; i < planes.Length; i++)
                {
                    var offset = i * 6;

                    planes[i] = new Plane
                    {
                        triangle0 = new Triangle
                        {
                            vert0 = transform.TransformPoint(verts[tris[offset + 0]]),
                            vert1 = transform.TransformPoint(verts[tris[offset + 1]]),
                            vert2 = transform.TransformPoint(verts[tris[offset + 2]]),

                            uv0 = uvs[tris[offset + 0]],
                            uv1 = uvs[tris[offset + 1]],
                            uv2 = uvs[tris[offset + 2]]
                        },

                        triangle1 = new Triangle
                        {
                            vert0 = transform.TransformPoint(verts[tris[offset + 3]]),
                            vert1 = transform.TransformPoint(verts[tris[offset + 4]]),
                            vert2 = transform.TransformPoint(verts[tris[offset + 5]]),

                            uv0 = uvs[tris[offset + 3]],
                            uv1 = uvs[tris[offset + 4]],
                            uv2 = uvs[tris[offset + 5]]
                        },
                    };
                }

                GraphicsBuffer planesBuffer = null;
                CSUtil.GraphicsBuffer(ref planesBuffer, GraphicsBuffer.Target.Structured, planes.Length, Marshal.SizeOf<Plane>());
                planesBuffer.SetData(planes);

                csEditTerrainHeight.SetBuffer(0, PROP_PLANES, planesBuffer);

                for (int i = 0; i < terrains.Length; i++)
                {
                    var terrain = terrains[i];

                    var data = terrain.terrainData;

                    var resolution = data.heightmapResolution;

                    var heights = data.GetHeights(0, 0, resolution, resolution);

                    var terrainPixel = new TerrainPixel[resolution * resolution];

                    var xSpace = data.size.x / (resolution - 1);
                    var zSpace = data.size.z / (resolution - 1);

                    for (int row = 0; row < resolution; row++)
                    {
                        for (int col = 0; col < resolution; col++)
                        {
                            var offset = col * resolution + row;
                            terrainPixel[offset].position.x = terrain.transform.position.x + xSpace * row;
                            terrainPixel[offset].position.z = terrain.transform.position.z + zSpace * col;
                            terrainPixel[offset].position.y = heights[col, row] * data.size.y + terrain.transform.position.y;

                            // uv is not determined until the compute shader is passed.
                        }
                    }

                    GraphicsBuffer terrainPixelsBuffer = null;
                    CSUtil.GraphicsBuffer(ref terrainPixelsBuffer, GraphicsBuffer.Target.Structured, terrainPixel.Length, Marshal.SizeOf<TerrainPixel>());
                    terrainPixelsBuffer.SetData(terrainPixel);

                    csEditTerrainHeight.SetBuffer(0, PROP_TERRAIN_PIXELS, terrainPixelsBuffer);

                    CSUtil.GetDispatchGroupSize(csEditTerrainHeight, 0,
                        resolution, resolution, 1,
                        out int groupSizeX, out int groupSizeY, out int groupSizeZ);

                    csEditTerrainHeight.SetInts(PROP_DISPATCH_GROUP_SIZE, groupSizeX, groupSizeY, groupSizeZ);

                    CSUtil.Dispatch(csEditTerrainHeight, 0, groupSizeX, groupSizeY, groupSizeZ);

                    terrainPixelsBuffer.GetData(terrainPixel);

                    for (int row = 0; row < resolution; row++)
                    {
                        for (int col = 0; col < resolution; col++)
                        {
                            var offset = col * resolution + row;

                            var height0 = heights[col, row];
                            var height1 = terrainPixel[offset].position.y;

                            // 0 ~ 1 --> -1 ~ 1
                            var lerpRatio = brushStrength.Evaluate(1f - Mathf.Abs((terrainPixel[offset].uv.x % 1f - 0.5f) * 2f));
                            var lerpHeight = height1 * lerpRatio + height0 * (1f - lerpRatio);

                            heights[col, row] = Mathf.Clamp01((lerpHeight - terrain.transform.position.y) / data.size.y);
                        }
                    }

                    data.SetHeights(0, 0, heights);

                    CSUtil.DisposeBuffer(ref terrainPixelsBuffer);
                }

                CSUtil.DisposeBuffer(ref planesBuffer);
            }
        }
    }
}
