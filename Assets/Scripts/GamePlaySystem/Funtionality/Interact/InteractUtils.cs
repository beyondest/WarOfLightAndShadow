using System.Runtime.CompilerServices;
using SparFlame.GamePlaySystem.General;
using Unity.Entities;

namespace SparFlame.GamePlaySystem.Interact
{
    
    public struct InteractUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsTargetValid(in InteractableAttr targetInteractAttr, in FactionTag selfFactionTag, in StatData targetStatData,
            bool heal, bool harvest)
        {
            // Target is dead or not valid
            if (targetStatData.CurValue <= 0)
            {
                return false;
            }
            // Healing state but target stat is already full. If it is in healing state, target should be ally unit, this logic is determined by Auto Choose System
            if (targetInteractAttr.FactionTag == selfFactionTag)
            {
                if (!heal) return false;
                return targetStatData.CurValue < targetStatData.MaxValue;
            }
            // Harvest target
            if (targetInteractAttr.BaseTag == BaseTag.Resources)
            {
                return harvest;
            }
            // Faction tag not same, and base tag not resource, and hp > 0, must be valid target
            return true;
        }
        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public static float3 GetRandomOffset(ref Random random, float scale)
        // {
        //     var randomDir = math.normalize(random.NextFloat3(-1f, 1f)); 
        //     return randomDir * scale; 
        // }
    }
}