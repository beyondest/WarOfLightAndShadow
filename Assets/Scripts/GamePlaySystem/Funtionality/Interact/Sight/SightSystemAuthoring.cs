using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace SparFlame.GamePlaySystem.Interact
{
    public class SightSystemAuthoring : MonoBehaviour
    {
        [Tooltip(
            "If heal above attack 20, then for a healer, wounded ally unit's priority is above 20 than enemy's in default" +
            "So usually if you want heal always before attack, this should be set extremely large")]
        public float healAboveAttack = 1e5f;

        public float harvestAboveAttack = -1e5f;

        [Tooltip("If target is 5 dis from self, baseLine is 400, then disValue = 400 - 5*5 = 375")]
        public float baseLineDistanceSq = 400f;

        [Tooltip("If damage dealt is 100 and multiplier is 1.0f, than attacker's statValue += 100 * 1.0f")]
        public float statValueChangeMultiplier = 1.0f;
        
        public float disSqMultiplier = 1f;
        private class AutoChooseTargetSystemAuthoringBaker : Baker<SightSystemAuthoring>
        {
            public override void Bake(SightSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new AutoChooseTargetSystemConfig
                {
                    HealAboveAttack = authoring.healAboveAttack,
                    HarvestAboveAttack = authoring.harvestAboveAttack,
                    BaseLineDistanceSq = authoring.baseLineDistanceSq,
                    StatValueChangeMultiplier = authoring.statValueChangeMultiplier,
                    DisSqValueMultiplier = authoring.disSqMultiplier,
                });
            }
        }
    }

    public struct AutoChooseTargetSystemConfig : IComponentData
    {
        public float HealAboveAttack;
        public float HarvestAboveAttack;
        public float BaseLineDistanceSq;
        public float DisSqValueMultiplier;
        public float StatValueChangeMultiplier;
    }

    public struct SightData : IComponentData
    {
        public Entity BelongsTo;
    }
}