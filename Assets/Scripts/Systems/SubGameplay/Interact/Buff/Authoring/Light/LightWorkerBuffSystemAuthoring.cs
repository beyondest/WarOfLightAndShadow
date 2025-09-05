using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.Interact
{
    public class LightWorkerBuffSystemAuthoring : MonoBehaviour
    {
        public List<LightWorkerBuffConfig> configs;

        private class LightWorkerBuffSystemAuthoringBaker : Baker<LightWorkerBuffSystemAuthoring>
        {
            public override void Bake(LightWorkerBuffSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var buffer = AddBuffer<LightWorkerBuffConfig>(entity);
                foreach (var config in authoring.configs)
                {
                    buffer.Add(config);
                }
            }
        }
    }

    [Serializable]
    public struct LightWorkerBuffConfig : IBufferElementData
    {
        public float generateBonusScale;
    }
}