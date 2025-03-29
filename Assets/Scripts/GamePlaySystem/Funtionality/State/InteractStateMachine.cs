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
            state.RequireForUpdate<SightSystemConfig>();
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
            _interactable = state.GetComponentLookup<InteractableAttr>(true);
            _insightTarget = state.GetBufferLookup<InsightTarget>(true);
            _healState = state.GetComponentLookup<HealStateTag>(true);
            _harvestState = state.GetComponentLookup<HarvestStateTag>(true);
            _localTransform = state.GetComponentLookup<LocalTransform>();
            _movable = state.GetComponentLookup<MovableData>();
            _basicState = state.GetComponentLookup<BasicStateData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var config = SystemAPI.GetSingleton<InteractStateMachineConfig>();
            var sightSystemConfig = SystemAPI.GetSingleton<SightSystemConfig>();
            _stat.Update(ref state);
            _localTransform.Update(ref state);
            _interactable.Update(ref state);
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
                InsightTarget = _insightTarget,
                ECB = ecb,
                MovableLookup = _movable,
                BasicStateData = _basicState,
                HealLookup = _healState,
                HarvestLookup = _harvestState,
                DeltaTime = deltaTime,
                InteractTurnSpeed = config.InteractTurnSpeed,
                Config = sightSystemConfig
            }.Schedule(attackEntities.Length, config.AttackJobBatchCount, state.Dependency);
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
                InsightTarget = _insightTarget,
                ECB = ecb,
                MovableLookup = _movable,
                BasicStateData = _basicState,
                HealLookup = _healState,
                HarvestLookup = _harvestState,
                DeltaTime = deltaTime,
                InteractTurnSpeed = config.InteractTurnSpeed,
                Config = sightSystemConfig
            }.Schedule(healEntities.Length, config.HealJobBatchCount, state.Dependency);
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
                InsightTarget = _insightTarget,
                ECB = ecb,
                MovableLookup = _movable,
                BasicStateData = _basicState,
                HealLookup = _healState,
                HarvestLookup = _harvestState,
                DeltaTime = deltaTime,
                InteractTurnSpeed = config.InteractTurnSpeed,
                Config = sightSystemConfig
            }.Schedule(harvestEntities.Length, config.HarvestJobBatchCount, state.Dependency);
            state.Dependency = harvestJob;

            attackJob.Complete();
            healJob.Complete();
            harvestJob.Complete();

            attackEntities.Dispose();
            attackAbilities.Dispose();
            healEntities.Dispose();
            healAbilities.Dispose();
            harvestEntities.Dispose();
            harvestAbilities.Dispose();
        }


        [BurstCompile]
        private struct InteractStateJob<TInteractAbility> : IJobParallelFor
            where TInteractAbility : struct, IInteractAbility
        {
            public NativeArray<TInteractAbility> Ability;
            public NativeArray<Entity> Entities;

            [ReadOnly] public ComponentLookup<StatData> StatDataLookup;
            [ReadOnly] public ComponentLookup<InteractableAttr> InteractableLookup;
            [ReadOnly] public ComponentLookup<HealStateTag> HealLookup;
            [ReadOnly] public ComponentLookup<HarvestStateTag> HarvestLookup;
            [ReadOnly] public BufferLookup<InsightTarget> InsightTarget;

            [ReadOnly] public float InteractTurnSpeed;

            // Turn rotation to target
            [NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> TransformLookup;

            // Change self state
            [NativeDisableParallelForRestriction] public ComponentLookup<MovableData> MovableLookup;

            // Change self state
            [NativeDisableParallelForRestriction] public ComponentLookup<BasicStateData> BasicStateData;

            [ReadOnly] public float DeltaTime;
            [ReadOnly] public SightSystemConfig Config;
            public EntityCommandBuffer.ParallelWriter ECB;

            public void Execute(int index)
            {
                var entity = Entities[index];
                var ability = Ability[index];
                ref var selfStateData = ref BasicStateData.GetRefRW(entity).ValueRW;
                var selfFactionTag = InteractableLookup[entity].FactionTag;
                // This should check in every state machine, because switch state tag only happens in next frame dur to ecb playback
                if (selfStateData.CurState != UnitState.Attacking
                    && selfStateData.TargetState != UnitState.Healing
                    && selfStateData.TargetState != UnitState.Harvesting) return;

                if (!InsightTarget.TryGetBuffer(entity, out var targetList)) return; // This should never return

                // Check if target is valid
                /*Target is dead and removed. This may happen when it kills target after exactly this attack, but the entity is removed next frame end
                This may happen due to truly switch state always happen in te end of frame(ECB Playback) .
                Fake Interact State , next frame will turn to another state*/
                bool isTargetValid;
                if (!InteractableLookup.TryGetComponent(selfStateData.TargetEntity, out var targetInteractAttr)
                    || !StatDataLookup.TryGetComponent(selfStateData.TargetEntity, out var targetStat))
                {
                    selfStateData.TargetEntity = Entity.Null;
                    isTargetValid = false;
                }
                else
                {
                    isTargetValid = InteractUtils.IsTargetValid(in targetInteractAttr, in selfFactionTag,
                        in targetStat, HealLookup.HasComponent(entity), HarvestLookup.HasComponent(entity));
                }

                if (!isTargetValid) // Current target is invalid
                {
                    // Focus interact state but target is invalid, exit focus state
                    selfStateData.Focus = false;
                    // No enemy around , turn to idle
                    if (targetList.IsEmpty)
                    {
                        selfStateData.TargetEntity = Entity.Null;
                        selfStateData.TargetState = UnitState.Idle;
                    }
                    // Target insight, choose the highest value target
                    else
                    {
                        selfStateData.TargetEntity = InteractUtils.ChooseTarget(in targetList);
                        StateUtils.SetTargetStateViaTargetType(in selfFactionTag,
                            InteractableLookup[selfStateData.TargetEntity],
                            ref selfStateData);
                    }

                    StateUtils.SwitchState(ref selfStateData, ECB, entity, index);
                    return;
                }

                // Player can decide whether unit can switch target during interact state
                if (ShouldChangeTarget(ref selfStateData, HealLookup.HasComponent(entity),
                        in targetList, entity))
                {
                    StateUtils.SetTargetStateViaTargetType(in selfFactionTag,
                        InteractableLookup[selfStateData.TargetEntity], ref selfStateData);
                    StateUtils.SwitchState(ref selfStateData, ECB, entity, index);
                    return;
                }

                
              

                ref var transform = ref TransformLookup.GetRefRW(entity).ValueRW;
                var curPos = transform.Position;
                var targetPos = TransformLookup[selfStateData.TargetEntity].Position;

                // Check if target in range
                /*As long as target is valid, movable unit will never change target in interact state.
                 The target can only be changed while moving*/
                if (!IsTargetInRange(ability.Range, in curPos, in targetPos, in targetInteractAttr))
                {
                    // Interacter is movable
                    if (MovableLookup.HasComponent(entity))
                    {
                        ref var movableData = ref MovableLookup.GetRefRW(entity).ValueRW;
                        InteractMoveToTarget(ref selfStateData, ref movableData, in ability, entity, index);
                        return;
                    }

                    // Interacter is not movable, like building
                    if (targetList.IsEmpty)
                    {
                        // No enemy around, turn to idle
                        selfStateData.TargetState = UnitState.Idle;
                        selfStateData.TargetEntity = Entity.Null;
                        StateUtils.SwitchState(ref selfStateData, ECB, entity, index);
                    }
                    else
                    {
                        // Enemy in sight, choose the highest value target
                        selfStateData.TargetEntity = InteractUtils.ChooseTarget(in targetList);
                    }

                    return;
                }

                // Look at target
                var targetRotation = quaternion.LookRotationSafe(-(targetPos - curPos), math.up());
                transform.Rotation =
                    math.slerp(transform.Rotation.value, targetRotation, DeltaTime * InteractTurnSpeed);

                // Try attack current target                
                PlayAnimationAudio(ability.Speed);
                var counter = 1 / DeltaTime / ability.Speed;
                if (++selfStateData.InteractCounter > (int)counter)
                {
                    selfStateData.InteractCounter = 0;
                    SendStatChangeRequest(selfStateData.TargetEntity, ability.Amount, index, entity, ability.InteractType);
                }
            }


            private bool ShouldChangeTarget(ref BasicStateData selfStateData, bool heal,
                in DynamicBuffer<InsightTarget> targets, Entity selfEntity)
            {
                // Focus mode cannot switch target unless this is a healer and healer heal first
                if (selfStateData.Focus && (!heal || !Config.HealerAlwaysHealFirst)) return false;
                
                var selfStatData = StatDataLookup[selfEntity];
                var originalTarget = selfStateData.TargetEntity;
                // Heal self first
                if (heal && selfStatData.CurValue < selfStatData.MaxValue )
                {
                    selfStateData.TargetEntity = selfEntity;
                    return originalTarget != selfStateData.TargetEntity;
                }

                // If dynamic choose target
                if (Config.DynamicChooseTargetInInteract && !targets.IsEmpty)
                {
                    selfStateData.TargetEntity = InteractUtils.ChooseTarget(in targets);
                    return originalTarget != selfStateData.TargetEntity;
                }

                return false;
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
                    ability.Range
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
            private static bool IsTargetInRange(float rangeSq, in float3 curPos, in float3 targetPos,
                in InteractableAttr targetInteractAttr)
            {
                var curPos2 = new float2(curPos.x, curPos.z);
                var targetPos2 = new float2(targetPos.x, targetPos.z);
                var targetColliderSizeXz = new float2(targetInteractAttr.BoxColliderSize.x,
                    targetInteractAttr.BoxColliderSize.z);
                var disSqPointToRect = MovementUtils.DistanceSqPointToRect(targetPos2, targetColliderSizeXz, curPos2);
                return disSqPointToRect < rangeSq;
            }
        }
    }
}