using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using Unity.Burst;
using Unity.Entities;

namespace SparFlame.GamePlaySystem.Command
{
    
    [UpdateAfter(typeof(PlayerCommandSystem))]
    [UpdateBefore(typeof(SightUpdateListSystem))]
    [UpdateBefore(typeof(BuffSystem))]
    [BurstCompile]
    public partial struct PCCommandSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NotPauseTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {

        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}