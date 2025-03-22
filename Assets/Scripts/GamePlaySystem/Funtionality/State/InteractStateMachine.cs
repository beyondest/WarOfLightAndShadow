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
    [UpdateAfter(typeof(StatSystem))]
    [UpdateAfter(typeof(BuffSystem))]
    public partial struct InteractStateMachine : ISystem
    {
        private BufferLookup<InsightTarget> _insightTarget;

        private ComponentLookup<StatData> _stat;
        private ComponentLookup<LocalTransform> _localTransform;
        private ComponentLookup<InteractableAttr> _interactable;
        private ComponentLookup<BuffData> _buff;
        private ComponentLookup<MovableData> _movable;

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
            _movable = state.GetComponentLookup<MovableData>();
            _insightTarget = state.GetBufferLookup<InsightTarget>(true);
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

            var attackAbilities = _attackEntityQuery.ToComponentDataArray<AttackAbility>(Allocator.TempJob);
            var attackStateData = _attackEntityQuery.ToComponentDataArray<BasicStateData>(Allocator.TempJob);
            var attackEntities = _attackEntityQuery.ToEntityArray(Allocator.TempJob);
            var attackJob = new InteractStateJob<AttackAbility>
            {
                Ability = attackAbilities,
                StateData = attackStateData,
                Entities = attackEntities,
                StatDataLookup = _stat,
                TransformLookup = _localTransform,
                InteractableLookup = _interactable,
                BuffDataLookup = _buff,
                InsightTarget = _insightTarget,
                ECB = ecb,
            }.Schedule(attackEntities.Length, config.AttackJobBatchCount);
            var healAbilities = _healEntityQuery.ToComponentDataArray<HealAbility>(Allocator.TempJob);
            var healStateData = _healEntityQuery.ToComponentDataArray<BasicStateData>(Allocator.TempJob);
            var healEntities = _healEntityQuery.ToEntityArray(Allocator.TempJob);
            var healJob = new InteractStateJob<HealAbility>
            {
                Ability = healAbilities,
                StateData = healStateData,
                Entities = healEntities,
                StatDataLookup = _stat,
                TransformLookup = _localTransform,
                InteractableLookup = _interactable,
                BuffDataLookup = _buff,
                InsightTarget = _insightTarget,
                ECB = ecb
            }.Schedule(healEntities.Length, config.HealJobBatchCount);
            
            var harvestAbilities = _harvestEntityQuery.ToComponentDataArray<HarvestAbility>(Allocator.TempJob);
            var harvestStateData = _harvestEntityQuery.ToComponentDataArray<BasicStateData>(Allocator.TempJob);
            var harvestEntities = _harvestEntityQuery.ToEntityArray(Allocator.TempJob);
            var harvestJob = new InteractStateJob<HarvestAbility>
            {
                Ability = harvestAbilities,
                StateData = harvestStateData,
                Entities = harvestEntities,
                StatDataLookup = _stat,
                TransformLookup = _localTransform,
                InteractableLookup = _interactable,
                BuffDataLookup = _buff,
                InsightTarget = _insightTarget,
                ECB = ecb
            }.Schedule(healEntities.Length, config.HarvestJobBatchCount);
            attackJob.Complete();
            healJob.Complete();
            harvestJob.Complete();
            
            attackAbilities.Dispose();
            healAbilities.Dispose();
            harvestAbilities.Dispose();
            attackStateData.Dispose();
            healStateData.Dispose();
            harvestStateData.Dispose();
            attackEntities.Dispose();
            healEntities.Dispose();
            healEntities.Dispose();
        }


        [BurstCompile]
        private struct InteractStateJob<TInteractAbility> : IJobParallelFor
            where TInteractAbility : struct, IInteractAbility
        {
            public NativeArray<TInteractAbility> Ability;
            public NativeArray<BasicStateData> StateData;
            public NativeArray<Entity> Entities;

            [ReadOnly] public ComponentLookup<StatData> StatDataLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public ComponentLookup<InteractableAttr> InteractableLookup;
            [ReadOnly] public ComponentLookup<BuffData> BuffDataLookup;
            [ReadOnly] public BufferLookup<InsightTarget> InsightTarget;
            [NativeDisableParallelForRestriction] public ComponentLookup<MovableData> MovableData;

            [ReadOnly] public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter ECB;

            public void Execute(int index)
            {
                var entity = Entities[index];
                var ability = Ability[index];
                var stateData = StateData[index];

                if (!InsightTarget.TryGetBuffer(entity, out var targetList)) return; // This should never return

                // Current target is not valid
                if (!IsTargetValid(stateData.TargetEntity, in ability))
                {
                    // Focus state, no enemy insight or memory target is insight, continue command
                    if (stateData.Focus && (targetList.IsEmpty || targetList.Contains(new InsightTarget
                        {
                            Entity = stateData.MemoryEntity
                        })))
                    {
                        // Memory command is pure march or memory target is valid
                        if (stateData.MemoryState == UnitState.Idle
                            ||(stateData.MemoryState != UnitState.Idle &&
                             IsTargetValid(stateData.MemoryEntity, in ability)))
                        {
                            StateUtils.ContinueLastCommand(ref stateData, ECB, entity, index);
                            return;
                        }
                        // Memory target is invalid, exit focus state
                        else
                        {
                            stateData.Focus = false;
                            stateData.MemoryEntity = Entity.Null;
                        }
                    }
                    // No enemy around and not focus, turn to idle
                    if (targetList.IsEmpty)
                    {
                        stateData.TargetState = UnitState.Idle;
                        StateUtils.SwitchState(ref stateData, ECB, entity, index);
                    }
                    // Enemy insight but not focus, or enemy insight but not contain memory target, choose the highest value target
                    else
                    {
                        stateData.TargetEntity = targetList[0].Entity;
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
                }

                var curPos = TransformLookup.GetRefRO(entity).ValueRO.Position;
                var targetPos = TransformLookup.GetRefRO(stateData.TargetEntity).ValueRO.Position;


                if (!IsTargetInRange(rangeSq, in curPos, in targetPos))
                {
                    // Interacter is movable
                    if (MovableData.TryGetComponent(entity, out var movableData))
                    {
                        InteractMoveToTarget(ref stateData, ref movableData, in ability, entity, index);
                        return;
                    }
                    // Interacter is not movable, like building
                    else
                    {
                        // No enemy around, turn to idle
                        if (targetList.IsEmpty)
                        {
                            stateData.TargetState = UnitState.Idle;
                            StateUtils.SwitchState(ref stateData, ECB, entity, index);
                        }
                        // Enemy in sight, choose the highest value target
                        else
                        {
                            stateData.TargetEntity = targetList[0].Entity;
                        }
                        return;
                    }
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
                Debug.Log("Interact");
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
                var tarPos = TransformLookup.GetRefRO(stateData.TargetEntity).ValueRO.Position;
                var tarColliderShape = InteractableLookup.GetRefRO(stateData.TargetEntity).ValueRO.BoxColliderSize;
                MovementUtils.SetMoveTarget(ref movableData, tarPos, tarColliderShape,
                    MovementCommandType.Interactive,
                    ability.RangeSq
                );
                stateData.TargetState = UnitState.Moving;
                StateUtils.SwitchState(ref stateData, ECB, entity, index);
            }

            private bool IsTargetValid(Entity target, in TInteractAbility ability)
            {
                // Target is dead or not valid
                if (
                    !StatDataLookup.TryGetComponent(target, out StatData statData)
                    || statData.CurValue <= 0)
                {
                    return false;
                }

                // Healing state but target stat is already full. If it is in healing state, target should be ally unit, this logic is determined by Auto Choose System
                if (ability.InteractType == InteractType.Heal && statData.CurValue >= statData.MaxValue)
                {
                    return false;
                }

                return true;
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