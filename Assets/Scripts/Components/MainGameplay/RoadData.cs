using System;
using Unity.Entities;

namespace SparFlame.Components.MainGameplay
{
    [Serializable]
    public struct DirectRoadPointData : IBufferElementData
    {
        public int cityAId;
        public int cityBId;
        public float directDistance;
    }

    public struct RoadPointData : IBufferElementData
    {
        public int CityAId;
        public int CityBId;
        public float TotalDistance;
    }
}