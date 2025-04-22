using System;
using System.Collections.Generic;
using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Resource
{
    public class ResourceManageSystemAuthoring : MonoBehaviour
    {

        private class ResourceSystemAuthoringBaker : Baker<ResourceManageSystemAuthoring>
        {
            public override void Bake(ResourceManageSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new ResourceManageSystemConfig
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
        Obsidian = 4,
        
        // Population
        Population = 5,
    }

    public enum ResourceRequestType
    {
        Harvest = 0,
        Generate = 1,
        Consume = 2,
        Release = 3
    }

    /// TODO Change construction use this request too
    public struct ResourceChangeRequest : IComponentData
    {
        public ResourceType Type;
        public FactionTag FromFaction;
        /// <summary>
        /// This value must be positive
        /// </summary>
        public int Amount;
        public ResourceRequestType RequestType;
    }

    


    public struct ResourceManageSystemConfig : IComponentData
    {
        
    }


 
}