using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Systems.SubGameplay.EnemyAI
{
    public partial struct AIUnitCommandSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<AIUnitCommandData>();
            state.RequireForUpdate<SubGamingTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new AIMovementMarchJob
            {
                ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            }.ScheduleParallel();
        }

    
    }
    
    [BurstCompile]
    [WithNone(typeof(UnitDeadTag))]
    public partial struct AIMovementMarchJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        private void Execute([ChunkIndexInQuery] int index, ref MovableData movableData,
            ref BasicStateData basicStateData, ref DynamicBuffer<InsightTarget> targets,
            in AIUnitCommandData commandData,
            Entity entity)
        {
            
            ECB.SetComponentEnabled<AIUnitCommandData>(index, entity, false);
            MovementUtils.SetMoveTarget(ref movableData, commandData.TargetPosition, float3.zero,
                MovementCommandType.March, 0f);
            basicStateData.TargetState = InteractState.Moving;
            StateUtils.SwitchState(ref basicStateData, ECB, entity, index);
            // Remove target so that player command it to move than it will move
            if (basicStateData.TargetEntity != Entity.Null)
            {
                InteractUtils.Remove(ref targets, basicStateData.TargetEntity);
            }

            basicStateData.TargetEntity = Entity.Null;
            basicStateData.TargetState = InteractState.Idle;
            basicStateData.Focus = commandData.Focus;
        }
    }

}