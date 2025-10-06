using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.Systems.MainGameplay.City
{
    public partial struct CityInvaderManageSystem : ISystem
    {
        private EntityQuery _removeRequest;
        private EntityQuery _clearRequest;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MainGamingTag>();
            _removeRequest = SystemAPI.QueryBuilder().WithAll<RemoveCityFutureInvaderRequest>().Build();
            _clearRequest = SystemAPI.QueryBuilder().WithAll<ClearCityFutureInvadersRequest>().Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (_removeRequest.IsEmpty && _clearRequest.IsEmpty) return;
            using var ecb = new EntityCommandBuffer(Allocator.Temp);
            using var requests1 = _removeRequest.ToComponentDataArray<RemoveCityFutureInvaderRequest>(Allocator.Temp);
            using var entities1 = _removeRequest.ToEntityArray(Allocator.Temp);
            for (var index = 0; index < requests1.Length; index++)
            {
                var request = requests1[index];
                var entity = entities1[index];
                ecb.DestroyEntity(entity);
                var buffer = SystemAPI.GetBuffer<CityFutureInvaders>(request.City);
                for (var i = buffer.Length - 1; i >= 0; i--)
                {
                    var invader = buffer[i];
                    if (invader.ArmyGroup == request.ArmyGroup)
                    {
                        buffer.RemoveAt(i);
                    }
                }
            }
            var requests = _clearRequest.ToComponentDataArray<ClearCityFutureInvadersRequest>(Allocator.Temp);
            var entities = _clearRequest.ToEntityArray(Allocator.Temp);
            for (var index = 0; index < requests.Length; index++)
            {
                var request = requests[index];
                var entity = entities[index];
                ecb.DestroyEntity(entity);
                var buffer = SystemAPI.GetBuffer<CityFutureInvaders>(request.City);
                buffer.Clear();
            }
            ecb.Playback(state.EntityManager);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}