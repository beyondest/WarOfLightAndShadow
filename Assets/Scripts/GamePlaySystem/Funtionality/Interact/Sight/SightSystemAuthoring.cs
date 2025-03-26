using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace SparFlame.GamePlaySystem.Interact
{
    public class SightSystemAuthoring : MonoBehaviour
    {
        [Header("Internal Config")]

        [Tooltip("If target is 5 dis from self, baseLine is 400, then disValue = 400 - 5*5 = 375")]
        public float baseLineDistanceSq = 400f;

        [Tooltip("If damage dealt is 100 and multiplier is 1.0f, than attacker's statValue += 100 * 1.0f")]
        public float statValueChangeMultiplier = 1.0f;
        
        public float disSqMultiplier = 1f;
        
        [Header("Player Config")]
        
        [Tooltip("this value will be added to memory target when it begins to attack stuck building")]
        public float memoryTargetAfterStuckByBuilding = 10000f;
        [Tooltip("this value will be added to memory target when it change target due to any reason")]
        public float memoryTargetWhenFocus = 10000f;

        [Tooltip(
            "If heal above attack 20, then for a healer, wounded ally unit's priority is above 20 than enemy's in default" +
            "So usually if you want heal always before attack, this should be set extremely large")]
        public float healAboveAttack = 1e5f;
        public float harvestAboveAttack = -1e5f;

        public bool dynamicChooseTargetInInteract = true;
        public bool healerHealSelfFirst = true;

        
        private class AutoChooseTargetSystemAuthoringBaker : Baker<SightSystemAuthoring>
        {
            public override void Bake(SightSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new SightSystemConfig
                {
                    HealAboveAttack = authoring.healAboveAttack,
                    HarvestAboveAttack = authoring.harvestAboveAttack,
                    BaseLineDistanceSq = authoring.baseLineDistanceSq,
                    StatValueChangeMultiplier = authoring.statValueChangeMultiplier,
                    DisSqValueMultiplier = authoring.disSqMultiplier,
                    MemoryTargetAfterStuckByBuilding = authoring.memoryTargetAfterStuckByBuilding,
                    MemoryTargetWhenFocus = authoring.memoryTargetWhenFocus,
                    DynamicChooseTargetInInteract = authoring.dynamicChooseTargetInInteract,
                    HealerAlwaysHealSelfFirst = authoring.healerHealSelfFirst,
                });
            }
        }
    }

    public struct SightSystemConfig : IComponentData
    {
        public float HealAboveAttack;
        public float HarvestAboveAttack;
        public float BaseLineDistanceSq;
        public float DisSqValueMultiplier;
        public float StatValueChangeMultiplier;
        public float MemoryTargetWhenFocus;
        public float MemoryTargetAfterStuckByBuilding;
        public bool DynamicChooseTargetInInteract;
        public bool HealerAlwaysHealSelfFirst;
    }

    public struct SightData : IComponentData
    {
        public Entity BelongsTo;
    }
}