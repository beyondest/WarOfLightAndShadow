using System;
using UnityEngine;

namespace SparFlame.Core.Utils
{
    public static class BurstSafe
    {
        public static void UnexpectedEnum<T>(T value) where T : Enum
        {
#if UNITY_EDITOR
            Debug.LogError($"Unexpected enum value: {value} ");
#endif
        }
        
        public static TReturn UnexpectedEnum<T,TReturn>(T value, TReturn defaultReturn) where T : Enum
        {
#if UNITY_EDITOR
            Debug.LogError($"Unexpected enum value: {value} ");
#endif
            return defaultReturn;
        }
    }

}