using SparFlame.Components.General;
using Unity.Entities;

namespace SparFlame.Components.SubGameplay
{
   

    public struct GenerateAttr : IComponentData
    {
        public float GenerateSpeedHoursPerUnit;
        public int MinCultivatorsRequireToGenerate;
        public ResourceType GenerateResourceType;
    }
    
    public struct GeneratingTag : IComponentData
    {
        
    }
 
}