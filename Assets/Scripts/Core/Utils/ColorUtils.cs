using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.Core.Utils
{
    public static class ColorUtils
    {
        public static float4 ToFloat4(this Color c)
        {
            return new float4(c.r, c.g, c.b, c.a);
        }
    }
}