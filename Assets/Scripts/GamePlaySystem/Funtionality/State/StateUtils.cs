using System;
using System.Runtime.CompilerServices;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Movement;
using Unity.Entities;

namespace SparFlame.GamePlaySystem.State
{
    public struct StateUtils
    {
        public static void SwitchState(ref BasicStateData stateData,  EntityCommandBuffer.ParallelWriter ecb, Entity entity,int index)
        {
            // Debug.Log($"Cur state : {stateData.CurState}, Target state : {stateData.TargetState}");
            if(stateData.TargetState == stateData.CurState)return;
            switch (stateData.TargetState)
            {
                case UnitState.Idle:
                {
                    ecb.SetComponentEnabled<IdleStateTag>(index,entity,true);
                    break;
                }
                case UnitState.Attacking:
                {
                    ecb.SetComponentEnabled<AttackStateTag>(index,entity,true);
                    break;
                }
                case UnitState.Moving:
                {
                    ecb.SetComponentEnabled<MovingStateTag>(index,entity,true);
                    break;
                }
                case UnitState.Garrison:
                {
                    ecb.SetComponentEnabled<GarrisonStateTag>(index,entity,true);
                    break;
                }
                case UnitState.Harvesting:
                {
                    ecb.SetComponentEnabled<HarvestStateTag>(index,entity,true);
                    break;
                }
                case UnitState.Healing:
                {
                    ecb.SetComponentEnabled<HealStateTag>(index,entity,true);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            switch (stateData.CurState)
            {
                case UnitState.Idle:
                {
                    ecb.SetComponentEnabled<IdleStateTag>(index,entity,false);
                    break;
                }
                case UnitState.Attacking:
                {
                    ecb.SetComponentEnabled<AttackStateTag>(index,entity,false);
                    break;
                }
                case UnitState.Moving:
                {
                    ecb.SetComponentEnabled<MovingStateTag>(index,entity,false);
                    break;
                }
                case UnitState.Garrison:
                {
                    ecb.SetComponentEnabled<GarrisonStateTag>(index,entity,false);
                    break;
                }
                case UnitState.Harvesting:
                {
                    ecb.SetComponentEnabled<HarvestStateTag>(index,entity,false);
                    break;
                }
                case UnitState.Healing:
                {
                    ecb.SetComponentEnabled<HealStateTag>(index,entity,false);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
            stateData.CurState = stateData.TargetState;
        }
    

        
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetTargetStateViaTargetType(in FactionTag selfFactionTag,
            in InteractableAttr targetInteractAttr,ref BasicStateData selfStateData)
        {
            if (selfFactionTag == targetInteractAttr.FactionTag)
                selfStateData.TargetState = UnitState.Healing;
            if (targetInteractAttr.BaseTag == BaseTag.Resources)
                selfStateData.TargetState = UnitState.Harvesting;
            if (selfFactionTag == ~targetInteractAttr.FactionTag)
                selfStateData.TargetState = UnitState.Attacking;
        }
    }
}