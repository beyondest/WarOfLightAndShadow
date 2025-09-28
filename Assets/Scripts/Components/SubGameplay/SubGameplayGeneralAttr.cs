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
        public FactionTag Faction;
        public SubFactionTag SubFaction;
    }

    public struct BoxColliderSize : IComponentData
    {
        public float3 Box;
        public float Radius;
    }
}