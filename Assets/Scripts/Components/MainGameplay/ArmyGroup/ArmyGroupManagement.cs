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
    }


 

    public enum RemoveFromArmyGroupType
    {
        RemoveSpecifiedUnit,
        MoveOutAllSameId,
        MoveOutAll,
        RandomRemoveSingleSameId,
    }
    
    public struct RemoveFromArmyGroupRequest : IComponentData
    {
        public Entity Unit;
        public Entity ArmyGroup;
        public int MoveOutId;
        public RemoveFromArmyGroupType Type;
    }

    
    // Army Group Composition Data
    public struct ArmyGroupUnit : IBufferElementData
    {
        public Entity Unit;
        public int Id;
    }

    public struct ArmyGroupUnitTypeData : IBufferElementData
    {
        public UnitType UnitType;
        public int Id;
        public int Count;
    }

    
    // Config data
    public struct ArmyGroupManageConfig : IComponentData
    {
        public float3 HidePosition;
        public Entity LightArmyGroupPrefab;
        public Entity DarkArmyGroupPrefab;
    }
}