using System;
using System.Collections.Generic;
using Latios.Authoring;
using Latios.Kinemation;
using Latios.Kinemation.Authoring;
using SparFlame.Components.General;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.General.Animation
{
    // public class AnimationBake

    
    public class AnimationGeneralAuthoring : MonoBehaviour
    {
        public List<AnimationClipPair> pairs;
        public UnitAnimationState initialState = UnitAnimationState.Idle;

        private class Baker : SmartBaker<AnimationGeneralAuthoring, AnimationClipSmartBakeItem>
        {
        }

        [TemporaryBakingType]
        public struct AnimationClipSmartBakeItem : ISmartBakeItem<AnimationGeneralAuthoring>
        {
            private SmartBlobberHandle<SkeletonClipSetBlob> _blob;

            public bool Bake(AnimationGeneralAuthoring authoring, IBaker baker)
            {
                var entity = baker.GetEntity(TransformUsageFlags.Renderable);
                baker.AddComponent<ClipBlobData>(entity);
                if (authoring.pairs.Count != Enum.GetValues(typeof(UnitAnimationState)).Length)
                    throw new ArgumentException(
                        "Animation general clip authoring wrong, list must contain all types of state clip");

                var clips = new NativeArray<SkeletonClipConfig>(authoring.pairs.Count, Allocator.Temp);
                var stateSet = new HashSet<UnitAnimationState>();
                foreach (var pair in authoring.pairs)
                {
                    if (!stateSet.Add(pair.state))
                        throw new ArgumentException(
                            $"Duplicated unit animation state, {pair.state} for go : {authoring.gameObject.name}");

                    var clip = pair.clip;
                    var events = clip.ExtractKinemationClipEvents(Allocator.Temp);
                    clips[(int)pair.state] = new SkeletonClipConfig
                    {
                        clip = clip,
                        settings = SkeletonClipCompressionSettings.kDefaultSettings,
                        events = events.Length == 0 ? default : events
                    };
                }
                baker.AddBuffer<AnimationEventData>(entity);
                baker.AddComponent(entity, new AnimationStateData
                {
                    State = authoring.initialState,
                    PlaySpeed = 1f,
                    Blending = false,
                    ClipBStartTime = 0,
                    ClipBWeight = 0,
                    ClipAStartTime = 0,
                    ClipAWeight = 1,
                    ClipBIndex = 0,
                    ClipAIndex = (int)authoring.initialState
                });
                _blob = baker.RequestCreateBlobAsset(baker.GetComponent<Animator>(), clips);
                return true;
            }

            public void PostProcessBlobRequests(EntityManager entityManager, Entity entity)
            {
                entityManager.SetComponentData(entity, new ClipBlobData { Blob = _blob.Resolve(entityManager) });
            }
        }

        [Serializable]
        public struct AnimationClipPair
        {
            public AnimationClip clip;
            public UnitAnimationState state;
        }
    }
}