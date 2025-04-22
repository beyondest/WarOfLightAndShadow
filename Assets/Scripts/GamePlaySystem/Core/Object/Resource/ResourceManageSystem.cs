using SparFlame.GamePlaySystem.General;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.Resource
{
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct ResourceManageSystem : ISystem
    {
        private NativeHashMap<int, ResourceData> _globalResourceDataCenter;
        private NativeHashSet<int> _renewableResources;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalResourceDataTag>();
            state.RequireForUpdate<EnemyResourceDataTag>();
            state.RequireForUpdate<AllyResourceDataTag>();
            state.RequireForUpdate<NotPauseTag>();
            state.RequireForUpdate<ResourceManageSystemConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!_globalResourceDataCenter.IsCreated)
                InitGlobalResourceDataCenter(ref state);


            // var config = SystemAPI.GetSingleton<ResourceSystemConfig>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            DealResourceChangeRequest(ref state, ecb);

            UpdateGlobalResourceDataCenter(ref state);

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private void DealResourceChangeRequest(ref SystemState state, EntityCommandBuffer ecb)
        {
            var allyDataCenter = SystemAPI.GetSingletonEntity<AllyResourceDataTag>();
            var enemyDataCenter = SystemAPI.GetSingletonEntity<EnemyResourceDataTag>();
            foreach (var (requestRO, entity) in SystemAPI.Query<RefRO<ResourceChangeRequest>>().WithEntityAccess())
            {
                var request = requestRO.ValueRO;
                var resourceKey = (int)request.Type;
                var amount = request.Amount;

                // If from harvest not reneable resources, then reduce global data center resource
                if (request.RequestType == ResourceRequestType.Harvest
                    && !_renewableResources.Contains(resourceKey))
                {
                    var v = _globalResourceDataCenter[resourceKey];
                    v.Amount -= amount;
                    _globalResourceDataCenter[resourceKey] = v;
                }

                var targetCenter = request.FromFaction == FactionTag.Ally ? allyDataCenter : enemyDataCenter;
                var buffer = SystemAPI.GetBuffer<ResourceData>(targetCenter);

                // Except for population release, others will change total amount. 
                // NOTICE : Population total amount accounts for available value, true total value = available + occupied
                if (request.RequestType != ResourceRequestType.Release)
                {
                    var v2 = buffer[resourceKey];
                    v2.Amount += amount;
                    buffer[resourceKey] = v2;
                }
                // Population consume and release must be handled separately, for correct showing : current occupied/total value

                if (request is { Type: ResourceType.Population, RequestType: ResourceRequestType.Consume }
                    or { Type: ResourceType.Population, RequestType: ResourceRequestType.Release })
                {
                    var pData = SystemAPI.GetComponent<PopulationOccupiedData>(targetCenter);
                    SystemAPI.SetComponent(targetCenter, new PopulationOccupiedData
                    {
                        Value = pData.Value -
                                amount // Consume population : amount should be negative, but recording should add;
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
                var buffer = SystemAPI.GetBuffer<ResourceData>(SystemAPI.GetSingletonEntity<GlobalResourceDataTag>());
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

        private void InitGlobalResourceDataCenter(ref SystemState state)
        {
            var buffer = SystemAPI.GetBuffer<ResourceData>(SystemAPI.GetSingletonEntity<GlobalResourceDataTag>());
            var buffer2 = SystemAPI.GetSingletonBuffer<RenewableResourceType>();

            _globalResourceDataCenter = new NativeHashMap<int, ResourceData>(buffer.Length, Allocator.Persistent);
            _renewableResources = new NativeHashSet<int>(buffer2.Length, Allocator.Persistent);
            for (var i = 0; i < buffer.Length; i++)
            {
                var data = new ResourceData
                {
                    ResourceType = (ResourceType)i,
                    Amount = buffer[i].Amount
                };
                _globalResourceDataCenter[i] = data;
            }

            foreach (var type in buffer2)
            {
                _renewableResources.Add((int)type.ResourceType);
            }
        }
    }
}