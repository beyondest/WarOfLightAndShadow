using Unity.Entities;

namespace SparFlame.Components.MainGameplay
{
    public struct RemoveCityFutureInvaderRequest : IComponentData
    {
        public Entity ArmyGroup;
        public Entity City;
    }

    public struct ClearCityFutureInvadersRequest : IComponentData
    {
        public Entity City;
    }
    public struct CityFutureInvaders : IBufferElementData, ICityArmyGroupElement
    {
        public Entity ArmyGroup { get; set; }
        public long SingleId { get; set; }
    }
}