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
    public partial struct UnitTransformApplySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<CbrConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var debug = new MovementDebug();
            if (SystemAPI.HasSingleton<DebugTag>())
            {
                SystemAPI.TryGetSingleton(out debug);
            }
            new UnitTransformApplyJob
            {
                Debug = debug,
                Config = SystemAPI.GetSingleton<CbrConfig>(),
                DeltaTime = SystemAPI.GetSingleton<GameTimeData>().DeltaTime
            }.ScheduleParallel();
        }
    }

    [BurstCompile]
    public partial struct UnitTransformApplyJob : IJobEntity
    {
        [ReadOnly] public CbrConfig Config;
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public MovementDebug Debug;

        private void Execute(
            ref LocalTransform transform,
            ref Velocity velocity,
            in SeekTarget seekTarget,
            in Separation separation,
            in MovableData movableData,
            in InteractAbilityBonus bonus)
        {
            // ---- 1. 计算目标速度（来自 seekTarget）
            var debugScale = Debug.enabled ? Debug.playerMovementScale : 1f;
            var maxSpeed = (movableData.MoveSpeed + bonus.MoveSpeedBonus) * debugScale;
            var targetV = float3.zero;

            if (math.lengthsq(seekTarget.Direction) > 0.001f)
            {
                targetV = seekTarget.Direction * maxSpeed * Config.SeekTargetWeight;
            }

            // ---- 2. 加上 separationForce 影响
            targetV += separation.Value * Config.SeparationWeight;

            // ---- 3. 计算 deltaV = targetV - currentV
            var deltaV = targetV - velocity.Value;

            // ---- 4. 限制加速度
            if (math.lengthsq(deltaV) > Config.MaxAcceleration * Config.MaxAcceleration)
            {
                deltaV = math.normalizesafe(deltaV) * Config.MaxAcceleration;
            }

            // ---- 5. 更新速度
            velocity.Value += deltaV ;

            // 限制最大速度
            if (math.lengthsq(velocity.Value) > maxSpeed * maxSpeed)
            {
                velocity.Value = math.normalizesafe(velocity.Value) * maxSpeed;
            }

            // ---- 6. 更新位置
            transform.Position += velocity.Value * DeltaTime;

            // ---- 7. 更新朝向
            if (math.lengthsq(velocity.Value) > 0.001f)
            {
                var dir = math.normalizesafe(velocity.Value);
                var targetRotation = quaternion.LookRotationSafe(-dir, math.up());
                transform.Rotation = math.slerp(
                    transform.Rotation.value,
                    targetRotation,
                    DeltaTime * Config.RotationSpeed);
            }
        }
    }
}