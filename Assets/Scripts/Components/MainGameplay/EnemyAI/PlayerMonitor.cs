using SparFlame.Components.General;
using Unity.Entities;

namespace SparFlame.Components.MainGameplay
{
  
    
    public struct TowerChangeCityThreatenRequest : IComponentData
    {
        public bool IsAdd;
        public Tier TowerTier;
    }

    public struct UnitChangeCityThreatenRequest : IComponentData
    {
        public bool IsMage;
        public bool IsAdd;
        public int Level;
        public int Count;
    }
}