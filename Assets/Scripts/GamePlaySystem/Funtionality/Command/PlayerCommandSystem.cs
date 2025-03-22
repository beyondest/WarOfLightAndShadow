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
    [UpdateBefore(typeof(MovementSystem))]
    [UpdateAfter(typeof(CursorManageSystem))]
    [UpdateBefore(typeof(VolumeObstacleSystem))]
    public partial struct PlayerCommandSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<NotPauseTag>();
            state.RequireForUpdate<CommandConfig>();
            state.RequireForUpdate<CursorData>();
            state.RequireForUpdate<MouseSystemData>();
            state.RequireForUpdate<UnitSelectionData>();
            state.RequireForUpdate<BuildingConfig>();
        }

        // TODO : Rewrite this with generic type ijobchunk
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var buildingConfig = SystemAPI.GetSingleton<BuildingConfig>();
            var cursorData = SystemAPI.GetSingleton<CursorData>();
            var mouseData = SystemAPI.GetSingleton<MouseSystemData>();
            var unitSelectionData = SystemAPI.GetSingleton<UnitSelectionData>();
            if (unitSelectionData.CurrentSelectCount == 0) return;
            if (mouseData is not { ClickFlag: ClickFlag.Start, ClickType: ClickType.Right }) return;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();

            var targetCollider = SystemAPI.GetComponent<InteractableAttr>(mouseData.HitEntity).BoxColliderSize;
            var transform = SystemAPI.GetComponent<LocalTransform>(mouseData.HitEntity);
            switch (cursorData.RightCursorType)
            {
                case CursorType.Attack:
                {
                    new MovementAttackJob
                    {
                        ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                        TargetPos = transform.Position,
                        TargetColliderShape = targetCollider,
                        TargetEntity = mouseData.HitEntity,
                        Focus = mouseData.Focus
                    }.ScheduleParallel();

                    break;
                }

                case CursorType.Garrison:
                {
                    new MovementGarrisonJob()
                    {
                        ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                        TargetPos = transform.Position,
                        TargetColliderShape = targetCollider,
                        TargetEntity = mouseData.HitEntity,
                        Focus = mouseData.Focus,
                        InteractiveRangeSq = buildingConfig.BuildingGarrisonRadiusSq
                    }.ScheduleParallel();
                    break;
                }

                case CursorType.Harvest:
                {
                    new MovementHarvestJob
                    {
                        ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                        TargetPos = transform.Position,
                        TargetColliderShape = targetCollider,
                        TargetEntity = mouseData.HitEntity,
                        Focus = mouseData.Focus
                    }.ScheduleParallel();
                    break;
                }

                case CursorType.Heal:
                {
                    new MovementHealJob
                    {
                        ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                        TargetPos = transform.Position,
                        TargetColliderShape = targetCollider,
                        TargetEntity = mouseData.HitEntity,
                        Focus = mouseData.Focus
                    }.ScheduleParallel();
                    break;
                }

                case CursorType.March:
                {
                    new MovementMarchJob
                    {
                        ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                        TargetPos = mouseData.HitPosition,
                        Focus = mouseData.Focus
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
            ref BasicStateData basicStateData, 
             in AttackAbility attackAbility,
            Entity entity)
        {
            MovementUtils.SetMoveTarget(ref movableData, TargetPos, TargetColliderShape,
                MovementCommandType.Interactive, attackAbility.RangeSq);

            basicStateData.TargetState = UnitState.Moving;
            StateUtils.SwitchState(ref basicStateData, ECB, entity, index);
            basicStateData.Focus = Focus;
            basicStateData.TargetState = UnitState.Attacking;
            basicStateData.TargetEntity = TargetEntity;
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
            ref BasicStateData basicStateData,
            in HealAbility healingAbility,
            Entity entity)
        {
            MovementUtils.SetMoveTarget(ref movableData, TargetPos, TargetColliderShape,
                MovementCommandType.Interactive, healingAbility.RangeSq);
            basicStateData.TargetState = UnitState.Moving;
            StateUtils.SwitchState(ref basicStateData, ECB, entity, index);
            basicStateData.TargetEntity = TargetEntity;
            basicStateData.Focus = Focus;
            basicStateData.TargetState = UnitState.Healing;
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
            ref BasicStateData basicStateData,
            in HarvestAbility harvestAbility,
            Entity entity)
        {
            MovementUtils.SetMoveTarget(ref movableData, TargetPos, TargetColliderShape,
                MovementCommandType.Interactive, harvestAbility.RangeSq);
            basicStateData.TargetState = UnitState.Moving;
            StateUtils.SwitchState(ref basicStateData, ECB, entity, index);
            basicStateData.TargetEntity = TargetEntity;
            basicStateData.Focus = Focus;
            basicStateData.TargetState = UnitState.Harvesting;
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
            ref BasicStateData basicStateData,
            Entity entity)
        {
            MovementUtils.SetMoveTarget(ref movableData, TargetPos, float3.zero,
                MovementCommandType.March, 0f);
            basicStateData.TargetState = UnitState.Moving;
            StateUtils.SwitchState(ref basicStateData, ECB, entity, index);
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