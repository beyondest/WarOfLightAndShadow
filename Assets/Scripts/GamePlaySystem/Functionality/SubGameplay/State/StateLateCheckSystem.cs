using System;
using SparFlame.GamePlaySystem.Animation;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Movement;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.GamePlaySystem.State
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial struct StateLateCheckSystem : ISystem
    {
        private ComponentLookup<IdleStateTag> _idle;
        private ComponentLookup<AttackStateTag> _attack;
        private ComponentLookup<MovingStateTag> _moving;
        private ComponentLookup<GarrisonStateTag> _garrison;
        private ComponentLookup<HealStateTag> _heal;
        private ComponentLookup<HarvestStateTag> _harvest;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BasicStateData>();
            state.RequireForUpdate<SubGamingTag>();
            _idle = state.GetComponentLookup<IdleStateTag>();
            _attack = state.GetComponentLookup<AttackStateTag>();
            _moving = state.GetComponentLookup<MovingStateTag>();
            _garrison = state.GetComponentLookup<GarrisonStateTag>();
            _heal = state.GetComponentLookup<HealStateTag>();
            _harvest = state.GetComponentLookup<HarvestStateTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _harvest.Update(ref state);
            _heal.Update(ref state);
            _garrison.Update(ref state);
            _moving.Update(ref state);
            _attack.Update(ref state);
            _idle.Update(ref state);

            new StateLateCheckJob
            {
                Idle = _idle,
                Attack = _attack,
                Moving = _moving,
                Garrison = _garrison,
                Heal = _heal,
                Harvest = _harvest,
            }.ScheduleParallel();
        }


        [BurstCompile]
        [WithNone(typeof(UnitDeadTag))]
        public partial struct StateLateCheckJob : IJobEntity
        {
            [NativeDisableParallelForRestriction] public ComponentLookup<IdleStateTag> Idle;
            [NativeDisableParallelForRestriction] public ComponentLookup<AttackStateTag> Attack;
            [NativeDisableParallelForRestriction] public ComponentLookup<MovingStateTag> Moving;
            [NativeDisableParallelForRestriction] public ComponentLookup<GarrisonStateTag> Garrison;
            [NativeDisableParallelForRestriction] public ComponentLookup<HealStateTag> Heal;
            [NativeDisableParallelForRestriction] public ComponentLookup<HarvestStateTag> Harvest;

            private void Execute(ref BasicStateData state, Entity selfEntity
            )
            {

                switch (state.CurState)
                {
                    case InteractState.Idle:
                        Idle.SetComponentEnabled(selfEntity, true);
                        break;
                    case InteractState.Attacking:
                        Attack.SetComponentEnabled(selfEntity, true);
                        break;
                    case InteractState.Moving:
                        Moving.SetComponentEnabled(selfEntity, true);
                        break;
                    case InteractState.Garrison:
                        Garrison.SetComponentEnabled(selfEntity, true);
                        break;
                    case InteractState.Harvesting:
                        Harvest.SetComponentEnabled(selfEntity, true);
                        break;
                    case InteractState.Healing:
                        Heal.SetComponentEnabled(selfEntity, true);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
     
            }
        }
    }
}