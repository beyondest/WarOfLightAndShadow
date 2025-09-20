using System;
using SparFlame.Components.SubGameplay;
using Unity.Entities;

namespace SparFlame.Components.MainGameplay
{
    // This component add when enemy AI generate army gruops
    public struct EnemyArmyGroupBelongsToCity : IComponentData
    {
        public Entity City;
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
    }
    public struct ArmyGroupCommandUpdate : IComponentData, IEnableableComponent{}
    

    [Serializable]
    public struct ArmyGroupThreatenCalculationConfig : IComponentData
    {
        public float nonMagicUnitBaseThreatenValue;
        public float magicUnitBaseThreatenValue;
        public float levelCoefficientA;
        public float levelCoefficientB;
        public float levelCoefficientC;
        // unit threaten value = base + EnemyAIUtils.EvaluateLevelAddThreaten(level, a, b, c)
        
        public float towerBaseThreatenValue;
        public float towerTierMultiplier;
    }

    [Serializable]
    public struct VeryRadicalPossibility : IBufferElementData
    {
        public float minSelfTotalThreaten;
        public float maxSelfTotalThreaten;
        public float invadeChance;
    }
    
    [Serializable]
    public struct FormationConfig : IComponentData
    {
        public float squareSpacing;
    }


    public struct EnemyArmyGroupShouldSaveTag : IComponentData{}
    public struct EnemyArmyGroupSaveTag : IComponentData {}
    
}