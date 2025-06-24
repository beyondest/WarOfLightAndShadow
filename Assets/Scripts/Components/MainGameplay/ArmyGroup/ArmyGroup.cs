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
    public struct ArmyGroupAttr : IComponentData
    {
        public ArmyGroupIconType IconType;
        // public int ArmyGroupId;
        public FixedString32Bytes GameplayName;
        public long SaveId;
    }

    public struct LastPassingByPlayerCity : IComponentData
    {
        public Entity City;
    }


    // Army Group Selection Data


    

    
  
    
}