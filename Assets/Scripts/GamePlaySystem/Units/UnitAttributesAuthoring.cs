using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using BoxCollider = UnityEngine.BoxCollider;

namespace SparFlame.GamePlaySystem.Units
{
    public class UnitAttributesAuthoring : MonoBehaviour
    {
        
        [Header("General")]
        
        [Tooltip("Notice : attackRange not only influence attack abilities, but also influence the march positioning" +
                 "So even if a unit can only heal, its attackRange should be set carefully so that it will not get" +
                 "too closed to enemy")]
        public float attackRange = 1f;
        public float moveSpeed = 5f;
        public float attackSpeed = 1f;
        public int attackCount = 1;
        
        [Header("HealingAbility")]
        public bool healingAbility = false;
        public float healingRange = 3f;
        public float healingAmount = 1f;
        public float healingSpeed = 1f;
        public int healingCount = 1;
        
        
        [Header("HarvestingAbility")]
        public bool harvestAbility = false;
        public float harvestingRange = 1f;
        public float harvestingAmount = 1f;
        public float harvestingSpeed = 1f;
        public int harvestingCount = 1;
        
        
        class UnitAttributesAuthoringBaker : Baker<UnitAttributesAuthoring>
        {
            public override void Bake(UnitAttributesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var boxCollider = authoring.GetComponent<BoxCollider>();
                AddComponent(entity, new UnitAttr
                {
                    BoxColliderSize = boxCollider.size,
                    AttackRangeSq = authoring.attackRange * authoring.attackRange,
                    MoveSpeed = authoring.moveSpeed,
                    AttackCount = authoring.attackCount,
                    AttackSpeed = authoring.attackSpeed
                });
                if (authoring.healingAbility)
                    AddComponent(entity, new HealingAbility
                    {
                        HealingAmount = authoring.healingAmount,
                        HealingSpeed = authoring.healingSpeed,
                        HealingCount = authoring.healingCount,
                        HealingRangeSq = authoring.healingRange * authoring.healingRange,
                    });
                if (authoring.harvestAbility)
                    AddComponent(entity, new HarvestAbility
                    {
                        HarvestingAmount = authoring.harvestingAmount,
                        HarvestingSpeed = authoring.harvestingSpeed,
                        HarvestingRangeSq = authoring.harvestingRange * authoring.harvestingRange,
                        HarvestingCount = authoring.harvestingCount,
                    });
            }
        }
    }

    public struct UnitAttr : IComponentData
    {
        public float3 BoxColliderSize;
        public float AttackRangeSq;
        public float MoveSpeed;
        public float AttackSpeed;
        public float AttackCount;
    }

    public struct HealingAbility : IComponentData
    {
        public float HealingRangeSq ;
        public float HealingAmount ;
        public float HealingSpeed ;
        public int HealingCount ;
    }

    public struct HarvestAbility : IComponentData
    {
        public float HarvestingRangeSq;
        public float HarvestingAmount;
        public float HarvestingSpeed ;
        public int HarvestingCount ;
    }
    
}
