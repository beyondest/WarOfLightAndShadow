using System;
using System.Collections.Generic;
using SparFlame.Components.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.General.Animation
{
    public class AnimationPlaySystemAuthoring : MonoBehaviour
    {
        public AnimationEventTriggerModelIndex triggerModelIndex;
        public AnimationPlayConfig config;
        public List<AnimationModelIndices> configs;
        private class Baker : Baker<AnimationPlaySystemAuthoring>
        {
            public override void Bake(AnimationPlaySystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new AnimationPlayData
                {
                    LastEt = 0
                });
                AddComponent(entity, authoring.config);
                var buffer = AddBuffer<AnimationModelIndices>(entity);
                foreach (var config in authoring.configs)
                {
                    buffer.Add(config);
                }
                AddComponent(entity, authoring.triggerModelIndex);
            }
        }
    }

    public struct AnimationPlayData : IComponentData
    {
        // Only for check animation events
        public float LastEt;
    }

    [Serializable]
    public struct AnimationPlayConfig : IComponentData
    {
        public float blendDuration;
    }


    
}