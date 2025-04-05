using System;
using System.Collections.Generic;
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


    
    public struct CostList : IBufferElementData
    {
        public ResourceType Type;
        public int Amount;
    }
}