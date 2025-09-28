using System;
using Unity.Entities;
using Random = Unity.Mathematics.Random;

namespace SparFlame.Components.General
{
   
    public enum FactionTag
    {
        Neutral = 0,
        Light = 1,
        Dark = ~1,
    }
    
    public enum Tier
    {
        Tier1 = 3,
        Tier2 = 4,
        Tier3 = 5,
        // Tier4 = 6,
        // Tier5 = 7,
    }

    public enum SubFactionTag
    {
        None          = 1 << 0,
        LightFaction1 = 1 << 1,  
        LightFaction2 = 1 << 2,  
        LightFaction3 = 1 << 3,  
        DarkFaction1  = 1 << 4,  
        DarkFaction2  = 1 << 5,  
        DarkFaction3  = 1 << 6,  
    }

    public enum Relationship
    {
        Neutral = 0,
        Ally = 1,
        Hostile = ~1,
        Self = 2
    }

    [Serializable]
    public struct GlobalSingleId : IComponentData
    {
        public long value;
    }
    public struct AssignGlobalSingleIDRequest : IComponentData{}

    [Serializable]
    public struct PrefabId : IComponentData
    {
        public int value;
    }
   
    public struct NeedSaveTag : IComponentData, IEnableableComponent
    {
    }

    [Serializable]
    public struct Rnd : IComponentData
    {
        public Random value;
    }
    public struct AssignRandomRequest : IComponentData{}
}