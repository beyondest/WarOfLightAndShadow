using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{
    public class LightCavalryBuffSystemAuthoring : MonoBehaviour
    {
        public List<LightCavalryBuffConfig> lightCavalryBuffConfigs;
        public LightCavalryBuffGeneralConfig lightCavalryBuffGeneralConfig;
        private class LightCavalryBuffSystemBaker : Baker<LightCavalryBuffSystemAuthoring>
        {
            public override void Bake(LightCavalryBuffSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var buffer = AddBuffer<LightCavalryBuffConfig>(entity);
                foreach (var config in authoring.lightCavalryBuffConfigs)
                {
                    buffer.Add(config);
                }
                AddComponent(entity,authoring.lightCavalryBuffGeneralConfig);
            }
        }
        
    }
    [System.Serializable]
    public struct LightCavalryBuffConfig : IBufferElementData
    {
        public int amountBonus;
        public float speedBonus;
        public float rangeBonus;
        public int targetsBonus;
        public float moveSpeedBonus;
        public int statBonus;
        
        public int maxAllyCount;
    }
    
    [Serializable]
    public struct LightCavalryBuffGeneralConfig : IComponentData
    {
        public float lastTime;
    }


    public struct LightCavalryBuff : IComponentData
    {
    }
    public struct LightCavalryUnderBonus : IComponentData,IEnableableComponent
    {
        public Entity Provider;
        public float LastTime;
    }
    
}