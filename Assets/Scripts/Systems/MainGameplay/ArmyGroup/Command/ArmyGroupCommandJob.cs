using System;
using SparFlame.Components.General;
using SparFlame.Components.Input;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.Systems.MainGameplay.ArmyGroup
{
    [BurstCompile]
    [WithAll(typeof(ArmyGroupSelected))]
    public partial struct ArmyGroupSetTargetJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public float3 TargetPosition;
        [ReadOnly] public ComponentLookup<ArmyGroupMovingTag> ArmyGroupMovingTagLookup;
        [ReadOnly] public ArmyGroupState TargetState;
        [ReadOnly] public Entity TargetEntity;
        [ReadOnly] public float2 TargetBoxColliderSizeXz;

        private void Execute([ChunkIndexInQuery] int index, ref ArmyGroupMovableData movableData,
            ref DynamicBuffer<ArmyGroupMovingTarget> targets,
            ref DynamicBuffer<ArmyGroupFinalWayPoint> finalWayPoints,
            ref ArmyGroupCalculatePathData pathData, ref PathVisualizeData visualizeData,
            ref NavAgentComponent navAgent, ref ArmyGroupStateData stateData,
            Entity selfEntity
        )
        {
            // If army group is already moving, make it stop and clear its waypoints and targets
            if (ArmyGroupMovingTagLookup.IsComponentEnabled(selfEntity))
            {
                ArmyGroupUtils.ResetArmyGroupMovableData(ref movableData, ref pathData, ref finalWayPoints,
                    ref visualizeData,
                    ref navAgent,
                    ECB, index, selfEntity);
                movableData.movementInfo = ArmyGroupMovementInfo.None;
                stateData.TargetState = ArmyGroupState.Idle;
                stateData.CurState = ArmyGroupState.Idle;
                stateData.Target = Entity.Null;
            }

            if (stateData.TargetState != ArmyGroupState.Idle)
            {
                var hintRequest = ECB.CreateEntity(index);
                ECB.AddComponent(index, hintRequest, new HintRequest
                {
                    Name = HintName.PleaseDeleteArmyGroupLastTargetForNewTarget,
                });
                ECB.AddComponent<MainGameplayEntityTag>(index, hintRequest);
                return;
            }
            
            stateData.TargetState = TargetState;
            stateData.Target = TargetEntity;


            targets.Add(new ArmyGroupMovingTarget
            {
                position = TargetPosition,
                boxColliderSizeXz = TargetBoxColliderSizeXz
            });
        }
    }

    [BurstCompile]
    [WithAll(typeof(ArmyGroupSelected))]
    public partial struct ArmyGroupStartMovingJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;

        private void Execute([ChunkIndexInQuery] int index, Entity selfEntity,
            ref ArmyGroupMovableData movableData, in ArmyGroupStateData stateData)
        {
            if (!movableData.isTargetReachable)
            {
                var hintRequest = ECB.CreateEntity(index);
                ECB.AddComponent<MainGameplayEntityTag>(index, hintRequest);
                ECB.AddComponent(index, hintRequest, new HintRequest
                {
                    Name = HintName.ArmyGroupNotReachable
                });
                return;
            }

            if (stateData.TargetState == ArmyGroupState.Invade)
            {
                ECB.AppendToBuffer(index, stateData.Target, new CityFutureInvaders
                {
                    ArmyGroup = selfEntity
                });
            }

            movableData.movementInfo = ArmyGroupMovementInfo.NotComplete;
            movableData.curWaypoint = 0;
            ECB.SetComponentEnabled<ArmyGroupMovingTag>(index, selfEntity, true);
        }
    }

    [BurstCompile]
    [WithAll(typeof(ArmyGroupSelected))]
    [WithNone(typeof(ArmyGroupMovingTag))]
    public partial struct ArmyGroupDeleteLastTargetJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;

        private void Execute([ChunkIndexInQuery] int index, ref DynamicBuffer<ArmyGroupMovingTarget> targets,
            ref DynamicBuffer<ArmyGroupFinalWayPoint> finalWayPoints,
            ref DynamicBuffer<WaypointBuffer> waypointBuffer, ref ArmyGroupMovableData movableData,
            ref ArmyGroupCalculatePathData pathData, ref ArmyGroupStateData stateData,
            ref PathVisualizeData visualizeData, ref NavAgentComponent navAgent, Entity selfEntity
        )
        {
            ArmyGroupUtils.ResetArmyGroupMovableData(ref movableData, ref pathData, ref finalWayPoints,
                ref visualizeData, ref navAgent, ECB, index, selfEntity);
            stateData.TargetState = ArmyGroupState.Idle;

            if (targets.Length <= 0) return;
            targets.RemoveAt(targets.Length - 1);
        }
    }

    [BurstCompile]
    [WithAll(typeof(ArmyGroupSelected))]
    public partial struct ArmyGroupEndMovingJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;

        private void Execute([ChunkIndexInQuery] int index, ref DynamicBuffer<ArmyGroupMovingTarget> targets,
            ref DynamicBuffer<ArmyGroupFinalWayPoint> finalWayPoints,
            ref DynamicBuffer<WaypointBuffer> waypointBuffer, ref ArmyGroupMovableData movableData,
            ref ArmyGroupCalculatePathData pathData, ref ArmyGroupStateData stateData,
            ref PathVisualizeData visualizeData, ref NavAgentComponent navAgent, Entity selfEntity
        )
        {
            ArmyGroupUtils.ResetArmyGroupMovableData(ref movableData, ref pathData, ref finalWayPoints,
                ref visualizeData, ref navAgent, ECB, index, selfEntity);
            if (stateData.TargetState == ArmyGroupState.Invade)
            {
                var request = ECB.CreateEntity(index);
                ECB.AddComponent<MainGameplayEntityTag>(index, request);
                ECB.AddComponent(index, request, new RemoveCityFutureInvaderRequest
                {
                    ArmyGroup = selfEntity,
                    City = stateData.Target
                });
            }
            movableData.movementInfo = ArmyGroupMovementInfo.None;
            stateData.CurState = ArmyGroupState.Idle;
            stateData.TargetState = ArmyGroupState.Idle;
            targets.Clear();
            ECB.SetComponentEnabled<ArmyGroupMovingTag>(index, selfEntity, false);
            
        }
    }

    [BurstCompile]
    [WithAll(typeof(ArmyGroupSelected))]
    [WithNone(typeof(ArmyGroupMovingTag))]
    public partial struct ArmyGroupClearAllMovingTargetsJob : IJobEntity
    {
        public EntityCommandBuffer ECB;

        private void Execute(ref DynamicBuffer<ArmyGroupMovingTarget> targets,
            ref DynamicBuffer<ArmyGroupFinalWayPoint> finalWayPoints,
            ref DynamicBuffer<WaypointBuffer> waypointBuffer, ref ArmyGroupMovableData movableData,
            ref ArmyGroupCalculatePathData pathData, ref ArmyGroupStateData stateData,
            ref PathVisualizeData visualizeData, ref NavAgentComponent navAgent, Entity selfEntity
        )
        {
            ArmyGroupUtils.ResetArmyGroupMovableData(ref movableData, ref pathData, ref finalWayPoints,
                ref visualizeData, ref navAgent, ECB, selfEntity);
            stateData.TargetState = ArmyGroupState.Idle;
            targets.Clear();
        }
    }
}