// using Latios.Kinemation;
// using Latios.Kinemation.Systems;
// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
//
// namespace SparFlame.GamePlaySystem.Animation.GamePlaySystem.Core.Animation
// {
//     [UpdateInGroup(typeof(InitializationSystemGroup))]
//     public partial struct InitAnimationSystem : ISystem
//     {
//
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             var ecb = new EntityCommandBuffer(Allocator.TempJob);
//
//             foreach (var(skeleton, entity)  in SystemAPI.Query<OptimizedSkeletonAspect>().WithAll<AnimationNeedInitTag>().WithEntityAccess())
//             {
//                 ecb.RemoveComponent<AnimationNeedInitTag>(entity); 
//                 skeleton.ForceInitialize();
//             }
//             
//             ecb.Playback(state.EntityManager);
//             ecb.Dispose();
//         }
//
//         
//         [WithAll(typeof(AnimationNeedInitTag))]
//         [BurstCompile]   
//         public partial struct InitAnimationJob : IJobEntity
//         {
//             public EntityCommandBuffer.ParallelWriter ECB;
//             private void Execute([ChunkIndexInQuery]int index,OptimizedSkeletonAspect skeleton, Entity selfEntity)
//             {
//                 skeleton.ForceInitialize();
//                 ECB.RemoveComponent<AnimationNeedInitTag>(index, selfEntity);
//             }
//         }
//     }
// }