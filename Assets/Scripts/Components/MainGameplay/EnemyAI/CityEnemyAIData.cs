using System;
using Unity.Collections;
using Unity.Entities;
using Random = Unity.Mathematics.Random;

namespace SparFlame.Components.MainGameplay
{
    public enum EnemyCityStrategy
    {
        VeryConservative, // Always conjure and stay. Not invade at all.
        Conservative, // Conjure defenders first, then conjure attackers, if buffer is full and attackers if full hp, go fight
        Radical, // Conjure attackers first, if attackers is ready, check and invade; then check and conjure defenders
        VeryRadical, // Will check target's threaten value, if existing army groups are stronger than target, then invade; otherwise, conjure attacker army groups first
    }

    public enum EnemyArmyGroupDuty
    {
        Attack,
        Defend
    }


    // Need to be initialized each time enter game; no need to be saved
    // Will be useless when its target is player
    // These cities should not contain support fight city id
    public struct CheckCity : IBufferElementData
    {
        public int CityId;
    }



    // Spawn army group in sequence, no any other logic
    public struct AttackArmyGroupPrefab : IBufferElementData
    {
        public Entity ArmyGroupPrefab;
        public float NeedHours;

    }

    public struct DefendArmyGroupPrefab : IBufferElementData
    {
        public Entity ArmyGroupPrefab;
        public float NeedHours;
    }

    public struct AttackArmyGroup : IBufferElementData
    {
        public Entity ArmyGroup;
        // public ArmyGroupIconType IconType;
    }

    public struct InvadingArmyGroup : IBufferElementData
    {
        public Entity ArmyGroup;
    }

    public struct DefendArmyGroup : IBufferElementData
    {
        public Entity ArmyGroup;
        // public ArmyGroupIconType IconType;
    }

    public struct ExtraArmyGroup : IBufferElementData
    {
        public Entity ArmyGroup;
    }

    // Need to be saved and updated when first time loaded; will update during gameplay
    public struct InvadeTarget : IBufferElementData
    {
        public Entity City;
        public int CityId;
    }
    

    public struct ArmyGroupConjureStack : IBufferElementData
    {
        public Entity ArmyGroupPrefab;
        public float NeedHours;
        public EnemyArmyGroupDuty Duty;
    }

    public struct CityAIData : IComponentData
    {
        public float StartConjuringTotalHours;
        public EnemyCityStrategy Strategy;
        public Random Rnd;
        public int FightCountWithPlayer;
    }

    public struct FocusOnPlayerTag : IComponentData, IEnableableComponent
    {
    }

    public struct CheckFocusPlayerRequest : IComponentData
    {
        public Entity EnemyCity;
    }
}