using System;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.General.Animation
{
    public class AnimationPlaySystemAuthoring : MonoBehaviour
    {
        public AnimationPlayConfig config;
        private class Baker : Baker<AnimationPlaySystemAuthoring>
        {
            public override void Bake(AnimationPlaySystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new AnimationPlayData
                {
                    
                });
                AddComponent(entity, authoring.config);
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