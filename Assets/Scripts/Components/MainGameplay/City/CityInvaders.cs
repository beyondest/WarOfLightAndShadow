using Unity.Entities;

namespace SparFlame.Components.MainGameplay
{
    public struct RemoveCityFutureInvaderRequest : IComponentData
    {
        public Entity ArmyGroup;
        public Entity City;
    }
    public struct CityFutureInvaders : IBufferElementData
    {
        public Entity ArmyGroup;
    }
}