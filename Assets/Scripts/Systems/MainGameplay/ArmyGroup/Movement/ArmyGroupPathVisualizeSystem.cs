using SparFlame.Components.General;
using SparFlame.Components.Input;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.Systems.MainGameplay.ArmyGroup
{
    public partial struct ArmyGroupPathVisualizeSystem : ISystem
    {
        private ComponentLookup<ArmyGroupMovingTag> _movingTagLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SubGameStatusData>();
            state.RequireForUpdate<GameStatusData>();
            state.RequireForUpdate<ArmyGroupSelectionData>();
            state.RequireForUpdate<InputArmyGroupControlData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<ArmyGroupPathVisualizeConfig>();
            _movingTagLookup = state.GetComponentLookup<ArmyGroupMovingTag>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameStatusData = SystemAPI.GetSingleton<GameStatusData>();
            if (gameStatusData.Value != GameStatus.MainGaming && gameStatusData.Value != GameStatus.SubGaming) return;
            var subGameStatusData = SystemAPI.GetSingleton<SubGameStatusData>();
            if (GameStatusUtils.IsInBattle(subGameStatusData)) return;

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var inputArmyGroupControlData = SystemAPI.GetSingleton<InputArmyGroupControlData>();
            var selectionData = SystemAPI.GetSingleton<ArmyGroupSelectionData>();
            _movingTagLookup.Update(ref state);
            if (inputArmyGroupControlData.StartMoving || selectionData.CurrentSelectCount == 0
                                                      || inputArmyGroupControlData.ClearAllTargets ||
                                                      inputArmyGroupControlData.DeleteLastTarget
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
        [WithAll(typeof(ArmyGroupPathVisualizeEnabled))]
        public partial struct ArmyGroupPathVisualizeJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public ArmyGroupPathVisualizeConfig Config;
            [ReadOnly] public ComponentLookup<ArmyGroupMovingTag> MovingTagLookup;

            private void Execute([ChunkIndexInQuery] int index, in DynamicBuffer<ArmyGroupFinalWayPoint> finalWaypoints,
                in ArmyGroupMovableData movableData,
                ref ArmyGroupPathVisualizeData visualizeData,
                in NavAgentComponent navAgent, Entity selfEntity)
            {
                var isMoving = MovingTagLookup.IsComponentEnabled(selfEntity);
                var notUpdate = !isMoving && visualizeData.preWaypoint == finalWaypoints.Length;
                if (!navAgent.calculationComplete || finalWaypoints.Length == 0 || notUpdate) return;
                ECB.SetComponentEnabled<ArmyGroupPathVisualizeEnabled>(index, selfEntity, false);
                var startIndex = isMoving
                    ? movableData.curWaypoint
                    : visualizeData.preWaypoint;
                visualizeData.preWaypoint = finalWaypoints.Length;
                for (var i = startIndex; i < finalWaypoints.Length; i++)
                {
                    var waypoint = finalWaypoints[i];
                    var pathVisualizer = ECB.Instantiate(index,
                        movableData.isTargetReachable ? Config.ReachableRef : Config.UnreachableRef);
                    ECB.AddComponent<MainGameplayEntityTag>(index, pathVisualizer);
                    ECB.SetComponent(index, pathVisualizer, new LocalTransform
                    {
                        Position = waypoint.position,
                        Rotation = quaternion.identity,
                        Scale = 1f
                    });
                    ECB.AddComponent<PathVisualizer>(index, pathVisualizer);
                }
            }
        }
    }
}