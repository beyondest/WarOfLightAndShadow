using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{
    public class GarrisonAuthoring : MonoBehaviour
    {
        private class GarrisonAuthoringBaker : Baker<GarrisonAuthoring>
        {
            public override void Bake(GarrisonAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<GarrisonStateTag>(entity);
                SetComponentEnabled<GarrisonStateTag>(entity, false);
            }
        }
    }
    public struct GarrisonStateTag : IComponentData, IEnableableComponent
    {
        
    }
}