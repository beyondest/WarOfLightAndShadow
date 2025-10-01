using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.Systems.SubGameplay.Movement
{
    [BurstCompile]
    public partial struct UnitLocalTransformApplySystem : ISystem
    {
        private ComponentLookup<PlayerTag> _playerTagLookup;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<CbrConfig>();
            state.RequireForUpdate<SubGamingTag>();
            _playerTagLookup = state.GetComponentLookup<PlayerTag>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var debug = new MovementDebug();
            if (SystemAPI.HasSingleton<DebugTag>())
            {
                SystemAPI.TryGetSingleton(out debug);
            }
            _playerTagLookup.Update(ref state);
            new UnitTransformApplyJob
            {
                Debug = debug,
                Config = SystemAPI.GetSingleton<CbrConfig>(),
                DeltaTime = SystemAPI.GetSingleton<GameTimeData>().DeltaTime,
                PlayerTagLookup = _playerTagLookup
            }.ScheduleParallel();
        }
    }


    [BurstCompile]
    public partial struct UnitTransformApplyJob : IJobEntity
    {
        [ReadOnly] public CbrConfig Config;
        [ReadOnly] public float DeltaTime;

        [ReadOnly] public MovementDebug Debug;

        [ReadOnly] public ComponentLookup<PlayerTag> PlayerTagLookup;
        // [ReadOnly]public NativeList<NavMeshQuery> Queries;
        private void Execute(
            ref LocalTransform transform,
            ref Velocity velocity,
            in SeekTarget seekTargetOri,
            in Separation separationOri,
            in MovableData movableData,
            in InteractAbilityBonus bonus,
            in NavAgentComponent agent,
            // in TargetRotation targetRotation,
            in GroundInfo groundInfo,
            Entity selfEntity
        )
        {
            // Calculate truly movement direction and apply rotation
            var horizontalSeekTarget = new float3(seekTargetOri.Direction.x, 0, seekTargetOri.Direction.z);
            var seekTarget = new SeekTarget { Direction = float3.zero };
            var separation = new Separation { Value = float3.zero };
            if (math.lengthsq(horizontalSeekTarget) > 0.001f)
            {
                float3 forward = math.normalize(horizontalSeekTarget);

                float3 right = math.normalizesafe(math.cross(groundInfo.Normal, forward));
                forward = math.cross(right, groundInfo.Normal);
                var targetRotation = quaternion.LookRotationSafe(-forward, groundInfo.Normal);
                transform.Rotation = math.slerp(
                    transform.Rotation.value,
                    targetRotation,
                    DeltaTime * Config.RotationSpeed);
                seekTarget.Direction = forward;
            }

            if (math.lengthsq(separationOri.Value) > 0.001f)
            {
                separation.Value = separationOri.Value - groundInfo.Normal * math.dot(separationOri.Value , groundInfo.Normal);
            }

            var scale = PlayerTagLookup.HasComponent(selfEntity) ? Debug.playerMovementScale : Debug.aiMovementScale;
            var debugScale = Debug.enabled ? scale: 1f;
            var maxSpeed = (movableData.MoveSpeed + bonus.MoveSpeedBonus) * debugScale;
            var targetV = float3.zero;

            if (math.lengthsq(seekTarget.Direction) > 0.001f)
            {
                targetV = seekTarget.Direction * maxSpeed * Config.SeekTargetWeight;
            }

            targetV += separation.Value * Config.SeparationWeight;

            var deltaV = targetV - velocity.Value;

            if (math.lengthsq(deltaV) > Config.MaxAcceleration * Config.MaxAcceleration)
            {
                deltaV = math.normalizesafe(deltaV) * Config.MaxAcceleration;
            }

            velocity.Value += deltaV;

            if (math.lengthsq(velocity.Value) > maxSpeed * maxSpeed)
            {
                velocity.Value = math.normalizesafe(velocity.Value) * maxSpeed;
            }


            transform.Position += velocity.Value * DeltaTime;
            transform.Position.y = groundInfo.HitPosition.y;
        }
    }
}