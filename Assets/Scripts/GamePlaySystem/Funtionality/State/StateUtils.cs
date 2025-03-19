using System;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Movement;
using Unity.Entities;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.State
{
    public struct StateUtils
    {
        public static void SwitchState(ref UnitBasicStateData stateData,  EntityCommandBuffer.ParallelWriter ecb, Entity entity,int index)
        {
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
    }
}