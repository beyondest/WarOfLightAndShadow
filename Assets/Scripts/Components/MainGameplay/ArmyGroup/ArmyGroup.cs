using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Components.MainGameplay
{
    // Army Group Attr

    public enum ArmyGroupIconType
    {
        Horse = 0,
        Rabbit = 1,
        Wolf = 2,
        Deer = 3,
        Bear = 4,
        Snake = 5,
        Lion = 6,
        Scorpion = 7,
        Butterfly = 8,
        Dragon = 9
    }


    [Serializable]
    public struct ArmyGroupAttr : IComponentData
    {
        // Static data
        public ArmyGroupIconType iconType;
        public FixedString32Bytes gameplayName;
        public float createTimeInTotalHours;

        // Unit data
        public int avgLevel;
        // public int tier1UnitCount;
        // public int tier2UnitCount;
        // public int tier3UnitCount;

        // This is used to record units formation info
        public float2 boundingBoxDelta;

        public float3
            loadingCenter; // This value should be set when an army group garrisons a city or leaves a garrisoned city

        public float loadingScale;
    }

    [Serializable]
    public struct ArmyGroupStatData : IComponentData
    {
        public float totalMaxHp;
        public float totalCurrentHp;
    }


    public struct LastPassingByCity : IComponentData
    {
        public Entity City;
    }
}