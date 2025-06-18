using SparFlame.Components.General;
using Unity.Entities;

namespace SparFlame.Components.SubGameplay
{
    public struct ResourceMineGenerateAttr : IComponentData
    {
        public ResourceType GenerateResourceType;
        public int MinCultivatorsRequireToGenerate;
        public float CurGenerateSpeed;
    }

    public struct PlantGenerateAttr : IComponentData
    {
        public ResourceType GenerateResourceType;
        public float GenerateSpeed;
    }
    
    public struct GeneratingTag : IComponentData
    {
        
    }
    public struct GenerateData : IComponentData
    {
        public float GenerateTime;
    }
}