using Unity.Entities;

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

    public interface ICityArmyGroupElement
    {
        Entity ArmyGroup { get; set; }
        long SingleId { get; set; }
    }

    public interface ICityArmyGroupPrefabElement
    {
        Entity Prefab { get; set; }
        float NeedHours { get; set; }
        int PrefabId { get; set; }
    }
    
    // Need to be initialized each time enter game; no need to be saved
    // Will be useless when its target is player
    // These cities should not contain support fight city id
    public struct CheckCity : IBufferElementData
    {
        public int CityId;
    }


    // Spawn army group in sequence, no any other logic
    public struct AttackArmyGroupPrefab : IBufferElementData,ICityArmyGroupPrefabElement
    {
        public Entity Prefab { get; set; }
        public float NeedHours { get; set; }
        public int PrefabId { get; set; }
    }

    public struct DefendArmyGroupPrefab : IBufferElementData,ICityArmyGroupPrefabElement
    {
        public Entity Prefab { get; set; }
        public float NeedHours { get; set; }
        public int PrefabId { get; set; }
    }

    public struct AttackArmyGroup : IBufferElementData, ICityArmyGroupElement
    {
        public Entity ArmyGroup { get; set; }
        public long SingleId { get; set; }
    }

    public struct InvadingArmyGroup : IBufferElementData,ICityArmyGroupElement
    {
        public Entity ArmyGroup { get; set; }
        public long SingleId { get; set; }
    }

    public struct DefendArmyGroup : IBufferElementData,ICityArmyGroupElement
    {
        public Entity ArmyGroup { get; set; }
        public long SingleId { get; set; }
    }

    public struct ExtraArmyGroup : IBufferElementData,ICityArmyGroupElement
    {
        public Entity ArmyGroup { get; set; }
        public long SingleId { get; set; }
    }

    // Need to be saved and updated when first time loaded; will update during gameplay
    public struct InvadeTarget : IBufferElementData
    {
        public Entity City;
        public long SingleId;
        public int CityPrefabId;
    }


    public struct ArmyGroupConjureStack : IBufferElementData
    {
        public float NeedHours;
        public int PrefabId;
        public EnemyArmyGroupDuty Duty;
    }

    public struct CityAIData : IComponentData
    {
        public float StartConjuringTotalHours;
        public int FightCountWithPlayer;
        public EnemyCityStrategy Strategy;
        public bool IsFocusOnPlayer;
    }


    public struct CheckFocusPlayerRequest : IComponentData
    {
        public Entity EnemyCity;
    }
}