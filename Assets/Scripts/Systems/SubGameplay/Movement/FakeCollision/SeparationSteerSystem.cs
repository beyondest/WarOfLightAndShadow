using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;

namespace SparFlame.Systems.SubGameplay.Movement.FakeCollision
{
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Transforms;

    /// <summary>
    /// 用于单位之间简单的分离，避免堆成一坨
    /// </summary>
    [BurstCompile]
    public partial struct SeparationSteeringSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _localTransformLookup;
        private ComponentLookup<BoxColliderSize> _boxColliderSizeLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SeparationSteerConfig>();
            state.RequireForUpdate<FakeColliderTarget>();
            state.RequireForUpdate<SubGamingTag>();
            _boxColliderSizeLookup = state.GetComponentLookup<BoxColliderSize>(true);
            _localTransformLookup = state.GetComponentLookup<LocalTransform>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {

            _boxColliderSizeLookup.Update(ref state);
            _localTransformLookup.Update(ref state);
           new SeparationJob
            {
                LocalTransformLookup = _localTransformLookup,
                BoxColliderSizeLookup = _boxColliderSizeLookup,
            }.ScheduleParallel();
            new AttackSeparationJob().ScheduleParallel();
            new HealSeparationJob().ScheduleParallel();
            new HarvestSeparationJob().ScheduleParallel();
        }

        [BurstCompile]
        [WithAll(typeof(AttackStateTag))]
        private partial struct AttackSeparationJob : IJobEntity
        {
            private void Execute(ref Separation separation)
            {
                separation.Value = float3.zero;
            }
        }
        [BurstCompile]
        [WithAll(typeof(HealStateTag))]
        private partial struct HealSeparationJob : IJobEntity
        {
            private void Execute(ref Separation separation)
            {
                separation.Value = float3.zero;
            }
        }
        [BurstCompile]
        [WithAll(typeof(HarvestStateTag))]
        private partial struct HarvestSeparationJob : IJobEntity
        {
            private void Execute(ref Separation separation)
            {
                separation.Value = float3.zero;
            }
        }
        
        [BurstCompile]
        [WithNone(typeof(UnitDeadTag))]
        [WithNone(typeof(InGarrison))]
        [WithNone(typeof(AttackStateTag))]
        [WithNone(typeof(HealStateTag))]
        [WithNone(typeof(HarvestStateTag))]
        private partial struct SeparationJob : IJobEntity
        {
            [NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> LocalTransformLookup;
            [ReadOnly] public ComponentLookup<BoxColliderSize> BoxColliderSizeLookup;
            private void Execute(ref DynamicBuffer<FakeColliderTarget> targets, 
                ref Separation separation,
                ref Rnd rnd,Entity selfEntity)
            {
                ref var transform = ref LocalTransformLookup.GetRefRW(selfEntity).ValueRW;
                var selfBox = BoxColliderSizeLookup[selfEntity].Box;
                var selfHalf = new float2(selfBox.x, selfBox.z) * 0.5f;

                var pos = transform.Position.xz;

                var bestSeparation = float2.zero;
                var bestOverlap = 0f;

                for (var i = 0; i < targets.Length; i++)
                {
                    var otherPos3 = LocalTransformLookup[targets[i].Target].Position;
                    var otherPos = otherPos3.xz;

                    var otherBox = BoxColliderSizeLookup[targets[i].Target].Box;
                    var otherHalf = new float2(otherBox.x, otherBox.z) * 0.5f;

                    var delta = pos - otherPos;
                    var absDelta = math.abs(delta);


                    var overlapX = selfHalf.x + otherHalf.x - absDelta.x;
                    var overlapZ = selfHalf.y + otherHalf.y - absDelta.y;

                    if (overlapX > 0 && overlapZ > 0)
                    {
                        float2 sep;
                        float overlapAmt;
                        if (overlapX < overlapZ)
                        {
                            if (absDelta is { x: < 1e-4f, y: < 1e-4f })
                            {
                                var randDir = rnd.value.NextFloat2();
                                sep = randDir * math.min(selfHalf.x, selfHalf.y) * 0.5f;
                                overlapAmt = math.length(sep);
                            }
                            else
                            {
                                var dir = math.sign(delta.x); // 推向左/右
                                sep = new float2(overlapX * dir, 0);
                                overlapAmt = overlapX;
                            }
                        }
                        else
                        {
                            if (absDelta is { x: < 1e-4f, y: < 1e-4f })
                            {
                                // 中心重叠情况：加一个伪随机微偏移方向
                                var randDir = rnd.value.NextFloat2();                        
                                sep = randDir * math.min(selfHalf.x, selfHalf.y) * 0.5f;
                                overlapAmt = math.length(sep);
                            }
                            else
                            {
                                var dir = math.sign(delta.y); // 推向前/后
                                sep = new float2(0, overlapZ * dir);
                                overlapAmt = overlapZ;
                            }
                        }

                        // 选取最大重叠的分离向量
                        if (overlapAmt > bestOverlap)
                        {
                            bestOverlap = overlapAmt;
                            bestSeparation = sep;
                        }
                    }
                }
                // 应用分离
                separation.Value = bestOverlap > 0 ? new float3(bestSeparation.x, 0, bestSeparation.y) : float3.zero;
                // targets.Clear();
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