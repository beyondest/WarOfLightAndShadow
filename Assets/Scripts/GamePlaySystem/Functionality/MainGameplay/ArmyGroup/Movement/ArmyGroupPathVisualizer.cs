using SparFlame.GamePlaySystem.CustomInput;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Movement;
using Unity.Android.Gradle.Manifest;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Functionality.MainGameplay.ArmyGroup
{
    public partial struct ArmyGroupPathVisualizer : ISystem
    {
        private ComponentLookup<ArmyGroupMovingTag> _movingTagLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ArmyGroupSelectionData>();
            state.RequireForUpdate<InputArmyGroupControlData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<ArmyGroupPathVisualizeConfig>();
            _movingTagLookup = state.GetComponentLookup<ArmyGroupMovingTag>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var inputArmyGroupControlData = SystemAPI.GetSingleton<InputArmyGroupControlData>();
            var selectionData = SystemAPI.GetSingleton<ArmyGroupSelectionData>();
            _movingTagLookup.Update(ref state);
            if (inputArmyGroupControlData.StartMoving || selectionData.CurrentSelectCount == 0
                || inputArmyGroupControlData.ClearAllTargets || inputArmyGroupControlData.DeleteLastTarget
                || inputArmyGroupControlData.EndMovingAndClearAllTargets)
            {
                new ArmyGroupClearAllPathVisualizersJob
                {
                    ECB = ecb
                }.ScheduleParallel();
            }

            new ArmyGroupPathVisualizeJob
            {
                ECB = ecb,
                Config = SystemAPI.GetSingleton<ArmyGroupPathVisualizeConfig>(),
                MovingTagLookup = _movingTagLookup
            }.ScheduleParallel();
        }

        [BurstCompile]
        [WithAll(typeof(PathVisualizer))]
        public partial struct ArmyGroupClearAllPathVisualizersJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery] int index,
                Entity selfEntity)
            {
                ECB.DestroyEntity(index, selfEntity);
            }
        }

        [BurstCompile]
        [WithAll(typeof(ArmyGroupSelected))]
        [WithAll(typeof(PathVisualizeEnabled))]
        public partial struct ArmyGroupPathVisualizeJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public ArmyGroupPathVisualizeConfig Config;
            [ReadOnly] public ComponentLookup<ArmyGroupMovingTag> MovingTagLookup;

            private void Execute([ChunkIndexInQuery] int index, in DynamicBuffer<ArmyGroupFinalWayPoint> finalWaypoints,
                in ArmyGroupMovableData movableData,
                ref PathVisualizeData visualizeData,
                in NavAgentComponent navAgent, Entity selfEntity)
            {
                var isMoving = MovingTagLookup.IsComponentEnabled(selfEntity);
                var notUpdate = !isMoving && visualizeData.PreWaypoint == finalWaypoints.Length;
                if (!navAgent.CalculationComplete || finalWaypoints.Length == 0 || notUpdate) return;
                ECB.SetComponentEnabled<PathVisualizeEnabled>(index, selfEntity, false);
                var startIndex = isMoving
                    ? movableData.CurWaypoint
                    : visualizeData.PreWaypoint;
                visualizeData.PreWaypoint = finalWaypoints.Length;
                for (var i = startIndex; i < finalWaypoints.Length; i++)
                {
                    var waypoint = finalWaypoints[i];
                    var pathVisualizer = ECB.Instantiate(index,
                        movableData.IsTargetReachable ? Config.ReachableRef : Config.UnreachableRef);
                    ECB.AddComponent<MainGameplayEntityTag>(index, pathVisualizer);
                    ECB.SetComponent(index, pathVisualizer, new LocalTransform
                    {
                        Position = waypoint.Position,
                        Rotation = quaternion.identity,
                        Scale = 1f
                    });
                    ECB.AddComponent<PathVisualizer>(index, pathVisualizer);
                }
            }
        }
    }
}