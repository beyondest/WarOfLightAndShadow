using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.Interact
{
    public class DarkCavalryBuffSystemAuthoring : MonoBehaviour
    {
        public List<DarkCavalryBuffConfig> darkCavalryBuffConfigs;
        public float selfDuration = 15f;
        private class DarkCavalryBuffSystemAuthoringBaker : Baker<DarkCavalryBuffSystemAuthoring>
        {
            public override void Bake(DarkCavalryBuffSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var buffer = AddBuffer<DarkCavalryBuffConfig>(entity);
                foreach (var config in authoring.darkCavalryBuffConfigs)
                {
                    buffer.Add(config);
                }
                AddComponent(entity, new DarkCavalryBuffGeneralConfig {selfDuration = authoring.selfDuration});
            }
        }
    }
    
    [Serializable]
    public struct DarkCavalryBuffConfig : IBufferElementData
    {
        public float attackAmountBonusWhenFullLossHp;
    }

    [Serializable]
    public struct DarkCavalryBuffGeneralConfig : IComponentData
    {
        public float selfDuration;
    }

 
}