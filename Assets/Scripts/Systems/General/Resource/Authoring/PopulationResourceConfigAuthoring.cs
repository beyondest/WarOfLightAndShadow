using System.Collections.Generic;
using SparFlame.Components.General;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.General.Resource
{
    public class PopulationResourceConfigAuthoring : MonoBehaviour
    {
        public ResourceType populationResourceType = ResourceType.SoulPact;


        private class ResourceSystemAuthoringBaker : Baker<PopulationResourceConfigAuthoring>
        {
             public override void Bake(PopulationResourceConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
          
                AddComponent(entity, new PopulationResourceType
                {
                   Value = authoring.populationResourceType,
                });
            }
        }
    }


  
  

    



 
}