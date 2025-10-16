using Unity.Mathematics;

namespace SparFlame.Systems.SubGameplay.Movement.Deprecated
{
    public class Utils
    {
        // 魔法数字
        public static float FastInverseSqrt(float x) {
            uint i = math.asuint(x);
            i = 0x5f3759df - (i >> 1); 
            return math.asfloat(i);
        }
    }
}