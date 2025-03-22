using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Movement
{
    public class SqueezeSystemAuthoring : MonoBehaviour
    {
        private class SqueezeSystemAuthoringBaker : Baker<SqueezeSystemAuthoring>
        {
            public override void Bake(SqueezeSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new SqueezeSystemConfig
                {
                    
                });
            }
        }
    }

    public struct SqueezeSystemConfig : IComponentData
    {
        
    }
}