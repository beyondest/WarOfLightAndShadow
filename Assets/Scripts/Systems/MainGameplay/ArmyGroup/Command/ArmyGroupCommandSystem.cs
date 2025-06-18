using SparFlame.Components.General;
using SparFlame.Components.Input;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Systems.General.Audio;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
// ReSharper disable UseIndexFromEndExpression

namespace SparFlame.Systems.MainGameplay.ArmyGroup
{
    public partial struct ArmyGroupCommandSystem : ISystem
    {
        private ComponentLookup<ArmyGroupMovingTag> _armyGroupMovingTagLookup;
        private NativeList<Entity> _flags;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ArmyGroupSelectionData>();
            state.RequireForUpdate<ArmyGroupCommandSystemConfig>();
            state.RequireForUpdate<CameraData>();
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<InputMouseData>();
            state.RequireForUpdate<InputArmyGroupControlData>();
            _armyGroupMovingTagLookup = state.GetComponentLookup<ArmyGroupMovingTag>(true);
            _flags = new NativeList<Entity>(Allocator.Persistent);
        }

        public void OnDestroy(ref SystemState state)
        {
            if (_flags.IsCreated)
                _flags.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var inputArmyGroupData = SystemAPI.GetSingleton<InputArmyGroupControlData>();
            var inputMouseData = SystemAPI.GetSingleton<InputMouseData>();
            var config = SystemAPI.GetSingleton<ArmyGroupCommandSystemConfig>();
            var armyGroupSelectionData = SystemAPI.GetSingleton<ArmyGroupSelectionData>();
            if (armyGroupSelectionData.CurrentSelectCount <= 0)
            {
                if (!_flags.IsEmpty)
                {
                    foreach (var flag in _flags)
                    {
                        state.EntityManager.DestroyEntity(flag);
                    }

                    _flags.Clear();
                }

                return;
            }

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var ecbP = ecb.AsParallelWriter();
            if (ArmyGroupUtils.IsSettingTarget(inputArmyGroupData, inputMouseData, state.EntityManager))
            {
                var flag = state.EntityManager.Instantiate(config.FlagPrefab);
                var trans = SystemAPI.GetComponent<LocalTransform>(config.FlagPrefab);
                trans.Position = inputMouseData.HitPosition;
                state.EntityManager.AddComponent<MainGameplayEntityTag>(flag);
                state.EntityManager.SetComponentData(flag, trans);
                _flags.Add(flag);
                _armyGroupMovingTagLookup.Update(ref state);
                new ArmyGroupSetTargetJob
                {
                    TargetPosition = inputMouseData.HitPosition,
                    ArmyGroupMovingTagLookup = _armyGroupMovingTagLookup,
                    ECB = ecbP
                }.ScheduleParallel();
            }

            if (inputArmyGroupData.StartMoving)
            {
                new ArmyGroupStartMovingJob
                {
                    ECB = ecbP
                }.ScheduleParallel();
                var pos = SystemAPI.GetSingleton<CameraData>().CameraRigPosition;
                AudioUtils.PlayAudioClip(AudioName.ArmyGroupStartMoving, pos, state.EntityManager);
            }

            if (inputArmyGroupData.ClearAllTargets)
            {
                foreach (var flag in _flags)
                {
                    ecb.DestroyEntity(flag);
                }
                _flags.Clear();

                new ArmyGroupClearAllMovingTargetsJob
                {
                    ECB = ecb
                }.Schedule();
                
            }

            if (inputArmyGroupData.DeleteLastTarget)
            {
                if (_flags.Length > 0)
                {
                    ecb.DestroyEntity(_flags[_flags.Length - 1]);
                    _flags.RemoveAt(_flags.Length - 1);
                }
                
                new ArmyGroupDeleteLastTargetJob
                {
                    ECB = ecbP
                }.ScheduleParallel();
            }

            if (inputArmyGroupData.EndMovingAndClearAllTargets)
            {
                foreach (var flag in _flags)
                {
                    ecb.DestroyEntity(flag);
                }
                _flags.Clear();
                new ArmyGroupEndMovingJob
                {
                    ECB = ecbP
                }.ScheduleParallel();
            }
        }


        [BurstCompile]
        [WithAll(typeof(ArmyGroupSelected))]
        public partial struct ArmyGroupSetTargetJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            public float3 TargetPosition;
            [ReadOnly] public ComponentLookup<ArmyGroupMovingTag> ArmyGroupMovingTagLookup;

            private void Execute([ChunkIndexInQuery] int index, ref ArmyGroupMovableData movableData,
                ref DynamicBuffer<ArmyGroupMovingTarget> targets,
                ref DynamicBuffer<ArmyGroupFinalWayPoint> finalWayPoints,
                ref ArmyGroupCalculatePathData pathData, ref PathVisualizeData visualizeData,
                ref NavAgentComponent navAgent,
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
                }
                targets.Add(new ArmyGroupMovingTarget
                {
                    Position = TargetPosition
                });
            }
        }

        [BurstCompile]
        [WithAll(typeof(ArmyGroupSelected))]
        public partial struct ArmyGroupStartMovingJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery] int index, Entity selfEntity,
                ref ArmyGroupMovableData data)
            {
                if (!data.IsTargetReachable)
                {
                    Debug.Log("Target not reachable , this should pop up hints msg");
                    return;
                }

                data.CurWaypoint = 0;
                ECB.SetComponentEnabled<ArmyGroupMovingTag>(index, selfEntity, true);
            }
        }

        [BurstCompile]
        [WithAll(typeof(ArmyGroupSelected))]
        [WithNone(typeof(ArmyGroupMovingTag))]
        public partial struct ArmyGroupDeleteLastTargetJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery]int index,ref DynamicBuffer<ArmyGroupMovingTarget> targets,
                ref DynamicBuffer<ArmyGroupFinalWayPoint> finalWayPoints,
                ref DynamicBuffer<WaypointBuffer> waypointBuffer, ref ArmyGroupMovableData movableData,
                ref ArmyGroupCalculatePathData pathData,
                ref PathVisualizeData visualizeData, ref NavAgentComponent navAgent, Entity selfEntity
            )
            {
                ArmyGroupUtils.ResetArmyGroupMovableData(ref movableData, ref pathData, ref finalWayPoints,
                    ref visualizeData, ref navAgent, ECB,index, selfEntity);
                if (targets.Length <= 0) return;
                targets.RemoveAt(targets.Length - 1);
            }
        }
        [BurstCompile]
        [WithAll(typeof(ArmyGroupSelected))]
        public partial struct ArmyGroupEndMovingJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery]int index,ref DynamicBuffer<ArmyGroupMovingTarget> targets,
                ref DynamicBuffer<ArmyGroupFinalWayPoint> finalWayPoints,
                ref DynamicBuffer<WaypointBuffer> waypointBuffer, ref ArmyGroupMovableData movableData,
                ref ArmyGroupCalculatePathData pathData,
                ref PathVisualizeData visualizeData, ref NavAgentComponent navAgent, Entity selfEntity
            )
            {
                ArmyGroupUtils.ResetArmyGroupMovableData(ref movableData, ref pathData, ref finalWayPoints,
                    ref visualizeData, ref navAgent, ECB,index, selfEntity);
                targets.Clear();
                ECB.SetComponentEnabled<ArmyGroupMovingTag>(index,selfEntity,false);
            }
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
            ref ArmyGroupCalculatePathData pathData,
            ref PathVisualizeData visualizeData, ref NavAgentComponent navAgent, Entity selfEntity
        )
        {
            ArmyGroupUtils.ResetArmyGroupMovableData(ref movableData, ref pathData, ref finalWayPoints,
                ref visualizeData, ref navAgent, ECB, selfEntity);
            targets.Clear();
        }
    }
}