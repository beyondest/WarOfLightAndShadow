
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact.GamePlaySystem.Functionality.Interact.Buff.Authoring;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{
    public partial struct BuffManageSystem : ISystem
    {
        private NativeParallelMultiHashMap<int, BuffPrefabDataPair> _buffNameToPrefabDataPair;
        private ComponentLookup<LocalTransform> _transLookup;
        private ComponentLookup<UnitDeadTag> _unitDeadLookup;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<BuffSystemConfig>();
            state.RequireForUpdate<BuffPrefabDataPair>();
            state.RequireForUpdate<GamingTag>();
            _transLookup = state.GetComponentLookup<LocalTransform>();
            _unitDeadLookup = state.GetComponentLookup<UnitDeadTag>();
        }
        

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if(!_buffNameToPrefabDataPair.IsCreated)
                Initialize();
            var curTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (request, entity) in SystemAPI.Query<RefRO<BuffRequest>>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
                var prefabDataPair = new BuffPrefabDataPair();
                var find = false;
                foreach (var pair in _buffNameToPrefabDataPair.GetValuesForKey((int)request.ValueRO.Name))
                {
                    if (request.ValueRO.Filter.factionFilterEnabled)
                    {
                        if(pair.Filter.factionFilterEnabled && pair.Filter.faction != request.ValueRO.Filter.faction)continue;
                    }

                    if (request.ValueRO.Filter.tierFilterEnabled)
                    {
                        if(pair.Filter.tierFilterEnabled && pair.Filter.tier != request.ValueRO.Filter.tier)continue;
                    }
                    find = true;
                    prefabDataPair = pair;
                    break;
                }
                if (!find)
                {
                    // This should never happen
                    Debug.LogError($"Not find request buff name {request.ValueRO.Name} for filter {request.ValueRO.Filter}");
                    continue;
                }
                
                var buff = ecb.Instantiate(prefabDataPair.Prefab);
                ecb.AddComponent<GameplayEntityTag>(buff);
                ecb.SetComponent(buff,new LocalTransform
                {
                    Position = request.ValueRO.SpawnPosition,
                    Rotation = request.ValueRO.SpawnRotation,
                    Scale = 1
                });
                ecb.AddComponent(buff, new BuffData
                {
                    TrackTarget = request.ValueRO.TrackTarget,
                    Duration = request.ValueRO.IfBuffLifeHandledByGeneralBuffManageSystem ? request.ValueRO.Duration : float.MaxValue,
                    StartTime = curTime
                });
                CheckAndApplySpecifiedBuffData(ref state, entity, ecb,buff, request.ValueRO, prefabDataPair.Prefab);
                
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            _transLookup.Update(ref state);
            _unitDeadLookup.Update(ref state);
            new GeneralBuffManageJob
            {
                ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                TransformLookup = _transLookup,
                CurTime = curTime,
                UnitDeadTagLookup = _unitDeadLookup
            }.ScheduleParallel();
        }

        private void CheckAndApplySpecifiedBuffData(ref SystemState state,Entity requestEntity, EntityCommandBuffer ecb,
            Entity buff, in BuffRequest request, Entity prefab)
        {
            switch(request.BuffType)
            {
                case BuffType.None:
                    break;
                case BuffType.AoeInteract:
                    var aoeInteractData = SystemAPI.GetComponent<AoeInteractData>(prefab);
                    var tarData = SystemAPI.GetComponent<AoeInteractData>(requestEntity);
                    aoeInteractData.TargetFaction = tarData.TargetFaction;
                    aoeInteractData.StatChangeRequest = tarData.StatChangeRequest;
                    aoeInteractData.CurrentTriggerCount = 0;
                    aoeInteractData.TriggerTime = tarData.TriggerTime;
                    ecb.SetComponent(buff, aoeInteractData);
                    break;
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_buffNameToPrefabDataPair.IsCreated)
                _buffNameToPrefabDataPair.Dispose();
        }

        private void Initialize()
        {
            var buffer = SystemAPI.GetSingletonBuffer<BuffPrefabDataPair>();
            _buffNameToPrefabDataPair = new NativeParallelMultiHashMap<int, BuffPrefabDataPair>(5,Allocator.Persistent);
            foreach (var pair in buffer)
            {
                _buffNameToPrefabDataPair.Add((int)pair.Name, pair);
            }
            
        }
        
        [BurstCompile]
        public partial struct GeneralBuffManageJob : IJobEntity
        {
            [ReadOnly] public float CurTime;
            public EntityCommandBuffer.ParallelWriter ECB;
            [NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public ComponentLookup<UnitDeadTag> UnitDeadTagLookup;
            private void Execute([ChunkIndexInQuery] int index, Entity selfEntity,in BuffData data)
            {
                if (data.TrackTarget != Entity.Null)
                {
                    if ( !TransformLookup.TryGetComponent(data.TrackTarget, out var transform) || UnitDeadTagLookup.HasComponent(data.TrackTarget))
                    {
                        ECB.DestroyEntity(index,selfEntity);
                        return;
                    }
                    ref var selfTrans = ref TransformLookup.GetRefRW(selfEntity).ValueRW;
                    selfTrans.Position = transform.Position;
                    selfTrans.Rotation = transform.Rotation;
                }

                if (CurTime > data.StartTime + data.Duration)
                {
                    ECB.DestroyEntity(index,selfEntity);
                }
                
            }
        }
        
        
        
        
    }
}