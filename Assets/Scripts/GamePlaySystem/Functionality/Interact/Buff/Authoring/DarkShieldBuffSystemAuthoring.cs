using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{
    
    
    public class DarkShieldBuffSystemAuthoring : MonoBehaviour
    {
        public List<DarkShieldBuffConfig> reflectionDamageScalesTier;
        public float darkShieldReflectDamageDuration = 1f;
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
                        maxTauntCount = config.maxTauntCount
                    });
                    
                }
                AddComponent(entity, new DarkShieldBuffGeneralConfig
                {
                    DarkShieldReflectDamageDuration = systemAuthoring.darkShieldReflectDamageDuration
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
        public float DarkShieldReflectDamageDuration;
    }
    public struct DarkShieldTauntBuff : IComponentData
    {
        public float ReflectPhysicalDamageScale;
        public float ReflectMagicDamageScale;
        public int MaxTauntCount;
    }


    public struct DarkShieldTauntedBuff : IComponentData,IEnableableComponent
    {
        public float TauntTime;
        public Entity TauntedBy;
    }
    
}