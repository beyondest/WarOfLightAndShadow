using System;
using SparFlame.Components.SubGameplay;
using Unity.Entities;

namespace SparFlame.Components.MainGameplay
{
    // This component add when enemy AI generate army gruops
    public struct EnemyArmyGroupBelongsToCity : IComponentData
    {
        public Entity City;
        public long SingleId;
    }

    public struct EnemyArmyGroupCompositionData : IBufferElementData
    {
        public Entity UnitPrefab;
        public int Count;
        public int Level;
    }

    [Serializable]
    public struct ArmyGroupThreatenData : IComponentData
    {
        public UnitType mainUnitType;
        public float totalThreatenValue;
    }

    public struct ArmyGroupCommandData : IComponentData
    {
        public Entity TargetCity;
        public bool WaitForGarrisonOut;
    }
    public struct ArmyGroupCommandUpdate : IComponentData, IEnableableComponent{}
    

    [Serializable]
    public struct ArmyGroupThreatenCalculationConfig : IComponentData
    {
        public float unitBaseThreatenValue;
        public float levelCoefficientA;
        public float levelCoefficientB;
        public float levelCoefficientC;
        // unit threaten value = base + EnemyAIUtils.EvaluateLevelAddThreaten(level, a, b, c)
        
        public float towerBaseThreatenValue;
        public float towerTierMultiplier;
    }

    [Serializable]
    public struct VeryRadicalPossibilityConfig : IBufferElementData
    {
        public float minSelfTotalThreaten;
        public float maxSelfTotalThreaten;
        public float invadeChance;
    }
    
 


    public struct EnemyArmyGroupShouldSaveTag : IComponentData
    {
        public int TotalUnitCount;
    }
    public struct EnemyArmyGroupSaveTag : IComponentData {}

    public struct FakeUnitNeedAddToArmyGroupAfterAssignSingleId : IComponentData
    {
        public Entity ArmyGroup;
    }

    public struct FakeUnitTag : IComponentData
    {
        
    }

}