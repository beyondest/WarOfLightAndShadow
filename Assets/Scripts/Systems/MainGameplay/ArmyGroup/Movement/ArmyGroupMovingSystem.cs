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
    public partial struct ArmyGroupMovingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SubGameStatusData>();
            state.RequireForUpdate<WorldTimeData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<ArmyGroupMovingSystemConfig>();
            state.RequireForUpdate<GameStatusData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameStatusData = SystemAPI.GetSingleton<GameStatusData>();
            if(gameStatusData.Value != GameStatus.MainGaming && gameStatusData.Value != GameStatus.SubGaming)return;
            var subGameStatusData = SystemAPI.GetSingleton<SubGameStatusData>();
            if(GameStatusUtils.IsInBattle(subGameStatusData))return;
            
            
            var debug = new MovementDebug();
            if (SystemAPI.HasSingleton<DebugTag>())
            {
                SystemAPI.TryGetSingleton(out debug);
            }

            new ArmyGroupMovingJob
            {
                Config = SystemAPI.GetSingleton<ArmyGroupMovingSystemConfig>(),
                DeltaHour = SystemAPI.GetSingleton<WorldTimeData>().deltaHour,
                DeltaTime =  SystemAPI.GetSingleton<GameTimeData>().DeltaTime,
                PlayerFactionData = SystemAPI.GetSingleton<PlayerFactionData>(),
                Debug = debug,
                ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            }.ScheduleParallel();
        }


        [BurstCompile]
        [WithAll(typeof(ArmyGroupMovingTag))]
        public partial struct ArmyGroupMovingJob : IJobEntity
        {
            [ReadOnly] public PlayerFactionData PlayerFactionData;
            [ReadOnly] public ArmyGroupMovingSystemConfig Config;
            [ReadOnly] public MovementDebug Debug;
            [ReadOnly] public float DeltaHour;
            [ReadOnly] public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery] int index, ref ArmyGroupMovableData movableData,
                ref LocalTransform transform,
                ref DynamicBuffer<ArmyGroupFinalWayPoint> finalWaypoints,
                in MainGameplayGeneralAttr generalData, ref DynamicBuffer<ArmyGroupMovingTarget> targets,
                ref ArmyGroupCalculatePathData pathData, ref NavAgentComponent navAgentComponent,
                ref PathVisualizeData visualizeData,
                in ArmyGroupStateData stateData,
                Entity selfEntity)
            {
                if (!navAgentComponent.calculationComplete || finalWaypoints.Length == 0)
                {
                    return;
                }

                var range = stateData.TargetState == ArmyGroupState.Idle ?   Config.finalReachRangeNormal : Config.finalReachRangeForCity;
                if (math.distance(transform.Position, targets[0].position) < range)
                {
                    targets.RemoveAt(0);
                    if (targets.Length == 0)
                    {
                        ArmyGroupUtils.ResetArmyGroupMovableData(ref movableData, ref pathData, ref finalWaypoints,
                            ref visualizeData, ref navAgentComponent, ECB, index, selfEntity);
                        movableData.movementInfo = ArmyGroupMovementInfo.Complete;
                        return;
                    }
                }

                var maxDisToNextPoint = math.distance(finalWaypoints[movableData.curWaypoint].position, transform.Position);
                if (movableData.curWaypoint + 1 < finalWaypoints.Length && maxDisToNextPoint < Config.waypointReachRange)
                {
                    movableData.curWaypoint += 1;
                }

                var nextPosition = finalWaypoints[movableData.curWaypoint].position;
                var direction = math.normalizesafe(nextPosition - transform.Position);
                var scale = Config.moveSpeedScale;
                if (Debug.enabled)
                {
                    var relation = FactionUtils.GetRelationship(PlayerFactionData, generalData.faction,
                        generalData.subFaction);
                    scale *= relation == Relationship.Player
                        ? Debug.playerArmyGroupMovementScale
                        : Debug.nonPlayerArmyGroupMovementScale;
                }

                var moveLength = DeltaHour * movableData.minUnitMoveSpeed * scale;
                moveLength = math.min(moveLength, maxDisToNextPoint);
                var targetRotation = quaternion.LookRotationSafe(-direction, math.up());
                transform.Rotation = math.slerp(transform.Rotation.value, targetRotation,
                    DeltaTime * Config.rotationSpeed);
                transform.Position += moveLength * direction;
              
            }
        }
    }
}