using Unity.Burst;
using Unity.Entities;
using Unity.Physics;

namespace SparFlame.GamePlaySystem.Physics
{
    partial struct PhysicsLockAxisSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsLockAxisAuthoring.SetPhysicsMassBaking>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var(mass, setPhysicsMass)
                     in SystemAPI.Query<RefRW<PhysicsMass>, RefRO<PhysicsLockAxisAuthoring.SetPhysicsMassBaking>>()
                         .WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities))
            {
                mass.ValueRW.InverseInertia[0] = setPhysicsMass.ValueRO.InfiniteInertiaX ? 0 : mass.ValueRW.InverseInertia[0];
                mass.ValueRW.InverseInertia[1] = setPhysicsMass.ValueRO.InfiniteInertiaY ? 0 : mass.ValueRW.InverseInertia[1];
                mass.ValueRW.InverseInertia[2] = setPhysicsMass.ValueRO.InfiniteInertiaZ ? 0 : mass.ValueRW.InverseInertia[2];
                mass.ValueRW.InverseMass = setPhysicsMass.ValueRO.InfiniteMass ? 0 : mass.ValueRW.InverseMass;
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        
        }
    }
}
