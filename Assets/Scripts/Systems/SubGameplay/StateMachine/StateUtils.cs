using System;
using System.Runtime.CompilerServices;
using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Systems.SubGameplay.Movement;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Systems.SubGameplay.StateMachine
{
    public struct StateUtils
    {
        public static void SwitchState(ref BasicStateData stateData, EntityCommandBuffer.ParallelWriter ecb,
            Entity entity, int index)
        {
            
            if (stateData.TargetState == stateData.CurState) return;
            switch (stateData.TargetState)
            {
                case InteractState.Idle:
                {
                    stateData.TargetEntity = Entity.Null;
                    stateData.Focus = false;
                    ecb.SetComponentEnabled<IdleStateTag>(index, entity, true);
                    break;
                }
                case InteractState.Attacking:
                {
                    ecb.SetComponentEnabled<AttackStateTag>(index, entity, true);
                    break;
                }
                case InteractState.Moving:
                {
                    ecb.SetComponentEnabled<MovingStateTag>(index, entity, true);
                    break;
                }
                case InteractState.Garrison:
                {
                    ecb.SetComponentEnabled<GarrisonStateTag>(index, entity, true);
                    break;
                }
                case InteractState.Harvesting:
                {
                    ecb.SetComponentEnabled<HarvestStateTag>(index, entity, true);
                    break;
                }
                case InteractState.Healing:
                {
                    ecb.SetComponentEnabled<HealStateTag>(index, entity, true);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            switch (stateData.CurState)
            {
                case InteractState.Idle:
                {
                    ecb.SetComponentEnabled<IdleStateTag>(index, entity, false);
                    break;
                }
                case InteractState.Attacking:
                {
                    ecb.SetComponentEnabled<AttackStateTag>(index, entity, false);
                    break;
                }
                case InteractState.Moving:
                {
                    ecb.SetComponentEnabled<MovingStateTag>(index, entity, false);
                    break;
                }
                case InteractState.Garrison:
                {
                    ecb.SetComponentEnabled<GarrisonStateTag>(index, entity, false);
                    break;
                }
                case InteractState.Harvesting:
                {
                    ecb.SetComponentEnabled<HarvestStateTag>(index, entity, false);
                    break;
                }
                case InteractState.Healing:
                {
                    ecb.SetComponentEnabled<HealStateTag>(index, entity, false);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            stateData.CurState = stateData.TargetState;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetTargetStateViaTargetType(in FactionTag selfFactionTag,
            in SubGameplayGeneralAttr targetgeneralAttr, ref BasicStateData selfStateData)
        {
            if (selfFactionTag == targetgeneralAttr.Faction)
                selfStateData.TargetState = InteractState.Healing;
            if (targetgeneralAttr.BaseTag == BaseTag.Resources)
                selfStateData.TargetState = InteractState.Harvesting;
            if (selfFactionTag == ~targetgeneralAttr.Faction)
                selfStateData.TargetState = InteractState.Attacking;
        }


        public static void GarrisonMoveBack(in InGarrison garrison, 
            ref BasicStateData stateData,
            ref MovableData movableData, 
            in float3 buildingPos,
            in float3 buildingColliderSize,
            float garrisonRadiusSq,
            bool focus,
            Entity entity,int index,
            EntityCommandBuffer.ParallelWriter ecb)
        {
            MovementUtils.SetMoveTarget(ref movableData, buildingPos, buildingColliderSize,
                MovementCommandType.Interactive, garrisonRadiusSq);
            stateData.TargetState = InteractState.Moving;
            SwitchState(ref stateData, ecb, entity, index);
            stateData.TargetEntity = garrison.BuildingEntity;
            stateData.TargetState = InteractState.Garrison;
            stateData.Focus = focus;
        }
    }
}