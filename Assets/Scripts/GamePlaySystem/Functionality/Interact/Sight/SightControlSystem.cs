using SparFlame.GamePlaySystem.Garrison;
using SparFlame.GamePlaySystem.General;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{
    public partial struct SightControlSystem : ISystem
    {
        private ComponentLookup<PhysicsCollider> _colliderLookup;
        private ComponentLookup<LocalTransform> _transformLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<NotPauseTag>();
            _transformLookup = state.GetComponentLookup<LocalTransform>();
            _colliderLookup = state.GetComponentLookup<PhysicsCollider>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            _transformLookup.Update(ref state);
            _colliderLookup.Update(ref state);
            new GenerateSightJob
            {
                ECB = ecb,
            }.ScheduleParallel();
           
            new SyncSightJob
            {
                ECB = ecb,
                LocalTransformLookup = _transformLookup,
            }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct GenerateSightJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery] int index, ref GenerateSightRequest request, Entity requestEntity,
                in LocalTransform localTransform)
            {
                var sight = ECB.Instantiate(index, request.SightPrefab);
                ECB.AddComponent(index, sight, new SightData
                {
                    BelongsTo = requestEntity
                });
                ECB.SetComponent(index, sight, localTransform);
                ECB.RemoveComponent<GenerateSightRequest>(index, requestEntity);
                

            }
        }

        [BurstCompile]
        public partial struct ChangeSightRangeJob : IJobEntity
        {
            [NativeDisableParallelForRestriction]
            public ComponentLookup<PhysicsCollider> ColliderLookup;
            [ReadOnly]
            public EntityCommandBuffer.ParallelWriter ECB;
            private void Execute([ChunkIndexInQuery] int index,Entity entity, in DeferredColliderUpdate update)
            {
                ref var collider = ref ColliderLookup.GetRefRW(entity).ValueRW;
                unsafe
                {
                    var old = (CylinderCollider*)collider.ColliderPtr;
                    var geo = old->Geometry;
                    var material = old->Material;
                    geo.Radius = update.NewRadius;
                  
                    var newValue = CylinderCollider.Create(geo, update.Filter,material);
                    collider.Value = newValue;
                }
                ECB.RemoveComponent<DeferredColliderUpdate>(index,entity);
            }
        }
        
        [BurstCompile]
        public partial struct SyncSightJob : IJobEntity
        {
            [NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> LocalTransformLookup;
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery] int index, in SightData data, Entity entity)
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
        
        public struct DeferredColliderUpdate : IComponentData
        {
            public float NewRadius;
            public CollisionFilter Filter;
        }

    }
}