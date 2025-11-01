using SparFlame.Components.General;
using Unity.Entities;

namespace SparFlame.Components.MainGameplay
{
  
    public struct TowerChangeCityThreatenRequest : IComponentData
    {
        public Tier TowerTier;
        public bool IsAdd;
    }
    public struct UnitChangeCityThreatenRequest : IComponentData
    {
        public int Level;
        public int Count;
        public bool IsMage;
        public bool IsAdd;
    }
}