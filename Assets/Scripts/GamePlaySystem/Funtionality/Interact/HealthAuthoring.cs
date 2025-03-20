using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{
    public class HealthAuthoring : MonoBehaviour
    {
        private class GarrisonAuthoringBaker : Baker<HealthAuthoring>
        {
            public override void Bake(HealthAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new HealthData
                {
                    
                });
            }
        }
    }

    public struct HealthData : IComponentData
    {
        public int Value;
        
    }

}