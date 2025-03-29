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
        Essence, 
        
        // Summon
        LightEnergy, 
        DarkEnergy,
        
        // Building
        Luminite,
        Obsidian
    }

    public struct CostList : IBufferElementData
    {
        public ResourceType Type;
        public int Amount;
    }
}