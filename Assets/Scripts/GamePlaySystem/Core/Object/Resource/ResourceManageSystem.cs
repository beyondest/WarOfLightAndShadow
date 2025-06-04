using SparFlame.GamePlaySystem.General;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.GamePlaySystem.Resource
{
    public partial struct ResourceManageSystem : ISystem
    {
        private NativeHashMap<int, ResourceTypeToAvailableAmount> _globalResourceDataCenter;
        private NativeHashSet<int> _renewableResources;
        private NativeHashSet<int> _populationResources;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalResourceDataTag>();
            state.RequireForUpdate<EnemyResourceDataTag>();
            state.RequireForUpdate<AllyResourceDataTag>();
            state.RequireForUpdate<ResourceManageSystemConfig>();
            state.RequireForUpdate<GameStatusData>();
            _globalResourceDataCenter =
                new NativeHashMap<int, ResourceTypeToAvailableAmount>(5, Allocator.Persistent);
            _renewableResources = new NativeHashSet<int>(5, Allocator.Persistent);
            _populationResources = new NativeHashSet<int>(3, Allocator.Persistent);
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

            if (gameStatusData.Value != GameStatus.Gaming) return;
            // var config = SystemAPI.GetSingleton<ResourceSystemConfig>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var allyDataCenter = SystemAPI.GetSingletonEntity<AllyResourceDataTag>();
            var enemyDataCenter = SystemAPI.GetSingletonEntity<EnemyResourceDataTag>();
            DealResourceChangeRequest(ref state, ecb, allyDataCenter, enemyDataCenter);
            UpdateGlobalResourceDataCenter(ref state);
            DealtDwellingGeneratePopulationRequest(ref state, ecb);
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private void DealtDwellingGeneratePopulationRequest(ref SystemState state,
            EntityCommandBuffer ecb)
        {
            foreach (var (generalAttr, dwellingAttr, entity) in SystemAPI
                         .Query<RefRO<GeneralAttr>, RefRO<DwellingAttr>>()
                         .WithAll<DwellingGeneratePopulationTag>().WithEntityAccess())
            {
                ecb.RemoveComponent<DwellingGeneratePopulationTag>(entity);
                var dwellingGenerateRequest = new ResourceChangeRequest
                {
                    AbsAmount = dwellingAttr.ValueRO.Amount,
                    RequestType = ResourceRequestType.Generate,
                    FromFaction = generalAttr.ValueRO.FactionTag,
                    Type = dwellingAttr.ValueRO.ResourceType
                };
                var request = ecb.CreateEntity();
                ecb.AddComponent<GameplayEntityTag>(request);
                ecb.AddComponent(request, dwellingGenerateRequest);
                // var datas = generalAttr.ValueRO.FactionTag == FactionTag.Ally
                //     ? SystemAPI.GetBuffer<ResourceTypeToAvailableAmount>(allyDataCenter)
                //     : SystemAPI.GetBuffer<ResourceTypeToAvailableAmount>(enemyDataCenter);
                // var data = datas[(int)dwellingAttr.ValueRO.ResourceType];
                // data.Amount += dwellingAttr.ValueRO.Amount;
                // datas[(int)dwellingAttr.ValueRO.ResourceType] = data;
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

                Entity targetCenter;

                if (request.FromFaction == FactionTag.Ally)
                {
                    targetCenter = allyDataCenter;
                }
                else
                {
                    targetCenter = enemyDataCenter;
                }

                var availableDatas = SystemAPI.GetBuffer<ResourceTypeToAvailableAmount>(targetCenter);

                // Consume will minus abs amount to current data center available amount
                if (request.RequestType is ResourceRequestType.Consume or ResourceRequestType.DwellingDestroyConsume)
                {
                    var v2 = availableDatas[resourceKey];
                    v2.Amount = math.max(0, v2.Amount - absAmount);
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
                if (request.RequestType is ResourceRequestType.Consume or ResourceRequestType.Release
                    && _populationResources.Contains((int)request.Type))
                {
                    var pData = SystemAPI.GetComponent<PopulationSpecialData>(targetCenter);
                    pData.OccupiedAmount = request.RequestType == ResourceRequestType.Release
                        ? pData.OccupiedAmount - absAmount
                        : pData.OccupiedAmount +
                          absAmount; // Consume population : amount should be negative, but recording should add;
                    // Release population : amount should be positive, but recording should minus
                    SystemAPI.SetComponent(targetCenter, pData);
                }

                if (request.RequestType is ResourceRequestType.Generate or ResourceRequestType.DwellingDestroyConsume &&
                    _populationResources.Contains((int)request.Type))
                {
                    var pData = SystemAPI.GetComponent<PopulationSpecialData>(targetCenter);
                    pData.TotalAmount = request.RequestType ==
                                        ResourceRequestType.DwellingDestroyConsume
                        ? pData.TotalAmount - absAmount
                        : pData.TotalAmount + absAmount; 
                    SystemAPI.SetComponent(targetCenter, pData);
                }

                ecb.DestroyEntity(entity);
            }
        }


        private void UpdateGlobalResourceDataCenter(ref SystemState state)
        {
            foreach (var pair in _globalResourceDataCenter)
            {
                var buffer =
                    SystemAPI.GetBuffer<ResourceTypeToAvailableAmount>(SystemAPI
                        .GetSingletonEntity<GlobalResourceDataTag>());
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
            if (_populationResources.IsCreated)
                _populationResources.Dispose();
        }

        private void InitDataCenter(ref SystemState state)
        {
            // Init global data center
            _globalResourceDataCenter.Clear();
            _renewableResources.Clear();
            _populationResources.Clear();
            var globalInitBuffer =
                SystemAPI.GetBuffer<ResourceTypeToInitAmount>(SystemAPI.GetSingletonEntity<GlobalResourceDataTag>());
            var buffer2 = SystemAPI.GetSingletonBuffer<RenewableResourceType>();
            var config = SystemAPI.GetSingleton<ResourceManageSystemConfig>();
            for (var i = 0; i < globalInitBuffer.Length; i++)
            {
                var data = new ResourceTypeToAvailableAmount
                {
                    ResourceType = (ResourceType)i,
                    Amount = 0 // Global resource data should be zeror
                };
                _globalResourceDataCenter[i] = data;
            }

            foreach (var type in buffer2)
            {
                _renewableResources.Add((int)type.resourceType);
            }

            foreach (var type in config.PopulationResourceTypes)
            {
                _populationResources.Add((int)type);
            }

            var allyDataCenter = SystemAPI.GetSingletonEntity<AllyResourceDataTag>();
            var enemyDataCenter = SystemAPI.GetSingletonEntity<EnemyResourceDataTag>();
            var allyInitBuffer = SystemAPI.GetBuffer<ResourceTypeToInitAmount>(allyDataCenter);
            var enemyInitBuffer = SystemAPI.GetBuffer<ResourceTypeToInitAmount>(enemyDataCenter);
            var allyAvailableBuffer = SystemAPI.GetBuffer<ResourceTypeToAvailableAmount>(allyDataCenter);
            var enemyAvailableBuffer = SystemAPI.GetBuffer<ResourceTypeToAvailableAmount>(enemyDataCenter);
            for (int i = 0; i < allyInitBuffer.Length; i++)
            {
                allyAvailableBuffer[i] = new ResourceTypeToAvailableAmount
                {
                    ResourceType = allyInitBuffer[i].ResourceType,
                    Amount = allyInitBuffer[i].Amount
                };
            }

            for (int i = 0; i < enemyInitBuffer.Length; i++)
            {
                enemyAvailableBuffer[i] = new ResourceTypeToAvailableAmount
                {
                    ResourceType = enemyInitBuffer[i].ResourceType,
                    Amount = enemyInitBuffer[i].Amount
                };
            }
        }
    }
}