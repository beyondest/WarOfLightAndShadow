using Unity.Entities;
using UnityEngine;

namespace Systems.SubGameplay.ArmyGroupRealTimeControl
{
    public class EnemyArrowRainTargetAuthoring : MonoBehaviour
    {
        private class EnemyArrowRainTargetAuthoringBaker : Baker<EnemyArrowRainTargetAuthoring>
        {
            public override void Bake(EnemyArrowRainTargetAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<EnemyArrowRainTarget>(entity);
            }
        }
    }

    public struct EnemyArrowRainTarget : IComponentData
    {
        
    }
}