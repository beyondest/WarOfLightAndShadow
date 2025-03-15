using Latios.Kinemation;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Latios.Transforms;
using Latios.Transforms.Systems;
using Unity.Collections;
using UnityEngine;
using static Unity.Entities.SystemAPI;

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


public partial struct SingleClipPlayerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        new ExposedJob
            {
                ClipLookup = GetComponentLookup<SingleClip>(true),
                Et = (float)SystemAPI.Time.ElapsedTime
            }
            .ScheduleParallel();
        new OptimizedJob
        {
            Et = (float)SystemAPI.Time.ElapsedTime
        }.ScheduleParallel();
    }

    [BurstCompile]
    partial struct OptimizedJob : IJobEntity
    {
        [ReadOnly]public float Et;

        private void Execute(OptimizedSkeletonAspect skeleton, in SingleClip singleClip)
        {
            ref var clip     = ref singleClip.blob.Value.clips[0];
            var     clipTime = clip.LoopToClipTime(Et);

            clip.SamplePose(ref skeleton, clipTime, 1f);
            skeleton.EndSamplingAndSync();
        }
    }
    
    partial struct ExposedJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<SingleClip> ClipLookup;
        public float Et;
    
        private void Execute(ref LocalTransform transform, in BoneIndex boneIndex,
            in BoneOwningSkeletonReference skeletonRef)
        {
            var has = ClipLookup.HasComponent(skeletonRef.skeletonRoot);
            if (boneIndex.index <= 0 || !has)
                return;
    
            ref var clip = ref ClipLookup[skeletonRef.skeletonRoot].blob.Value.clips[0];
            var clipTime = clip.LoopToClipTime(Et);
    
            var transformQvvs = clip.SampleBone(boneIndex.index, clipTime);
            transform.Position = transformQvvs.position;
            transform.Rotation = transformQvvs.rotation;
            transform.Scale = transformQvvs.scale;
        }
    }
}