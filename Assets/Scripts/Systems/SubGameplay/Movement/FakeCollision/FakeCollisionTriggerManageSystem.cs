using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace SparFlame.Systems.SubGameplay.Movement.FakeCollision
{
    public struct FakeCollisionTriggerData : IComponentData
    {
        public Entity BelongsTo;
    }
    public partial struct FakeCollisionTriggerManageSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _transformLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SubGamingTag>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            _transformLookup = state.GetComponentLookup<LocalTransform>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            _transformLookup.Update(ref state);
            new GenerateFakeColliderTriggerJob
            {
                ECB = ecb,
            }.ScheduleParallel();
           
            new SyncFakeCollisionTriggerJob
            {
                ECB = ecb,
                LocalTransformLookup = _transformLookup,
            }.ScheduleParallel();
        }
        
        [BurstCompile]
        public partial struct GenerateFakeColliderTriggerJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            private void Execute([ChunkIndexInQuery] int index, ref FakeCollisionTriggerRequest request, Entity requestEntity,
                in LocalTransform localTransform)
            {
                var sight = ECB.Instantiate(index, request.TriggerPrefab);
                ECB.AddComponent<SubGameplayEntityTag>(index,sight);

                ECB.AddComponent(index, sight, new FakeCollisionTriggerData
                {
                    BelongsTo = requestEntity
                });
                ECB.SetComponent(index, sight, localTransform);
                ECB.RemoveComponent<FakeCollisionTriggerRequest>(index, requestEntity);
            }
        }
        
        [BurstCompile]
        public partial struct SyncFakeCollisionTriggerJob : IJobEntity
        {
            [NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> LocalTransformLookup;
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery] int index, in FakeCollisionTriggerData data, Entity entity)
            {
                // Entity dead and sight should remove
                if (!LocalTransformLookup.TryGetComponent(data.BelongsTo, out var localTransform))
                {
                    ECB.DestroyEntity(index, entity);
                    return;
                }
                ref var transform = ref LocalTransformLookup.GetRefRW(entity).ValueRW;
                transform.Position = localTransform.Position;
                transform.Rotation = localTransform.Rotation;
            }
        }
    }
    
    
}