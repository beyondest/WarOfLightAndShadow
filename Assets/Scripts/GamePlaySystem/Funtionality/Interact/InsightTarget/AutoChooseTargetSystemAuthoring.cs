using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace SparFlame.GamePlaySystem.Interact
{
    public class AutoChooseTargetSystemAuthoring : MonoBehaviour
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


        private class AutoChooseTargetSystemAuthoringBaker : Baker<AutoChooseTargetSystemAuthoring>
        {
            public override void Bake(AutoChooseTargetSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new AutoChooseTargetSystemConfig
                {
                    HealAboveAttack = authoring.healAboveAttack,
                    HarvestAboveAttack = authoring.harvestAboveAttack,
                    BaseLineDistanceSq = authoring.baseLineDistanceSq,
                    StatValueChangeMultiplier = authoring.statValueChangeMultiplier,
                });
            }
        }
    }

    public struct AutoChooseTargetSystemConfig : IComponentData
    {
        public float HealAboveAttack;
        public float HarvestAboveAttack;
        public float BaseLineDistanceSq;
        public float StatValueChangeMultiplier;
    }
}