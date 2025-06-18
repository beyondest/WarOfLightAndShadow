using System;
using System.Collections.Generic;
using SparFlame.Components.SubGameplay;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.Interact
{
    public class ExpSystemAuthoring : MonoBehaviour
    {
        public List<ExpSystemConfig> configs;
        class Baker : Baker<ExpSystemAuthoring>
        {
            public override void Bake(ExpSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var buffer = AddBuffer<ExpSystemConfig>(entity);
                foreach (var config in authoring.configs)
                {
                    buffer.Add(config);
                }
            }
        }
    }


    [Serializable]
    public struct ExpSystemConfig : IBufferElementData
    {
        public ExpGainType type;
        public int gainAmount;
    }

  

 

 
}