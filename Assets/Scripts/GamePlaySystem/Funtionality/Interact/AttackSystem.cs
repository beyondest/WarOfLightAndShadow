using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
namespace SparFlame.GamePlaySystem.Interact
{
    public partial struct AttackSystem : ISystem
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

        [BurstCompile]
        [WithAll(typeof(AttackStateTag))]
        public partial struct AttackStateJob : IJobEntity
        {
            [ReadOnly] public float DeltaTime;
            
            private void Execute(ref LocalTransform transform, in AttackAbility attackAbility, Entity entity)
            {
                
            }
        }
    }
}