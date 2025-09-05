using System;
using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Components.MainGameplay
{
    


    // Army Group Attr

    public enum ArmyGroupIconType
    {
        Bear,
        Butterfly,
        Dragon,
        Deer,
        Horse,
        Lion,
        Rabbit,
        Scorpion,
        Snake,
        Wolf
    }
    
    [Serializable]
    public struct ArmyGroupAttr : IComponentData
    {
        public ArmyGroupIconType iconType;
        public FixedString32Bytes gameplayName;
        
        // This id is generated when new an army group, and will never duplicate nor change.
        public long saveId;
    }

    public struct LastPassingByPlayerCity : IComponentData
    {
        public Entity City;
    }


    // Army Group Selection Data


    

    
  
    
}