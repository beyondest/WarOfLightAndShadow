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

        public static bool NoDupAdd(ref DynamicBuffer<InsightTarget> targets, InsightTarget insightTarget)
        {
            int j;
            for ( j= 0; j < targets.Length; j++)
            {
                if(targets[j].Entity == insightTarget.Entity)break;
            }
            if (j != targets.Length) return false;
            targets.Add(insightTarget);
            return true;
        }

        public static bool MemoryTarget(ref DynamicBuffer<InsightTarget> targets, Entity targetEntity,
            float memoryValue)
        {
            if(memoryValue != 0)
            {
                // Memory target by assign high memory value
                for (int i = 0; i < targets.Length; i++)
                {
                    if (targets[i].Entity == targetEntity)
                    {
                        var insightTarget = targets[i];
                        insightTarget.MemoryValue = memoryValue;
                        targets[i] = insightTarget;
                    }
                }
                return true;
            }
            return false;
        }
        
                
        public static Entity ChooseTarget(in DynamicBuffer<InsightTarget> targets)
        {
            // Because the value might be negative when priority or override settings so
            var maxValue = -1e10f;
            var bestTarget = Entity.Null;
            foreach (var target in targets)
            {
                if (target.TotalValue >= maxValue)
                {
                    maxValue = target.TotalValue;
                    bestTarget = target.Entity;
                }
            }
            return bestTarget;            

        }
        
        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public static float3 GetRandomOffset(ref Random random, float scale)
        // {
        //     var randomDir = math.normalize(random.NextFloat3(-1f, 1f)); 
        //     return randomDir * scale; 
        // }
    }
}