using Latios.Authoring;
using Latios.Kinemation;
using Latios.Kinemation.Authoring;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Animation
{


    [DisallowMultipleComponent]
    public class AnimationClipAuthoring : MonoBehaviour
    {
        public AnimationClip clip;

        private int _eventCount;


        private class Baker : SmartBaker<AnimationClipAuthoring, SingleClipSmartBakeItem>
        {

        }

        [TemporaryBakingType]
        private struct SingleClipSmartBakeItem : ISmartBakeItem<AnimationClipAuthoring>
        {
            private SmartBlobberHandle<SkeletonClipSetBlob> _blob;

            public bool Bake(AnimationClipAuthoring authoring, IBaker baker)
            {
                var entity = baker.GetEntity(TransformUsageFlags.Dynamic);
                baker.AddComponent<SingleClip>(entity);

                var clips = new NativeArray<SkeletonClipConfig>(1, Allocator.Temp);
                var events = authoring.clip.ExtractKinemationClipEvents(Allocator.Temp);
                if (events.Length == 0)
                {
                    clips[0] = new SkeletonClipConfig
                    {
                        clip = authoring.clip,
                        settings = SkeletonClipCompressionSettings.kDefaultSettings,
                    };
                }
                else
                {
                    clips[0] = new SkeletonClipConfig
                    {
                        clip = authoring.clip,
                        settings = SkeletonClipCompressionSettings.kDefaultSettings,
                        events = events
                    };
                    baker.AddBuffer<AnimationEventRequest>(entity);
                }

                _blob = baker.RequestCreateBlobAsset(baker.GetComponent<Animator>(), clips);
                return true;
            }

            public void PostProcessBlobRequests(EntityManager entityManager, Entity entity)
            {
                entityManager.SetComponentData(entity, new SingleClip { Blob = _blob.Resolve(entityManager) });
            }
        }

    }


    public struct SingleClip : IComponentData
    {
        public BlobAssetReference<SkeletonClipSetBlob> Blob;
    }

    public struct AnimationEventRequest : IBufferElementData
    {
        public int NameHash;
        public int Parameter;
    }
}