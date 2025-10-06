using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.Systems.SubGameplay.StateMachine
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
        private ComponentLookup<CastSkillStateTag> _castSkill;
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
            _castSkill = state.GetComponentLookup<CastSkillStateTag>();
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
            _castSkill.Update(ref state);

            state.Dependency = new StateLateCheckJob
            {
                Idle = _idle,
                Attack = _attack,
                Moving = _moving,
                Garrison = _garrison,
                Heal = _heal,
                Harvest = _harvest,
                CastSkill = _castSkill,
            }.ScheduleParallel(state.Dependency);
        }


        [BurstCompile]
        [WithNone(typeof(UnitDeadTag))]
        public partial struct StateLateCheckJob : IJobEntity
        {
            // These components are only written to self
            [NativeDisableParallelForRestriction] public ComponentLookup<IdleStateTag> Idle;
            [NativeDisableParallelForRestriction] public ComponentLookup<AttackStateTag> Attack;
            [NativeDisableParallelForRestriction] public ComponentLookup<MovingStateTag> Moving;
            [NativeDisableParallelForRestriction] public ComponentLookup<GarrisonStateTag> Garrison;
            [NativeDisableParallelForRestriction] public ComponentLookup<HealStateTag> Heal;
            [NativeDisableParallelForRestriction] public ComponentLookup<HarvestStateTag> Harvest;
            [NativeDisableParallelForRestriction] public ComponentLookup<CastSkillStateTag> CastSkill;
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
                    case InteractState.CastSkill:
                        CastSkill.SetComponentEnabled(selfEntity, true);
                        break;
                    default:
                        BurstSafe.UnexpectedEnum(state.CurState);
                        break;
                }
     
            }
        }
    }
}