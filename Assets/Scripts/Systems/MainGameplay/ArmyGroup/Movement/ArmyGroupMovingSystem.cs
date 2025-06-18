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
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<ArmyGroupMovingSystemConfig>();
            state.RequireForUpdate<MainGamingTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var debug = new MovementDebug();
            if (SystemAPI.HasSingleton<DebugTag>())
            {
                SystemAPI.TryGetSingleton(out debug);
            }

            new ArmyGroupMovingJob
            {
                Config = SystemAPI.GetSingleton<ArmyGroupMovingSystemConfig>(),
                DeltaTime = SystemAPI.GetSingleton<GameTimeData>().DeltaTime,
                PlayerFaction = SystemAPI.GetSingleton<PlayerFactionData>().Value,
                Debug = debug,
            }.ScheduleParallel();
        }


        [BurstCompile]
        [WithAll(typeof(ArmyGroupMovingTag))]
        public partial struct ArmyGroupMovingJob : IJobEntity
        {
            [ReadOnly] public FactionTag PlayerFaction;
            [ReadOnly] public ArmyGroupMovingSystemConfig Config;
            [ReadOnly] public MovementDebug Debug;
            [ReadOnly] public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery] int index, ref ArmyGroupMovableData movableData,
                ref LocalTransform transform,
                ref DynamicBuffer<ArmyGroupFinalWayPoint> finalWaypoints,
                in MainGameplayGeneralAttr generalData, ref DynamicBuffer<ArmyGroupMovingTarget> targets,
                ref ArmyGroupCalculatePathData pathData, ref NavAgentComponent navAgentComponent,
                ref PathVisualizeData visualizeData,
                Entity selfEntity)
            {
                if (!navAgentComponent.CalculationComplete || finalWaypoints.Length == 0)
                {
                    return;
                }

                if (math.distance(transform.Position, targets[0].Position) < Config.finalReachRange)
                {
                    targets.RemoveAt(0);
                    if (targets.Length == 0)
                    {
                        ArmyGroupUtils.ResetArmyGroupMovableData(ref movableData, ref pathData, ref finalWaypoints,
                            ref visualizeData, ref navAgentComponent,ECB, index, selfEntity);
                        return;
                    }
                }


                if (movableData.CurWaypoint + 1 < finalWaypoints.Length &&
                    math.distance(finalWaypoints[movableData.CurWaypoint].Position, transform.Position) <
                    Config.waypointReachRange)
                {
                    movableData.CurWaypoint += 1;
                }

                var nextPosition = finalWaypoints[movableData.CurWaypoint].Position;
                var direction = math.normalizesafe(nextPosition - transform.Position);
                var scale = Config.moveSpeedScale;
                if (Debug.enabled)
                {
                    scale = generalData.Faction == PlayerFaction
                        ? Debug.playerArmyGroupMovementScale
                        : Debug.enemyArmyGroupMovementScale;
                }

                var moveLength = DeltaTime * movableData.Speed * scale;

                var targetRotation = quaternion.LookRotationSafe(-direction, math.up());
                transform.Rotation = math.slerp(transform.Rotation.value, targetRotation,
                    DeltaTime * Config.rotationSpeed);
                transform.Position += moveLength * direction;
            }
        }
    }
}