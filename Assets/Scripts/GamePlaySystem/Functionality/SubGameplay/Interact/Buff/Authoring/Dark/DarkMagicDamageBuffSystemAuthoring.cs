using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{
    public class DarkMagicDamageBuffSystemAuthoring : MonoBehaviour
    {
        public List<DarkMagicDamageBuffConfig> darkMagicDamageBuffConfigs;
        private class DarkMagicDamageBuffSystemAuthoringBaker : Baker<DarkMagicDamageBuffSystemAuthoring>
        {
            public override void Bake(DarkMagicDamageBuffSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var buffer = AddBuffer<DarkMagicDamageBuffConfig>(entity);
                foreach (var config in authoring.darkMagicDamageBuffConfigs)
                {
                    buffer.Add(config);
                }
            }
        }
    }
    
    [Serializable]
    public struct DarkMagicDamageBuffConfig : IBufferElementData
    {
        public float healingReductionPercent;
        public float lastTime;
    }
    
    public struct DarkMagicDamageBuff : IComponentData, IEnableableComponent
    {
        public float HealingReductionPercent;
        public float LastTime;
    }
}