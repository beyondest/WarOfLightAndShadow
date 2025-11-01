using System;
using SparFlame.Components.SubGameplay;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Components.MainGameplay
{
    public struct InArmyGroup : IComponentData
    {
        public Entity BelongsTo;
        public long SingleId;
    }

    
    // Request
    public enum AddToArmyGroupType
    {
        AllSelectedExceptAlreadyIn = 0,
        AllSelectedOverrideAlreadyIn = 1,
        OnlySpecifiedUnit = 2
    }

    public struct AddToArmyGroupRequest : IComponentData
    {
        public Entity Unit;
        public Entity ArmyGroup;
        public AddToArmyGroupType Type;
    }

 

    public enum RemoveFromArmyGroupType
    {
        RemoveSpecifiedUnitWithoutRemovingInArmyGroup, // The InArmyGroup component does not need to be removed in 2 cases: 1. When a unit died 2. When player override the army group of a unit 
        MoveOutAllSameId,
        MoveOutAll,
        RandomRemoveSingleSameId,
    }
    
    public struct RemoveFromArmyGroupRequest : IComponentData
    {
        public Entity Unit;
        public Entity ArmyGroup;
        public int MoveOutId;
        public int StatMaxValue;
        public RemoveFromArmyGroupType RemoveType;
    }

    
    // Army Group Composition Data
    public struct ArmyGroupUnit : IBufferElementData
    {
        public Entity Unit;
        public long SingleId;
        public int PrefabId;
    }

    public struct ArmyGroupUnitTypeData : IBufferElementData, IEquatable<ArmyGroupUnitTypeData>
    {
        public int PrefabId;
        public int Count;
        public UnitType UnitType;
        public bool Equals(ArmyGroupUnitTypeData other)
        {
            return PrefabId == other.PrefabId;
        }
    }

    
    // Config data
    public struct ArmyGroupConfig : IComponentData
    {
        public float3 HidePosition;
        public Entity LightArmyGroupPrefab;
        public Entity DarkArmyGroupPrefab;
    }
}