using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace SparFlame.Systems.MainGameplay.EnemyAI
{
    public struct EnemyArmyGroupCheckShouldMovingTag : IComponentData
    {
    }

    
    public partial struct EnemyArmyGroupCommandSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _transformLookup;
        private ComponentLookup<BoxColliderSize> _boxColliderSizeLookup;
        private ComponentLookup<ArmyGroupMovingTag> _armyGroupMovingTagLookup;
        private ComponentLookup<ArmyGroupInGarrison> _armyGroupInGarrisonLookup;
        private ComponentLookup<GlobalSingleId> _singleIdLookup;


        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SubGameStatusData>();
            state.RequireForUpdate<GameStatusData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<ArmyGroupCommandData>();
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _boxColliderSizeLookup = state.GetComponentLookup<BoxColliderSize>(true);
            _armyGroupMovingTagLookup = state.GetComponentLookup<ArmyGroupMovingTag>(true);
            _armyGroupInGarrisonLookup = state.GetComponentLookup<ArmyGroupInGarrison>(true);
            _singleIdLookup = state.GetComponentLookup<GlobalSingleId>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameStatusData = SystemAPI.GetSingleton<GameStatusData>().Value;
            if(gameStatusData != GameStatus.MainGaming && gameStatusData != GameStatus.SubGaming) return;
            var subGameStatusData = SystemAPI.GetSingleton<SubGameStatusData>();
            if(GameStatusUtils.IsInBattle(subGameStatusData))return;
            _armyGroupMovingTagLookup.Update(ref state);
            _boxColliderSizeLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _armyGroupInGarrisonLookup.Update(ref state);
            _singleIdLookup.Update(ref state);
            var ecbP = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            new EnemyArmyGroupCommandJob
            {
                TransformLookup = _transformLookup,
                BoxColliderSizeLookup = _boxColliderSizeLookup,
                ArmyGroupMovingTagLookup = _armyGroupMovingTagLookup,
                InGarrisonLookup = _armyGroupInGarrisonLookup,
                SingleIdLookup = _singleIdLookup,
                ECB = ecbP
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
            [ReadOnly] public ComponentLookup<ArmyGroupInGarrison> InGarrisonLookup;
            [ReadOnly] public ComponentLookup<GlobalSingleId> SingleIdLookup;
            private void Execute([ChunkIndexInQuery] int index,
                in GlobalSingleId singleId,
                ref ArmyGroupMovableData movableData,
                ref DynamicBuffer<ArmyGroupMovingTarget> targets,
                ref DynamicBuffer<ArmyGroupFinalWayPoint> finalWayPoints,
                ref ArmyGroupCalculatePathData pathData, ref ArmyGroupPathVisualizeData visualizeData,
                ref NavAgentComponent navAgent, ref ArmyGroupStateData stateData,
                ref ArmyGroupCommandData commandData,
                Entity selfEntity
            )
            {
                var isInGarrison = InGarrisonLookup.TryGetComponent(selfEntity, out var inGarrison);
                if(!commandData.WaitForGarrisonOut && !isInGarrison)return;
                if (isInGarrison)
                {
                    if (!commandData.WaitForGarrisonOut)
                    {
                        var garrisonOutRequest = ECB.CreateEntity(index);
                        ECB.AddComponent<MainGameplayEntityTag>(index, garrisonOutRequest);
                        ECB.AddComponent(index, garrisonOutRequest, new ArmyGroupGarrisonRequest
                        {
                            ArmyGroup = selfEntity,
                            City = inGarrison.City,
                            IfGarrisonIn = false
                        });
                        commandData.WaitForGarrisonOut = true;
                    }
                    return;
                }
                commandData.WaitForGarrisonOut = false;
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
                    stateData.TargetSingleId = 0;
                }

                stateData.CurState = ArmyGroupState.Idle;
                stateData.TargetState = ArmyGroupState.Invade;
                stateData.Target = commandData.TargetCity;
                stateData.TargetSingleId = SingleIdLookup.TryGetComponent(commandData.TargetCity, out var id) ? id.value : 0;
                ECB.AppendToBuffer(index, stateData.Target, new CityFutureInvaders
                {
                    ArmyGroup = selfEntity,
                    SingleId = singleId.value
                });
                targets.Add(new ArmyGroupMovingTarget
                {
                    position = TransformLookup[commandData.TargetCity].Position,
                    boxColliderSizeXz = BoxColliderSizeLookup[commandData.TargetCity].Box.xz
                });
                ECB.AddComponent<EnemyArmyGroupCheckShouldMovingTag>(index, selfEntity);
            }
        }


        // Check calculation complete and start moving
        [BurstCompile]
        [WithAll(typeof(EnemyArmyGroupCheckShouldMovingTag))]
        public partial struct EnemyArmyGroupCheckShouldMovingJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            private void Execute([ChunkIndexInQuery] int index, in ArmyGroupCalculatePathData calculatePathData,
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
                // Not complete
                if (calculatePathData.curTargetIndex < targets.Length - 1 || !agentComponent.calculationComplete) return;
                
                movableData.movementInfo = ArmyGroupMovementInfo.NotComplete;
                movableData.curWaypoint = 0;
                ECB.SetComponentEnabled<ArmyGroupMovingTag>(index, selfEntity, true);
                ECB.RemoveComponent<EnemyArmyGroupCheckShouldMovingTag>(index, selfEntity);
            }
        }
    }
}