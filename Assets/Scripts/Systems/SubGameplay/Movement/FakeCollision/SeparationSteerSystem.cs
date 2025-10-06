using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.Systems.SubGameplay.Movement.FakeCollision
{
    /// <summary>
    /// 用于单位之间简单的分离，避免堆成一坨
    /// </summary>
    [BurstCompile]
    public partial struct SeparationSteeringSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _localTransformLookup;
        private ComponentLookup<BoxColliderSize> _boxColliderSizeLookup;
        private ComponentLookup<Velocity> _velocityLookup;
        private ComponentLookup<FakeCollisionTriggerData> _triggerDataLookup;
        private ComponentLookup<AttackStateTag> _attackStateTagLookup;
        private ComponentLookup<HealStateTag> _healStateTagLookup;
        private ComponentLookup<AutoGiveWayTag> _autoGiveWayTagLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SeparationConfig>();
            state.RequireForUpdate<AvoidanceConfig>();
            state.RequireForUpdate<FakeColliderTarget>();
            state.RequireForUpdate<SubGamingTag>();
            _boxColliderSizeLookup = state.GetComponentLookup<BoxColliderSize>(true);
            _localTransformLookup = state.GetComponentLookup<LocalTransform>(true);
            _velocityLookup = state.GetComponentLookup<Velocity>(true);
            _triggerDataLookup = state.GetComponentLookup<FakeCollisionTriggerData>(true);
            _attackStateTagLookup = state.GetComponentLookup<AttackStateTag>(true);
            _healStateTagLookup = state.GetComponentLookup<HealStateTag>(true);
            
            _autoGiveWayTagLookup = state.GetComponentLookup<AutoGiveWayTag>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _boxColliderSizeLookup.Update(ref state);
            _localTransformLookup.Update(ref state);
            _velocityLookup.Update(ref state);
            _triggerDataLookup.Update(ref state);
            _attackStateTagLookup.Update(ref state);
            _healStateTagLookup.Update(ref state);
            _autoGiveWayTagLookup.Update(ref state);
            new CheckSurroundingJob
            {
                LocalTransformLookup = _localTransformLookup,
                BoxColliderSizeLookup = _boxColliderSizeLookup,
                VelocityLookup = _velocityLookup,
                TriggerDataLookup = _triggerDataLookup,
                AvoidanceConfig = SystemAPI.GetSingleton<AvoidanceConfig>(),
                AttackStateLookup = _attackStateTagLookup,
                HealStateLookup = _healStateTagLookup,
                SeparationConfig = SystemAPI.GetSingleton<SeparationConfig>(),
                AutoGiveWayLookup = _autoGiveWayTagLookup
            }.ScheduleParallel();
            new SeparationSetZeroJob().ScheduleParallel();
            new FormationMovingSeparationSteeringJob().ScheduleParallel();
        }


        [BurstCompile]
        [WithNone(typeof(MovingStateTag))]
        [WithNone(typeof(IdleStateTag))]
        [WithNone(typeof(FormationMovingTag))]
        private partial struct SeparationSetZeroJob : IJobEntity
        {
            private void Execute(ref Separation separation, ref Avoidance avoidance)
            {
                separation.Value = float3.zero;
                avoidance.Value = float3.zero;
            }
        }

        [BurstCompile]
        [WithAny(typeof(FormationMovingTag), typeof(AutoGiveWayTag))]
        public partial struct FormationMovingSeparationSteeringJob : IJobEntity
        {
            private void Execute(ref Separation separation, ref Avoidance avoidance)
            {
                separation.Value = float3.zero;
                avoidance.Value = float3.zero;
            }
        }

        [BurstCompile]
        [WithNone(typeof(UnitDeadTag))]
        [WithNone(typeof(InGarrison))]
        [WithAny(typeof(MovingStateTag), typeof(IdleStateTag))]
        [WithNone(typeof(FormationMovingTag))]
        [WithNone(typeof(AutoGiveWayTag))]
        private partial struct CheckSurroundingJob : IJobEntity
        {
            [ReadOnly] public AvoidanceConfig AvoidanceConfig;
            [ReadOnly] public SeparationConfig SeparationConfig;
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
            [ReadOnly] public ComponentLookup<BoxColliderSize> BoxColliderSizeLookup;
            [ReadOnly] public ComponentLookup<Velocity> VelocityLookup;
            [ReadOnly] public ComponentLookup<FakeCollisionTriggerData> TriggerDataLookup;
            [ReadOnly] public ComponentLookup<AttackStateTag> AttackStateLookup;
            [ReadOnly] public ComponentLookup<HealStateTag> HealStateLookup;
            [ReadOnly] public ComponentLookup<AutoGiveWayTag> AutoGiveWayLookup;
            private void Execute(ref DynamicBuffer<FakeColliderTarget> targets,
                ref Separation separation, ref Avoidance avoidance,
                ref Rnd rnd, Entity selfEntity)
            {
                var selfVelocity = VelocityLookup[selfEntity].Value.xz;
                var transform = LocalTransformLookup[selfEntity];
                var selfRot = transform.Rotation.value.y; // 若是 quaternion, 则用 math.atan2
                if (math.abs(transform.Rotation.value.w) < 0.999f)
                {
                    // 假设为 quaternion
                    selfRot = math.atan2(2f * (transform.Rotation.value.w * transform.Rotation.value.y),
                        1f - 2f * (transform.Rotation.value.y * transform.Rotation.value.y));
                }

                var selfRotMat = new float2x2(math.cos(selfRot), -math.sin(selfRot),
                    math.sin(selfRot), math.cos(selfRot));
                var selfBox = BoxColliderSizeLookup[selfEntity].SeparationBox;
                var selfHalf = new float2(selfBox.x, selfBox.z) * 0.5f;
                var selfPos = transform.Position.xz;
                var forward = math.normalizesafe(selfVelocity);
                var left = MovementUtils.GetLeftOrRight(new float3(forward.x, 0, forward.y), true);
                var bestSeparation = float2.zero;
                var bestOverlap = 0f;
                var leftTargetsCount = 0;
                var rightTargetsCount = 0;
                var shouldAddAvoidValue = false;
                for (var i = 0; i < targets.Length; i++)
                {
                    if (!LocalTransformLookup.TryGetComponent(targets[i].Target, out var otherTransform)) continue;
                    if(!TriggerDataLookup.TryGetComponent(targets[i].Target, out var triggerData))continue;
                    // This unit is auto give way, do not consider it as separation or avoiding
                    if(AutoGiveWayLookup.HasComponent(triggerData.BelongsTo) && AutoGiveWayLookup.IsComponentEnabled(triggerData.BelongsTo))continue;
                    var otherPos3 = otherTransform.Position;
                    var otherPos = otherPos3.xz;
                    var otherRot = otherTransform.Rotation.value.y;
                    if (math.abs(otherTransform.Rotation.value.w) < 0.999f)
                    {
                        otherRot = math.atan2(2f * (otherTransform.Rotation.value.w * otherTransform.Rotation.value.y),
                            1f - 2f * (otherTransform.Rotation.value.y * otherTransform.Rotation.value.y));
                    }

                    var otherBox = BoxColliderSizeLookup[targets[i].Target].SeparationBox;
                    var otherHalf = new float2(otherBox.x, otherBox.z) * 0.5f;

                    var otherRotMat = new float2x2(math.cos(otherRot), -math.sin(otherRot),
                        math.sin(otherRot), math.cos(otherRot));

                    var delta = otherPos - selfPos;
                    // float2 localDelta = math.mul(math.transpose(selfRotMat), delta);
                    if (math.lengthsq(delta) < 0.001f)
                    {
                        bestSeparation = MathUtils.Get2D(ref rnd.value) * SeparationConfig.ValueWhenTotallyOverlapped;
                        bestOverlap = 1f;
                        break;
                    }

                    var ifFront = math.dot(forward, delta) > 0f;
                    // calculate the avoidance
                    if (ifFront)
                    {
                        var otherVelocity = float2.zero;
                        if (VelocityLookup.TryGetComponent(triggerData.BelongsTo, out var velocity))
                            otherVelocity = new float2(velocity.Value.x, velocity.Value.z);
                        var shouldWait = math.dot(otherVelocity, selfVelocity) > 0f;
                        if (!shouldWait)
                        {
                            if (math.dot(left.xz, delta) > 0f) leftTargetsCount++;
                            else rightTargetsCount++;
                            if ((AttackStateLookup.HasComponent(triggerData.BelongsTo) &&
                                 AttackStateLookup.IsComponentEnabled(triggerData.BelongsTo))
                                ||
                                (HealStateLookup.HasComponent(triggerData.BelongsTo) &&
                                 HealStateLookup.IsComponentEnabled(triggerData.BelongsTo)))
                            {
                                shouldAddAvoidValue = true;
                            }
                        }
                    }

                    // 旋转矩形投影轴（自己和对方的局部X、Z方向）
                    var axes = new NativeArray<float2>(4, Allocator.Temp);
                    axes[0] = selfRotMat.c0; // 自己的局部X
                    axes[1] = selfRotMat.c1; // 自己的局部Z
                    axes[2] = otherRotMat.c0; // 对方局部X
                    axes[3] = otherRotMat.c1; // 对方局部Z

                    var smallestAxis = float2.zero;
                    var minOverlap = float.MaxValue;
                    var overlapped = true;

                    for (var a = 0; a < 4; a++)
                    {
                        var axis = math.normalize(axes[a]);

                        // 两个盒子在此轴上的投影半径
                        var projSelf = math.abs(math.dot(axis, selfRotMat.c0)) * selfHalf.x +
                                       math.abs(math.dot(axis, selfRotMat.c1)) * selfHalf.y;

                        var projOther = math.abs(math.dot(axis, otherRotMat.c0)) * otherHalf.x +
                                        math.abs(math.dot(axis, otherRotMat.c1)) * otherHalf.y;

                        var centerDist = math.abs(math.dot(axis, delta));

                        var overlap = projSelf + projOther - centerDist;

                        if (overlap < 0f)
                        {
                            overlapped = false;
                            break;
                        }
                        if (overlap < minOverlap)
                        {
                            minOverlap = overlap;
                            smallestAxis = axis * math.sign(math.dot(axis, delta));
                        }
                    }
                    // 只有在重叠时才更新分离向量
                    if (overlapped)
                    {
                        if (minOverlap > bestOverlap)
                        {
                            bestOverlap = minOverlap;
                            bestSeparation = -smallestAxis * minOverlap;
                        }
                    }
                }

                separation.Value = bestOverlap > 0 ? new float3(bestSeparation.x, 0, bestSeparation.y) : float3.zero;
                if (leftTargetsCount == 0 && rightTargetsCount == 0)
                {
                    avoidance.Value = float3.zero;
                }
                else
                {
                    avoidance.Value = leftTargetsCount > rightTargetsCount ? -left : left;
                    if (shouldAddAvoidValue)
                        avoidance.Value *= AvoidanceConfig.AddAvoidanceValueForInteractState;
                }
            }
        }


        /*[BurstCompile]
        [WithNone(typeof(UnitDeadTag))]
        [WithNone(typeof(InGarrison))]
        private partial struct SeparationJob : IJobEntity
        {
            [NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> LocalTransformLookup;
            [ReadOnly] public ComponentLookup<BoxColliderSize> BoxColliderSizeLookup;
            [ReadOnly] public SeparationSteerConfig Config;
            private void Execute(  ref DynamicBuffer<FakeColliderTarget> targets, Entity selfEntity)
            {
                ref var transform = ref LocalTransformLookup.GetRefRW(selfEntity).ValueRW;
                var boxColliderSize = BoxColliderSizeLookup[selfEntity];
                var pos = transform.Position;
                var separation = float3.zero;

                for (var i = 0; i < targets.Length; i++)
                {
                    var otherPos = LocalTransformLookup[targets[i].Target].Position;
                    var r = boxColliderSize.Radius + BoxColliderSizeLookup[targets[i].Target].Radius;
                    var diff = pos - otherPos;
                    var distSq = math.lengthsq(diff);

                    if (distSq < r * r && distSq > 0.0001f)
                    {
                        var dist = math.sqrt(distSq);
                        var dir = diff / dist;
                        var overlap = r - dist;

                        // 简单 push 开（取一部分力度，避免过强抖动）
                        separation += dir * overlap * Config.CoefficientOfOverlap;
                    }
                }
                transform.Position += separation;
            }
        }*/

        // // 应用修正（留接口做插值平滑）
        // [BurstCompile]
        // private partial struct ApplySeparationJob : IJobEntity
        // {
        //     public float DeltaTime;
        //
        //     public void Execute(ref LocalTransform transform)
        //     {
        //         // 这里如果 separation 过强，可以插值
        //         // transform.Position = math.lerp(transform.Position, transform.Position + correction, DeltaTime * 10f);
        //     }
        // }
    }
}