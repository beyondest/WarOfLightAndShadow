using System.Linq;
using System.Runtime.CompilerServices;
using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using SparFlame.GamePlaySystem.Movement;
using SparFlame.GamePlaySystem.Interact;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;


namespace SparFlame.GamePlaySystem.State
{
    [BurstCompile]
    [UpdateAfter(typeof(AutoChooseTargetSystem))]
    public partial struct InteractStateMachine : ISystem
    {
        private BufferLookup<InsightTarget> _insightTarget;

        private ComponentLookup<StatData> _stat;
        private ComponentLookup<LocalTransform> _localTransform;
        private ComponentLookup<InteractableAttr> _interactable;
        private ComponentLookup<BuffData> _buff;
        private ComponentLookup<MovableData> _movable;
        private ComponentLookup<BasicStateData> _basicState;

        private EntityQuery _attackEntityQuery;
        private EntityQuery _healEntityQuery;
        private EntityQuery _harvestEntityQuery;


        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<InteractStateMachineConfig>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<NotPauseTag>();


            _attackEntityQuery = SystemAPI.QueryBuilder().WithAllRW<AttackAbility>().WithAllRW<BasicStateData>()
                .WithAll<AttackStateTag>().Build();
            _healEntityQuery = SystemAPI.QueryBuilder().WithAllRW<HealAbility>().WithAllRW<BasicStateData>()
                .WithAll<HealStateTag>().Build();
            _harvestEntityQuery = SystemAPI.QueryBuilder().WithAllRW<HarvestAbility>().WithAllRW<BasicStateData>()
                .WithAll<HarvestStateTag>().Build();


            _stat = state.GetComponentLookup<StatData>(true);
            _localTransform = state.GetComponentLookup<LocalTransform>(true);
            _interactable = state.GetComponentLookup<InteractableAttr>(true);
            _buff = state.GetComponentLookup<BuffData>(true);
            _insightTarget = state.GetBufferLookup<InsightTarget>(true);
            _movable = state.GetComponentLookup<MovableData>();
            _basicState = state.GetComponentLookup<BasicStateData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var config = SystemAPI.GetSingleton<InteractStateMachineConfig>();
            _stat.Update(ref state);
            _localTransform.Update(ref state);
            _interactable.Update(ref state);
            _buff.Update(ref state);
            _movable.Update(ref state);
            _insightTarget.Update(ref state);
            _basicState.Update(ref state);
            var attackAbilities = _attackEntityQuery.ToComponentDataArray<AttackAbility>(Allocator.TempJob);
            var attackEntities = _attackEntityQuery.ToEntityArray(Allocator.TempJob);
            var attackJob = new InteractStateJob<AttackAbility>
            {
                Ability = attackAbilities,
                Entities = attackEntities,
                StatDataLookup = _stat,
                TransformLookup = _localTransform,
                InteractableLookup = _interactable,
                BuffDataLookup = _buff,
                InsightTarget = _insightTarget,
                ECB = ecb,
                MovableLookup = _movable,
                BasicStateData = _basicState,
            }.Schedule(attackEntities.Length, config.AttackJobBatchCount);
            var healAbilities = _healEntityQuery.ToComponentDataArray<HealAbility>(Allocator.TempJob);
            var healEntities = _healEntityQuery.ToEntityArray(Allocator.TempJob);
            var healJob = new InteractStateJob<HealAbility>
            {
                Ability = healAbilities,
                Entities = healEntities,
                StatDataLookup = _stat,
                TransformLookup = _localTransform,
                InteractableLookup = _interactable,
                BuffDataLookup = _buff,
                InsightTarget = _insightTarget,
                ECB = ecb,
                MovableLookup = _movable,
                BasicStateData = _basicState,
            }.Schedule(healEntities.Length, config.HealJobBatchCount);

            var harvestAbilities = _harvestEntityQuery.ToComponentDataArray<HarvestAbility>(Allocator.TempJob);
            var harvestEntities = _harvestEntityQuery.ToEntityArray(Allocator.TempJob);
            var harvestJob = new InteractStateJob<HarvestAbility>
            {
                Ability = harvestAbilities,
                Entities = harvestEntities,
                StatDataLookup = _stat,
                TransformLookup = _localTransform,
                InteractableLookup = _interactable,
                BuffDataLookup = _buff,
                InsightTarget = _insightTarget,
                ECB = ecb,
                MovableLookup = _movable,
                BasicStateData = _basicState,
            }.Schedule(healEntities.Length, config.HarvestJobBatchCount);

            attackJob.Complete();
            healJob.Complete();
            harvestJob.Complete();

            attackAbilities.Dispose();
            healAbilities.Dispose();
            harvestAbilities.Dispose();
            attackEntities.Dispose();
            healEntities.Dispose();
        }


        [BurstCompile]
        private struct InteractStateJob<TInteractAbility> : IJobParallelFor
            where TInteractAbility : struct, IInteractAbility
        {
            public NativeArray<TInteractAbility> Ability;
            public NativeArray<Entity> Entities;

            [ReadOnly] public ComponentLookup<StatData> StatDataLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public ComponentLookup<InteractableAttr> InteractableLookup;
            [ReadOnly] public ComponentLookup<BuffData> BuffDataLookup;
            [ReadOnly] public BufferLookup<InsightTarget> InsightTarget;

            // Change self state
            [NativeDisableParallelForRestriction] public ComponentLookup<MovableData> MovableLookup;

            // Change self state
            [NativeDisableParallelForRestriction] public ComponentLookup<BasicStateData> BasicStateData;

            [ReadOnly] public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter ECB;

            public void Execute(int index)
            {
                var entity = Entities[index];
                var ability = Ability[index];
                ref var stateData = ref BasicStateData.GetRefRW(entity).ValueRW;
                if (!InsightTarget.TryGetBuffer(entity, out var targetList)) return; // This should never return
                var selfFactionTag = InteractableLookup[entity].FactionTag;
                var targetStat = StatDataLookup[entity];

                var targetInteractAttr = InteractableLookup[stateData.TargetEntity];
                // Current target is invalid
                if (!InteractUtils.IsTargetValid(in targetInteractAttr.FactionTag, in selfFactionTag, in targetStat))
                {
                    // Focus interact state but target is invalid, exit focus state
                    if (stateData.Focus )
                    {
                        stateData.Focus = false;
                    }

                    // No enemy around , turn to idle
                    if (targetList.IsEmpty)
                    {
                        stateData.TargetEntity = Entity.Null;
                        stateData.TargetState = UnitState.Idle;
                        StateUtils.SwitchState(ref stateData, ECB, entity, index);
                    }
                    // Enemy insight, choose the highest value target
                    else
                    {
                        stateData.TargetEntity = StateUtils.ChooseTarget(in targetList);
                    }

                    return;
                }

                // Try to apply buff
                var rangeSq = ability.RangeSq;
                var amount = ability.BasicAmount;
                var speed = ability.Speed;
                if (BuffDataLookup.TryGetComponent(entity, out BuffData buffData))
                {
                    rangeSq *= buffData.InteractRangeMultiplier * buffData.InteractRangeMultiplier;
                    amount *= (int)buffData.InteractAmountMultiplier;
                    speed *= buffData.InteractSpeedMultiplier;
                    ECB.RemoveComponent<BuffData>(index, entity);
                }

                var curPos = TransformLookup.GetRefRO(entity).ValueRO.Position;
                var targetPos = TransformLookup.GetRefRO(stateData.TargetEntity).ValueRO.Position;

                // As long as target is valid, movable unit will never change target in interact state. The target can only be changed while moving
                if (!IsTargetInRange(rangeSq, in curPos, in targetPos))
                {
                    // Interacter is movable
                    if (MovableLookup.TryGetComponent(entity, out var movableData))
                    {
                        InteractMoveToTarget(ref stateData, ref movableData, in ability, entity, index);
                        return;
                    }

                    // Interacter is not movable, like building
                    // No enemy around, turn to idle
                    if (targetList.IsEmpty)
                    {
                        stateData.TargetState = UnitState.Idle;
                        stateData.TargetEntity = Entity.Null;
                        StateUtils.SwitchState(ref stateData, ECB, entity, index);
                    }
                    // Enemy in sight, choose the highest value target
                    else
                    {
                        stateData.TargetEntity = StateUtils.ChooseTarget(in targetList);
                    }

                    return;
                }

                // Try attack current target                
                PlayAnimationAudio(speed);
                if (++ability.CurCounter > (int)(1 / DeltaTime / speed))
                {
                    ability.CurCounter = 0;
                    SendStatChangeRequest(stateData.TargetEntity, amount, index, entity, ability.InteractType);
                }
            }


            private void PlayAnimationAudio(float count)
            {
                Debug.Log("Interact happened");
            }


            private void SendStatChangeRequest(Entity targetEntity, int amount, int index,
                Entity entity, InteractType interactType)
            {
                var request = ECB.CreateEntity(index);
                ECB.AddComponent(index, request, new StatChangeRequest
                {
                    Interactor = entity,
                    Interactee = targetEntity,
                    Amount = amount,
                    InteractType = interactType
                });
            }

            private void InteractMoveToTarget(ref BasicStateData stateData, ref MovableData movableData,
                in TInteractAbility ability, Entity entity, int index)
            {
                var tarPos = TransformLookup[stateData.TargetEntity].Position;
                var tarColliderShape = InteractableLookup[stateData.TargetEntity].BoxColliderSize;
                MovementUtils.SetMoveTarget(ref movableData, tarPos, tarColliderShape,
                    MovementCommandType.Interactive,
                    ability.RangeSq
                );
                stateData.TargetState = UnitState.Moving;
                StateUtils.SwitchState(ref stateData, ECB, entity, index);
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool IsTargetInRange(float rangeSq, in float3 curPos, in float3 targetPos)
            {
                var disSq = math.distancesq(curPos, targetPos);
                return disSq < rangeSq;
            }
        }
    }
}