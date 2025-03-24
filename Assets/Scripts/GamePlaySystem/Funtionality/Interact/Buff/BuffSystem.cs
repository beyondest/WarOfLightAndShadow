using SparFlame.GamePlaySystem.Movement;
using Unity.Burst;
using Unity.Entities;
using Unity.Physics.Systems;

namespace SparFlame.GamePlaySystem.Interact
{
    [UpdateAfter(typeof(StatSystem))]
    public partial struct BuffSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
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