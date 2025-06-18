using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

// ReSharper disable UseIndexFromEndExpression

namespace SparFlame.Systems.MainGameplay.ArmyGroup
{
    public partial struct ArmyGroupSetNavCalculationSystem : ISystem
    {
        private ComponentLookup<ArmyGroupCalculateEnable> _armyGroupCalculateEnableLookup;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ArmyGroupMovingSystemConfig>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<MainGamingTag>();
            state.RequireForUpdate<ArmyGroupSelected>();
            _armyGroupCalculateEnableLookup = state.GetComponentLookup<ArmyGroupCalculateEnable>(
                );
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _armyGroupCalculateEnableLookup.Update(ref state);
            new ArmyGroupSetNavJob
            {
                ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                Config = SystemAPI.GetSingleton<ArmyGroupMovingSystemConfig>(),
                ArmyGroupCalculateEnableLookup = _armyGroupCalculateEnableLookup,
            }.ScheduleParallel();
        }
      
        
        [BurstCompile]
        [WithAll(typeof(ArmyGroupSelected))]
        [WithNone(typeof(ArmyGroupMovingTag))]
        public partial struct ArmyGroupSetNavJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public ArmyGroupMovingSystemConfig Config;
            [NativeDisableParallelForRestriction] public ComponentLookup<ArmyGroupCalculateEnable> ArmyGroupCalculateEnableLookup;
            private void Execute([ChunkIndexInQuery]int index,
                ref ArmyGroupCalculatePathData data, ref NavAgentComponent navAgent, ref ArmyGroupMovableData movableData,
                ref DynamicBuffer<WaypointBuffer> wayPoints, ref DynamicBuffer<ArmyGroupFinalWayPoint> finalWayPoints,
                in DynamicBuffer<ArmyGroupMovingTarget> targets, in LocalTransform transform,
                Entity selfEntity)
            {
                if (!navAgent.CalculationComplete) return;
                if (wayPoints.Length != 0)
                {
                    ECB.SetComponentEnabled<PathVisualizeEnabled>(index, selfEntity, true);
                    foreach (var waypoint in wayPoints)
                    {
                        finalWayPoints.Add(new ArmyGroupFinalWayPoint { Position = waypoint.Position });
                    }
                    var lastPosition = wayPoints[wayPoints.Length - 1].Position;
                    var targetPosition = targets[data.CurTargetIndex].Position;
                    if (math.distance(lastPosition, targetPosition) > Config.finalReachRange)
                    {
                        movableData.IsTargetReachable = false;
                    }
                    wayPoints.Clear();
                }
              
                // Already finish all targets calculation
                if (data.CurTargetIndex >= targets.Length - 1)
                {
                    wayPoints.Clear();
                    if (ArmyGroupCalculateEnableLookup.IsComponentEnabled(selfEntity))
                    {
                        ArmyGroupCalculateEnableLookup.SetComponentEnabled(selfEntity, false);
                    }
                    return;
                }
                // ECB.SetComponentEnabled<ArmyGroupCalculateEnable>(index,selfEntity,true );
                ArmyGroupCalculateEnableLookup.SetComponentEnabled(selfEntity, true);
                data.CurTargetIndex++;
                data.StartPosition = data.CurTargetIndex - 1 < 0
                    ? transform.Position
                    : targets[data.CurTargetIndex - 1].Position;
                navAgent.Extents = Config.extents;
                navAgent.TargetPosition = targets[data.CurTargetIndex].Position;
            }
        }
    }
}