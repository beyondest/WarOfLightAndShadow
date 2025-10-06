using System;
using SparFlame.Core.Interfaces;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Components.SubGameplay
{


    [Serializable]
    public struct UnitAttr : IComponentData,IEquatable<UnitAttr>
    {
        public UnitType type;
        public int subTypeIndex;
        public float conjureSpeedHoursPerUnit;

        public bool Equals(UnitAttr other)
        {
            return type == other.type && subTypeIndex == other.subTypeIndex ;
        }
        public override int GetHashCode()
        {
            var int2 = new int2((int)type, subTypeIndex);
            return int2.GetHashCode();
        }
    }

    public struct ShieldTag : IComponentData{}
    public struct ArcherTag : IComponentData{}
    public struct ClericTag : IComponentData{}
    public struct DualSpearTag : IComponentData{}
    public struct WorkerTag : IComponentData{}
    public struct SpellSwordTag : IComponentData{}
    public struct GreatSwordTag : IComponentData{}
    public struct MageTag : IComponentData{}
    
    public enum UnitType
    {
        Shield = 0, 
        Archer = 1,
        Cleric = 2, 
        DualSpear = 3, // Attack
        Worker = 4 ,
        SpellSword = 5,
        GreatSword = 6,
        Mage = 7
    }

    public enum ShieldType
    {
        Guardian = 0,  // shieldBearer, royal guard, guardian
        Paladin = 1, // holy warrior, guardian
    }

    public enum RangedType
    {
        Archer = 0, //  archer, marksman, warbow
        Sniper = 1, // hunter, sentinel, sharpshooter
        Ranger // skirmisher 流射 ranger
    }

    public enum MagicType
    {
        /// <summary>
        ///  Heal and buff
        /// </summary>
        Cleric = 0, // healer, cleric, sage
        /// <summary> 
        /// Attack
        /// </summary>
        Mage = 1, // apprentice mage, mage, archmage
        /// <summary>
        /// Heal and attack
        /// </summary>
        Prophet = 2,  // Ritualist, prophet, oracle 
        /// <summary>
        /// Attack and buff
        /// </summary>
        Buffer = 3,  // Enchanter, warlock
    }

    public enum CavalryType
    {
        BalancedCavalry = 0, // rider, cavalier, heavy cavalry
        Skirmisher = 1, // scout, skirmisher, charger
        Knight = 2 // vanguard, knight
    }

    public enum WorkerType
    {
        Harvester = 0, // gatherer, prospector, harvester
        Attuner = 1, // Cultivator, botanist, druid
    }

    public enum SpellSwordType
    {
        Basic = 0,
    }

    public enum GreatSwordType
    {
        Basic = 0
    }
    
    public struct UnitEntityPrefabData : IEntityPrefabData<UnitType>
    {
        public Entity Prefab { get; set; }
        public UnitType Type { get; set; }
        public int PrefabId { get; set; }
    }
    
}