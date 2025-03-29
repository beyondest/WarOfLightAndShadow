using SparFlame.GamePlaySystem.Movement;
using Unity.Burst;
using Unity.Entities;
using Unity.Physics.Systems;

namespace SparFlame.GamePlaySystem.Interact
{
    public partial struct BuffSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // if (BuffDataLookup.TryGetComponent(entity, out BuffData buffData))
            // {
            //     rangeSq *= buffData.InteractRangeMultiplier * buffData.InteractRangeMultiplier;
            //     amount *= (int)buffData.InteractAmountMultiplier;
            //     speed *= buffData.InteractSpeedMultiplier;
            //     ECB.RemoveComponent<BuffData>(index, entity);
            // }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}