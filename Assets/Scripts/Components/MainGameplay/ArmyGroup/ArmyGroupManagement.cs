using System;
using SparFlame.Components.SubGameplay;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Components.MainGameplay
{
    public struct InArmyGroup : IComponentData
    {
        public Entity BelongsTo;
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
        public AddToArmyGroupType Type;
        public Entity ArmyGroup;
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
        public RemoveFromArmyGroupType RemoveType;
        public int StatMaxValue;
    }

    
    // Army Group Composition Data
    public struct ArmyGroupUnit : IBufferElementData
    {
        public Entity Unit;
        public int GlobalId;
        public long SaveTmpId;
    }

    public struct ArmyGroupUnitTypeData : IBufferElementData, IEquatable<ArmyGroupUnitTypeData>
    {
        public UnitType UnitType;
        public int Id;
        public int Count;

        public bool Equals(ArmyGroupUnitTypeData other)
        {
            return Id == other.Id;
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