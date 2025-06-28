using SparFlame.Components.General;
using SparFlame.Components.Input;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Components.VFX;
using SparFlame.Systems.SubGameplay.Interact;
using SparFlame.Systems.SubGameplay.Movement;
using SparFlame.Systems.SubGameplay.StateMachine;
using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.Systems.SubGameplay.Command
{
    [BurstCompile]
    [UpdateBefore(typeof(MovementSystem))]
    public partial struct PlayerCommandSystem : ISystem
    {
        private ComponentLookup<InArmyGroup> _inArmyGroupLookup;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<SubGamingTag>();
            state.RequireForUpdate<PlayerCommandConfig>();
            state.RequireForUpdate<InputUnitControlData>();
            state.RequireForUpdate<SubGameplayCursorData>();
            state.RequireForUpdate<InputMouseData>();
            state.RequireForUpdate<UnitSelectionData>();
            state.RequireForUpdate<GarrisonSystemConfig>();
            _inArmyGroupLookup = state.GetComponentLookup<InArmyGroup>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var garrisonConfig = SystemAPI.GetSingleton<GarrisonSystemConfig>();
            var cursorData = SystemAPI.GetSingleton<SubGameplayCursorData>();
            var inputMouseData = SystemAPI.GetSingleton<InputMouseData>();
            var inputUnitControlData = SystemAPI.GetSingleton<InputUnitControlData>();
            
            var config = SystemAPI.GetSingleton<PlayerCommandConfig>();
            if (config.playerAlwaysFocus) inputUnitControlData.Focus = true;
            
            var unitSelectionData = SystemAPI.GetSingleton<UnitSelectionData>();
            if (unitSelectionData.CurrentSelectCount == 0) return;
            if (!inputUnitControlData.Command) return;

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecbP = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            float3 targetPos;
            VFXName name;
            switch (cursorData.RightCursorType)
            {
                case SubGameplayCursorType.Attack:
                {
                    targetPos = SystemAPI.GetComponent<LocalTransform>(inputMouseData.HitEntity).Position;
                    name = VFXName.ControlToAttack;
                    new MovementAttackJob
                    {
                        ECB = ecbP,
                        TargetPos = targetPos,
                        TargetColliderShape =
                            SystemAPI.GetComponent<BoxColliderSize>(inputMouseData.HitEntity).Value,
                        TargetEntity = inputMouseData.HitEntity,
                        Focus = inputUnitControlData.Focus,
                    }.ScheduleParallel();
                    new MovementHealerMarchJob
                    {
                        ECB = ecbP,
                        TargetPos = targetPos,
                        Focus = inputUnitControlData.Focus,
                    }.ScheduleParallel();
                    break;
                }

                case SubGameplayCursorType.Garrison:
                {
                    if (!SystemAPI.HasComponent<GarrisonAttr>(inputMouseData.HitEntity))
                    {
                        var hint = state.EntityManager.CreateEntity();
                        state.EntityManager.AddComponent<HintRequest>(hint);
                        state.EntityManager.AddComponent<SubGameplayEntityTag>(hint);
                        state.EntityManager.SetComponentData(hint, new HintRequest
                        {
                            Name = HintName.TargetNotGarrisonable,
                        });
                        return;
                    }
                    targetPos = SystemAPI.GetComponent<LocalTransform>(inputMouseData.HitEntity).Position;
                    name = VFXName.ControlToGarrison;
                    _inArmyGroupLookup.Update(ref state);
                    new MovementGarrisonJob
                    {
                        ECB = ecbP,
                        TargetPos = targetPos,
                        TargetColliderShape =
                            SystemAPI.GetComponent<BoxColliderSize>(inputMouseData.HitEntity).Value,
                        TargetEntity = inputMouseData.HitEntity,
                        Focus = inputUnitControlData.Focus,
                        InteractiveRangeSq = garrisonConfig.GarrisonRadiusSq,
                        InArmyGroupLookup = _inArmyGroupLookup,
                    }.ScheduleParallel();
                    break;
                }

                case SubGameplayCursorType.Harvest:
                {
                    targetPos = SystemAPI.GetComponent<LocalTransform>(inputMouseData.HitEntity).Position;
                    name = VFXName.ControlToHarvest;
                    new MovementHarvestJob
                    {
                        ECB = ecbP,
                        TargetPos = targetPos,
                        TargetColliderShape =
                            SystemAPI.GetComponent<BoxColliderSize>(inputMouseData.HitEntity).Value,
                        TargetEntity = inputMouseData.HitEntity,
                        Focus = inputUnitControlData.Focus,
                    }.ScheduleParallel();
                    break;
                }

                case SubGameplayCursorType.Heal:
                {
                    targetPos = SystemAPI.GetComponent<LocalTransform>(inputMouseData.HitEntity).Position;
                    name = VFXName.ControlToHeal;
                    new MovementHealJob
                    {
                        ECB = ecbP,
                        TargetPos = SystemAPI.GetComponent<LocalTransform>(inputMouseData.HitEntity).Position,
                        TargetColliderShape =
                            SystemAPI.GetComponent<BoxColliderSize>(inputMouseData.HitEntity).Value,
                        TargetEntity = inputMouseData.HitEntity,
                        Focus = inputUnitControlData.Focus,
                    }.ScheduleParallel();
                    break;
                }

                case SubGameplayCursorType.March:
                {
                    targetPos = inputMouseData.HitPosition;
                    name = VFXName.ControlToMarch;
                    new MovementMarchJob
                    {
                        ECB = ecbP,
                        TargetPos = inputMouseData.HitPosition,
                        Focus = inputUnitControlData.Focus,
                    }.ScheduleParallel();
                    break;
                }
                default:
                    return;
            }
            GenerateControlVFX(ref state, targetPos, name, unitSelectionData);
        }

        private void GenerateControlVFX(ref SystemState state, float3 spawnPos, VFXName name,
            in UnitSelectionData unitSelectionData)
        {
            var vfx = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponent<VFXRequest>(vfx);
            state.EntityManager.AddComponent<SubGameplayEntityTag>(vfx);
            state.EntityManager.SetComponentData(vfx, new VFXRequest
            {
                RequestType = VFXRequestType.Spawn,
                Filter = new VFXSubFilter
                {
                    Faction = unitSelectionData.CurrentSelectFaction,
                    FactionFilterEnable = true,
                    
                },
                KeepDuration = 0,
                SpawnPosition = spawnPos,
                VFXName = name,
                StatChangeRequest = default,
                ParabolaTargetPosition = default,
                VFXTrackTarget = Entity.Null,
            });
        }
    }


    #region MovementJob

    [BurstCompile]
    [WithAll(typeof(Selected))]
    [WithNone(typeof(UnitDeadTag))]
    public partial struct MovementAttackJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public float3 TargetColliderShape;
        [ReadOnly] public float3 TargetPos;
        [ReadOnly] public Entity TargetEntity;
        [ReadOnly] public bool Focus;

        private void Execute([ChunkIndexInQuery] int index, ref MovableData movableData,
            ref BasicStateData basicStateData, ref DynamicBuffer<InsightTarget> targets,
            in AttackAbility attackAbility,
            Entity entity)
        {
            MovementUtils.SetMoveTarget(ref movableData, TargetPos, TargetColliderShape,
                MovementCommandType.Interactive, attackAbility.Range);
            basicStateData.TargetState = InteractState.Moving;
            StateUtils.SwitchState(ref basicStateData, ECB, entity, index);
            basicStateData.Focus = Focus;
            basicStateData.TargetState = InteractState.Attacking;
            basicStateData.TargetEntity = TargetEntity;
            InteractUtils.NoDupAdd(ref targets, new InsightTarget
            {
                Entity = basicStateData.TargetEntity
            });
        }
    }

    [BurstCompile]
    [WithAll(typeof(Selected))]
    [WithNone(typeof(UnitDeadTag))]
    public partial struct MovementHealJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public float3 TargetColliderShape;
        [ReadOnly] public float3 TargetPos;
        [ReadOnly] public Entity TargetEntity;
        [ReadOnly] public bool Focus;
        private void Execute([ChunkIndexInQuery] int index, ref MovableData movableData,
            ref BasicStateData basicStateData, ref DynamicBuffer<InsightTarget> targets,
            in HealAbility healingAbility,
            Entity entity)
        {

            MovementUtils.SetMoveTarget(ref movableData, TargetPos, TargetColliderShape,
                MovementCommandType.Interactive, healingAbility.Range);
            basicStateData.TargetState = InteractState.Moving;
            StateUtils.SwitchState(ref basicStateData, ECB, entity, index);
            basicStateData.TargetEntity = TargetEntity;
            basicStateData.Focus = Focus;
            basicStateData.TargetState = InteractState.Healing;
            InteractUtils.NoDupAdd(ref targets, new InsightTarget
            {
                Entity = basicStateData.TargetEntity
            });
        }
    }

    [BurstCompile]
    [WithAll(typeof(Selected))]
    [WithNone(typeof(UnitDeadTag))]

    public partial struct MovementHarvestJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public float3 TargetColliderShape;
        [ReadOnly] public float3 TargetPos;
        [ReadOnly] public Entity TargetEntity;
        [ReadOnly] public bool Focus;
        private void Execute([ChunkIndexInQuery] int index, ref MovableData movableData,
            ref BasicStateData basicStateData, ref DynamicBuffer<InsightTarget> targets,
            in HarvestAbility harvestAbility,
            Entity entity)
        {

            MovementUtils.SetMoveTarget(ref movableData, TargetPos, TargetColliderShape,
                MovementCommandType.Interactive, harvestAbility.Range);
            basicStateData.TargetState = InteractState.Moving;
            StateUtils.SwitchState(ref basicStateData, ECB, entity, index);
            basicStateData.TargetEntity = TargetEntity;
            basicStateData.Focus = Focus;
            basicStateData.TargetState = InteractState.Harvesting;
            InteractUtils.NoDupAdd(ref targets, new InsightTarget
            {
                Entity = basicStateData.TargetEntity
            });
        }
    }

    [BurstCompile]
    [WithAll(typeof(Selected))]
    [WithNone(typeof(UnitDeadTag))]

    public partial struct MovementMarchJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public float3 TargetPos;
        [ReadOnly] public bool Focus;
        private void Execute([ChunkIndexInQuery] int index, ref MovableData movableData,
            ref BasicStateData basicStateData, ref DynamicBuffer<InsightTarget> targets,
            Entity entity)
        {
            
            MovementUtils.SetMoveTarget(ref movableData, TargetPos, float3.zero,
                MovementCommandType.March, 0f);
            basicStateData.TargetState = InteractState.Moving;
            StateUtils.SwitchState(ref basicStateData, ECB, entity, index);
            // Remove target so that player command it to move than it will move
            if (basicStateData.TargetEntity != Entity.Null)
            {
                InteractUtils.Remove(ref targets, basicStateData.TargetEntity);
            }

            basicStateData.TargetEntity = Entity.Null;
            basicStateData.TargetState = InteractState.Idle;
            basicStateData.Focus = Focus;
        }
    }


    [BurstCompile]
    [WithAll(typeof(Selected))]
    [WithNone(typeof(UnitDeadTag))]

    public partial struct MovementGarrisonJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public ComponentLookup<InArmyGroup> InArmyGroupLookup;
        [ReadOnly] public float3 TargetColliderShape;
        [ReadOnly] public float3 TargetPos;
        [ReadOnly] public float InteractiveRangeSq;
        [ReadOnly] public Entity TargetEntity;
        [ReadOnly] public bool Focus;
        private void Execute([ChunkIndexInQuery] int index, ref MovableData movableData,
            ref BasicStateData basicStateData,
            Entity selfEntity)
        {
            if (InArmyGroupLookup.HasComponent(selfEntity))
            {
                var hintRequest = ECB.CreateEntity(index);
                ECB.AddComponent<SubGameplayEntityTag>(index, hintRequest);
                ECB.AddComponent(index, hintRequest, new HintRequest
                {
                    Name = HintName.UnitInArmyGroupCannotGarrisonInBuilding,
                });
                return;
            }
            MovementUtils.SetMoveTarget(ref movableData, TargetPos, TargetColliderShape,
                MovementCommandType.Interactive, InteractiveRangeSq);
            basicStateData.TargetState = InteractState.Moving;
            StateUtils.SwitchState(ref basicStateData, ECB, selfEntity, index);
            basicStateData.TargetEntity = TargetEntity;
            basicStateData.Focus = Focus;
            basicStateData.TargetState = InteractState.Garrison;
        }
    }

    // When player commands unattackable unit to attack, call this job for them to make them follow the troop
    [BurstCompile]
    [WithAll(typeof(Selected))]
    [WithNone(typeof(AttackAbility))]
    [WithNone(typeof(UnitDeadTag))]

    public partial struct MovementHealerMarchJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public float3 TargetPos;
        [ReadOnly] public bool Focus;
        private void Execute([ChunkIndexInQuery] int index, ref MovableData movableData,
            ref BasicStateData basicStateData, ref DynamicBuffer<InsightTarget> targets,
            Entity entity)
        {

            MovementUtils.SetMoveTarget(ref movableData, TargetPos, float3.zero,
                MovementCommandType.March, 0f);
            basicStateData.TargetState = InteractState.Moving;
            StateUtils.SwitchState(ref basicStateData, ECB, entity, index);
            // Remove target so that player command it to move than it will move
            if (basicStateData.TargetEntity != Entity.Null)
            {
                InteractUtils.Remove(ref targets, basicStateData.TargetEntity);
            }

            basicStateData.TargetEntity = Entity.Null;
            basicStateData.TargetState = InteractState.Idle;
            basicStateData.Focus = Focus;
        }
    }

    #endregion
}