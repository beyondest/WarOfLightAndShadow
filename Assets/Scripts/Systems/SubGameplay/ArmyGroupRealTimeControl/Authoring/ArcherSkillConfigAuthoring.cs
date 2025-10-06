using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;
using Range = SparFlame.Core.Structs.Range;

namespace Systems.SubGameplay.ArmyGroupRealTimeControl
{
    public class ArcherSkillSettingSystemAuthoring : MonoBehaviour
    {
        [Serializable]
        public class Config
        {
            [AssetsOnly]
            public GameObject indicatorPrefab;
            public Range damageRange;
        }

        public List<Config> configs;
        public int preTierUnitCountForArrowRain = 1;
        public float arrowFlightSpeed = 40f;
        public float triggerTimeBias = 0.5f;
        private class ArcherSkillSettingSystemAuthoringBaker : Baker<ArcherSkillSettingSystemAuthoring>
        {
            public override void Bake(ArcherSkillSettingSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                var buffer = AddBuffer<ArcherSKillConfigs>(entity);
                foreach (var config in authoring.configs)
                {
                    buffer.Add(new ArcherSKillConfigs
                    {
                        IndicatorPrefab = GetEntity(config.indicatorPrefab, TransformUsageFlags.Dynamic),
                        DamageRange = config.damageRange
                    });
                }
                AddComponent(entity, new ArcherSkillGeneralConfig
                {
                    ArrowRainPreTierUnitCount = authoring.preTierUnitCountForArrowRain,
                    ArrowFlightSpeed = authoring.arrowFlightSpeed,
                    TriggerTimeBias = authoring.triggerTimeBias
                });
            }
        }
    }

    public struct ArcherSKillConfigs : IBufferElementData
    {
        public Entity IndicatorPrefab;
        public Range DamageRange;
    }

    public struct ArcherSkillGeneralConfig : IComponentData
    {
        public int ArrowRainPreTierUnitCount;
        public float ArrowFlightSpeed; // Should equal to arrow projectile flight speed
        public float TriggerTimeBias;
    }
}