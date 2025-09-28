using SparFlame.Components.General;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.Systems.General.BasicControl.Init
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [BurstCompile]
    public partial class AssignIDSystem : SystemBase
    {
        [BurstCompile]
        protected override void OnCreate()
        {
            RequireForUpdate<AssignGlobalSingleIDRequest>();
            RequireForUpdate<GameStatusData>();
        }

        [BurstCompile]
        protected override void OnUpdate()
        {
            var gameStatusData = SystemAPI.GetSingleton<GameStatusData>();
            if(gameStatusData.Value != GameStatus.MainGaming && gameStatusData.Value != GameStatus.SubGaming)return;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            ref var counter = ref SystemAPI.GetSingletonRW<GlobalSingIDCounter>().ValueRW;
            foreach (var (id, entity) in SystemAPI.Query<RefRW<GlobalSingleId>>()
                         .WithAll<AssignGlobalSingleIDRequest>().WithEntityAccess())
            {
                id.ValueRW.value = counter.baseValue + counter.addValue;
                counter.addValue++;
                ecb.RemoveComponent<AssignGlobalSingleIDRequest>(entity);
            }
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}