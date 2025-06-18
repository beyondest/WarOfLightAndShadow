using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.Interact
{
    public class DarkClericBuffSystemAuthoring : MonoBehaviour
    {
        public List<DarkClericBuffConfig> darkClericBuffConfigs;
        private class DarkClericBuffSystemAuthoringBaker : Baker<DarkClericBuffSystemAuthoring>
        {
            public override void Bake(DarkClericBuffSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var buffer = AddBuffer<DarkClericBuffConfig>(entity);
                foreach (var config in authoring.darkClericBuffConfigs)
                {
                    buffer.Add(config);
                }
            }
        }
    }

    [Serializable]
    public struct DarkClericBuffConfig : IBufferElementData
    {
        public float attackAmountBonusScale;
        public float lastTime;
    }


}