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
            // state.RequireForUpdate<ArmyGroupSelected>();
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
        // [WithAll(typeof(ArmyGroupSelected))]
        [WithDisabled(typeof(ArmyGroupMovingTag))]
        [WithNone(typeof(ArmyGroupInGarrison))]
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
                if (!navAgent.calculationComplete) return;
                // Try to add this section way points to final way points first, even target is not reachable
                if (wayPoints.Length != 0)
                {
                    ECB.SetComponentEnabled<ArmyGroupPathVisualizeEnabled>(index, selfEntity, true);
                    foreach (var waypoint in wayPoints)
                    {
                        finalWayPoints.Add(new ArmyGroupFinalWayPoint { position = waypoint.position });
                    }
                    var lastPosition = wayPoints[wayPoints.Length - 1].position;
                    var targetPosition = targets[data.curTargetIndex].position;
                    var finalReachRange = Config.finalReachRangeNormal;
                    if (math.lengthsq(data.boxColliderSizeXz) > 0.001f)
                    {
                        targetPosition = ArmyGroupUtils.GetNearestPointOnRect(targetPosition, data.boxColliderSizeXz, data.startPosition);
                        finalReachRange = Config.finalReachRangeForCity;
                    }
                    if (math.distance(lastPosition, targetPosition) > finalReachRange)
                    {
                        movableData.isTargetReachable = false;
                    }
                    wayPoints.Clear();
                }
                
              
                // Already finish all targets calculation
                if (data.curTargetIndex >= targets.Length - 1)
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
                data.curTargetIndex++;
                data.startPosition = data.curTargetIndex - 1 < 0
                    ? transform.Position
                    : targets[data.curTargetIndex - 1].position;
                data.boxColliderSizeXz = targets[data.curTargetIndex].boxColliderSizeXz;
                navAgent.extents = Config.extents;
                navAgent.targetPosition = targets[data.curTargetIndex].position;
            }
        }
    }
}