using SparFlame.Components.General;
using Unity.Entities;

namespace SparFlame.Components.SubGameplay
{
   

    public struct GenerateAttr : IComponentData
    {
        public ResourceType GenerateResourceType;
        public float GenerateSpeedHoursPerUnit;
        public int MinCultivatorsRequireToGenerate;

    }
    
    public struct GeneratingTag : IComponentData
    {
        
    }
 
}