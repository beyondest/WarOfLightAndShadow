using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Systems.SubGameplay.Interact;
using SparFlame.Systems.SubGameplay.Movement;
using SparFlame.Systems.SubGameplay.StateMachine;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Systems.SubGameplay.EnemyAI
{
    [UpdateBefore(typeof(MovementSystem))]
    public partial struct EnemyUnitCommandSystem : ISystem
    {
        private ComponentLookup<BoxColliderSize> _generalAttr;
        private ComponentLookup<AttackAbility> _attackability;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GarrisonSystemConfig>();
            state.RequireForUpdate<AIUnitCommandUpdate>();
            state.RequireForUpdate<SubGamingTag>();
            state.RequireForUpdate<AIUnitCommandSystemConfig>();
            state.RequireForUpdate<AIUnitCommandUpdate>();
            _generalAttr = state.GetComponentLookup<BoxColliderSize>(true);
            _attackability = state.GetComponentLookup<AttackAbility>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            _generalAttr.Update(ref state);
            _attackability.Update(ref state);
            var config = SystemAPI.GetSingleton<AIUnitCommandSystemConfig>();
            new EnemyUnitCommandJob
            {
                GeneralAttrLookUp = _generalAttr,
                AttackAbilityLookUp = _attackability,
                GarrisonRangeSq = SystemAPI.GetSingleton<GarrisonSystemConfig>().GarrisonRadiusSq,
                ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
            }.ScheduleParallel();
        }


        [BurstCompile]
        [WithAll(typeof(AIUnitCommandUpdate))]
        private partial struct EnemyUnitCommandJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public ComponentLookup<AttackAbility> AttackAbilityLookUp;
            [ReadOnly] public ComponentLookup<BoxColliderSize> GeneralAttrLookUp;
            [ReadOnly] public float GarrisonRangeSq;
            [ReadOnly] public AIUnitCommandSystemConfig Config;

            private void Execute([ChunkIndexInQuery] int index, ref AIUnitCommandData commandData,
                ref BasicStateData basicStateData, ref MovableData movableData,
                ref DynamicBuffer<InsightTarget> targets,
                Entity entity)
            {
                ECB.SetComponentEnabled<AIUnitCommandUpdate>(index, entity, false);
                var targetEntity = commandData.TargetEntity;
                var focus = commandData.Focus;
                var targetPos = commandData.TargetPos;
                var targetColliderShape = float3.zero;
                if (GeneralAttrLookUp.TryGetComponent(commandData.TargetEntity, out var boxColliderSize))
                {
                    targetColliderShape = boxColliderSize.Value;
                }
                switch (commandData.CommandType)
                {
                    case AICommandType.None:
                        break;
                    case AICommandType.March:
                        targetColliderShape = new float3(Config.aiMarchExtent, 1f, Config.aiMarchExtent);
                        MovementUtils.SetMoveTarget(ref movableData, targetPos, targetColliderShape,
                            MovementCommandType.March, 0f);
                        basicStateData.TargetState = InteractState.Moving;
                        StateUtils.SwitchState(ref basicStateData, ECB, entity, index);
                        // Remove target so that player commandData it to move than it will move
                        if (basicStateData.TargetEntity != Entity.Null)
                        {
                            InteractUtils.Remove(ref targets, basicStateData.TargetEntity);
                        }

                        basicStateData.TargetEntity = Entity.Null;
                        basicStateData.TargetState = InteractState.Idle;
                        basicStateData.Focus = focus;
                        break;
                    case AICommandType.Garrison:
                        MovementUtils.SetMoveTarget(ref movableData, targetPos, targetColliderShape,
                            MovementCommandType.Interactive, GarrisonRangeSq);
                        basicStateData.TargetState = InteractState.Moving;
                        StateUtils.SwitchState(ref basicStateData, ECB, entity, index);
                        basicStateData.TargetEntity = targetEntity;
                        basicStateData.Focus = focus;
                        basicStateData.TargetState = InteractState.Garrison;
                        break;
                    case AICommandType.Attack:
                        if (AttackAbilityLookUp.TryGetComponent(entity, out var attackAbility))
                        {
                            MovementUtils.SetMoveTarget(ref movableData, targetPos,
                                targetColliderShape,
                                MovementCommandType.Interactive, attackAbility.Range);
                            basicStateData.TargetState = InteractState.Moving;
                            StateUtils.SwitchState(ref basicStateData, ECB, commandData.TargetEntity, index);
                            basicStateData.Focus = commandData.Focus;
                            basicStateData.TargetState = InteractState.Attacking;
                            basicStateData.TargetEntity = targetEntity;
                            InteractUtils.NoDupAdd(ref targets, new InsightTarget
                            {
                                Entity = basicStateData.TargetEntity
                            });
                        }
                        else // Cleric in troop
                        {
                            targetColliderShape = new float3(Config.aiMarchExtent, 1f, Config.aiMarchExtent);
                            MovementUtils.SetMoveTarget(ref movableData, targetPos, targetColliderShape,
                                MovementCommandType.March, 0f);
                            basicStateData.TargetState = InteractState.Moving;
                            StateUtils.SwitchState(ref basicStateData, ECB, entity, index);
                            // Remove target so that player commandData it to move than it will move
                            if (basicStateData.TargetEntity != Entity.Null)
                            {
                                InteractUtils.Remove(ref targets, basicStateData.TargetEntity);
                            }

                            basicStateData.TargetEntity = Entity.Null;
                            basicStateData.TargetState = InteractState.Idle;
                            basicStateData.Focus = focus;
                        }

                        break;
                }

                EnemyAIUtils.ResetPendingCommand(ref commandData);
            }
        }
    }
}