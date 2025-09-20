using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;

namespace SparFlame.Systems.General.BasicControl.TagManagement
{
    public partial struct CityModelInitSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CityNeedInitModelTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (cityAttr, generalAttr, entity) in SystemAPI
                         .Query<RefRO<CityAttr>, RefRO<MainGameplayGeneralAttr>>().WithAll<CityNeedInitModelTag>()
                         .WithEntityAccess())
            {
                var children = SystemAPI.GetBuffer<LinkedEntityGroup>(entity);
                var inactiveIndex = generalAttr.ValueRO.faction == FactionTag.Light
                    ? cityAttr.ValueRO.darkModelIndex
                    : cityAttr.ValueRO.lightModelIndex;
                ecb.AddComponent<DisableRendering>(children[inactiveIndex].Value);
                ecb.RemoveComponent<CityNeedInitModelTag>(entity);
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }


    }
}