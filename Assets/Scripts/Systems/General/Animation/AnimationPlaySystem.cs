using Latios.Kinemation;
using SparFlame.Components.General;
using Unity.Burst;
using Unity.Entities;
using Unity.Collections;

namespace SparFlame.Systems.General.Animation
{
    [BurstCompile]
    public partial struct SingleClipPlayerSystem : ISystem
    {
        private BufferLookup<AnimationEventData> _bufferLookup;
        private ComponentLookup<AnimationStateData> _stateLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTimeData>();
            // state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<AnimationPlayData>();
            state.RequireForUpdate<WaitInfo>();
            state.RequireForUpdate<GameStatusData>();
            _bufferLookup = state.GetBufferLookup<AnimationEventData>();
            _stateLookup = state.GetComponentLookup<AnimationStateData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameStatusData = SystemAPI.GetSingleton<GameStatusData>();
            var waitInfo = SystemAPI.GetSingleton<WaitInfo>();
            if (gameStatusData.Value != GameStatus.MainGaming && gameStatusData.Value != GameStatus.SubGaming) return;
            if (waitInfo.WaitType != WaitType.None) return;


            _bufferLookup.Update(ref state);
            _stateLookup.Update(ref state);
            var data = SystemAPI.GetSingletonRW<AnimationPlayData>();
            // var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var curTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime;
            // new ExposedJob
            //     {
            //         ClipLookup = SystemAPI.GetComponentLookup<ClipBlobData>(true),
            //         Et = curTime,
            //         LastEt = data.ValueRW.LastEt,
            //         BufferLookup = _bufferLookup,
            //         StateLookup = _stateLookup
            //     }
            //     .ScheduleParallel();

            new OptimizedJob
            {
                Et = curTime,
                PreClipTime = data.ValueRW.LastEt
            }.ScheduleParallel();
            data.ValueRW.LastEt = curTime;
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private partial struct OptimizedJob : IJobEntity
        {
            [ReadOnly] public float Et;
            [ReadOnly] public float PreClipTime;

            private void Execute(OptimizedSkeletonAspect skeleton, in ClipBlobData clipBlobData,
                in AnimationStateData stateData, ref DynamicBuffer<AnimationEventData> buffer)
            {
                skeleton.ForceInitialize();
                if (!stateData.Blending)
                {
                    ref var clip = ref clipBlobData.Blob.Value.clips[stateData.ClipAIndex];
                    var clipTime = clip.LoopToClipTime((Et - stateData.ClipAStartTime) * stateData.PlaySpeed);
                    var preClipTime =
                        clip.LoopToClipTime((PreClipTime - stateData.ClipAStartTime) * stateData.PlaySpeed);
                    clip.SamplePose(ref skeleton, clipTime, 1f);

                    // We only assume one event happen in delta time
                    if (clip.events.TryGetEventsRange(preClipTime, clipTime, out var firstEventIndex,
                            out var count))
                    {
                        if (count > 0)
                        {
                            buffer.Add(new AnimationEventData
                            {
                                NameHash = clip.events.nameHashes[firstEventIndex],
                                Parameter = clip.events.parameters[firstEventIndex],
                            });
                        }
                    }
                    /*if (eventCount > 0)
                    {
                        var eventsIndices = clip.events.GetEventIndicesInRange(preClipTime, true, clipTime, false, 0);
                        foreach (var i in eventsIndices)
                        {
                            if (i < 0 || i >= clip.events.nameHashes.Length)
                                continue;
                           
                        }
                    }*/
                }
                else
                {
                    ref var clipA = ref clipBlobData.Blob.Value.clips[stateData.ClipAIndex];
                    ref var clipB = ref clipBlobData.Blob.Value.clips[stateData.ClipBIndex];
                    var clipATime = clipA.LoopToClipTime(stateData.PlaySpeed * (Et - stateData.ClipAStartTime));
                    var clipBTime = clipB.LoopToClipTime(stateData.PlaySpeed * (Et - stateData.ClipBStartTime));
                    clipA.SamplePose(ref skeleton, clipATime, stateData.ClipAWeight);
                    clipB.SamplePose(ref skeleton, clipBTime, stateData.ClipBWeight);
                }


                skeleton.EndSamplingAndSync();
            }
        }

        // partial struct ExposedJob : IJobEntity
        // {
        //     [NativeDisableParallelForRestriction] public BufferLookup<AnimationEventData> BufferLookup;
        //     [ReadOnly] public ComponentLookup<AnimationStateData> StateLookup;
        //     [ReadOnly] public ComponentLookup<ClipBlobData> ClipLookup;
        //     [ReadOnly] public float Et;
        //     [ReadOnly] public float LastEt;
        //
        //     private void Execute(ref LocalTransform transform, in BoneIndex boneIndex,
        //         in BoneOwningSkeletonReference skeletonRef)
        //     {
        //         // if this skeleton belongs to a root skeleton which has a Single Clip component
        //         var has = ClipLookup.HasComponent(skeletonRef.skeletonRoot);
        //         if (!has)
        //             return;
        //
        //         var state = StateLookup[skeletonRef.skeletonRoot];
        //
        //         ref var clip = ref ClipLookup[skeletonRef.skeletonRoot].Blob.Value.clips[(int)state.State];
        //         var preClipTime = clip.LoopToClipTime(LastEt);
        //         var clipTime = clip.LoopToClipTime(Et);
        //
        //         // Only root bone will invoke animation event, other bones ignore it
        //         if (boneIndex.index <= 0)
        //         {
        //             clip.events.TryGetEventsRange(preClipTime, clipTime, out var firstEventIndex, out var eventCount);
        //             if (eventCount <= 0) return;
        //             Debug.Log($"Event found : {eventCount}");
        //             Debug.Log($"First event: {clip.events.names[firstEventIndex]}");
        //             for (var i = 0; i < eventCount; i++)
        //             {
        //                 var buffer = BufferLookup[skeletonRef.skeletonRoot];
        //                 buffer.Add(new AnimationEventData
        //                 {
        //                     NameHash = clip.events.nameHashes[i],
        //                     Parameter = clip.events.parameters[i],
        //                 });
        //
        //                 Debug.Log("add");
        //             }
        //         }
        //         else
        //         {
        //             var latiosTransform = clip.SampleBone(boneIndex.index, clipTime);
        //             transform.Position = latiosTransform.position;
        //             transform.Rotation = latiosTransform.rotation;
        //             transform.Scale = latiosTransform.scale;
        //         }
        //     }
        // }
    }
}


// Single thread approach

// public partial struct SingleClipPlayerSystem2 : ISystem
// {
//     [BurstCompile]
//     public void OnUpdate(ref SystemState state)
//     {
//         float t = (float)SystemAPI.GetSingleton<GameTimeData>().ElapsedTime;
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