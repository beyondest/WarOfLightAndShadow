using GamePlaySystem.Functionality.MainGameplay.ArmyGroup.Sight;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace SparFlame.Systems.MainGameplay.ArmyGroup
{
 

    public struct ArmyGroupSightData : IComponentData
    {
        public Entity BelongsTo;
    }

    public partial struct AoeTriggerManageSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _transformLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GameStatusData>();
            state.RequireForUpdate<ArmyGroupSightConfig>();
            _transformLookup = state.GetComponentLookup<LocalTransform>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameStatusData = SystemAPI.GetSingleton<GameStatusData>();
            if(gameStatusData.Value != GameStatus.MainGaming && gameStatusData.Value != GameStatus.SubGaming)return;
            _transformLookup.Update(ref state);
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            new GenerateArmyGroupSightJob
            {
                TransformLookup = _transformLookup,
                ECB = ecb,
            }.ScheduleParallel();
            new SyncArmyGroupSightJob
            {
                LocalTransformLookup = _transformLookup,
                ECB = ecb,
            }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct GenerateArmyGroupSightJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> TransformLookup;

            private void Execute([ChunkIndexInQuery] int index, Entity selfEntity, in ArmyGroupSightRequest request)
            {
                ECB.RemoveComponent<ArmyGroupSightRequest>(index, selfEntity);
                // Safety check
                if (!TransformLookup.TryGetComponent(selfEntity, out var transform)) return;
                // This should not happen, only for safety
                var prefab = request.Prefab;
                var monitor = ECB.Instantiate(index, prefab);
                ECB.AddComponent<MainGameplayEntityTag>(index, monitor);
                ECB.AddComponent(index, monitor, new ArmyGroupSightData
                {
                    BelongsTo = selfEntity
                });
                ECB.SetComponent(index, monitor, transform);
            }
        }


        [BurstCompile]
        public partial struct SyncArmyGroupSightJob : IJobEntity
        {
            [NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> LocalTransformLookup;
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery] int index, in ArmyGroupSightData data, Entity entity)
            {
                // Buff is dead and should be removed
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