using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{
    public class DarkWorkerBuffSystemAuthoring : MonoBehaviour
    {
        public List<DarkWorkerBuffConfig> configs;
        private class DarkWorkerBuffSystemAuthoringBaker : Baker<DarkWorkerBuffSystemAuthoring>
        {
            public override void Bake(DarkWorkerBuffSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var buffer = AddBuffer<DarkWorkerBuffConfig>(entity);
                foreach (var config in authoring.configs)
                {
                    buffer.Add(config);
                }
            }
        }
    }
    [System.Serializable]
    public struct DarkWorkerBuffConfig : IBufferElementData
    {
        public float convertBonusScale;
    }
}