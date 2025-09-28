using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Components.General
{
    
    public enum HintName
    {
        None = 0,
        ResourceTierNotMatch = 1,
        ResourceAmountNotEnough = 2,
        ConstructOverlapping = 3,
        ConstructOnNotConstructable = 4,
        CrystalUnderAttack = 5,
        
        EnemyIsAssemblingAttackTeam = 6,
        EnemyIsAssemblingDefenseTeam = 7,
        EnemyIsAssemblingGatheringTeam = 8,
        EnemyIsAssemblingStrikeTeam = 9,
        
        NotEnoughResource = 10,
        CrystalCannotRelocate = 11,
        CannotRelocateWhenUnderAttack = 12,
        CannotRelocateWhenHasGarrisonUnits = 13,
        CannotRelocateWhenConstructing = 14,
        CrystalCannotRecycle = 15,
        CannotRecycleWhenUnderAttack = 16,
        CannotRecycleWhenConstructing = 17,
        
        TargetNotGarrisonable = 18,
        
        ArmyGroupNotReachable = 19,
        ArmyGroupCountExceededInCity = 20,
        PleaseDeleteArmyGroupLastTargetForNewTarget = 21,
        UnitInArmyGroupCannotGarrisonInBuilding = 22,
        
        PlayerRetreatedArmyGroupBackToLastPassingByCity = 23,
        EnemyRetreatedArmyGroupBackToLastPassingByCity = 24,
        
        EnemyIsGoingToInvade = 25,
        DebugEnemyArmyGroupNotReachable = 26,
    }

    public enum HintType
    {
        Info = 0,
        Warning = 1,
        Critical = 2,
    }

    public struct HintRequest : IComponentData
    {
        public float3 Position;
        public HintName Name;
    }

    public struct HintsInfo : IBufferElementData
    {
        public FixedString128Bytes Content;
        public float UpdateTime;
        public HintType HintType;
    }

    public struct HintConfigs : IBufferElementData
    {
        public HintName Name;
        public HintType Type;
        public FixedString128Bytes Content;
    }

}