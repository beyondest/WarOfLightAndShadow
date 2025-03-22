// using System.Linq;
// using System.Runtime.CompilerServices;
// using SparFlame.GamePlaySystem.General;
// using Unity.Entities;
// using Unity.Collections;
// using Unity.Transforms;
// using SparFlame.GamePlaySystem.Movement;
// using SparFlame.GamePlaySystem.Interact;
// using Unity.Burst;
// using Unity.Mathematics;
//
//
// 
// namespace SparFlame.GamePlaySystem.State
// {
//     [BurstCompile]
//     [UpdateAfter(typeof(SortInsightTargetSysetm))]
//     [UpdateAfter(typeof(HealthSystem))]
//     [UpdateAfter(typeof(BuffSystem))]
//     public partial struct AttackingStateMachine : ISystem
//     {
//
//         private ComponentLookup<HealthData> _health;
//         private ComponentLookup<LocalTransform> _localTransform;
//         private ComponentLookup<InteractBasicData> _interactBasic;
//         private ComponentLookup<BuffData> _buff;
//         private BufferLookup<InsightTarget> _insightTarget;
//         
//         
//         [BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
//             state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
//             state.RequireForUpdate<NotPauseTag>();
//             state.RequireForUpdate<AttackStateMachineConfig>();
//             _health = state.GetComponentLookup<HealthData>(true);
//             _localTransform = state.GetComponentLookup<LocalTransform>(true);
//             _interactBasic = state.GetComponentLookup<InteractBasicData>(true);
//             _buff = state.GetComponentLookup<BuffData>(true);
//             _insightTarget = state.GetBufferLookup<InsightTarget>(true);
//             
//         }
//
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
//             _health.Update(ref state);
//             _localTransform.Update(ref state);
//             _interactBasic.Update(ref state);
//             _buff.Update(ref state);
//             _insightTarget.Update(ref state);
//
//             new AttackJob
//             {
//                 ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
//                 HealthData = _health,
//                 TransformLookup = _localTransform,
//                 InteractBasicDataLookup = _interactBasic,
//                 BuffDataLookup = _buff,
//                 TargetList = _insightTarget,
//                 DeltaTime = SystemAPI.Time.DeltaTime
//             }.ScheduleParallel();
//             
//         }
//         
//         
//         
//         [BurstCompile]
//         [WithAll(typeof(AttackStateTag))]
//         public partial struct AttackJob : IJobEntity
//         {
//             public EntityCommandBuffer.ParallelWriter ECB;
//             [ReadOnly] public ComponentLookup<HealthData> HealthData;
//             [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
//             [ReadOnly] public ComponentLookup<InteractBasicData> InteractBasicDataLookup;
//             [ReadOnly] public ComponentLookup<BuffData> BuffDataLookup;
//             [ReadOnly] public BufferLookup<InsightTarget> TargetList;
//             [ReadOnly] public float DeltaTime;
//
//             private void Execute([ChunkIndexInQuery] int index, ref UnitBasicStateData stateData,
//                 ref MovableData movableData,
//                 ref AttackAbility ability,
//                 Entity entity)
//             {
//                 if (!TargetList.TryGetBuffer(entity, out var targetList)) return;
//                 // Current target is not alive
//                 if (!IsTargetAlive(ref stateData))
//                 {
//                     // No enemy in sight or memory target not in sight
//                     if (targetList.IsEmpty || (stateData.Focus && targetList.Contains(new InsightTarget
//                         {
//                             Target = stateData.MemoryEntity
//                         })))
//                     {
//                         StateUtils.ContinueLastCommand(ref stateData, ECB, entity, index);
//                         return;
//                     }
//
//                     // No enemy around and no memory command, turn to idle
//                     if (targetList.IsEmpty)
//                     {
//                         stateData.TargetState = UnitState.Idle;
//                         StateUtils.SwitchState(ref stateData, ECB, entity, index);
//                     }
//                     // Enemy in sight and no memory command, choose the highest value target
//                     else
//                     {
//                         stateData.TargetEntity = targetList[0].Target;
//                     }
//
//                     return;
//                 }
//
//                 var curPos = TransformLookup.GetRefRO(entity).ValueRO.Position;
//                 var targetPos = TransformLookup.GetRefRO(stateData.TargetEntity).ValueRO.Position;
//
//                 // Try to apply buff
//                 var rangeSq = ability.RangeSq;
//                 var amount = ability.BasicAmount;
//                 var count = ability.Count;
//                 if (BuffDataLookup.TryGetComponent(entity, out BuffData buffData))
//                 {
//                     rangeSq *= buffData.InteractRangeMultiplier * buffData.InteractRangeMultiplier;
//                     amount *= (int)buffData.InteractAmountMultiplier;
//                     count *= buffData.InteractSpeedMultiplier;
//                 }
//
//                 // Current target is not in range
//                 if (!IsTargetInRange(rangeSq, in curPos, in targetPos))
//                 {
//                     AttackMoveToTarget(ref stateData, ref movableData, in ability, entity, index);
//                     return;
//                 }
//                 
//                 // Try attack current target                
//                 PlayAnimationAudio(count);
//                 if (++ability.CurCounter > (int)(1 / DeltaTime / count))
//                 {
//                     ability.CurCounter = 0;
//                     SendDamageDealtRequest(stateData.TargetEntity, amount, index, entity);
//                 }
//             }
//
//             private void PlayAnimationAudio(float count)
//             {
//             }
//
//
//             private void SendDamageDealtRequest(Entity targetEntity, int amount, int index,
//                 Entity entity)
//             {
//                 ECB.AddComponent(index, targetEntity, new DamageDealtRequest
//                 {
//                     Attacker = entity,
//                     Amount = amount,
//                     InteractType = InteractType.Attack
//                 });
//             }
//
//             private void AttackMoveToTarget(ref UnitBasicStateData stateData, ref MovableData movableData,
//                 in AttackAbility ability, Entity entity, int index)
//             {
//                 var tarPos = TransformLookup.GetRefRO(stateData.TargetEntity).ValueRO.Position;
//                 var tarColliderShape = InteractBasicDataLookup.GetRefRO(stateData.TargetEntity).ValueRO.BoxColliderSize;
//                 MovementUtils.SetMoveTarget(ref movableData, tarPos, tarColliderShape, MovementCommandType.Interactive,
//                     ability.RangeSq
//                 );
//                 stateData.TargetState = UnitState.Moving;
//                 StateUtils.SwitchState(ref stateData, ECB, entity, index);
//             }
//
//
//             [MethodImpl(MethodImplOptions.AggressiveInlining)]
//             private static bool IsTargetInRange(float rangeSq, in float3 curPos, in float3 targetPos)
//             {
//                 var disSq = math.distancesq(curPos, targetPos);
//                 return disSq < rangeSq;
//             }
//
//             private bool IsTargetAlive(ref UnitBasicStateData stateData)
//             {
//                 if (!HealthData.TryGetComponent(stateData.TargetEntity, out HealthData healthData)) return false;
//                 return healthData.Value > 0;
//             }
//         }
//     }
// }