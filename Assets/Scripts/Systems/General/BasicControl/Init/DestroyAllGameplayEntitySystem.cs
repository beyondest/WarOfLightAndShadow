using System;
using SparFlame.Components.General;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.Systems.General.BasicControl
{


    public enum ClearGameplayEntitiesType
    {
        All = 0,
        MainGameplay = 1,
        SubGameplay = 2
    }
    
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial class DestroyAllGameplayEntitySystem : SystemBase
    {
        private bool _initialized;
        protected override void OnCreate()
        {
            RequireForUpdate<GameStatusData>();
        }


        protected override void OnStartRunning()
        {
            if (!_initialized && GameController.Instance)
            {
                GameController.Instance.OnEcsClearGameplayEntities +=
                    type =>
                    {
                        switch (type)
                        {
                            case ClearGameplayEntitiesType.All:
                                ClearMainGameplayEntities();
                                ClearSubGameplayEntities();
                                break;
                            case ClearGameplayEntitiesType.MainGameplay:
                                ClearMainGameplayEntities();
                                break;
                            case ClearGameplayEntitiesType.SubGameplay:
                                ClearSubGameplayEntities();
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(type), type, null);
                        }
                    };
            }
        }

        private void ClearMainGameplayEntities()
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            
           var job =  new DestroyMainGameplayEntityJob
            {
                ECB = ecb.AsParallelWriter()
            }.ScheduleParallel(Dependency);
           job.Complete();
           ecb.Playback(EntityManager);
           ecb.Dispose();
            
        }

        private void ClearSubGameplayEntities()
        {
            var ecb =  new EntityCommandBuffer(Allocator.TempJob);
            var job = new DestroySubGameplayEntityJob
            {
                ECB = ecb.AsParallelWriter()
            }.ScheduleParallel(Dependency);
            job.Complete();
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        [WithAll(typeof(SubGameplayEntityTag))]
        private partial struct DestroySubGameplayEntityJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery] int index, Entity selfEntity)
            {
                ECB.DestroyEntity(index, selfEntity);
            }
        }
        [BurstCompile]
        [WithAll(typeof(MainGameplayEntityTag))]
        private partial struct DestroyMainGameplayEntityJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery] int index, Entity selfEntity)
            {
                ECB.DestroyEntity(index, selfEntity);
            }
        }

        protected override void OnUpdate()
        {
            
        }
    }
}