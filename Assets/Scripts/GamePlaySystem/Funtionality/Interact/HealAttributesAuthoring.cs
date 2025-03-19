using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{
    public class HealAttributesAuthoring : MonoBehaviour
    {
                
        [Header("HealingAbility")]
        public float healingRange = 3f;
        public float healingAmount = 1f;
        public float healingSpeed = 1f;
        public int healingCount = 1;

        private class HealAttributesAuthoringBaker : Baker<HealAttributesAuthoring>
        {
            public override void Bake(HealAttributesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<HealStateTag>(entity);
                SetComponentEnabled<HealStateTag>(entity, false);
                AddComponent(entity, new HealingAbility
                {
                    HealingBasicAmount = authoring.healingAmount,
                    HealingSpeed = authoring.healingSpeed,
                    HealingCount = authoring.healingCount,
                    HealingRangeSq = authoring.healingRange * authoring.healingRange,
                });
            }
        }
    }
    

}