using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.Interact
{
    public class BlessingBuffSystemAuthoring : MonoBehaviour
    {
        public List<BlessingBuffConfig> lightCavalryBuffConfigs;
        public float blessingBuffDuration;
        public float underBlessingBonusDuration;
        [AssetsOnly] public List<GameObject> blessingAoePrefabs;
        private class BlessingBuffSystemBaker : Baker<BlessingBuffSystemAuthoring>
        {
            public override void Bake(BlessingBuffSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var buffer = AddBuffer<BlessingBuffConfig>(entity);
                foreach (var config in authoring.lightCavalryBuffConfigs)
                {
                    buffer.Add(config);
                }

                var buffer2 = AddBuffer<BlessingBuffAoeTriggerPrefabs>(entity);
                foreach (var prefab in authoring.blessingAoePrefabs)
                {
                    buffer2.Add(new BlessingBuffAoeTriggerPrefabs
                    {
                        Prefab = GetEntity(prefab, TransformUsageFlags.Dynamic)
                    });
                }
                AddComponent(entity, new BlessingBuffGeneralConfig
                {
                    UnderBlessingBonusDuration = authoring.underBlessingBonusDuration,
                    BlessingBuffDuration = authoring.blessingBuffDuration,
                });
            }
        }
    }
    [Serializable]
    public struct BlessingBuffConfig : IBufferElementData
    {
        public int amountBonus;
        public float speedBonus;
        public float rangeBonus;
        public int targetsBonus;
        public float moveSpeedBonus;
        public int maxAllyCount;
    }

    public struct BlessingBuffGeneralConfig : IComponentData
    {
        public float UnderBlessingBonusDuration;
        public float BlessingBuffDuration;
    }

    public struct BlessingBuffAoeTriggerPrefabs : IBufferElementData
    {
        public Entity Prefab;
    }
}