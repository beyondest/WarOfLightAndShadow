using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.Building
{
    public struct PlacementUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SnapToNearest15(float currentDeg, float direction)
        {
            currentDeg = (currentDeg % 360 + 360) % 360;
            var lower = math.floor(currentDeg / 15f) * 15f;
            var upper = lower + 15f;
            return direction > 0 ? upper - currentDeg : lower - 15 - currentDeg;
        }

        public static float GetCurrentYDeg(quaternion q)
        {
            var siny_cosp = 2f * (q.value.w * q.value.y + q.value.x * q.value.z);
            var cosy_cosp = 1f - 2f * (q.value.y * q.value.y + q.value.x * q.value.x);
            var yRadians = math.atan2(siny_cosp, cosy_cosp);
            return math.degrees(yRadians);
        }
    }
}