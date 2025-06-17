using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Movement;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.State
{
    [UpdateAfter(typeof(BuffManageSystem))]
    [UpdateAfter(typeof(SightUpdateListSystem))]
    [BurstCompile]
    public partial struct IdleStateMachine : ISystem
    {
        private ComponentLookup<SubGameplayGeneralAttr> _interactableAttrLookup;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<SubGamingTag>();
            _interactableAttrLookup = state.GetComponentLookup<SubGameplayGeneralAttr>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton < EndSimulationEntityCommandBufferSystem.Singleton>();
            _interactableAttrLookup.Update(ref state);
            new IdleStateJob
            {
                ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                GeneralAttrLookup = _interactableAttrLookup
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
        
        [BurstCompile]
        [WithAll(typeof(IdleStateTag))]
        [WithNone(typeof(UnitDeadTag))]
        [WithNone(typeof(ConstructingData))]
        public partial struct IdleStateJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public ComponentLookup<SubGameplayGeneralAttr> GeneralAttrLookup;
            
            private void Execute([ChunkIndexInQuery] int index,ref BasicStateData stateData, in DynamicBuffer<InsightTarget> targets, Entity entity)
            {
                // This should check in every state machine, because switch state tag only happens in next frame dur to ecb playback
                if(stateData.CurState != InteractState.Idle )return;
                
                stateData.TargetEntity = Entity.Null;
                stateData.TargetState = InteractState.Idle;
                stateData.Focus = false;
                
                CheckIfHomeUnderAttack();
                if(targets.IsEmpty)return;
                
                stateData.TargetEntity = InteractUtils.ChooseTarget(targets);
                var targetGeneralAttr = GeneralAttrLookup[stateData.TargetEntity];
                var selfGeneralAttr = GeneralAttrLookup[entity];
                
                if (selfGeneralAttr.FactionTag == targetGeneralAttr.FactionTag)
                    stateData.TargetState = InteractState.Healing;
                if(targetGeneralAttr.BaseTag == BaseTag.Resources)
                    stateData.TargetState = InteractState.Harvesting;
                if(selfGeneralAttr.FactionTag == ~targetGeneralAttr.FactionTag)
                    stateData.TargetState = InteractState.Attacking;
                StateUtils.SwitchState(ref stateData,ECB,entity, index);
            }
            private void CheckIfHomeUnderAttack()
            {
            }
        }
    }
}