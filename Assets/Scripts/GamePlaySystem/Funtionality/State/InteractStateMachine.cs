using System;
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


namespace SparFlame.GamePlaySystem.State
{
    
    // TODO : Split to 3 systems, and use 3 ijobentity, see if that can schedule parallel
    [BurstCompile]
    [UpdateAfter(typeof(BuffSystem))]
    [UpdateAfter(typeof(SightUpdateListSystem))]
    [UpdateBefore(typeof(StatSystem))]
    public partial struct InteractStateMachine : ISystem
    {
        private BufferLookup<InsightTarget> _insightTarget;

        private ComponentLookup<StatData> _stat;
        private ComponentLookup<LocalTransform> _localTransform;
        private ComponentLookup<InteractableAttr> _interactable;
        private ComponentLookup<BuffData> _buff;
        private ComponentLookup<MovableData> _movable;
        private ComponentLookup<BasicStateData> _basicState;
        private ComponentLookup<HealStateTag> _healState;
        private ComponentLookup<HarvestStateTag> _harvestState;

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
            _healState = state.GetComponentLookup<HealStateTag>(true);
            _harvestState = state.GetComponentLookup<HarvestStateTag>(true);
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
            _healState.Update(ref state);
            _harvestState.Update(ref state);
            
            var attackAbilities = _attackEntityQuery.ToComponentDataArray<AttackAbility>(Allocator.TempJob);
            var attackEntities = _attackEntityQuery.ToEntityArray(Allocator.TempJob);
            state.Dependency.Complete();
            var deltaTime = SystemAPI.Time.DeltaTime;
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
                HealLookup = _healState,
                HarvestLookup = _harvestState,
                DeltaTime = deltaTime
            }.Schedule(attackEntities.Length, config.AttackJobBatchCount,state.Dependency);
            state.Dependency = attackJob;
            
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
                HealLookup = _healState,
                HarvestLookup = _harvestState,
                DeltaTime = deltaTime
            }.Schedule(healEntities.Length, config.HealJobBatchCount,state.Dependency);
            state.Dependency = healJob;
            
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
                HealLookup = _healState,
                HarvestLookup = _harvestState,
                DeltaTime = deltaTime
            }.Schedule(harvestEntities.Length, config.HarvestJobBatchCount,state.Dependency);
            state.Dependency = harvestJob;
            
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
            [ReadOnly] public ComponentLookup<HealStateTag> HealLookup;
            [ReadOnly] public ComponentLookup<HarvestStateTag> HarvestLookup;
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
                ref var selfStatData = ref BasicStateData.GetRefRW(entity).ValueRW;
                var selfFactionTag = InteractableLookup[entity].FactionTag;
                if (!InsightTarget.TryGetBuffer(entity, out var targetList)) return; // This should never return
                
                // Check if target is valid
                
                // This may happen due to truly switch state always happen in te end of frame(ECB Playback) .
                // Fake Interact State , next frame will turn to another state
                if(selfStatData.TargetEntity == Entity.Null)return;
                
                var isTargetValid = true;
                
                // Target is dead and removed. This may happen when it kills target after exactly this attack, but the entity is removed next frame end
                if (!InteractableLookup.TryGetComponent(selfStatData.TargetEntity, out var targetInteractAttr))
                {
                    selfStatData.TargetEntity = Entity.Null;
                    isTargetValid = false;
                }
                if (selfStatData.TargetEntity != Entity.Null)
                {
                    var targetStat = StatDataLookup[selfStatData.TargetEntity];
                    isTargetValid = InteractUtils.IsTargetValid(in targetInteractAttr, in selfFactionTag,
                        in targetStat,HealLookup.HasComponent(entity),HarvestLookup.HasComponent(entity) );
                }
                
                // Current target is invalid
                if (!isTargetValid)
                {
                    // Focus interact state but target is invalid, exit focus state
                    if (selfStatData.Focus )
                    {
                        selfStatData.Focus = false;
                    }
                    // No enemy around , turn to idle
                    if (targetList.IsEmpty)
                    {
                        selfStatData.TargetEntity = Entity.Null;
                        selfStatData.TargetState = UnitState.Idle;
                    }
                    // Target insight, choose the highest value target
                    else
                    {
                        selfStatData.TargetEntity = StateUtils.ChooseTarget(in targetList);
                        targetInteractAttr = InteractableLookup[selfStatData.TargetEntity];
                        if (selfFactionTag == targetInteractAttr.FactionTag)
                            selfStatData.TargetState = UnitState.Healing;
                        if(targetInteractAttr.BaseTag == BaseTag.Resources)
                            selfStatData.TargetState = UnitState.Harvesting;
                        if(selfFactionTag == ~targetInteractAttr.FactionTag)
                            selfStatData.TargetState = UnitState.Attacking;
                    }
                    StateUtils.SwitchState(ref selfStatData, ECB, entity, index);
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

                var curPos = TransformLookup[entity].Position;
                var targetPos = TransformLookup[selfStatData.TargetEntity].Position;

                
                // As long as target is valid, movable unit will never change target in interact state. The target can only be changed while moving
                if (!IsTargetInRange(rangeSq, in curPos, in targetPos, in targetInteractAttr))
                {
                    // Interacter is movable
                    if (MovableLookup.HasComponent(entity))
                    {
                        ref var movableData = ref MovableLookup.GetRefRW(entity).ValueRW;
                        InteractMoveToTarget(ref selfStatData, ref movableData, in ability, entity, index);
                        return;
                    }

                    // Interacter is not movable, like building
                    if (targetList.IsEmpty)
                    {
                        // No enemy around, turn to idle
                        selfStatData.TargetState = UnitState.Idle;
                        selfStatData.TargetEntity = Entity.Null;
                        StateUtils.SwitchState(ref selfStatData, ECB, entity, index);
                    }
                    else
                    {
                        // Enemy in sight, choose the highest value target
                        selfStatData.TargetEntity = StateUtils.ChooseTarget(in targetList);
                    }

                    return;
                }

                // Try attack current target                
                PlayAnimationAudio(speed);
                var counter = 1 / DeltaTime / speed;
                if (++selfStatData.InteractCounter > (int)counter)
                {
                    selfStatData.InteractCounter = 0;
                    SendStatChangeRequest(selfStatData.TargetEntity, amount, index, entity, ability.InteractType);
                }
            }


            private void PlayAnimationAudio(float count)
            {
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
                stateData.TargetState = ability.InteractType switch
                {
                    InteractType.Attack => UnitState.Attacking,
                    InteractType.Heal => UnitState.Healing,
                    InteractType.Harvest => UnitState.Harvesting,
                    _ => throw new ArgumentOutOfRangeException()
                };
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool IsTargetInRange(float rangeSq, in float3 curPos, in float3 targetPos, in InteractableAttr targetInteractAttr)
            {
                var curPos2 = new float2(curPos.x, curPos.z);
                var targetPos2 = new float2(targetPos.x, targetPos.z);
                var targetColliderSizeXz = new float2(targetInteractAttr.BoxColliderSize.x, targetInteractAttr.BoxColliderSize.z);
                var disSqPointToRect = MovementUtils.DistanceSqPointToRect(targetPos2,targetColliderSizeXz,curPos2);
                return disSqPointToRect < rangeSq;
            }
        }
    }
}