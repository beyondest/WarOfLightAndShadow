using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.Interact
{
    public class DarkArcherBuffSystemAuthoring : MonoBehaviour
    {
        public List<DarkArcherBuffConfig> darkArcherBuffConfigs;
        private class DarkArcherBuffSystemAuthoringBaker : Baker<DarkArcherBuffSystemAuthoring>
        {
            public override void Bake(DarkArcherBuffSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var buffer = AddBuffer<DarkArcherBuffConfig>(entity);
                foreach (var config in authoring.darkArcherBuffConfigs)
                {
                    buffer.Add(config);
                }
            }
        }
    }
    [Serializable]
    public struct DarkArcherBuffConfig : IBufferElementData
    {
        public int extraArrowCount;
        public float extraArrowDamageScale;
        public float extraArrowTriggerChance;
    }
    

}