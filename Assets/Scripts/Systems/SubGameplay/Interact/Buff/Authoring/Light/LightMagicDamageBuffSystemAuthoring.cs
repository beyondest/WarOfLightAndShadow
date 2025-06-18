using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.Interact
{
    public class LightMagicDamageBuffSystemAuthoring : MonoBehaviour
    {
        public List<LightMagicDamageBuffConfig> lightMagicDamageBuffConfigs;
        private class
            LightMagicDamageBuffSystemAuthoringBaker : Baker<LightMagicDamageBuffSystemAuthoring>
        {
            public override void Bake(LightMagicDamageBuffSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var buffer = AddBuffer<LightMagicDamageBuffConfig>(entity);
                foreach (var config in authoring.lightMagicDamageBuffConfigs)
                {
                    buffer.Add(config);
                }
            }
        }
    }
    
    [Serializable]
    public struct LightMagicDamageBuffConfig : IBufferElementData
    {
        public float speedNegativeBonus;
        public float moveSpeedNegativeBonus;
        public float lastTime;
    }

}