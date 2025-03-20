using Unity.Burst;
using Unity.Entities;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Mouse;
using SparFlame.GamePlaySystem.Movement;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.GamePlaySystem.UnitSelection;
using SparFlame.GamePlaySystem.Units;
using SparFlame.GamePlaySystem.State;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Command
{
    [UpdateBefore(typeof(MovementSystem))]
    public partial struct CommandSystem : ISystem
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

            var targetCollider = SystemAPI.GetComponent<InteractBasicData>(mouseData.HitEntity).BoxColliderSize;
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
            ref UnitBasicStateData unitBasicStateData, in InteractableAttr interactableAttr,
            in UnitBasicAttr unitBasicAttr, in AttackAbility attackAbility,
            Entity entity)
        {
            MovementUtils.SetMoveTarget(ref movableData, TargetPos, TargetColliderShape,
                MovementCommandType.Interactive, attackAbility.RangeSq);

            unitBasicStateData.TargetState = UnitState.Moving;
            StateUtils.SwitchState(ref unitBasicStateData, ECB, entity, index);
            unitBasicStateData.Focus = Focus;
            unitBasicStateData.TargetState = UnitState.Attacking;
            unitBasicStateData.TargetEntity = TargetEntity;
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
            ref UnitBasicStateData unitBasicStateData, in InteractableAttr interactableAttr,
            in UnitBasicAttr unitBasicAttr,
            Entity entity)
        {

            MovementUtils.SetMoveTarget(ref movableData, TargetPos, float3.zero,
                MovementCommandType.March, 0f);
            unitBasicStateData.TargetState = UnitState.Moving;
            StateUtils.SwitchState(ref unitBasicStateData, ECB, entity, index);
            unitBasicStateData.TargetEntity = Entity.Null;
            unitBasicStateData.TargetState = UnitState.Idle;
            unitBasicStateData.Focus = Focus;
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
            ref UnitBasicStateData unitBasicStateData,
            in HealingAbility healingAbility,
            Entity entity)
        {
            MovementUtils.SetMoveTarget(ref movableData, TargetPos, TargetColliderShape,
                MovementCommandType.Interactive, healingAbility.RangeSq);
            unitBasicStateData.TargetState = UnitState.Moving;
            StateUtils.SwitchState(ref unitBasicStateData, ECB, entity, index);
            unitBasicStateData.TargetEntity = TargetEntity;
            unitBasicStateData.Focus = Focus;
            unitBasicStateData.TargetState = UnitState.Healing;
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
            ref UnitBasicStateData unitBasicStateData,
            in InteractableAttr interactableAttr, in UnitBasicAttr unitBasicAttr,
            in HarvestAbility harvestAbility,
            Entity entity)
        {
            MovementUtils.SetMoveTarget(ref movableData, TargetPos, TargetColliderShape,
                MovementCommandType.Interactive, harvestAbility.RangeSq);
            unitBasicStateData.TargetState = UnitState.Moving;
            StateUtils.SwitchState(ref unitBasicStateData, ECB, entity, index);
            unitBasicStateData.TargetEntity = TargetEntity;
            unitBasicStateData.Focus = Focus;
            unitBasicStateData.TargetState = UnitState.Harvesting;
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
            ref UnitBasicStateData unitBasicStateData,
            in InteractableAttr interactableAttr, in UnitBasicAttr unitBasicAttr,
            Entity entity)
        {
            MovementUtils.SetMoveTarget(ref movableData, TargetPos, TargetColliderShape,
                MovementCommandType.Interactive, InteractiveRangeSq);
            unitBasicStateData.TargetState = UnitState.Moving;
            StateUtils.SwitchState(ref unitBasicStateData, ECB, entity, index);
            unitBasicStateData.TargetEntity = TargetEntity;
            unitBasicStateData.Focus = Focus;
            unitBasicStateData.TargetState = UnitState.Garrison;
        }
    }

    #endregion
}