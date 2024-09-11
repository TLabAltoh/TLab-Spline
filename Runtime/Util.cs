using System.Collections.Generic;
using UnityEngine;

namespace TLab.Spline.Util
{
    public static class Extention
    {
        public static T GetRandom<T>(this List<T> @params)
        {
            return @params[Random.Range(0, @params.Count)];
        }

        public static void RemoveComponent<T>(this GameObject go) where T : Component
        {
            var tmp = go.GetComponent<T>();
            if (tmp)
                Object.DestroyImmediate(tmp);
        }

        public static T GetAddComponent<T>(this GameObject go) where T : Component
        {
            var tmp = go.GetComponent<T>();
            if (!tmp)
                tmp = go.AddComponent<T>();

            return tmp;
        }
    }

    public static class CSUtil
    {
        public static void GraphicsBuffer(ref GraphicsBuffer graphics_buffer, GraphicsBuffer.Target target, int count, int stride)
        {
            if (graphics_buffer != null)
            {
                DisposeBuffer(ref graphics_buffer);
            }

            graphics_buffer = new GraphicsBuffer(target, count, stride);
        }

        public static void DisposeBuffer(ref GraphicsBuffer graphics_buffer)
        {
            if (graphics_buffer != null)
            {
                graphics_buffer.Release();
                graphics_buffer.Dispose();
            }
        }

        public static void GetDispatchGroupSize(ComputeShader shader,
            int kernelIndex,
            int dispatchSizeX, int dispatchSizeY, int dispatchSizeZ,
            out int groupSizeX, out int groupSizeY, out int groupSizeZ)
        {
            shader.GetKernelThreadGroupSizes(kernelIndex, out uint xDim, out uint yDim, out uint zDim);

            groupSizeX = (int)(dispatchSizeX / xDim) + (int)(dispatchSizeX % xDim);
            groupSizeY = (int)(dispatchSizeY / yDim) + (int)(dispatchSizeY % yDim);
            groupSizeZ = (int)(dispatchSizeZ / zDim) + (int)(dispatchSizeZ % zDim);
        }

        public static void Dispatch(ComputeShader shader, int kernelIndex,
            int groupSizeX, int groupSizeY, int groupSizeZ)
        {
            shader.Dispatch(kernelIndex, groupSizeX, groupSizeY, groupSizeZ);
        }
    }
}
