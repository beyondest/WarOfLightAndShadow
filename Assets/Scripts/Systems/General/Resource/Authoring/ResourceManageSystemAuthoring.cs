using System.Collections.Generic;
using SparFlame.Components.General;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.General.Resource
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


  
  

    
    public struct ResourceManageSystemConfig : IComponentData
    {
        public FixedList64Bytes<ResourceType> PopulationResourceTypes;
    }


 
}