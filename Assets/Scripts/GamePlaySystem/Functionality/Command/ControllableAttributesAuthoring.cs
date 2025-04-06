using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Command
{
    public class ControllableAttributesAuthoring : MonoBehaviour
    {
        private class ControllableAttributesAuthoringBaker : Baker<ControllableAttributesAuthoring>
        {
            public override void Bake(ControllableAttributesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<Controllable>(entity);
                SetComponentEnabled<Controllable>(entity, true);
            }
        }
    }
    public struct Controllable : IComponentData, IEnableableComponent
    {
        
    }
}