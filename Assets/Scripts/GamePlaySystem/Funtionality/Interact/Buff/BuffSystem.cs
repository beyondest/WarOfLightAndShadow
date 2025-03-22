using SparFlame.GamePlaySystem.Movement;
using Unity.Burst;
using Unity.Entities;

namespace SparFlame.GamePlaySystem.Interact
{
    [UpdateAfter(typeof(AutoChooseTargetSystem))]
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