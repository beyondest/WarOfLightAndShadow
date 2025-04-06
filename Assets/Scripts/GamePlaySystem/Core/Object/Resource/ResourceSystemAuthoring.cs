using System;
using System.Collections.Generic;
using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Resource
{
    public class ResourceSystemAuthoring : MonoBehaviour
    {

        private class ResourceSystemAuthoringBaker : Unity.Entities.Baker<ResourceSystemAuthoring>
        {
            public override void Bake(ResourceSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new ResourceSystemConfig
                {
                    
                });
            }
        }
    }


    public enum ResourceType
    {
        // Total
        Essence = 0, 
        
        // Summon
        LightEnergy = 1, 
        DarkEnergy = 2,
        
        // Building
        Luminite = 3,
        Obsidian = 4
    }


    public struct HarvestResourceRequest : IComponentData
    {
        public ResourceType Type;
        public FactionTag FromFaction;
        /// <summary>
        /// This value must be positive
        /// </summary>
        public int HarvestAmount;
    }

    public struct ResourceSystemConfig : IComponentData
    {
        
    }


 
}