using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{
    public class LightClericBuffSystemAuthoring : MonoBehaviour
    {
        public List<LightClericBuffConfig> lightClericBuffConfigs;
        private class LightClericBuffSystemAuthoringBaker : Baker<LightClericBuffSystemAuthoring>
        {
            public override void Bake(LightClericBuffSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var buffer = AddBuffer<LightClericBuffConfig>(entity);
                foreach (var config in authoring.lightClericBuffConfigs)
                {
                    buffer.Add(config);
                }
            }
        }
    }
    [Serializable]
    public struct LightClericBuffConfig : IBufferElementData
    {
        public float healingBonusScale;
    }
    
    public struct LightClericBuff : IComponentData
    {
    }
   
}