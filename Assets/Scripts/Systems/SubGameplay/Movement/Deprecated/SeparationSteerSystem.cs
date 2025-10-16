// using SparFlame.Components.General;
// using SparFlame.Components.SubGameplay;
// using SparFlame.Core.Utils;
// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Mathematics;
// using Unity.Transforms;
//
// namespace SparFlame.Systems.SubGameplay.Movement.FakeCollision
// {
//     [BurstCompile]
//     public partial struct SeparationSteeringSystem : ISystem
//     {
//         private struct SurroundingLookups
//         {
//             [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
//             [ReadOnly] public ComponentLookup<BoxColliderSize> BoxColliderSizeLookup;
//             [ReadOnly] public ComponentLookup<Velocity> VelocityLookup;
//             [ReadOnly] public ComponentLookup<FakeCollisionTriggerData> TriggerDataLookup;
//             [ReadOnly] public ComponentLookup<AttackStateTag> AttackStateLookup;
//             [ReadOnly] public ComponentLookup<HealStateTag> HealStateLookup;
//             [ReadOnly] public ComponentLookup<AutoGiveWayTag> AutoGiveWayLookup;
//             [ReadOnly] public ComponentLookup<MovingStateTag> MovingStateLookup;
//             [ReadOnly] public ComponentLookup<IdleStateTag> IdleStateLookup;
//             [ReadOnly] public ComponentLookup<FormationMovingTag> FormationMovingTagLookup;
//         }
//
//         [BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
//             state.RequireForUpdate<SeparationConfig>();
//             state.RequireForUpdate<AvoidanceConfig>();
//             state.RequireForUpdate<FakeColliderTarget>();
//             state.RequireForUpdate<SubGamingTag>();
//         }
//
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             var lookups = new SurroundingLookups
//             {
//                 LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
//                 BoxColliderSizeLookup = SystemAPI.GetComponentLookup<BoxColliderSize>(true),
//                 VelocityLookup = SystemAPI.GetComponentLookup<Velocity>(true),
//                 TriggerDataLookup = SystemAPI.GetComponentLookup<FakeCollisionTriggerData>(true),
//                 AttackStateLookup = SystemAPI.GetComponentLookup<AttackStateTag>(true),
//                 HealStateLookup = SystemAPI.GetComponentLookup<HealStateTag>(true),
//                 AutoGiveWayLookup = SystemAPI.GetComponentLookup<AutoGiveWayTag>(true),
//                 MovingStateLookup = SystemAPI.GetComponentLookup<MovingStateTag>(true),
//                 IdleStateLookup = SystemAPI.GetComponentLookup<IdleStateTag>(true),
//                 FormationMovingTagLookup = SystemAPI.GetComponentLookup<FormationMovingTag>(true),
//             };
//
//             state.Dependency = new CheckSurroundingJob
//             {
//                 AvoidanceConfig = SystemAPI.GetSingleton<AvoidanceConfig>(),
//                 SeparationConfig = SystemAPI.GetSingleton<SeparationConfig>(),
//                 Lookups = lookups,
//             }.ScheduleParallel(state.Dependency);
//            
//         }
//
//
//         [BurstCompile]
//         [WithNone(typeof(UnitDeadTag))]
//         private partial struct CheckSurroundingJob : IJobEntity
//         {
//             [ReadOnly] public AvoidanceConfig AvoidanceConfig;
//             [ReadOnly] public SeparationConfig SeparationConfig;
//             public SurroundingLookups Lookups;
//
//             private void Execute(ref DynamicBuffer<FakeColliderTarget> targets,
//                 ref Separation separation, ref Avoidance avoidance,
//                 ref Rnd rnd, Entity selfEntity)
//             {
//                 if (CheckShouldApplySeparationToSelf(ref separation, ref avoidance, selfEntity)) return;
//
//                 // Get self transform data
//                 var selfVelocity = Lookups.VelocityLookup[selfEntity].Value.xz;
//                 var transform = Lookups.LocalTransformLookup[selfEntity];
//                 var selfBox = Lookups.BoxColliderSizeLookup[selfEntity].SeparationBox;
//                 var selfHalf = new float2(selfBox.x, selfBox.z) * 0.5f;
//                 var selfPos = transform.Position.xz;
//                 var selfRotMat = MathUtils.GetRotationMatrix(transform.Rotation);
//                 var selfVelocityForward = math.normalizesafe(selfVelocity);
//                 var left = MovementUtils.GetLeftOrRight(new float3(selfVelocityForward.x, 0, selfVelocityForward.y),
//                     true);
//
//                 // Reset the temp data
//                 var bestSeparation = float2.zero;
//                 var bestOverlap = 0f;
//                 var leftTargetsCount = 0;
//                 var rightTargetsCount = 0;
//                 var shouldAddAvoidValue = false;
//                 for (var i = 0; i < targets.Length; i++)
//                 {
//                     if (IfInValidSeparationTarget(targets, i, out var otherTransform, out var triggerData)) continue;
//                     // Get the other transform data
//                     var otherPos3 = otherTransform.Position;
//                     var otherPos = otherPos3.xz;
//                     var otherBox = Lookups.BoxColliderSizeLookup[targets[i].Target].SeparationBox;
//                     var otherHalf = new float2(otherBox.x, otherBox.z) * 0.5f;
//                     var otherRotMat = MathUtils.GetRotationMatrix(otherTransform.Rotation);
//
//                     var delta = otherPos - selfPos;
//                     if (math.lengthsq(delta) < 0.001f)
//                     {
//                         bestSeparation = MathUtils.Get2D(ref rnd.value) * SeparationConfig.ValueWhenTotallyOverlapped;
//                         bestOverlap = 1f;
//                         break;
//                     }
//
//                     // Calculate the avoidance
//                     if (math.dot(selfVelocityForward, delta) > 0f)
//                     {
//                         var otherVelocity = float2.zero;
//                         if (Lookups.VelocityLookup.TryGetComponent(triggerData.BelongsTo, out var velocity))
//                             otherVelocity = new float2(velocity.Value.x, velocity.Value.z);
//                         var shouldWait = math.dot(otherVelocity, selfVelocity) > 0f;
//                         if (!shouldWait)
//                         {
//                             if (math.dot(left.xz, delta) > 0f) leftTargetsCount++;
//                             else rightTargetsCount++;
//                             if (IsTargetValidForAvoidance(triggerData))
//                             {
//                                 shouldAddAvoidValue = true;
//                             }
//                         }
//                     }
//
//                     MathUtils.ObbDetect(selfRotMat, otherRotMat, selfHalf, otherHalf, delta,
//                         out var minOverlap, out var
//                             overlapped, out var smallestAxis);
//
//                     // Update best separation
//                     if (overlapped)
//                     {
//                         if (minOverlap > bestOverlap)
//                         {
//                             bestOverlap = minOverlap;
//                             bestSeparation = -smallestAxis * minOverlap;
//                         }
//                     }
//                 }
//
//                 separation.Value = bestOverlap > 0 ? new float3(bestSeparation.x, 0, bestSeparation.y) : float3.zero;
//                 // Set the avoidance direction
//                 if (leftTargetsCount == 0 && rightTargetsCount == 0)
//                 {
//                     avoidance.Value = float3.zero;
//                 }
//                 else
//                 {
//                     avoidance.Value = leftTargetsCount > rightTargetsCount ? -left : left;
//                     if (shouldAddAvoidValue)
//                         avoidance.Value *= AvoidanceConfig.AddAvoidanceValueForInteractState;
//                 }
//             }
//
//             private bool CheckShouldApplySeparationToSelf(ref Separation separation, ref Avoidance avoidance, Entity selfEntity)
//             {
//                 if ((Lookups.FormationMovingTagLookup.HasComponent(selfEntity) &&
//                      Lookups.FormationMovingTagLookup.IsComponentEnabled(selfEntity))
//                     || Lookups.AutoGiveWayLookup.HasComponent(selfEntity) &&
//                     Lookups.AutoGiveWayLookup.IsComponentEnabled(selfEntity)
//                     || !Lookups.MovingStateLookup.HasComponent(selfEntity) || !Lookups.MovingStateLookup
//                         .IsComponentEnabled(selfEntity)
//                     || !Lookups.IdleStateLookup.HasComponent(
//                         selfEntity)
//                     || !Lookups.IdleStateLookup.IsComponentEnabled(
//                         selfEntity))
//                 {
//                     separation.Value = float3.zero;
//                     avoidance.Value = float3.zero;
//                     return true;
//                 }
//
//                 return false;
//             }
//
//
//             private bool IsTargetValidForAvoidance(FakeCollisionTriggerData triggerData)
//             {
//                 return (Lookups.AttackStateLookup.HasComponent(triggerData.BelongsTo) &&
//                         Lookups.AttackStateLookup.IsComponentEnabled(triggerData.BelongsTo))
//                        ||
//                        (Lookups.HealStateLookup.HasComponent(triggerData.BelongsTo) &&
//                         Lookups.HealStateLookup.IsComponentEnabled(triggerData.BelongsTo));
//             }
//
//             private bool IfInValidSeparationTarget(DynamicBuffer<FakeColliderTarget> targets, int i,
//                 out LocalTransform otherTransform,
//                 out FakeCollisionTriggerData triggerData)
//             {
//                 otherTransform = default;
//                 triggerData = default;
//                 if (!Lookups.LocalTransformLookup.TryGetComponent(targets[i].Target, out otherTransform))
//                     return true;
//                 if (!Lookups.TriggerDataLookup.TryGetComponent(targets[i].Target, out triggerData)) return true;
//                 if (Lookups.AutoGiveWayLookup.HasComponent(triggerData.BelongsTo) &&
//                     Lookups.AutoGiveWayLookup.IsComponentEnabled(triggerData.BelongsTo)) return true;
//                 return false;
//             }
//         }
//
//
//         [BurstCompile]
//         [WithNone(typeof(MovingStateTag))]
//         [WithNone(typeof(IdleStateTag))]
//         [WithNone(typeof(FormationMovingTag))]
//         private partial struct SeparationSetZeroJob : IJobEntity
//         {
//             private void Execute(ref Separation separation, ref Avoidance avoidance)
//             {
//                 separation.Value = float3.zero;
//                 avoidance.Value = float3.zero;
//             }
//         }
//
//         [BurstCompile]
//         [WithAny(typeof(FormationMovingTag), typeof(AutoGiveWayTag))]
//         public partial struct FormationMovingSeparationSteeringJob : IJobEntity
//         {
//             private void Execute(ref Separation separation, ref Avoidance avoidance)
//             {
//                 separation.Value = float3.zero;
//                 avoidance.Value = float3.zero;
//             }
//         }
//     }
// }