using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Units
{
    // public class UnitAttributesDef : MonoBehaviour
    // {
    //     
    //     public UnitType unitType;
    //     public int subTypeIndex;
    //     
    //     class UnitAttributesAuthoringBaker : Baker<UnitAttributesDef>
    //     {
    //         public override void Bake(UnitAttributesDef def)
    //         {
    //             var entity = GetEntity(TransformUsageFlags.Dynamic);
    //             AddComponent(entity, new UnitAttr
    //             {
    //                 Type = def.unitType,
    //                 SubTypeIndex = def.subTypeIndex,
    //             });
    //             AddComponent<GarrisonStateTag>(entity);
    //             SetComponentEnabled<GarrisonStateTag>(entity, false);
    //         }
    //     }
    // }

    public struct EnemyUnitBelongsTo : IComponentData
    {
        public Entity Base;
    }
    

    public struct UnitAttr : IComponentData
    {
        public UnitType Type;
        public int SubTypeIndex;
        public float ConjureSpeedSecondPerUnit;
    }

    public struct AttunerAttr : IComponentData
    {
        public float GenerateSpeedBonus;
    }
    
    public enum UnitType
    {
        Shield = 0, // Attack
        Ranged = 1,// Attack
        Magic = 2, // Attack, heal
        Cavalry = 3, // Attack
        Worker = 4 // Attack, harvest
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
    
    
    public struct UnitEntityPrefabData : IEntityPrefabData<UnitType>
    {
        public Entity Prefab { get; set; }
        public UnitType Type { get; set; }
    }
    
    
}
