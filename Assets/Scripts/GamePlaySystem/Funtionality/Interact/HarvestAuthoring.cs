using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{
    public class HarvestAuthoring : MonoBehaviour
    {
        private class HarvestAuthoringBaker : Baker<HarvestAuthoring>
        {
            public override void Bake(HarvestAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<HarvestStateTag>(entity);
                SetComponentEnabled<HarvestStateTag>(entity, false);
            }
        }
    }
    public struct HarvestStateTag : IComponentData, IEnableableComponent
    {
        
    }
    
}