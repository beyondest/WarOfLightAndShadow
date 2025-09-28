using Unity.Entities;
using Unity.Transforms;

namespace SparFlame.Components.SubGameplay
{
    public enum StandPositionType
    {
        Front,
        Middle,
        Back,
        Side,
    }

    public struct FormationTransform : IComponentData
    {
        public LocalTransform Transform;
    }

    

}