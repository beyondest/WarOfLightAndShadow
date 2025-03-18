using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{
    public class HealAttributesAuthoring : MonoBehaviour
    {
        private class HealAttributesAuthoringBaker : Baker<HealAttributesAuthoring>
        {
            public override void Bake(HealAttributesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<HealStateTag>(entity);
                SetComponentEnabled<HealStateTag>(entity, false);
            }
        }
    }
    
    public struct HealStateTag : IComponentData,IEnableableComponent
    {
        
    }
}