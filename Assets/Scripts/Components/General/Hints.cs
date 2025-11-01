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
        
        TargetNotReachable = 27,
        ArcherUnitCountNotEnoughToCastArrowRain = 28,
        EnemyBeginFocusOnPlayer = 29,
        EnemyStopFocusOnPlayer = 30,
        EnemyIsRaisingAGarrison = 31,
        EnemyIsRaisingAStrikeForce = 32,
        EnemyShieldArmyGroupCastSkill = 33,
        EnemyArcherCastSkill = 34,
        EnemyClericArmyGroupCastSkill = 35,
        EnemyDualSpearArmyGroupCastSkill = 36,
        EnemyWorkerArmyGroupCastSkill = 37,
        EnemySpellSwordArmyGroupCastSkill = 38,
        EnemyGreatSwordArmyGroupCastSkill = 39,
        EnemyMageArmyGroupCastSkill = 40,
        
        AllyShieldArmyGroupCastSkill = 41,
        AllyArcherArmyGroupCastSkill = 42,
        AllyClericArmyGroupCastSkill = 43,
        AllyDualSpearArmyGroupCastSkill = 44,
        AllyWorkerArmyGroupCastSkill = 45,
        AllySpellSwordArmyGroupCastSkill = 46,
        AllyGreatSwordArmyGroupCastSkill = 47,
        AllyMageArmyGroupCastSkill = 48,
        
        EnemyIsAttackingYourAllies = 49,
        EnemyArmyGroupIsBeingDestroyedByYourAllies = 50,
        
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
        public FixedString128Bytes Content;
        public HintName Name;
        public HintType Type;
    }
}