using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.Interact
{
    public class LightArcherBuffSystemAuthoring : MonoBehaviour
    {
        public List<LightArcherBuffConfig> lightArcherBuffConfigs;
        private class LightArcherBuffSystemAuthoringBaker : Baker<LightArcherBuffSystemAuthoring>
        {
            public override void Bake(LightArcherBuffSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var buffer = AddBuffer<LightArcherBuffConfig>(entity);
                foreach (var config in authoring.lightArcherBuffConfigs)
                {
                    buffer.Add(config);
                }
            }
        }
    }

    [Serializable]
    public struct LightArcherBuffConfig : IBufferElementData
    {
        public float bonusMaxHpScaleUnit;
        public float bonusMaxHpScaleBuilding;
        public float bonusTriggerChance;
    }
    
  
    
}