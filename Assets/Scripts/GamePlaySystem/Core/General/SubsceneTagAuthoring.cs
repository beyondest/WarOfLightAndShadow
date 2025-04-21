using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.General
{
    public class SubsceneTagAuthoring : MonoBehaviour
    { 
        class Baker : Baker<SubsceneTagAuthoring>
        {
            public override void Bake(SubsceneTagAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<SubsceneTag>(entity);
            }
        }
    }

    public struct SubsceneTag : IComponentData
    {
        
    }
}