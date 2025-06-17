using SparFlame.GamePlaySystem.General;
using SparFlame.Utils;
using Unity.Entities;
namespace SparFlame.GamePlaySystem.Resource
{



    public enum ResourceType
    {
       
        SoulPact = 0, // Basic Population
        Essence = 1,  // Everywhere, generate and harvest 
        
        LightEnergy = 2, // Only generate
        DarkEnergy = 3, // Only generate
        
        Luminite = 4, //Only Harvest
        Obsidian = 5, // Only Harvest

        Aetherium = 6, // Only Harvest
        
        StarLight = 7,  //  Only Harvest
        NetherFlame = 8,  // Only Harvest

        BloodCrystal = 9, //  Only Harvest
       
        SoulMist = 10, // Generate and harvest
        ArcaneEnergy = 11, // Generate and harvest
        
        ChaosShard = 12, //  only harvest
        RelicFragments = 13, //  only harvest
        
        OathOfLight = 14,// Light high-level population
        ShadowCovenant = 15 // Dark high-level population
    }

    public struct ResourceAttr : IComponentData
    {
        public CustomDs.Range AmountRange; 
        public ResourceType Type;
    }

    public struct RenewableData : IComponentData
    {
        public float RegenerationTimeSeconds;
        public float RegeneratingLeftTime;
    }
    
    public struct RegeneratingTag : IComponentData
    {
        
    }
    
    public struct ResourceEntityPrefabData : IEntityPrefabData<ResourceType>
    {
        public Entity Prefab { get; set; }
        public ResourceType Type { get; set; }
        public int GlobalIdx { get; set; }
        public float Probability;
        public CustomDs.Range AmountRange;
    }
}