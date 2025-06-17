using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{
    public class DarkCavalryBuffSystemAuthoring : MonoBehaviour
    {
        public List<DarkCavalryBuffConfig> darkCavalryBuffConfigs;
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
            }
        }
    }
    
    [Serializable]
    public struct DarkCavalryBuffConfig : IBufferElementData
    {
        public float attackAmountBonusWhenFullLossHp;
    }

    public struct DarkCavalryBuff : IComponentData
    {
    }
}