using Unity.Entities;

namespace SparFlame.Components.General
{
   
    public enum FactionTag
    {
        Neutral = 0,
        Ally = 1,
        Enemy = ~1,
    }
    
    public enum Tier
    {
        Tier1 = 3,
        Tier2 = 4,
        Tier3 = 5,
        // Tier4 = 6,
        // Tier5 = 7,
    }

    public enum SubFaction
    {
        None = 0,
        LightFaction1 = 1,
        LightFaction2 = 2,
        LightFaction3 = 3,
        DarkFaction1 = 4,
        DarkFaction2 = 5,
        DarkFaction3 = 6,
    }
   
    
   
}