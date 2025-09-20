using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.Systems.General.BasicControl.Init
{
    public partial class AssignIDSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<AssignGlobalSingleIDRequest>();
        }

        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            ref var counter = ref SystemAPI.GetSingletonRW<GlobalSingIDCounter>().ValueRW;
            foreach (var (id, armyGroup) in SystemAPI.Query<RefRW<GlobalSingleId>>()
                         .WithAll<AssignGlobalSingleIDRequest>().WithEntityAccess())
            {
                id.ValueRW.value = counter.baseValue + counter.addValue;
                counter.addValue++;
                ecb.RemoveComponent<AssignGlobalSingleIDRequest>(armyGroup);
            }
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}