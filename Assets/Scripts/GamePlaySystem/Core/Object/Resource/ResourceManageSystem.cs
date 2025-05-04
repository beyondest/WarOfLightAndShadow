using SparFlame.GamePlaySystem.General;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.GamePlaySystem.Resource
{
    public partial struct ResourceManageSystem : ISystem
    {
        private NativeHashMap<int, ResourceAvailableData> _globalResourceDataCenter;
        private NativeHashSet<int> _renewableResources;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalResourceDataTag>();
            state.RequireForUpdate<EnemyResourceDataTag>();
            state.RequireForUpdate<AllyResourceDataTag>();
            state.RequireForUpdate<ResourceManageSystemConfig>();
            state.RequireForUpdate<GameStatusData>();
            _globalResourceDataCenter =
                new NativeHashMap<int, ResourceAvailableData>(5, Allocator.Persistent);
            _renewableResources = new NativeHashSet<int>(5, Allocator.Persistent);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameStatusData = SystemAPI.GetSingleton<GameStatusData>();
            if (gameStatusData.Value == GameStatus.Init)
            {
                InitDataCenter(ref state);
                UpdateGlobalResourceDataCenter(ref state);
                return;
            }
            if(gameStatusData.Value != GameStatus.Gaming)return;
            // var config = SystemAPI.GetSingleton<ResourceSystemConfig>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var allyDataCenter = SystemAPI.GetSingletonEntity<AllyResourceDataTag>();
            var enemyDataCenter = SystemAPI.GetSingletonEntity<EnemyResourceDataTag>();
            DealResourceChangeRequest(ref state, ecb, allyDataCenter, enemyDataCenter);
            UpdateGlobalResourceDataCenter(ref state);
            DealtDwellingGeneratePopulationRequest(ref state, allyDataCenter, enemyDataCenter, ecb);
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private void DealtDwellingGeneratePopulationRequest(ref SystemState state, Entity allyDataCenter,
            Entity enemyDataCenter,
            EntityCommandBuffer ecb)
        {
            foreach (var (generalAttr, dwellingAttr, entity) in SystemAPI
                         .Query<RefRO<GeneralAttr>, RefRO<DwellingAttr>>()
                         .WithAll<DwellingGeneratePopulationTag>().WithEntityAccess())
            {
                var datas = generalAttr.ValueRO.FactionTag == FactionTag.Ally
                    ? SystemAPI.GetBuffer<ResourceAvailableData>(allyDataCenter)
                    : SystemAPI.GetBuffer<ResourceAvailableData>(enemyDataCenter);
                var data = datas[(int)dwellingAttr.ValueRO.ResourceType];
                data.Amount += dwellingAttr.ValueRO.Amount;
                datas[(int)dwellingAttr.ValueRO.ResourceType] = data;
                ecb.RemoveComponent<DwellingGeneratePopulationTag>(entity);
            }
        }

        private void DealResourceChangeRequest(ref SystemState state, EntityCommandBuffer ecb,
            Entity allyDataCenter, Entity enemyDataCenter)
        {
            foreach (var (requestRO, entity) in SystemAPI.Query<RefRO<ResourceChangeRequest>>().WithEntityAccess())
            {
                var request = requestRO.ValueRO;
                var resourceKey = (int)request.Type;
                var absAmount = request.AbsAmount;

                // If from harvest not renewable resources, then reduce global data center resource
                if (request.RequestType == ResourceRequestType.Harvest
                    && !_renewableResources.Contains(resourceKey))
                {
                    var v = _globalResourceDataCenter[resourceKey];
                    v.Amount -= absAmount;
                    _globalResourceDataCenter[resourceKey] = v;
                }

                var targetCenter = request.FromFaction == FactionTag.Ally ? allyDataCenter : enemyDataCenter;
                var availableDatas = SystemAPI.GetBuffer<ResourceAvailableData>(targetCenter);

                // Consume will minus abs amount to current data center available amount
                if (request.RequestType is ResourceRequestType.Consume or ResourceRequestType.DwellingDestroyConsume)
                {
                    var v2 = availableDatas[resourceKey];
                    v2.Amount -= absAmount;
                    availableDatas[resourceKey] = v2;
                }

                // Release, harvest, generate will add abs amount to current data cetner available amount
                if (request.RequestType is ResourceRequestType.Generate or ResourceRequestType.Harvest
                    or ResourceRequestType.Release
                   )
                {
                    var v2 = availableDatas[resourceKey];
                    v2.Amount += absAmount;
                    availableDatas[resourceKey] = v2;
                }

                // Population consume and release must be handled separately, for correct showing : current occupied/total value
                if (request is { Type: ResourceType.Population, RequestType: ResourceRequestType.Consume }
                    or { Type: ResourceType.Population, RequestType: ResourceRequestType.Release })
                {
                    var pData = SystemAPI.GetComponent<PopulationOccupiedData>(targetCenter);
                    SystemAPI.SetComponent(targetCenter, new PopulationOccupiedData
                    {
                        Value = request.RequestType == ResourceRequestType.Release
                            ? pData.Value - absAmount
                            : pData.Value +
                              absAmount // Consume population : amount should be negative, but recording should add;
                        // Release population : amount should be positive, but recording should minus
                    });
                }

                ecb.DestroyEntity(entity);
            }
        }


        private void UpdateGlobalResourceDataCenter(ref SystemState state)
        {
            foreach (var pair in _globalResourceDataCenter)
            {
                var buffer =
                    SystemAPI.GetBuffer<ResourceAvailableData>(SystemAPI.GetSingletonEntity<GlobalResourceDataTag>());
                var data = buffer[pair.Key];
                data.Amount = pair.Value.Amount;
                buffer[pair.Key] = data;
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_globalResourceDataCenter.IsCreated)
                _globalResourceDataCenter.Dispose();
            if (_renewableResources.IsCreated)
                _renewableResources.Dispose();
        }

        private void InitDataCenter(ref SystemState state)
        {
            // Init global data center
            _globalResourceDataCenter.Clear();
            _renewableResources.Clear();

            var globalInitBuffer =
                SystemAPI.GetBuffer<InitResourceData>(SystemAPI.GetSingletonEntity<GlobalResourceDataTag>());
            var buffer2 = SystemAPI.GetSingletonBuffer<RenewableResourceType>();
            for (var i = 0; i < globalInitBuffer.Length; i++)
            {
                var data = new ResourceAvailableData
                {
                    ResourceType = (ResourceType)i,
                    Amount = 0 // Global resource data should be zeror
                };
                _globalResourceDataCenter[i] = data;
            }

            foreach (var type in buffer2)
            {
                _renewableResources.Add((int)type.ResourceType);
            }

            var allyDataCenter = SystemAPI.GetSingletonEntity<AllyResourceDataTag>();
            var enemyDataCenter = SystemAPI.GetSingletonEntity<EnemyResourceDataTag>();
            var allyInitBuffer = SystemAPI.GetBuffer<InitResourceData>(allyDataCenter);
            var enemyInitBuffer = SystemAPI.GetBuffer<InitResourceData>(enemyDataCenter);
            var allyAvailableBuffer = SystemAPI.GetBuffer<ResourceAvailableData>(allyDataCenter);
            var enemyAvailableBuffer = SystemAPI.GetBuffer<ResourceAvailableData>(enemyDataCenter);
            for (int i = 0; i < allyInitBuffer.Length; i++)
            {
                allyAvailableBuffer[i] = new ResourceAvailableData
                {
                    ResourceType = allyInitBuffer[i].ResourceType,
                    Amount = allyInitBuffer[i].Amount
                };
            }
            for (int i = 0; i < enemyInitBuffer.Length; i++)
            {
                enemyAvailableBuffer[i] = new ResourceAvailableData
                {
                    ResourceType = enemyInitBuffer[i].ResourceType,
                    Amount = enemyInitBuffer[i].Amount
                };
            }
        }
    }
}