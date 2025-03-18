using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Animation
{
    public class AnimationPlaySystemAuthoring : MonoBehaviour
    {
        
        private class Baker : Baker<AnimationPlaySystemAuthoring>
        {
            public override void Bake(AnimationPlaySystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new AnimationPlayData
                {
                    
                });
            }
        }
    }

    public struct AnimationPlayData : IComponentData
    {
        public float LastEt;
    }
    
}