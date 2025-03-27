using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{
    public class AttackAttributesAuthoring : MonoBehaviour
    {
        private class AttackAttributesAuthoringBaker : Baker<AttackAttributesAuthoring>
        {
            public override void Bake(AttackAttributesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<AttackStateTag>(entity);
                SetComponentEnabled<AttackStateTag>(entity,false);
            }
        }
    }
    public struct AttackStateTag : IComponentData,IEnableableComponent
    {
        
    }
}