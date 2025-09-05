using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.Interact
{
    public class LightShieldBuffSystemAuthoring : MonoBehaviour
    {
        [Tooltip("Affect how soon it will check to add under defend buff, as well as check to remove out of range light shield buff")]
        public float defendTime = 1f;
        public List<LightShieldBuffConfig> buffConfigs;
        private class ShieldBuffAuthoringBaker : Baker<LightShieldBuffSystemAuthoring>
        {
            public override void Bake(LightShieldBuffSystemAuthoring systemAuthoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var buffer = AddBuffer<LightShieldBuffConfig>(entity);
                foreach (var config in systemAuthoring.buffConfigs)
                {
                    buffer.Add(config);
                }
                AddComponent(entity, new LightShieldBuffGeneralConfig
                {
                    DefendTime = systemAuthoring.defendTime,
                });
              
                
            }
        }
    }



    [Serializable]
    public struct LightShieldBuffConfig : IBufferElementData
    {
        public float shieldGetPhysicalDamageScale;
        public float shieldGetMagicDamageScale;
        public float selfGetPhysicalDamageScale;
        public float selfGetMagicDamageScale;
        public int maxDefendCount;
    }

    public struct LightShieldBuffGeneralConfig : IComponentData
    {
        public float DefendTime;
    }




}