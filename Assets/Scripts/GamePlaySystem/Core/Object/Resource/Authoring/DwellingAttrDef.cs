using Unity.Entities;

namespace SparFlame.GamePlaySystem.Resource
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