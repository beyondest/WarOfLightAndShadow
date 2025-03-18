using Latios.Kinemation;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Animation
{
    public partial struct SingleClipPlayerSystem : ISystem
    {
        private BufferLookup<AnimationEventRequest> _bufferLookup;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<AnimationPlayData>();

            _bufferLookup = state.GetBufferLookup<AnimationEventRequest>();
        }

        public void OnUpdate(ref SystemState state)
        {
            _bufferLookup.Update(ref state);
            var data = SystemAPI.GetSingletonRW<AnimationPlayData>();
            // var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var curTime = (float)SystemAPI.Time.ElapsedTime;
            new ExposedJob
                {
                    ClipLookup = SystemAPI.GetComponentLookup<SingleClip>(true),
                    Et = curTime,
                    LastEt = data.ValueRW.LastEt,
                    BufferLookup = _bufferLookup
                }
                .ScheduleParallel();

            new OptimizedJob
            {
                Et = curTime,
            }.ScheduleParallel();
            data.ValueRW.LastEt = curTime;
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private partial struct OptimizedJob : IJobEntity
        {
            [ReadOnly] public float Et;

            private void Execute(OptimizedSkeletonAspect skeleton, in SingleClip singleClip)
            {
                ref var clip = ref singleClip.Blob.Value.clips[0];
                var clipTime = clip.LoopToClipTime(Et * 10);

                clip.SamplePose(ref skeleton, clipTime, 1f);

                skeleton.EndSamplingAndSync();
            }
        }

        partial struct ExposedJob : IJobEntity
        {
            [NativeDisableParallelForRestriction] public BufferLookup<AnimationEventRequest> BufferLookup;
            [ReadOnly] public ComponentLookup<SingleClip> ClipLookup;
            [ReadOnly] public float Et;
            [ReadOnly] public float LastEt;

            private void Execute(ref LocalTransform transform, in BoneIndex boneIndex,
                in BoneOwningSkeletonReference skeletonRef)
            {
                // if this skeleton belongs to a root skeleton which has a Single Clip component
                var has = ClipLookup.HasComponent(skeletonRef.skeletonRoot);
                if (!has)
                    return;

                ref var clip = ref ClipLookup[skeletonRef.skeletonRoot].Blob.Value.clips[0];
                var preClipTime = clip.LoopToClipTime(LastEt);
                var clipTime = clip.LoopToClipTime(Et);

                // Only root bone will invoke animation event, other bones ignore it
                if (boneIndex.index <= 0)
                {
                    clip.events.TryGetEventsRange(preClipTime, clipTime, out var firstEventIndex, out var eventCount);
                    if (eventCount <= 0) return;
                    Debug.Log($"Event found : {eventCount}");
                    Debug.Log($"First event: {clip.events.names[firstEventIndex]}");
                    for (var i = 0; i < eventCount; i++)
                    {
                        var buffer = BufferLookup[skeletonRef.skeletonRoot];
                        buffer.Add(new AnimationEventRequest
                        {
                            NameHash = clip.events.nameHashes[i],
                            Parameter = clip.events.parameters[i],
                        });

                        Debug.Log("add");
                    }
                }
                else
                {
                    var latiosTransform = clip.SampleBone(boneIndex.index, clipTime);
                    transform.Position = latiosTransform.position;
                    transform.Rotation = latiosTransform.rotation;
                    transform.Scale = latiosTransform.scale;
                }
            }
        }
    }
}


// Single thread approach

// public partial struct SingleClipPlayerSystem2 : ISystem
// {
//     [BurstCompile]
//     public void OnUpdate(ref SystemState state)
//     {
//         float t = (float)SystemAPI.Time.ElapsedTime;
//
//         foreach ((var bones, var singleClip) in Query<DynamicBuffer<BoneReference>, RefRO<SingleClip>>())
//         {
//             ref var clip = ref singleClip.ValueRO.blob.Value.clips[0];
//             var clipTime = clip.LoopToClipTime(t);
//             for (int i = 1; i < bones.Length; i++)
//             {
//                 var boneSampledLocalTransform = clip.SampleBone(i, clipTime);
//
//                 var boneTransformAspect = GetComponentRW<LocalTransform>(bones[i].bone);
//                 boneTransformAspect.ValueRW.Position = boneSampledLocalTransform.position;
//                 boneTransformAspect.ValueRW.Rotation = boneSampledLocalTransform.rotation;
//                 boneTransformAspect.ValueRW.Scale = boneSampledLocalTransform.scale;
//             }
//         }
//     }
// }