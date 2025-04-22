using Unity.Burst;
using Unity.Entities;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.CustomInput;
using SparFlame.GamePlaySystem.Garrison;
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
    [UpdateBefore(typeof(MovementSystem))]
    public partial struct PlayerCommandSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<NotPauseTag>();
            state.RequireForUpdate<CommandConfig>();
            state.RequireForUpdate<InputUnitControlData>();
            state.RequireForUpdate<CursorData>();
            state.RequireForUpdate<InputMouseData>();
            state.RequireForUpdate<UnitSelectionData>();
            state.RequireForUpdate<GarrisonSystemConfig>();
        }

        // TODO : Rewrite this with generic type ijobchunk or generic type ijobparallelfor
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var garrisonConfig = SystemAPI.GetSingleton<GarrisonSystemConfig>();
            var cursorData = SystemAPI.GetSingleton<CursorData>();
            var inputMouseData = SystemAPI.GetSingleton<InputMouseData>();
            var inputUnitControlData = SystemAPI.GetSingleton<InputUnitControlData>();
            
            var unitSelectionData = SystemAPI.GetSingleton<UnitSelectionData>();
            if (unitSelectionData.CurrentSelectCount == 0) return;
            // if (inputMouseData is not { ClickFlag: ClickFlag.Start, ClickType: ClickType.Right, IsOverUI: false}) return;
            if(!inputUnitControlData.Command)return;
            
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            switch (cursorData.RightCursorType)
            {
                case CursorType.Attack:
                {
                    new MovementAttackJob
                    {
                        ECB = ecb,
                        TargetPos =  SystemAPI.GetComponent<LocalTransform>(inputMouseData.HitEntity).Position,
                        TargetColliderShape = SystemAPI.GetComponent<GeneralAttr>(inputMouseData.HitEntity).BoxColliderSize,
                        TargetEntity = inputMouseData.HitEntity,
                        Focus = inputUnitControlData.Focus
                    }.ScheduleParallel();

                    break;
                }

                case CursorType.Garrison:
                {
                    new MovementGarrisonJob()
                    {
                        ECB = ecb,
                        TargetPos =  SystemAPI.GetComponent<LocalTransform>(inputMouseData.HitEntity).Position,
                        TargetColliderShape = SystemAPI.GetComponent<GeneralAttr>(inputMouseData.HitEntity).BoxColliderSize,
                        TargetEntity = inputMouseData.HitEntity,
                        Focus = inputUnitControlData.Focus,
                        InteractiveRangeSq = garrisonConfig.GarrisonRadiusSq
                    }.ScheduleParallel();
                    break;
                }

                case CursorType.Harvest:
                {
                    new MovementHarvestJob
                    {
                        ECB = ecb,
                        TargetPos = SystemAPI.GetComponent<LocalTransform>(inputMouseData.HitEntity).Position,
                        TargetColliderShape = SystemAPI.GetComponent<GeneralAttr>(inputMouseData.HitEntity).BoxColliderSize,
                        TargetEntity = inputMouseData.HitEntity,
                        Focus = inputUnitControlData.Focus
                    }.ScheduleParallel();
                    break;
                }

                case CursorType.Heal:
                {
                    new MovementHealJob
                    {
                        ECB = ecb,
                        TargetPos =  SystemAPI.GetComponent<LocalTransform>(inputMouseData.HitEntity).Position,
                        TargetColliderShape = SystemAPI.GetComponent<GeneralAttr>(inputMouseData.HitEntity).BoxColliderSize,
                        TargetEntity = inputMouseData.HitEntity,
                        Focus = inputUnitControlData.Focus
                    }.ScheduleParallel();
                    break;
                }

                case CursorType.March:
                {
                    new MovementMarchJob
                    {
                        ECB = ecb,
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
                MovementCommandType.Interactive, attackAbility.RangeSq);
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
                MovementCommandType.Interactive, healingAbility.RangeSq);
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
                MovementCommandType.Interactive, harvestAbility.RangeSq);
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
            basicStateData.TargetState = InteractState.Moving;
            StateUtils.SwitchState(ref basicStateData, ECB, entity, index);
            basicStateData.TargetEntity = TargetEntity;
            basicStateData.Focus = Focus;
            basicStateData.TargetState = InteractState.Garrison;
        }
    }

    #endregion
}