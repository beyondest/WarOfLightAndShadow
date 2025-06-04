using SparFlame.GamePlaySystem.Resource;
using Unity.Entities;

namespace SparFlame.GamePlaySystem.Generate
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
    /// <summary>
    /// InternalData
    /// </summary>
    public struct GenerateData : IComponentData
    {
        public float GenerateTime;
    }
    
    

}