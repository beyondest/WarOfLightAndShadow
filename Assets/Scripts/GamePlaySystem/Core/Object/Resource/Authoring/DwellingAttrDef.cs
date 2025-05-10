using Unity.Entities;

namespace SparFlame.GamePlaySystem.Resource
{
    

    // TODO This attribute needs to be in building folder?
    public struct DwellingAttr : IComponentData
    {
        public ResourceType ResourceType;
        public int Amount;
    }

    public struct DwellingGeneratePopulationTag : IComponentData
    {
        
    }
}