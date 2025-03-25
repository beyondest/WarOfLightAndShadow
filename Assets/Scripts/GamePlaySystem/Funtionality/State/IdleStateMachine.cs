using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.State
{
    [UpdateAfter(typeof(BuffSystem))]
    [UpdateAfter(typeof(SightUpdateListSystem))]
    [UpdateBefore(typeof(StatSystem))]
    [BurstCompile]
    public partial struct IdleStateMachine : ISystem
    {
        private ComponentLookup<InteractableAttr> _interactableAttrLookup;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<NotPauseTag>();
            _interactableAttrLookup = state.GetComponentLookup<InteractableAttr>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton < EndSimulationEntityCommandBufferSystem.Singleton>();
            _interactableAttrLookup.Update(ref state);
            new IdleStateJob
            {
                ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                InteractableAttrLookup = _interactableAttrLookup
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
        
        [BurstCompile]
        [WithAll(typeof(IdleStateTag))]
        public partial struct IdleStateJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public ComponentLookup<InteractableAttr> InteractableAttrLookup;
            
            private void Execute([ChunkIndexInQuery] int index,ref BasicStateData stateData,in DynamicBuffer<InsightTarget> targets, Entity entity)
            {
                stateData.TargetEntity = Entity.Null;
                stateData.TargetState = UnitState.Idle;
                stateData.Focus = false;
                
                CheckIfHomeUnderAttack();
                if(targets.IsEmpty)return;
                
                stateData.TargetEntity = StateUtils.ChooseTarget(targets);
                var targetInteractAttr = InteractableAttrLookup[stateData.TargetEntity];
                var selfInteractAttr = InteractableAttrLookup[entity];
                
                if (selfInteractAttr.FactionTag == targetInteractAttr.FactionTag)
                    stateData.TargetState = UnitState.Healing;
                if(targetInteractAttr.BaseTag == BaseTag.Resources)
                    stateData.TargetState = UnitState.Harvesting;
                if(selfInteractAttr.FactionTag == ~targetInteractAttr.FactionTag)
                    stateData.TargetState = UnitState.Attacking;
                StateUtils.SwitchState(ref stateData,ECB,entity, index);
            }
            private void CheckIfHomeUnderAttack()
            {
            }
        }
    }
}