using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{
    public class HarvestAuthoring : MonoBehaviour
    {
        [Header("HarvestingAbility")]
        public float harvestingRange = 1f;
        public float harvestingAmount = 1f;
        public float harvestingSpeed = 1f;
        public int harvestingCount = 1;


        private class HarvestAuthoringBaker : Baker<HarvestAuthoring>
        {
            public override void Bake(HarvestAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<HarvestStateTag>(entity);
                SetComponentEnabled<HarvestStateTag>(entity, false);
                AddComponent(entity, new HarvestAbility
                {
                    HarvestBasocAmount = authoring.harvestingAmount,
                    HarvestSpeed = authoring.harvestingSpeed,
                    HarvestRangeSq = authoring.harvestingRange * authoring.harvestingRange,
                    HarvestCount = authoring.harvestingCount,
                });
            }
        }
    }
  

    
}