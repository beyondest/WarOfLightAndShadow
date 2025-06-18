using SparFlame.Components.General;
using Unity.Entities;

namespace SparFlame.Components.SubGameplay
{
    public struct DwellingAttr : IComponentData
    {
        public ResourceType ResourceType;
        public int Amount;
    }

    public struct DwellingGeneratePopulationTag : IComponentData,IEnableableComponent
    {
        
    }

}