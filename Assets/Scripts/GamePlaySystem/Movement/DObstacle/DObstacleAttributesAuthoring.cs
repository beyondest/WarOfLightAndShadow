using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Movement
{
    public class DynamicObstacleAttributesAuthoring : MonoBehaviour
    {
        class Baker : Baker<DynamicObstacleAttributesAuthoring>
        {
            public override void Bake(DynamicObstacleAttributesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<DObstacleTag>(entity);
            }
        }
    }

    public struct DObstacleTag : IComponentData
    {
    }
}