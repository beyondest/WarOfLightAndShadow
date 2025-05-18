using Unity.Collections;

namespace SparFlame.GamePlaySystem.Animation.GamePlaySystem.Core.Animation
{
    public struct AnimationUtils
    {
        public static int Hash(string str)
        {
            FixedString64Bytes string64 = str;
            return string64.GetHashCode();
        }
    }
}