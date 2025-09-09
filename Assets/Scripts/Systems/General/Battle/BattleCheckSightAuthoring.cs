using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.General.Battle
{
    public class BattleCheckSightAuthoring : MonoBehaviour
    {
        private class BattleCheckSightAuthoringBaker : Baker<BattleCheckSightAuthoring>
        {
            public override void Bake(BattleCheckSightAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddBuffer<BattleCheckSightTarget>(entity);
                AddComponent<BattleCheckSightData>(entity);
            }
        }
    }

    public struct BattleCheckSightTarget : IBufferElementData
    {
        public Entity Entity;
    }
    public struct BattleCheckSightData : IComponentData
    {
        public Entity BelongsTo;
    }
}