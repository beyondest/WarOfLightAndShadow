using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.Interact
{
    
    
    public class DarkShieldBuffSystemAuthoring : MonoBehaviour
    {
        public List<DarkShieldBuffConfig> reflectionDamageScalesTier;
        public float tauntedDuration = 10f;
        public float selfDuration = 1f;
        private class DarkShieldBuffBaker : Baker<DarkShieldBuffSystemAuthoring>
        {
            public override void Bake(DarkShieldBuffSystemAuthoring systemAuthoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var buffer = AddBuffer<DarkShieldBuffConfig>(entity);
                foreach (var config in systemAuthoring.reflectionDamageScalesTier)
                {
                    buffer.Add(new DarkShieldBuffConfig
                    {
                        reflectPhysicalDamageScale = config.reflectPhysicalDamageScale,
                        reflectMagicDamageScale = config.reflectMagicDamageScale,
                        maxTauntCount = config.maxTauntCount
                    });
                    
                }
                AddComponent(entity, new DarkShieldBuffGeneralConfig
                {
                    TauntedDuration = systemAuthoring.tauntedDuration,
                    SelfDuration = systemAuthoring.selfDuration,
                });
            }
        }
    }


    [Serializable]
    public struct DarkShieldBuffConfig : IBufferElementData
    {
        public float reflectPhysicalDamageScale;
        public float reflectMagicDamageScale;
        public int maxTauntCount;
    }

    public struct DarkShieldBuffGeneralConfig : IComponentData
    {
        public float TauntedDuration;
        public float SelfDuration;
    }
   
}