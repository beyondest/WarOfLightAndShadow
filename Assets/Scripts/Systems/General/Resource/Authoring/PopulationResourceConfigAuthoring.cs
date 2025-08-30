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
        public int lightInitPopulation;
        public int darkInitPopulation;


        private class ResourceSystemAuthoringBaker : Baker<PopulationResourceConfigAuthoring>
        {
             public override void Bake(PopulationResourceConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
          
                AddComponent(entity, new PopulationResourceConfig
                {
                   PopulationResourceType = authoring.populationResourceType,
                   LightInitPopulation = authoring.lightInitPopulation,
                   DarkInitPopulation = authoring.darkInitPopulation
                });
            }
        }
    }


  
  

    
    public struct PopulationResourceConfig : IComponentData
    {
        public ResourceType PopulationResourceType;
        public int LightInitPopulation;
        public int DarkInitPopulation;

    }


 
}