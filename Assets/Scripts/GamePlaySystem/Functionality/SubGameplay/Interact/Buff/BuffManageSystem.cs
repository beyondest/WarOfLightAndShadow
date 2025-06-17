using SparFlame.GamePlaySystem.General;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.Interact
{
    public partial struct BuffManageSystem : ISystem
    {
        private NativeParallelMultiHashMap<int, BuffPrefabDataPair> _buffNameToPrefabDataPair;
        private ComponentLookup<LocalTransform> _transLookup;
        private ComponentLookup<UnitDeadTag> _unitDeadLookup;
        private EntityQuery _buffRequestQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<BuffSystemConfig>();
            state.RequireForUpdate<BuffPrefabDataPair>();
            state.RequireForUpdate<SubGamingTag>();
            _transLookup = state.GetComponentLookup<LocalTransform>();
            _unitDeadLookup = state.GetComponentLookup<UnitDeadTag>();
            _buffRequestQuery = SystemAPI.QueryBuilder().WithAll<BuffRequest>().Build();
        }


        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!_buffNameToPrefabDataPair.IsCreated)
                Initialize();
            var curTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var buffRequests = _buffRequestQuery.ToComponentDataArray<BuffRequest>(Allocator.Temp);
            var entities = _buffRequestQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < buffRequests.Length; i++)
            {
                var request = buffRequests[i];
                var entity = entities[i];
                ecb.DestroyEntity(entity);
                // Tracked target is dead or invalid, then do nothing
                if (request.TrackTarget != Entity.Null)
                {
                    if (!SystemAPI.HasBuffer<TrackedByBuff>(request.TrackTarget))
                        continue;
                }

                var prefabDataPair = new BuffPrefabDataPair();
                var find = false;
                foreach (var pair in _buffNameToPrefabDataPair.GetValuesForKey((int)request.Name))
                {
                    if (request.Filter.factionFilterEnabled)
                    {
                        if (pair.Filter.factionFilterEnabled && pair.Filter.faction != request.Filter.faction) continue;
                    }

                    if (request.Filter.tierFilterEnabled)
                    {
                        if (pair.Filter.tierFilterEnabled && pair.Filter.tier != request.Filter.tier) continue;
                    }

                    find = true;
                    prefabDataPair = pair;
                    break;
                }

                if (!find)
                {
                    // This should never happen
                    // Debug.LogError($"Not find request buff name {request.Name} for filter {request.Filter}");
                    continue;
                }

                CheckAndApplySpecifiedBuffData(ref state, ecb,
                    in request, entity, prefabDataPair);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            entities.Dispose();
            buffRequests.Dispose();

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

        private void CheckAndApplySpecifiedBuffData(ref SystemState state, EntityCommandBuffer ecb,
            in BuffRequest request,
            Entity requestEntity, in BuffPrefabDataPair pair)
        {
            var curTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime;
            var buff = Entity.Null;
            switch (pair.BuffType)
            {
                case BuffType.None:
                    break;
                case BuffType.AoeInteract:
                    buff = state.EntityManager.Instantiate(pair.Prefab);
                    var aoeInteractData = SystemAPI.GetComponent<AoeInteractData>(pair.Prefab);
                    var tarData = SystemAPI.GetComponent<AoeInteractData>(requestEntity);
                    aoeInteractData.TargetFaction = tarData.TargetFaction;
                    aoeInteractData.StatChangeRequest = tarData.StatChangeRequest;
                    aoeInteractData.CurrentTriggerCount = 0;
                    aoeInteractData.TriggerTime = tarData.TriggerTime;
                    ecb.SetComponent(buff, aoeInteractData);
                    break;
                case BuffType.SingleTargetNotStackable:
                    // Target is dead, then not spawn the buff
                    var buffer = SystemAPI.GetBuffer<TrackedByBuff>(request.TrackTarget);
                    foreach (var buffExist in buffer)
                    {
                        if (buffExist.Name == request.Name)
                        {
                            var buffDataRw = SystemAPI.GetComponentRW<GeneralBuffData>(buffExist.BuffEntity);
                            buffDataRw.ValueRW.StartTime = curTime;
                            return;
                        }
                    }

                    buff = state.EntityManager.Instantiate(pair.Prefab);
                    buffer.Add(new TrackedByBuff
                    {
                        BuffEntity = buff,
                        Name = pair.Name,
                        Count = 1,
                        MaxStackCount = 1
                    });
                    break;
      
            }

            if (buff == Entity.Null) return;

            ecb.AddComponent<SubGameplayEntityTag>(buff);
            ecb.SetComponent(buff, new LocalTransform
            {
                Position = request.SpawnPosition,
                Rotation = request.SpawnRotation,
                Scale = 1
            });
            var generalBuffData = SystemAPI.GetComponent<GeneralBuffData>(pair.Prefab);
            generalBuffData.TrackTarget = request.TrackTarget;
            generalBuffData.StartTime = curTime;
            ecb.SetComponent(buff, generalBuffData);
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
            _buffNameToPrefabDataPair =
                new NativeParallelMultiHashMap<int, BuffPrefabDataPair>(5, Allocator.Persistent);
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

            private void Execute([ChunkIndexInQuery] int index, Entity selfEntity, in GeneralBuffData data)
            {
                if (data.TrackTarget != Entity.Null)
                {
                    if (!TransformLookup.TryGetComponent(data.TrackTarget, out var transform) ||
                        UnitDeadTagLookup.HasComponent(data.TrackTarget))
                    {
                        ECB.DestroyEntity(index, selfEntity);
                        return;
                    }

                    ref var selfTrans = ref TransformLookup.GetRefRW(selfEntity).ValueRW;
                    selfTrans.Position = transform.Position;
                    selfTrans.Rotation = transform.Rotation;
                }

                if (CurTime > data.StartTime + data.Duration)
                {
                    ECB.DestroyEntity(index, selfEntity);
                }
            }
        }
    }
}