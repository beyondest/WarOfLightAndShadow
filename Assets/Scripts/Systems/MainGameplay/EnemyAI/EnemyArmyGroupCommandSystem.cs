using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace SparFlame.Systems.MainGameplay.EnemyAI
{
    
    public struct EnemyArmyGroupCheckShouldMovingTag : IComponentData{}
    public partial struct EnemyArmyGroupCommandSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _transformLookup;
        private ComponentLookup<BoxColliderSize> _boxColliderSizeLookup;
        private ComponentLookup<ArmyGroupMovingTag> _armyGroupMovingTagLookup;


        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<ArmyGroupCommandData>();
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _boxColliderSizeLookup = state.GetComponentLookup<BoxColliderSize>(true);
            _armyGroupMovingTagLookup = state.GetComponentLookup<ArmyGroupMovingTag>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _armyGroupMovingTagLookup.Update(ref state);
            _boxColliderSizeLookup.Update(ref state);
            _transformLookup.Update(ref state);
            var ecbP = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            new EnemyArmyGroupCommandJob
            {
                TransformLookup = _transformLookup,
                BoxColliderSizeLookup = _boxColliderSizeLookup,
                ArmyGroupMovingTagLookup = _armyGroupMovingTagLookup,
                ECB =ecbP
            }.ScheduleParallel();

            new EnemyArmyGroupCheckShouldMovingJob
            {
                ECB = ecbP,
            }.ScheduleParallel();
        }

        [BurstCompile]
        [WithAll(typeof(ArmyGroupCommandUpdate))]
        public partial struct EnemyArmyGroupCommandJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public ComponentLookup<ArmyGroupMovingTag> ArmyGroupMovingTagLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public ComponentLookup<BoxColliderSize> BoxColliderSizeLookup;

            private void Execute([ChunkIndexInQuery] int index, ref ArmyGroupMovableData movableData,
                ref DynamicBuffer<ArmyGroupMovingTarget> targets,
                ref DynamicBuffer<ArmyGroupFinalWayPoint> finalWayPoints,
                ref ArmyGroupCalculatePathData pathData, ref PathVisualizeData visualizeData,
                ref NavAgentComponent navAgent, ref ArmyGroupStateData stateData,
                in ArmyGroupCommandData commandData,
                Entity selfEntity
            )
            {
                ECB.SetComponentEnabled<ArmyGroupCommandUpdate>(index, selfEntity, false);
                // If army group is already moving, make it stop and clear its waypoints and targets
                if (ArmyGroupMovingTagLookup.IsComponentEnabled(selfEntity))
                {
                    ArmyGroupUtils.ResetArmyGroupMovableData(ref movableData, ref pathData, ref finalWayPoints,
                        ref visualizeData,
                        ref navAgent,
                        ECB, index, selfEntity);
                    movableData.movementInfo = ArmyGroupMovementInfo.None;
                    stateData.TargetState = ArmyGroupState.Idle;
                    stateData.CurState = ArmyGroupState.Idle;
                    stateData.Target = Entity.Null;
                }

                stateData.CurState = ArmyGroupState.Idle;
                stateData.TargetState = ArmyGroupState.Invade;
                stateData.Target = commandData.TargetCity;

                ECB.AppendToBuffer(index, stateData.Target, new CityFutureInvaders
                {
                    ArmyGroup = selfEntity
                });
                targets.Add(new ArmyGroupMovingTarget
                {
                    position = TransformLookup[commandData.TargetCity].Position,
                    boxColliderSizeXz = BoxColliderSizeLookup[commandData.TargetCity].Value.xz
                });
                ECB.AddComponent<EnemyArmyGroupCheckShouldMovingTag>(index,selfEntity);
            }
        }


        
        // Check calculation complete and start moving
        [BurstCompile]
        [WithAll(typeof(EnemyArmyGroupCheckShouldMovingTag))]
        public partial struct EnemyArmyGroupCheckShouldMovingJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery] int index, in ArmyGroupCalculatePathData data,
                ref ArmyGroupMovableData movableData, Entity selfEntity, in NavAgentComponent agentComponent,
                in DynamicBuffer<ArmyGroupMovingTarget> targets)
            {
                if (!movableData.isTargetReachable)
                {
                    var hintRequest = ECB.CreateEntity(index);
                    ECB.AddComponent<MainGameplayEntityTag>(index, hintRequest);
                    ECB.AddComponent(index, hintRequest, new HintRequest
                    {
                        Name = HintName.DebugEnemyArmyGroupNotReachable
                    });
                    return;
                }
                if(data.curTargetIndex < targets.Length - 1 || !agentComponent.calculationComplete)return;

                movableData.movementInfo = ArmyGroupMovementInfo.NotComplete;
                movableData.curWaypoint = 0;
                ECB.SetComponentEnabled<ArmyGroupMovingTag>(index, selfEntity, true);
                ECB.RemoveComponent<EnemyArmyGroupCheckShouldMovingTag>(index,selfEntity);
            }
        }
    }
}