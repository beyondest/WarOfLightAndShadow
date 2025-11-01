using Unity.Entities;

namespace SparFlame.Components.MainGameplay
{
    public struct ArmyGroupGenerateSightRequest : IComponentData
    {
        public Entity Prefab;
    }
    public struct ArmyGroupSightTarget : IBufferElementData
    {
        public Entity Entity;
    }
}