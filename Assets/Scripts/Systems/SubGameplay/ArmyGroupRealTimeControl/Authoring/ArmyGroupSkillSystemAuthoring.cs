using SparFlame.Components.MainGameplay;
using Unity.Entities;
using UnityEngine;

namespace Systems.SubGameplay.ArmyGroupRealTimeControl
{
    public class ArmyGroupSkillSystemAuthoring : MonoBehaviour
    {
        public float armyGroupSkillChargeCoolDown = 5.0f;
        private class ArmyGroupSkillSystemAuthoringBaker : Baker<ArmyGroupSkillSystemAuthoring>
        {
            public override void Bake(ArmyGroupSkillSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new ArmyGroupSkillConfig
                {
                    maxChargeCoolDown = authoring.armyGroupSkillChargeCoolDown,
                });
            }
        }
    }
}