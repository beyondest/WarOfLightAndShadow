using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Systems.SubGameplay.Interact;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace SparFlame.Systems.SubGameplay.EnemyAI
{
  

    public partial struct AoeTriggerManageSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _transformLookup;
        private BufferLookup<AoeTarget> _targetLookup;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<SubGamingTag>();
            _transformLookup = state.GetComponentLookup<LocalTransform>();
            _targetLookup = state.GetBufferLookup<AoeTarget>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _transformLookup.Update(ref state);
            _targetLookup.Update(ref state);
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            new GenerateAoeTriggerJob
            {
                TransformLookup = _transformLookup,
                ECB = ecb,
            }.ScheduleParallel();
            new SyncAoeTriggerJob
            {
                LocalTransformLookup = _transformLookup,
                AoeTargetLookup = _targetLookup,
                ECB = ecb,
            }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct GenerateAoeTriggerJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> TransformLookup;

            private void Execute([ChunkIndexInQuery] int index, Entity selfEntity, in AoeTriggerRequest request)
            {
                ECB.RemoveComponent<AoeTriggerRequest>(index, selfEntity);
                // Safety check
                if (!TransformLookup.TryGetComponent(selfEntity, out var transform)) return;
                // This should not happen, only for safety
                var prefab = request.Prefab;
                var monitor = ECB.Instantiate(index, prefab);
                ECB.AddComponent<SubGameplayEntityTag>(index, monitor);
                ECB.AddComponent(index, monitor, new AoeTriggerData
                {
                    BelongsTo = selfEntity
                });
                ECB.SetComponent(index, monitor, transform);
            }
        }


        [BurstCompile]
        public partial struct SyncAoeTriggerJob : IJobEntity
        {
            [NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> LocalTransformLookup;
            [ReadOnly] public BufferLookup<AoeTarget> AoeTargetLookup;
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery] int index, in AoeTriggerData data, Entity entity)
            {
                // Buff is dead and should be removed
                if (!LocalTransformLookup.TryGetComponent(data.BelongsTo, out var localTransform)
                    || !AoeTargetLookup.HasBuffer(data.BelongsTo))
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