using Unity.Entities;

namespace SparFlame.Components.General
{
    public struct MapInfo : IComponentData
    {
        public float CameraMaxCoordinate;
        public float CameraMinCoordinate;
    }

    public struct CurrentSubMapInfo : IComponentData
    {
        public MapInfo MapInfo;
    }
}