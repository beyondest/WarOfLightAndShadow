using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SparFlame.Core.Interfaces;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.Core.Utils
{
    public static class PointDataUtils
    {
        /// <summary>
        /// E.g. cur wave point is 3, settings is 2, 4, 6, then cur point data is data of settings 4
        /// </summary>
        /// <param name="curPoint"></param>
        /// <param name="buffer"></param>
        /// <typeparam name="TPointData"></typeparam>
        /// <typeparam name="TData"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TData GetPointData<TPointData, TData>(float curPoint, DynamicBuffer<TPointData> buffer)
            where TPointData : unmanaged, IPointsData<TData>
            where TData : unmanaged
        {
            foreach (var t in buffer)
            {
                if (t.Points > curPoint)
                {
                    return t.Value;
                }
            }

            return buffer[buffer.Length - 1].Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TData GetPointData<TPointData, TData>(float curTimeFromGameStart, IList<TPointData> buffer)
            where TPointData : unmanaged, IPointsData<TData>
            where TData : unmanaged
        {
            foreach (var t in buffer)
            {
                if (t.Points > curTimeFromGameStart)
                {
                    return t.Value;
                }
            }

            return buffer[buffer.Count - 1].Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetPoint(int curPoint, NativeList<int> list)
        {
            foreach (var t in list)
            {
                if (t > curPoint)
                {
                    return t;
                }
            }

            return list[list.Length - 1];
        }
    }
}