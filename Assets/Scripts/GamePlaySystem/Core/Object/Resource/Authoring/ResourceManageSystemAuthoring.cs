using System;
using System.Collections.Generic;
using SparFlame.GamePlaySystem.General;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Resource
{
    public class ResourceManageSystemAuthoring : MonoBehaviour
    {
        public List<ResourceType> populationResourceTypes = new();
        private class ResourceSystemAuthoringBaker : Baker<ResourceManageSystemAuthoring>
        {
            public override void Bake(ResourceManageSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var popTypes = new FixedList64Bytes<ResourceType>();
                foreach (var type in authoring.populationResourceTypes)
                {
                    popTypes.Add(type);
                }
                AddComponent(entity, new ResourceManageSystemConfig
                {
                    PopulationResourceTypes = popTypes
                });
            }
        }
    }


  
    public enum ResourceRequestType
    {
        Harvest = 0,
        Generate = 1,
        Consume = 2,
        Release = 3,
        DwellingDestroyConsume = 4
    }

    public struct ResourceChangeRequest : IComponentData
    {
        public ResourceType Type;
        public FactionTag FromFaction;
        /// <summary>
        /// This value must be positive
        /// </summary>
        public int AbsAmount;
        public ResourceRequestType RequestType;
    }

    


    
    public struct ResourceManageSystemConfig : IComponentData
    {
        public FixedList64Bytes<ResourceType> PopulationResourceTypes;
    }


 
}