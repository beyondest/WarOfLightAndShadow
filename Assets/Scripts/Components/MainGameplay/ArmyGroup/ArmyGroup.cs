using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Components.MainGameplay
{
    // Army Group Attr

    // Deprecated
    /*public enum ArmyGroupIconType
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
    }*/
    public enum ArmyGroupIconType
    {
        Shield = 0, 
        Archer = 1,
        Cleric = 2, 
        DualSpear = 3,
        Worker = 4 ,
        SpellSword = 5,
        GreatSword = 6,
        Mage = 7
    }


    [Serializable]
    public struct ArmyGroupAttr : IComponentData
    {
        public FixedString32Bytes gameplayName;
        public float3 loadingCenter; 
        public float2 boundingBoxDelta;
        public float createTimeInTotalHours;
        public float loadingScale;
        public int avgLevel;
        public ArmyGroupIconType iconType;
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
        public long SingleId;
    }

    public struct ArmyGroupEntityPrefabData : IBufferElementData
    {
        public Entity Prefab;
        public int PrefabId;
    }
}