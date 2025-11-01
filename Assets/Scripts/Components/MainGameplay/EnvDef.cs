using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.Components.MainGameplay
{
    public enum EcoType
    {
        Unknown = 0,
        EcoType1 = 1,
        EcoType2 = 2,
    }
    [Serializable]
    public struct EcoColorEntry
    {
        /// <summary>
        /// This color is the Env Png Config, e.g., black for Central Grove, white for Obsidian Expanse
        /// </summary>
        public Color color; 
        public EcoType eco;
    }
    public struct EcoTypeToEcoConfig : IBufferElementData
    {
        public Entity EcoEntity;
        public EcoType EcoType;
    }
    [Serializable]
    public struct LoadingPositionInfo : IComponentData
    {
        public float3 defenderPosition;
        public float3 invaderPosition;
        public float invaderPositionSquareSize;
        public float defenderPositionSquareSize;
    }
    
    [Serializable]
    public struct DirectRoadPointData : IBufferElementData
    {
        public float directDistance;
        public int cityAId;
        public int cityBId;
    }

    public struct RoadPointData : IBufferElementData
    {
        public float TotalDistance;
        public int CityAId;
        public int CityBId;
    }
}