using System;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace SparFlame.Core.Utils
{
    public static class NativeContainerUtils
    {
        /// <summary>
        /// 通用且安全：需要 T 实现 IEquatable&lt;T&gt;。
        /// Burst 兼容，无装箱、无 LINQ。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsEq<T>( NativeList<T> list, in T value)
            where T : unmanaged, IEquatable<T>
        {
            var len = list.Length;
            for (var i = 0; i < len; i++)
            {
                if (value.Equals(list[i]))
                    return true;
            }
            return false;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsEq<T>( FixedList128Bytes<T> list, in T value)
            where T : unmanaged, IEquatable<T>
        {
            var len = list.Length;
            for (var i = 0; i < len; i++)
            {
                if (value.Equals(list[i]))
                    return true;
            }
            return false;
        }
    }
}