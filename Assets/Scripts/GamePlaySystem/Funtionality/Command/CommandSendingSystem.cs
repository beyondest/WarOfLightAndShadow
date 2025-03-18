using Unity.Burst;
using Unity.Entities;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.General;
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

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var buildingConfig = SystemAPI.GetSingleton<BuildingConfig>();
            var cursorData = SystemAPI.GetSingleton<CursorData>();
            var mouseData = SystemAPI.GetSingleton<MouseSystemData>();
            var unitSelectionData = SystemAPI.GetSingleton<UnitSelectionData>();
            if(unitSelectionData.CurrentSelectCount == 0) return;
            if (mouseData is not { ClickFlag: ClickFlag.Start, ClickType: ClickType.Right }) return;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            
            
            switch (cursorData.RightCursorType)
            {
                case CursorType.Attack:
                {
                    float2 targetColliderXz;
                    var interactableAttr = SystemAPI.GetComponent<InteractableAttr>(mouseData.HitEntity);
                    var transform = SystemAPI.GetComponent<LocalTransform>(mouseData.HitEntity);
                    if (interactableAttr.BaseTag == BaseTag.Buildings)
                    {
                        var buildingAttr = SystemAPI.GetComponent<BuildingAttr>(mouseData.HitEntity);
                        targetColliderXz = new float2(buildingAttr.BoxColliderSize.x, buildingAttr.BoxColliderSize.z);
                 
                    }
                    else if (interactableAttr.BaseTag == BaseTag.Units)
                    {
                        var unitAttr = SystemAPI.GetComponent<UnitAttr>(mouseData.HitEntity);
                        targetColliderXz = new float2(unitAttr.BoxColliderSize.x, unitAttr.BoxColliderSize.z);
                    }
                    else
                    {
                        // This line should never reach
                        return;
                    }
                    new MovementAttackJob
                    {
                        ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                        TargetCenterPos = transform.Position,
                        TargetColliderShapeXz = targetColliderXz,
                        TargetEntity = mouseData.HitEntity,
                        Focus = mouseData.Focus
                    }.ScheduleParallel();

                    break;
                }
                
                case CursorType.Garrison:
                {
                    var buildingAttr = SystemAPI.GetComponent<BuildingAttr>(mouseData.HitEntity);
                    var transform = SystemAPI.GetComponent<LocalTransform>(mouseData.HitEntity);
                    new MovementGarrisonJob
                    {
                        ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                        BuildingInteractiveRangeSq = buildingConfig.BuildingGarrisonRadiusSq,
                        TargetCenterPos = transform.Position,
                        TargetColliderShapeXz = new float2(buildingAttr.BoxColliderSize.x,
                            buildingAttr.BoxColliderSize.z),
                        TargetEntity = mouseData.HitEntity,
                        Focus = mouseData.Focus
                    }.ScheduleParallel();
                    break;
                }
                
                case CursorType.Harvest:
                {
                    var resourceAttr = SystemAPI.GetComponent<ResourceAttr>(mouseData.HitEntity);
                    var transform = SystemAPI.GetComponent<LocalTransform>(mouseData.HitEntity);
                    new MovementHarvestJob
                    {
                        ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                        TargetCenterPos = transform.Position,
                        TargetColliderShapeXz = new float2(resourceAttr.BoxColliderSize.x, resourceAttr.BoxColliderSize.z),
                        TargetEntity = mouseData.HitEntity,
                        Focus = mouseData.Focus
                    }.ScheduleParallel();
                    break;
                }
                
                case CursorType.Heal:
                {
                    var unitAttr = SystemAPI.GetComponent<UnitAttr>(mouseData.HitEntity);
                    var transform = SystemAPI.GetComponent<LocalTransform>(mouseData.HitEntity);
                    new MovementHealJob
                    {
                        ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                        TargetCenterPos = transform.Position,
                        TargetColliderShapeXz = new float2(unitAttr.BoxColliderSize.x, unitAttr.BoxColliderSize.z),
                        TargetEntity = mouseData.HitEntity,
                        Focus = mouseData.Focus
                    }.ScheduleParallel();
                    break;
                }

                case CursorType.March:
                {
                    // TODO No idle tag, debug output but not move
                    Debug.Log("March");
                    new MovementMarchJob
                    {
                        ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                        TargetCenterPos = mouseData.HitPosition,
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
        [ReadOnly] public float2 TargetColliderShapeXz;
        [ReadOnly] public float3 TargetCenterPos;
        [ReadOnly] public Entity TargetEntity;
        [ReadOnly] public bool Focus;

        private void Execute([ChunkIndexInQuery]int index, ref MovableData movableData,ref UnitBasicStateData unitBasicStateData, in InteractableAttr interactableAttr, 
            in UnitAttr unitAttr,
            Entity entity)
        {
            movableData.MovementCommandType = MovementCommandType.Interactive;
            movableData.TargetColliderShapeXZ = TargetColliderShapeXz;
            movableData.InteractiveRangeSq = unitAttr.AttackRangeSq;
            movableData.TargetCenterPos = TargetCenterPos;
            movableData.MoveSpeed = unitAttr.MoveSpeed;
            movableData.ForceCalculate = true;
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
        [ReadOnly] public float3 TargetCenterPos;
        [ReadOnly] public bool Focus;

        private void Execute([ChunkIndexInQuery] int index,ref MovableData movableData,ref UnitBasicStateData unitBasicStateData, in InteractableAttr interactableAttr, in UnitAttr unitAttr,
            Entity entity)
        {
            movableData.MovementCommandType = MovementCommandType.March;
            movableData.TargetCenterPos = TargetCenterPos;
            movableData.MoveSpeed = unitAttr.MoveSpeed;
            movableData.ForceCalculate = true;
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
        [ReadOnly] public float2 TargetColliderShapeXz;
        [ReadOnly] public float3 TargetCenterPos;
        [ReadOnly] public Entity TargetEntity;
        [ReadOnly] public bool Focus;

        private void Execute([ChunkIndexInQuery] int index, ref MovableData movableData,ref UnitBasicStateData unitBasicStateData,
            in InteractableAttr interactableAttr, in UnitAttr unitAttr,
            in HealingAbility healingAbility,
            Entity entity)
        {
            movableData.MovementCommandType = MovementCommandType.Interactive;
            movableData.TargetColliderShapeXZ = TargetColliderShapeXz;
            movableData.InteractiveRangeSq = healingAbility.HealingRangeSq;
            movableData.TargetCenterPos = TargetCenterPos;
            movableData.MoveSpeed = unitAttr.MoveSpeed;
            movableData.ForceCalculate = false;
            
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
        [ReadOnly] public float2 TargetColliderShapeXz;
        [ReadOnly] public float3 TargetCenterPos;
        [ReadOnly] public Entity TargetEntity;
        [ReadOnly] public bool Focus;

        private void Execute([ChunkIndexInQuery] int index, ref MovableData movableData,ref UnitBasicStateData unitBasicStateData,
            in InteractableAttr interactableAttr, in UnitAttr unitAttr,
            in HarvestAbility harvestAbility,
            Entity entity)
        {
            movableData.MovementCommandType = MovementCommandType.Interactive;
            movableData.TargetColliderShapeXZ = TargetColliderShapeXz;
            movableData.InteractiveRangeSq = harvestAbility.HarvestingRangeSq;
            movableData.TargetCenterPos = TargetCenterPos;
            movableData.MoveSpeed = unitAttr.MoveSpeed;
            movableData.ForceCalculate = false;
            
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
        [ReadOnly] public float2 TargetColliderShapeXz;
        [ReadOnly] public float3 TargetCenterPos;
        [ReadOnly] public float BuildingInteractiveRangeSq;
        [ReadOnly] public Entity TargetEntity;
        [ReadOnly] public bool Focus;

        private void Execute([ChunkIndexInQuery] int index, ref MovableData movableData,ref UnitBasicStateData unitBasicStateData,
            in InteractableAttr interactableAttr, in UnitAttr unitAttr,
            Entity entity)
        {
            movableData.MovementCommandType = MovementCommandType.Interactive;
            movableData.TargetColliderShapeXZ = TargetColliderShapeXz;
            movableData.InteractiveRangeSq = BuildingInteractiveRangeSq;
            movableData.TargetCenterPos = TargetCenterPos;
            movableData.MoveSpeed = unitAttr.MoveSpeed;
            movableData.ForceCalculate = false;
            
            unitBasicStateData.TargetState = UnitState.Moving;
            StateUtils.SwitchState(ref unitBasicStateData, ECB, entity, index);
            unitBasicStateData.TargetEntity = TargetEntity;
            unitBasicStateData.Focus = Focus;
            unitBasicStateData.TargetState = UnitState.Garrison;
        }
    }
    
    #endregion
    
}