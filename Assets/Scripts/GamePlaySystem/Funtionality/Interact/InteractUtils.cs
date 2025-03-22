using System.Runtime.CompilerServices;
using SparFlame.GamePlaySystem.General;

namespace SparFlame.GamePlaySystem.Interact
{
    
    public struct InteractUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsTargetValid(in FactionTag targetFaction, in FactionTag selfFactionTag, in StatData statData)
        {
            // Target is dead or not valid
            if (statData.CurValue <= 0)
            {
                return false;
            }
            // Healing state but target stat is already full. If it is in healing state, target should be ally unit, this logic is determined by Auto Choose System
            return targetFaction != selfFactionTag || statData.CurValue < statData.MaxValue;
        }
    }
}