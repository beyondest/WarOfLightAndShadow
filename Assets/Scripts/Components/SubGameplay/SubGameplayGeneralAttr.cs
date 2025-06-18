using SparFlame.Components.General;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Components.SubGameplay
{
    public enum BaseTag
    {
        Units,
        Buildings,
        Resources
    }
    public struct SubGameplayGeneralAttr : IComponentData
    {
        public BaseTag BaseTag;
        public FactionTag FactionTag;
        public float3 BoxColliderSize;
        public int ID;
    }
}