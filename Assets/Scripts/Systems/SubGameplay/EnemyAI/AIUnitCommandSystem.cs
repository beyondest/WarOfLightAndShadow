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
            state.Dependency = new AIMovementMarchJob
            {
                ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            }.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    [WithNone(typeof(UnitDeadTag))]
    [WithNone(typeof(AttackStateTag))]
    [WithNone(typeof(HealStateTag))]
    public partial struct AIMovementMarchJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;

        private void Execute([ChunkIndexInQuery] int index, ref MovableData movableData,
            ref BasicStateData basicStateData,
            in AIUnitCommandData commandData,
            Entity entity)
        {
            ECB.SetComponentEnabled<AIUnitCommandData>(index, entity, false);
            MovementUtils.SetMoveTarget(ref movableData, commandData.TargetPosition, float3.zero,
                MovementCommandType.March, 0f);
            basicStateData.TargetState = InteractState.Moving;
            StateUtils.SwitchState(ref basicStateData, ECB, entity, index);
            basicStateData.TargetEntity = Entity.Null;
            basicStateData.TargetState = InteractState.Idle;
            basicStateData.Focus = commandData.Focus;
        }
    }
}