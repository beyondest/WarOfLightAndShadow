using Unity.Burst;
using Unity.Entities;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Mouse;
using SparFlame.GamePlaySystem.Movement;
using SparFlame.GamePlaySystem.UnitSelection;
using SparFlame.GamePlaySystem.State;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.Command
{
    // [UpdateBefore(typeof(MovementSystem))]
    [UpdateAfter(typeof(CursorManageSystem))]
    public partial struct PlayerCommandSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<InputUnitControlData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<NotPauseTag>();
            state.RequireForUpdate<CommandConfig>();
            state.RequireForUpdate<CursorData>();
            state.RequireForUpdate<InputMouseData>();
            state.RequireForUpdate<UnitSelectionData>();
            state.RequireForUpdate<BuildingConfig>();
        }

        // TODO : Rewrite this with generic type ijobchunk or generic type ijobparallelfor
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var buildingConfig = SystemAPI.GetSingleton<BuildingConfig>();
            var cursorData = SystemAPI.GetSingleton<CursorData>();
            var inputMouseData = SystemAPI.GetSingleton<InputMouseData>();
            var inputUnitControlData = SystemAPI.GetSingleton<InputUnitControlData>();
            
            var unitSelectionData = SystemAPI.GetSingleton<UnitSelectionData>();
            if (unitSelectionData.CurrentSelectCount == 0) return;
            if (inputMouseData is not { ClickFlag: ClickFlag.Start, ClickType: ClickType.Right, IsOverUI: false}) return;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            
            switch (cursorData.RightCursorType)
            {
                case CursorType.Attack:
                {
                    new MovementAttackJob
                    {
                        ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                        TargetPos =  SystemAPI.GetComponent<LocalTransform>(inputMouseData.HitEntity).Position,
                        TargetColliderShape = SystemAPI.GetComponent<InteractableAttr>(inputMouseData.HitEntity).BoxColliderSize,
                        TargetEntity = inputMouseData.HitEntity,
                        Focus = inputUnitControlData.Focus
                    }.ScheduleParallel();

                    break;
                }

                case CursorType.Garrison:
                {
                    new MovementGarrisonJob()
                    {
                        ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                        TargetPos =  SystemAPI.GetComponent<LocalTransform>(inputMouseData.HitEntity).Position,
                        TargetColliderShape = SystemAPI.GetComponent<InteractableAttr>(inputMouseData.HitEntity).BoxColliderSize,
                        TargetEntity = inputMouseData.HitEntity,
                        Focus = inputUnitControlData.Focus,
                        InteractiveRangeSq = buildingConfig.BuildingGarrisonRadiusSq
                    }.ScheduleParallel();
                    break;
                }

                case CursorType.Harvest:
                {
                    new MovementHarvestJob
                    {
                        ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                        TargetPos = SystemAPI.GetComponent<LocalTransform>(inputMouseData.HitEntity).Position,
                        TargetColliderShape = SystemAPI.GetComponent<InteractableAttr>(inputMouseData.HitEntity).BoxColliderSize,
                        TargetEntity = inputMouseData.HitEntity,
                        Focus = inputUnitControlData.Focus
                    }.ScheduleParallel();
                    break;
                }

                case CursorType.Heal:
                {
                    new MovementHealJob
                    {
                        ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                        TargetPos =  SystemAPI.GetComponent<LocalTransform>(inputMouseData.HitEntity).Position,
                        TargetColliderShape = SystemAPI.GetComponent<InteractableAttr>(inputMouseData.HitEntity).BoxColliderSize,
                        TargetEntity = inputMouseData.HitEntity,
                        Focus = inputUnitControlData.Focus
                    }.ScheduleParallel();
                    break;
                }

                case CursorType.March:
                {
                    new MovementMarchJob
                    {
                        ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                        TargetPos = inputMouseData.HitPosition,
                        Focus = inputUnitControlData.Focus
                    }.ScheduleParallel();
                    break;
                }

                default:
                    return;
            }
        }
    }


    #region MovementJob

    [BurstCompile]
    [WithAll(typeof(Selected))]
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
            basicStateData.TargetState = UnitState.Moving;
            StateUtils.SwitchState(ref basicStateData, ECB, entity, index);
            basicStateData.Focus = Focus;
            basicStateData.TargetState = UnitState.Attacking;
            basicStateData.TargetEntity = TargetEntity;
            InteractUtils.NoDupAdd(ref targets, new InsightTarget
            {
                Entity = basicStateData.TargetEntity
            });
        }
    }

    [BurstCompile]
    [WithAll(typeof(Selected))]
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
            basicStateData.TargetState = UnitState.Moving;
            StateUtils.SwitchState(ref basicStateData, ECB, entity, index);
            basicStateData.TargetEntity = TargetEntity;
            basicStateData.Focus = Focus;
            basicStateData.TargetState = UnitState.Healing;
            InteractUtils.NoDupAdd(ref targets, new InsightTarget
            {
                Entity = basicStateData.TargetEntity
            });
        }
    }

    [BurstCompile]
    [WithAll(typeof(Selected))]
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
            basicStateData.TargetState = UnitState.Moving;
            StateUtils.SwitchState(ref basicStateData, ECB, entity, index);
            basicStateData.TargetEntity = TargetEntity;
            basicStateData.Focus = Focus;
            basicStateData.TargetState = UnitState.Harvesting;
            InteractUtils.NoDupAdd(ref targets, new InsightTarget
            {
                Entity = basicStateData.TargetEntity
            });
        }
    }

    [BurstCompile]
    [WithAll(typeof(Selected))]
    public partial struct MovementMarchJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public float3 TargetPos;
        [ReadOnly] public bool Focus;

        private void Execute([ChunkIndexInQuery] int index, ref MovableData movableData,
            ref BasicStateData basicStateData,ref DynamicBuffer<InsightTarget> targets,
            Entity entity)
        {
            MovementUtils.SetMoveTarget(ref movableData, TargetPos, float3.zero,
                MovementCommandType.March, 0f);
            basicStateData.TargetState = UnitState.Moving;
            StateUtils.SwitchState(ref basicStateData, ECB, entity, index);
            // Remove target so that player command it to move than it will move
            if (basicStateData.TargetEntity != Entity.Null)
            {
                InteractUtils.Remove(ref targets, basicStateData.TargetEntity);
            }
            basicStateData.TargetEntity = Entity.Null;
            basicStateData.TargetState = UnitState.Idle;
            basicStateData.Focus = Focus;
        }
    }
    
    
    [BurstCompile]
    [WithAll(typeof(Selected))]
    public partial struct MovementGarrisonJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public float3 TargetColliderShape;
        [ReadOnly] public float3 TargetPos;
        [ReadOnly] public float InteractiveRangeSq;
        [ReadOnly] public Entity TargetEntity;
        [ReadOnly] public bool Focus;

        private void Execute([ChunkIndexInQuery] int index, ref MovableData movableData,
            ref BasicStateData basicStateData,
            Entity entity)
        {
            MovementUtils.SetMoveTarget(ref movableData, TargetPos, TargetColliderShape,
                MovementCommandType.Interactive, InteractiveRangeSq);
            basicStateData.TargetState = UnitState.Moving;
            StateUtils.SwitchState(ref basicStateData, ECB, entity, index);
            basicStateData.TargetEntity = TargetEntity;
            basicStateData.Focus = Focus;
            basicStateData.TargetState = UnitState.Garrison;
        }
    }

    #endregion
}