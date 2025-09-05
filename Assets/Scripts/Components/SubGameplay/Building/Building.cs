using System;
using SparFlame.Components.General;
using SparFlame.Core.Interfaces;
using Unity.Entities;

namespace SparFlame.Components.SubGameplay
{
    public enum BuildingState
    {
        Idle = 0,
        Constructing = 1,
        Working = 2,
        UnderAttack = 3,
        // Worked = 4, // Obsolete
    }


    [Serializable]
    public struct ConstructingTimer : IComponentData
    {
        public float builtUpTargetTotalHours;
    }

    public struct BuildingAttr : IComponentData
    {
        public BuildingType Type;
        public int SubTypeIndex;
        public float ConstructTimeHours;
    }

    public enum BuildingType
    {
        Fortifications = 0,
        Generators = 1,
        ConjuringShrines = 2,
        CapacityBuildings = 3,
        Ornaments = 4,
    }

    public enum ConjuringShrineType
    {
        AegisShrine = 0, // Stone Sigil（石之符印）, Ward Circle（守护法阵）, Bulwark Core（壁垒核心）
        StormSpire = 1, // Wind Glyph（风之印记）, Arrow Rift（箭矢裂隙）, Tempest Spire（风暴尖塔）
        EldritchSeal = 2, // Mana Ring（法力之环）, Arcane Core（奥术核心）, Eldritch Pillar（神秘石柱）
        PhantomGate = 3, // Wild Rift（野性裂隙）, Charge Beacon（充能灯塔）, Thunder Gate（雷霆之门）
        TerraNexus = 4 // Rune Pad（符文阵盘）, Soul Anchor（灵魂锚点）, Golem Crucible（魔像熔炉）
    }

    public enum CapacityBuildingType
    {
        Dwelling = 0,
        ManaPool = 1,
        CrystalStoreHouse = 2,
    }


    public enum FortificationType
    {
        Wall = 0, // Fence（木栅栏）, Rampart（防坡墙）, Bastion（棱堡）
        Tower = 1, // Watchpost（瞭望哨）, Guard Tower（守卫塔）, Keep（主堡楼）
        BigTower = 2,
    }


    public enum GeneratorType
    {
        ResourceMine = 0,
        PlantGenerator = 1,
    }

    public enum ResourceMineType
    {
        Mine0 = 0,
        Mine1 = 1,
        Mine2 = 2,
    }

    public enum PlantGeneratorType
    {
        Plant0 = 0,
        Plant1 = 1,
        Plant2 = 2,
    }


    public enum OrnamentType
    {
        Crystal = 0, // Shard（水晶碎片）, Cluster（水晶簇）, Monolith（晶体巨柱）
        Beacon = 1,

        Others = 3
        // StorableOrnament = 1,      // Crate（储物箱）, Depot（储存站）, Warehouse（仓库）
        // UnStorableOrnament = 2,    // Relic（遗物）, Totem（图腾柱）, Monument（纪念碑）
    }


    public struct BuildingEntityPrefabData : IEntityPrefabData<BuildingType>
    {
        public Entity Prefab { get; set; }
        public BuildingType Type { get; set; }
        public int GlobalIdx { get; set; }
    }
    public struct CapacityBuildingAttr : IComponentData
    {
        public ResourceType ResourceType;
        public int StorageAmount;
    }



    public struct BuildingUtils
    {
        public static BuildingState GetBuildingState(bool underAttack, bool constructing, bool conjuring,
            bool generating)
        {
            if (underAttack) return BuildingState.UnderAttack;
            if (constructing) return BuildingState.Constructing;
            if (conjuring || generating) return BuildingState.Working;
            return BuildingState.Idle;
        }
    }
}