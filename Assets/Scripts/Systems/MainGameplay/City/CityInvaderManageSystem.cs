using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.Systems.MainGameplay.City
{
    public partial struct CityInvaderManageSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MainGamingTag>();
            state.RequireForUpdate<RemoveCityFutureInvaderRequest>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (request, entity) in SystemAPI.Query<RefRO<RemoveCityFutureInvaderRequest>>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
                var buffer = SystemAPI.GetBuffer<CityFutureInvaders>(request.ValueRO.City);
                for (var i = buffer.Length - 1; i >= 0; i--)
                {
                    var invader = buffer[i];
                    if (invader.ArmyGroup == request.ValueRO.ArmyGroup)
                    {
                        buffer.RemoveAt(i);
                    }
                }
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}